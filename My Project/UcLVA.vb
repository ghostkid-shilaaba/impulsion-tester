Public Class UcLVA

    ' ------------------------------------------------------------------
    ' ÉVÉNEMENTS DE NAVIGATION (Capturés par Form1)
    ' ------------------------------------------------------------------
    Public Event SuivantClicked As EventHandler
    Public Event PrecedentClicked As EventHandler

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub UcLVA_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialisation spécifique à la vue LVA si nécessaire
    End Sub

    Private Sub btnPrecedent_Click(sender As Object, e As EventArgs) Handles BtnPrecedent.Click
        RaiseEvent PrecedentClicked(Me, EventArgs.Empty)
    End Sub

    Private Sub btnSuivant_Click(sender As Object, e As EventArgs) Handles BtnSuivant.Click
        RaiseEvent SuivantClicked(Me, EventArgs.Empty)
    End Sub

End Class