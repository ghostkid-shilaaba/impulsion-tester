Imports System.Text.Json
Imports System.Windows.Forms

Public Class LineariteGainResultsForm

    ' rows: a JsonElement array, one entry per gain step, each shaped like
    ' what linearite_gain.py returns:
    '   { reglage_gain, attenuateur_externe, tension_mesuree,
    '     gain_reel_total, gain_reel_par_pas, ecart_par_pas, ecart_total }
    Private _rowsData As JsonElement

    Public Sub New(rowsData As JsonElement)
        InitializeComponent()
        _rowsData = rowsData
    End Sub

    Private Sub LineariteGainResultsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Linéarité du Gain - Résultats"
        SetupTable()
        PopulateTable()
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

    Private Sub PopulateTable()
        Dim rowIndex As Integer = 0
        Dim maxEcartParPas As Double = 0.0
        Dim maxEcartTotal As Double = 0.0

        For Each res In _rowsData.EnumerateArray()
            Dim success As Boolean = True
            Dim successEl As JsonElement
            If res.TryGetProperty("success", successEl) AndAlso successEl.ValueKind = JsonValueKind.False Then
                success = False
            End If

            If Not success Then
                Dim errMsg As String = "Erreur"
                Dim errEl As JsonElement
                If res.TryGetProperty("error", errEl) Then errMsg = errEl.GetString()
                dataGridViewGain.Rows.Add("", "", "", "", "", "", $"❌ {errMsg}")
                Continue For
            End If

            Dim reglage = GetNullableDouble(res, "reglage_gain")
            Dim att = GetNullableDouble(res, "attenuateur_externe")
            Dim tension = GetNullableDouble(res, "tension_mesuree")
            Dim gainTotal = GetNullableDouble(res, "gain_reel_total")
            Dim gainParPas = GetNullableDouble(res, "gain_reel_par_pas")
            Dim ecartParPas = GetNullableDouble(res, "ecart_par_pas")
            Dim ecartTotal = GetNullableDouble(res, "ecart_total")

            dataGridViewGain.Rows.Add(
                FormatCell(reglage, "F1"),
                FormatCell(att, "F1"),
                If(tension.HasValue, tension.Value.ToString("F4") & " V", "—"),
                FormatCell(gainTotal),
                FormatCell(gainParPas),
                FormatCell(ecartParPas),
                FormatCell(ecartTotal)
            )

            If ecartParPas.HasValue AndAlso Math.Abs(ecartParPas.Value) > maxEcartParPas Then
                maxEcartParPas = Math.Abs(ecartParPas.Value)
            End If
            If ecartTotal.HasValue AndAlso Math.Abs(ecartTotal.Value) > maxEcartTotal Then
                maxEcartTotal = Math.Abs(ecartTotal.Value)
            End If

            rowIndex += 1
        Next

        ' Summary row: "Ecarts maxi", matching the reference constat layout
        dataGridViewGain.Rows.Add("", "", "", "", "Ecarts maxi",
                                    maxEcartParPas.ToString("F2"), maxEcartTotal.ToString("F2"))
        Dim summaryRow As DataGridViewRow = dataGridViewGain.Rows(dataGridViewGain.Rows.Count - 1)
        summaryRow.DefaultCellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        summaryRow.DefaultCellStyle.BackColor = Color.FromArgb(230, 230, 230)
    End Sub
End Class