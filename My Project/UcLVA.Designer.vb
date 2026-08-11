<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class UcLVA
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
        components = New ComponentModel.Container()
        GroupBox1 = New GroupBox()
        TableLayoutPanel2 = New TableLayoutPanel()
        txtTension = New TextBox()
        Label4 = New Label()
        TableLayoutPanel1 = New TableLayoutPanel()
        txtFreq = New TextBox()
        Label3 = New Label()
        Label2 = New Label()
        btnThreeDots = New Button()
        cmsAppareils = New ContextMenuStrip(components)
        BtnPrecedent = New Button()
        BtnSuivant = New Button()
        btnArreter = New Button()
        btnAcquerir = New Button()
        GroupBox1.SuspendLayout()
        TableLayoutPanel2.SuspendLayout()
        TableLayoutPanel1.SuspendLayout()
        SuspendLayout()
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(TableLayoutPanel2)
        GroupBox1.Controls.Add(TableLayoutPanel1)
        GroupBox1.Location = New Point(3, 161)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Size = New Size(996, 100)
        GroupBox1.TabIndex = 10
        GroupBox1.TabStop = False
        ' 
        ' TableLayoutPanel2
        ' 
        TableLayoutPanel2.ColumnCount = 2
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.Controls.Add(txtTension, 1, 0)
        TableLayoutPanel2.Controls.Add(Label4, 0, 0)
        TableLayoutPanel2.Location = New Point(507, 36)
        TableLayoutPanel2.Name = "TableLayoutPanel2"
        TableLayoutPanel2.RowCount = 1
        TableLayoutPanel2.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
        TableLayoutPanel2.Size = New Size(454, 41)
        TableLayoutPanel2.TabIndex = 8
        ' 
        ' txtTension
        ' 
        txtTension.Anchor = AnchorStyles.None
        txtTension.Location = New Point(270, 9)
        txtTension.Name = "txtTension"
        txtTension.Size = New Size(140, 23)
        txtTension.TabIndex = 8
        ' 
        ' Label4
        ' 
        Label4.Anchor = AnchorStyles.Left
        Label4.AutoSize = True
        Label4.Location = New Point(3, 13)
        Label4.Name = "Label4"
        Label4.Size = New Size(123, 15)
        Label4.TabIndex = 7
        Label4.Text = "Tension Vcc à envoyer"
        ' 
        ' TableLayoutPanel1
        ' 
        TableLayoutPanel1.ColumnCount = 2
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.Controls.Add(txtFreq, 1, 0)
        TableLayoutPanel1.Controls.Add(Label3, 0, 0)
        TableLayoutPanel1.Location = New Point(43, 36)
        TableLayoutPanel1.Name = "TableLayoutPanel1"
        TableLayoutPanel1.RowCount = 1
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.Size = New Size(454, 41)
        TableLayoutPanel1.TabIndex = 7
        ' 
        ' txtFreq
        ' 
        txtFreq.Anchor = AnchorStyles.None
        txtFreq.Location = New Point(270, 9)
        txtFreq.Name = "txtFreq"
        txtFreq.Size = New Size(140, 23)
        txtFreq.TabIndex = 7
        ' 
        ' Label3
        ' 
        Label3.Anchor = AnchorStyles.Left
        Label3.AutoSize = True
        Label3.Location = New Point(3, 13)
        Label3.Name = "Label3"
        Label3.Size = New Size(131, 15)
        Label3.TabIndex = 7
        Label3.Text = "Fréquence F0 à envoyer"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Font = New Font("Segoe UI Semibold", 12F, FontStyle.Bold Or FontStyle.Underline, GraphicsUnit.Point, CByte(0))
        Label2.Location = New Point(45, 32)
        Label2.Name = "Label2"
        Label2.RightToLeft = RightToLeft.No
        Label2.Size = New Size(264, 21)
        Label2.TabIndex = 9
        Label2.Text = "LINEARITE VERTICAL D'AFFICHAGE"
        ' 
        ' btnThreeDots
        ' 
        btnThreeDots.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnThreeDots.ContextMenuStrip = cmsAppareils
        btnThreeDots.Location = New Point(954, 30)
        btnThreeDots.Name = "btnThreeDots"
        btnThreeDots.Size = New Size(25, 23)
        btnThreeDots.TabIndex = 14
        btnThreeDots.Text = "⋮"
        btnThreeDots.UseVisualStyleBackColor = True
        ' 
        ' cmsAppareils
        ' 
        cmsAppareils.Name = "cmsAppareils"
        cmsAppareils.Size = New Size(181, 26)
        ' 
        ' BtnPrecedent
        ' 
        BtnPrecedent.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        BtnPrecedent.Location = New Point(23, 501)
        BtnPrecedent.Name = "BtnPrecedent"
        BtnPrecedent.Size = New Size(75, 23)
        BtnPrecedent.TabIndex = 17
        BtnPrecedent.Text = "Précédent"
        BtnPrecedent.UseVisualStyleBackColor = True
        ' 
        ' BtnSuivant
        ' 
        BtnSuivant.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        BtnSuivant.Location = New Point(904, 501)
        BtnSuivant.Name = "BtnSuivant"
        BtnSuivant.Size = New Size(75, 23)
        BtnSuivant.TabIndex = 16
        BtnSuivant.Text = "Suivant"
        BtnSuivant.UseVisualStyleBackColor = True
        ' 
        ' btnArreter
        ' 
        btnArreter.Location = New Point(228, 355)
        btnArreter.Margin = New Padding(3, 2, 3, 2)
        btnArreter.Name = "btnArreter"
        btnArreter.Size = New Size(82, 22)
        btnArreter.TabIndex = 19
        btnArreter.Text = "Arrêter"
        btnArreter.UseVisualStyleBackColor = True
        ' 
        ' btnAcquerir
        ' 
        btnAcquerir.Location = New Point(119, 355)
        btnAcquerir.Margin = New Padding(3, 2, 3, 2)
        btnAcquerir.Name = "btnAcquerir"
        btnAcquerir.Size = New Size(82, 22)
        btnAcquerir.TabIndex = 18
        btnAcquerir.Text = "Acquerir"
        btnAcquerir.UseVisualStyleBackColor = True
        ' 
        ' UcLVA
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        Controls.Add(btnArreter)
        Controls.Add(btnAcquerir)
        Controls.Add(BtnPrecedent)
        Controls.Add(BtnSuivant)
        Controls.Add(btnThreeDots)
        Controls.Add(GroupBox1)
        Controls.Add(Label2)
        Name = "UcLVA"
        Size = New Size(995, 527)
        GroupBox1.ResumeLayout(False)
        TableLayoutPanel2.ResumeLayout(False)
        TableLayoutPanel2.PerformLayout()
        TableLayoutPanel1.ResumeLayout(False)
        TableLayoutPanel1.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents TableLayoutPanel2 As TableLayoutPanel
    Friend WithEvents txtTension As TextBox
    Friend WithEvents Label4 As Label
    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents txtFreq As TextBox
    Friend WithEvents Label3 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents btnThreeDots As Button
    Friend WithEvents cmsAppareils As ContextMenuStrip
    Friend WithEvents BtnPrecedent As Button
    Friend WithEvents BtnSuivant As Button
    Friend WithEvents btnArreter As Button
    Friend WithEvents btnAcquerir As Button

End Class
