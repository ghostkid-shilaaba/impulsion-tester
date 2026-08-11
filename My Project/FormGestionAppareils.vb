Imports System.Data
Imports System.Data.SQLite
Imports System.Globalization

Public Class FormGestionAppareils
    Private _modeleId As Integer?
    Private _filtresDt As DataTable
    Private _currentFiltreRow As DataRow = Nothing
    Private _changingFilterSelection As Boolean = False
    Private _updatingFilterName As Boolean = False
    Private _savingFilter As Boolean = False
    Private _userChangingSelection As Boolean = False

    ' ------------------------------------------------------------------
    ' Parsing helper
    ' ------------------------------------------------------------------
    Private Function ParseDoubleSafe(input As String) As Double
        If String.IsNullOrWhiteSpace(input) Then Return 0.0
        Dim clean As String = input.Trim().Replace(" "c, "").Replace(","c, "."c)
        Dim result As Double = 0.0
        If Double.TryParse(clean, NumberStyles.AllowDecimalPoint Or NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, result) Then
            Return result
        End If
        Return 0.0
    End Function

    ' ------------------------------------------------------------------
    ' ID extraction helper
    ' ------------------------------------------------------------------
    Private Function GetSelectedId(cmb As ComboBox) As Integer?
        If cmb.SelectedValue Is Nothing Then Return Nothing
        If TypeOf cmb.SelectedValue Is Integer Then Return Convert.ToInt32(cmb.SelectedValue)
        If TypeOf cmb.SelectedValue Is DataRowView Then
            Dim drv As DataRowView = CType(cmb.SelectedValue, DataRowView)
            Return Convert.ToInt32(drv(cmb.ValueMember))
        End If
        Dim parsedId As Integer
        If Integer.TryParse(cmb.SelectedValue.ToString(), parsedId) Then Return parsedId
        Return Nothing
    End Function

    ' ------------------------------------------------------------------
    ' Load Fabricants & Modeles
    ' ------------------------------------------------------------------
    Private Sub FormGestionAppareils_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ChargerFabricants()
    End Sub

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
        If fabId.HasValue Then ChargerModeles(fabId.Value)
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
        _modeleId = GetSelectedId(cmbModGestion)
        If _modeleId.HasValue Then
            ChargerConfigurationModel(_modeleId.Value)
        Else
            ViderFormulaireConfiguration()
        End If
    End Sub

    ' ------------------------------------------------------------------
    ' Load model parameters & filters
    ' ------------------------------------------------------------------
    Private Sub ChargerConfigurationModel(modeleId As Integer)
        Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
            conn.Open()
            Dim query As String = "SELECT signal, prf, damping, echelle, mode, redressement, gain " &
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

                        ' Load filters
                        ChargerFiltres(modeleId)
                    Else
                        ViderFormulaireConfiguration()
                    End If
                End Using
            End Using
        End Using
    End Sub

    Private Sub ChargerFiltres(modeleId As Integer)
        _changingFilterSelection = True
        Try
            _filtresDt = DatabaseHelper.GetFiltresForModele(modeleId)
            If _filtresDt Is Nothing Then _filtresDt = New DataTable()

            cmbFiltre.DataSource = Nothing
            cmbFiltre.DataSource = _filtresDt
            cmbFiltre.DisplayMember = "nom_filtre"
            cmbFiltre.ValueMember = "filtre_id"

            _currentFiltreRow = Nothing

            If _filtresDt.Rows.Count > 0 Then
                cmbFiltre.SelectedIndex = 0
                Dim selectedView As DataRowView = TryCast(cmbFiltre.SelectedItem, DataRowView)
                If selectedView IsNot Nothing Then
                    _currentFiltreRow = selectedView.Row
                    txtFreq.Text = _currentFiltreRow("frequences").ToString()
                End If
            Else
                cmbFiltre.SelectedIndex = -1
                txtFreq.Clear()
            End If
        Finally
            _changingFilterSelection = False
        End Try
    End Sub

    ' ------------------------------------------------------------------
    ' Save current filter (ONLY frequencies)
    ' ------------------------------------------------------------------
    Private Sub SaveCurrentFilter()
        ' Reentrancy guard: writing to _currentFiltreRow("frequences") can,
        ' depending on how txtFreq is wired up (data binding and/or a
        ' TextChanged handler that also calls SaveCurrentFilter), cause
        ' txtFreq.Text to be programmatically reset, which re-fires
        ' TextChanged, which calls SaveCurrentFilter again -> infinite
        ' recursion -> StackOverflowException. This flag breaks that cycle
        ' no matter which event ends up calling this method.
        If _savingFilter Then Return
        If _currentFiltreRow Is Nothing Then Return
        If _currentFiltreRow.RowState = DataRowState.Deleted OrElse
           _currentFiltreRow.RowState = DataRowState.Detached Then
            Return
        End If

        _savingFilter = True
        Try
            Dim newValue As String = txtFreq.Text.Trim()
            If _currentFiltreRow("frequences").ToString() <> newValue Then
                _currentFiltreRow("frequences") = newValue
            End If
        Catch ex As Exception
            MessageBox.Show($"Erreur lors de la sauvegarde du filtre : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            _savingFilter = False
        End Try
    End Sub

    ' ------------------------------------------------------------------
    ' Live-save frequencies as the user types. Guarded by _savingFilter
    ' inside SaveCurrentFilter itself, so this can never recurse even if
    ' txtFreq.Text gets programmatically reassigned somewhere.
    ' ------------------------------------------------------------------
    Private Sub txtFreq_TextChanged(sender As Object, e As EventArgs) Handles txtFreq.TextChanged
        If _savingFilter Then Return
        If _changingFilterSelection Then Return
        SaveCurrentFilter()
    End Sub

    ' ------------------------------------------------------------------
    ' Fires when the user picks a DIFFERENT filter from the dropdown
    ' (mouse click or keyboard), BEFORE SelectedIndexChanged/TextChanged.
    ' We use it to flag that the upcoming TextChanged is a side-effect of
    ' switching filters, not the user typing a new name — otherwise
    ' switching filters would rename whichever row happened to be
    ' "current" to the new filter's name.
    ' ------------------------------------------------------------------
    Private Sub cmbFiltre_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles cmbFiltre.SelectionChangeCommitted
        _userChangingSelection = True
    End Sub

    ' ------------------------------------------------------------------
    ' Rename current filter (from cmbFiltre.Text) — GUARDED to avoid
    ' the databinding feedback loop that caused a StackOverflowException:
    ' writing to _currentFiltreRow("nom_filtre") updates the bound
    ' cmbFiltre's Text, which re-fires TextChanged, which wrote to the
    ' row again, forever. _updatingFilterName breaks that cycle, and we
    ' also skip this while _changingFilterSelection is true (i.e. while
    ' we're programmatically switching the selected filter, not typing
    ' a new name), and while _userChangingSelection is true (i.e. this
    ' Text change came from the user picking a different existing filter,
    ' not from typing a new name for the current one).
    ' ------------------------------------------------------------------
    Private Sub cmbFiltre_TextChanged(sender As Object, e As EventArgs) Handles cmbFiltre.TextChanged
        If _updatingFilterName Then Return
        If _changingFilterSelection Then Return
        If _userChangingSelection Then
            _userChangingSelection = False ' consume it: this Text change was a selection switch, not a rename
            Return
        End If
        If _currentFiltreRow Is Nothing Then Return
        If _currentFiltreRow.RowState = DataRowState.Deleted OrElse
           _currentFiltreRow.RowState = DataRowState.Detached Then
            Return
        End If

        _updatingFilterName = True
        Try
            _currentFiltreRow("nom_filtre") = cmbFiltre.Text
        Finally
            _updatingFilterName = False
        End Try
    End Sub

    ' ------------------------------------------------------------------
    ' ComboBox filter selection changed
    ' ------------------------------------------------------------------
    Private Sub cmbFiltre_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbFiltre.SelectedIndexChanged
        If _filtresDt Is Nothing Then Return
        If _changingFilterSelection Then Return

        ' Save the filter we were editing BEFORE changing _currentFiltreRow
        SaveCurrentFilter()

        If cmbFiltre.SelectedIndex >= 0 AndAlso cmbFiltre.SelectedIndex < _filtresDt.Rows.Count Then
            Dim selectedView As DataRowView = TryCast(cmbFiltre.SelectedItem, DataRowView)
            If selectedView IsNot Nothing Then
                _currentFiltreRow = selectedView.Row
                txtFreq.Text = _currentFiltreRow("frequences").ToString()
            End If
        Else
            _currentFiltreRow = Nothing
            txtFreq.Clear()
        End If
    End Sub

    ' ------------------------------------------------------------------
    ' Add filter
    ' ------------------------------------------------------------------
    Private Sub bttnAj_Click(sender As Object, e As EventArgs) Handles bttnAj.Click
        If _modeleId Is Nothing Then
            MessageBox.Show("Veuillez d'abord sélectionner un modèle.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim newName As String = InputBox("Entrez le nom du nouveau filtre :", "Ajouter un filtre", "")
        If String.IsNullOrWhiteSpace(newName) Then Return

        ' Save current filter before adding
        SaveCurrentFilter()

        Dim newRow As DataRow = _filtresDt.NewRow()
        newRow("nom_filtre") = newName
        newRow("frequences") = ""
        _filtresDt.Rows.Add(newRow)

        ' Select the new filter without triggering save or the rename handler
        _changingFilterSelection = True
        Try
            cmbFiltre.SelectedIndex = _filtresDt.Rows.IndexOf(newRow)
        Finally
            _changingFilterSelection = False
        End Try

        _currentFiltreRow = newRow
        txtFreq.Clear()
    End Sub

    ' ------------------------------------------------------------------
    ' Delete filter
    ' ------------------------------------------------------------------
    Private Sub bttnSup_Click(sender As Object, e As EventArgs) Handles bttnSup.Click
        If _currentFiltreRow Is Nothing Then Return

        If MessageBox.Show(
            $"Voulez-vous supprimer le filtre '{_currentFiltreRow("nom_filtre")}' ?",
            "Confirmation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        ) <> DialogResult.Yes Then
            Return
        End If

        ' Save current filter before deleting
        SaveCurrentFilter()

        Dim deletedRow As DataRow = _currentFiltreRow
        _filtresDt.Rows.Remove(deletedRow)
        _currentFiltreRow = Nothing

        If _filtresDt.Rows.Count = 0 Then
            _changingFilterSelection = True
            Try
                cmbFiltre.SelectedIndex = -1
            Finally
                _changingFilterSelection = False
            End Try
            txtFreq.Clear()
            Return
        End If

        ' Select another filter
        _changingFilterSelection = True
        Try
            cmbFiltre.SelectedIndex = 0
        Finally
            _changingFilterSelection = False
        End Try

        Dim selectedView As DataRowView = TryCast(cmbFiltre.SelectedItem, DataRowView)
        If selectedView IsNot Nothing Then
            _currentFiltreRow = selectedView.Row
            txtFreq.Text = _currentFiltreRow("frequences").ToString()
        End If
    End Sub

    ' ------------------------------------------------------------------
    ' Clear form
    ' ------------------------------------------------------------------
    Private Sub ViderFormulaireConfiguration()
        cmbSignal.SelectedIndex = -1
        cmbRedressement.SelectedIndex = -1
        cmbMode.SelectedIndex = -1

        txtPRF.Clear()
        txtAmortissement.Clear()
        txtEchelle.Clear()
        txtGain.Clear()

        _filtresDt = Nothing
        cmbFiltre.DataSource = Nothing
        txtFreq.Clear()
        _currentFiltreRow = Nothing
    End Sub

    ' ------------------------------------------------------------------
    ' Save DUT & filters
    ' ------------------------------------------------------------------
    Private Sub btnEnregistrer_Click(sender As Object, e As EventArgs) Handles btnEnregistrer.Click
        Dim modeleIdNullable As Integer? = GetSelectedId(cmbModGestion)
        If Not modeleIdNullable.HasValue Then
            MessageBox.Show("Veuillez sélectionner un modèle d'appareil.", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim selectedModeleId As Integer = modeleIdNullable.Value

        ' Save the currently selected filter first
        SaveCurrentFilter()

        Dim prfVal As Double = ParseDoubleSafe(txtPRF.Text)
        Dim dampVal As Double = ParseDoubleSafe(txtAmortissement.Text)
        Dim echVal As Double = ParseDoubleSafe(txtEchelle.Text)
        Dim gainVal As Double = ParseDoubleSafe(txtGain.Text)

        Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
            conn.Open()
            Dim query As String = "UPDATE ModelesAppareils SET " &
                                  "signal = @signal, " &
                                  "prf = @prf, " &
                                  "damping = @damping, " &
                                  "echelle = @echelle, " &
                                  "mode = @mode, " &
                                  "redressement = @redressement, " &
                                  "gain = @gain " &
                                  "WHERE modele_id = @id;"
            Using cmd As New SQLiteCommand(query, conn)
                cmd.Parameters.AddWithValue("@signal", If(cmbSignal.SelectedItem IsNot Nothing, cmbSignal.SelectedItem.ToString(), cmbSignal.Text))
                cmd.Parameters.AddWithValue("@mode", If(cmbMode.SelectedItem IsNot Nothing, cmbMode.SelectedItem.ToString(), cmbMode.Text))
                cmd.Parameters.AddWithValue("@redressement", If(cmbRedressement.SelectedItem IsNot Nothing, cmbRedressement.SelectedItem.ToString(), cmbRedressement.Text))
                cmd.Parameters.Add("@prf", DbType.Double).Value = prfVal
                cmd.Parameters.Add("@damping", DbType.Double).Value = dampVal
                cmd.Parameters.Add("@echelle", DbType.Double).Value = echVal
                cmd.Parameters.Add("@gain", DbType.Double).Value = gainVal
                cmd.Parameters.AddWithValue("@id", selectedModeleId)
                cmd.ExecuteNonQuery()
            End Using
        End Using

        ' Save all filters
        If _filtresDt IsNot Nothing AndAlso _filtresDt.Rows.Count > 0 Then
            DatabaseHelper.SaveFiltresForModele(selectedModeleId, _filtresDt)
        Else
            ' No filters left – delete any existing ones
            Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
                conn.Open()
                Using deleteCmd As New SQLiteCommand("DELETE FROM Filtres WHERE modele_id = @modeleId", conn)
                    deleteCmd.Parameters.AddWithValue("@modeleId", selectedModeleId)
                    deleteCmd.ExecuteNonQuery()
                End Using
            End Using
        End If

        MessageBox.Show("Réglages enregistrés avec succès !", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    ' ------------------------------------------------------------------
    ' Navigate to Impulsion
    ' ------------------------------------------------------------------
    Private Sub BttnImpulsion_Click(sender As Object, e As EventArgs) Handles BttnImpulsion.Click
        Dim modeleId As Integer? = GetSelectedId(cmbModGestion)
        If Not modeleId.HasValue Then
            MessageBox.Show("Veuillez sélectionner un modèle.", "Modèle non sélectionné", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Using form As New IMPForm(modeleId.Value)
            form.ShowDialog()
        End Using
    End Sub

    Private Sub btnAnnuler_Click(sender As Object, e As EventArgs) Handles btnAnnuler.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class