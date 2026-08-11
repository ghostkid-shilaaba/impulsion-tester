import sys
import json
import re
import math
import time
from pathlib import Path
import pyvisa


CONFIG_FOLDER = Path(__file__).parent / "instrument_configs"

MSG_SETUP_GAIN = "setup_gain"
MSG_CONFIRM_FREQUENCY = "confirm_frequency"
MSG_ENTER_SCREEN_READ = "enter_screen_read"


def debug(msg):
    print(f"DEBUG: {msg}", file=sys.stderr, flush=True)


def extract_prefix(model):
    m = re.match(r'^([A-Za-z]+\d)', model)
    return m.group(1).upper() if m else model.upper()


class ConfigManager:
    def __init__(self, config_folder):
        self.config_folder = config_folder
        self.cache = {}
        self._load_all()

    def _load_all(self):
        if not self.config_folder.exists():
            return
        for json_file in self.config_folder.glob("*.json"):
            try:
                with open(json_file, "r", encoding="utf-8") as f:
                    self.cache[json_file.stem.upper()] = json.load(f)
            except Exception as e:
                debug(f"Failed to load {json_file.name}: {e}")

    def get(self, prefix):
        return self.cache.get(prefix)


def get_command(cfg, name, **values):
    if cfg is None:
        return None
    cmds = cfg.get("commands", {})
    if name not in cmds:
        debug(f"WARNING: Command '{name}' not found in config")
        return None
    entry = cmds[name]
    cmd = entry.get("command", "") if isinstance(entry, dict) else entry
    if "channel" not in values:
        values["channel"] = 1
    consumed = set()
    for k, v in values.items():
        token = "{" + k + "}"
        if token in cmd:
            cmd = cmd.replace(token, str(v))
            consumed.add(k)
    if not cmd.rstrip().endswith("?"):
        leftover = [k for k in values if k not in consumed and k != "channel"]
        if leftover:
            cmd = f"{cmd} {values[leftover[0]]}"
    return cmd


def safe_query_float(inst, cmd):
    if not cmd:
        return None
    try:
        return float(inst.query(cmd).strip())
    except Exception:
        return None


def safe_write(inst, cmd):
    """Write a command and wait for *OPC? to confirm completion."""
    if not cmd:
        return False
    try:
        inst.write(cmd)
        try:
            inst.query("*OPC?")
        except Exception:
            time.sleep(0.05)
        return True
    except Exception as e:
        debug(f"Write failed for '{cmd}': {e}")
        return False


def send_response(response):
    print(json.dumps(response), flush=True)


def read_stdin_json():
    line = sys.stdin.readline()
    if not line:
        raise Exception("VB.NET disconnected (stdin closed)")
    return json.loads(line.strip())


class FrequencyResponseMeasurement:
    """
    ISO 22232-1 Section 9.4.2 - Reponse en frequence de l'amplificateur.

    IMPORTANT (corrige la version precedente) : le canal 1 de l'oscilloscope
    est branche sur la sortie de l'attenuateur externe, c'est-a-dire le
    signal envoye EN ENTREE du recepteur de l'appareil -- pas sa reponse.
    "Tension mesuree" ne sert donc qu'a verifier/compenser que le niveau
    injecte reste a peu pres constant d'une frequence a l'autre (le
    generateur + l'attenuateur ne sont jamais parfaitement plats). La
    reponse reelle de l'appareil (ce que son ecran affiche en % FSH) ne
    peut etre obtenue qu'en la lisant manuellement sur l'ecran de l'U.T,
    exactement comme dans lva_measurement.py -- d'ou l'ajout du message
    MSG_ENTER_SCREEN_READ a chaque frequence.

    Flux par frequence (measure()) :
        1. Le generateur passe a la nouvelle frequence.
        2. MSG_CONFIRM_FREQUENCY est envoye et measure() ATTEND le clic OK
           de l'operateur -- AUCUNE mesure n'a lieu avant ce clic.
        3. Seulement apres ce clic : lecture directe du Vpp (pas
           d'armement SINGLE ni de declenchement logiciel du burst --
           retires pour simplifier ; le generateur est deja en burst
           permanent depuis initialize(), le signal est donc deja stable
           a l'ecran au moment ou l'operateur clique OK).
        4. MSG_ENTER_SCREEN_READ est envoye avec ce Vpp, l'operateur entre
           le % ecran lu sur l'appareil U.T.
        5. Le point (frequence, tension, lu) est enregistre, on passe a la
           frequence suivante.

    Formule "% ecran attendu" / "Ecart dB" (reconstruite depuis la feuille
    Excel d'origine, verifiee cellule par cellule contre le tableau de
    reference) :
        - la frequence de reference est mesuree EN PREMIER (l'IHM envoie
          les frequences dans l'ordre : 7e, puis 1re..6e, puis 8e..13e) --
          c'est la que le gain/l'attenuateur ont ete regles pour amener la
          lecture ecran vers 80 % FSH, conformement au -9.4.2.1 de la
          norme ("adjust... to produce a signal at 80% of the FSH")
        - % ecran attendu(f) = tension(f) / tension(reference) * 80 %
          (colonne d'affichage uniquement, ne sert PAS au calcul de Fl/Fu)
        - Ecart dB(f) = 20*log10( lu(f) / 80 )  -- comparaison a la
          constante 80 %, PAS a "% ecran attendu" (verifie : la feuille
          Excel d'origine divise par une cellule qui vaut TOUJOURS 80% par
          construction, jamais par la valeur attendue ligne par ligne)
      Fl/Fu restent le croisement a -3 dB de cette courbe d'ecart, autour
      du point ou la lecture ecran est la plus haute (Fmax).
    """

    # Point de calibration ISO 22232-1 -9.4.2.1 : "adjust... to produce a
    # signal at 80% of the FSH". C'est TOUJOURS 80%, une constante de la
    # norme -- pas une valeur qu'on relit ou qu'on recalcule.
    REFERENCE_PCT = 80.0

    def __init__(self):
        self.rm = None
        self.scope = None
        self.gen = None
        self.scope_cfg = None
        self.gen_cfg = None
        self.config_manager = ConfigManager(CONFIG_FOLDER)
        self.is_initialized = False

        self.vcc = None
        self.gain_db = None
        self.trigger_cmd = None
        self.points = []  # list of (frequency_mhz, tension_v, screen_read_pct)
        self.v_ref = None  # tension mesuree au tout premier point (= frequence de reference)

        self.fo_constructeur = None
        self.df_constructeur = None

    def initialize(self, config):
        debug("=== INITIALIZE START ===")
        debug(f"Config keys received: {list(config.keys())}")

        self.rm = pyvisa.ResourceManager()
        debug("VISA ResourceManager created")

        self.scope = self.rm.open_resource(config["scope_resource"])
        debug(f"Scope opened: {config['scope_resource']}")

        self.gen = self.rm.open_resource(config["generator_resource"])
        debug(f"Generator opened: {config['generator_resource']}")

        self.scope.timeout = 10000
        self.gen.timeout = 10000

        try:
            self.scope.write("*CLS")
            self.gen.write("*CLS")
        except Exception:
            pass

        scope_idn = config.get("scope_idn") or self.scope.query("*IDN?").strip()
        debug(f"Scope IDN: {scope_idn}")
        gen_idn = config.get("generator_idn") or self.gen.query("*IDN?").strip()
        debug(f"Generator IDN: {gen_idn}")

        self.scope_cfg = config.get("scope_config")
        if not self.scope_cfg:
            scope_model = scope_idn.split(",")[1].strip()
            self.scope_cfg = self.config_manager.get(extract_prefix(scope_model))

        self.gen_cfg = config.get("generator_config")
        if not self.gen_cfg:
            gen_model = gen_idn.split(",")[1].strip()
            self.gen_cfg = self.config_manager.get(extract_prefix(gen_model))

        debug(f"Scope config resolved: {self.scope_cfg is not None}")
        debug(f"Generator config resolved: {self.gen_cfg is not None}")

        if self.scope_cfg is None:
            raise Exception(f"No configuration found for scope:\n{scope_idn}")
        if self.gen_cfg is None:
            raise Exception(f"No configuration found for generator:\n{gen_idn}")

        self.fo_constructeur = config.get("fo_constructeur_mhz")
        self.df_constructeur = config.get("df_constructeur_mhz")
        self.gain_db = config.get("gain_db", 20.0)

        # --- OSCILLOSCOPE (channel 1 = sortie de l'attenuateur externe) ---
        channel_display_cmd = get_command(self.scope_cfg, "channel_display", channel=1)
        if channel_display_cmd:
            safe_write(self.scope, f"{channel_display_cmd} ON")
        else:
            debug("WARNING: 'channel_display' command not found")

        channel_impedance_cmd = get_command(self.scope_cfg, "channel_impedance", channel=1)
        if channel_impedance_cmd:
            safe_write(self.scope, f"{channel_impedance_cmd} OMEG")
        else:
            debug("WARNING: 'channel_impedance' command not found")

        # 1 V/div vertical + 200 ns/div horizontal -- RFA never set either of
        # these (unlike LVA/LDG), so the scope kept whatever the previous
        # test left it in.
        channel_scale_cmd = get_command(self.scope_cfg, "channel_scale", channel=1)
        if channel_scale_cmd:
            safe_write(self.scope, f"{channel_scale_cmd} 1")
        else:
            debug("WARNING: 'channel_scale' command not found")

        timebase_scale_cmd = get_command(self.scope_cfg, "timebase_scale")
        if timebase_scale_cmd:
            safe_write(self.scope, f"{timebase_scale_cmd} 2e-7")
        else:
            debug("WARNING: 'timebase_scale' command not found")

        channel_offset_cmd = get_command(self.scope_cfg, "channel_offset", channel=1)
        if channel_offset_cmd:
            safe_write(self.scope, f"{channel_offset_cmd} 0")
        else:
            debug("WARNING: 'channel_offset' command not found")

        trigger_mode_cmd = get_command(self.scope_cfg, "trigger_mode")
        if trigger_mode_cmd:
            safe_write(self.scope, f"{trigger_mode_cmd} EDGe")
        else:
            debug("WARNING: 'trigger_mode' command not found")

        trigger_source_cmd = get_command(self.scope_cfg, "trigger_source")
        if trigger_source_cmd:
            safe_write(self.scope, f"{trigger_source_cmd} EXT")
        else:
            debug("WARNING: 'trigger_source' command not found")

        trigger_slope_cmd = get_command(self.scope_cfg, "trigger_slope")
        if trigger_slope_cmd:
            safe_write(self.scope, f"{trigger_slope_cmd} POSitive")
        else:
            debug("WARNING: 'trigger_slope' command not found")

        # Active Vpp AVANT toute acquisition (demande explicite) : on
        # nettoie les mesures existantes, on fixe la source sur CH1, puis
        # on "amorce" MEASure:VPP? une premiere fois ici -- avant meme
        # que le premier burst ne soit declenche -- pour que la chaine de
        # mesure Vpp soit deja active/configuree quand measure() la lira
        # pour de vrai. (Auparavant measurement_clear etait desactive et
        # rien n'activait Vpp avant le tout premier point.)
        measurement_clear_cmd = get_command(self.scope_cfg, "measurement_clear")
        if measurement_clear_cmd:
            safe_write(self.scope, f"{measurement_clear_cmd} ALL")
        else:
            debug("WARNING: 'measurement_clear' command not found")

        measurement_source_cmd = get_command(self.scope_cfg, "measurement_source", source="CHANnel1")
        if measurement_source_cmd:
            safe_write(self.scope, measurement_source_cmd)
        else:
            debug("WARNING: 'measurement_source' command not found")

        vpp_activate_cmd = get_command(self.scope_cfg, "measurement_vpp")
        if vpp_activate_cmd:
            safe_query_float(self.scope, vpp_activate_cmd)
            debug("Mesure Vpp activee sur CH1 avant toute acquisition")
        else:
            debug("WARNING: 'measurement_vpp' command not found -- impossible d'activer Vpp en avance")

        debug("Oscilloscope configured: 1MOhm, EXTernal trigger, POSitive slope, VPP-only display "
              "(channel 1 monitors the attenuator output, i.e. the injected signal, NOT the DUT response)")

        # --- GENERATEUR ---
        self.vcc = float(config["tension_vcc"])

        self.trigger_cmd = get_command(self.gen_cfg, "trigger")
        if not self.trigger_cmd:
            self.trigger_cmd = get_command(self.gen_cfg, "burst_trigger_immediate")
        if not self.trigger_cmd:
            debug("WARNING: No trigger command found in config. Manual triggering required.")

        func_cmd = get_command(self.gen_cfg, "function_shape", channel=1)
        if func_cmd:
            safe_write(self.gen, f"{func_cmd} SINusoid")

        burst_cmd = get_command(self.gen_cfg, "burst_state", state="ON")
        if burst_cmd:
            safe_write(self.gen, burst_cmd)
        else:
            debug("WARNING: 'burst_state' command not found")

        burst_mode_cmd = get_command(self.gen_cfg, "burst_mode", mode="TRIGgered")
        if burst_mode_cmd:
            safe_write(self.gen, burst_mode_cmd)
        else:
            debug("WARNING: 'burst_mode' command not found")

        # Cahier des charges (ligne 90) : salve de 11 cycles -- meme
        # convention que linearite_gain.py / lva_measurement.py, meme si
        # l'ISO 22232-1 -9.4.2.1 de base parle d'une salve de 5 cycles.
        # Garde a 11 pour rester coherent avec le reste du projet.
        burst_ncycles_cmd = get_command(self.gen_cfg, "burst_ncycles", ncycles=11)
        if burst_ncycles_cmd:
            safe_write(self.gen, burst_ncycles_cmd)
        else:
            debug("WARNING: 'burst_ncycles' command not found")

        burst_trigger_cmd = get_command(self.gen_cfg, "burst_trigger_source", source="EXTernal")
        if burst_trigger_cmd:
            safe_write(self.gen, burst_trigger_cmd)
        else:
            debug("WARNING: 'burst_trigger_source' command not found")

        burst_trigger_slope_cmd = get_command(self.gen_cfg, "output_trigger_slope", channel=1)
        if burst_trigger_slope_cmd:
            safe_write(self.gen, f"{burst_trigger_slope_cmd} POSitive")
        else:
            debug("WARNING: 'output_trigger_slope' command not found in config")

        # Amplitude fixee une fois pour toutes ici (1 V par defaut, cahier
        # des charges ligne 89) -- measure() ne touche plus qu'a la
        # frequence, jamais a la forme/amplitude/burst, pour ne pas
        # risquer de reinitialiser la config burst comme le faisait
        # APPLy:SINusoid dans la version precedente.
        amp_cmd = get_command(self.gen_cfg, "voltage_amplitude", channel=1)
        if amp_cmd:
            safe_write(self.gen, f"{amp_cmd} {self.vcc}")
        else:
            debug("WARNING: 'voltage_amplitude' command not found")

        out_cmd = get_command(self.gen_cfg, "output_state", state="ON")
        if out_cmd:
            safe_write(self.gen, out_cmd)

        debug(f"Generator configured: {self.vcc:.3f} Vcc, BURST 11 cycles, EXTernal trigger")

        time.sleep(0.5)
        self.is_initialized = True
        self.points = []
        debug("=== INITIALIZE SUCCESS ===")

    def trigger_burst(self):
        if not self.trigger_cmd:
            debug("No trigger command available - waiting for EXTernal trigger")
            return False
        try:
            safe_write(self.gen, self.trigger_cmd)
            return True
        except Exception as e:
            debug(f"Failed to trigger burst: {e}")
            return False

    def prompt_setup_gain(self):
        """
        Cahier des charges (ligne 96) : le gain de l'appareil doit etre
        regle manuellement sur une valeur moyenne (20 dB) avant de lancer
        la boucle de frequences. Envoye une seule fois, avant le premier
        point.
        """
        print(json.dumps({
            "type": MSG_SETUP_GAIN,
            "gain_db": self.gain_db,
            "message":
                       f"1. Reglez le gain de l'appareil U.T sur une valeur de : {self.gain_db:.0f} dB.\n"
                       f"2. Cliquez sur OK pour demarrer le balayage en frequence."
        }), flush=True)

        while True:
            data = read_stdin_json()
            action = data.get("action")
            if action == "confirm":
                return
            if action == "cancel":
                raise Exception("Measurement cancelled by user")

    def ask_operator_ready_to_acquire(self, frequency_mhz):
        """
        Bloque AVANT toute acquisition : l'operateur regarde l'oscillo,
        ajuste ce qu'il faut sur l'appareil U.T, puis clique OK. Rien
        n'est mesure tant que ce clic n'est pas recu.
        """
        print(json.dumps({
            "type": MSG_CONFIRM_FREQUENCY,
            "frequency_mhz": frequency_mhz,
            "message": f"Frequence reglee sur {frequency_mhz:.2f} MHz.\n"
                       f"Verifiez/ajustez le signal sur l'oscilloscope et sur l'appareil U.T.\n\n"
                       f"Cliquez sur OK pour lancer l'acquisition."
        }), flush=True)

        while True:
            data = read_stdin_json()
            action = data.get("action")
            if action == "confirm":
                return
            if action == "cancel":
                raise Exception("Measurement cancelled by user")

    def ask_operator_for_screen_read(self, frequency_mhz, tension_mesuree):
        print(json.dumps({
            "type": MSG_ENTER_SCREEN_READ,
            "frequency_mhz": frequency_mhz,
            "tension_mesuree": tension_mesuree,
            "message": f"Frequence : {frequency_mhz:.2f} MHz\n"
                       f"Tension mesuree (CH1, attenuateur) : {tension_mesuree:.4f} V\n\n"
                       f"Veuillez entrer le % ecran lu sur l'ecran de l'appareil :"
        }), flush=True)

        while True:
            data = read_stdin_json()
            action = data.get("action")
            if action == "screen_read":
                value = data.get("value")
                if value is not None:
                    return float(value)
            elif action == "cancel":
                raise Exception("Measurement cancelled by user")

    def measure(self, frequency_mhz, request_id=None):
        if not self.is_initialized:
            raise Exception("Measurement not initialized. Call initialize() first.")

        freq_hz = float(frequency_mhz) * 1e6

        # 1. Ne retouche QUE la frequence -- ne touche plus a
        # forme/amplitude/burst (voir commentaire dans initialize()).
        freq_cmd = get_command(self.gen_cfg, "frequency_fixed", channel=1)
        if freq_cmd:
            safe_write(self.gen, f"{freq_cmd} {freq_hz}")
        else:
            debug("WARNING: 'frequency_fixed' command not found")

        time.sleep(0.05)

        # 2. L'operateur a la main : il regarde l'oscillo, ajuste ce qu'il
        # faut, puis clique OK.
        self.ask_operator_ready_to_acquire(frequency_mhz)

        # 3. Lecture directe du Vpp -- plus d'armement SINGLE, plus de
        # declenchement logiciel du burst, plus d'attente de statut
        # d'acquisition (retires a la demande : trop de points de
        # blocage/erreur pour ce que ca apportait). Le generateur est
        # deja configure en burst permanent (voir initialize()) ; au
        # moment ou l'operateur clique OK, le signal est deja stable a
        # l'ecran -- on lit directement. La mesure Vpp elle-meme a deja
        # ete activee/amorcee une fois pour toutes dans initialize(),
        # donc ce query() ne fait que la relire, pas la (re)configurer.
        src_cmd = get_command(self.scope_cfg, "measurement_source", source="CHANnel1")
        if src_cmd:
            safe_write(self.scope, src_cmd)

        vpp_cmd = get_command(self.scope_cfg, "measurement_vpp")
        v_meas = safe_query_float(self.scope, vpp_cmd)

        if v_meas is None:
            raise Exception(f"Impossible de mesurer Vpp a {frequency_mhz} MHz")

        if self.v_ref is None:
            # Premier point mesure = frequence de reference (envoyee en
            # tete par l'IHM). C'est LA tension de calibration : tout le
            # reste de "% ecran attendu" est exprime par rapport a elle.
            self.v_ref = v_meas
            debug(f"Frequence de reference (calibration 80% FSH) : {frequency_mhz} MHz, V_ref={v_meas:.4f} V")

        # 4. Etape manuelle obligatoire : la reponse reelle de l'appareil
        # (ce que son ecran affiche) n'existe nulle part ailleurs que sur
        # son propre ecran -- le CH1 de l'oscillo ne voit que le signal
        # injecte (voir docstring de la classe).
        screen_read = self.ask_operator_for_screen_read(frequency_mhz, v_meas)

        self.points.append((float(frequency_mhz), v_meas, screen_read))

        result = {
            "success": True,
            "frequency_mhz": frequency_mhz,
            "tension_mesuree": v_meas,
            "screen_read_pct": screen_read,
        }
        if request_id is not None:
            result["request_id"] = request_id

        return result

    def analyze(self):
        """
        Fmax = frequence ou "% ecran lu" est maximal.
        Ecart dB(f) = 20*log10( lu(f) / 80 ).
        Fl/Fu = croisement a -3 dB de la courbe d'ecart, de part et
        d'autre de Fmax (interpolation lineaire en frequence).
        Fo = (Fu+Fl)/2, Df = Fu-Fl -- Formules (12)/(13), -9.4.2.
        Comparaison a la fiche constructeur : tolerance + ou -10 % (-9.4.2.2).
        """
        pts = sorted(self.points, key=lambda p: p[0])

        if len(pts) < 3:
            return {
                "success": True,
                "points": [self._point_dict(f, v, lu, None, None) for f, v, lu in pts],
                "error_analysis": "Pas assez de points pour calculer la bande passante (3 minimum).",
            }

        imax = max(range(len(pts)), key=lambda i: pts[i][2])
        fmax = pts[imax][0]

        if self.v_ref is None or self.v_ref <= 0:
            return {
                "success": True,
                "points": [self._point_dict(f, v, lu, None, None) for f, v, lu in pts],
                "error_analysis": "Tension de reference (frequence du milieu) manquante ou nulle.",
            }

        ecarts = []
        for f, v, lu in pts:
            ecart_db = (20 * math.log10(lu / self.REFERENCE_PCT)) if lu > 0 else None
            ecarts.append(ecart_db)

        fl = self._interpolate_crossing(pts, ecarts, imax, -3.0, direction=-1)
        fu = self._interpolate_crossing(pts, ecarts, imax, -3.0, direction=1)

        f0 = None
        delta_f = None
        if fl is not None and fu is not None:
            f0 = (fu + fl) / 2.0
            delta_f = fu - fl

        points_out = []
        for (f, v, lu), ecart_db in zip(pts, ecarts):
            # "% ecran attendu" : colonne d'affichage/QC seulement (compare
            # ce qu'on attendrait si SEULE la derive d'amplitude injectee
            # expliquait la lecture ecran) -- ne sert pas au calcul de
            # Fl/Fu, qui utilise la constante 80% (voir docstring).
            attendu = ((v / self.v_ref) * (self.REFERENCE_PCT)) if self.v_ref > 0 else None
            points_out.append(self._point_dict(f, v, lu, attendu, ecart_db))

        erreur_fo_pct = None
        erreur_df_pct = None
        conforme = None
        if f0 is not None and self.fo_constructeur:
            erreur_fo_pct = ((f0 - self.fo_constructeur) / self.fo_constructeur)*100 
        if delta_f is not None and self.df_constructeur:
            erreur_df_pct = ((delta_f - self.df_constructeur) / self.df_constructeur)*100 
        if erreur_fo_pct is not None and erreur_df_pct is not None:
            conforme = abs(erreur_fo_pct) <= 10.0 and abs(erreur_df_pct) <= 10.0

        out = {
            "success": True,
            "points": points_out,
            "fmax_mhz": fmax,
            "fl_mhz": round(fl, 3) if fl is not None else None,
            "fu_mhz": round(fu, 3) if fu is not None else None,
            "f0_mhz": round(f0, 3) if f0 is not None else None,
            "delta_f_mhz": round(delta_f, 3) if delta_f is not None else None,
            "fo_constructeur_mhz": self.fo_constructeur,
            "df_constructeur_mhz": self.df_constructeur,
            "erreur_fo_pct": round(erreur_fo_pct, 2) if erreur_fo_pct is not None else None,
            "erreur_df_pct": round(erreur_df_pct, 2) if erreur_df_pct is not None else None,
            "conforme": conforme,
        }
        if fl is None or fu is None:
            out["error_analysis"] = ("Le croisement a -3 dB n'a pas ete trouve d'un cote ou de l'autre de Fmax "
                                      "-- elargissez la plage de frequences testee pour ce filtre.")
        return out

    @staticmethod
    def _point_dict(f, v, lu, attendu, ecart_db):
        return {
            "frequency_mhz": f,
            "tension_v": v,
            "screen_read_pct": lu,
            "pct_attendu": round(attendu, 1) if attendu is not None else None,
            "ecart_db": round(ecart_db, 2) if ecart_db is not None else None,
        }

    @staticmethod
    def _interpolate_crossing(pts, ecarts, imax, threshold_db, direction):
        """
        Avance depuis Fmax dans la direction donnee jusqu'au premier point
        sous le seuil -3 dB, puis interpole lineairement (en frequence,
        sur la courbe d'ecart en dB) entre ce point et le precedent.
        """
        i = imax
        n = len(pts)
        while True:
            j = i + direction
            if j < 0 or j >= n:
                return None
            e_prev = ecarts[i]
            e_next = ecarts[j]
            f_prev = pts[i][0]
            f_next = pts[j][0]
            if e_prev is None or e_next is None:
                i = j
                continue
            if e_next < threshold_db:
                if e_prev == e_next:
                    return f_next
                ratio = (threshold_db - e_prev) / (e_next - e_prev)
                return f_prev + ratio * (f_next - f_prev)
            i = j

    def close(self):
        try:
            if self.gen and self.gen_cfg:
                out_cmd = get_command(self.gen_cfg, "output_state", state="OFF")
                if out_cmd:
                    safe_write(self.gen, out_cmd)
        except Exception:
            pass
        try:
            if self.scope:
                self.scope.close()
        except Exception:
            pass
        try:
            if self.gen:
                self.gen.close()
        except Exception:
            pass
        try:
            if self.rm:
                self.rm.close()
        except Exception:
            pass
        self.is_initialized = False
        self.points = []
        self.v_ref = None


def main():
    measurement = FrequencyResponseMeasurement()
    gain_confirmed = False

    try:
        line = sys.stdin.readline()
        if not line:
            send_response({"success": False, "error": "No configuration received."})
            return

        try:
            config = json.loads(line.strip())
        except json.JSONDecodeError as e:
            send_response({"success": False, "error": f"Invalid JSON config: {str(e)}"})
            return

        measurement.initialize(config)
        send_response({"status": "ready"})

        while True:
            line = sys.stdin.readline()
            if not line:
                break

            try:
                cmd = json.loads(line.strip())
            except json.JSONDecodeError as e:
                send_response({"success": False, "error": f"Invalid JSON command: {str(e)}"})
                continue

            command = cmd.get("command")
            request_id = cmd.get("request_id")

            if command == "measure":
                if not gain_confirmed:
                    measurement.prompt_setup_gain()
                    gain_confirmed = True

                freq = cmd.get("frequency_mhz")
                if freq is None:
                    send_response({"success": False, "error": "Missing 'frequency_mhz' in measure command", "request_id": request_id})
                    continue
                try:
                    result = measurement.measure(freq, request_id)
                    send_response(result)
                except Exception as e:
                    send_response({"success": False, "error": str(e), "request_id": request_id})

            elif command == "complete":
                try:
                    analysis = measurement.analyze()
                    analysis["request_id"] = request_id
                    send_response(analysis)
                except Exception as e:
                    send_response({"success": False, "error": str(e), "request_id": request_id})
                break

            elif command == "cancel":
                send_response({"success": False, "error": "Cancelled by user", "request_id": request_id})
                break

            else:
                send_response({"success": False, "error": f"Unknown command: {command}", "request_id": request_id})

    except Exception as ex:
        debug("========== PYTHON FATAL ERROR ==========")
        debug(repr(ex))
        import traceback
        traceback.print_exc(file=sys.stderr)
        try:
            send_response({"success": False, "error": str(ex)})
        except Exception:
            pass

    finally:
        measurement.close()


if __name__ == "__main__":
    main()