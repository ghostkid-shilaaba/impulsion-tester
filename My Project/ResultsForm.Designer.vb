<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ResultsForm
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
        flowPanelGraphs = New FlowLayoutPanel()
        dataGridViewResults = New DataGridView()
        CType(dataGridViewResults, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' flowPanelGraphs
        ' 
        flowPanelGraphs.Dock = DockStyle.Fill
        flowPanelGraphs.Location = New Point(0, 0)
        flowPanelGraphs.Name = "flowPanelGraphs"
        flowPanelGraphs.Size = New Size(800, 450)
        flowPanelGraphs.TabIndex = 0
        ' 
        ' dataGridViewResults
        ' 
        dataGridViewResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dataGridViewResults.Dock = DockStyle.Bottom
        dataGridViewResults.Location = New Point(0, 300)
        dataGridViewResults.Name = "dataGridViewResults"
        dataGridViewResults.Size = New Size(800, 150)
        dataGridViewResults.TabIndex = 1
        ' 
        ' ResultsForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 450)
        Controls.Add(dataGridViewResults)
        Controls.Add(flowPanelGraphs)
        Name = "ResultsForm"
        Text = "ResultsForm"
        CType(dataGridViewResults, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents flowPanelGraphs As FlowLayoutPanel
    Friend WithEvents dataGridViewResults As DataGridView
End Class
