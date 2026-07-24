<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class UcConstat
    Inherits System.Windows.Forms.UserControl

    'UserControl remplace la méthode Dispose pour nettoyer la liste des composants.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Requise par le Concepteur Windows Form
    Private components As System.ComponentModel.IContainer

    'REMARQUE : la procédure suivante est requise par le Concepteur Windows Form
    'Elle peut être modifiée à l'aide du Concepteur Windows Form.  
    'Ne la modifiez pas à l'aide de l'éditeur de code.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        cmbFabricants = New ComboBox()
        cmbModeles = New ComboBox()
        Label2 = New Label()
        Label3 = New Label()
        GroupBox1 = New GroupBox()
        TableLayoutPanel5 = New TableLayoutPanel()
        btnModifier = New Button()
        TableLayoutPanel4 = New TableLayoutPanel()
        btnSuivant = New Button()
        GroupBox2 = New GroupBox()
        TableLayoutPanel1 = New TableLayoutPanel()
        TextBox1 = New TextBox()
        Label13 = New Label()
        TextBox2 = New TextBox()
        TextBox3 = New TextBox()
        Label14 = New Label()
        TextBox4 = New TextBox()
        TableLayoutPanel2 = New TableLayoutPanel()
        TextBox6 = New TextBox()
        TextBox5 = New TextBox()
        Label15 = New Label()
        ComboBox4 = New ComboBox()
        Label16 = New Label()
        TextBox7 = New TextBox()
        Label17 = New Label()
        TableLayoutPanel3 = New TableLayoutPanel()
        Label10 = New Label()
        DateTimePicker1 = New DateTimePicker()
        Label11 = New Label()
        Label9 = New Label()
        cmbObjetVerification = New ComboBox()
        txtSnInterne = New TextBox()
        Label8 = New Label()
        txtSnAppareil = New TextBox()
        DateTimePicker2 = New DateTimePicker()
        Label12 = New Label()
        GroupBox1.SuspendLayout()
        TableLayoutPanel5.SuspendLayout()
        TableLayoutPanel4.SuspendLayout()
        GroupBox2.SuspendLayout()
        TableLayoutPanel1.SuspendLayout()
        TableLayoutPanel2.SuspendLayout()
        TableLayoutPanel3.SuspendLayout()
        SuspendLayout()
        ' 
        ' cmbFabricants
        ' 
        cmbFabricants.Anchor = AnchorStyles.None
        cmbFabricants.FormattingEnabled = True
        cmbFabricants.Location = New Point(230, 23)
        cmbFabricants.Margin = New Padding(3, 2, 3, 2)
        cmbFabricants.Name = "cmbFabricants"
        cmbFabricants.Size = New Size(182, 23)
        cmbFabricants.TabIndex = 0
        ' 
        ' cmbModeles
        ' 
        cmbModeles.Anchor = AnchorStyles.None
        cmbModeles.FormattingEnabled = True
        cmbModeles.Location = New Point(240, 23)
        cmbModeles.Margin = New Padding(3, 2, 3, 2)
        cmbModeles.Name = "cmbModeles"
        cmbModeles.Size = New Size(162, 23)
        cmbModeles.TabIndex = 1
        ' 
        ' Label2
        ' 
        Label2.Anchor = AnchorStyles.Left
        Label2.AutoSize = True
        Label2.Location = New Point(3, 27)
        Label2.Name = "Label2"
        Label2.Size = New Size(82, 15)
        Label2.TabIndex = 3
        Label2.Text = "par fabricants:" & vbTab & vbTab
        ' 
        ' Label3
        ' 
        Label3.Anchor = AnchorStyles.Left
        Label3.AutoSize = True
        Label3.Location = New Point(3, 27)
        Label3.Name = "Label3"
        Label3.Size = New Size(75, 15)
        Label3.TabIndex = 4
        Label3.Text = "par modèles:" & vbTab
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(TableLayoutPanel5)
        GroupBox1.Controls.Add(btnModifier)
        GroupBox1.Controls.Add(TableLayoutPanel4)
        GroupBox1.Dock = DockStyle.Top
        GroupBox1.Location = New Point(0, 0)
        GroupBox1.Margin = New Padding(3, 2, 3, 2)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Padding = New Padding(3, 2, 3, 2)
        GroupBox1.Size = New Size(995, 94)
        GroupBox1.TabIndex = 5
        GroupBox1.TabStop = False
        GroupBox1.Text = "choix de l'appareil à vérifier" & vbTab & vbTab
        ' 
        ' TableLayoutPanel5
        ' 
        TableLayoutPanel5.Anchor = AnchorStyles.Top
        TableLayoutPanel5.ColumnCount = 2
        TableLayoutPanel5.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel5.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel5.Controls.Add(cmbModeles, 1, 0)
        TableLayoutPanel5.Controls.Add(Label3, 0, 0)
        TableLayoutPanel5.Location = New Point(441, 20)
        TableLayoutPanel5.Margin = New Padding(3, 2, 3, 2)
        TableLayoutPanel5.Name = "TableLayoutPanel5"
        TableLayoutPanel5.RowCount = 1
        TableLayoutPanel5.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel5.Size = New Size(428, 70)
        TableLayoutPanel5.TabIndex = 7
        ' 
        ' btnModifier
        ' 
        btnModifier.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnModifier.Location = New Point(897, 68)
        btnModifier.Margin = New Padding(3, 2, 3, 2)
        btnModifier.Name = "btnModifier"
        btnModifier.Size = New Size(82, 22)
        btnModifier.TabIndex = 5
        btnModifier.Text = "Modifier"
        btnModifier.UseVisualStyleBackColor = True
        ' 
        ' TableLayoutPanel4
        ' 
        TableLayoutPanel4.Anchor = AnchorStyles.Top
        TableLayoutPanel4.ColumnCount = 2
        TableLayoutPanel4.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel4.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel4.Controls.Add(cmbFabricants, 1, 0)
        TableLayoutPanel4.Controls.Add(Label2, 0, 0)
        TableLayoutPanel4.Location = New Point(8, 20)
        TableLayoutPanel4.Margin = New Padding(3, 2, 3, 2)
        TableLayoutPanel4.Name = "TableLayoutPanel4"
        TableLayoutPanel4.RowCount = 1
        TableLayoutPanel4.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel4.Size = New Size(428, 70)
        TableLayoutPanel4.TabIndex = 6
        ' 
        ' btnSuivant
        ' 
        btnSuivant.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnSuivant.Location = New Point(907, 406)
        btnSuivant.Margin = New Padding(3, 2, 3, 2)
        btnSuivant.Name = "btnSuivant"
        btnSuivant.Size = New Size(82, 22)
        btnSuivant.TabIndex = 23
        btnSuivant.Text = "Suivant"
        btnSuivant.UseVisualStyleBackColor = True
        ' 
        ' GroupBox2
        ' 
        GroupBox2.Controls.Add(btnSuivant)
        GroupBox2.Controls.Add(TableLayoutPanel1)
        GroupBox2.Controls.Add(TableLayoutPanel3)
        GroupBox2.Dock = DockStyle.Fill
        GroupBox2.Location = New Point(0, 94)
        GroupBox2.Margin = New Padding(3, 2, 3, 2)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Padding = New Padding(3, 2, 3, 2)
        GroupBox2.Size = New Size(995, 433)
        GroupBox2.TabIndex = 6
        GroupBox2.TabStop = False
        GroupBox2.Text = "Informations à compléter et à reporter sur le constat de vérification" & vbTab & vbTab & vbTab & vbTab
        ' 
        ' TableLayoutPanel1
        ' 
        TableLayoutPanel1.Anchor = AnchorStyles.Top
        TableLayoutPanel1.CausesValidation = False
        TableLayoutPanel1.ColumnCount = 2
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.Controls.Add(TextBox1, 1, 2)
        TableLayoutPanel1.Controls.Add(Label13, 0, 2)
        TableLayoutPanel1.Controls.Add(TextBox2, 1, 1)
        TableLayoutPanel1.Controls.Add(TextBox3, 1, 0)
        TableLayoutPanel1.Controls.Add(Label14, 0, 1)
        TableLayoutPanel1.Controls.Add(TextBox4, 1, 3)
        TableLayoutPanel1.Controls.Add(TableLayoutPanel2, 1, 4)
        TableLayoutPanel1.Controls.Add(Label15, 0, 5)
        TableLayoutPanel1.Controls.Add(ComboBox4, 1, 5)
        TableLayoutPanel1.Controls.Add(Label16, 0, 6)
        TableLayoutPanel1.Controls.Add(TextBox7, 1, 6)
        TableLayoutPanel1.Controls.Add(Label17, 0, 0)
        TableLayoutPanel1.Location = New Point(5, 20)
        TableLayoutPanel1.Margin = New Padding(3, 2, 3, 2)
        TableLayoutPanel1.Name = "TableLayoutPanel1"
        TableLayoutPanel1.RowCount = 7
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 41.29693F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 58.70307F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 24F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 21F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 28F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 93F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 61F))
        TableLayoutPanel1.Size = New Size(470, 370)
        TableLayoutPanel1.TabIndex = 24
        ' 
        ' TextBox1
        ' 
        TextBox1.Anchor = AnchorStyles.None
        TextBox1.Location = New Point(260, 144)
        TextBox1.Margin = New Padding(3, 2, 3, 2)
        TextBox1.Name = "TextBox1"
        TextBox1.Size = New Size(184, 23)
        TextBox1.TabIndex = 6
        ' 
        ' Label13
        ' 
        Label13.Anchor = AnchorStyles.Left
        Label13.AutoSize = True
        Label13.Location = New Point(3, 147)
        Label13.Margin = New Padding(3, 1, 3, 0)
        Label13.Name = "Label13"
        Label13.Size = New Size(57, 15)
        Label13.TabIndex = 5
        Label13.Text = "ADRESSE "
        ' 
        ' TextBox2
        ' 
        TextBox2.Anchor = AnchorStyles.None
        TextBox2.Location = New Point(260, 89)
        TextBox2.Margin = New Padding(3, 2, 3, 2)
        TextBox2.Name = "TextBox2"
        TextBox2.Size = New Size(184, 23)
        TextBox2.TabIndex = 4
        ' 
        ' TextBox3
        ' 
        TextBox3.Anchor = AnchorStyles.None
        TextBox3.Location = New Point(260, 18)
        TextBox3.Margin = New Padding(3, 2, 3, 2)
        TextBox3.Name = "TextBox3"
        TextBox3.PlaceholderText = "ICE-XX-XXX"
        TextBox3.Size = New Size(184, 23)
        TextBox3.TabIndex = 2
        ' 
        ' Label14
        ' 
        Label14.Anchor = AnchorStyles.Left
        Label14.AutoSize = True
        Label14.Location = New Point(3, 93)
        Label14.Margin = New Padding(3, 1, 3, 0)
        Label14.Name = "Label14"
        Label14.Size = New Size(78, 15)
        Label14.TabIndex = 3
        Label14.Text = "NOM CLIENT"
        ' 
        ' TextBox4
        ' 
        TextBox4.Anchor = AnchorStyles.None
        TextBox4.Location = New Point(260, 168)
        TextBox4.Margin = New Padding(3, 2, 3, 2)
        TextBox4.Name = "TextBox4"
        TextBox4.Size = New Size(184, 23)
        TextBox4.TabIndex = 9
        ' 
        ' TableLayoutPanel2
        ' 
        TableLayoutPanel2.ColumnCount = 2
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.Controls.Add(TextBox6, 1, 0)
        TableLayoutPanel2.Controls.Add(TextBox5, 0, 0)
        TableLayoutPanel2.Dock = DockStyle.Fill
        TableLayoutPanel2.Location = New Point(238, 189)
        TableLayoutPanel2.Margin = New Padding(3, 2, 3, 2)
        TableLayoutPanel2.Name = "TableLayoutPanel2"
        TableLayoutPanel2.RowCount = 1
        TableLayoutPanel2.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.Size = New Size(229, 24)
        TableLayoutPanel2.TabIndex = 10
        ' 
        ' TextBox6
        ' 
        TextBox6.Anchor = AnchorStyles.Left
        TextBox6.CausesValidation = False
        TextBox6.Location = New Point(117, 2)
        TextBox6.Margin = New Padding(3, 2, 3, 2)
        TextBox6.Name = "TextBox6"
        TextBox6.PlaceholderText = "VILLE"
        TextBox6.Size = New Size(90, 23)
        TextBox6.TabIndex = 11
        ' 
        ' TextBox5
        ' 
        TextBox5.Anchor = AnchorStyles.Right
        TextBox5.Location = New Point(21, 2)
        TextBox5.Margin = New Padding(3, 2, 3, 2)
        TextBox5.Name = "TextBox5"
        TextBox5.PlaceholderText = "CP"
        TextBox5.Size = New Size(90, 23)
        TextBox5.TabIndex = 10
        ' 
        ' Label15
        ' 
        Label15.Anchor = AnchorStyles.Left
        Label15.AutoSize = True
        Label15.Location = New Point(3, 254)
        Label15.Margin = New Padding(3, 1, 3, 0)
        Label15.Name = "Label15"
        Label15.Size = New Size(124, 15)
        Label15.TabIndex = 11
        Label15.Text = "VALIDITE PRECONISEE"
        ' 
        ' ComboBox4
        ' 
        ComboBox4.Anchor = AnchorStyles.None
        ComboBox4.FormattingEnabled = True
        ComboBox4.Items.AddRange(New Object() {"6", "12", "18", "24", "30"})
        ComboBox4.Location = New Point(249, 250)
        ComboBox4.Margin = New Padding(3, 2, 3, 2)
        ComboBox4.Name = "ComboBox4"
        ComboBox4.Size = New Size(207, 23)
        ComboBox4.TabIndex = 12
        ' 
        ' Label16
        ' 
        Label16.Anchor = AnchorStyles.Left
        Label16.AutoSize = True
        Label16.Location = New Point(3, 332)
        Label16.Margin = New Padding(3, 1, 3, 0)
        Label16.Name = "Label16"
        Label16.Size = New Size(119, 15)
        Label16.TabIndex = 13
        Label16.Text = "SIGNATURE DIGITALE"
        ' 
        ' TextBox7
        ' 
        TextBox7.Anchor = AnchorStyles.None
        TextBox7.Location = New Point(260, 327)
        TextBox7.Margin = New Padding(3, 2, 3, 2)
        TextBox7.Name = "TextBox7"
        TextBox7.Size = New Size(184, 23)
        TextBox7.TabIndex = 14
        ' 
        ' Label17
        ' 
        Label17.Anchor = AnchorStyles.Left
        Label17.AutoSize = True
        Label17.Location = New Point(3, 22)
        Label17.Margin = New Padding(3, 1, 3, 0)
        Label17.Name = "Label17"
        Label17.Size = New Size(76, 15)
        Label17.TabIndex = 1
        Label17.Text = "N° CONSTAT"
        ' 
        ' TableLayoutPanel3
        ' 
        TableLayoutPanel3.Anchor = AnchorStyles.Top
        TableLayoutPanel3.ColumnCount = 2
        TableLayoutPanel3.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel3.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel3.Controls.Add(Label10, 0, 2)
        TableLayoutPanel3.Controls.Add(DateTimePicker1, 1, 2)
        TableLayoutPanel3.Controls.Add(Label11, 0, 3)
        TableLayoutPanel3.Controls.Add(Label9, 0, 0)
        TableLayoutPanel3.Controls.Add(cmbObjetVerification, 1, 3)
        TableLayoutPanel3.Controls.Add(txtSnInterne, 1, 0)
        TableLayoutPanel3.Controls.Add(Label8, 0, 1)
        TableLayoutPanel3.Controls.Add(txtSnAppareil, 1, 1)
        TableLayoutPanel3.Controls.Add(DateTimePicker2, 1, 4)
        TableLayoutPanel3.Controls.Add(Label12, 0, 4)
        TableLayoutPanel3.Location = New Point(480, 20)
        TableLayoutPanel3.Margin = New Padding(3, 2, 3, 2)
        TableLayoutPanel3.Name = "TableLayoutPanel3"
        TableLayoutPanel3.RowCount = 5
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Percent, 41.73554F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Percent, 58.26446F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 74F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 94F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 60F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 15F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 15F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 15F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 15F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 15F))
        TableLayoutPanel3.Size = New Size(490, 370)
        TableLayoutPanel3.TabIndex = 25
        ' 
        ' Label10
        ' 
        Label10.Anchor = AnchorStyles.Left
        Label10.AutoSize = True
        Label10.Location = New Point(3, 170)
        Label10.Name = "Label10"
        Label10.Size = New Size(145, 15)
        Label10.TabIndex = 27
        Label10.Text = "DATE DE MISE EN SERVICE"
        ' 
        ' DateTimePicker1
        ' 
        DateTimePicker1.Anchor = AnchorStyles.None
        DateTimePicker1.CustomFormat = "ddMMMMyyyy"
        DateTimePicker1.Format = DateTimePickerFormat.Custom
        DateTimePicker1.Location = New Point(270, 166)
        DateTimePicker1.Margin = New Padding(3, 2, 3, 2)
        DateTimePicker1.Name = "DateTimePicker1"
        DateTimePicker1.Size = New Size(194, 23)
        DateTimePicker1.TabIndex = 28
        ' 
        ' Label11
        ' 
        Label11.Anchor = AnchorStyles.Left
        Label11.AutoSize = True
        Label11.Location = New Point(3, 254)
        Label11.Name = "Label11"
        Label11.Size = New Size(152, 15)
        Label11.TabIndex = 29
        Label11.Text = "OBJET DE LA VERIFICATION"
        ' 
        ' Label9
        ' 
        Label9.Anchor = AnchorStyles.Left
        Label9.AutoSize = True
        Label9.Location = New Point(3, 22)
        Label9.Name = "Label9"
        Label9.Size = New Size(119, 15)
        Label9.TabIndex = 25
        Label9.Text = "S/N INTERNE CLIENT"
        ' 
        ' cmbObjetVerification
        ' 
        cmbObjetVerification.Anchor = AnchorStyles.None
        cmbObjetVerification.FormattingEnabled = True
        cmbObjetVerification.Items.AddRange(New Object() {"1ere mise en service", "périodique", "après maintenance"})
        cmbObjetVerification.Location = New Point(270, 250)
        cmbObjetVerification.Margin = New Padding(3, 2, 3, 2)
        cmbObjetVerification.Name = "cmbObjetVerification"
        cmbObjetVerification.Size = New Size(194, 23)
        cmbObjetVerification.TabIndex = 30
        ' 
        ' txtSnInterne
        ' 
        txtSnInterne.Anchor = AnchorStyles.None
        txtSnInterne.Location = New Point(270, 18)
        txtSnInterne.Margin = New Padding(3, 2, 3, 2)
        txtSnInterne.MaxLength = 16
        txtSnInterne.Name = "txtSnInterne"
        txtSnInterne.Size = New Size(194, 23)
        txtSnInterne.TabIndex = 26
        ' 
        ' Label8
        ' 
        Label8.Anchor = AnchorStyles.Left
        Label8.AutoSize = True
        Label8.Location = New Point(3, 92)
        Label8.Name = "Label8"
        Label8.Size = New Size(81, 15)
        Label8.TabIndex = 23
        Label8.Text = "S/N APPAREIL"
        ' 
        ' txtSnAppareil
        ' 
        txtSnAppareil.Anchor = AnchorStyles.None
        txtSnAppareil.Location = New Point(270, 88)
        txtSnAppareil.Margin = New Padding(3, 2, 3, 2)
        txtSnAppareil.MaxLength = 16
        txtSnAppareil.Name = "txtSnAppareil"
        txtSnAppareil.Size = New Size(194, 23)
        txtSnAppareil.TabIndex = 24
        ' 
        ' DateTimePicker2
        ' 
        DateTimePicker2.Anchor = AnchorStyles.None
        DateTimePicker2.CustomFormat = "ddMMMMyyyy"
        DateTimePicker2.Format = DateTimePickerFormat.Custom
        DateTimePicker2.Location = New Point(270, 328)
        DateTimePicker2.Margin = New Padding(3, 2, 3, 2)
        DateTimePicker2.Name = "DateTimePicker2"
        DateTimePicker2.Size = New Size(194, 23)
        DateTimePicker2.TabIndex = 32
        ' 
        ' Label12
        ' 
        Label12.Anchor = AnchorStyles.Left
        Label12.AutoSize = True
        Label12.Location = New Point(3, 332)
        Label12.Name = "Label12"
        Label12.Size = New Size(147, 15)
        Label12.TabIndex = 31
        Label12.Text = "DATE DE LA VERIFICATION"
        ' 
        ' UcConstat
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        Controls.Add(GroupBox2)
        Controls.Add(GroupBox1)
        Margin = New Padding(3, 2, 3, 2)
        Name = "UcConstat"
        Size = New Size(995, 527)
        GroupBox1.ResumeLayout(False)
        TableLayoutPanel5.ResumeLayout(False)
        TableLayoutPanel5.PerformLayout()
        TableLayoutPanel4.ResumeLayout(False)
        TableLayoutPanel4.PerformLayout()
        GroupBox2.ResumeLayout(False)
        TableLayoutPanel1.ResumeLayout(False)
        TableLayoutPanel1.PerformLayout()
        TableLayoutPanel2.ResumeLayout(False)
        TableLayoutPanel2.PerformLayout()
        TableLayoutPanel3.ResumeLayout(False)
        TableLayoutPanel3.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents cmbFabricants As ComboBox
    Friend WithEvents cmbModeles As ComboBox
    Friend WithEvents Label2 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents btnModifier As Button
    Friend WithEvents GroupBox2 As GroupBox
    Public WithEvents btnSuivant As Button
    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents TextBox1 As TextBox
    Friend WithEvents Label13 As Label
    Friend WithEvents TextBox2 As TextBox
    Friend WithEvents TextBox3 As TextBox
    Friend WithEvents Label14 As Label
    Friend WithEvents TextBox4 As TextBox
    Friend WithEvents TableLayoutPanel2 As TableLayoutPanel
    Friend WithEvents TextBox5 As TextBox
    Friend WithEvents TextBox6 As TextBox
    Friend WithEvents Label15 As Label
    Friend WithEvents ComboBox4 As ComboBox
    Friend WithEvents Label16 As Label
    Friend WithEvents TextBox7 As TextBox
    Friend WithEvents Label17 As Label
    Friend WithEvents TableLayoutPanel3 As TableLayoutPanel
    Friend WithEvents Label10 As Label
    Friend WithEvents cmbObjetVerification As ComboBox
    Friend WithEvents txtSnAppareil As TextBox
    Friend WithEvents Label8 As Label
    Friend WithEvents DateTimePicker2 As DateTimePicker
    Friend WithEvents Label11 As Label
    Friend WithEvents Label12 As Label
    Friend WithEvents DateTimePicker1 As DateTimePicker
    Friend WithEvents txtSnInterne As TextBox
    Friend WithEvents Label9 As Label
    Friend WithEvents TableLayoutPanel5 As TableLayoutPanel
    Friend WithEvents TableLayoutPanel4 As TableLayoutPanel

End Class
