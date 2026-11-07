#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO

#End Region

#Region " FileSystemHelperTests "

''' <summary>
''' Unit tests for <see cref="FileSystemHelper"/> module.
''' </summary>
<TestClass>
<OSCondition(OperatingSystems.Windows)>
Public NotInheritable Class FileSystemHelperTests

#Region " GetExtendedPath "

    <TestMethod>
    <DataRow("C:\Folder", "\\?\C:\Folder")>
    <DataRow("C:\", "\\?\C:\")>
    <DataRow("D:\Pictures\Wallpapers\image.jpg", "\\?\D:\Pictures\Wallpapers\image.jpg")>
    <DataRow("C:\Folder With Spaces\@Sorted by lightness", "\\?\C:\Folder With Spaces\@Sorted by lightness")>
    Public Sub GetExtendedPath_LocalPath_AddsExtendedPrefix(targetPath As String, expected As String)
        Assert.AreEqual(expected, FileSystemHelper.GetExtendedPath(targetPath))
    End Sub

    <TestMethod>
    <DataRow("\\Server\Share", "\\?\UNC\Server\Share")>
    <DataRow("\\Server\Share\Folder\image.png", "\\?\UNC\Server\Share\Folder\image.png")>
    Public Sub GetExtendedPath_UncPath_AddsExtendedUncPrefix(targetPath As String, expected As String)
        Assert.AreEqual(expected, FileSystemHelper.GetExtendedPath(targetPath))
    End Sub

    <TestMethod>
    <DataRow("\\?\C:\Folder")>
    <DataRow("\\?\UNC\Server\Share")>
    Public Sub GetExtendedPath_AlreadyExtended_ReturnsSamePath(targetPath As String)
        Assert.AreEqual(targetPath, FileSystemHelper.GetExtendedPath(targetPath))
    End Sub

    <TestMethod>
    <DataRow("\\.\COM1")>
    <DataRow("\\.\PhysicalDrive0")>
    Public Sub GetExtendedPath_DevicePath_ReturnsSamePath(targetPath As String)
        Assert.AreEqual(targetPath, FileSystemHelper.GetExtendedPath(targetPath))
    End Sub

    <TestMethod>
    <DataRow("image.jpg")>
    <DataRow("Folder\image.jpg")>
    <DataRow(".\Folder")>
    <DataRow("..\Folder")>
    <DataRow("\Folder")>    ' Root-relative (relative to the current drive).
    <DataRow("C:Folder")>   ' Drive-relative (relative to the current directory of drive C:).
    Public Sub GetExtendedPath_NotFullyQualified_ReturnsSamePath(targetPath As String)
        Assert.AreEqual(targetPath, FileSystemHelper.GetExtendedPath(targetPath))
    End Sub

    <TestMethod>
    Public Sub GetExtendedPath_Nothing_ThrowsArgumentNullException()
        Assert.ThrowsExactly(Of ArgumentNullException)(Sub() FileSystemHelper.GetExtendedPath(Nothing))
    End Sub

    <TestMethod>
    <DataRow("")>
    <DataRow("   ")>
    <DataRow(vbTab)>
    Public Sub GetExtendedPath_EmptyOrWhiteSpace_ThrowsArgumentException(targetPath As String)
        Assert.ThrowsExactly(Of ArgumentException)(Sub() FileSystemHelper.GetExtendedPath(targetPath))
    End Sub

    <TestMethod>
    Public Sub GetExtendedPath_AllowsAccessingPathsLongerThanMaxPath()
        Using tempDir As New TemporaryDirectory()
            Dim longPath As String = tempDir.FullPath
            Do While longPath.Length <= 300
                longPath = Path.Combine(longPath, New String("d"c, 50))
            Loop
            Dim filePath As String = Path.Combine(longPath, "image.png")

            Dim extendedDirPath As String = FileSystemHelper.GetExtendedPath(longPath)
            Dim extendedFilePath As String = FileSystemHelper.GetExtendedPath(filePath)
            Directory.CreateDirectory(extendedDirPath)
            File.WriteAllBytes(extendedFilePath, New Byte() {1, 2, 3})

            Assert.IsGreaterThan(260, filePath.Length)
            Assert.IsTrue(File.Exists(extendedFilePath))
            CollectionAssert.AreEqual(New Byte() {1, 2, 3}, File.ReadAllBytes(extendedFilePath))

            ' Clean up through the extended path, because the normal path may exceed MAX_PATH.
            Directory.Delete(FileSystemHelper.GetExtendedPath(Path.Combine(tempDir.FullPath, New String("d"c, 50))), recursive:=True)
        End Using
    End Sub

#End Region

#Region " GetNormalPath "

    <TestMethod>
    <DataRow("\\?\C:\Folder", "C:\Folder")>
    <DataRow("\\?\C:\", "C:\")>
    <DataRow("\\?\UNC\Server\Share", "\\Server\Share")>
    <DataRow("\\?\UNC\Server\Share\Folder\image.png", "\\Server\Share\Folder\image.png")>
    <DataRow("\\?\unc\Server\Share", "\\Server\Share")>
    Public Sub GetNormalPath_ExtendedPath_RemovesPrefix(targetPath As String, expected As String)
        Assert.AreEqual(expected, FileSystemHelper.GetNormalPath(targetPath))
    End Sub

    <TestMethod>
    <DataRow("C:\Folder")>
    <DataRow("\\Server\Share")>
    <DataRow("\\.\COM1")>
    <DataRow("Folder\image.jpg")>
    Public Sub GetNormalPath_NotExtended_ReturnsSamePath(targetPath As String)
        Assert.AreEqual(targetPath, FileSystemHelper.GetNormalPath(targetPath))
    End Sub

    <TestMethod>
    Public Sub GetNormalPath_Nothing_ThrowsArgumentNullException()
        Assert.ThrowsExactly(Of ArgumentNullException)(Sub() FileSystemHelper.GetNormalPath(Nothing))
    End Sub

    <TestMethod>
    <DataRow("")>
    <DataRow("   ")>
    Public Sub GetNormalPath_EmptyOrWhiteSpace_ThrowsArgumentException(targetPath As String)
        Assert.ThrowsExactly(Of ArgumentException)(Sub() FileSystemHelper.GetNormalPath(targetPath))
    End Sub

#End Region

#Region " Round Trip "

    <TestMethod>
    <DataRow("C:\Folder\Sub Folder")>
    <DataRow("C:\")>
    <DataRow("\\Server\Share\Folder")>
    <DataRow("Folder\image.jpg")>
    Public Sub GetNormalPath_OfGetExtendedPath_ReturnsOriginalPath(targetPath As String)
        Dim roundTrip As String = FileSystemHelper.GetNormalPath(FileSystemHelper.GetExtendedPath(targetPath))

        Assert.AreEqual(targetPath, roundTrip)
    End Sub

#End Region

End Class

#End Region
