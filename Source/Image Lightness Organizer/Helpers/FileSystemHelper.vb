
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO

#End Region

#Region " FileSystemHelper "

Friend Module FileSystemHelper

#Region " Constants "

    ''' <summary>
    ''' The Extended-Length Path prefix for local paths.
    ''' </summary>
    Private Const ExtendedPathPrefix As String = "\\?\"

    ''' <summary>
    ''' The Extended-Length Path prefix for UNC paths.
    ''' </summary>
    Private Const ExtendedUncPathPrefix As String = "\\?\UNC\"

    ''' <summary>
    ''' The Win32 device namespace prefix (e.g., <c>\\.\COM1</c>, <c>\\.\PhysicalDrive0</c>).
    ''' </summary>
    Private Const DevicePathPrefix As String = "\\.\"

#End Region

#Region " Static Methods "

    ''' <summary>
    ''' Converts a standard path into an Extended-Length Path (prefixed with \\?\) to bypass the 260 character MAX_PATH limitation.
    ''' </summary>
    '''
    ''' <param name="targetPath">
    ''' The absolute path to convert.
    ''' </param>
    '''
    ''' <returns>
    ''' The extended-length path string.
    ''' <para></para>
    ''' The path is returned unchanged when the current operating system is not Windows,
    ''' when it is already an extended-length path, when it is a Win32 device path (<c>\\.\</c>),
    ''' or when it is not fully qualified (e.g., <c>Folder\File.jpg</c>, <c>\Folder</c> or <c>C:Folder</c>).
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="targetPath"/> is <see langword="Nothing"/>.
    ''' </exception>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="targetPath"/> is empty or white space.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function GetExtendedPath(targetPath As String) As String

        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath)

        If Not OperatingSystem.IsWindows Then
            Return targetPath
        End If

        ' Already an extended path, or a Win32 device path.
        If targetPath.StartsWith(FileSystemHelper.ExtendedPathPrefix, StringComparison.Ordinal) OrElse
           targetPath.StartsWith(FileSystemHelper.DevicePathPrefix, StringComparison.Ordinal) Then
            Return targetPath
        End If

        ' Relative paths (including drive-relative "C:Folder" and root-relative "\Folder")
        ' cannot be converted to Extended-Length paths.
        If Not Path.IsPathFullyQualified(targetPath) Then
            Return targetPath
        End If

        ' Handle UNC paths: \\Server\Share -> \\?\UNC\Server\Share
        If targetPath.StartsWith("\\", StringComparison.Ordinal) Then
            Return $"{FileSystemHelper.ExtendedUncPathPrefix}{targetPath.Substring(2)}"
        End If

        ' Handle Local paths: C:\Folder -> \\?\C:\Folder
        Return $"{FileSystemHelper.ExtendedPathPrefix}{targetPath}"
    End Function

    ''' <summary>
    ''' Strips the Extended-Length Path prefix for UI display or Shell interop.
    ''' </summary>
    '''
    ''' <param name="targetPath">
    ''' The extended-length path to convert.
    ''' </param>
    '''
    ''' <returns>
    ''' The path without the Extended-Length Path prefix
    ''' (e.g., <c>\\?\C:\Folder</c> -> <c>C:\Folder</c>, <c>\\?\UNC\Server\Share</c> -> <c>\\Server\Share</c>).
    ''' <para></para>
    ''' The path is returned unchanged when the current operating system is not Windows,
    ''' or when it is not an extended-length path.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="targetPath"/> is <see langword="Nothing"/>.
    ''' </exception>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="targetPath"/> is empty or white space.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function GetNormalPath(targetPath As String) As String

        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath)

        If Not OperatingSystem.IsWindows Then
            Return targetPath
        End If

        If targetPath.StartsWith(FileSystemHelper.ExtendedUncPathPrefix, StringComparison.OrdinalIgnoreCase) Then
            Return $"\\{targetPath.Substring(FileSystemHelper.ExtendedUncPathPrefix.Length)}"
        End If

        If targetPath.StartsWith(FileSystemHelper.ExtendedPathPrefix, StringComparison.Ordinal) Then
            Return targetPath.Substring(FileSystemHelper.ExtendedPathPrefix.Length)
        End If

        Return targetPath
    End Function

#End Region

End Module

#End Region
