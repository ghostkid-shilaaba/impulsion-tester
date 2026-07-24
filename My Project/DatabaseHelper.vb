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
                    
                    -- 12 Frequencies (REAL)
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

                    FOREIGN KEY (fabricant_id) REFERENCES Fabricants(fabricant_id) ON DELETE CASCADE
                );"

            Using cmd As New SQLiteCommand(sqlFabricants, conn)
                cmd.ExecuteNonQuery()
            End Using

            Using cmd As New SQLiteCommand(sqlModeles, conn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Helper method to insert a new device model with all numeric parameters bound explicitly as Double.
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
                    freq1, freq2, freq3, freq4, freq5, freq6, freq7, freq8, freq9, freq10, freq11, freq12
                ) VALUES (
                    @fabId, @nom, @signal, @prf, @damping, @echelle, @filtre, @mode, @redressement, @gain,
                    @f1, @f2, @f3, @f4, @f5, @f6, @f7, @f8, @f9, @f10, @f11, @f12
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

                ' Dynamically populate freq1 through freq12 as REAL
                For i As Integer = 0 To 11
                    Dim val As Double = If(frequences IsNot Nothing AndAlso frequences.Length > i, frequences(i), 0.0)
                    cmd.Parameters.Add($"@f{i + 1}", DbType.Double).Value = val
                Next

                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Class