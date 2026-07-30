Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

Public Class LvaResultsForm

    ' ============================================================
    ' 1. CONSTRUCTOR (Receives data from UcLVA using the GLOBAL LvaResult)
    ' ============================================================
    Public Sub New(results As List(Of LvaResult),
                   freq As Double,
                   refVoltage As Double,
                   maxError As Double,
                   inTolerance As Boolean,
                   gain As Double)

        ' This calls the hidden Visual Studio code for your dgvResults
        InitializeComponent()

        ' Load the data into your existing grid
        LoadResultData(results)
    End Sub

    ' ============================================================
    ' 2. DATA LOADING LOGIC (Only populates your dgvResults)
    ' ============================================================
    Private Sub LoadResultData(results As List(Of LvaResult))

        ' Fill the Data Grid
        If results IsNot Nothing AndAlso results.Count > 0 Then
            dgvResults.DataSource = results

            ' Configure column headers
            If dgvResults.Columns.Count > 0 Then
                dgvResults.Columns("Db").HeaderText = "dB"
                dgvResults.Columns("MeasuredVoltage").HeaderText = "Mesuré (V)"
                dgvResults.Columns("IsoTarget").HeaderText = "Cible ISO"
                dgvResults.Columns("CalculatedPercent").HeaderText = "% Calculé"
                dgvResults.Columns("ScreenRead").HeaderText = "Lecture Écran"
                dgvResults.Columns("ScreenError").HeaderText = "Erreur Écran %"
                dgvResults.Columns("InTolerance").HeaderText = "Conforme"
            End If
        Else
            MessageBox.Show("Aucune donnée de résultat à afficher.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

End Class