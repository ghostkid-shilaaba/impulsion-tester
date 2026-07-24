Imports System.Data.SQLite

Public Class UcConstat

    Private Sub UcConstat_Load(sender As Object, e As EventArgs) Handles MyBase.Load
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
    ' 1. LOAD FABRICANTS & MODELES
    ' ==========================================
    Public Sub ChargerFabricants()
        RemoveHandler cmbFabricants.SelectedIndexChanged, AddressOf cmbFabricants_SelectedIndexChanged

        Dim dt As New DataTable()
        Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
            conn.Open()
            Dim query As String = "SELECT fabricant_id, nom_fabricant FROM Fabricants;"
            Using adapter As New SQLiteDataAdapter(query, conn)
                adapter.Fill(dt)
            End Using
        End Using

        cmbFabricants.DataSource = dt
        cmbFabricants.DisplayMember = "nom_fabricant"
        cmbFabricants.ValueMember = "fabricant_id"

        AddHandler cmbFabricants.SelectedIndexChanged, AddressOf cmbFabricants_SelectedIndexChanged

        ' Load models for the selected manufacturer immediately
        Dim fabId As Integer? = GetSelectedId(cmbFabricants)
        If fabId.HasValue Then
            ChargerModeles(fabId.Value)
        Else
            cmbModeles.DataSource = Nothing
        End If
    End Sub

    ' Fires whenever user picks a different manufacturer in the dropdown
    Private Sub cmbFabricants_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbFabricants.SelectedIndexChanged
        Dim fabId As Integer? = GetSelectedId(cmbFabricants)
        If fabId.HasValue Then
            ChargerModeles(fabId.Value)
        End If
    End Sub

    ''' <summary>
    ''' Loads models belonging to the selected fabricant ID into cmbModeles
    ''' </summary>
    Public Sub ChargerModeles(fabricantId As Integer)
        Dim dt As New DataTable()
        Using conn As New SQLiteConnection(DatabaseHelper.connectionString)
            conn.Open()
            Dim query As String = "SELECT modele_id, nom_modele FROM ModelesAppareils WHERE fabricant_id = @fabId;"
            Using adapter As New SQLiteDataAdapter(query, conn)
                adapter.SelectCommand.Parameters.AddWithValue("@fabId", fabricantId)
                adapter.Fill(dt)
            End Using
        End Using

        cmbModeles.DataSource = dt
        cmbModeles.DisplayMember = "nom_modele"
        cmbModeles.ValueMember = "modele_id"
    End Sub

    ' ==========================================
    ' 2. EVENTS
    ' ==========================================
    Public Event SuivantClique(sender As Object, e As EventArgs)

    Private Sub btnSuivant_Click(sender As Object, e As EventArgs) Handles btnSuivant.Click
        RaiseEvent SuivantClique(Me, e)
    End Sub

    Private Sub btnModifier_Click(sender As Object, e As EventArgs) Handles btnModifier.Click
        Using formGestion As New FormGestionAppareils()
            Dim result As DialogResult = formGestion.ShowDialog()

            If result = DialogResult.OK Then
                ' Refresh both manufacturers and models when returning from management dialog
                ChargerFabricants()
            End If
        End Using
    End Sub

End Class