Imports System.Text.Json
Imports System.Windows.Forms
Imports System.Globalization

Public Class RfaResultsForm

    Private _nomFiltre As String = ""
    Private _finalized As Boolean = False

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub RfaResultsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Réponse en fréquence de l'amplificateur - Résultats"
        SetupTable()
    End Sub

    Public Sub SetEntete(nomFiltre As String, foConstructeur As Double?, dfConstructeur As Double?)
        _nomFiltre = nomFiltre
        Me.Text = $"Réponse en fréquence - Filtre {nomFiltre}"
        lblResume.Text = $"Filtre actif : {nomFiltre}" & vbCrLf & "Acquisition en cours..."
        lblConclusion.Text = ""
    End Sub

    Private Sub SetupTable()
        dataGridViewRFA.Rows.Clear()
        dataGridViewRFA.Columns.Clear()

        dataGridViewRFA.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        dataGridViewRFA.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 120, 215)
        dataGridViewRFA.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        dataGridViewRFA.DefaultCellStyle.Font = New Font("Segoe UI", 9)

        dataGridViewRFA.Columns.Add("colFreq", "Fréquence (MHz)")
        dataGridViewRFA.Columns.Add("colTension", "Tension mesurée (V)")
        dataGridViewRFA.Columns.Add("colLu", "% écran lu")
        dataGridViewRFA.Columns.Add("colAttendu", "% écran attendu")
        dataGridViewRFA.Columns.Add("colEcart", "Ecart (dB)")

        dataGridViewRFA.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dataGridViewRFA.AllowUserToAddRows = False
        dataGridViewRFA.ReadOnly = True
    End Sub

    Private Function GetNullableDouble(el As JsonElement, prop As String) As Double?
        Dim v As JsonElement
        If el.TryGetProperty(prop, v) Then
            If v.ValueKind = JsonValueKind.Number Then
                Return v.GetDouble()
            End If
        End If
        Return Nothing
    End Function

    Private Function FormatCell(value As Double?, Optional fmt As String = "F2") As String
        If value.HasValue Then Return value.Value.ToString(fmt)
        Return "—"
    End Function

    ''' <summary>
    ''' Ajoute une ligne brute (fréquence + tension + % écran lu) au fur et
    ''' à mesure des mesures. "% écran attendu" et "Ecart (dB)" restent
    ''' vides ici : ils ne peuvent être calculés qu'une fois la fréquence
    ''' de référence (Fmax, celle où le % lu est le plus haut) connue sur
    ''' TOUTE la série, c'est-à-dire à la fin (voir FinalizeResults). Doit
    ''' être appelé sur le thread UI (l'appelant passe par SafeInvoke).
    ''' </summary>
    Public Sub AddResult(res As JsonElement)
        Dim success As Boolean = True
        Dim successEl As JsonElement
        If res.TryGetProperty("success", successEl) AndAlso successEl.ValueKind = JsonValueKind.False Then
            success = False
        End If

        If Not success Then
            Dim errMsg As String = "Erreur"
            Dim errEl As JsonElement
            If res.TryGetProperty("error", errEl) Then errMsg = errEl.GetString()
            Dim errRowIndex As Integer = dataGridViewRFA.Rows.Add("", "", "", "", $"❌ {errMsg}")
            dataGridViewRFA.FirstDisplayedScrollingRowIndex = errRowIndex
            Return
        End If

        Dim freq = GetNullableDouble(res, "frequency_mhz")
        Dim tension = GetNullableDouble(res, "tension_mesuree")
        Dim lu = GetNullableDouble(res, "screen_read_pct")

        Dim newRow As DataGridViewRow = New DataGridViewRow()
        newRow.CreateCells(dataGridViewRFA,
            FormatCell(freq, "F2"),
            If(tension.HasValue, tension.Value.ToString("F4") & " V", "—"),
            If(lu.HasValue, lu.Value.ToString("F1") & " %", "—"),
            "…",
            "…"
        )
        ' L'ordre de mesure (référence/6e en premier) ne correspond plus à
        ' l'ordre d'affichage voulu (croissant en fréquence) : on insère
        ' donc chaque nouvelle ligne à la bonne position plutôt que de
        ' l'ajouter en fin de tableau.
        Dim insertIndex As Integer = dataGridViewRFA.Rows.Count
        For i As Integer = 0 To dataGridViewRFA.Rows.Count - 1
            Dim existingFreqText As String = dataGridViewRFA.Rows(i).Cells(0).Value?.ToString()
            Dim existingFreq As Double
            If freq.HasValue AndAlso Double.TryParse(existingFreqText, NumberStyles.Float, CultureInfo.InvariantCulture, existingFreq) Then
                If freq.Value < existingFreq Then
                    insertIndex = i
                    Exit For
                End If
            End If
        Next
        dataGridViewRFA.Rows.Insert(insertIndex, newRow)
        dataGridViewRFA.FirstDisplayedScrollingRowIndex = insertIndex
    End Sub

    ''' <summary>
    ''' Reçoit la réponse de la commande 'complete' de reponse_frequence.py
    ''' (points recalculés avec % écran attendu et écart dB, plus
    ''' Fmax/Fl/Fu/Fo/Df -- voir ISO 22232-1 §9.4.2, formules 12 et 13).
    ''' Reconstruit le tableau puis affiche le résumé et, si les données
    ''' constructeur ont été fournies, la conclusion de conformité
    ''' (§9.4.2.2 : ±10 %). Doit être appelé sur le thread UI.
    ''' </summary>
    Public Sub FinalizeResults(summary As JsonElement)
        If _finalized Then Return
        _finalized = True

        Dim pointsEl As JsonElement
        If summary.TryGetProperty("points", pointsEl) AndAlso pointsEl.ValueKind = JsonValueKind.Array Then
            dataGridViewRFA.Rows.Clear()
            For Each pt In pointsEl.EnumerateArray()
                Dim freq = GetNullableDouble(pt, "frequency_mhz")
                Dim tension = GetNullableDouble(pt, "tension_v")
                Dim lu = GetNullableDouble(pt, "screen_read_pct")
                Dim attendu = GetNullableDouble(pt, "pct_attendu")
                Dim ecart = GetNullableDouble(pt, "ecart_db")

                Dim rowIndex As Integer = dataGridViewRFA.Rows.Add(
                    FormatCell(freq, "F2"),
                    If(tension.HasValue, tension.Value.ToString("F4") & " V", "—"),
                    If(lu.HasValue, lu.Value.ToString("F1") & " %", "—"),
                    If(attendu.HasValue, attendu.Value.ToString("F1") & " %", "—"),
                    FormatCell(ecart, "F2")
                )

                ' Met en évidence le point de référence (Fmax, 0 dB).
                If ecart.HasValue AndAlso Math.Abs(ecart.Value) < 0.005 Then
                    dataGridViewRFA.Rows(rowIndex).DefaultCellStyle.BackColor = Color.FromArgb(255, 244, 200)
                End If
            Next
        End If

        Dim errAnalyseEl As JsonElement
        If summary.TryGetProperty("error_analysis", errAnalyseEl) AndAlso errAnalyseEl.ValueKind = JsonValueKind.String Then
            lblResume.Text = $"Filtre actif : {_nomFiltre}" & vbCrLf & errAnalyseEl.GetString()
            lblConclusion.Text = "Indéterminé"
            lblConclusion.ForeColor = Color.DimGray
            Return
        End If

        Dim fmax = GetNullableDouble(summary, "fmax_mhz")
        Dim fl = GetNullableDouble(summary, "fl_mhz")
        Dim fu = GetNullableDouble(summary, "fu_mhz")
        Dim f0 = GetNullableDouble(summary, "f0_mhz")
        Dim df = GetNullableDouble(summary, "delta_f_mhz")
        Dim foC = GetNullableDouble(summary, "fo_constructeur_mhz")
        Dim dfC = GetNullableDouble(summary, "df_constructeur_mhz")
        Dim erreurFo = GetNullableDouble(summary, "erreur_fo_pct")
        Dim erreurDf = GetNullableDouble(summary, "erreur_df_pct")

        Dim conformeEl As JsonElement
        Dim conformeTexte As String = "Indéterminé (données constructeur manquantes, ou Fl/Fu non trouvés)"
        Dim conformeCouleur As Color = Color.DimGray
        If summary.TryGetProperty("conforme", conformeEl) Then
            If conformeEl.ValueKind = JsonValueKind.True Then
                conformeTexte = "Bande Passante de l'amplificateur CONFORME (ISO 22232-1 §9.4.2.2 : Fo et Df dans ±10 %)"
                conformeCouleur = Color.FromArgb(0, 128, 0)
            ElseIf conformeEl.ValueKind = JsonValueKind.False Then
                conformeTexte = "Bande Passante de l'amplificateur NON CONFORME (Fo et/ou Df hors ±10 %)"
                conformeCouleur = Color.FromArgb(200, 0, 0)
            End If
        End If

        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine($"Filtre actif : {_nomFiltre}")
        sb.AppendLine($"Fréq. max mesurée : {FormatCell(fmax)} MHz     Fréq. inf. Fl : {FormatCell(fl)} MHz     Fréq. sup. Fu : {FormatCell(fu)} MHz")
        sb.AppendLine($"Valeurs calculées  →  Fo = {FormatCell(f0)} MHz     Df = {FormatCell(df)} MHz")
        If foC.HasValue OrElse dfC.HasValue Then
            sb.AppendLine($"Données constructeur  →  Fo = {FormatCell(foC)} MHz     Df = {FormatCell(dfC)} MHz     Erreur Fo = {FormatCell(erreurFo)} %     Erreur Df = {FormatCell(erreurDf)} %")
        End If

        lblResume.Text = sb.ToString()
        lblConclusion.Text = conformeTexte
        lblConclusion.ForeColor = conformeCouleur
    End Sub

    ''' <summary>
    ''' Appelé quand l'acquisition est interrompue (annulation, erreur,
    ''' timeout) avant que 'complete' ait pu être envoyé/reçu -- sans ça la
    ''' fenêtre reste bloquée pour toujours sur des "…" en attente de
    ''' valeurs qui ne viendront jamais. N'écrase pas un FinalizeResults
    ''' déjà passé (appel sans effet si le run s'est en fait terminé
    ''' normalement juste avant). Doit être appelé sur le thread UI.
    ''' </summary>
    Public Sub FinalizePartial(reason As String)
        If _finalized Then Return
        _finalized = True

        lblResume.Text = $"Filtre actif : {_nomFiltre}" & vbCrLf & reason & vbCrLf &
            $"{dataGridViewRFA.Rows.Count} point(s) mesuré(s) avant l'arrêt -- Fmax/Fl/Fu/Fo/Df non calculés."
        lblConclusion.Text = "Test incomplet"
        lblConclusion.ForeColor = Color.DarkOrange
    End Sub

End Class