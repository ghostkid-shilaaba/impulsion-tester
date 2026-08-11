Imports System.Drawing
Imports System.Windows.Forms
Imports System.Text.Json
Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Globalization
Imports System.Data
Imports System.Linq

Public Class UcRFA

    Public Event SuivantClicked As EventHandler
    Public Event PrecedentClicked As EventHandler

    ' Doit correspondre aux constantes MSG_* de reponse_frequence.py
    Private Const MSG_SETUP_GAIN As String = "setup_gain"
    Private Const MSG_CONFIRM_FREQUENCY As String = "confirm_frequency"
    Private Const MSG_ENTER_SCREEN_READ As String = "enter_screen_read"

    ' Gain fixe demandé par le cahier des charges (ligne 96) pour ce test.
    ' Chargé depuis ModelesAppareils.gain (même colonne/valeur que celle
    ' utilisée par UcLVA et UcLDG pour cet appareil) plutôt que codé en dur
    ' -- la valeur doit rester celle enregistrée pour ce modèle, pas une
    ' constante indépendante du reste du programme.
    Private _gainDb As Double = 0.0

    Private isScopeConnected As Boolean = False
    Private isGenConnected As Boolean = False
    Private scopeResource As String = Nothing
    Private generatorResource As String = Nothing
    Private scopeIdn As String = Nothing
    Private generatorIdn As String = Nothing
    Private scopeConfig As JsonElement = Nothing
    Private generatorConfig As JsonElement = Nothing

    Private _pythonProcess As Process = Nothing
    Private _cancellationTokenSource As CancellationTokenSource = Nothing
    Private _streamWriter As StreamWriter = Nothing
    Private _streamReader As StreamReader = Nothing
    Private _requestId As Integer = 0
    Private ReadOnly _errorLock As New Object()
    Private _pythonStderr As New System.Text.StringBuilder()
    Private _resultsForm As RfaResultsForm = Nothing

    Private Sub SafeInvoke(action As Action)
        If Me.IsHandleCreated AndAlso Not Me.IsDisposed AndAlso Me.InvokeRequired Then
            Me.Invoke(action)
        ElseIf Me.IsHandleCreated AndAlso Not Me.IsDisposed Then
            action()
        End If
    End Sub

    Public Sub New()
        InitializeComponent()
        AddHandler Me.Disposed, AddressOf UcRFA_Disposed
    End Sub

    Private Sub UcRFA_Disposed(sender As Object, e As EventArgs)
        Dim proc = _pythonProcess
        Try
            If proc IsNot Nothing AndAlso Not proc.HasExited Then
                proc.Kill(True)
                proc.WaitForExit(1000)
            End If
        Catch ex As Exception
            Debug.WriteLine($"Dispose error: {ex.Message}")
        End Try
    End Sub

    Private Async Sub UcRFA_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If String.IsNullOrEmpty(txtTension.Text) Then txtTension.Text = "1.00"
        _gainDb = LoadGainFromDatabase(UcLVA.ModeleId)
        UpdateTitle(_gainDb)
        ChargerFiltres()
        Await VerifyConnectionsAsync()
    End Sub

    ' Identique à UcLVA.LoadGainFromDatabase : le gain est saisi une seule
    ' fois par modèle (écran "Gestion des appareils") et réutilisé par
    ' tous les tests qui en ont besoin (LVA, LDG, et maintenant RFA), pour
    ' qu'un seul et même réglage physique reste valable partout.
    Private Function LoadGainFromDatabase(modeleId As Integer) As Double
        Try
            Using conn As New Data.SQLite.SQLiteConnection(DatabaseHelper.connectionString)
                conn.Open()

                Dim query As String = "SELECT gain FROM ModelesAppareils WHERE modele_id = @modeleId"

                Using cmd As New Data.SQLite.SQLiteCommand(query, conn)
                    cmd.Parameters.AddWithValue("@modeleId", modeleId)

                    Dim result = cmd.ExecuteScalar()

                    If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                        Return Convert.ToDouble(result)
                    End If
                End Using
            End Using
        Catch ex As Exception
            Debug.WriteLine($"Failed to load gain: {ex.Message}")
        End Try

        Return 0
    End Function

    Private Sub UpdateTitle(gain As Double)
        Label2.Text = If(gain > 0,
            $"REPONSE EN FREQUENCE DE L'AMPLIFICATEUR ({gain:F1} dB)",
            "REPONSE EN FREQUENCE DE L'AMPLIFICATEUR")
    End Sub

    ' ---- Chargement du filtre ----

    Private Sub ChargerFiltres()
        cmbFiltre.DataSource = Nothing

        Dim modeleId As Integer = UcLVA.ModeleId
        If modeleId <= 0 Then Return

        Dim dt As DataTable = DatabaseHelper.GetFiltresForModele(modeleId)
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return

        cmbFiltre.DisplayMember = "nom_filtre"
        cmbFiltre.ValueMember = "filtre_id"
        cmbFiltre.DataSource = dt
        cmbFiltre.SelectedIndex = 0
    End Sub

    Private Function GetFrequencesFiltreSelectionne() As List(Of Double)
        Dim freqs As New List(Of Double)
        Dim row As DataRowView = TryCast(cmbFiltre.SelectedItem, DataRowView)
        If row Is Nothing Then Return freqs

        Dim raw As String = If(row("frequences") Is DBNull.Value, "", row("frequences").ToString())
        ' Accepte les listes séparées par retour à la ligne, virgule, point-virgule
        ' ou tabulation -- le point-virgule est nécessaire car c'est le séparateur
        ' de liste habituel en France (Excel, saisie clavier FR), y compris quand
        ' la virgule sert de séparateur décimal (ex: "0,3;0,6;1.0").
        Dim lines = raw.Split({vbCrLf, vbLf, vbCr, ";"c, vbTab}, StringSplitOptions.RemoveEmptyEntries)

        For Each line In lines
            Dim v As Double
            If Double.TryParse(line.Trim().Replace(","c, "."c), NumberStyles.Float, CultureInfo.InvariantCulture, v) Then
                freqs.Add(v)
            End If
        Next

        Return freqs.Distinct().OrderBy(Function(f) f).ToList()
    End Function

    ''' <summary>
    ''' Réordonne la liste (déjà triée par ordre croissant) pour mesurer la
    ''' fréquence du milieu en premier -- c'est là que le gain/l'atténuateur
    ''' ont été réglés pour amener la lecture écran vers 80 % FSH
    ''' (ISO 22232-1 §9.4.2.1), donc c'est ELLE qui sert de référence pour
    ''' toutes les autres. Pour 13 fréquences : 7e, puis 1re..6e, puis
    ''' 8e..13e. Le tableau de résultats reste néanmoins affiché par ordre
    ''' croissant de fréquence (voir RfaResultsForm) -- seul l'ORDRE DE
    ''' MESURE change, pas l'ordre d'affichage.
    ''' </summary>
    Private Function GetOrdreAcquisition(freqsTriees As List(Of Double)) As List(Of Double)
        If freqsTriees.Count = 0 Then Return New List(Of Double)

        ' Floor(N/2) donne le milieu exact pour N impair, et le "milieu
        ' haut" (7e sur 12/13, pas le 6e) pour N pair -- Ceiling(N/2)-1
        ' retombait sur le 6e par erreur pour N=12.
        Dim refIndex As Integer = Math.Max(0, CInt(Math.Floor(freqsTriees.Count / 2.0)))

        Dim ordre As New List(Of Double)
        ordre.Add(freqsTriees(refIndex))
        For i As Integer = 0 To freqsTriees.Count - 1
            If i <> refIndex Then ordre.Add(freqsTriees(i))
        Next
        Return ordre
    End Function

    ' ---- Détection matérielle : même logique que UcLDG/UcLVA ----

    Public Async Function VerifyConnectionsAsync() As Task
        scopeResource = Nothing
        generatorResource = Nothing
        scopeIdn = Nothing
        generatorIdn = Nothing
        scopeConfig = Nothing
        generatorConfig = Nothing

        ' Tant que la détection tourne, on désactive explicitement le bouton :
        ' sans ça, un clic pendant le scan (UI gelée -> fenêtre "fantôme"
        ' Windows) est tout simplement perdu et rien n'est envoyé.
        btnAcquerir.Enabled = False

        If VisaCacheHelper.HasValidCache() Then
            Dim cacheData = VisaCacheHelper.LoadFromCache()
            If cacheData.ValueKind <> JsonValueKind.Undefined Then
                Dim parsed As Boolean = ParseDetectionResults(cacheData)
                If parsed AndAlso isScopeConnected AndAlso isGenConnected Then
                    UpdateUIAfterDetection()
                    Return
                End If
            End If
        End If

        ' RunPythonDetection() lance Process.Start + WaitForExit(30000) de
        ' façon bloquante : on le sort donc du thread UI avec Task.Run pour
        ' que la fenêtre reste réactive pendant tout le scan matériel.
        Await Task.Run(Sub() RunPythonDetection())
    End Function

    Private Sub RunPythonDetection()
        Try
            Dim scriptPath As String = FindPythonScript("visa_checker.py")
            If String.IsNullOrEmpty(scriptPath) Then
                SafeInvoke(Sub()
                               MessageBox.Show("Impossible de trouver visa_checker.py.",
                                               "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                           End Sub)
                Return
            End If

            Dim psi As New ProcessStartInfo()
            psi.FileName = "python.exe"
            psi.Arguments = $"""{scriptPath}"""
            psi.RedirectStandardOutput = True
            psi.RedirectStandardError = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True

            Using p As Process = Process.Start(psi)
                Dim outputBuilder As New System.Text.StringBuilder()
                Dim errorBuilder As New System.Text.StringBuilder()

                AddHandler p.OutputDataReceived, Sub(sender, e)
                                                     If e.Data IsNot Nothing Then
                                                         outputBuilder.AppendLine(e.Data)
                                                     End If
                                                 End Sub
                AddHandler p.ErrorDataReceived, Sub(sender, e)
                                                    If e.Data IsNot Nothing Then
                                                        SyncLock _errorLock
                                                            errorBuilder.AppendLine(e.Data)
                                                        End SyncLock
                                                    End If
                                                End Sub
                p.BeginOutputReadLine()
                p.BeginErrorReadLine()

                If Not p.WaitForExit(30000) Then
                    Try
                        p.Kill(True)
                        p.WaitForExit()
                    Catch ex As Exception
                        Debug.WriteLine($"Kill error: {ex.Message}")
                    End Try
                    Throw New Exception("Le processus Python a expiré (30 secondes).")
                End If

                Dim output As String = outputBuilder.ToString()
                Dim errOutput As String = ""
                SyncLock _errorLock
                    errOutput = errorBuilder.ToString()
                End SyncLock

                If p.ExitCode <> 0 AndAlso Not String.IsNullOrEmpty(errOutput) Then
                    SafeInvoke(Sub()
                                   MessageBox.Show($"Erreur Python (code {p.ExitCode}) : {errOutput}",
                                                   "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                               End Sub)
                    Return
                End If

                If Not String.IsNullOrEmpty(output) Then
                    Using doc As JsonDocument = JsonDocument.Parse(output)
                        ParseDetectionResults(doc.RootElement)
                    End Using
                Else
                    SafeInvoke(Sub()
                                   MessageBox.Show("Aucune réponse du processus Python.",
                                                   "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                               End Sub)
                End If
            End Using
        Catch ex As Exception
            scopeResource = Nothing
            generatorResource = Nothing
            scopeIdn = Nothing
            generatorIdn = Nothing
            scopeConfig = Nothing
            generatorConfig = Nothing
            SafeInvoke(Sub()
                           MessageBox.Show($"Erreur lors de la détection : {ex.Message}",
                                           "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                       End Sub)
        End Try

        UpdateUIAfterDetection()
    End Sub

    Private Function ParseDetectionResults(root As JsonElement) As Boolean
        Try
            Dim scopeVal As JsonElement
            If root.TryGetProperty("oscilloscope", scopeVal) AndAlso scopeVal.ValueKind = JsonValueKind.Array Then
                If scopeVal.GetArrayLength() > 0 Then
                    Dim firstScope = scopeVal(0)
                    Dim idnEl As JsonElement
                    Dim resEl As JsonElement
                    Dim configEl As JsonElement

                    If firstScope.TryGetProperty("idn", idnEl) AndAlso idnEl.ValueKind = JsonValueKind.String Then
                        scopeIdn = idnEl.GetString()
                    End If
                    If firstScope.TryGetProperty("resource", resEl) AndAlso resEl.ValueKind = JsonValueKind.String Then
                        scopeResource = resEl.GetString()
                    End If
                    If firstScope.TryGetProperty("config", configEl) AndAlso configEl.ValueKind = JsonValueKind.Object Then
                        scopeConfig = configEl.Clone()
                    End If
                End If
            End If

            Dim genVal As JsonElement
            If root.TryGetProperty("generator", genVal) AndAlso genVal.ValueKind = JsonValueKind.Array Then
                If genVal.GetArrayLength() > 0 Then
                    Dim firstGen = genVal(0)
                    Dim idnEl As JsonElement
                    Dim resEl As JsonElement
                    Dim configEl As JsonElement

                    If firstGen.TryGetProperty("idn", idnEl) AndAlso idnEl.ValueKind = JsonValueKind.String Then
                        generatorIdn = idnEl.GetString()
                    End If
                    If firstGen.TryGetProperty("resource", resEl) AndAlso resEl.ValueKind = JsonValueKind.String Then
                        generatorResource = resEl.GetString()
                    End If
                    If firstGen.TryGetProperty("config", configEl) AndAlso configEl.ValueKind = JsonValueKind.Object Then
                        generatorConfig = configEl.Clone()
                    End If
                End If
            End If

            isScopeConnected = Not String.IsNullOrEmpty(scopeIdn) AndAlso Not String.IsNullOrEmpty(scopeResource)
            isGenConnected = Not String.IsNullOrEmpty(generatorIdn) AndAlso Not String.IsNullOrEmpty(generatorResource)

            Return True
        Catch ex As Exception
            Debug.WriteLine($"ParseDetectionResults error: {ex.Message}")
            Return False
        End Try
    End Function

    Private Sub UpdateUIAfterDetection()
        SafeInvoke(Sub()
                       If Not isScopeConnected OrElse Not isGenConnected Then
                           Dim missing As String = ""
                           If Not isScopeConnected Then missing &= "• Oscilloscope" & vbCrLf
                           If Not isGenConnected Then missing &= "• Générateur de fonctions" & vbCrLf

                           MessageBox.Show("Erreur de connexion hardware !" & vbCrLf & vbCrLf &
                                           "Équipement(s) non détecté(s) :" & vbCrLf & missing & vbCrLf &
                                           "Veuillez vérifier vos câbles USB/LAN et re-scanner.",
                                           "Connexion Hardware", MessageBoxButtons.OK, MessageBoxIcon.Error)
                       End If

                       UpdateContextMenu()
                       btnAcquerir.Enabled = isScopeConnected AndAlso isGenConnected
                   End Sub)
    End Sub

    ' Identique à UcLVA.UpdateContextMenu : construit le menu contextuel
    ' "..." affichant les appareils détectés (*IDN?) et permettant de
    ' forcer un nouveau scan matériel (vide le cache VISA puis relance
    ' VerifyConnections()).
    Private Sub UpdateContextMenu()
        cmsAppareils.Items.Clear()
        cmsAppareils.Items.Add(New ToolStripMenuItem("Équipements Détectés (*IDN?) :") With {.Enabled = False})
        cmsAppareils.Items.Add(New ToolStripSeparator())
        cmsAppareils.Items.Add(New ToolStripMenuItem(If(isScopeConnected, $"✔ Scope: {scopeIdn}", "❌ Scope: Non connecté")) With {.Enabled = False})
        cmsAppareils.Items.Add(New ToolStripMenuItem(If(isGenConnected, $"✔ Générateur: {generatorIdn}", "❌ Générateur: Non connecté")) With {.Enabled = False})
        cmsAppareils.Items.Add(New ToolStripSeparator())
        Dim btnRefresh As New ToolStripMenuItem("🔄 Re-scanner les appareils")
        AddHandler btnRefresh.Click,
    Async Sub(s, args)
        VisaCacheHelper.ClearCache()
        Await VerifyConnectionsAsync()
    End Sub
        cmsAppareils.Items.Add(btnRefresh)
    End Sub

    Private Sub btnThreeDots_Click(sender As Object, e As EventArgs) Handles btnThreeDots.Click
        cmsAppareils.Show(btnThreeDots, New Point(0, btnThreeDots.Height))
    End Sub

    Private Function FindPythonScript(scriptName As String) As String
        Dim exeFolder As String = Application.StartupPath
        Dim exePathCheck As String = Path.Combine(exeFolder, scriptName)
        If File.Exists(exePathCheck) Then Return exePathCheck

        Dim projectRoot As String = Nothing
        Try
            Dim exePath As String = Application.StartupPath
            projectRoot = Directory.GetParent(exePath).Parent.Parent.FullName
        Catch
        End Try

        If Not String.IsNullOrEmpty(projectRoot) Then
            Dim possiblePath As String = Path.Combine(projectRoot, "My Project", scriptName)
            If File.Exists(possiblePath) Then Return possiblePath

            possiblePath = Path.Combine(projectRoot, scriptName)
            If File.Exists(possiblePath) Then Return possiblePath
        End If

        Return Nothing
    End Function

    Private Sub BtnPrecedent_Click(sender As Object, e As EventArgs) Handles BtnPrecedent.Click
        RaiseEvent PrecedentClicked(Me, EventArgs.Empty)
    End Sub

    Private Sub BtnSuivant_Click(sender As Object, e As EventArgs) Handles BtnSuivant.Click
        RaiseEvent SuivantClicked(Me, EventArgs.Empty)
    End Sub

    ' ---- Acquisition ----

    Private Async Sub btnAcquerir_Click(sender As Object, e As EventArgs) Handles btnAcquerir.Click
        If String.IsNullOrEmpty(scopeResource) OrElse String.IsNullOrEmpty(generatorResource) Then
            MessageBox.Show("Veuillez re-scanner les appareils. Oscilloscope et générateur requis.",
                            "Connexion Hardware", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        If cmbFiltre.SelectedItem Is Nothing Then
            MessageBox.Show("Veuillez choisir un filtre.", "Saisie incomplète", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim freqList As List(Of Double) = GetFrequencesFiltreSelectionne()
        If freqList.Count < 3 Then
            MessageBox.Show("Ce filtre doit avoir au moins 3 fréquences enregistrées (voir Gestion des appareils).",
                            "Fréquences manquantes", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If String.IsNullOrEmpty(txtTension.Text) Then
            MessageBox.Show("Veuillez saisir la tension Vcc à envoyer.", "Saisie incomplète", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim tension As Double
        If Not Double.TryParse(txtTension.Text.Trim().Replace(","c, "."c), NumberStyles.Float, CultureInfo.InvariantCulture, tension) Then
            MessageBox.Show("La tension Vcc doit être un nombre valide (ex: 1.00 ou 1,00).",
                            "Format invalide", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        ' Données constructeur (facultatives) : servent uniquement au calcul
        ' de conformité ISO 22232-1 §9.4.2.2 (Fo et Df à ±10 % de la fiche
        ' technique). Laissé vide -> le test s'exécute quand même, sans
        ' conclusion de conformité.
        Dim foText As String = InputBox(
            "Fréquence centrale Fo annoncée par le constructeur (MHz) :" & vbCrLf &
            "(laissez vide pour ignorer le calcul de conformité)",
            "Données constructeur", "")
        Dim dfText As String = InputBox(
            "Bande passante Df annoncée par le constructeur (MHz) :" & vbCrLf &
            "(laissez vide pour ignorer le calcul de conformité)",
            "Données constructeur", "")

        Dim foConstructeur As Double? = Nothing
        Dim dfConstructeur As Double? = Nothing
        Dim tmp As Double
        If Double.TryParse(foText.Trim().Replace(","c, "."c), NumberStyles.Float, CultureInfo.InvariantCulture, tmp) Then
            foConstructeur = tmp
        End If
        If Double.TryParse(dfText.Trim().Replace(","c, "."c), NumberStyles.Float, CultureInfo.InvariantCulture, tmp) Then
            dfConstructeur = tmp
        End If

        Dim nomFiltre As String = DirectCast(cmbFiltre.SelectedItem, DataRowView)("nom_filtre").ToString()

        Dim ordreAcquisition As List(Of Double) = GetOrdreAcquisition(freqList)

        cmbFiltre.Enabled = False
        btnAcquerir.Enabled = False
        btnArreter.Enabled = True

        _cancellationTokenSource = New CancellationTokenSource()

        Try
            Await RunAcquisition(_cancellationTokenSource.Token, ordreAcquisition, tension, nomFiltre, foConstructeur, dfConstructeur)
        Catch ex As Exception
            Dim cancelled As Boolean = _cancellationTokenSource IsNot Nothing AndAlso _cancellationTokenSource.IsCancellationRequested
            If Not cancelled Then
                SafeInvoke(Sub()
                               MessageBox.Show($"Erreur lors de l'acquisition : {ex.Message}",
                                               "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                           End Sub)
            End If
        Finally
            ' Le filtre redevient sélectionnable : l'utilisateur peut en
            ' choisir un autre et relancer "Acquérir" directement.
            cmbFiltre.Enabled = True
            btnAcquerir.Enabled = True
            btnArreter.Enabled = False

            ' Si l'acquisition s'est arrêtée en cours de route (annulation
            ' ou erreur), la fenêtre de résultats ne doit pas rester
            ' bloquée sur des "…" en attente pour toujours : on la ferme
            ' proprement avec ce qui a été mesuré jusque-là.
            Dim formToFinalize = _resultsForm
            _resultsForm = Nothing
            If formToFinalize IsNot Nothing Then
                SafeInvoke(Sub() formToFinalize.FinalizePartial("Acquisition interrompue avant la fin du balayage."))
            End If

            If _cancellationTokenSource IsNot Nothing Then
                _cancellationTokenSource.Dispose()
                _cancellationTokenSource = Nothing
            End If
        End Try
    End Sub

    ' Attend une ligne JSON et la retourne parsée, avec gestion du timeout /
    ' annulation, identique au pattern utilisé dans UcLDG/UcLVA.
    Private Async Function ReadJsonLine(token As CancellationToken, timeoutMs As Integer) As Task(Of JsonDocument)
        Dim readTask = _streamReader.ReadLineAsync()
        Dim completed = Await Task.WhenAny(readTask, Task.Delay(timeoutMs, token))

        If token.IsCancellationRequested Then Return Nothing
        If completed IsNot readTask Then Throw New TimeoutException("Python n'a pas répondu à temps.")

        ' Si le pipe se ferme/plante PENDANT readTask (process Python tué,
        ' terminé de façon inattendue, etc.), attendre readTask ici relève
        ' l'exception d'origine (IOException/ObjectDisposedException type
        ' "canal de communication sur le point d'être fermé"). Si une
        ' annulation est EN COURS à ce moment précis (utilisateur ayant
        ' cliqué Arrêter juste avant que le pipe ne se ferme), on traite ça
        ' comme une annulation propre plutôt que de remonter l'exception
        ' brute -- sinon l'utilisateur voit un message technique au lieu
        ' du message d'interruption normal.
        Dim line As String
        Try
            line = Await readTask
        Catch ex As Exception When token.IsCancellationRequested
            Return Nothing
        Catch ex As Exception
            Throw New Exception($"Le canal de communication avec Python a été fermé.{vbCrLf}{vbCrLf}{PythonStderrSnapshot()}", ex)
        End Try
        If String.IsNullOrEmpty(line) Then
            Throw New Exception($"Python a fermé le canal sans envoyer de réponse.{vbCrLf}{vbCrLf}{PythonStderrSnapshot()}")
        End If

        Return JsonDocument.Parse(line)
    End Function

    ' Dernières lignes de stderr accumulées depuis le process Python en
    ' cours (voir debug() dans reponse_frequence.py) -- vide si rien n'a
    ' encore été écrit ou si aucun process n'est en cours.
    Private Function PythonStderrSnapshot() As String
        Dim err As String = ""
        SyncLock _errorLock
            err = _pythonStderr.ToString()
        End SyncLock
        Return If(String.IsNullOrWhiteSpace(err), "(aucune sortie stderr de Python)", $"Sortie Python (stderr) :{vbCrLf}{err}")
    End Function

    Private Async Function RunAcquisition(token As CancellationToken, freqList As List(Of Double), tension As Double,
                                           nomFiltre As String, foConstructeur As Double?, dfConstructeur As Double?) As Task
        Try
            _requestId = 0

            Dim scriptPath As String = FindPythonScript("reponse_frequence.py")
            If String.IsNullOrEmpty(scriptPath) Then
                SafeInvoke(Sub()
                               MessageBox.Show("Impossible de trouver reponse_frequence.py.",
                                               "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                           End Sub)
                Return
            End If

            Dim psi As New ProcessStartInfo()
            psi.FileName = "python.exe"
            psi.Arguments = $"""{scriptPath}"""
            psi.RedirectStandardInput = True
            psi.RedirectStandardOutput = True
            psi.RedirectStandardError = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True

            Using p As Process = Process.Start(psi)
                _pythonProcess = p
                _streamWriter = p.StandardInput
                _streamReader = p.StandardOutput

                SyncLock _errorLock
                    _pythonStderr.Clear()
                End SyncLock
                AddHandler p.ErrorDataReceived, Sub(sender, e)
                                                    If e.Data IsNot Nothing Then
                                                        SyncLock _errorLock
                                                            _pythonStderr.AppendLine(e.Data)
                                                        End SyncLock
                                                    End If
                                                End Sub
                p.BeginErrorReadLine()

                If p.HasExited Then
                    Dim errMsg As String = ""
                    SyncLock _errorLock
                        errMsg = _pythonStderr.ToString()
                    End SyncLock
                    Throw New Exception($"Le processus Python s'est arrêté prématurément : {errMsg}")
                End If

                Dim configPayload As New Dictionary(Of String, Object) From {
                    {"scope_resource", scopeResource},
                    {"scope_idn", scopeIdn},
                    {"generator_resource", generatorResource},
                    {"generator_idn", generatorIdn},
                    {"tension_vcc", tension},
                    {"gain_db", _gainDb}
                }
                If foConstructeur.HasValue Then configPayload.Add("fo_constructeur_mhz", foConstructeur.Value)
                If dfConstructeur.HasValue Then configPayload.Add("df_constructeur_mhz", dfConstructeur.Value)
                If scopeConfig.ValueKind <> JsonValueKind.Undefined Then configPayload.Add("scope_config", scopeConfig)
                If generatorConfig.ValueKind <> JsonValueKind.Undefined Then configPayload.Add("generator_config", generatorConfig)

                Await _streamWriter.WriteLineAsync(JsonSerializer.Serialize(configPayload))
                Await _streamWriter.FlushAsync()

                If p.HasExited Then
                    Dim errMsg As String = ""
                    SyncLock _errorLock
                        errMsg = _pythonStderr.ToString()
                    End SyncLock
                    Throw New Exception($"Le processus Python s'est arrêté après la configuration : {errMsg}")
                End If

                Using readyDoc = Await ReadJsonLine(token, 10000)
                    If readyDoc Is Nothing Then
                        HandleCancellation() : Return
                    End If

                    Dim statusEl As JsonElement
                    Dim isReady As Boolean = readyDoc.RootElement.TryGetProperty("status", statusEl) AndAlso
                                              statusEl.ValueKind = JsonValueKind.String AndAlso
                                              statusEl.GetString() = "ready"

                    If Not isReady Then
                        ' Python n'envoie PAS forcément {"status":"ready"} : si
                        ' initialize() a levé une exception (ex : ressource VISA
                        ' introuvable), il répond {"success": false, "error": "..."}
                        ' à la place. Sans ce contrôle, GetProperty("status") lève
                        ' une exception .NET générique qui masque le vrai message
                        ' Python (ex: VI_ERROR_RSRC_NFOUND) derrière un simple
                        ' "Python n'est pas prêt.".
                        Dim errMsg As String = "Python n'est pas prêt."
                        Dim errEl As JsonElement
                        If readyDoc.RootElement.TryGetProperty("error", errEl) AndAlso errEl.ValueKind = JsonValueKind.String Then
                            errMsg = errEl.GetString()
                        End If
                        Throw New Exception(errMsg)
                    End If
                End Using

                ' Fenêtre de résultats ouverte dès le début, vide, puis
                ' remplie mesure par mesure (comme LDG/LVA).
                SafeInvoke(Sub()
                               _resultsForm = New RfaResultsForm()
                               _resultsForm.Owner = Me.FindForm()
                               _resultsForm.SetEntete(nomFiltre, foConstructeur, dfConstructeur)
                               _resultsForm.Show()
                               _resultsForm.BringToFront()
                           End Sub)

                For Each freq In freqList
                    If token.IsCancellationRequested Then HandleCancellation() : Return

                    If p.HasExited Then
                        Dim errMsg As String = ""
                        SyncLock _errorLock
                            errMsg = _pythonStderr.ToString()
                        End SyncLock
                        Throw New Exception($"Le processus Python s'est arrêté pendant les mesures : {errMsg}")
                    End If

                    Dim measureCmd As New Dictionary(Of String, Object) From {
                        {"command", "measure"},
                        {"frequency_mhz", freq},
                        {"request_id", _requestId}
                    }
                    Dim sentRequestId As Integer = _requestId
                    _requestId += 1
                    Await _streamWriter.WriteLineAsync(JsonSerializer.Serialize(measureCmd))
                    Await _streamWriter.FlushAsync()

                    ' Une seule commande "measure" peut déclencher plusieurs
                    ' échanges côté Python : le réglage du gain (une seule
                    ' fois, au premier point) puis, à CHAQUE point, la
                    ' demande de lecture manuelle du % écran. On boucle donc
                    ' ici tant qu'on reçoit des messages "type"-és, jusqu'au
                    ' vrai résultat de mesure (qui n'a pas de champ "type").
                    Dim gotResult As Boolean = False
                    While Not gotResult
                        If token.IsCancellationRequested Then HandleCancellation() : Return

                        ' 30 minutes, pas 30 secondes : cette boucle attend des
                        ' actions MANUELLES (vérifier/ajuster le signal sur
                        ' l'oscillo, puis lire le % écran sur l'appareil U.T),
                        ' pas une réponse instantanée de l'instrument -- un
                        ' timeout court ici ne fait que planter l'acquisition
                        ' si l'opérateur prend plus de quelques secondes pour
                        ' regarder/ajuster le matériel avant de cliquer OK.
                        Using doc = Await ReadJsonLine(token, 1800000)
                            If doc Is Nothing Then HandleCancellation() : Return

                            Dim typeEl As JsonElement
                            If doc.RootElement.TryGetProperty("type", typeEl) Then
                                Dim msgType As String = typeEl.GetString()
                                Dim msgEl As JsonElement
                                Dim message As String = ""
                                If doc.RootElement.TryGetProperty("message", msgEl) AndAlso msgEl.ValueKind = JsonValueKind.String Then
                                    message = msgEl.GetString()
                                End If

                                Dim responseDict As New Dictionary(Of String, Object)

                                If msgType = MSG_SETUP_GAIN Then
                                    Dim dialogResult As DialogResult = DialogResult.Cancel
                                    SafeInvoke(Sub() dialogResult = MessageBox.Show(message, "Réglage du gain",
                                                                                    MessageBoxButtons.OKCancel, MessageBoxIcon.Information))
                                    responseDict.Add("action", If(dialogResult = DialogResult.OK, "confirm", "cancel"))

                                ElseIf msgType = MSG_CONFIRM_FREQUENCY Then
                                    Dim dialogResultFreq As DialogResult = DialogResult.Cancel
                                    SafeInvoke(Sub() dialogResultFreq = MessageBox.Show(message, "Vérification du signal",
                                                                                        MessageBoxButtons.OKCancel, MessageBoxIcon.Information))
                                    responseDict.Add("action", If(dialogResultFreq = DialogResult.OK, "confirm", "cancel"))

                                ElseIf msgType = MSG_ENTER_SCREEN_READ Then
                                    Dim input As String = ""
                                    SafeInvoke(Sub() input = InputBox(message, "Lecture écran", ""))
                                    Dim parsedInput As Double
                                    If String.IsNullOrWhiteSpace(input) Then
                                        responseDict.Add("action", "cancel")
                                    ElseIf Double.TryParse(input.Trim().Replace(","c, "."c), NumberStyles.Float, CultureInfo.InvariantCulture, parsedInput) Then
                                        responseDict.Add("action", "screen_read")
                                        responseDict.Add("value", parsedInput)
                                    Else
                                        SafeInvoke(Sub() MessageBox.Show("Valeur invalide. Veuillez entrer un nombre.",
                                                                         "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning))
                                        ' Renvoie une nouvelle demande de lecture côté Python en
                                        ' renvoyant simplement rien de valide -- ici on annule
                                        ' plutôt que de bloquer indéfiniment ; l'utilisateur peut
                                        ' relancer l'acquisition.
                                        responseDict.Add("action", "cancel")
                                    End If
                                Else
                                    Throw New Exception($"Message Python inattendu : {msgType}")
                                End If

                                If token.IsCancellationRequested Then HandleCancellation() : Return

                                Await _streamWriter.WriteLineAsync(JsonSerializer.Serialize(responseDict))
                                Await _streamWriter.FlushAsync()

                                If DirectCast(responseDict("action"), String) = "cancel" Then
                                    HandleCancellation()
                                    Return
                                End If
                                ' sinon, on reboucle pour lire le prochain message
                            Else
                                ' Pas de champ "type" -> c'est le résultat final de la mesure.
                                gotResult = True

                                If doc.RootElement.GetProperty("success").GetBoolean() Then
                                    Dim resultClone As JsonElement = doc.RootElement.Clone()
                                    SafeInvoke(Sub() _resultsForm?.AddResult(resultClone))
                                Else
                                    ' Si on est ici parce que l'utilisateur vient de cliquer
                                    ' "Arrêter" (Python répond "success: false" à sa propre
                                    ' annulation interne, sans champ "type"), il ne faut pas
                                    ' afficher "Erreur à X MHz" -- c'est une annulation normale,
                                    ' pas un échec de mesure.
                                    If token.IsCancellationRequested Then
                                        HandleCancellation()
                                        Return
                                    End If

                                    Dim errMsg As String = "Erreur inconnue"
                                    Dim errEl As JsonElement
                                    If doc.RootElement.TryGetProperty("error", errEl) AndAlso errEl.ValueKind = JsonValueKind.String Then
                                        errMsg = errEl.GetString()
                                    End If
                                    SafeInvoke(Sub()
                                                   MessageBox.Show($"Erreur à {freq} MHz : {errMsg}",
                                                                   "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                               End Sub)
                                    ' On arrête complètement le balayage : la courbe ne serait
                                    ' plus exploitable avec un point manquant au milieu.
                                    Return
                                End If
                            End If
                        End Using
                    End While
                Next

                If p.HasExited Then
                    Dim errMsg As String = ""
                    SyncLock _errorLock
                        errMsg = _pythonStderr.ToString()
                    End SyncLock
                    Throw New Exception($"Le processus Python s'est arrêté avant la fin : {errMsg}")
                End If

                Dim completeCmd As New Dictionary(Of String, Object) From {
                    {"command", "complete"},
                    {"request_id", _requestId}
                }
                Dim sentCompleteId As Integer = _requestId
                _requestId += 1
                Await _streamWriter.WriteLineAsync(JsonSerializer.Serialize(completeCmd))
                Await _streamWriter.FlushAsync()

                Using finalDoc = Await ReadJsonLine(token, 15000)
                    If finalDoc Is Nothing Then HandleCancellation() : Return

                    If finalDoc.RootElement.GetProperty("success").GetBoolean() Then
                        Dim summary As JsonElement = finalDoc.RootElement.Clone()
                        SafeInvoke(Sub()
                                       _resultsForm?.FinalizeResults(summary)
                                       _resultsForm = Nothing
                                   End Sub)
                    Else
                        ' Même garde-fou qu'au-dessus : une annulation en
                        ' cours ne doit jamais s'afficher comme une erreur.
                        If token.IsCancellationRequested Then
                            HandleCancellation()
                            Return
                        End If

                        Dim errMsg As String = "Erreur inconnue"
                        Dim errEl As JsonElement
                        If finalDoc.RootElement.TryGetProperty("error", errEl) AndAlso errEl.ValueKind = JsonValueKind.String Then
                            errMsg = errEl.GetString()
                        End If
                        SafeInvoke(Sub()
                                       MessageBox.Show($"Erreur : {errMsg}",
                                                       "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                   End Sub)
                    End If
                End Using

                Try
                    If Not p.WaitForExit(5000) Then
                        p.Kill(True)
                        p.WaitForExit()
                    End If
                Catch ex As Exception
                    Debug.WriteLine($"Process cleanup error: {ex.Message}")
                End Try

            End Using

        Catch ex As IOException
            If token.IsCancellationRequested Then Return
            Throw
        Catch ex As ObjectDisposedException
            If token.IsCancellationRequested Then Return
            Throw
        Catch ex As TimeoutException
            Throw
        Catch ex As Exception
            Throw
        Finally
            _pythonProcess = Nothing
            _streamWriter = Nothing
            _streamReader = Nothing
        End Try
    End Function

    Private Sub HandleCancellation()
        SafeInvoke(Sub()
                       MessageBox.Show("Acquisition interrompue.", "Interruption", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                   End Sub)
    End Sub

    Private Sub btnArreter_Click(sender As Object, e As EventArgs) Handles btnArreter.Click
        btnArreter.Enabled = False

        If _cancellationTokenSource IsNot Nothing Then
            _cancellationTokenSource.Cancel()
        End If

        Dim proc = _pythonProcess
        Dim writer = _streamWriter

        Try
            If proc IsNot Nothing AndAlso Not proc.HasExited AndAlso writer IsNot Nothing Then
                Try
                    ' "action" débloque un prompt imbriqué (setup_gain /
                    ' enter_screen_read) ; "command" débloque la boucle
                    ' principale si Python attend un "measure"/"complete".
                    ' Les deux sont inoffensifs à envoyer ensemble.
                    Dim cancelCmd As New Dictionary(Of String, Object) From {
                        {"action", "cancel"},
                        {"command", "cancel"},
                        {"request_id", -1}
                    }
                    writer.WriteLine(JsonSerializer.Serialize(cancelCmd))
                    writer.Flush()
                Catch
                    ' Pipe peut-être déjà fermé -> on tombe sur Kill() ci-dessous.
                End Try

                If Not proc.WaitForExit(2000) Then
                    Try
                        proc.Kill(True)
                        proc.WaitForExit(1000)
                    Catch ex As Exception
                        Debug.WriteLine($"Stop error: {ex.Message}")
                    End Try
                End If
            ElseIf proc IsNot Nothing AndAlso Not proc.HasExited Then
                proc.Kill(True)
                proc.WaitForExit(1000)
            End If
        Catch ex As Exception
            Debug.WriteLine($"Stop error: {ex.Message}")
        End Try
    End Sub

End Class