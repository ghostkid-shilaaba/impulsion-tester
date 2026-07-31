<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class UcImpulsion
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
        TableLayoutPanel1 = New TableLayoutPanel()
        cmbNbImpulsions = New ComboBox()
        Label1 = New Label()
        d = New Label()
        dgvImpulsions = New DataGridView()
        btnAcquerir = New Button()
        Label2 = New Label()
        btnArreter = New Button()
        btnPrecedent = New Button()
        btnSuivant = New Button()
        btnThreeDots = New Button()
        cmsAppareils = New ContextMenuStrip(components)
        Label3 = New Label()
        TableLayoutPanel1.SuspendLayout()
        CType(dgvImpulsions, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' TableLayoutPanel1
        ' 
        TableLayoutPanel1.ColumnCount = 2
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.Controls.Add(cmbNbImpulsions, 1, 0)
        TableLayoutPanel1.Controls.Add(Label1, 0, 0)
        TableLayoutPanel1.Location = New Point(33, 77)
        TableLayoutPanel1.Margin = New Padding(3, 2, 3, 2)
        TableLayoutPanel1.Name = "TableLayoutPanel1"
        TableLayoutPanel1.RowCount = 1
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
        TableLayoutPanel1.Size = New Size(509, 68)
        TableLayoutPanel1.TabIndex = 0
        ' 
        ' cmbNbImpulsions
        ' 
        cmbNbImpulsions.Anchor = AnchorStyles.None
        cmbNbImpulsions.FormattingEnabled = True
        cmbNbImpulsions.Items.AddRange(New Object() {"1", "2", "3", "4", "5", "6", "7", "8"})
        cmbNbImpulsions.Location = New Point(315, 22)
        cmbNbImpulsions.Margin = New Padding(3, 2, 3, 2)
        cmbNbImpulsions.Name = "cmbNbImpulsions"
        cmbNbImpulsions.Size = New Size(133, 23)
        cmbNbImpulsions.TabIndex = 1
        ' 
        ' Label1
        ' 
        Label1.Anchor = AnchorStyles.Left
        Label1.AutoSize = True
        Label1.Location = New Point(3, 26)
        Label1.Name = "Label1"
        Label1.Size = New Size(223, 15)
        Label1.TabIndex = 1
        Label1.Text = "Entrer le nombre d'impulsions à vérifier : "
        ' 
        ' d
        ' 
        d.AllowDrop = True
        d.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        d.Font = New Font("Segoe UI", 10.8F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        d.ForeColor = Color.Red
        d.Location = New Point(3, 165)
        d.Name = "d"
        d.Padding = New Padding(9, 0, 0, 0)
        d.Size = New Size(1022, 40)
        d.TabIndex = 1
        d.Text = " Avant de connecter la prise T/R de l'appareil, s'assurer d'avoir protégé l'entrée de l'oscilloscope avec un atténuateur calibré d'une valeur  40 dB."
        d.UseCompatibleTextRendering = True
        ' 
        ' dgvImpulsions
        ' 
        dgvImpulsions.AllowUserToAddRows = False
        dgvImpulsions.AllowUserToDeleteRows = False
        dgvImpulsions.AllowUserToOrderColumns = True
        dgvImpulsions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dgvImpulsions.Location = New Point(33, 224)
        dgvImpulsions.Margin = New Padding(3, 2, 3, 2)
        dgvImpulsions.Name = "dgvImpulsions"
        dgvImpulsions.RowHeadersWidth = 51
        dgvImpulsions.Size = New Size(542, 212)
        dgvImpulsions.TabIndex = 2
        ' 
        ' btnAcquerir
        ' 
        btnAcquerir.Location = New Point(602, 266)
        btnAcquerir.Margin = New Padding(3, 2, 3, 2)
        btnAcquerir.Name = "btnAcquerir"
        btnAcquerir.Size = New Size(82, 22)
        btnAcquerir.TabIndex = 3
        btnAcquerir.Text = "Acquerir"
        btnAcquerir.UseVisualStyleBackColor = True
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Font = New Font("Segoe UI Semibold", 12F, FontStyle.Bold Or FontStyle.Underline, GraphicsUnit.Point, CByte(0))
        Label2.Location = New Point(33, 27)
        Label2.Name = "Label2"
        Label2.Size = New Size(190, 21)
        Label2.TabIndex = 4
        Label2.Text = "IMPULSION D'EMISSION"
        ' 
        ' btnArreter
        ' 
        btnArreter.Location = New Point(602, 304)
        btnArreter.Margin = New Padding(3, 2, 3, 2)
        btnArreter.Name = "btnArreter"
        btnArreter.Size = New Size(82, 22)
        btnArreter.TabIndex = 5
        btnArreter.Text = "Arrêter"
        btnArreter.UseVisualStyleBackColor = True
        ' 
        ' btnPrecedent
        ' 
        btnPrecedent.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        btnPrecedent.Location = New Point(3, 501)
        btnPrecedent.Name = "btnPrecedent"
        btnPrecedent.Size = New Size(75, 23)
        btnPrecedent.TabIndex = 11
        btnPrecedent.Text = "Précédent"
        btnPrecedent.UseVisualStyleBackColor = True
        ' 
        ' btnSuivant
        ' 
        btnSuivant.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnSuivant.Location = New Point(917, 501)
        btnSuivant.Name = "btnSuivant"
        btnSuivant.Size = New Size(75, 23)
        btnSuivant.TabIndex = 10
        btnSuivant.Text = "Suivant"
        btnSuivant.UseVisualStyleBackColor = True
        ' 
        ' btnThreeDots
        ' 
        btnThreeDots.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnThreeDots.ContextMenuStrip = cmsAppareils
        btnThreeDots.Location = New Point(967, 3)
        btnThreeDots.Name = "btnThreeDots"
        btnThreeDots.Size = New Size(25, 23)
        btnThreeDots.TabIndex = 12
        btnThreeDots.Text = "⋮"
        btnThreeDots.UseVisualStyleBackColor = True
        ' 
        ' cmsAppareils
        ' 
        cmsAppareils.Name = "cmsAppareils"
        cmsAppareils.Size = New Size(61, 4)
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(36, 460)
        Label3.Name = "Label3"
        Label3.Size = New Size(201, 15)
        Label3.TabIndex = 13
        Label3.Text = "Vérifiez les réglages de l'oscilloscope."
        ' 
        ' UcImpulsion
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        Controls.Add(Label3)
        Controls.Add(btnThreeDots)
        Controls.Add(btnPrecedent)
        Controls.Add(btnSuivant)
        Controls.Add(btnArreter)
        Controls.Add(Label2)
        Controls.Add(btnAcquerir)
        Controls.Add(dgvImpulsions)
        Controls.Add(d)
        Controls.Add(TableLayoutPanel1)
        Margin = New Padding(3, 2, 3, 2)
        Name = "UcImpulsion"
        Size = New Size(995, 527)
        TableLayoutPanel1.ResumeLayout(False)
        TableLayoutPanel1.PerformLayout()
        CType(dgvImpulsions, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents cmbNbImpulsions As ComboBox
    Friend WithEvents Label1 As Label
    Friend WithEvents d As Label
    Friend WithEvents dgvImpulsions As DataGridView
    Friend WithEvents btnAcquerir As Button
    Friend WithEvents Label2 As Label
    Friend WithEvents btnArreter As Button
    Friend WithEvents btnPrecedent As Button
    Friend WithEvents btnSuivant As Button
    Friend WithEvents btnThreeDots As Button
    Friend WithEvents cmsAppareils As ContextMenuStrip
    Friend WithEvents Label3 As Label

End Class
