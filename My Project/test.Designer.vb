<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class test
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
        TableLayoutPanel1 = New TableLayoutPanel()
        txtAdresse1 = New TextBox()
        Label5 = New Label()
        txtNomClient = New TextBox()
        txtNoConstat = New TextBox()
        Label4 = New Label()
        txtAdresse2 = New TextBox()
        TableLayoutPanel2 = New TableLayoutPanel()
        txtCP = New TextBox()
        txtVille = New TextBox()
        Label6 = New Label()
        ComboBox3 = New ComboBox()
        Label7 = New Label()
        txtSignatureDigitale = New TextBox()
        Label1 = New Label()
        FlowLayoutPanel1 = New FlowLayoutPanel()
        TableLayoutPanel3 = New TableLayoutPanel()
        Label10 = New Label()
        cmbObjetVerification = New ComboBox()
        txtSnAppareil = New TextBox()
        Label8 = New Label()
        DateTimePicker2 = New DateTimePicker()
        Label11 = New Label()
        Label12 = New Label()
        DateTimePicker1 = New DateTimePicker()
        txtSnInterne = New TextBox()
        Label9 = New Label()
        TableLayoutPanel1.SuspendLayout()
        TableLayoutPanel2.SuspendLayout()
        TableLayoutPanel3.SuspendLayout()
        SuspendLayout()
        ' 
        ' TableLayoutPanel1
        ' 
        TableLayoutPanel1.Anchor = AnchorStyles.None
        TableLayoutPanel1.CausesValidation = False
        TableLayoutPanel1.ColumnCount = 2
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.Controls.Add(txtAdresse1, 1, 2)
        TableLayoutPanel1.Controls.Add(Label5, 0, 2)
        TableLayoutPanel1.Controls.Add(txtNomClient, 1, 1)
        TableLayoutPanel1.Controls.Add(txtNoConstat, 1, 0)
        TableLayoutPanel1.Controls.Add(Label4, 0, 1)
        TableLayoutPanel1.Controls.Add(txtAdresse2, 1, 3)
        TableLayoutPanel1.Controls.Add(TableLayoutPanel2, 1, 4)
        TableLayoutPanel1.Controls.Add(Label6, 0, 5)
        TableLayoutPanel1.Controls.Add(ComboBox3, 1, 5)
        TableLayoutPanel1.Controls.Add(Label7, 0, 6)
        TableLayoutPanel1.Controls.Add(txtSignatureDigitale, 1, 6)
        TableLayoutPanel1.Controls.Add(Label1, 0, 0)
        TableLayoutPanel1.Location = New Point(20, 3)
        TableLayoutPanel1.Name = "TableLayoutPanel1"
        TableLayoutPanel1.RowCount = 7
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 49.2957764F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 50.7042236F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 34F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 35F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 44F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 39F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 100F))
        TableLayoutPanel1.Size = New Size(215, 267)
        TableLayoutPanel1.TabIndex = 0
        ' 
        ' txtAdresse1
        ' 
        txtAdresse1.Anchor = AnchorStyles.Top
        txtAdresse1.Location = New Point(110, 17)
        txtAdresse1.Name = "txtAdresse1"
        txtAdresse1.Size = New Size(102, 27)
        txtAdresse1.TabIndex = 6
        ' 
        ' Label5
        ' 
        Label5.Anchor = AnchorStyles.None
        Label5.AutoSize = True
        Label5.Location = New Point(16, 21)
        Label5.Name = "Label5"
        Label5.Size = New Size(75, 20)
        Label5.TabIndex = 5
        Label5.Text = "ADRESSE "
        ' 
        ' txtNomClient
        ' 
        txtNomClient.Anchor = AnchorStyles.Top
        txtNomClient.Location = New Point(110, 10)
        txtNomClient.Name = "txtNomClient"
        txtNomClient.Size = New Size(102, 27)
        txtNomClient.TabIndex = 4
        ' 
        ' txtNoConstat
        ' 
        txtNoConstat.Anchor = AnchorStyles.None
        txtNoConstat.Location = New Point(110, 3)
        txtNoConstat.Name = "txtNoConstat"
        txtNoConstat.PlaceholderText = "ICE-XX-XXX"
        txtNoConstat.Size = New Size(102, 27)
        txtNoConstat.TabIndex = 2
        ' 
        ' Label4
        ' 
        Label4.Anchor = AnchorStyles.None
        Label4.AutoSize = True
        Label4.Location = New Point(6, 7)
        Label4.Name = "Label4"
        Label4.Size = New Size(95, 7)
        Label4.TabIndex = 3
        Label4.Text = "NOM CLIENT"
        ' 
        ' txtAdresse2
        ' 
        txtAdresse2.Anchor = AnchorStyles.Top
        txtAdresse2.Location = New Point(110, 51)
        txtAdresse2.Name = "txtAdresse2"
        txtAdresse2.Size = New Size(102, 27)
        txtAdresse2.TabIndex = 9
        ' 
        ' TableLayoutPanel2
        ' 
        TableLayoutPanel2.ColumnCount = 2
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.Controls.Add(txtCP, 0, 0)
        TableLayoutPanel2.Controls.Add(txtVille, 1, 0)
        TableLayoutPanel2.Dock = DockStyle.Fill
        TableLayoutPanel2.Location = New Point(110, 86)
        TableLayoutPanel2.Name = "TableLayoutPanel2"
        TableLayoutPanel2.RowCount = 1
        TableLayoutPanel2.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.Size = New Size(102, 38)
        TableLayoutPanel2.TabIndex = 10
        ' 
        ' txtCP
        ' 
        txtCP.Anchor = AnchorStyles.None
        txtCP.Location = New Point(3, 5)
        txtCP.Name = "txtCP"
        txtCP.PlaceholderText = "CP"
        txtCP.Size = New Size(45, 27)
        txtCP.TabIndex = 10
        ' 
        ' txtVille
        ' 
        txtVille.Anchor = AnchorStyles.None
        txtVille.CausesValidation = False
        txtVille.Location = New Point(54, 5)
        txtVille.Name = "txtVille"
        txtVille.PlaceholderText = "VILLE"
        txtVille.Size = New Size(45, 27)
        txtVille.TabIndex = 11
        ' 
        ' Label6
        ' 
        Label6.Anchor = AnchorStyles.None
        Label6.AutoSize = True
        Label6.Location = New Point(7, 127)
        Label6.Name = "Label6"
        Label6.Size = New Size(93, 39)
        Label6.TabIndex = 11
        Label6.Text = "VALIDITE PRECONISEE"
        ' 
        ' ComboBox3
        ' 
        ComboBox3.Anchor = AnchorStyles.None
        ComboBox3.FormattingEnabled = True
        ComboBox3.Items.AddRange(New Object() {"6", "12", "18", "24", "30"})
        ComboBox3.Location = New Point(121, 132)
        ComboBox3.Name = "ComboBox3"
        ComboBox3.Size = New Size(80, 28)
        ComboBox3.TabIndex = 12
        ' 
        ' Label7
        ' 
        Label7.Anchor = AnchorStyles.None
        Label7.AutoSize = True
        Label7.Location = New Point(8, 196)
        Label7.Name = "Label7"
        Label7.Size = New Size(90, 40)
        Label7.TabIndex = 13
        Label7.Text = "SIGNATURE DIGITALE"
        ' 
        ' txtSignatureDigitale
        ' 
        txtSignatureDigitale.Anchor = AnchorStyles.None
        txtSignatureDigitale.Location = New Point(110, 203)
        txtSignatureDigitale.Name = "txtSignatureDigitale"
        txtSignatureDigitale.Size = New Size(102, 27)
        txtSignatureDigitale.TabIndex = 14
        ' 
        ' Label1
        ' 
        Label1.Anchor = AnchorStyles.None
        Label1.AutoSize = True
        Label1.Location = New Point(7, 0)
        Label1.Name = "Label1"
        Label1.Size = New Size(93, 7)
        Label1.TabIndex = 1
        Label1.Text = "N° CONSTAT"
        ' 
        ' FlowLayoutPanel1
        ' 
        FlowLayoutPanel1.Location = New Point(595, 89)
        FlowLayoutPanel1.Name = "FlowLayoutPanel1"
        FlowLayoutPanel1.Size = New Size(250, 125)
        FlowLayoutPanel1.TabIndex = 1
        ' 
        ' TableLayoutPanel3
        ' 
        TableLayoutPanel3.ColumnCount = 2
        TableLayoutPanel3.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel3.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel3.Controls.Add(Label10, 0, 2)
        TableLayoutPanel3.Controls.Add(cmbObjetVerification, 1, 0)
        TableLayoutPanel3.Controls.Add(txtSnAppareil, 0, 4)
        TableLayoutPanel3.Controls.Add(Label8, 0, 4)
        TableLayoutPanel3.Controls.Add(DateTimePicker2, 1, 1)
        TableLayoutPanel3.Controls.Add(Label11, 0, 0)
        TableLayoutPanel3.Controls.Add(Label12, 0, 1)
        TableLayoutPanel3.Controls.Add(DateTimePicker1, 1, 2)
        TableLayoutPanel3.Controls.Add(txtSnInterne, 1, 3)
        TableLayoutPanel3.Controls.Add(Label9, 0, 3)
        TableLayoutPanel3.Location = New Point(427, 244)
        TableLayoutPanel3.Name = "TableLayoutPanel3"
        TableLayoutPanel3.RowCount = 5
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Percent, 42.3076935F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Percent, 57.6923065F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 47F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 62F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 80F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
        TableLayoutPanel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
        TableLayoutPanel3.Size = New Size(381, 323)
        TableLayoutPanel3.TabIndex = 2
        ' 
        ' Label10
        ' 
        Label10.Anchor = AnchorStyles.None
        Label10.AutoSize = True
        Label10.Location = New Point(29, 136)
        Label10.Name = "Label10"
        Label10.Size = New Size(132, 40)
        Label10.TabIndex = 27
        Label10.Text = "DATE DE MISE EN SERVICE"
        ' 
        ' cmbObjetVerification
        ' 
        cmbObjetVerification.Anchor = AnchorStyles.None
        cmbObjetVerification.FormattingEnabled = True
        cmbObjetVerification.Items.AddRange(New Object() {"1ere mise en service", "périodique", "après maintenance"})
        cmbObjetVerification.Location = New Point(205, 14)
        cmbObjetVerification.Name = "cmbObjetVerification"
        cmbObjetVerification.Size = New Size(161, 28)
        cmbObjetVerification.TabIndex = 30
        ' 
        ' txtSnAppareil
        ' 
        txtSnAppareil.Anchor = AnchorStyles.None
        txtSnAppareil.Location = New Point(205, 269)
        txtSnAppareil.Name = "txtSnAppareil"
        txtSnAppareil.Size = New Size(161, 27)
        txtSnAppareil.TabIndex = 24
        ' 
        ' Label8
        ' 
        Label8.Anchor = AnchorStyles.None
        Label8.AutoSize = True
        Label8.Location = New Point(44, 272)
        Label8.Name = "Label8"
        Label8.Size = New Size(101, 20)
        Label8.TabIndex = 23
        Label8.Text = "S/N APPAREIL"
        ' 
        ' DateTimePicker2
        ' 
        DateTimePicker2.Anchor = AnchorStyles.None
        DateTimePicker2.CustomFormat = "ddMMMMyyyy"
        DateTimePicker2.Format = DateTimePickerFormat.Custom
        DateTimePicker2.Location = New Point(205, 81)
        DateTimePicker2.Name = "DateTimePicker2"
        DateTimePicker2.Size = New Size(161, 27)
        DateTimePicker2.TabIndex = 32
        ' 
        ' Label11
        ' 
        Label11.Anchor = AnchorStyles.None
        Label11.AutoSize = True
        Label11.Location = New Point(44, 8)
        Label11.Name = "Label11"
        Label11.Size = New Size(102, 40)
        Label11.TabIndex = 29
        Label11.Text = "OBJET DE LA VERIFICATION"
        ' 
        ' Label12
        ' 
        Label12.Anchor = AnchorStyles.None
        Label12.AutoSize = True
        Label12.Location = New Point(44, 74)
        Label12.Name = "Label12"
        Label12.Size = New Size(102, 40)
        Label12.TabIndex = 31
        Label12.Text = "DATE DE LA VERIFICATION"
        ' 
        ' DateTimePicker1
        ' 
        DateTimePicker1.Anchor = AnchorStyles.None
        DateTimePicker1.CustomFormat = "ddMMMMyyyy"
        DateTimePicker1.Format = DateTimePickerFormat.Custom
        DateTimePicker1.Location = New Point(205, 143)
        DateTimePicker1.Name = "DateTimePicker1"
        DateTimePicker1.Size = New Size(161, 27)
        DateTimePicker1.TabIndex = 28
        ' 
        ' txtSnInterne
        ' 
        txtSnInterne.Anchor = AnchorStyles.None
        txtSnInterne.Location = New Point(205, 197)
        txtSnInterne.Name = "txtSnInterne"
        txtSnInterne.Size = New Size(161, 27)
        txtSnInterne.TabIndex = 26
        ' 
        ' Label9
        ' 
        Label9.Anchor = AnchorStyles.None
        Label9.AutoSize = True
        Label9.Location = New Point(21, 201)
        Label9.Name = "Label9"
        Label9.Size = New Size(148, 20)
        Label9.TabIndex = 25
        Label9.Text = "S/N INTERNE CLIENT"
        ' 
        ' test
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        Controls.Add(TableLayoutPanel3)
        Controls.Add(FlowLayoutPanel1)
        Controls.Add(TableLayoutPanel1)
        Name = "test"
        Size = New Size(1137, 703)
        TableLayoutPanel1.ResumeLayout(False)
        TableLayoutPanel1.PerformLayout()
        TableLayoutPanel2.ResumeLayout(False)
        TableLayoutPanel2.PerformLayout()
        TableLayoutPanel3.ResumeLayout(False)
        TableLayoutPanel3.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents Label1 As Label
    Friend WithEvents txtNoConstat As TextBox
    Friend WithEvents Label4 As Label
    Friend WithEvents txtNomClient As TextBox
    Friend WithEvents Label5 As Label
    Friend WithEvents txtAdresse1 As TextBox
    Friend WithEvents txtAdresse2 As TextBox
    Friend WithEvents txtCP As TextBox
    Friend WithEvents TableLayoutPanel2 As TableLayoutPanel
    Friend WithEvents txtVille As TextBox
    Friend WithEvents Label6 As Label
    Friend WithEvents ComboBox3 As ComboBox
    Friend WithEvents Label7 As Label
    Friend WithEvents txtSignatureDigitale As TextBox
    Friend WithEvents FlowLayoutPanel1 As FlowLayoutPanel
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

End Class
