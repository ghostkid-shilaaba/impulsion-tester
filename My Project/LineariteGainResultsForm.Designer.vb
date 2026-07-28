<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class LineariteGainResultsForm
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
        dataGridViewGain = New DataGridView()
        CType(dataGridViewGain, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' dataGridViewGain
        ' 
        dataGridViewGain.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dataGridViewGain.Dock = DockStyle.Fill
        dataGridViewGain.Location = New Point(0, 0)
        dataGridViewGain.Name = "dataGridViewGain"
        dataGridViewGain.Size = New Size(800, 450)
        dataGridViewGain.TabIndex = 0
        ' 
        ' LineariteGainResultsForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 450)
        Controls.Add(dataGridViewGain)
        Name = "LineariteGainResultsForm"
        Text = "LineariteGainResultsForm"
        CType(dataGridViewGain, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents dataGridViewGain As DataGridView
End Class
