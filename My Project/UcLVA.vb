Imports System.Drawing
Imports System.Windows.Forms
Imports System.Text.Json
Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Data.SQLite

Public Class UcLVA

    Public Event SuivantClicked As EventHandler
    Public Event PrecedentClicked As EventHandler
    Public Shared ModeleId As Integer

    ' Python message type constants (must match the Python script)
    Private Const MSG_SETUP_REFERENCE As String = "setup_reference"
    Private Const MSG_CHANGE_ATTENUATOR As String = "change_attenuator"
    Private Const MSG_ENTER_SCREEN_READ As String = "enter_screen_read"

    ' Hardware state
    Private isScopeConnected As Boolean = False
    Private isGenConnected As Boolean = False
    Private scopeResource As String = Nothing
    Private generatorResource As String = Nothing
    Private scopeIdn As String = Nothing
    Private generatorIdn As String = Nothing
    Private scopeConfig As JsonElement
    Private generatorConfig As JsonElement

    ' Python process state
    Private _pythonProcess As Process = Nothing
    Private _cancellationTokenSource As CancellationTokenSource = Nothing
    Private _streamWriter As StreamWriter = Nothing
    Private _streamReader As StreamReader = Nothing
    Private ReadOnly _errorLock As New Object()
    Private _currentGain As Double = 0

    Private Sub SafeInvoke(action As Action)
        If Me.IsHandleCreated AndAlso Not Me.IsDisposed AndAlso Me.InvokeRequired Then
            Me.Invoke(action)
        ElseIf Me.IsHandleCreated AndAlso Not Me.IsDisposed Then
            action()
        End If
    End Sub

    Public Sub New()
        InitializeComponent()
        AddHandler Me.Disposed, AddressOf UcLVA_Disposed
    End Sub

    Private Sub UcLVA_Disposed(sender As Object, e As EventArgs)
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

    Private Sub UcLVA_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        txtFreq.Text = "5.00"
        txtTension.Text = "0.40"

        _currentGain = LoadGainFromDatabase(ModeleId)

        UpdateTitle(_currentGain)
        VerifyConnections()
    End Sub

    Private Function LoadGainFromDatabase(modeleId As Integer) As Double
        Try
            Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
                conn.Open()

                Dim query As String =
                "SELECT gain FROM ModelesAppareils WHERE modele_id = @modeleId"

                Using cmd As New SQLiteCommand(query, conn)
                    cmd.Parameters.AddWithValue("@modeleId", modeleId)

                    Dim result = cmd.ExecuteScalar()

                    If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                        Return Convert.ToDouble(result)
                    End If
                End Using
            End Using

        Catch ex As Exception
            Debug.WriteLine($"Failed to load gain: {ex.Message}")
        End Try

        Return 0
    End Function

    Private Sub UpdateTitle(gain As Double)
        Label2.Text = If(gain > 0, $"LINÉARITÉ VERTICALE D'AFFICHAGE ({gain:F1} dB)", "LINÉARITÉ VERTICALE D'AFFICHAGE")
    End Sub

    ' ------------------------------------------------------------------
    ' DETECTION (EXACTLY LIKE UcLDG)
    ' ------------------------------------------------------------------
    Public Sub VerifyConnections()
        scopeResource = Nothing : generatorResource = Nothing : scopeIdn = Nothing : generatorIdn = Nothing
        scopeConfig = New JsonElement() : generatorConfig = New JsonElement()

        If VisaCacheHelper.HasValidCache() Then
            Dim cacheData = VisaCacheHelper.LoadFromCache()
            If cacheData.ValueKind <> JsonValueKind.Undefined Then
                Dim parsed As Boolean = ParseDetectionResults(cacheData)
                If parsed AndAlso isScopeConnected AndAlso isGenConnected Then
                    Debug.WriteLine("UcLVA: Utilisation du cache")
                    UpdateUIAfterDetection()
                    Return
                End If
            End If
        End If
        RunPythonDetection()
    End Sub

    Private Sub RunPythonDetection()
        Try
            Dim scriptPath As String = FindPythonScript("visa_checker.py")
            If String.IsNullOrEmpty(scriptPath) Then
                SafeInvoke(Sub() MessageBox.Show("Impossible de trouver visa_checker.py.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error))
                Return
            End If

            Dim psi As New ProcessStartInfo()
            psi.FileName = "python.exe" : psi.Arguments = $"""{scriptPath}"""
            psi.RedirectStandardOutput = True : psi.RedirectStandardError = True : psi.UseShellExecute = False : psi.CreateNoWindow = True

            Using p As Process = Process.Start(psi)
                Dim outputBuilder As New System.Text.StringBuilder() : Dim errorBuilder As New System.Text.StringBuilder()

                AddHandler p.OutputDataReceived,
    Sub(s, e)
        If e.Data IsNot Nothing Then
            outputBuilder.AppendLine(e.Data)
        End If
    End Sub

                AddHandler p.ErrorDataReceived,
                    Sub(s, e)
                        If e.Data IsNot Nothing Then
                            SyncLock _errorLock
                                errorBuilder.AppendLine(e.Data)
                            End SyncLock
                        End If
                    End Sub

                p.BeginOutputReadLine() : p.BeginErrorReadLine()

                If Not p.WaitForExit(30000) Then
                    Try : p.Kill(True) : Catch : End Try
                    Throw New Exception("Le processus Python a expiré (30 secondes).")
                End If

                Dim output As String = outputBuilder.ToString()
                Dim errOutput As String = ""
                SyncLock _errorLock : errOutput = errorBuilder.ToString() : End SyncLock

                If p.ExitCode <> 0 AndAlso Not String.IsNullOrEmpty(errOutput) Then
                    SafeInvoke(Sub() MessageBox.Show($"Erreur Python (code {p.ExitCode}) : {errOutput}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error))
                    Return
                End If

                If Not String.IsNullOrEmpty(output) Then
                    Using doc As JsonDocument = JsonDocument.Parse(output)
                        ParseDetectionResults(doc.RootElement)
                    End Using
                Else
                    SafeInvoke(Sub() MessageBox.Show("Aucune réponse du processus Python.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error))
                End If
            End Using
        Catch ex As Exception
            scopeResource = Nothing : generatorResource = Nothing : scopeIdn = Nothing : generatorIdn = Nothing
            scopeConfig = New JsonElement() : generatorConfig = New JsonElement()
            SafeInvoke(Sub() MessageBox.Show($"Erreur lors de la détection : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error))
        End Try
        UpdateUIAfterDetection()
    End Sub

    Private Function ParseDetectionResults(root As JsonElement) As Boolean
        Try
            Dim scopeVal As JsonElement
            If root.TryGetProperty("oscilloscope", scopeVal) AndAlso scopeVal.ValueKind = JsonValueKind.Array AndAlso scopeVal.GetArrayLength() > 0 Then
                Dim firstScope = scopeVal(0)
                Dim idnEl As JsonElement : Dim resEl As JsonElement : Dim configEl As JsonElement
                If firstScope.TryGetProperty("idn", idnEl) AndAlso idnEl.ValueKind = JsonValueKind.String Then scopeIdn = idnEl.GetString()
                If firstScope.TryGetProperty("resource", resEl) AndAlso resEl.ValueKind = JsonValueKind.String Then scopeResource = resEl.GetString()
                If firstScope.TryGetProperty("config", configEl) AndAlso configEl.ValueKind = JsonValueKind.Object Then scopeConfig = configEl.Clone()
            End If

            Dim genVal As JsonElement
            If root.TryGetProperty("generator", genVal) AndAlso genVal.ValueKind = JsonValueKind.Array AndAlso genVal.GetArrayLength() > 0 Then
                Dim firstGen = genVal(0)
                Dim idnEl As JsonElement : Dim resEl As JsonElement : Dim configEl As JsonElement
                If firstGen.TryGetProperty("idn", idnEl) AndAlso idnEl.ValueKind = JsonValueKind.String Then generatorIdn = idnEl.GetString()
                If firstGen.TryGetProperty("resource", resEl) AndAlso resEl.ValueKind = JsonValueKind.String Then generatorResource = resEl.GetString()
                If firstGen.TryGetProperty("config", configEl) AndAlso configEl.ValueKind = JsonValueKind.Object Then generatorConfig = configEl.Clone()
            End If

            isScopeConnected = Not String.IsNullOrEmpty(scopeIdn) AndAlso Not String.IsNullOrEmpty(scopeResource)
            isGenConnected = Not String.IsNullOrEmpty(generatorIdn) AndAlso Not String.IsNullOrEmpty(generatorResource)
            Return True
        Catch ex As Exception
            Debug.WriteLine($"ParseDetectionResults error: {ex.Message}") : Return False
        End Try
    End Function

    Private Sub UpdateUIAfterDetection()
        SafeInvoke(Sub()
                       If Not isScopeConnected OrElse Not isGenConnected Then
                           Dim missing As String = ""
                           If Not isScopeConnected Then missing &= "• Oscilloscope" & vbCrLf
                           If Not isGenConnected Then missing &= "• Générateur de fonctions" & vbCrLf
                           MessageBox.Show($"Erreur de connexion hardware !{vbCrLf}{vbCrLf}Équipement(s) non détecté(s) :{vbCrLf}{missing}{vbCrLf}Veuillez vérifier vos câbles USB/LAN et re-scanner.", "Connexion Hardware", MessageBoxButtons.OK, MessageBoxIcon.Error)
                       End If
                       UpdateContextMenu()
                       btnAcquerir.Enabled = isScopeConnected AndAlso isGenConnected
                   End Sub)
    End Sub

    Private Function FindPythonScript(scriptName As String) As String
        Dim exeFolder As String = Application.StartupPath
        Dim exePathCheck As String = Path.Combine(exeFolder, scriptName)
        If File.Exists(exePathCheck) Then Return exePathCheck
        Dim projectRoot As String = Nothing
        Try : projectRoot = Directory.GetParent(Application.StartupPath).Parent.Parent.FullName : Catch : End Try
        If Not String.IsNullOrEmpty(projectRoot) Then
            Dim possiblePath As String = Path.Combine(projectRoot, "My Project", scriptName)
            If File.Exists(possiblePath) Then Return possiblePath
            possiblePath = Path.Combine(projectRoot, scriptName)
            If File.Exists(possiblePath) Then Return possiblePath
        End If
        Return Nothing
    End Function

    Private Sub UpdateContextMenu()
        cmsAppareils.Items.Clear()
        cmsAppareils.Items.Add(New ToolStripMenuItem("Équipements Détectés (*IDN?) :") With {.Enabled = False})
        cmsAppareils.Items.Add(New ToolStripSeparator())
        cmsAppareils.Items.Add(New ToolStripMenuItem(If(isScopeConnected, $"✔ Scope: {scopeIdn}", "❌ Scope: Non connecté")) With {.Enabled = False})
        cmsAppareils.Items.Add(New ToolStripMenuItem(If(isGenConnected, $"✔ Générateur: {generatorIdn}", "❌ Générateur: Non connecté")) With {.Enabled = False})
        cmsAppareils.Items.Add(New ToolStripSeparator())
        Dim btnRefresh As New ToolStripMenuItem("🔄 Re-scanner les appareils")
        AddHandler btnRefresh.Click,
    Sub(s, args)
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

    ' ------------------------------------------------------------------
    ' ACQUISITION (LVA SPECIFIC)
    ' ------------------------------------------------------------------
    Private Async Sub btnAcquerir_Click(sender As Object, e As EventArgs) Handles btnAcquerir.Click
        If String.IsNullOrEmpty(scopeResource) OrElse String.IsNullOrEmpty(generatorResource) Then
            MessageBox.Show("Veuillez re-scanner les appareils.", "Connexion Hardware", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        Dim freq As Double : Dim tension As Double
        If Not Double.TryParse(txtFreq.Text, NumberStyles.Any, CultureInfo.InvariantCulture, freq) Then
            MessageBox.Show("La fréquence doit être un nombre valide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error) : Return
        End If
        If Not Double.TryParse(txtTension.Text, NumberStyles.Any, CultureInfo.InvariantCulture, tension) Then
            MessageBox.Show("La tension Vcc doit être un nombre valide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error) : Return
        End If

        btnAcquerir.Enabled = False : btnArreter.Enabled = True : txtFreq.Enabled = False : txtTension.Enabled = False
        _cancellationTokenSource = New CancellationTokenSource()

        Try
            Await RunLVAacquisition(_cancellationTokenSource.Token, freq, tension)
        Catch ex As Exception
            Dim cancelled As Boolean = _cancellationTokenSource IsNot Nothing AndAlso _cancellationTokenSource.IsCancellationRequested
            If Not cancelled Then SafeInvoke(Sub() MessageBox.Show($"Erreur lors de l'acquisition : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error))
        Finally
            btnAcquerir.Enabled = True : btnArreter.Enabled = False : txtFreq.Enabled = True : txtTension.Enabled = True
            If _cancellationTokenSource IsNot Nothing Then _cancellationTokenSource.Dispose() : _cancellationTokenSource = Nothing
        End Try
    End Sub

    Private Async Function RunLVAacquisition(token As CancellationToken, freq As Double, tension As Double) As Task
        Dim errorBuilder As New System.Text.StringBuilder()
        Try
            Dim scriptPath As String = FindPythonScript("lva_measurement.py")
            If String.IsNullOrEmpty(scriptPath) Then
                SafeInvoke(Sub() MessageBox.Show("Impossible de trouver lva_measurement.py.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error))
                Return
            End If

            Dim psi As New ProcessStartInfo()
            psi.FileName = "python.exe" : psi.Arguments = $"""{scriptPath}"""
            psi.RedirectStandardInput = True : psi.RedirectStandardOutput = True : psi.RedirectStandardError = True
            psi.UseShellExecute = False : psi.CreateNoWindow = True

            Using p As Process = Process.Start(psi)
                _pythonProcess = p : _streamWriter = p.StandardInput : _streamReader = p.StandardOutput

                AddHandler p.ErrorDataReceived,
    Sub(s, e)
        If e.Data IsNot Nothing Then
            SyncLock _errorLock
                errorBuilder.AppendLine(e.Data)
            End SyncLock
        End If
    End Sub
                p.BeginErrorReadLine()

                If p.HasExited Then
                    Dim errMsg As String = "" : SyncLock _errorLock : errMsg = errorBuilder.ToString() : End SyncLock
                    Throw New Exception($"Le processus Python s'est arrêté prématurément : {errMsg}")
                End If

                ' 1. SEND PAYLOAD
                Dim configPayload As New Dictionary(Of String, Object) From {
                    {"scope_resource", scopeResource}, {"scope_idn", scopeIdn},
                    {"generator_resource", generatorResource}, {"generator_idn", generatorIdn},
                    {"frequency", freq}, {"voltage", tension}, {"gain", _currentGain}
                }
                If scopeConfig.ValueKind <> JsonValueKind.Undefined Then configPayload.Add("scope_config", scopeConfig)
                If generatorConfig.ValueKind <> JsonValueKind.Undefined Then configPayload.Add("generator_config", generatorConfig)
                Await _streamWriter.WriteLineAsync(JsonSerializer.Serialize(configPayload))
                Await _streamWriter.FlushAsync()

                ' 2. READ FIRST PROMPT
                Dim readTask = _streamReader.ReadLineAsync()
                Dim completed = Await Task.WhenAny(readTask, Task.Delay(10000, token))
                If token.IsCancellationRequested Then HandleCancellation() : Return
                If completed IsNot readTask Then Throw New TimeoutException("Python n'a pas répondu.")

                Dim responseLine As String = Await readTask
                If String.IsNullOrEmpty(responseLine) Then Throw New Exception("Python ne répond pas.")

                Dim currentState As String = ""
                Dim promptMessage As String = ""
                Using doc As JsonDocument = JsonDocument.Parse(responseLine)
                    Dim typeEl As JsonElement
                    If doc.RootElement.TryGetProperty("type", typeEl) Then
                        currentState = typeEl.GetString()
                        Dim msgEl As JsonElement
                        If doc.RootElement.TryGetProperty("message", msgEl) AndAlso msgEl.ValueKind = JsonValueKind.String Then
                            promptMessage = msgEl.GetString()
                        End If
                    Else
                        SafeInvoke(Sub() ShowResults(responseLine)) : Return
                    End If
                End Using

                Dim input As String = "" : Dim userResponse As String = ""

                ' 3. LVA MESSAGE LOOP
                While True
                    If token.IsCancellationRequested Then HandleCancellation() : Return
                    If p.HasExited Then
                        Dim errMsg As String = "" : SyncLock _errorLock : errMsg = errorBuilder.ToString() : End SyncLock
                        Throw New Exception($"Le processus Python s'est arrêté : {errMsg}")
                    End If

                    userResponse = ""

                    If currentState = MSG_SETUP_REFERENCE Then
                        Dim dialogResult As DialogResult = DialogResult.Cancel
                        SafeInvoke(Sub() dialogResult = MessageBox.Show(promptMessage, "Réglage de référence", MessageBoxButtons.OKCancel, MessageBoxIcon.Information))
                        userResponse = If(dialogResult = DialogResult.OK, "confirm", "cancel")

                    ElseIf currentState = MSG_CHANGE_ATTENUATOR Then
                        Dim dialogResult As DialogResult = DialogResult.Cancel
                        SafeInvoke(Sub() dialogResult = MessageBox.Show(promptMessage, "Atténuateur", MessageBoxButtons.OKCancel, MessageBoxIcon.Information))
                        userResponse = If(dialogResult = DialogResult.OK, "confirm", "cancel")

                    ElseIf currentState = MSG_ENTER_SCREEN_READ Then
                        input = ""
                        SafeInvoke(Sub() input = InputBox(promptMessage, "Lecture écran", "100"))
                        If input = "" Then
                            userResponse = "cancel"
                        Else
                            Dim parsedInput As Double = 0
                            If Double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, parsedInput) Then
                                userResponse = "screen_read"
                            Else
                                MessageBox.Show("Valeur invalide. Veuillez entrer un nombre.")
                                Continue While
                            End If
                        End If
                    Else
                        Throw New Exception($"Message Python inattendu : {currentState}")
                    End If

                    If token.IsCancellationRequested Then HandleCancellation() : Return

                    ' Send response
                    Dim responseDict As New Dictionary(Of String, Object) From {{"action", userResponse}}
                    If userResponse = "screen_read" Then responseDict.Add("value", Double.Parse(input, NumberStyles.Any, CultureInfo.InvariantCulture))
                    Await _streamWriter.WriteLineAsync(JsonSerializer.Serialize(responseDict))
                    Await _streamWriter.FlushAsync()

                    ' Read next
                    readTask = _streamReader.ReadLineAsync()
                    completed = Await Task.WhenAny(readTask, Task.Delay(10000, token))
                    If token.IsCancellationRequested Then HandleCancellation() : Return
                    If completed IsNot readTask Then Throw New TimeoutException("Python n'a pas répondu.")

                    responseLine = Await readTask
                    If String.IsNullOrEmpty(responseLine) Then Throw New Exception("Pas de réponse de Python.")
                    Using doc As JsonDocument = JsonDocument.Parse(responseLine)
                        Dim typeEl As JsonElement
                        If doc.RootElement.TryGetProperty("type", typeEl) Then
                            currentState = typeEl.GetString()
                            Dim msgEl As JsonElement
                            If doc.RootElement.TryGetProperty("message", msgEl) AndAlso msgEl.ValueKind = JsonValueKind.String Then
                                promptMessage = msgEl.GetString()
                            End If
                        Else
                            SafeInvoke(Sub() ShowResults(responseLine))
                            Exit While
                        End If
                    End Using
                End While

                ' 4. CLEANUP
                Try
                    If Not p.WaitForExit(5000) Then p.Kill(True) : p.WaitForExit()
                Catch ex As Exception
                    Debug.WriteLine($"Process cleanup error: {ex.Message}")
                End Try
            End Using
        Catch ex As Exception
            If token.IsCancellationRequested Then Return
            Dim stderrText As String = ""
            SyncLock _errorLock
                stderrText = errorBuilder.ToString()
            End SyncLock
            If Not String.IsNullOrWhiteSpace(stderrText) Then
                Throw New Exception($"{ex.Message}" & vbCrLf & vbCrLf & "Sortie Python (stderr) :" & vbCrLf & stderrText, ex)
            End If
            Throw
        Finally
            _pythonProcess = Nothing : _streamWriter = Nothing : _streamReader = Nothing
        End Try
    End Function

    Private Sub HandleCancellation()
        SafeInvoke(Sub() MessageBox.Show("Acquisition interrompue.", "Interruption", MessageBoxButtons.OK, MessageBoxIcon.Warning))
    End Sub

    ' ------------------------------------------------------------------
    ' LVA RESULT PARSING
    ' ------------------------------------------------------------------
    Public Class LvaMeasurementResult
        Public Property Success As Boolean = False
        Public Property Results As List(Of LvaResult) = New List(Of LvaResult)()
        Public Property Frequency As Double = 0
        Public Property ReferenceVoltage As Double = 0
        Public Property MaxError As Double = 0
        Public Property InTolerance As Boolean = False
        Public Property ErrorMessage As String = ""
    End Class

    Private Sub ShowResults(json As String)
        Dim parsed = ParseLVAResults(json)
        If parsed.Success Then
            Using frm As New LvaResultsForm(parsed.Results, parsed.Frequency, parsed.ReferenceVoltage, parsed.MaxError, parsed.InTolerance, _currentGain)
                frm.ShowDialog()
            End Using
        Else
            MessageBox.Show($"Erreur de mesure : {parsed.ErrorMessage}")
        End If
    End Sub

    Private Function ParseLVAResults(json As String) As LvaMeasurementResult
        Dim result As New LvaMeasurementResult()
        Try
            Using doc As JsonDocument = JsonDocument.Parse(json)
                Dim root = doc.RootElement
                If Not root.GetProperty("success").GetBoolean() Then
                    Dim errEl As JsonElement
                    If root.TryGetProperty("error", errEl) Then result.ErrorMessage = errEl.GetString()
                    Return result
                End If
                result.Success = True
                result.ReferenceVoltage = root.GetProperty("reference_voltage").GetDouble()
                result.Frequency = root.GetProperty("frequency").GetDouble()
                result.MaxError = root.GetProperty("max_error").GetDouble()
                result.InTolerance = root.GetProperty("all_in_tolerance").GetBoolean()

                For Each item In root.GetProperty("results").EnumerateArray()
                    result.Results.Add(New LvaResult With {
                        .Db = item.GetProperty("db").GetDouble(),
                        .MeasuredVoltage = item.GetProperty("measured_voltage").GetDouble(),
                        .IsoTarget = item.GetProperty("iso_target").GetDouble(),
                        .CalculatedPercent = item.GetProperty("calculated_percent").GetDouble(),
                        .ScreenRead = item.GetProperty("screen_read").GetDouble(),
                        .ScreenError = item.GetProperty("screen_error").GetDouble(),
                        .InTolerance = item.GetProperty("in_tolerance").GetBoolean()
                    })
                Next
            End Using
        Catch ex As Exception
            result.ErrorMessage = $"JSON Parsing error: {ex.Message}"
        End Try
        Return result
    End Function

    Private Sub btnArreter_Click(sender As Object, e As EventArgs) Handles btnArreter.Click
        btnArreter.Enabled = False
        If _cancellationTokenSource IsNot Nothing Then _cancellationTokenSource.Cancel()
        Dim proc = _pythonProcess : Dim writer = _streamWriter
        Try
            If proc IsNot Nothing AndAlso Not proc.HasExited AndAlso writer IsNot Nothing Then
                Try
                    writer.WriteLine(JsonSerializer.Serialize(New Dictionary(Of String, Object) From {{"action", "cancel"}}))
                    writer.Flush()
                Catch : End Try
                If Not proc.WaitForExit(2000) Then
                    Try : proc.Kill(True) : proc.WaitForExit(1000) : Catch : End Try
                End If
            ElseIf proc IsNot Nothing AndAlso Not proc.HasExited Then
                proc.Kill(True) : proc.WaitForExit(1000)
            End If
        Catch ex As Exception
            Debug.WriteLine($"Stop error: {ex.Message}")
        End Try
    End Sub

    Private Sub btnSuivant_Click(sender As Object, e As EventArgs) Handles BtnSuivant.Click
        RaiseEvent SuivantClicked(Me, EventArgs.Empty)
    End Sub
End Class