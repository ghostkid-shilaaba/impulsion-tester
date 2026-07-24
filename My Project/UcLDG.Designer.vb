<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class UcLDG
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
        Label1 = New Label()
        Label2 = New Label()
        GroupBox1 = New GroupBox()
        TableLayoutPanel2 = New TableLayoutPanel()
        TextBox2 = New TextBox()
        Label4 = New Label()
        TableLayoutPanel1 = New TableLayoutPanel()
        TextBox1 = New TextBox()
        Label3 = New Label()
        Label5 = New Label()
        Button1 = New Button()
        Button2 = New Button()
        GroupBox2 = New GroupBox()
        GroupBox1.SuspendLayout()
        TableLayoutPanel2.SuspendLayout()
        TableLayoutPanel1.SuspendLayout()
        SuspendLayout()
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(60, 227)
        Label1.Name = "Label1"
        Label1.Size = New Size(287, 15)
        Label1.TabIndex = 0
        Label1.Text = "Régler le gain de l'appareil sur une valeur mini : 10 dB"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Font = New Font("Segoe UI Semibold", 12F, FontStyle.Bold Or FontStyle.Underline, GraphicsUnit.Point, CByte(0))
        Label2.Location = New Point(60, 35)
        Label2.Name = "Label2"
        Label2.RightToLeft = RightToLeft.No
        Label2.Size = New Size(155, 21)
        Label2.TabIndex = 5
        Label2.Text = "LINEARITE DU GAIN"
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(TableLayoutPanel2)
        GroupBox1.Controls.Add(TableLayoutPanel1)
        GroupBox1.Location = New Point(23, 262)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Size = New Size(996, 100)
        GroupBox1.TabIndex = 6
        GroupBox1.TabStop = False
        ' 
        ' TableLayoutPanel2
        ' 
        TableLayoutPanel2.ColumnCount = 2
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.Controls.Add(TextBox2, 1, 0)
        TableLayoutPanel2.Controls.Add(Label4, 0, 0)
        TableLayoutPanel2.Location = New Point(507, 36)
        TableLayoutPanel2.Name = "TableLayoutPanel2"
        TableLayoutPanel2.RowCount = 1
        TableLayoutPanel2.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel2.Size = New Size(454, 41)
        TableLayoutPanel2.TabIndex = 8
        ' 
        ' TextBox2
        ' 
        TextBox2.Anchor = AnchorStyles.None
        TextBox2.Location = New Point(270, 9)
        TextBox2.Name = "TextBox2"
        TextBox2.Size = New Size(140, 23)
        TextBox2.TabIndex = 8
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
        TableLayoutPanel1.Controls.Add(TextBox1, 1, 0)
        TableLayoutPanel1.Controls.Add(Label3, 0, 0)
        TableLayoutPanel1.Location = New Point(43, 36)
        TableLayoutPanel1.Name = "TableLayoutPanel1"
        TableLayoutPanel1.RowCount = 1
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 50F))
        TableLayoutPanel1.Size = New Size(454, 41)
        TableLayoutPanel1.TabIndex = 7
        ' 
        ' TextBox1
        ' 
        TextBox1.Anchor = AnchorStyles.None
        TextBox1.Location = New Point(270, 9)
        TextBox1.Name = "TextBox1"
        TextBox1.Size = New Size(140, 23)
        TextBox1.TabIndex = 7
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
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(60, 398)
        Label5.Name = "Label5"
        Label5.Size = New Size(246, 15)
        Label5.TabIndex = 7
        Label5.Text = "Régler le signal à 80% de la hauteur de l'écran"
        ' 
        ' Button1
        ' 
        Button1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        Button1.Location = New Point(904, 491)
        Button1.Name = "Button1"
        Button1.Size = New Size(75, 23)
        Button1.TabIndex = 8
        Button1.Text = "Suivant"
        Button1.UseVisualStyleBackColor = True
        ' 
        ' Button2
        ' 
        Button2.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Button2.Location = New Point(24, 495)
        Button2.Name = "Button2"
        Button2.Size = New Size(75, 23)
        Button2.TabIndex = 9
        Button2.Text = "Précédent"
        Button2.UseVisualStyleBackColor = True
        ' 
        ' GroupBox2
        ' 
        GroupBox2.Location = New Point(60, 71)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Size = New Size(377, 135)
        GroupBox2.TabIndex = 10
        GroupBox2.TabStop = False
        GroupBox2.Text = "Réglages"
        ' 
        ' UcLDG
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        Controls.Add(GroupBox2)
        Controls.Add(Button2)
        Controls.Add(Button1)
        Controls.Add(Label5)
        Controls.Add(GroupBox1)
        Controls.Add(Label2)
        Controls.Add(Label1)
        Name = "UcLDG"
        Size = New Size(995, 527)
        GroupBox1.ResumeLayout(False)
        TableLayoutPanel2.ResumeLayout(False)
        TableLayoutPanel2.PerformLayout()
        TableLayoutPanel1.ResumeLayout(False)
        TableLayoutPanel1.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents TableLayoutPanel2 As TableLayoutPanel
    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents Label3 As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents TextBox2 As TextBox
    Friend WithEvents TextBox1 As TextBox
    Friend WithEvents Label5 As Label
    Friend WithEvents Button1 As Button
    Friend WithEvents Button2 As Button
    Friend WithEvents GroupBox2 As GroupBox

End Class
