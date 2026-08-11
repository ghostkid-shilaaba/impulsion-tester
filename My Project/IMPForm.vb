Imports System.Data.SQLite

Public Class IMPForm

    Private _modeleId As Integer

    Public Sub New(modeleId As Integer)
        InitializeComponent()

        _modeleId = modeleId

        ConfigurerGrille()
        ChargerImpulsions()
    End Sub
    Private Sub ConfigurerGrille()

        dgvImpulsions.Columns.Clear()

        dgvImpulsions.Columns.Add("colNum", "Impulsion N°")
        dgvImpulsions.Columns.Add("colTension", "Tension (V)")
        dgvImpulsions.Columns.Add("colAmortissement", "Amortissement (Ω)")
        dgvImpulsions.Columns.Add("colPRF", "PRF (Hz)")


        dgvImpulsions.Columns("colNum").ReadOnly = True


        dgvImpulsions.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill

        dgvImpulsions.AllowUserToAddRows = False
        dgvImpulsions.AllowUserToDeleteRows = False

    End Sub
    Private Sub cmbNbImpulsions_SelectedIndexChanged(
    sender As Object,
    e As EventArgs
) Handles cmbNbImpulsions.SelectedIndexChanged

        If cmbNbImpulsions.SelectedItem Is Nothing Then Return

        Dim nbImpulsions As Integer =
            Convert.ToInt32(cmbNbImpulsions.SelectedItem)

        dgvImpulsions.Rows.Clear()

        For i As Integer = 1 To nbImpulsions
            dgvImpulsions.Rows.Add(
                $"Impulsion {i}",
                "",
                "",
                "",
                "En attente"
            )
        Next

    End Sub
    Private Sub ChargerImpulsions()

        dgvImpulsions.Rows.Clear()

        Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
            conn.Open()

            Dim sql As String =
            "SELECT numero, tension, amortissement, prf
             FROM Impulsions
             WHERE modele_id = @modeleId
             ORDER BY numero"

            Using cmd As New SQLiteCommand(sql, conn)
                cmd.Parameters.AddWithValue("@modeleId", _modeleId)

                Using reader As SQLiteDataReader = cmd.ExecuteReader()

                    While reader.Read()

                        dgvImpulsions.Rows.Add(
                        $"Impulsion {reader("numero")}",
                        reader("tension").ToString(),
                        reader("amortissement").ToString(),
                        reader("prf").ToString()
                    )

                    End While

                End Using
            End Using
        End Using

    End Sub
    Private Sub btnEnregistrer_Click(sender As Object, e As EventArgs) Handles btnEnregistrer.Click

        Dim dt As New DataTable()

        dt.Columns.Add("numero", GetType(Integer))
        dt.Columns.Add("tension", GetType(Double))
        dt.Columns.Add("amortissement", GetType(Double))
        dt.Columns.Add("prf", GetType(Double))

        For Each row As DataGridViewRow In dgvImpulsions.Rows

            If row.IsNewRow Then Continue For

            Dim tension As Double
            Dim amortissement As Double
            Dim prf As Double

            If Not Double.TryParse(row.Cells("colTension").Value?.ToString(), tension) Then
                MessageBox.Show("Tension invalide.", "Erreur",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            If Not Double.TryParse(row.Cells("colAmortissement").Value?.ToString(), amortissement) Then
                MessageBox.Show("Amortissement invalide.", "Erreur",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            If Not Double.TryParse(row.Cells("colPRF").Value?.ToString(), prf) Then
                MessageBox.Show("PRF invalide.", "Erreur",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            Dim numero As Integer = row.Index + 1

            dt.Rows.Add(numero, tension, amortissement, prf)

        Next

        Try
            DatabaseHelper.EnregistrerImpulsions(_modeleId, dt)

            MessageBox.Show("Les paramètres des impulsions ont été enregistrés.",
                        "Enregistrement",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)

            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show($"Erreur lors de l'enregistrement : {ex.Message}",
                        "Erreur",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error)
        End Try

    End Sub
    Private Sub btnAnnuler_Click(sender As Object, e As EventArgs) Handles btnAnnuler.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

End Class