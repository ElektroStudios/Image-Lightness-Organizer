#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO

Imports SkiaSharp

#End Region

#Region " ProgramIntegrationTests "

''' <summary>
''' End-to-end integration tests that launch the application executable as a separate process.
''' </summary>
'''
''' <remarks>
''' The application calls <see cref="Environment.Exit"/>, so it cannot be hosted inside the test process;
''' instead, it is launched with redirected standard streams and its exit code, output and file system effects are verified.
''' </remarks>
<TestClass>
<OSCondition(OperatingSystems.Windows)>
Public NotInheritable Class ProgramIntegrationTests

#Region " Constants "

    ''' <summary>
    ''' The name of the output folder created by the application.
    ''' </summary>
    Private Const OutputFolderName As String = "@Sorted by lightness"

#End Region

#Region " Fields "

    ''' <summary>
    ''' The temporary source directory used by the current test.
    ''' </summary>
    Private tempDir As TemporaryDirectory

#End Region

#Region " Test Initialization "

    <TestInitialize>
    Public Sub TestInitialize()
        Me.tempDir = New TemporaryDirectory()
    End Sub

    <TestCleanup>
    Public Sub TestCleanup()
        Me.tempDir.Dispose()
    End Sub

#End Region

#Region " Argument Errors "

    <TestMethod>
    Public Sub Run_WithoutArguments_ExitsWithCode2AndShowsUsage()
        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run()

        Assert.AreEqual(2, result.ExitCode, result.ToString())
        Assert.Contains("Missing argument", result.StandardOutput)
        Assert.Contains("Usage:", result.StandardOutput)
    End Sub

    <TestMethod>
    <DataRow("0")>
    <DataRow("11")>
    <DataRow("-5")>
    <DataRow("abc")>
    <DataRow("2.5")>
    Public Sub Run_WithInvalidStep_ExitsWithCode5(stepArgument As String)
        Me.WriteImage("white.png", Color.White)

        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(Me.tempDir.FullPath, stepArgument)

        Assert.AreEqual(5, result.ExitCode, result.ToString())
        Assert.Contains("percentage step", result.StandardOutput)
        Assert.IsTrue(File.Exists(Me.tempDir.Combine("white.png")), "No file must be moved when the arguments are invalid.")
    End Sub

    <TestMethod>
    Public Sub Run_WithNonExistentDirectory_ExitsWithCode3()
        Dim missingDir As String = Me.tempDir.Combine("does-not-exist")

        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(missingDir)

        Assert.AreEqual(3, result.ExitCode, result.ToString())
        Assert.Contains("does not exist", result.StandardOutput)
    End Sub

    <TestMethod>
    Public Sub Run_WithEmptyDirectory_ExitsWithCode4()
        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(Me.tempDir.FullPath)

        Assert.AreEqual(4, result.ExitCode, result.ToString())
        Assert.Contains("No supported image files", result.StandardOutput)
    End Sub

    <TestMethod>
    Public Sub Run_WithOnlyUnsupportedFiles_ExitsWithCode4AndLeavesFilesUntouched()
        Me.tempDir.WriteFile("notes.txt", New Byte() {65, 66, 67})
        Me.tempDir.WriteFile("animation.gif", TestImageFactory.EncodeSolidWithGdi(Color.White, ImageFormat.Gif))
        Me.tempDir.WriteFile("no-extension", New Byte() {1})

        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(Me.tempDir.FullPath)

        Assert.AreEqual(4, result.ExitCode, result.ToString())
        Assert.IsTrue(File.Exists(Me.tempDir.Combine("notes.txt")))
        Assert.IsTrue(File.Exists(Me.tempDir.Combine("animation.gif")))
        Assert.IsTrue(File.Exists(Me.tempDir.Combine("no-extension")))
        Assert.IsFalse(Directory.Exists(Me.tempDir.Combine(ProgramIntegrationTests.OutputFolderName)))
    End Sub

#End Region

#Region " Successful Processing "

    <TestMethod>
    Public Sub Run_WithDefaultStep_MovesImagesIntoExpectedFolders()
        ' L*(black) = 0, L*(gray 128) ≈ 53.6, L*(white) = 100.
        Me.WriteImage("black.png", Color.Black)
        Me.WriteImage("gray.bmp", Color.FromArgb(255, 128, 128, 128))
        Me.WriteImage("white.png", Color.White)

        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(Me.tempDir.FullPath)

        Assert.AreEqual(0, result.ExitCode, result.ToString())
        Me.AssertMoved("black.png", "Light 00%-10%")
        Me.AssertMoved("gray.bmp", "Light 50%-60%")
        Me.AssertMoved("white.png", "Light 90%-100%")
        Assert.Contains("Success: 3, Failed: 0", result.StandardOutput)
    End Sub

    <TestMethod>
    Public Sub Run_WithCustomStep_UsesCustomFolderRanges()
        Me.WriteImage("black.png", Color.Black)
        Me.WriteImage("gray.png", Color.FromArgb(255, 128, 128, 128))
        Me.WriteImage("white.png", Color.White)

        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(Me.tempDir.FullPath, "5")

        Assert.AreEqual(0, result.ExitCode, result.ToString())
        Me.AssertMoved("black.png", "Light 00%-05%")
        Me.AssertMoved("gray.png", "Light 50%-55%")
        Me.AssertMoved("white.png", "Light 95%-100%")
        Assert.Contains("5% steps", result.StandardOutput)
    End Sub

    <TestMethod>
    Public Sub Run_WithEverySupportedFormat_ProcessesAllOfThem()
        Me.tempDir.WriteFile("a.jpg", TestImageFactory.EncodeSolidWithGdi(Color.White, ImageFormat.Jpeg))
        Me.tempDir.WriteFile("b.JPEG", TestImageFactory.EncodeSolidWithGdi(Color.White, ImageFormat.Jpeg))
        Me.tempDir.WriteFile("c.tif", TestImageFactory.EncodeSolidWithGdi(Color.White, ImageFormat.Tiff))
        Me.tempDir.WriteFile("d.tiff", TestImageFactory.EncodeSolidWithGdi(Color.White, ImageFormat.Tiff))
        Me.tempDir.WriteFile("e.bmp", TestImageFactory.EncodeSolidWithGdi(Color.White, ImageFormat.Bmp))
        Me.tempDir.WriteFile("f.PNG", TestImageFactory.EncodeSolidWithGdi(Color.White, ImageFormat.Png))
        Me.tempDir.WriteFile("g.webp", TestImageFactory.EncodeSolidWithSkia(Color.White, SKEncodedImageFormat.Webp))
        Me.tempDir.WriteFile("h.ico", TestImageFactory.EncodeSolidIco(Color.White))

        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(Me.tempDir.FullPath)

        Assert.AreEqual(0, result.ExitCode, result.ToString())
        For Each fileName As String In {"a.jpg", "b.JPEG", "c.tif", "d.tiff", "e.bmp", "f.PNG", "g.webp", "h.ico"}
            Me.AssertMoved(fileName, "Light 90%-100%")
        Next
        Assert.Contains("Success: 8, Failed: 0", result.StandardOutput)
    End Sub

    <TestMethod>
    Public Sub Run_ProcessesTopLevelFilesOnly()
        Me.WriteImage("top.png", Color.White)
        Me.WriteImage(Path.Combine("SubFolder", "nested.png"), Color.White)
        Me.tempDir.WriteFile("readme.txt", New Byte() {65})

        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(Me.tempDir.FullPath)

        Assert.AreEqual(0, result.ExitCode, result.ToString())
        Me.AssertMoved("top.png", "Light 90%-100%")
        Assert.IsTrue(File.Exists(Me.tempDir.Combine("SubFolder", "nested.png")), "Files in sub folders must not be processed.")
        Assert.IsTrue(File.Exists(Me.tempDir.Combine("readme.txt")), "Unsupported files must not be moved.")
    End Sub

    <TestMethod>
    Public Sub Run_ManyFilesInParallel_CountsEveryFileExactlyOnce()
        ' Regression test for the non-atomic success/failure counters updated from parallel threads.
        Const FileCount As Integer = 120
        For i As Integer = 0 To FileCount - 1
            Dim level As Integer = (i * 2) Mod 256
            Me.WriteImage($"image_{i:D3}.png", Color.FromArgb(255, level, level, level), size:=64)
        Next

        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(Me.tempDir.FullPath)

        Assert.AreEqual(0, result.ExitCode, result.ToString())
        Assert.Contains($"Success: {FileCount}, Failed: 0", result.StandardOutput)
        Assert.IsEmpty(Directory.GetFiles(Me.tempDir.FullPath, "*.png", SearchOption.TopDirectoryOnly))

        Dim movedFiles As String() = Directory.GetFiles(Me.tempDir.Combine(ProgramIntegrationTests.OutputFolderName), "*.png", SearchOption.AllDirectories)
        Assert.HasCount(FileCount, movedFiles)
    End Sub

    <TestMethod>
    Public Sub Run_TwiceOnSameDirectory_SecondRunFindsNoTopLevelImages()
        Me.WriteImage("white.png", Color.White)

        Dim firstRun As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(Me.tempDir.FullPath)
        Dim secondRun As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(Me.tempDir.FullPath)

        Assert.AreEqual(0, firstRun.ExitCode, firstRun.ToString())
        Assert.AreEqual(4, secondRun.ExitCode, secondRun.ToString())
        Me.AssertMoved("white.png", "Light 90%-100%")
    End Sub

    <TestMethod>
    Public Sub Run_WithSourcePathContainingSpacesAndTrailingSeparator_Works()
        Dim sourceDir As String = Me.tempDir.Combine("My Pictures")
        Directory.CreateDirectory(sourceDir)
        File.WriteAllBytes(Path.Combine(sourceDir, "white.png"), TestImageFactory.EncodeSolidWithGdi(Color.White, ImageFormat.Png))

        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run($"{sourceDir}{Path.DirectorySeparatorChar}")

        Assert.AreEqual(0, result.ExitCode, result.ToString())
        Assert.IsTrue(File.Exists(Path.Combine(sourceDir, ProgramIntegrationTests.OutputFolderName, "Light 90%-100%", "white.png")))
    End Sub

#End Region

#Region " Partial Failures "

    <TestMethod>
    Public Sub Run_WhenDestinationFileAlreadyExists_DoesNotOverwriteAndExitsWithCode1()
        Me.WriteImage("black.png", Color.Black)
        Me.WriteImage("white.png", Color.White)
        Dim existingContent As Byte() = New Byte() {1, 2, 3, 4}
        Me.tempDir.WriteFile(Path.Combine(ProgramIntegrationTests.OutputFolderName, "Light 00%-10%", "black.png"), existingContent)

        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(Me.tempDir.FullPath)

        Assert.AreEqual(1, result.ExitCode, result.ToString())
        Assert.IsTrue(File.Exists(Me.tempDir.Combine("black.png")), "The source file must stay in place when the destination already exists.")
        CollectionAssert.AreEqual(existingContent,
                                  File.ReadAllBytes(Me.tempDir.Combine(ProgramIntegrationTests.OutputFolderName, "Light 00%-10%", "black.png")),
                                  "The existing destination file must not be overwritten.")
        Me.AssertMoved("white.png", "Light 90%-100%")
        Assert.Contains("[WARN]", result.StandardOutput)
        Assert.Contains("Success: 1, Failed: 1", result.StandardOutput)
    End Sub

    <TestMethod>
    Public Sub Run_WithCorruptedImages_ReportsErrorsAndContinuesWithTheRest()
        Me.tempDir.WriteFile("broken.png", TestImageFactory.CreateGarbageBytes())
        Me.tempDir.WriteFile("broken.webp", TestImageFactory.CreateGarbageBytes())
        Me.WriteImage("white.png", Color.White)

        Dim result As ConsoleAppRunner.AppRunResult = ConsoleAppRunner.Run(Me.tempDir.FullPath)

        Assert.AreEqual(1, result.ExitCode, result.ToString())
        Assert.IsTrue(File.Exists(Me.tempDir.Combine("broken.png")))
        Assert.IsTrue(File.Exists(Me.tempDir.Combine("broken.webp")))
        Me.AssertMoved("white.png", "Light 90%-100%")
        Assert.Contains("Corrupted file or unsupported image format", result.StandardOutput)
        Assert.Contains("Success: 1, Failed: 2", result.StandardOutput)
    End Sub

#End Region

#Region " Private Helpers "

    ''' <summary>
    ''' Writes a solid color PNG or BMP image into the temporary source directory.
    ''' </summary>
    '''
    ''' <param name="relativePath">
    ''' The relative file path. The extension determines the image format (.png or .bmp).
    ''' </param>
    '''
    ''' <param name="fillColor">
    ''' The fill color.
    ''' </param>
    '''
    ''' <param name="size">
    ''' The image width and height, in pixels.
    ''' </param>
    Private Sub WriteImage(relativePath As String, fillColor As Color, Optional size As Integer = 16)

        Dim format As ImageFormat
        Select Case Path.GetExtension(relativePath).ToLowerInvariant()
            Case ".png"
                format = ImageFormat.Png
            Case ".bmp"
                format = ImageFormat.Bmp
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(relativePath), relativePath, "Only .png and .bmp files are supported by this helper.")
        End Select

        Me.tempDir.WriteFile(relativePath, TestImageFactory.EncodeSolidWithGdi(fillColor, format, size, size))
    End Sub

    ''' <summary>
    ''' Asserts that the specified source file was moved into the specified lightness folder.
    ''' </summary>
    '''
    ''' <param name="fileName">
    ''' The source file name.
    ''' </param>
    '''
    ''' <param name="expectedFolderName">
    ''' The expected lightness folder name (e.g., "Light 90%-100%").
    ''' </param>
    Private Sub AssertMoved(fileName As String, expectedFolderName As String)

        Dim sourcePath As String = Me.tempDir.Combine(fileName)
        Dim expectedPath As String = Me.tempDir.Combine(ProgramIntegrationTests.OutputFolderName, expectedFolderName, fileName)

        Assert.IsFalse(File.Exists(sourcePath), $"The source file was not moved: {sourcePath}")
        Assert.IsTrue(File.Exists(expectedPath), $"The file was not found in the expected folder: {expectedPath}")
    End Sub

#End Region

End Class

#End Region
