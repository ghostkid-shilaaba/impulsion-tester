<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class IMPForm
    Inherits System.Windows.Forms.Form

    'Form remplace la méthode Dispose pour nettoyer la liste des composants.
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
        dgvImpulsions = New DataGridView()
        cmbNbImpulsions = New ComboBox()
        btnAnnuler = New Button()
        btnEnregistrer = New Button()
        CType(dgvImpulsions, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' dgvImpulsions
        ' 
        dgvImpulsions.AllowUserToAddRows = False
        dgvImpulsions.AllowUserToDeleteRows = False
        dgvImpulsions.AllowUserToOrderColumns = True
        dgvImpulsions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dgvImpulsions.Location = New Point(12, 11)
        dgvImpulsions.Margin = New Padding(3, 2, 3, 2)
        dgvImpulsions.Name = "dgvImpulsions"
        dgvImpulsions.RowHeadersWidth = 51
        dgvImpulsions.Size = New Size(542, 212)
        dgvImpulsions.TabIndex = 3
        ' 
        ' cmbNbImpulsions
        ' 
        cmbNbImpulsions.FormattingEnabled = True
        cmbNbImpulsions.Items.AddRange(New Object() {"1", "2", "3", "4", "5", "6", "7", "8"})
        cmbNbImpulsions.Location = New Point(569, 27)
        cmbNbImpulsions.Name = "cmbNbImpulsions"
        cmbNbImpulsions.Size = New Size(121, 23)
        cmbNbImpulsions.TabIndex = 4
        ' 
        ' btnAnnuler
        ' 
        btnAnnuler.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnAnnuler.Location = New Point(608, 238)
        btnAnnuler.Margin = New Padding(3, 2, 3, 2)
        btnAnnuler.Name = "btnAnnuler"
        btnAnnuler.Size = New Size(82, 22)
        btnAnnuler.TabIndex = 6
        btnAnnuler.Text = "Annuler"
        btnAnnuler.UseVisualStyleBackColor = True
        ' 
        ' btnEnregistrer
        ' 
        btnEnregistrer.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnEnregistrer.Location = New Point(512, 238)
        btnEnregistrer.Margin = New Padding(3, 2, 3, 2)
        btnEnregistrer.Name = "btnEnregistrer"
        btnEnregistrer.Size = New Size(82, 22)
        btnEnregistrer.TabIndex = 5
        btnEnregistrer.Text = "Enregistrer"
        btnEnregistrer.UseVisualStyleBackColor = True
        ' 
        ' IMPForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(702, 271)
        Controls.Add(btnAnnuler)
        Controls.Add(btnEnregistrer)
        Controls.Add(cmbNbImpulsions)
        Controls.Add(dgvImpulsions)
        Name = "IMPForm"
        Text = "IMPForm"
        CType(dgvImpulsions, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents dgvImpulsions As DataGridView
    Friend WithEvents cmbNbImpulsions As ComboBox
    Friend WithEvents btnAnnuler As Button
    Friend WithEvents btnEnregistrer As Button
End Class
