Imports System.Data
Imports System.Data.SQLite
Imports System.Globalization

Public Class FormGestionAppareils

    ' ==========================================
    ' BULLETPROOF DECIMAL PARSER
    ' Treats both '.' and ',' as decimals; prevents '0.3' -> '3' bug
    ' ==========================================
    Private Function ParseDoubleSafe(input As String) As Double
        If String.IsNullOrWhiteSpace(input) Then Return 0.0

        ' Remove any spaces and convert comma to dot for unified parsing
        Dim clean As String = input.Trim().Replace(" "c, "").Replace(","c, "."c)

        Dim result As Double = 0.0

        ' Strictly parse using InvariantCulture with AllowDecimalPoint ONLY (no thousands separators)
        If Double.TryParse(clean, NumberStyles.AllowDecimalPoint Or NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, result) Then
            Return result
        End If

        Return 0.0
    End Function

    Private Sub FormGestionAppareils_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ChargerFabricants()
    End Sub

    ' ==========================================
    ' HELPER: SAFELY EXTRACT ID FROM COMBOBOX
    ' ==========================================
    Private Function GetSelectedId(cmb As ComboBox) As Integer?
        If cmb.SelectedValue Is Nothing Then Return Nothing

        If TypeOf cmb.SelectedValue Is Integer Then
            Return Convert.ToInt32(cmb.SelectedValue)
        End If

        If TypeOf cmb.SelectedValue Is DataRowView Then
            Dim drv As DataRowView = CType(cmb.SelectedValue, DataRowView)
            Return Convert.ToInt32(drv(cmb.ValueMember))
        End If

        Dim parsedId As Integer
        If Integer.TryParse(cmb.SelectedValue.ToString(), parsedId) Then
            Return parsedId
        End If

        Return Nothing
    End Function

    ' ==========================================
    ' HELPER: FORMAT FILTER RANGE OR DEFAULT "LB"
    ' ==========================================
    Private Function GetValeurFiltre() As String
        Dim minVal As String = txtFiltreMin.Text.Trim()
        Dim maxVal As String = txtFiltreMax.Text.Trim()

        If String.IsNullOrEmpty(minVal) AndAlso String.IsNullOrEmpty(maxVal) Then
            Return "LB"
        End If

        If String.IsNullOrEmpty(maxVal) Then
            Return $"{minVal} MHz"
        ElseIf String.IsNullOrEmpty(minVal) Then
            Return $"{maxVal} MHz"
        Else
            Return $"{minVal}-{maxVal} MHz"
        End If
    End Function

    ' ==========================================
    ' 1. LOAD & CASCADE DROPDOWNS
    ' ==========================================
    Public Sub ChargerFabricants()
        RemoveHandler cmbFabGestion.SelectedIndexChanged, AddressOf cmbFabGestion_SelectedIndexChanged

        Dim dt As New DataTable()
        Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
            conn.Open()
            Dim query As String = "SELECT fabricant_id, nom_fabricant FROM Fabricants;"
            Using adapter As New SQLiteDataAdapter(query, conn)
                adapter.Fill(dt)
            End Using
        End Using

        cmbFabGestion.DataSource = dt
        cmbFabGestion.DisplayMember = "nom_fabricant"
        cmbFabGestion.ValueMember = "fabricant_id"

        AddHandler cmbFabGestion.SelectedIndexChanged, AddressOf cmbFabGestion_SelectedIndexChanged

        Dim fabId As Integer? = GetSelectedId(cmbFabGestion)
        If dt.Rows.Count > 0 AndAlso fabId.HasValue Then
            ChargerModeles(fabId.Value)
        Else
            cmbModGestion.DataSource = Nothing
            ViderFormulaireConfiguration()
        End If
    End Sub

    Private Sub cmbFabGestion_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbFabGestion.SelectedIndexChanged
        Dim fabId As Integer? = GetSelectedId(cmbFabGestion)
        If fabId.HasValue Then
            ChargerModeles(fabId.Value)
        End If
    End Sub

    Private Sub ChargerModeles(fabricantId As Integer)
        RemoveHandler cmbModGestion.SelectedIndexChanged, AddressOf cmbModGestion_SelectedIndexChanged

        Dim dt As New DataTable()
        Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
            conn.Open()
            Dim query As String = "SELECT modele_id, nom_modele FROM ModelesAppareils WHERE fabricant_id = @fabId;"
            Using adapter As New SQLiteDataAdapter(query, conn)
                adapter.SelectCommand.Parameters.AddWithValue("@fabId", fabricantId)
                adapter.Fill(dt)
            End Using
        End Using

        cmbModGestion.DataSource = dt
        cmbModGestion.DisplayMember = "nom_modele"
        cmbModGestion.ValueMember = "modele_id"

        AddHandler cmbModGestion.SelectedIndexChanged, AddressOf cmbModGestion_SelectedIndexChanged

        Dim modeleId As Integer? = GetSelectedId(cmbModGestion)
        If dt.Rows.Count > 0 AndAlso modeleId.HasValue Then
            ChargerConfigurationModel(modeleId.Value)
        Else
            ViderFormulaireConfiguration()
        End If
    End Sub

    Private Sub cmbModGestion_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbModGestion.SelectedIndexChanged
        Dim modeleId As Integer? = GetSelectedId(cmbModGestion)
        If modeleId.HasValue Then
            ChargerConfigurationModel(modeleId.Value)
        Else
            ViderFormulaireConfiguration()
        End If
    End Sub

    Private Sub ChargerConfigurationModel(modeleId As Integer)
        Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
            conn.Open()
            Dim query As String = "SELECT signal, prf, damping, echelle, filtre, mode, redressement, gain, " &
                                  "freq1, freq2, freq3, freq4, freq5, freq6, freq7, freq8, freq9, freq10, freq11, freq12 " &
                                  "FROM ModelesAppareils WHERE modele_id = @id;"

            Using cmd As New SQLiteCommand(query, conn)
                cmd.Parameters.AddWithValue("@id", modeleId)
                Using reader As SQLiteDataReader = cmd.ExecuteReader()
                    If reader.Read() Then
                        cmbSignal.Text = reader("signal").ToString()
                        cmbRedressement.Text = reader("redressement").ToString()
                        cmbMode.Text = reader("mode").ToString()

                        txtPRF.Text = reader("prf").ToString()
                        txtAmortissement.Text = reader("damping").ToString()
                        txtEchelle.Text = reader("echelle").ToString()
                        txtGain.Text = reader("gain").ToString()

                        ' Filter Range Parsing
                        Dim rawFiltre As String = reader("filtre").ToString().Trim()
                        txtFiltreMin.Clear()
                        txtFiltreMax.Clear()

                        If Not String.IsNullOrEmpty(rawFiltre) AndAlso Not rawFiltre.Equals("LB", StringComparison.OrdinalIgnoreCase) Then
                            Dim cleanFiltre As String = rawFiltre.Replace("MHz", "").Trim()
                            Dim parts As String() = cleanFiltre.Split("-"c)
                            If parts.Length >= 1 Then txtFiltreMin.Text = parts(0).Trim()
                            If parts.Length >= 2 Then txtFiltreMax.Text = parts(1).Trim()
                        End If

                        ' Load Frequencies (Formatted with InvariantCulture dot '.')
                        Dim freqList As New List(Of String)()
                        For i As Integer = 1 To 12
                            Dim colName As String = $"freq{i}"
                            If Not reader.IsDBNull(reader.GetOrdinal(colName)) Then
                                Dim dblVal As Double = Convert.ToDouble(reader(colName))
                                If dblVal > 0 Then
                                    freqList.Add(dblVal.ToString(CultureInfo.InvariantCulture))
                                End If
                            End If
                        Next

                        txtFreq.Text = String.Join("; ", freqList)
                    Else
                        ViderFormulaireConfiguration()
                    End If
                End Using
            End Using
        End Using
    End Sub

    Private Sub ViderFormulaireConfiguration()
        cmbSignal.SelectedIndex = -1
        cmbRedressement.SelectedIndex = -1
        cmbMode.SelectedIndex = -1

        txtFiltreMin.Clear()
        txtFiltreMax.Clear()
        txtPRF.Clear()
        txtAmortissement.Clear()
        txtEchelle.Clear()
        txtGain.Clear()
        txtFreq.Clear()
    End Sub

    ' ==========================================
    ' 2. ADD & DELETE FABRICANTS
    ' ==========================================
    Private Sub btnAjouterFab_Click(sender As Object, e As EventArgs) Handles btnAjouterFab.Click
        Dim nouveauFab As String = txtNouveauFabricant.Text.Trim()
        If String.IsNullOrWhiteSpace(nouveauFab) Then
            MessageBox.Show("Veuillez saisir un nom de fabricant.", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
            conn.Open()
            Dim query As String = "INSERT INTO Fabricants (nom_fabricant) VALUES (@nom);"
            Using cmd As New SQLiteCommand(query, conn)
                cmd.Parameters.AddWithValue("@nom", nouveauFab)
                Try
                    cmd.ExecuteNonQuery()
                    txtNouveauFabricant.Clear()
                    ChargerFabricants()
                    cmbFabGestion.Text = nouveauFab
                Catch ex As SQLiteException
                    MessageBox.Show("Ce fabricant existe déjà.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End Using
        End Using
    End Sub

    Private Sub btnSupprimerFab_Click(sender As Object, e As EventArgs) Handles btnSupprimerFab.Click
        Dim fabIdNullable As Integer? = GetSelectedId(cmbFabGestion)
        If Not fabIdNullable.HasValue Then Return

        If MessageBox.Show("Voulez-vous vraiment supprimer ce fabricant et tous ses modèles ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
                conn.Open()
                Dim query As String = "DELETE FROM Fabricants WHERE fabricant_id = @id;"
                Using cmd As New SQLiteCommand(query, conn)
                    cmd.Parameters.AddWithValue("@id", fabIdNullable.Value)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
            ChargerFabricants()
        End If
    End Sub

    ' ==========================================
    ' 3. ADD & DELETE MODÈLES
    ' ==========================================
    Private Sub btnAjouterMod_Click(sender As Object, e As EventArgs) Handles btnAjouterMod.Click
        Dim fabIdNullable As Integer? = GetSelectedId(cmbFabGestion)
        If Not fabIdNullable.HasValue Then Return

        Dim nouveauModele As String = txtNouveauModele.Text.Trim()
        If String.IsNullOrWhiteSpace(nouveauModele) Then Return

        Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
            conn.Open()
            Dim query As String = "INSERT INTO ModelesAppareils (fabricant_id, nom_modele) VALUES (@fabId, @nom);"
            Using cmd As New SQLiteCommand(query, conn)
                cmd.Parameters.AddWithValue("@fabId", fabIdNullable.Value)
                cmd.Parameters.AddWithValue("@nom", nouveauModele)
                cmd.ExecuteNonQuery()
            End Using
        End Using

        txtNouveauModele.Clear()
        ChargerModeles(fabIdNullable.Value)
        cmbModGestion.Text = nouveauModele
    End Sub

    Private Sub btnSupprimerMod_Click(sender As Object, e As EventArgs) Handles btnSupprimerMod.Click
        Dim modeleIdNullable As Integer? = GetSelectedId(cmbModGestion)
        If Not modeleIdNullable.HasValue Then Return

        If MessageBox.Show("Voulez-vous vraiment supprimer ce modèle ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
                conn.Open()
                Dim query As String = "DELETE FROM ModelesAppareils WHERE modele_id = @id;"
                Using cmd As New SQLiteCommand(query, conn)
                    cmd.Parameters.AddWithValue("@id", modeleIdNullable.Value)
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            Dim fabIdNullable As Integer? = GetSelectedId(cmbFabGestion)
            If fabIdNullable.HasValue Then ChargerModeles(fabIdNullable.Value)
        End If
    End Sub

    ' ==========================================
    ' 4. SAVE TO SQLITE (Explicit DbType.Double)
    ' ==========================================
    Private Sub btnEnregistrer_Click(sender As Object, e As EventArgs) Handles btnEnregistrer.Click
        Dim modeleIdNullable As Integer? = GetSelectedId(cmbModGestion)

        If Not modeleIdNullable.HasValue Then
            MessageBox.Show("Veuillez sélectionner un modèle d'appareil.", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim selectedModeleId As Integer = modeleIdNullable.Value

        ' Clean decimal parsing for single parameters
        Dim prfVal As Double = ParseDoubleSafe(txtPRF.Text)
        Dim dampVal As Double = ParseDoubleSafe(txtAmortissement.Text)
        Dim echVal As Double = ParseDoubleSafe(txtEchelle.Text)
        Dim gainVal As Double = ParseDoubleSafe(txtGain.Text)
        Dim filterVal As String = GetValeurFiltre()

        ' Clean decimal parsing for semicolon or comma separated frequencies
        Dim freqs(11) As Double
        Dim rawTokens = txtFreq.Text.Split(New Char() {";"c, ","c}, StringSplitOptions.RemoveEmptyEntries)

        For i As Integer = 0 To Math.Min(rawTokens.Length - 1, 11)
            freqs(i) = ParseDoubleSafe(rawTokens(i))
        Next

        Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
            conn.Open()
            Dim query As String = "UPDATE ModelesAppareils SET " &
                                  "signal = @signal, " &
                                  "prf = @prf, " &
                                  "damping = @damping, " &
                                  "echelle = @echelle, " &
                                  "filtre = @filtre, " &
                                  "mode = @mode, " &
                                  "redressement = @redressement, " &
                                  "gain = @gain, " &
                                  "freq1 = @f1, freq2 = @f2, freq3 = @f3, freq4 = @f4, " &
                                  "freq5 = @f5, freq6 = @f6, freq7 = @f7, freq8 = @f8, " &
                                  "freq9 = @f9, freq10 = @f10, freq11 = @f11, freq12 = @f12 " &
                                  "WHERE modele_id = @id;"

            Using cmd As New SQLiteCommand(query, conn)
                cmd.Parameters.AddWithValue("@signal", If(cmbSignal.SelectedItem IsNot Nothing, cmbSignal.SelectedItem.ToString(), cmbSignal.Text))
                cmd.Parameters.AddWithValue("@mode", If(cmbMode.SelectedItem IsNot Nothing, cmbMode.SelectedItem.ToString(), cmbMode.Text))
                cmd.Parameters.AddWithValue("@redressement", If(cmbRedressement.SelectedItem IsNot Nothing, cmbRedressement.SelectedItem.ToString(), cmbRedressement.Text))
                cmd.Parameters.AddWithValue("@filtre", filterVal)

                ' Bind numerical parameters as DbType.Double (REAL)
                cmd.Parameters.Add("@prf", DbType.Double).Value = prfVal
                cmd.Parameters.Add("@damping", DbType.Double).Value = dampVal
                cmd.Parameters.Add("@echelle", DbType.Double).Value = echVal
                cmd.Parameters.Add("@gain", DbType.Double).Value = gainVal

                ' Bind 12 frequencies dynamically as DbType.Double
                For i As Integer = 1 To 12
                    cmd.Parameters.Add($"@f{i}", DbType.Double).Value = freqs(i - 1)
                Next

                cmd.Parameters.AddWithValue("@id", selectedModeleId)
                cmd.ExecuteNonQuery()
            End Using
        End Using

        MessageBox.Show("Réglages enregistrés avec succès !", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub btnAnnuler_Click(sender As Object, e As EventArgs) Handles btnAnnuler.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

End Class