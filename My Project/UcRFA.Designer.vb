<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class UcRFA
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
        BtnPrecedent = New Button()
        BtnSuivant = New Button()
        Label2 = New Label()
        Label1 = New Label()
        cmbFiltre = New ComboBox()
        TableLayoutPanel2 = New TableLayoutPanel()
        txtTension = New TextBox()
        Label4 = New Label()
        btnArreter = New Button()
        btnAcquerir = New Button()
        btnThreeDots = New Button()
        cmsAppareils = New ContextMenuStrip(components)
        TableLayoutPanel2.SuspendLayout()
        SuspendLayout()
        ' 
        ' BtnPrecedent
        ' 
        BtnPrecedent.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        BtnPrecedent.Location = New Point(22, 492)
        BtnPrecedent.Name = "BtnPrecedent"
        BtnPrecedent.Size = New Size(75, 23)
        BtnPrecedent.TabIndex = 11
        BtnPrecedent.Text = "Précédent"
        BtnPrecedent.UseVisualStyleBackColor = True
        ' 
        ' BtnSuivant
        ' 
        BtnSuivant.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        BtnSuivant.Location = New Point(902, 488)
        BtnSuivant.Name = "BtnSuivant"
        BtnSuivant.Size = New Size(75, 23)
        BtnSuivant.TabIndex = 10
        BtnSuivant.Text = "Suivant"
        BtnSuivant.UseVisualStyleBackColor = True
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Font = New Font("Segoe UI Semibold", 12F, FontStyle.Bold Or FontStyle.Underline, GraphicsUnit.Point, CByte(0))
        Label2.Location = New Point(22, 14)
        Label2.Name = "Label2"
        Label2.RightToLeft = RightToLeft.No
        Label2.Size = New Size(351, 21)
        Label2.TabIndex = 12
        Label2.Text = "REPONSE EN FREQUENCE DE L'AMPLIFICATEUR"
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(22, 88)
        Label1.Name = "Label1"
        Label1.Size = New Size(151, 15)
        Label1.TabIndex = 13
        Label1.Text = "Choisissez le filtre a vérifier:"
        ' 
        ' cmbFiltre
        ' 
        cmbFiltre.FormattingEnabled = True
        cmbFiltre.Location = New Point(210, 85)
        cmbFiltre.Name = "cmbFiltre"
        cmbFiltre.Size = New Size(163, 23)
        cmbFiltre.TabIndex = 14
        ' 
        ' TableLayoutPanel2
        ' 
        TableLayoutPanel2.ColumnCount = 2
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.Controls.Add(txtTension, 1, 0)
        TableLayoutPanel2.Controls.Add(Label4, 0, 0)
        TableLayoutPanel2.Location = New Point(125, 194)
        TableLayoutPanel2.Name = "TableLayoutPanel2"
        TableLayoutPanel2.RowCount = 1
        TableLayoutPanel2.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
        TableLayoutPanel2.Size = New Size(454, 41)
        TableLayoutPanel2.TabIndex = 15
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
        ' btnArreter
        ' 
        btnArreter.Location = New Point(200, 311)
        btnArreter.Margin = New Padding(3, 2, 3, 2)
        btnArreter.Name = "btnArreter"
        btnArreter.Size = New Size(82, 22)
        btnArreter.TabIndex = 21
        btnArreter.Text = "Arrêter"
        btnArreter.UseVisualStyleBackColor = True
        ' 
        ' btnAcquerir
        ' 
        btnAcquerir.Location = New Point(91, 311)
        btnAcquerir.Margin = New Padding(3, 2, 3, 2)
        btnAcquerir.Name = "btnAcquerir"
        btnAcquerir.Size = New Size(82, 22)
        btnAcquerir.TabIndex = 20
        btnAcquerir.Text = "Acquerir"
        btnAcquerir.UseVisualStyleBackColor = True
        ' 
        ' btnThreeDots
        ' 
        btnThreeDots.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnThreeDots.ContextMenuStrip = cmsAppareils
        btnThreeDots.Location = New Point(967, 3)
        btnThreeDots.Name = "btnThreeDots"
        btnThreeDots.Size = New Size(25, 23)
        btnThreeDots.TabIndex = 22
        btnThreeDots.Text = "⋮"
        btnThreeDots.UseVisualStyleBackColor = True
        ' 
        ' cmsAppareils
        ' 
        cmsAppareils.Name = "cmsAppareils"
        cmsAppareils.Size = New Size(61, 4)
        ' 
        ' UcRFA
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        Controls.Add(btnThreeDots)
        Controls.Add(btnArreter)
        Controls.Add(btnAcquerir)
        Controls.Add(TableLayoutPanel2)
        Controls.Add(cmbFiltre)
        Controls.Add(Label1)
        Controls.Add(Label2)
        Controls.Add(BtnPrecedent)
        Controls.Add(BtnSuivant)
        Name = "UcRFA"
        Size = New Size(995, 527)
        TableLayoutPanel2.ResumeLayout(False)
        TableLayoutPanel2.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents BtnPrecedent As Button
    Friend WithEvents BtnSuivant As Button
    Friend WithEvents Label2 As Label
    Friend WithEvents Label1 As Label
    Friend WithEvents cmbFiltre As ComboBox
    Friend WithEvents TableLayoutPanel2 As TableLayoutPanel
    Friend WithEvents txtTension As TextBox
    Friend WithEvents Label4 As Label
    Friend WithEvents btnArreter As Button
    Friend WithEvents btnAcquerir As Button
    Friend WithEvents btnThreeDots As Button
    Friend WithEvents cmsAppareils As ContextMenuStrip

End Class
