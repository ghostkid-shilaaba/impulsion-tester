Public Class Form1

    ' Conservation des instances pour conserver la saisie lors des retours arrière
    Private vueConnexion As UcConnexion
    Private vueConstat As UcConstat
    Private vueImpulsion As UcImpulsion
    Private vueLDG As UcLDG
    Private vueLVA As UcLVA
    Private vueRFA As UcRFA

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialiser la base de données locale
        DatabaseHelper.InitialiserBaseDeDonnees()

        ' Écran initial : Connexion
        vueConnexion = New UcConnexion()
        vueConnexion.Dock = DockStyle.Fill
        pnlConteneur.Controls.Add(vueConnexion)

        ' Écoute de la validation du mot de passe
        AddHandler vueConnexion.btnValider.Click, AddressOf VerifierMotDePasse
    End Sub

    ' ------------------------------------------------------------------
    ' ÉTAPE 1 : Connexion -> UcConstat
    ' ------------------------------------------------------------------
    Private Sub VerifierMotDePasse(sender As Object, e As EventArgs)
        ' TODO: Placer la logique de vérification du mot de passe ici
        AfficherUcConstat()
    End Sub

    Private Sub AfficherUcConstat()
        pnlConteneur.Controls.Clear()

        If vueConstat Is Nothing Then
            vueConstat = New UcConstat()
            vueConstat.Dock = DockStyle.Fill
            AddHandler vueConstat.SuivantClique, AddressOf ChargerUcImpulsion
        End If

        pnlConteneur.Controls.Add(vueConstat)
    End Sub

    ' ------------------------------------------------------------------
    ' ÉTAPE 2 : UcConstat <-> UcImpulsion
    ' ------------------------------------------------------------------
    Private Sub ChargerUcImpulsion(sender As Object, e As EventArgs)
        pnlConteneur.Controls.Clear()

        If vueImpulsion Is Nothing Then
            vueImpulsion = New UcImpulsion()
            vueImpulsion.Dock = DockStyle.Fill
            AddHandler vueImpulsion.PrecedentClicked, AddressOf RetournerA_UcConstat
            AddHandler vueImpulsion.SuivantClicked, AddressOf ChargerUcLDG
        End If

        pnlConteneur.Controls.Add(vueImpulsion)
    End Sub

    Private Sub RetournerA_UcConstat(sender As Object, e As EventArgs)
        pnlConteneur.Controls.Clear()
        pnlConteneur.Controls.Add(vueConstat)
    End Sub

    ' ------------------------------------------------------------------
    ' ÉTAPE 3 : UcImpulsion <-> UcLDG
    ' ------------------------------------------------------------------
    Private Sub ChargerUcLDG(sender As Object, e As EventArgs)
        pnlConteneur.Controls.Clear()

        If vueLDG Is Nothing Then
            vueLDG = New UcLDG()
            vueLDG.Dock = DockStyle.Fill
            AddHandler vueLDG.PrecedentClicked, AddressOf RetournerA_UcImpulsion
            AddHandler vueLDG.SuivantClicked, AddressOf ChargerUcLVA
        End If

        pnlConteneur.Controls.Add(vueLDG)
    End Sub

    Private Sub RetournerA_UcImpulsion(sender As Object, e As EventArgs)
        pnlConteneur.Controls.Clear()
        pnlConteneur.Controls.Add(vueImpulsion)
    End Sub

    ' ------------------------------------------------------------------
    ' ÉTAPE 4 : UcLDG <-> UcLVA
    ' ------------------------------------------------------------------
    Private Sub ChargerUcLVA(sender As Object, e As EventArgs)
        pnlConteneur.Controls.Clear()

        If vueLVA Is Nothing Then
            vueLVA = New UcLVA()
            vueLVA.Dock = DockStyle.Fill
            AddHandler vueLVA.PrecedentClicked, AddressOf RetournerA_UcLDG
            AddHandler vueLVA.SuivantClicked, AddressOf ChargerUcRFA
        End If

        pnlConteneur.Controls.Add(vueLVA)
    End Sub

    Private Sub RetournerA_UcLDG(sender As Object, e As EventArgs)
        pnlConteneur.Controls.Clear()
        pnlConteneur.Controls.Add(vueLDG)
    End Sub

    ' ------------------------------------------------------------------
    ' ÉTAPE 5 : UcLVA <-> UcRFA
    ' ------------------------------------------------------------------
    Private Sub ChargerUcRFA(sender As Object, e As EventArgs)
        pnlConteneur.Controls.Clear()

        If vueRFA Is Nothing Then
            vueRFA = New UcRFA()
            vueRFA.Dock = DockStyle.Fill
            'AddHandler vueRFA.PrecedentClicked, AddressOf RetournerA_UcLVA
        End If

        pnlConteneur.Controls.Add(vueRFA)
    End Sub

    Private Sub RetournerA_UcLVA(sender As Object, e As EventArgs)
        pnlConteneur.Controls.Clear()
        pnlConteneur.Controls.Add(vueLVA)
    End Sub
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Optional: Clear cache when app closes
        VisaCacheHelper.ClearCache()
    End Sub
End Class