Imports System.Drawing
Imports System.Windows.Forms
Imports System.Text.Json
Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Globalization

Public Class UcLDG

    Public Event SuivantClicked As EventHandler
    Public Event PrecedentClicked As EventHandler

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
    Private _resultsForm As LineariteGainResultsForm = Nothing

    Private Sub SafeInvoke(action As Action)
        If Me.IsHandleCreated AndAlso Not Me.IsDisposed AndAlso Me.InvokeRequired Then
            Me.Invoke(action)
        ElseIf Me.IsHandleCreated AndAlso Not Me.IsDisposed Then
            action()
        End If
    End Sub

    Public Sub New()
        InitializeComponent()
        AddHandler Me.Disposed, AddressOf UcLDG_Disposed
    End Sub

    Private Sub UcLDG_Disposed(sender As Object, e As EventArgs)
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

    Private Sub UcLDG_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Set default values
        txtFreq.Text = "5.00"
        txtTension.Text = "0.40"

        VerifyConnections()
    End Sub

    Public Sub VerifyConnections()
        ' Réinitialiser les adresses
        scopeResource = Nothing
        generatorResource = Nothing
        scopeIdn = Nothing
        generatorIdn = Nothing
        scopeConfig = Nothing
        generatorConfig = Nothing

        ' 1. ESSAYER LE CACHE D'ABORD
        If VisaCacheHelper.HasValidCache() Then
            Dim cacheData = VisaCacheHelper.LoadFromCache()
            If cacheData.ValueKind <> JsonValueKind.Undefined Then
                Dim parsed As Boolean = ParseDetectionResults(cacheData)
                If parsed AndAlso isScopeConnected AndAlso isGenConnected Then
                    Debug.WriteLine("UcLDG: Utilisation du cache pour la détection hardware")
                    UpdateUIAfterDetection()
                    Return
                End If
            End If
        End If

        ' 2. PAS DE CACHE VALIDE → EXÉCUTER PYTHON
        Debug.WriteLine("UcLDG: Pas de cache valide, exécution de visa_checker.py...")
        RunPythonDetection()
    End Sub

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
                        Dim root = doc.RootElement
                        ParseDetectionResults(root)
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

    Private Function FindPythonScript(scriptName As String) As String
        Dim exeFolder As String = Application.StartupPath
        Dim exePathCheck As String = Path.Combine(exeFolder, scriptName)
        If File.Exists(exePathCheck) Then
            Return exePathCheck
        End If

        Dim projectRoot As String = Nothing
        Try
            Dim exePath As String = Application.StartupPath
            projectRoot = Directory.GetParent(exePath).Parent.Parent.FullName
        Catch
        End Try

        If Not String.IsNullOrEmpty(projectRoot) Then
            Dim possiblePath As String = Path.Combine(projectRoot, "My Project", scriptName)
            If File.Exists(possiblePath) Then
                Return possiblePath
            End If

            possiblePath = Path.Combine(projectRoot, scriptName)
            If File.Exists(possiblePath) Then
                Return possiblePath
            End If
        End If

        Return Nothing
    End Function

    Private Sub UpdateContextMenu()
        cmsAppareils.Items.Clear()

        Dim lblHeader As New ToolStripMenuItem("Équipements Détectés (*IDN?) :")
        lblHeader.Enabled = False
        cmsAppareils.Items.Add(lblHeader)
        cmsAppareils.Items.Add(New ToolStripSeparator())

        Dim oscStatus As String = If(isScopeConnected, $"✔ Scope: {scopeIdn}", "❌ Scope: Non connecté")
        Dim oscItem As New ToolStripMenuItem(oscStatus)
        oscItem.Enabled = False
        cmsAppareils.Items.Add(oscItem)

        Dim genStatus As String = If(isGenConnected, $"✔ Générateur: {generatorIdn}", "❌ Générateur: Non connecté")
        Dim genItem As New ToolStripMenuItem(genStatus)
        genItem.Enabled = False
        cmsAppareils.Items.Add(genItem)

        cmsAppareils.Items.Add(New ToolStripSeparator())

        Dim btnRefresh As New ToolStripMenuItem("🔄 Re-scanner les appareils")
        AddHandler btnRefresh.Click, Sub(s, args)
                                         ' Clear cache and force fresh detection
                                         VisaCacheHelper.ClearCache()
                                         VerifyConnections()
                                     End Sub
        cmsAppareils.Items.Add(btnRefresh)
    End Sub

    Private Sub btnThreeDots_Click(sender As Object, e As EventArgs) Handles btnThreeDots.Click
        cmsAppareils.Show(btnThreeDots, New Point(0, btnThreeDots.Height))
    End Sub

    Private Sub btnPrecedent_Click(sender As Object, e As EventArgs) Handles BtnPrecedent.Click
        RaiseEvent PrecedentClicked(Me, EventArgs.Empty)
    End Sub

    Private Async Sub btnAcquerir_Click(sender As Object, e As EventArgs) Handles btnAcquerir.Click
        If String.IsNullOrEmpty(scopeResource) OrElse String.IsNullOrEmpty(generatorResource) Then
            MessageBox.Show("Veuillez re-scanner les appareils. Oscilloscope et générateur requis.",
                            "Connexion Hardware", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        If String.IsNullOrEmpty(txtFreq.Text) OrElse String.IsNullOrEmpty(txtTension.Text) Then
            MessageBox.Show("Veuillez saisir la fréquence et la tension Vcc.",
                            "Saisie incomplète", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim freq As Double
        Dim tension As Double

        Dim freqText As String = txtFreq.Text.Trim().Replace(","c, "."c)
        Dim tensionText As String = txtTension.Text.Trim().Replace(","c, "."c)

        If Not Double.TryParse(freqText,
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       freq) Then

            MessageBox.Show("La fréquence doit être un nombre valide (ex: 5.1 ou 5,1).",
                    "Format invalide",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)
            Return
        End If

        If Not Double.TryParse(tensionText,
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       tension) Then

            MessageBox.Show("La tension Vcc doit être un nombre valide (ex: 0.40 ou 0,40).",
                    "Format invalide",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)
            Return
        End If

        btnAcquerir.Enabled = False
        btnArreter.Enabled = True

        _cancellationTokenSource = New CancellationTokenSource()

        Try
            Await RunAcquisition(_cancellationTokenSource.Token, freq, tension)
        Catch ex As Exception
            Dim cancelled As Boolean = _cancellationTokenSource IsNot Nothing AndAlso _cancellationTokenSource.IsCancellationRequested
            If Not cancelled Then
                SafeInvoke(Sub()
                               MessageBox.Show($"Erreur lors de l'acquisition : {ex.Message}",
                                               "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                           End Sub)
            End If
        Finally
            btnAcquerir.Enabled = True
            btnArreter.Enabled = False

            If _cancellationTokenSource IsNot Nothing Then
                _cancellationTokenSource.Dispose()
                _cancellationTokenSource = Nothing
            End If
        End Try
    End Sub

    Private Async Function RunAcquisition(token As CancellationToken, freq As Double, tension As Double) As Task
        Dim resultsArray As JsonElement

        Try
            _requestId = 0

            Dim scriptPath As String = FindPythonScript("linearite_gain.py")
            If String.IsNullOrEmpty(scriptPath) Then
                SafeInvoke(Sub()
                               MessageBox.Show("Impossible de trouver linearite_gain.py.",
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

                Dim errorBuilder As New System.Text.StringBuilder()
                AddHandler p.ErrorDataReceived, Sub(sender, e)
                                                    If e.Data IsNot Nothing Then
                                                        SyncLock _errorLock
                                                            errorBuilder.AppendLine(e.Data)
                                                        End SyncLock
                                                    End If
                                                End Sub
                p.BeginErrorReadLine()

                If p.HasExited Then
                    Dim errMsg As String = ""
                    SyncLock _errorLock
                        errMsg = errorBuilder.ToString()
                    End SyncLock
                    Throw New Exception($"Le processus Python s'est arrêté prématurément : {errMsg}")
                End If

                Dim configPayload As New Dictionary(Of String, Object) From {
                    {"scope_resource", scopeResource},
                    {"scope_idn", scopeIdn},
                    {"generator_resource", generatorResource},
                    {"generator_idn", generatorIdn},
                    {"frequency_mhz", freq},
                    {"tension_vcc", tension}
                }

                If scopeConfig.ValueKind <> JsonValueKind.Undefined Then
                    configPayload.Add("scope_config", scopeConfig)
                End If

                If generatorConfig.ValueKind <> JsonValueKind.Undefined Then
                    configPayload.Add("generator_config", generatorConfig)
                End If

                Await _streamWriter.WriteLineAsync(JsonSerializer.Serialize(configPayload))
                Await _streamWriter.FlushAsync()

                If p.HasExited Then
                    Dim errMsg As String = ""
                    SyncLock _errorLock
                        errMsg = errorBuilder.ToString()
                    End SyncLock
                    Throw New Exception($"Le processus Python s'est arrêté après la configuration : {errMsg}")
                End If

                Dim readTask = _streamReader.ReadLineAsync()
                Dim completed = Await Task.WhenAny(readTask, Task.Delay(10000, token))

                If token.IsCancellationRequested Then
                    HandleCancellation()
                    Return
                End If

                If completed IsNot readTask Then
                    Throw New TimeoutException("Python n'a pas répondu dans les 10 secondes.")
                End If

                Dim readyLine As String = Await readTask
                If String.IsNullOrEmpty(readyLine) Then
                    Throw New Exception("Python ne répond pas.")
                End If

                Using doc As JsonDocument = JsonDocument.Parse(readyLine)
                    If doc.RootElement.GetProperty("status").GetString() <> "ready" Then
                        Throw New Exception("Python n'est pas prêt.")
                    End If
                End Using

                ' Ouvrir la fenêtre de résultats dès le début de l'acquisition,
                ' vide, et la remplir mesure par mesure au fur et à mesure
                ' (au lieu d'attendre la fin pour tout afficher d'un coup).
                SafeInvoke(Sub()
                               _resultsForm = New LineariteGainResultsForm()
                               _resultsForm.Owner = Me.FindForm()
                               _resultsForm.Show()
                               _resultsForm.BringToFront()
                           End Sub)

                Dim gainSteps As Double() = {10.0, 15.0, 16.0, 17.0, 20.0, 25.0, 26.0, 27.0, 30.0, 35.0,
                                             36.0, 37.0, 40.0, 45.0, 46.0, 47.0, 50.0, 55.0, 56.0, 57.0, 60.0}

                For Each gain In gainSteps
                    If token.IsCancellationRequested Then
                        HandleCancellation()
                        Return
                    End If

                    If p.HasExited Then
                        Dim errMsg As String = ""
                        SyncLock _errorLock
                            errMsg = errorBuilder.ToString()
                        End SyncLock
                        Throw New Exception($"Le processus Python s'est arrêté pendant les mesures : {errMsg}")
                    End If

                    Dim promptResult As DialogResult = DialogResult.Cancel
                    SafeInvoke(Sub()
                                   promptResult = MessageBox.Show(
                                       $"Réglez le gain sur : {gain} dB" & vbCrLf &
                                       $"Atténuateur externe : {gain - 10} dB" & vbCrLf & vbCrLf &
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

                    Dim measureCmd As New Dictionary(Of String, Object) From {
                        {"command", "measure"},
                        {"gain", gain},
                        {"request_id", _requestId}
                    }
                    Dim sentRequestId As Integer = _requestId
                    _requestId += 1
                    Await _streamWriter.WriteLineAsync(JsonSerializer.Serialize(measureCmd))
                    Await _streamWriter.FlushAsync()

                    readTask = _streamReader.ReadLineAsync()
                    completed = Await Task.WhenAny(readTask, Task.Delay(10000, token))

                    If token.IsCancellationRequested Then
                        HandleCancellation()
                        Return
                    End If

                    If completed IsNot readTask Then
                        Throw New TimeoutException($"Pas de réponse de Python pour le gain {gain} dB (10 secondes).")
                    End If

                    Dim responseLine As String = Await readTask
                    If String.IsNullOrEmpty(responseLine) Then
                        Throw New Exception($"Pas de réponse de Python pour le gain {gain} dB.")
                    End If

                    Using doc As JsonDocument = JsonDocument.Parse(responseLine)
                        Dim reqElement As JsonElement
                        If doc.RootElement.TryGetProperty("request_id", reqElement) Then
                            Dim returnedId As Integer = reqElement.GetInt32()
                            If returnedId <> sentRequestId Then
                                Throw New Exception($"Request ID mismatch: sent {sentRequestId}, got {returnedId}")
                            End If
                        End If

                        If doc.RootElement.GetProperty("success").GetBoolean() Then
                            ' Ligne remplie en direct dans la fenêtre de résultats,
                            ' pendant que l'acquisition continue.
                            Dim resultClone As JsonElement = doc.RootElement.Clone()
                            SafeInvoke(Sub() _resultsForm?.AddResult(resultClone))
                        Else
                            Dim errMsg As String = "Erreur inconnue"
                            Dim errEl As JsonElement
                            If doc.RootElement.TryGetProperty("error", errEl) AndAlso errEl.ValueKind = JsonValueKind.String Then
                                errMsg = errEl.GetString()
                            End If
                            Dim errClone As JsonElement = doc.RootElement.Clone()
                            SafeInvoke(Sub()
                                           _resultsForm?.AddResult(errClone)
                                           MessageBox.Show($"Erreur à {gain} dB : {errMsg}",
                                                           "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                       End Sub)
                            Exit For
                        End If
                    End Using
                Next

                If p.HasExited Then
                    Dim errMsg As String = ""
                    SyncLock _errorLock
                        errMsg = errorBuilder.ToString()
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

                readTask = _streamReader.ReadLineAsync()
                completed = Await Task.WhenAny(readTask, Task.Delay(15000, token))

                If token.IsCancellationRequested Then
                    HandleCancellation()
                    Return
                End If

                If completed IsNot readTask Then
                    Throw New TimeoutException("Python n'a pas répondu à la commande 'complete' (15 secondes).")
                End If

                Dim finalLine As String = Await readTask
                If Not String.IsNullOrEmpty(finalLine) Then
                    Using doc As JsonDocument = JsonDocument.Parse(finalLine)
                        Dim reqElement As JsonElement
                        If doc.RootElement.TryGetProperty("request_id", reqElement) Then
                            Dim returnedId As Integer = reqElement.GetInt32()
                            If returnedId <> sentCompleteId Then
                                Throw New Exception($"Request ID mismatch on complete: sent {sentCompleteId}, got {returnedId}")
                            End If
                        End If

                        If doc.RootElement.GetProperty("success").GetBoolean() Then
                            resultsArray = doc.RootElement.GetProperty("results")
                            ' Les lignes ont déjà été ajoutées en direct pendant la
                            ' boucle ci-dessus ; la ligne "Ecarts maxi" est ajoutée
                            ' automatiquement dans le bloc Finally, quelle que soit
                            ' l'issue de l'acquisition.
                            If resultsArray.GetArrayLength() = 0 Then
                                SafeInvoke(Sub()
                                               MessageBox.Show("Aucun résultat obtenu.",
                                                               "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                           End Sub)
                            End If
                        Else
                            Dim errMsg As String = "Erreur inconnue"
                            Dim errEl As JsonElement
                            If doc.RootElement.TryGetProperty("error", errEl) AndAlso errEl.ValueKind = JsonValueKind.String Then
                                errMsg = errEl.GetString()
                            End If
                            SafeInvoke(Sub()
                                           MessageBox.Show($"Erreur : {errMsg}",
                                                           "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                       End Sub)
                        End If
                    End Using
                End If

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
            If token.IsCancellationRequested Then
                Return
            End If
            Throw
        Catch ex As ObjectDisposedException
            If token.IsCancellationRequested Then
                Return
            End If
            Throw
        Catch ex As TimeoutException
            Throw
        Catch ex As Exception
            Throw
        Finally
            _pythonProcess = Nothing
            _streamWriter = Nothing
            _streamReader = Nothing
            ' Que l'acquisition se termine normalement, soit annulée, soit en
            ' erreur : on clôt le tableau déjà rempli avec la ligne "Ecarts
            ' maxi" à partir de ce qui a été mesuré jusque-là, puis on oublie
            ' la référence pour que la prochaine acquisition ouvre une
            ' nouvelle fenêtre plutôt que de continuer à remplir l'ancienne.
            Dim formToFinalize = _resultsForm
            _resultsForm = Nothing
            If formToFinalize IsNot Nothing Then
                SafeInvoke(Sub() formToFinalize.FinalizeSummary())
            End If
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
                    Dim cancelCmd As New Dictionary(Of String, Object) From {
                        {"command", "cancel"},
                        {"request_id", -1}
                    }
                    writer.WriteLine(JsonSerializer.Serialize(cancelCmd))
                    writer.Flush()
                Catch
                    ' Pipe may already be broken/closing -- fall through to Kill()
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

    Private Sub btnSuivant_Click(sender As Object, e As EventArgs) Handles BtnSuivant.Click
        RaiseEvent SuivantClicked(Me, EventArgs.Empty)
    End Sub


End Class