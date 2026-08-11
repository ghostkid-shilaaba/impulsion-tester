Imports System.Data
Imports System.Data.SQLite
Imports System.IO

Public Class DatabaseHelper

    Private Shared dbFile As String = "AppareilsDB.sqlite"
    Public Shared connectionString As String = $"Data Source={dbFile};Version=3;"

    ''' <summary>
    ''' Initializes a clean SQLite database structure with REAL type for all numerical fields.
    ''' </summary>
    Public Shared Sub InitialiserBaseDeDonnees()
        If Not File.Exists(dbFile) Then
            SQLiteConnection.CreateFile(dbFile)
        End If

        Using conn As New SQLiteConnection(connectionString)
            conn.Open()

            ' Enable Foreign Key constraints in SQLite
            Using pragmaCmd As New SQLiteCommand("PRAGMA foreign_keys = ON;", conn)
                pragmaCmd.ExecuteNonQuery()
            End Using

            ' 1. Create Fabricants Table
            Dim sqlFabricants As String = "
                CREATE TABLE IF NOT EXISTS Fabricants (
                    fabricant_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    nom_fabricant TEXT NOT NULL UNIQUE
                );"

            ' 2. Create ModelesAppareils Table (All numeric fields strictly REAL)
            Dim sqlModeles As String = "
                CREATE TABLE IF NOT EXISTS ModelesAppareils (
                    modele_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    fabricant_id INTEGER NOT NULL,
                    nom_modele TEXT NOT NULL,
                    
                    -- Hardware Parameters (Numerical = REAL)
                    signal TEXT DEFAULT '',
                    prf REAL DEFAULT 0.0,
                    damping REAL DEFAULT 0.0,
                    echelle REAL DEFAULT 0.0,
                    filtre TEXT DEFAULT '',
                    mode TEXT DEFAULT '',
                    redressement TEXT DEFAULT '',
                    gain REAL DEFAULT 0.0,
                    
                    -- 13 Frequencies (REAL)
                    freq1 REAL DEFAULT 0.0,
                    freq2 REAL DEFAULT 0.0,
                    freq3 REAL DEFAULT 0.0,
                    freq4 REAL DEFAULT 0.0,
                    freq5 REAL DEFAULT 0.0,
                    freq6 REAL DEFAULT 0.0,
                    freq7 REAL DEFAULT 0.0,
                    freq8 REAL DEFAULT 0.0,
                    freq9 REAL DEFAULT 0.0,
                    freq10 REAL DEFAULT 0.0,
                    freq11 REAL DEFAULT 0.0,
                    freq12 REAL DEFAULT 0.0,
                    freq13 REAL DEFAULT 0.0,

                    FOREIGN KEY (fabricant_id) REFERENCES Fabricants(fabricant_id) ON DELETE CASCADE
                );"

            Using cmd As New SQLiteCommand(sqlFabricants, conn)
                cmd.ExecuteNonQuery()
            End Using

            Using cmd As New SQLiteCommand(sqlModeles, conn)
                cmd.ExecuteNonQuery()
            End Using

            ' If the app is running against an older DB file created before
            ' freq13 existed, ALTER it in -- CREATE TABLE IF NOT EXISTS above
            ' only applies to brand new databases, it does not add columns to
            ' an existing ModelesAppareils table.
            AjouterColonneFreq13SiAbsente(conn)

            ' 3. Create Impulsions Table
            Dim sqlImpulsions As String = "
    CREATE TABLE IF NOT EXISTS Impulsions (
        impulsion_id INTEGER PRIMARY KEY AUTOINCREMENT,
        modele_id INTEGER NOT NULL,
        numero INTEGER NOT NULL,
        tension REAL DEFAULT 0.0,
        amortissement REAL DEFAULT 0.0,
        prf REAL DEFAULT 0.0,

        FOREIGN KEY (modele_id)
            REFERENCES ModelesAppareils(modele_id)
            ON DELETE CASCADE
    );"

            Using cmd As New SQLiteCommand(sqlImpulsions, conn)
                cmd.ExecuteNonQuery()
            End Using

            ' 4. Create Filtres Table (one DUT can now have several filters,
            ' each with its own name and its own set of frequencies)
            Dim sqlFiltres As String = "
    CREATE TABLE IF NOT EXISTS Filtres (
        filtre_id INTEGER PRIMARY KEY AUTOINCREMENT,
        modele_id INTEGER NOT NULL,
        nom_filtre TEXT DEFAULT '',
        frequences TEXT DEFAULT '',

        FOREIGN KEY (modele_id)
            REFERENCES ModelesAppareils(modele_id)
            ON DELETE CASCADE
    );"

            Using cmd As New SQLiteCommand(sqlFiltres, conn)
                cmd.ExecuteNonQuery()
            End Using

            MigrerFrequencesLegacyVersFiltres(conn)
        End Using
    End Sub

    ''' <summary>
    ''' Ajoute la colonne freq13 si la base existait déjà (créée avant son
    ''' introduction). Sans cela, "CREATE TABLE IF NOT EXISTS" ne fait rien
    ''' sur une table déjà présente et freq13 n'existerait jamais sur une
    ''' base ancienne, ce qui ferait planter la migration ci-dessous.
    ''' </summary>
    Private Shared Sub AjouterColonneFreq13SiAbsente(conn As SQLiteConnection)
        Dim aLaColonne As Boolean = False
        Using cmd As New SQLiteCommand("PRAGMA table_info(ModelesAppareils);", conn)
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    If reader("name").ToString().Equals("freq13", StringComparison.OrdinalIgnoreCase) Then
                        aLaColonne = True
                        Exit While
                    End If
                End While
            End Using
        End Using

        If Not aLaColonne Then
            Using cmd As New SQLiteCommand("ALTER TABLE ModelesAppareils ADD COLUMN freq13 REAL DEFAULT 0.0;", conn)
                cmd.ExecuteNonQuery()
            End Using
        End If
    End Sub

    ''' <summary>
    ''' Migration unique : copie les fréquences héritées (ModelesAppareils.
    ''' freq1..freq13, ancienne conception "un seul filtre par modèle") vers
    ''' la table Filtres, seule source lue désormais par l'application (voir
    ''' UcRFA.GetFrequencesFiltreSelectionne). Idempotente : ne touche que
    ''' les modèles qui n'ont encore aucun filtre enregistré dans Filtres,
    ''' donc peut rester appelée à chaque démarrage sans effet une fois la
    ''' migration faite. Les colonnes freq1..freq13 restent en base (pour ne
    ''' pas risquer un DROP COLUMN) mais ne sont plus lues nulle part après
    ''' cette migration.
    ''' </summary>
    Private Shared Sub MigrerFrequencesLegacyVersFiltres(conn As SQLiteConnection)
        Dim aMigrer As New List(Of (ModeleId As Integer, NomFiltre As String, Frequences As String))

        Dim selectSql As String =
            "SELECT m.modele_id, m.filtre, " &
            "m.freq1, m.freq2, m.freq3, m.freq4, m.freq5, m.freq6, " &
            "m.freq7, m.freq8, m.freq9, m.freq10, m.freq11, m.freq12, m.freq13 " &
            "FROM ModelesAppareils m " &
            "WHERE NOT EXISTS (SELECT 1 FROM Filtres f WHERE f.modele_id = m.modele_id);"

        Using cmd As New SQLiteCommand(selectSql, conn)
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    Dim modeleId As Integer = Convert.ToInt32(reader("modele_id"))
                    Dim nomFiltre As String = If(reader("filtre") Is DBNull.Value, "", reader("filtre").ToString())
                    If String.IsNullOrWhiteSpace(nomFiltre) Then nomFiltre = "Filtre 1"

                    Dim valeurs As New List(Of Double)
                    For i As Integer = 1 To 13
                        Dim colName As String = $"freq{i}"
                        If Not reader.IsDBNull(reader.GetOrdinal(colName)) Then
                            Dim v As Double = Convert.ToDouble(reader(colName))
                            If v > 0.0 Then valeurs.Add(v)
                        End If
                    Next

                    If valeurs.Count > 0 Then
                        aMigrer.Add((modeleId, nomFiltre, String.Join(Environment.NewLine, valeurs)))
                    End If
                End While
            End Using
        End Using

        For Each item In aMigrer
            Using cmd As New SQLiteCommand(
                "INSERT INTO Filtres (modele_id, nom_filtre, frequences) VALUES (@modeleId, @nom, @freq);", conn)
                cmd.Parameters.AddWithValue("@modeleId", item.ModeleId)
                cmd.Parameters.AddWithValue("@nom", item.NomFiltre)
                cmd.Parameters.AddWithValue("@freq", item.Frequences)
                cmd.ExecuteNonQuery()
            End Using
        Next
    End Sub

    ''' <summary>
    ''' Helper method to insert a new device model with all numeric parameters bound explicitly as Double.
    ''' Expects exactly 13 frequencies in "frequences" (missing entries default to 0.0).
    ''' </summary>
    Public Shared Sub AjouterModele(
        fabricantId As Integer,
        nomModele As String,
        signal As String,
        prf As Double,
        damping As Double,
        echelle As Double,
        filtre As String,
        mode As String,
        redressement As String,
        gain As Double,
        frequences As Double()
    )
        Using conn As New SQLiteConnection(connectionString)
            conn.Open()

            Dim sql As String = "
                INSERT INTO ModelesAppareils (
                    fabricant_id, nom_modele, signal, prf, damping, echelle, filtre, mode, redressement, gain,
                    freq1, freq2, freq3, freq4, freq5, freq6, freq7, freq8, freq9, freq10, freq11, freq12, freq13
                ) VALUES (
                    @fabId, @nom, @signal, @prf, @damping, @echelle, @filtre, @mode, @redressement, @gain,
                    @f1, @f2, @f3, @f4, @f5, @f6, @f7, @f8, @f9, @f10, @f11, @f12, @f13
                );"

            Using cmd As New SQLiteCommand(sql, conn)
                cmd.Parameters.AddWithValue("@fabId", fabricantId)
                cmd.Parameters.AddWithValue("@nom", nomModele)
                cmd.Parameters.AddWithValue("@signal", signal)
                cmd.Parameters.AddWithValue("@filtre", filtre)
                cmd.Parameters.AddWithValue("@mode", mode)
                cmd.Parameters.AddWithValue("@redressement", redressement)

                ' Bind numerical parameters as REAL (DbType.Double)
                cmd.Parameters.Add("@prf", DbType.Double).Value = prf
                cmd.Parameters.Add("@damping", DbType.Double).Value = damping
                cmd.Parameters.Add("@echelle", DbType.Double).Value = echelle
                cmd.Parameters.Add("@gain", DbType.Double).Value = gain

                ' Dynamically populate freq1 through freq13 as REAL
                For i As Integer = 0 To 12
                    Dim val As Double = If(frequences IsNot Nothing AndAlso frequences.Length > i, frequences(i), 0.0)
                    cmd.Parameters.Add($"@f{i + 1}", DbType.Double).Value = val
                Next

                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
    Public Shared Sub EnregistrerImpulsions(
    modeleId As Integer,
    impulsions As DataTable
)
        Using conn As New SQLiteConnection(connectionString)
            conn.Open()

            ' Remove old settings for this model
            Using deleteCmd As New SQLiteCommand(
                "DELETE FROM Impulsions WHERE modele_id = @modeleId", conn)

                deleteCmd.Parameters.AddWithValue("@modeleId", modeleId)
                deleteCmd.ExecuteNonQuery()
            End Using

            ' Insert new settings
            For Each row As DataRow In impulsions.Rows

                Using cmd As New SQLiteCommand("
                INSERT INTO Impulsions
                (modele_id, numero, tension, amortissement, prf)
                VALUES
                (@modeleId, @numero, @tension, @amortissement, @prf)", conn)

                    cmd.Parameters.AddWithValue("@modeleId", modeleId)
                    cmd.Parameters.AddWithValue("@numero", Convert.ToInt32(row("numero")))
                    cmd.Parameters.AddWithValue("@tension", Convert.ToDouble(row("tension")))
                    cmd.Parameters.AddWithValue("@amortissement", Convert.ToDouble(row("amortissement")))
                    cmd.Parameters.AddWithValue("@prf", Convert.ToDouble(row("prf")))

                    cmd.ExecuteNonQuery()
                End Using

            Next
        End Using
    End Sub

    ''' <summary>
    ''' Returns all filters (name + frequencies) saved for a given model, one
    ''' row per filter. "frequences" holds the raw multi-line text as typed
    ''' in txtFreq (one frequency per line), unparsed -- the caller is
    ''' responsible for splitting/joining it.
    ''' </summary>
    Public Shared Function GetFiltresForModele(modeleId As Integer) As DataTable
        Dim dt As New DataTable()
        Using conn As New SQLiteConnection(connectionString)
            conn.Open()
            Dim query As String = "SELECT filtre_id, nom_filtre, frequences " &
                                  "FROM Filtres WHERE modele_id = @modeleId ORDER BY filtre_id;"
            Using adapter As New SQLiteDataAdapter(query, conn)
                adapter.SelectCommand.Parameters.AddWithValue("@modeleId", modeleId)
                adapter.Fill(dt)
            End Using
        End Using
        Return dt
    End Function

    ''' <summary>
    ''' Replaces all filters for a model with the contents of the given
    ''' DataTable (columns: nom_filtre, frequences). Existing filters for
    ''' this model are deleted first, same pattern as EnregistrerImpulsions.
    ''' </summary>
    Public Shared Sub SaveFiltresForModele(modeleId As Integer, filtres As DataTable)
        Using conn As New SQLiteConnection(connectionString)
            conn.Open()

            Using deleteCmd As New SQLiteCommand(
                "DELETE FROM Filtres WHERE modele_id = @modeleId", conn)
                deleteCmd.Parameters.AddWithValue("@modeleId", modeleId)
                deleteCmd.ExecuteNonQuery()
            End Using

            For Each row As DataRow In filtres.Rows
                Dim nom As String = If(row("nom_filtre") Is DBNull.Value, "", row("nom_filtre").ToString())
                Dim freq As String = If(row("frequences") Is DBNull.Value, "", row("frequences").ToString())
                If String.IsNullOrWhiteSpace(nom) AndAlso String.IsNullOrWhiteSpace(freq) Then Continue For

                Using cmd As New SQLiteCommand("
                INSERT INTO Filtres (modele_id, nom_filtre, frequences)
                VALUES (@modeleId, @nom, @freq)", conn)
                    cmd.Parameters.AddWithValue("@modeleId", modeleId)
                    cmd.Parameters.AddWithValue("@nom", nom)
                    cmd.Parameters.AddWithValue("@freq", freq)
                    cmd.ExecuteNonQuery()
                End Using
            Next
        End Using
    End Sub
End Class