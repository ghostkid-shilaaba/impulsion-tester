Imports System.Text.Json
Imports System.Windows.Forms

Public Class LineariteGainResultsForm

    ' Each result added via AddResult is shaped like what linearite_gain.py
    ' returns for one gain step:
    '   { reglage_gain, attenuateur_externe, tension_mesuree,
    '     gain_reel_total, gain_reel_par_pas, ecart_par_pas, ecart_total }
    Private _maxEcartParPas As Double = 0.0
    Private _maxEcartTotal As Double = 0.0
    Private _summaryAdded As Boolean = False

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub LineariteGainResultsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Linéarité du Gain - Résultats"
        SetupTable()
    End Sub

    Private Sub SetupTable()
        dataGridViewGain.Rows.Clear()
        dataGridViewGain.Columns.Clear()

        dataGridViewGain.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        dataGridViewGain.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 120, 215)
        dataGridViewGain.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        dataGridViewGain.DefaultCellStyle.Font = New Font("Segoe UI", 9)

        dataGridViewGain.Columns.Add("colReglage", "Réglage gain sur D20+")
        dataGridViewGain.Columns.Add("colAtt", "Atténuateur externe")
        dataGridViewGain.Columns.Add("colTension", "Tension mesurée")
        dataGridViewGain.Columns.Add("colGainTotal", "Gain réel total")
        dataGridViewGain.Columns.Add("colGainParPas", "Gain réel par pas")
        dataGridViewGain.Columns.Add("colEcartParPas", "Ecart par pas")
        dataGridViewGain.Columns.Add("colEcartTotal", "Ecart total")

        dataGridViewGain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dataGridViewGain.AllowUserToAddRows = False
        dataGridViewGain.ReadOnly = True
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
        If value.HasValue Then
            Return value.Value.ToString(fmt)
        End If
        Return "—"
    End Function

    ''' <summary>
    ''' Appends one row for a single gain-step measurement. Call this as each
    ''' "measure" response comes back from Python, so the grid fills in live
    ''' instead of waiting for the whole acquisition to finish. Must be called
    ''' on the UI thread (the caller is expected to marshal via SafeInvoke).
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
            Dim errRowIndex As Integer = dataGridViewGain.Rows.Add("", "", "", "", "", "", $"❌ {errMsg}")
            dataGridViewGain.FirstDisplayedScrollingRowIndex = errRowIndex
            Return
        End If

        Dim reglage = GetNullableDouble(res, "reglage_gain")
        Dim att = GetNullableDouble(res, "attenuateur_externe")
        Dim tension = GetNullableDouble(res, "tension_mesuree")
        Dim gainTotal = GetNullableDouble(res, "gain_reel_total")
        Dim gainParPas = GetNullableDouble(res, "gain_reel_par_pas")
        Dim ecartParPas = GetNullableDouble(res, "ecart_par_pas")
        Dim ecartTotal = GetNullableDouble(res, "ecart_total")

        Dim newRowIndex As Integer = dataGridViewGain.Rows.Add(
            FormatCell(reglage, "F1"),
            FormatCell(att, "F1"),
            If(tension.HasValue, tension.Value.ToString("F4") & " V", "—"),
            FormatCell(gainTotal),
            FormatCell(gainParPas),
            FormatCell(ecartParPas),
            FormatCell(ecartTotal)
        )
        dataGridViewGain.FirstDisplayedScrollingRowIndex = newRowIndex

        If ecartParPas.HasValue AndAlso Math.Abs(ecartParPas.Value) > _maxEcartParPas Then
            _maxEcartParPas = Math.Abs(ecartParPas.Value)
        End If
        If ecartTotal.HasValue AndAlso Math.Abs(ecartTotal.Value) > _maxEcartTotal Then
            _maxEcartTotal = Math.Abs(ecartTotal.Value)
        End If
    End Sub

    ''' <summary>
    ''' Appends the "Ecarts maxi" summary row from whatever results have been
    ''' added so far. Safe to call once at the end of a completed run, or from
    ''' a cancellation/error path to summarize a partial run. Calling it more
    ''' than once is a no-op. Must be called on the UI thread.
    ''' </summary>
    Public Sub FinalizeSummary()
        If _summaryAdded Then Return
        If dataGridViewGain.Rows.Count = 0 Then Return

        _summaryAdded = True
        Dim summaryIndex As Integer = dataGridViewGain.Rows.Add("", "", "", "", "Ecarts maxi",
                                    _maxEcartParPas.ToString("F2"), _maxEcartTotal.ToString("F2"))
        Dim summaryRow As DataGridViewRow = dataGridViewGain.Rows(summaryIndex)
        summaryRow.DefaultCellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        summaryRow.DefaultCellStyle.BackColor = Color.FromArgb(230, 230, 230)
        dataGridViewGain.FirstDisplayedScrollingRowIndex = summaryIndex
    End Sub
End Class