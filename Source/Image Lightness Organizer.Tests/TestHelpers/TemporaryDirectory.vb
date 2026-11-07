#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO

#End Region

#Region " TemporaryDirectory "

''' <summary>
''' Represents a uniquely named temporary directory that is recursively deleted when disposed.
''' </summary>
Friend NotInheritable Class TemporaryDirectory : Implements IDisposable

#Region " Properties "

    ''' <summary>
    ''' Gets the full path of the temporary directory.
    ''' </summary>
    Friend ReadOnly Property FullPath As String

#End Region

#Region " Fields "

    ''' <summary>
    ''' A value indicating whether this instance has been disposed.
    ''' </summary>
    Private isDisposed As Boolean

#End Region

#Region " Constructors "

    ''' <summary>
    ''' Initializes a new instance of the <see cref="TemporaryDirectory"/> class,
    ''' creating a new uniquely named directory inside the user's temporary folder.
    ''' </summary>
    Friend Sub New()

        Me.FullPath = Path.Combine(Path.GetTempPath(), $"ILO.Tests_{Guid.NewGuid():N}")
        Directory.CreateDirectory(Me.FullPath)
    End Sub

#End Region

#Region " Methods "

    ''' <summary>
    ''' Combines the temporary directory path with the specified relative path.
    ''' </summary>
    '''
    ''' <param name="relativePaths">
    ''' The relative path segments to combine.
    ''' </param>
    '''
    ''' <returns>
    ''' The combined full path.
    ''' </returns>
    Friend Function Combine(ParamArray relativePaths As String()) As String

        ArgumentNullException.ThrowIfNull(relativePaths)

        Dim segments As New List(Of String) From {Me.FullPath}
        segments.AddRange(relativePaths)
        Return Path.Combine(segments.ToArray())
    End Function

    ''' <summary>
    ''' Writes the specified bytes to a file inside the temporary directory.
    ''' </summary>
    '''
    ''' <param name="fileName">
    ''' The relative file name.
    ''' </param>
    '''
    ''' <param name="content">
    ''' The file content.
    ''' </param>
    '''
    ''' <returns>
    ''' The full path of the written file.
    ''' </returns>
    Friend Function WriteFile(fileName As String, content As Byte()) As String

        Dim filePath As String = Me.Combine(fileName)
        Dim parentDirectory As String = Path.GetDirectoryName(filePath)
        Directory.CreateDirectory(parentDirectory)
        File.WriteAllBytes(filePath, content)
        Return filePath
    End Function

    ''' <summary>
    ''' Deletes the temporary directory and all its contents.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose

        If Me.isDisposed Then
            Return
        End If

        Me.isDisposed = True
        If Directory.Exists(Me.FullPath) Then
            Directory.Delete(Me.FullPath, recursive:=True)
        End If
    End Sub

#End Region

End Class

#End Region
