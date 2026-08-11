<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormGestionAppareils
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
        GroupBox1 = New GroupBox()
        txtNouveauFabricant = New TextBox()
        btnSupprimerFab = New Button()
        btnAjouterFab = New Button()
        Label1 = New Label()
        cmbFabGestion = New ComboBox()
        Label2 = New Label()
        btnAjouterMod = New Button()
        Label3 = New Label()
        cmbModGestion = New ComboBox()
        Label4 = New Label()
        GroupBox2 = New GroupBox()
        txtNouveauModele = New TextBox()
        btnSupprimerMod = New Button()
        GroupBox3 = New GroupBox()
        bttnAj = New Button()
        bttnSup = New Button()
        cmbFiltre = New ComboBox()
        BttnImpulsion = New Button()
        Label15 = New Label()
        txtFreq = New TextBox()
        Label14 = New Label()
        txtGain = New TextBox()
        cmbMode = New ComboBox()
        txtEchelle = New TextBox()
        txtAmortissement = New TextBox()
        txtPRF = New TextBox()
        cmbRedressement = New ComboBox()
        cmbSignal = New ComboBox()
        Label12 = New Label()
        Label11 = New Label()
        Label10 = New Label()
        Label9 = New Label()
        Label8 = New Label()
        Label7 = New Label()
        Label6 = New Label()
        Label5 = New Label()
        btnEnregistrer = New Button()
        btnAnnuler = New Button()
        GroupBox1.SuspendLayout()
        GroupBox2.SuspendLayout()
        GroupBox3.SuspendLayout()
        SuspendLayout()
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        GroupBox1.Controls.Add(txtNouveauFabricant)
        GroupBox1.Controls.Add(btnSupprimerFab)
        GroupBox1.Controls.Add(btnAjouterFab)
        GroupBox1.Controls.Add(Label1)
        GroupBox1.Controls.Add(cmbFabGestion)
        GroupBox1.Controls.Add(Label2)
        GroupBox1.Location = New Point(10, 17)
        GroupBox1.Margin = New Padding(3, 2, 3, 2)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Padding = New Padding(3, 2, 3, 2)
        GroupBox1.Size = New Size(729, 94)
        GroupBox1.TabIndex = 0
        GroupBox1.TabStop = False
        GroupBox1.Text = "Fabricants"
        ' 
        ' txtNouveauFabricant
        ' 
        txtNouveauFabricant.Location = New Point(164, 60)
        txtNouveauFabricant.Margin = New Padding(3, 2, 3, 2)
        txtNouveauFabricant.Name = "txtNouveauFabricant"
        txtNouveauFabricant.Size = New Size(169, 23)
        txtNouveauFabricant.TabIndex = 12
        ' 
        ' btnSupprimerFab
        ' 
        btnSupprimerFab.Location = New Point(370, 26)
        btnSupprimerFab.Margin = New Padding(3, 2, 3, 2)
        btnSupprimerFab.Name = "btnSupprimerFab"
        btnSupprimerFab.Size = New Size(82, 22)
        btnSupprimerFab.TabIndex = 11
        btnSupprimerFab.Text = "Supprimer"
        btnSupprimerFab.UseVisualStyleBackColor = True
        ' 
        ' btnAjouterFab
        ' 
        btnAjouterFab.Location = New Point(370, 56)
        btnAjouterFab.Margin = New Padding(3, 2, 3, 2)
        btnAjouterFab.Name = "btnAjouterFab"
        btnAjouterFab.Size = New Size(82, 22)
        btnAjouterFab.TabIndex = 10
        btnAjouterFab.Text = "Ajouter"
        btnAjouterFab.UseVisualStyleBackColor = True
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(5, 62)
        Label1.Name = "Label1"
        Label1.Size = New Size(110, 15)
        Label1.TabIndex = 8
        Label1.Text = "Nouveau Fabricant:"
        ' 
        ' cmbFabGestion
        ' 
        cmbFabGestion.Anchor = AnchorStyles.None
        cmbFabGestion.FormattingEnabled = True
        cmbFabGestion.Location = New Point(164, 26)
        cmbFabGestion.Margin = New Padding(3, 2, 3, 2)
        cmbFabGestion.Name = "cmbFabGestion"
        cmbFabGestion.Size = New Size(169, 23)
        cmbFabGestion.TabIndex = 6
        ' 
        ' Label2
        ' 
        Label2.Anchor = AnchorStyles.Left
        Label2.AutoSize = True
        Label2.Location = New Point(5, 28)
        Label2.Name = "Label2"
        Label2.Size = New Size(64, 15)
        Label2.TabIndex = 4
        Label2.Text = "Fabricants:" & vbTab & vbTab
        ' 
        ' btnAjouterMod
        ' 
        btnAjouterMod.Location = New Point(370, 54)
        btnAjouterMod.Margin = New Padding(3, 2, 3, 2)
        btnAjouterMod.Name = "btnAjouterMod"
        btnAjouterMod.Size = New Size(82, 22)
        btnAjouterMod.TabIndex = 11
        btnAjouterMod.Text = "Ajouter"
        btnAjouterMod.UseVisualStyleBackColor = True
        ' 
        ' Label3
        ' 
        Label3.Anchor = AnchorStyles.Left
        Label3.AutoSize = True
        Label3.Location = New Point(13, 28)
        Label3.Name = "Label3"
        Label3.Size = New Size(55, 15)
        Label3.TabIndex = 5
        Label3.Text = "Modèles:" & vbTab
        ' 
        ' cmbModGestion
        ' 
        cmbModGestion.Anchor = AnchorStyles.None
        cmbModGestion.FormattingEnabled = True
        cmbModGestion.Location = New Point(164, 26)
        cmbModGestion.Margin = New Padding(3, 2, 3, 2)
        cmbModGestion.Name = "cmbModGestion"
        cmbModGestion.Size = New Size(169, 23)
        cmbModGestion.TabIndex = 7
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(13, 61)
        Label4.Name = "Label4"
        Label4.Size = New Size(101, 15)
        Label4.TabIndex = 9
        Label4.Text = "Nouveau Modèle:"
        ' 
        ' GroupBox2
        ' 
        GroupBox2.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        GroupBox2.Controls.Add(txtNouveauModele)
        GroupBox2.Controls.Add(btnSupprimerMod)
        GroupBox2.Controls.Add(btnAjouterMod)
        GroupBox2.Controls.Add(Label3)
        GroupBox2.Controls.Add(cmbModGestion)
        GroupBox2.Controls.Add(Label4)
        GroupBox2.Location = New Point(10, 116)
        GroupBox2.Margin = New Padding(3, 2, 3, 2)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Padding = New Padding(3, 2, 3, 2)
        GroupBox2.Size = New Size(729, 86)
        GroupBox2.TabIndex = 1
        GroupBox2.TabStop = False
        GroupBox2.Text = "Modèles"
        ' 
        ' txtNouveauModele
        ' 
        txtNouveauModele.Location = New Point(164, 56)
        txtNouveauModele.Margin = New Padding(3, 2, 3, 2)
        txtNouveauModele.Name = "txtNouveauModele"
        txtNouveauModele.Size = New Size(169, 23)
        txtNouveauModele.TabIndex = 13
        ' 
        ' btnSupprimerMod
        ' 
        btnSupprimerMod.Location = New Point(370, 22)
        btnSupprimerMod.Margin = New Padding(3, 2, 3, 2)
        btnSupprimerMod.Name = "btnSupprimerMod"
        btnSupprimerMod.Size = New Size(82, 22)
        btnSupprimerMod.TabIndex = 12
        btnSupprimerMod.Text = "Supprimer"
        btnSupprimerMod.UseVisualStyleBackColor = True
        ' 
        ' GroupBox3
        ' 
        GroupBox3.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        GroupBox3.Controls.Add(bttnAj)
        GroupBox3.Controls.Add(bttnSup)
        GroupBox3.Controls.Add(cmbFiltre)
        GroupBox3.Controls.Add(BttnImpulsion)
        GroupBox3.Controls.Add(Label15)
        GroupBox3.Controls.Add(txtFreq)
        GroupBox3.Controls.Add(Label14)
        GroupBox3.Controls.Add(txtGain)
        GroupBox3.Controls.Add(cmbMode)
        GroupBox3.Controls.Add(txtEchelle)
        GroupBox3.Controls.Add(txtAmortissement)
        GroupBox3.Controls.Add(txtPRF)
        GroupBox3.Controls.Add(cmbRedressement)
        GroupBox3.Controls.Add(cmbSignal)
        GroupBox3.Controls.Add(Label12)
        GroupBox3.Controls.Add(Label11)
        GroupBox3.Controls.Add(Label10)
        GroupBox3.Controls.Add(Label9)
        GroupBox3.Controls.Add(Label8)
        GroupBox3.Controls.Add(Label7)
        GroupBox3.Controls.Add(Label6)
        GroupBox3.Controls.Add(Label5)
        GroupBox3.Location = New Point(10, 213)
        GroupBox3.Margin = New Padding(3, 2, 3, 2)
        GroupBox3.Name = "GroupBox3"
        GroupBox3.Padding = New Padding(3, 2, 3, 2)
        GroupBox3.Size = New Size(729, 164)
        GroupBox3.TabIndex = 2
        GroupBox3.TabStop = False
        GroupBox3.Text = "Configuration de Réglages"
        ' 
        ' bttnAj
        ' 
        bttnAj.Location = New Point(445, 95)
        bttnAj.Name = "bttnAj"
        bttnAj.Size = New Size(25, 24)
        bttnAj.TabIndex = 25
        bttnAj.Text = "+"
        bttnAj.UseVisualStyleBackColor = True
        ' 
        ' bttnSup
        ' 
        bttnSup.Location = New Point(470, 95)
        bttnSup.Name = "bttnSup"
        bttnSup.Size = New Size(25, 24)
        bttnSup.TabIndex = 24
        bttnSup.Text = "-"
        bttnSup.UseVisualStyleBackColor = True
        ' 
        ' cmbFiltre
        ' 
        cmbFiltre.FormattingEnabled = True
        cmbFiltre.Location = New Point(391, 95)
        cmbFiltre.Name = "cmbFiltre"
        cmbFiltre.Size = New Size(48, 23)
        cmbFiltre.TabIndex = 22
        ' 
        ' BttnImpulsion
        ' 
        BttnImpulsion.Anchor = AnchorStyles.None
        BttnImpulsion.Location = New Point(663, 102)
        BttnImpulsion.Name = "BttnImpulsion"
        BttnImpulsion.Size = New Size(19, 23)
        BttnImpulsion.TabIndex = 21
        BttnImpulsion.Text = "⋮"
        BttnImpulsion.UseVisualStyleBackColor = True
        ' 
        ' Label15
        ' 
        Label15.AutoSize = True
        Label15.Location = New Point(525, 102)
        Label15.Name = "Label15"
        Label15.Size = New Size(68, 15)
        Label15.TabIndex = 20
        Label15.Text = "Impulsions:"
        ' 
        ' txtFreq
        ' 
        txtFreq.Location = New Point(391, 124)
        txtFreq.Name = "txtFreq"
        txtFreq.PlaceholderText = "X;X;X..."
        txtFreq.Size = New Size(104, 23)
        txtFreq.TabIndex = 19
        ' 
        ' Label14
        ' 
        Label14.AutoSize = True
        Label14.Location = New Point(243, 129)
        Label14.Name = "Label14"
        Label14.Size = New Size(121, 15)
        Label14.TabIndex = 18
        Label14.Text = "Fréquences (de 1à12):"
        ' 
        ' txtGain
        ' 
        txtGain.Location = New Point(614, 36)
        txtGain.Name = "txtGain"
        txtGain.Size = New Size(100, 23)
        txtGain.TabIndex = 16
        ' 
        ' cmbMode
        ' 
        cmbMode.FormattingEnabled = True
        cmbMode.Items.AddRange(New Object() {"Monocapteur", "Émetteur / Récepteur", "Transmission directe", "Double"})
        cmbMode.Location = New Point(391, 65)
        cmbMode.Name = "cmbMode"
        cmbMode.Size = New Size(104, 23)
        cmbMode.TabIndex = 15
        ' 
        ' txtEchelle
        ' 
        txtEchelle.Location = New Point(391, 31)
        txtEchelle.Name = "txtEchelle"
        txtEchelle.Size = New Size(104, 23)
        txtEchelle.TabIndex = 12
        ' 
        ' txtAmortissement
        ' 
        txtAmortissement.Location = New Point(113, 95)
        txtAmortissement.Name = "txtAmortissement"
        txtAmortissement.Size = New Size(104, 23)
        txtAmortissement.TabIndex = 11
        ' 
        ' txtPRF
        ' 
        txtPRF.Location = New Point(113, 65)
        txtPRF.Name = "txtPRF"
        txtPRF.Size = New Size(104, 23)
        txtPRF.TabIndex = 10
        ' 
        ' cmbRedressement
        ' 
        cmbRedressement.FormattingEnabled = True
        cmbRedressement.Items.AddRange(New Object() {"RF", "1/1 Onde", "1/2 Onde +", "1/2 Onde -", "Pleine Onde"})
        cmbRedressement.Location = New Point(113, 124)
        cmbRedressement.Name = "cmbRedressement"
        cmbRedressement.Size = New Size(104, 23)
        cmbRedressement.TabIndex = 9
        ' 
        ' cmbSignal
        ' 
        cmbSignal.FormattingEnabled = True
        cmbSignal.Items.AddRange(New Object() {"RF", "1/1 Onde", "1/2 Onde +", "1/2 Onde -"})
        cmbSignal.Location = New Point(113, 31)
        cmbSignal.Name = "cmbSignal"
        cmbSignal.Size = New Size(104, 23)
        cmbSignal.TabIndex = 8
        ' 
        ' Label12
        ' 
        Label12.AutoSize = True
        Label12.Location = New Point(525, 39)
        Label12.Name = "Label12"
        Label12.Size = New Size(37, 15)
        Label12.TabIndex = 7
        Label12.Text = "Gain: "
        ' 
        ' Label11
        ' 
        Label11.AutoSize = True
        Label11.Location = New Point(13, 127)
        Label11.Name = "Label11"
        Label11.Size = New Size(87, 15)
        Label11.TabIndex = 6
        Label11.Text = "Redressement: "
        ' 
        ' Label10
        ' 
        Label10.AutoSize = True
        Label10.Location = New Point(243, 68)
        Label10.Name = "Label10"
        Label10.Size = New Size(44, 15)
        Label10.TabIndex = 5
        Label10.Text = "Mode: "
        ' 
        ' Label9
        ' 
        Label9.AutoSize = True
        Label9.Location = New Point(243, 95)
        Label9.Name = "Label9"
        Label9.Size = New Size(39, 15)
        Label9.TabIndex = 4
        Label9.Text = "Filtre: "
        ' 
        ' Label8
        ' 
        Label8.AutoSize = True
        Label8.Location = New Point(243, 31)
        Label8.Name = "Label8"
        Label8.Size = New Size(99, 15)
        Label8.TabIndex = 3
        Label8.Text = "Echelle (en mm): "
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Location = New Point(13, 97)
        Label7.Name = "Label7"
        Label7.Size = New Size(94, 15)
        Label7.TabIndex = 2
        Label7.Text = "Amortissement: "
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.Location = New Point(13, 65)
        Label6.Name = "Label6"
        Label6.Size = New Size(33, 15)
        Label6.TabIndex = 1
        Label6.Text = "PRF: "
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(13, 31)
        Label5.Name = "Label5"
        Label5.Size = New Size(45, 15)
        Label5.TabIndex = 0
        Label5.Text = "Signal: "
        ' 
        ' btnEnregistrer
        ' 
        btnEnregistrer.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnEnregistrer.Location = New Point(597, 381)
        btnEnregistrer.Margin = New Padding(3, 2, 3, 2)
        btnEnregistrer.Name = "btnEnregistrer"
        btnEnregistrer.Size = New Size(82, 22)
        btnEnregistrer.TabIndex = 3
        btnEnregistrer.Text = "Enregistrer"
        btnEnregistrer.UseVisualStyleBackColor = True
        ' 
        ' btnAnnuler
        ' 
        btnAnnuler.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnAnnuler.Location = New Point(693, 381)
        btnAnnuler.Margin = New Padding(3, 2, 3, 2)
        btnAnnuler.Name = "btnAnnuler"
        btnAnnuler.Size = New Size(82, 22)
        btnAnnuler.TabIndex = 4
        btnAnnuler.Text = "Annuler"
        btnAnnuler.UseVisualStyleBackColor = True
        ' 
        ' FormGestionAppareils
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(786, 411)
        Controls.Add(btnAnnuler)
        Controls.Add(btnEnregistrer)
        Controls.Add(GroupBox3)
        Controls.Add(GroupBox2)
        Controls.Add(GroupBox1)
        Margin = New Padding(3, 2, 3, 2)
        Name = "FormGestionAppareils"
        Text = "Gestion des Appareils et Réglages"
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        GroupBox2.ResumeLayout(False)
        GroupBox2.PerformLayout()
        GroupBox3.ResumeLayout(False)
        GroupBox3.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents Label2 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents cmbFabGestion As ComboBox
    Friend WithEvents btnAjouterMod As Button
    Friend WithEvents btnAjouterFab As Button
    Friend WithEvents Label4 As Label
    Friend WithEvents Label1 As Label
    Friend WithEvents cmbModGestion As ComboBox
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents btnSupprimerFab As Button
    Friend WithEvents btnSupprimerMod As Button
    Friend WithEvents txtNouveauFabricant As TextBox
    Friend WithEvents txtNouveauModele As TextBox
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents btnEnregistrer As Button
    Friend WithEvents btnAnnuler As Button
    Friend WithEvents Label10 As Label
    Friend WithEvents Label9 As Label
    Friend WithEvents Label8 As Label
    Friend WithEvents Label12 As Label
    Friend WithEvents txtEchelle As TextBox
    Friend WithEvents txtGain As TextBox
    Friend WithEvents cmbMode As ComboBox
    Friend WithEvents txtAmortissement As TextBox
    Friend WithEvents txtPRF As TextBox
    Friend WithEvents cmbRedressement As ComboBox
    Friend WithEvents cmbSignal As ComboBox
    Friend WithEvents Label11 As Label
    Friend WithEvents Label7 As Label
    Friend WithEvents Label6 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents txtFreq As TextBox
    Friend WithEvents Label14 As Label
    Friend WithEvents Label15 As Label
    Friend WithEvents BttnImpulsion As Button
    Friend WithEvents cmbFiltre As ComboBox
    Friend WithEvents bttnAj As Button
    Friend WithEvents bttnSup As Button
End Class
