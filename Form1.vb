Public Class Form1

    ' Conservation des instances pour conserver la saisie lors des retours arrière
    Private vueConnexion As UcConnexion
    Private vueConstat As UcConstat
    Private vueImpulsion As UcImpulsion
    Private vueLDG As UcLDG

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
        AfficherUcConstat()
    End Sub

    ' ------------------------------------------------------------------
    ' ÉTAPE 3 : UcImpulsion <-> UcLDG
    ' ------------------------------------------------------------------
    Private Sub ChargerUcLDG(sender As Object, e As EventArgs)
        pnlConteneur.Controls.Clear()

        If vueLDG Is Nothing Then
            vueLDG = New UcLDG()
            vueLDG.Dock = DockStyle.Fill
            ' TODO: Attacher l'événement Précédent de UcLDG s'il existe :
            ' AddHandler vueLDG.PrecedentClicked, AddressOf ChargerUcImpulsion
        End If

        pnlConteneur.Controls.Add(vueLDG)
    End Sub

End Class