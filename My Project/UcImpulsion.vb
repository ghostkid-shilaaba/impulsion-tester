Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Text.Json
Imports System.Threading
Imports System.Threading.Tasks

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
    Private _pythonProcess As Process = Nothing
    Private _acquisitionTask As Task = Nothing
    Private _cancellationTokenSource As CancellationTokenSource = Nothing
    Private scopeIdn As String = Nothing
    Private generatorIdn As String = Nothing
    Private scopeConfig As JsonElement = Nothing
    Private generatorConfig As JsonElement = Nothing

    ' Helper to safely invoke UI actions from background threads
    Private Sub SafeInvoke(action As Action)
        If Me.IsHandleCreated AndAlso Me.InvokeRequired Then
            Me.Invoke(action)
        ElseIf Me.IsHandleCreated Then
            action()
        End If
    End Sub

    Public Sub New()
        InitializeComponent()

        ' Hook up the Disposed event for cleanup
        AddHandler Me.Disposed, AddressOf UcImpulsion_Disposed
    End Sub

    Private Sub UcImpulsion_Disposed(sender As Object, e As EventArgs)
        ' Ensure Python process is killed when control is disposed
        Try
            If _pythonProcess IsNot Nothing AndAlso Not _pythonProcess.HasExited Then
                _pythonProcess.Kill()
                _pythonProcess.WaitForExit(1000)
            End If
        Catch
            ' Ignore errors
        End Try
    End Sub

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
        ' Réinitialiser les adresses tant qu'on n'a pas confirmé une nouvelle détection
        scopeResource = Nothing
        generatorResource = Nothing
        scopeIdn = Nothing
        generatorIdn = Nothing
        scopeConfig = Nothing
        generatorConfig = Nothing

        Try
            ' Find the Python script
            Dim scriptPath As String = FindPythonScript("visa_checker.py")
            If String.IsNullOrEmpty(scriptPath) Then
                SafeInvoke(Sub()
                               MessageBox.Show("Impossible de trouver visa_checker.py. Assurez-vous que le fichier est présent.",
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
                                                        errorBuilder.AppendLine(e.Data)
                                                    End If
                                                End Sub

                p.BeginOutputReadLine()
                p.BeginErrorReadLine()

                ' Wait with timeout
                If Not p.WaitForExit(30000) Then
                    Try
                        p.Kill()
                    Catch
                        ' Ignore
                    End Try
                    Throw New Exception("Le processus Python a expiré (30 secondes).")
                End If

                Dim output As String = outputBuilder.ToString()
                Dim errOutput As String = errorBuilder.ToString()

                ' Only treat stderr as error if the process exit code is non-zero
                If p.ExitCode <> 0 AndAlso Not String.IsNullOrEmpty(errOutput) Then
                    SafeInvoke(Sub()
                                   MessageBox.Show($"Erreur Python (code {p.ExitCode}) : {errOutput}",
                                                   "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                               End Sub)
                    Return
                End If

                If Not String.IsNullOrEmpty(output) Then
                    Using doc As JsonDocument = JsonDocument.Parse(output)
                        Dim root = doc.RootElement

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
                                    ' FIXED: Clone() keeps the data alive after the JsonDocument is disposed
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
                                    ' FIXED: Clone() keeps the data alive after the JsonDocument is disposed
                                    generatorConfig = configEl.Clone()
                                End If
                            End If
                        End If
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

        isScopeConnected = Not String.IsNullOrEmpty(scopeIdn) AndAlso Not String.IsNullOrEmpty(scopeResource)
        isGenConnected = Not String.IsNullOrEmpty(generatorIdn) AndAlso Not String.IsNullOrEmpty(generatorResource)

        SafeInvoke(Sub()
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
                   End Sub)
    End Sub

    Private Function FindPythonScript(scriptName As String) As String
        ' 1. Check in the executable folder (if files are copied to output)
        Dim exeFolder As String = Application.StartupPath
        Dim exePathCheck As String = Path.Combine(exeFolder, scriptName)
        If File.Exists(exePathCheck) Then
            Return exePathCheck
        End If

        ' 2. Check in "My Project" folder (development)
        Try
            Dim exePath As String = Application.StartupPath
            Dim projectRoot As String = Directory.GetParent(exePath).Parent.Parent.FullName
            Dim possiblePath As String = Path.Combine(projectRoot, "My Project", scriptName)
            If File.Exists(possiblePath) Then
                Return possiblePath
            End If
        Catch
            ' Ignore
        End Try

        ' 3. Check in the project root (where instrument_configs is)
        Try
            Dim exePath As String = Application.StartupPath
            Dim projectRoot As String = Directory.GetParent(exePath).Parent.Parent.FullName
            Dim possiblePath As String = Path.Combine(projectRoot, scriptName)
            If File.Exists(possiblePath) Then
                Return possiblePath
            End If
        Catch
            ' Ignore
        End Try

        Return Nothing
    End Function

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
        btnAcquerir.Enabled = False
        btnArreter.Enabled = True

        _cancellationTokenSource = New CancellationTokenSource()
        _acquisitionTask = Task.Run(Function() RunAcquisition(_cancellationTokenSource.Token))
    End Sub

    Private Function RunAcquisition(token As CancellationToken) As Task
        Return Task.Run(Sub()
                            Try
                                ' Check resources
                                If String.IsNullOrEmpty(scopeResource) Then
                                    SafeInvoke(Sub()
                                                   MessageBox.Show("Adresses VISA introuvables. Veuillez re-scanner les appareils.",
                                                                   "Connexion Hardware", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                   VerifyConnections()
                                               End Sub)
                                    Return
                                End If

                                If token.IsCancellationRequested Then
                                    HandleCancellation()
                                    Return
                                End If

                                ' Get rows safely from UI thread
                                Dim rows As List(Of DataGridViewRow) = Nothing
                                SafeInvoke(Sub()
                                               rows = dgvImpulsions.Rows.Cast(Of DataGridViewRow)().
                                                   Where(Function(r) Not r.IsNewRow).
                                                   ToList()
                                           End Sub)

                                If rows Is Nothing OrElse rows.Count = 0 Then
                                    Return
                                End If

                                ' Validate each row - on UI thread
                                Dim validationPassed As Boolean = True
                                SafeInvoke(Sub()
                                               For Each row As DataGridViewRow In rows
                                                   Dim tensionVal As String = Convert.ToString(row.Cells("colTension").Value).Trim()
                                                   Dim amortVal As String = Convert.ToString(row.Cells("colAmortissement").Value).Trim()
                                                   Dim prfVal As String = Convert.ToString(row.Cells("colPRF").Value).Trim()

                                                   If String.IsNullOrEmpty(tensionVal) OrElse String.IsNullOrEmpty(amortVal) OrElse String.IsNullOrEmpty(prfVal) Then
                                                       MessageBox.Show($"Veuillez remplir toutes les valeurs pour la ligne {row.Cells("colNum").Value}.",
                                                                       "Saisie incomplète", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                       validationPassed = False
                                                       Return
                                                   End If

                                                   Dim tempDouble As Double
                                                   If Not Double.TryParse(tensionVal, NumberStyles.Any, CultureInfo.InvariantCulture, tempDouble) OrElse
                                                      Not Double.TryParse(amortVal, NumberStyles.Any, CultureInfo.InvariantCulture, tempDouble) OrElse
                                                      Not Double.TryParse(prfVal, NumberStyles.Any, CultureInfo.InvariantCulture, tempDouble) Then

                                                       MessageBox.Show($"Les valeurs de la ligne {row.Cells("colNum").Value} doivent être des nombres valides.",
                                                                       "Format invalide", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                       validationPassed = False
                                                       Return
                                                   End If
                                               Next
                                           End Sub)

                                If Not validationPassed Then
                                    Return
                                End If

                                If token.IsCancellationRequested Then
                                    HandleCancellation()
                                    Return
                                End If

                                _cancelRequested = False

                                ' Reset status - UI thread
                                SafeInvoke(Sub()
                                               For Each row As DataGridViewRow In rows
                                                   row.Cells("colStatut").Value = "En attente"
                                               Next
                                               dgvImpulsions.Refresh()
                                           End Sub)

                                Dim allResultsJson As New List(Of String)()
                                Dim rowIndex As Integer = 0

                                For Each row As DataGridViewRow In rows
                                    If token.IsCancellationRequested Then
                                        HandleCancellation()
                                        Return
                                    End If

                                    ' Get values from UI thread
                                    Dim numLabel As String = ""
                                    Dim tension As Double = 0
                                    Dim damping As Double = 0
                                    Dim prf As Double = 0

                                    SafeInvoke(Sub()
                                                   numLabel = Convert.ToString(row.Cells("colNum").Value)
                                                   tension = Convert.ToDouble(row.Cells("colTension").Value, CultureInfo.InvariantCulture)
                                                   damping = Convert.ToDouble(row.Cells("colAmortissement").Value, CultureInfo.InvariantCulture)
                                                   prf = Convert.ToDouble(row.Cells("colPRF").Value, CultureInfo.InvariantCulture)
                                               End Sub)

                                    ' Prompt the operator - UI thread
                                    Dim promptResult As DialogResult = DialogResult.Cancel
                                    SafeInvoke(Sub()
                                                   promptResult = MessageBox.Show(
                                                       $"Réglez l'appareil à vérifier sur les valeurs suivantes pour {numLabel} :" & vbCrLf & vbCrLf &
                                                       $"Tension : {tension} V" & vbCrLf &
                                                       $"Amortissement : {damping} Ω" & vbCrLf &
                                                       $"PRF : {prf} Hz" & vbCrLf & vbCrLf &
                                                       "Cliquez sur OK une fois le réglage effectué, ou Annuler pour arrêter.",
                                                       "Réglage manuel requis",
                                                       MessageBoxButtons.OKCancel,
                                                       MessageBoxIcon.Information)
                                               End Sub)

                                    If promptResult <> DialogResult.OK Then
                                        HandleCancellation()
                                        Return
                                    End If

                                    If token.IsCancellationRequested Then
                                        HandleCancellation()
                                        Return
                                    End If

                                    ' Update status - UI thread
                                    SafeInvoke(Sub()
                                                   row.Cells("colStatut").Value = "En cours..."
                                                   dgvImpulsions.Refresh()
                                               End Sub)

                                    ' Build payload for this one impulse
                                    Dim singleImpulseDict As New Dictionary(Of String, Object) From {
                                        {"index", rowIndex + 1},
                                        {"amplitude", tension},
                                        {"damping", damping},
                                        {"prf", prf}
                                    }

                                    Dim mainPayload As New Dictionary(Of String, Object) From {
                                        {"scope_resource", scopeResource},
                                        {"scope_idn", scopeIdn},
                                        {"impulses", New List(Of Dictionary(Of String, Object)) From {singleImpulseDict}}
                                    }

                                    ' FIXED: scopeConfig is now cloned so it stays valid
                                    If scopeConfig.ValueKind <> JsonValueKind.Undefined Then
                                        mainPayload.Add("scope_config", scopeConfig)
                                    End If

                                    Dim jsonPayload As String = JsonSerializer.Serialize(mainPayload)
                                    Dim tempFile As String = Path.Combine(Path.GetTempPath(), $"impulsion_payload_{Guid.NewGuid()}.json")
                                    File.WriteAllText(tempFile, jsonPayload)

                                    Dim singleResultJson As String = Nothing

                                    Try
                                        Dim scriptPath As String = FindPythonScript("visa_commander.py")
                                        If String.IsNullOrEmpty(scriptPath) Then
                                            SafeInvoke(Sub()
                                                           MessageBox.Show("Impossible de trouver visa_commander.py. Assurez-vous que le fichier est présent.",
                                                                           "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                       End Sub)
                                            Return
                                        End If

                                        Dim psi As New ProcessStartInfo()
                                        psi.FileName = "python.exe"
                                        psi.Arguments = $"""{scriptPath}"" ""{tempFile}"""
                                        psi.RedirectStandardOutput = True
                                        psi.RedirectStandardError = True
                                        psi.UseShellExecute = False
                                        psi.CreateNoWindow = True

                                        Using p As Process = Process.Start(psi)
                                            _pythonProcess = p

                                            Dim outputBuilder As New System.Text.StringBuilder()
                                            Dim errorBuilder As New System.Text.StringBuilder()

                                            AddHandler p.OutputDataReceived, Sub(sender, e)
                                                                                 If e.Data IsNot Nothing Then
                                                                                     outputBuilder.AppendLine(e.Data)
                                                                                 End If
                                                                             End Sub

                                            AddHandler p.ErrorDataReceived, Sub(sender, e)
                                                                                If e.Data IsNot Nothing Then
                                                                                    errorBuilder.AppendLine(e.Data)
                                                                                End If
                                                                            End Sub

                                            p.BeginOutputReadLine()
                                            p.BeginErrorReadLine()

                                            While Not p.WaitForExit(100)
                                                If token.IsCancellationRequested Then
                                                    Try
                                                        p.Kill()
                                                        p.WaitForExit(1000)
                                                    Catch
                                                        ' Ignore
                                                    End Try
                                                    HandleCancellation()
                                                    Return
                                                End If
                                            End While

                                            If token.IsCancellationRequested Then
                                                HandleCancellation()
                                                Return
                                            End If

                                            If p.ExitCode <> 0 AndAlso errorBuilder.Length > 0 Then
                                                SafeInvoke(Sub()
                                                               MessageBox.Show($"Erreur Python (code {p.ExitCode}) : {errorBuilder.ToString()}",
                                                                               "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                               row.Cells("colStatut").Value = "Erreur"
                                                           End Sub)
                                                Continue For
                                            End If

                                            Dim output As String = outputBuilder.ToString()
                                            If String.IsNullOrEmpty(output) Then
                                                SafeInvoke(Sub()
                                                               MessageBox.Show($"Aucune réponse du processus Python pour {numLabel}.",
                                                                               "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                               row.Cells("colStatut").Value = "Erreur"
                                                           End Sub)
                                                Continue For
                                            End If

                                            Using doc As JsonDocument = JsonDocument.Parse(output)
                                                Dim root = doc.RootElement

                                                If root.GetProperty("success").GetBoolean() Then
                                                    Dim resultsArray = root.GetProperty("results")
                                                    If resultsArray.GetArrayLength() > 0 Then
                                                        Dim singleResult = resultsArray(0)
                                                        singleResultJson = singleResult.GetRawText()
                                                        SafeInvoke(Sub() row.Cells("colStatut").Value = "OK ✔")
                                                    Else
                                                        SafeInvoke(Sub() row.Cells("colStatut").Value = "Aucun résultat")
                                                    End If
                                                Else
                                                    Dim errElement As JsonElement
                                                    Dim errMessage As String = "Erreur inconnue"
                                                    If root.TryGetProperty("error", errElement) Then
                                                        errMessage = errElement.GetString()
                                                    End If
                                                    SafeInvoke(Sub() row.Cells("colStatut").Value = $"Erreur : {errMessage}")
                                                End If
                                            End Using
                                        End Using
                                    Finally
                                        Try
                                            If File.Exists(tempFile) Then File.Delete(tempFile)
                                        Catch
                                            ' Ignore
                                        End Try
                                    End Try

                                    If singleResultJson IsNot Nothing Then
                                        allResultsJson.Add(singleResultJson)
                                    End If

                                    SafeInvoke(Sub() dgvImpulsions.Refresh())
                                    rowIndex += 1
                                Next

                                ' Combine all results and show ResultsForm - UI thread
                                If allResultsJson.Count > 0 Then
                                    Dim combinedJson As String = "[" & String.Join(",", allResultsJson) & "]"
                                    Using combinedDoc As JsonDocument = JsonDocument.Parse(combinedJson)
                                        ' Clone the root element so it stays alive after combinedDoc is disposed
                                        SafeInvoke(Sub()
                                                       Dim resultsForm As New ResultsForm(combinedDoc.RootElement.Clone())
                                                       resultsForm.ShowDialog()
                                                   End Sub)
                                    End Using
                                Else
                                    SafeInvoke(Sub()
                                                   MessageBox.Show("Aucun résultat n'a été obtenu pour cette acquisition.",
                                                                   "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                               End Sub)
                                End If

                            Catch ex As Exception
                                SafeInvoke(Sub()
                                               MessageBox.Show($"Erreur critique : {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                           End Sub)
                            Finally
                                _pythonProcess = Nothing
                                _cancelRequested = False

                                SafeInvoke(Sub()
                                               btnAcquerir.Enabled = True
                                               btnArreter.Enabled = False
                                           End Sub)
                            End Try
                        End Sub, token)
    End Function

    Private Sub UpdateProgress(progress As Integer)
        SafeInvoke(Sub()
                       Dim rowIndex As Integer = 0
                       For Each row As DataGridViewRow In dgvImpulsions.Rows
                           If row.IsNewRow Then Continue For
                           If rowIndex < progress Then
                               row.Cells("colStatut").Value = "OK ✔"
                           ElseIf rowIndex = progress Then
                               row.Cells("colStatut").Value = "En cours..."
                           Else
                               row.Cells("colStatut").Value = "En attente"
                           End If
                           rowIndex += 1
                       Next
                       dgvImpulsions.Refresh()
                   End Sub)
    End Sub

    Private Sub HandleCancellation()
        SafeInvoke(Sub()
                       For Each row As DataGridViewRow In dgvImpulsions.Rows
                           If row.IsNewRow Then Continue For
                           row.Cells("colStatut").Value = "Interrompu"
                       Next
                       dgvImpulsions.Refresh()
                   End Sub)

        Try
            If _pythonProcess IsNot Nothing AndAlso Not _pythonProcess.HasExited Then
                Dim cancelFile As String = Path.Combine(Path.GetTempPath(), "impulsion_cancel.flag")
                File.WriteAllText(cancelFile, "CANCEL")
            End If
        Catch
            ' Ignore
        End Try

        SafeInvoke(Sub()
                       MessageBox.Show("Acquisition interrompue.", "Interruption", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                   End Sub)
    End Sub

    Private Sub btnPrecedent_Click(sender As Object, e As EventArgs) Handles btnPrecedent.Click
        RaiseEvent PrecedentClicked(Me, EventArgs.Empty)
    End Sub

    Private Sub btnSuivant_Click(sender As Object, e As EventArgs) Handles btnSuivant.Click
        RaiseEvent SuivantClicked(Me, EventArgs.Empty)
    End Sub

    Private Async Sub btnArreter_Click(sender As Object, e As EventArgs) Handles btnArreter.Click
        btnArreter.Enabled = False

        _cancelRequested = True

        If _cancellationTokenSource IsNot Nothing Then
            _cancellationTokenSource.Cancel()
        End If

        Try
            If _pythonProcess IsNot Nothing AndAlso Not _pythonProcess.HasExited Then
                Dim cancelFile As String = Path.Combine(Path.GetTempPath(), "impulsion_cancel.flag")
                File.WriteAllText(cancelFile, "CANCEL")

                Await Task.Delay(500)

                If Not _pythonProcess.HasExited Then
                    _pythonProcess.Kill()
                    _pythonProcess.WaitForExit(1000)
                End If
            End If
        Catch
            ' Ignore
        End Try

        btnArreter.Enabled = True
    End Sub
End Class