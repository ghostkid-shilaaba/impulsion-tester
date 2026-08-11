<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class RfaResultsForm
    Inherits System.Windows.Forms.Form

    'Form remplace la méthode Dispose pour nettoyer la liste des composants.
    <System.Diagnostics.DebuggerNonUserCode()>
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

    'REMARQUE : la procédure suivante est requise par le Concepteur Windows Form
    'Elle peut être modifiée à l'aide du Concepteur Windows Form.
    'Ne la modifiez pas à l'aide de l'éditeur de code.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        dataGridViewRFA = New DataGridView()
        pnlResume = New Panel()
        lblConclusion = New Label()
        lblResume = New Label()
        CType(dataGridViewRFA, ComponentModel.ISupportInitialize).BeginInit()
        pnlResume.SuspendLayout()
        SuspendLayout()
        '
        ' dataGridViewRFA
        '
        dataGridViewRFA.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dataGridViewRFA.Dock = DockStyle.Fill
        dataGridViewRFA.Location = New Point(0, 0)
        dataGridViewRFA.Name = "dataGridViewRFA"
        dataGridViewRFA.Size = New Size(900, 420)
        dataGridViewRFA.TabIndex = 0
        '
        ' pnlResume
        '
        pnlResume.Controls.Add(lblConclusion)
        pnlResume.Controls.Add(lblResume)
        pnlResume.Dock = DockStyle.Bottom
        pnlResume.Location = New Point(0, 420)
        pnlResume.Name = "pnlResume"
        pnlResume.Padding = New Padding(12)
        pnlResume.Size = New Size(900, 150)
        pnlResume.TabIndex = 1
        '
        ' lblResume
        '
        lblResume.Dock = DockStyle.Top
        lblResume.Font = New Font("Segoe UI", 9.5!)
        lblResume.Location = New Point(12, 12)
        lblResume.Name = "lblResume"
        lblResume.Size = New Size(876, 90)
        lblResume.TabIndex = 0
        lblResume.Text = ""
        '
        ' lblConclusion
        '
        lblConclusion.Dock = DockStyle.Bottom
        lblConclusion.Font = New Font("Segoe UI Semibold", 11.0F, FontStyle.Bold)
        lblConclusion.Location = New Point(12, 108)
        lblConclusion.Name = "lblConclusion"
        lblConclusion.Size = New Size(876, 30)
        lblConclusion.TabIndex = 1
        lblConclusion.Text = ""
        lblConclusion.TextAlign = ContentAlignment.MiddleLeft
        '
        ' RfaResultsForm
        '
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(900, 570)
        Controls.Add(dataGridViewRFA)
        Controls.Add(pnlResume)
        Name = "RfaResultsForm"
        Text = "RfaResultsForm"
        CType(dataGridViewRFA, ComponentModel.ISupportInitialize).EndInit()
        pnlResume.ResumeLayout(False)
        ResumeLayout(False)
    End Sub

    Friend WithEvents dataGridViewRFA As DataGridView
    Friend WithEvents pnlResume As Panel
    Friend WithEvents lblResume As Label
    Friend WithEvents lblConclusion As Label
End Class
