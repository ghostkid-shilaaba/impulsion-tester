Imports System.Text.Json
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting
Imports System.IO

Public Class ResultsForm
    Private _resultsData As JsonElement

    ' Constructor receiving the processed JSON array directly from Python
    Public Sub New(resultsArray As JsonElement)
        InitializeComponent()
        _resultsData = resultsArray
    End Sub

    Private Sub ResultsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SetupCleanTable()
        PopulateResultsAndGraphs()
    End Sub

    Private Sub SetupCleanTable()
        dataGridViewResults.Rows.Clear()
        dataGridViewResults.Columns.Clear()

        ' Style the header
        dataGridViewResults.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        dataGridViewResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 120, 215)
        dataGridViewResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        dataGridViewResults.DefaultCellStyle.Font = New Font("Segoe UI", 9)

        ' Table columns matching your measurement specifications.
        ' NOTE: Vr (Vreverb) column removed -- per the cahier des charges,
        ' this measurement is no longer requested (V50 now equals Vcc directly).
        dataGridViewResults.Columns.Add("colDamping", "Damping (Ω)")
        dataGridViewResults.Columns.Add("colAmplitude", "Amplitude (V)")
        dataGridViewResults.Columns.Add("colV50Meas", "V50 / Vcc Mesuré (V)")
        dataGridViewResults.Columns.Add("colTdMeas", "td Mesuré (ns)")
        dataGridViewResults.Columns.Add("colTrMeas", "tr Mesuré (ns)")

        dataGridViewResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dataGridViewResults.AllowUserToAddRows = False
    End Sub

    Private Function GetDoubleFromJson(element As JsonElement, propertyName As String, Optional defaultValue As Double = 0.0) As Double
        Try
            Dim valueElement As JsonElement
            If element.TryGetProperty(propertyName, valueElement) Then
                Return valueElement.GetDouble()
            End If
        Catch
            ' Return default value if property doesn't exist or can't be parsed
        End Try
        Return defaultValue
    End Function

    Private Sub PopulateResultsAndGraphs()
        flowPanelGraphs.Controls.Clear()

        Dim rowIndex As Integer = 0

        For Each res In _resultsData.EnumerateArray()
            ' Get values from JSON - using the correct property names from Python
            Dim success As Boolean = res.GetProperty("success").GetBoolean()

            If Not success Then
                ' Handle failed measurement
                Dim errorMsg As String = "Erreur inconnue"
                Dim errEl As JsonElement
                If res.TryGetProperty("error", errEl) AndAlso errEl.ValueKind = JsonValueKind.String Then
                    errorMsg = errEl.GetString()
                End If
                dataGridViewResults.Rows.Add("", "", "", "", $"❌ {errorMsg}")
                Continue For
            End If

            ' FIXED: visa_commander.py sends "amplitude_set" / "damping_set"
            ' (these are the values the technician set on the flaw detector's
            ' physical dials -- informational only). Read via the safe
            ' GetDoubleFromJson helper rather than a direct .GetProperty()
            ' call, so a missing/renamed field never crashes this form again.
            Dim damping As Double = GetDoubleFromJson(res, "damping_set")
            Dim amplitude As Double = GetDoubleFromJson(res, "amplitude_set")

            ' Get measured values - using helper function
            Dim v50Meas As Double = GetDoubleFromJson(res, "v50_meas")
            Dim tdMeas As Double = GetDoubleFromJson(res, "td")
            Dim trMeas As Double = GetDoubleFromJson(res, "tr")
            ' NOTE: "vr" is no longer read at all -- this measurement was
            ' dropped per the cahier des charges.

            ' 1. Add data row to the grid
            dataGridViewResults.Rows.Add(
                damping.ToString("F1"),
                amplitude.ToString("F1"),
                v50Meas.ToString("F2"),
                tdMeas.ToString("F1"),
                trMeas.ToString("F1")
            )

            ' Apply color based on pass/fail criteria (example thresholds)
            Dim row As DataGridViewRow = dataGridViewResults.Rows(rowIndex)

            ' Check td limits (example: 55-65 ns)
            If tdMeas >= 55 AndAlso tdMeas <= 65 Then
                row.Cells("colTdMeas").Style.BackColor = Color.LightGreen
            ElseIf tdMeas > 0 Then
                row.Cells("colTdMeas").Style.BackColor = Color.LightPink
            End If

            ' Check tr limits (example: <10 ns)
            If trMeas < 10 AndAlso trMeas > 0 Then
                row.Cells("colTrMeas").Style.BackColor = Color.LightGreen
            ElseIf trMeas > 0 Then
                row.Cells("colTrMeas").Style.BackColor = Color.LightPink
            End If

            rowIndex += 1

            ' 2. Dynamically build the graph box with the settings header block on top
            Dim groupBox As New GroupBox()
            groupBox.Width = 420
            groupBox.Height = 310
            groupBox.Margin = New Padding(10)
            groupBox.Font = New Font("Segoe UI", 9, FontStyle.Bold)
            groupBox.ForeColor = Color.FromArgb(64, 64, 64)
            groupBox.BackColor = Color.White
            groupBox.Text = $"Impulsion {res.GetProperty("index").GetInt32()}"

            ' Parameters label at top of group box
            Dim lblParams As New Label()
            lblParams.Dock = DockStyle.Top
            lblParams.Height = 45
            lblParams.Padding = New Padding(10, 5, 0, 0)
            lblParams.Text = $"Amplitude: {amplitude:F1} V    |    Damping: {damping:F1} Ω"
            lblParams.Font = New Font("Segoe UI", 9, FontStyle.Regular)
            lblParams.ForeColor = Color.FromArgb(64, 64, 64)
            lblParams.BackColor = Color.FromArgb(240, 240, 240)

            ' Screenshot button: opens the oscilloscope's saved screen
            ' capture (per impulse) in the default image viewer, if one
            ' was successfully captured for this row.
            Dim screenshotPath As String = Nothing
            Dim screenshotEl As JsonElement
            If res.TryGetProperty("screenshot_path", screenshotEl) AndAlso screenshotEl.ValueKind = JsonValueKind.String Then
                screenshotPath = screenshotEl.GetString()
            End If

            Dim btnScreenshot As New Button()
            btnScreenshot.Dock = DockStyle.Top
            btnScreenshot.Height = 28
            btnScreenshot.Font = New Font("Segoe UI", 8)
            btnScreenshot.FlatStyle = FlatStyle.Flat

            Dim hasScreenshot As Boolean = Not String.IsNullOrEmpty(screenshotPath) AndAlso File.Exists(screenshotPath)
            If hasScreenshot Then
                btnScreenshot.Text = "📷 Voir la capture d'écran"
                btnScreenshot.Enabled = True
                Dim capturedPath As String = screenshotPath ' local copy for the lambda
                AddHandler btnScreenshot.Click, Sub(s, args)
                                                    Try
                                                        Dim psi As New ProcessStartInfo(capturedPath)
                                                        psi.UseShellExecute = True
                                                        Process.Start(psi)
                                                    Catch ex As Exception
                                                        MessageBox.Show($"Impossible d'ouvrir la capture d'écran : {ex.Message}",
                                                                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                    End Try
                                                End Sub
            Else
                btnScreenshot.Text = "📷 Capture indisponible"
                btnScreenshot.Enabled = False
            End If

            ' Create chart with dark background for better visibility
            Dim chart As New Chart()
            chart.Dock = DockStyle.Fill
            chart.BackColor = Color.FromArgb(30, 30, 30)

            Dim chartArea As New ChartArea("Waveform")
            chartArea.AxisX.Title = "Time (ns)"
            chartArea.AxisX.TitleFont = New Font("Segoe UI", 8)
            chartArea.AxisX.TitleForeColor = Color.White
            chartArea.AxisX.LabelStyle.Font = New Font("Segoe UI", 7)
            chartArea.AxisX.LabelStyle.ForeColor = Color.White
            chartArea.AxisX.MajorGrid.LineColor = Color.FromArgb(60, 60, 60)

            chartArea.AxisY.Title = "Voltage (V)"
            chartArea.AxisY.TitleFont = New Font("Segoe UI", 8)
            chartArea.AxisY.TitleForeColor = Color.White
            chartArea.AxisY.LabelStyle.Font = New Font("Segoe UI", 7)
            chartArea.AxisY.LabelStyle.ForeColor = Color.White
            chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(60, 60, 60)

            chartArea.BackColor = Color.FromArgb(30, 30, 30)
            chart.ChartAreas.Add(chartArea)

            ' Main waveform series (green)
            Dim series As New Series("Waveform")
            series.ChartType = SeriesChartType.Line
            series.Color = Color.FromArgb(0, 200, 0) ' Bright green
            series.BorderWidth = 2
            series.ChartArea = "Waveform"
            chart.Series.Add(series)

            ' Populate waveform points returned by Python
            Dim waveformPoints = res.GetProperty("waveform_points")
            Dim xIndex As Double = 0

            ' Try to get x_increment for proper time scaling
            Dim xInc As Double = GetDoubleFromJson(res, "x_increment", 0.001)

            For Each pt In waveformPoints.EnumerateArray()
                series.Points.AddXY(xIndex * xInc * 1000000000.0, pt.GetDouble()) ' Convert to ns
                xIndex += 1
            Next

            ' Add horizontal line for V50/Vcc target (red dashed line).
            ' FIXED: V50 now equals Vcc directly (the pulse's own peak
            ' voltage, per the updated cahier des charges), not "50% of
            ' the peak" as it meant in the old version of this form.
            ' The reference line is drawn at v50Meas itself, not v50Meas*0.5.
            If v50Meas > 0 Then
                Dim v50Line As New Series("V50 / Vcc")
                v50Line.ChartType = SeriesChartType.Line
                v50Line.Color = Color.Red
                v50Line.BorderWidth = 1
                v50Line.BorderDashStyle = ChartDashStyle.Dash
                v50Line.ChartArea = "Waveform"

                ' Get x range from waveform
                Dim xMax As Double = 10
                If series.Points.Count > 0 Then
                    xMax = series.Points(series.Points.Count - 1).XValue
                End If

                v50Line.Points.AddXY(0, v50Meas)
                v50Line.Points.AddXY(xMax, v50Meas)
                chart.Series.Add(v50Line)
            End If

            ' Add series for td (vertical line at pulse duration)
            If tdMeas > 0 Then
                Dim tdLine As New Series("td")
                tdLine.ChartType = SeriesChartType.Line
                tdLine.Color = Color.Yellow
                tdLine.BorderWidth = 1
                tdLine.BorderDashStyle = ChartDashStyle.Dot
                tdLine.ChartArea = "Waveform"

                Dim yMin As Double = -0.5
                Dim yMax As Double = v50Meas * 1.1
                If yMax <= 0 Then yMax = 10

                tdLine.Points.AddXY(tdMeas, yMin)
                tdLine.Points.AddXY(tdMeas, yMax)
                chart.Series.Add(tdLine)
            End If

            groupBox.Controls.Add(chart)
            groupBox.Controls.Add(lblParams)
            groupBox.Controls.Add(btnScreenshot)
            flowPanelGraphs.Controls.Add(groupBox)
        Next

        ' If no results were added, show a message
        If flowPanelGraphs.Controls.Count = 0 Then
            Dim lblNoData As New Label()
            lblNoData.Text = "Aucune donnée à afficher."
            lblNoData.Font = New Font("Segoe UI", 12, FontStyle.Bold)
            lblNoData.ForeColor = Color.Gray
            lblNoData.Dock = DockStyle.Fill
            lblNoData.TextAlign = ContentAlignment.MiddleCenter
            flowPanelGraphs.Controls.Add(lblNoData)
        End If
    End Sub
End Class