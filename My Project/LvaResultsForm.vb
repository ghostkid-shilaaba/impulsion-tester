Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Linq

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

            ' L'ordre de mesure reste 2, 1, 0, 4, 6, 8... (c'est l'ordre dans
            ' lequel l'opérateur règle l'atténuateur pendant le test, ne pas
            ' y toucher). On trie uniquement la liste utilisée pour
            ' l'affichage, pour que le tableau final ressemble à
            ' 0, 1, 2, 4, 6, 8, 12, 14, 20, 26.
            Dim sortedResults As List(Of LvaResult) = results.OrderBy(Function(r) r.Db).ToList()

            dgvResults.DataSource = sortedResults

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