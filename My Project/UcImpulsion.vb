Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Text.Json
Imports System.Collections.Generic

Public Class UcImpulsion

    ' ------------------------------------------------------------------
    ' ÉVÉNEMENTS DE NAVIGATION
    ' ------------------------------------------------------------------
    Public Event SuivantClicked As EventHandler
    Public Event PrecedentClicked As EventHandler

    Private isScopeConnected As Boolean = False
    Private isGenConnected As Boolean = False

    ' Adresses VISA réellement détectées (remplacent les valeurs codées en dur)
    Private scopeResource As String = Nothing
    Private generatorResource As String = Nothing

    Private _cancelRequested As Boolean = False

    Private Sub UcImpulsion_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ConfigurerGrille()

        If cmbNbImpulsions.Items.Count > 0 Then
            cmbNbImpulsions.SelectedIndex = 0
        End If

        VerifyConnections()
    End Sub

    Private Sub ConfigurerGrille()
        dgvImpulsions.Columns.Clear()
        dgvImpulsions.Columns.Add("colNum", "Impulsion N°")
        dgvImpulsions.Columns.Add("colTension", "Tension (V)")
        dgvImpulsions.Columns.Add("colAmortissement", "Amortissement (Ω)")
        dgvImpulsions.Columns.Add("colPRF", "PRF (Hz)")
        dgvImpulsions.Columns.Add("colStatut", "Statut")

        dgvImpulsions.Columns("colNum").ReadOnly = True
        dgvImpulsions.Columns("colStatut").ReadOnly = True

        dgvImpulsions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvImpulsions.AllowUserToAddRows = False
        dgvImpulsions.AllowUserToDeleteRows = False
    End Sub

    Private Sub cmbNbImpulsions_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbNbImpulsions.SelectedIndexChanged
        If cmbNbImpulsions.SelectedItem Is Nothing Then Exit Sub

        Dim nbImpulsions As Integer = Convert.ToInt32(cmbNbImpulsions.SelectedItem)
        dgvImpulsions.Rows.Clear()

        For i As Integer = 1 To nbImpulsions
            dgvImpulsions.Rows.Add($"Impulsion {i}", "", "", "", "En attente")
        Next
    End Sub

    ' ------------------------------------------------------------------
    ' DÉTECTION HARDWARE (Via Processus Python Isolé)
    ' ------------------------------------------------------------------
    Public Sub VerifyConnections()
        Dim scopeIdn As String = Nothing
        Dim generatorIdn As String = Nothing

        ' Réinitialiser les adresses tant qu'on n'a pas confirmé une nouvelle détection
        scopeResource = Nothing
        generatorResource = Nothing

        Try
            Dim psi As New ProcessStartInfo()
            psi.FileName = "python"
            psi.Arguments = "visa_checker.py"
            psi.RedirectStandardOutput = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True

            Using p As Process = Process.Start(psi)
                Dim output As String = p.StandardOutput.ReadToEnd()
                p.WaitForExit()

                If Not String.IsNullOrEmpty(output) Then
                    Using doc As JsonDocument = JsonDocument.Parse(output)
                        Dim root = doc.RootElement

                        ' visa_checker.py renvoie un OBJET par appareil, pas une simple
                        ' chaîne, ex : {"oscilloscope": {"idn": "...", "resource": "...", "manufacturer": "..."}}
                        Dim scopeVal As JsonElement
                        If root.TryGetProperty("oscilloscope", scopeVal) AndAlso scopeVal.ValueKind = JsonValueKind.Object Then
                            Dim idnEl As JsonElement
                            Dim resEl As JsonElement

                            If scopeVal.TryGetProperty("idn", idnEl) AndAlso idnEl.ValueKind = JsonValueKind.String Then
                                scopeIdn = idnEl.GetString()
                            End If

                            If scopeVal.TryGetProperty("resource", resEl) AndAlso resEl.ValueKind = JsonValueKind.String Then
                                scopeResource = resEl.GetString()
                            End If
                        End If

                        Dim genVal As JsonElement
                        If root.TryGetProperty("generator", genVal) AndAlso genVal.ValueKind = JsonValueKind.Object Then
                            Dim idnEl As JsonElement
                            Dim resEl As JsonElement

                            If genVal.TryGetProperty("idn", idnEl) AndAlso idnEl.ValueKind = JsonValueKind.String Then
                                generatorIdn = idnEl.GetString()
                            End If

                            If genVal.TryGetProperty("resource", resEl) AndAlso resEl.ValueKind = JsonValueKind.String Then
                                generatorResource = resEl.GetString()
                            End If
                        End If
                    End Using
                End If
            End Using
        Catch ex As Exception
            scopeIdn = Nothing
            generatorIdn = Nothing
            scopeResource = Nothing
            generatorResource = Nothing
        End Try

        ' Un appareil n'est considéré connecté que si on a à la fois son IDN et
        ' son adresse VISA -- sans adresse, on ne peut pas s'y connecter ensuite.
        isScopeConnected = Not String.IsNullOrEmpty(scopeIdn) AndAlso Not String.IsNullOrEmpty(scopeResource)
        isGenConnected = Not String.IsNullOrEmpty(generatorIdn) AndAlso Not String.IsNullOrEmpty(generatorResource)

        If Not isScopeConnected OrElse Not isGenConnected Then
            btnAcquerir.Enabled = False

            Dim missing As String = ""
            If Not isScopeConnected Then missing &= "• Oscilloscope" & vbCrLf
            If Not isGenConnected Then missing &= "• Générateur de fonctions" & vbCrLf

            MessageBox.Show("Erreur de connexion hardware !" & vbCrLf & vbCrLf &
                            "Équipement(s) non détecté(s) :" & vbCrLf & missing & vbCrLf &
                            "Veuillez vérifier vos câbles USB/LAN et re-scanner.",
                            "Connexion Hardware", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Else
            btnAcquerir.Enabled = True
        End If

        UpdateContextMenu(scopeIdn, generatorIdn)
    End Sub

    Private Sub UpdateContextMenu(scopeName As String, generatorName As String)
        cmsAppareils.Items.Clear()

        Dim lblHeader As New ToolStripMenuItem("Équipements Détectés (*IDN?) :")
        lblHeader.Enabled = False
        cmsAppareils.Items.Add(lblHeader)
        cmsAppareils.Items.Add(New ToolStripSeparator())

        Dim oscStatus As String = If(isScopeConnected, $"✔ Scope: {scopeName}", "❌ Scope: Non connecté")
        Dim oscItem As New ToolStripMenuItem(oscStatus)
        oscItem.Enabled = False
        cmsAppareils.Items.Add(oscItem)

        Dim genStatus As String = If(isGenConnected, $"✔ Générateur: {generatorName}", "❌ Générateur: Non connecté")
        Dim genItem As New ToolStripMenuItem(genStatus)
        genItem.Enabled = False
        cmsAppareils.Items.Add(genItem)

        cmsAppareils.Items.Add(New ToolStripSeparator())

        Dim btnRefresh As New ToolStripMenuItem("🔄 Re-scanner les appareils")
        AddHandler btnRefresh.Click, Sub(s, args) VerifyConnections()
        cmsAppareils.Items.Add(btnRefresh)
    End Sub

    Private Sub btnThreeDots_Click(sender As Object, e As EventArgs) Handles btnThreeDots.Click
        cmsAppareils.Show(btnThreeDots, New Point(0, btnThreeDots.Height))
    End Sub

    Private Sub btnAcquerir_Click(sender As Object, e As EventArgs) Handles btnAcquerir.Click
        ' 0. S'assurer qu'on a bien des adresses VISA valides avant de continuer
        If String.IsNullOrEmpty(scopeResource) OrElse String.IsNullOrEmpty(generatorResource) Then
            MessageBox.Show("Adresses VISA introuvables. Veuillez re-scanner les appareils.",
                            "Connexion Hardware", MessageBoxButtons.OK, MessageBoxIcon.Error)
            VerifyConnections()
            Exit Sub
        End If

        ' 1. Validation de toutes les entrées
        For Each row As DataGridViewRow In dgvImpulsions.Rows
            If row.IsNewRow Then Continue For

            Dim tensionVal As String = Convert.ToString(row.Cells("colTension").Value).Trim()
            Dim amortVal As String = Convert.ToString(row.Cells("colAmortissement").Value).Trim()
            Dim prfVal As String = Convert.ToString(row.Cells("colPRF").Value).Trim()

            If String.IsNullOrEmpty(tensionVal) OrElse String.IsNullOrEmpty(amortVal) OrElse String.IsNullOrEmpty(prfVal) Then
                MessageBox.Show($"Veuillez remplir toutes les valeurs pour la ligne {row.Cells("colNum").Value}.",
                                "Saisie incomplète", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Exit Sub
            End If

            Dim tempDouble As Double
            If Not Double.TryParse(tensionVal, NumberStyles.Any, CultureInfo.InvariantCulture, tempDouble) OrElse
               Not Double.TryParse(amortVal, NumberStyles.Any, CultureInfo.InvariantCulture, tempDouble) OrElse
               Not Double.TryParse(prfVal, NumberStyles.Any, CultureInfo.InvariantCulture, tempDouble) Then

                MessageBox.Show($"Les valeurs de la ligne {row.Cells("colNum").Value} doivent être des nombres valides.",
                                "Format invalide", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Exit Sub
            End If
        Next

        _cancelRequested = False
        btnAcquerir.Enabled = False

        ' Mettre à jour le statut visuel
        For Each row As DataGridViewRow In dgvImpulsions.Rows
            If row.IsNewRow Then Continue For
            row.Cells("colStatut").Value = "En cours..."
        Next
        dgvImpulsions.Refresh()
        Application.DoEvents()

        ' 2. Construire le payload JSON pour Python
        Dim impulsesList As New List(Of Dictionary(Of String, Object))()
        Dim indexCounter As Integer = 1

        For Each row As DataGridViewRow In dgvImpulsions.Rows
            If row.IsNewRow Then Continue For

            Dim impDict As New Dictionary(Of String, Object) From {
                {"index", indexCounter},
                {"amplitude", Convert.ToDouble(row.Cells("colTension").Value, CultureInfo.InvariantCulture)},
                {"damping", Convert.ToDouble(row.Cells("colAmortissement").Value, CultureInfo.InvariantCulture)},
                {"prf", Convert.ToDouble(row.Cells("colPRF").Value, CultureInfo.InvariantCulture)}
            }
            impulsesList.Add(impDict)
            indexCounter += 1
        Next

        ' Utiliser les adresses VISA réellement détectées (voir VerifyConnections),
        ' plus aucune adresse codée en dur ici.
        Dim mainPayload As New Dictionary(Of String, Object) From {
            {"scope_resource", scopeResource},
            {"generator_resource", generatorResource},
            {"impulses", impulsesList}
        }

        Dim jsonPayload As String = JsonSerializer.Serialize(mainPayload)

        ' Échapper les guillemets pour l'argument en ligne de commande
        Dim escapedJson As String = jsonPayload.Replace("""", "\""")

        ' 3. Exécuter le processus Python
        Try
            Dim psi As New ProcessStartInfo()
            psi.FileName = "python"
            psi.Arguments = $"visa_commander.py ""{escapedJson}"""
            psi.RedirectStandardOutput = True
            psi.RedirectStandardError = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True

            Using p As Process = Process.Start(psi)
                Dim output As String = p.StandardOutput.ReadToEnd()
                Dim errOutput As String = p.StandardError.ReadToEnd()
                p.WaitForExit()

                If _cancelRequested Then
                    For Each row As DataGridViewRow In dgvImpulsions.Rows
                        If row.IsNewRow Then Continue For
                        row.Cells("colStatut").Value = "Interrompu"
                    Next
                    MessageBox.Show("Acquisition interrompue.", "Interruption", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Exit Sub
                End If

                If Not String.IsNullOrEmpty(errOutput) AndAlso p.ExitCode <> 0 Then
                    MessageBox.Show($"Erreur d'exécution Python : {errOutput}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Exit Sub
                End If

                If Not String.IsNullOrEmpty(output) Then
                    Using doc As JsonDocument = JsonDocument.Parse(output)
                        Dim root = doc.RootElement

                        If root.GetProperty("success").GetBoolean() Then
                            ' Mettre à jour le statut de toutes les lignes à OK
                            For Each row As DataGridViewRow In dgvImpulsions.Rows
                                If row.IsNewRow Then Continue For
                                row.Cells("colStatut").Value = "OK ✔"
                            Next
                            dgvImpulsions.Refresh()

                            ' Récupérer les résultats
                            Dim resultsArray = root.GetProperty("results")

                            ' Vérifier si des résultats existent
                            If resultsArray.GetArrayLength() > 0 Then
                                ' 4. Ouvrir ResultsForm en passant le tableau JSON directement
                                Dim resultsForm As New ResultsForm(resultsArray)
                                resultsForm.ShowDialog()
                            Else
                                MessageBox.Show("Aucun résultat retourné par l'acquisition.",
                                                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                            End If
                        Else
                            ' Gérer l'erreur - TryGetProperty avec JsonElement
                            Dim errElement As JsonElement
                            If root.TryGetProperty("error", errElement) Then
                                Dim errMessage As String = errElement.GetString()
                                MessageBox.Show($"Erreur hardware : {errMessage}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            Else
                                MessageBox.Show("Erreur hardware inconnue.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            End If
                        End If
                    End Using
                Else
                    MessageBox.Show("Aucune réponse du processus Python.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End Using

        Catch ex As Exception
            MessageBox.Show($"Erreur critique : {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            btnAcquerir.Enabled = True
        End Try
    End Sub

    Private Sub btnPrecedent_Click(sender As Object, e As EventArgs) Handles btnPrecedent.Click
        RaiseEvent PrecedentClicked(Me, EventArgs.Empty)
    End Sub

    Private Sub btnSuivant_Click(sender As Object, e As EventArgs) Handles btnSuivant.Click
        RaiseEvent SuivantClicked(Me, EventArgs.Empty)
    End Sub

    Private Sub btnArreter_Click(sender As Object, e As EventArgs) Handles btnArreter.Click
        _cancelRequested = True
    End Sub
End Class