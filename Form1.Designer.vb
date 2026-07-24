<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        pnlConteneur = New Panel()
        SuspendLayout()
        ' 
        ' pnlConteneur
        ' 
        pnlConteneur.Dock = DockStyle.Fill
        pnlConteneur.Location = New Point(0, 0)
        pnlConteneur.Name = "pnlConteneur"
        pnlConteneur.Size = New Size(1119, 656)
        pnlConteneur.TabIndex = 0
        ' 
        ' Form1
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1119, 656)
        Controls.Add(pnlConteneur)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "Form1"
        StartPosition = FormStartPosition.CenterScreen
        Text = "LogicielBancDeTest"
        ResumeLayout(False)
    End Sub

    Friend WithEvents pnlConteneur As Panel

End Class
