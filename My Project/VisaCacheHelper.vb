Imports System.IO
Imports System.Text.Json

Public Class VisaCacheHelper

    Private Shared Function GetCacheFilePath() As String
        Return Path.Combine(Path.GetTempPath(), "visa_devices.json")
    End Function

    ''' <summary>
    ''' Checks if a valid cache file exists (less than 5 minutes old)
    ''' </summary>
    Public Shared Function HasValidCache() As Boolean
        Dim cacheFile As String = GetCacheFilePath()
        If Not File.Exists(cacheFile) Then Return False

        Try
            Dim fileInfo As New FileInfo(cacheFile)
            ' Cache valid for 5 minutes
            If (DateTime.Now - fileInfo.LastWriteTime).TotalMinutes > 5 Then
                Return False
            End If

            ' Verify it's valid JSON
            Dim json As String = File.ReadAllText(cacheFile)
            Using doc As JsonDocument = JsonDocument.Parse(json)
                Return doc.RootElement.ValueKind = JsonValueKind.Object
            End Using
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Loads detection results from cache file
    ''' </summary>
    Public Shared Function LoadFromCache() As JsonElement
        Dim cacheFile As String = GetCacheFilePath()
        If Not File.Exists(cacheFile) Then Return Nothing

        Try
            Dim json As String = File.ReadAllText(cacheFile)
            Using doc As JsonDocument = JsonDocument.Parse(json)
                Return doc.RootElement.Clone()
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Saves detection results to cache file
    ''' </summary>
    Public Shared Sub SaveToCache(jsonContent As String)
        Dim cacheFile As String = GetCacheFilePath()
        Try
            File.WriteAllText(cacheFile, jsonContent)
        Catch
            ' Ignore - Python script also saves the cache
        End Try
    End Sub

    ''' <summary>
    ''' Deletes the cache file (forces fresh detection)
    ''' </summary>
    Public Shared Sub ClearCache()
        Dim cacheFile As String = GetCacheFilePath()
        Try
            If File.Exists(cacheFile) Then
                File.Delete(cacheFile)
            End If
        Catch
            ' Ignore
        End Try
    End Sub

    ''' <summary>
    ''' Gets the cache file age in minutes (returns -1 if not exists)
    ''' </summary>
    Public Shared Function GetCacheAgeMinutes() As Double
        Dim cacheFile As String = GetCacheFilePath()
        If Not File.Exists(cacheFile) Then Return -1

        Try
            Dim fileInfo As New FileInfo(cacheFile)
            Return (DateTime.Now - fileInfo.LastWriteTime).TotalMinutes
        Catch
            Return -1
        End Try
    End Function

End Class