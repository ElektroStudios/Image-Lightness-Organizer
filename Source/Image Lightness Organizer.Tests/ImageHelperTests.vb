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

#Region " ImageHelperTests "

''' <summary>
''' Unit tests for <see cref="ImageHelper"/> module, using real image files written to a temporary directory.
''' </summary>
<TestClass>
<OSCondition(OperatingSystems.Windows)>
Public NotInheritable Class ImageHelperTests

#Region " Constants "

    ''' <summary>
    ''' The HRESULT value of E_INVALIDARG, raised by GDI+ when an image cannot be decoded.
    ''' <para></para>
    ''' The application relies on this value to report corrupted files.
    ''' </summary>
    Private Const HResultInvalidArgument As Integer = -2147024809

    ''' <summary>
    ''' The tolerance used for lossy (JPEG) image formats.
    ''' </summary>
    Private Const LossyTolerance As Double = 1.0R

#End Region

#Region " Fields "

    ''' <summary>
    ''' The temporary directory used by the current test.
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

#Region " Argument Validation "

    <TestMethod>
    Public Sub ComputeAverageLightness_NullPath_ThrowsArgumentNullException()
        Assert.ThrowsExactly(Of ArgumentNullException)(Sub() ImageHelper.ComputeAverageLightness(Nothing))
    End Sub

    <TestMethod>
    <DataRow("")>
    <DataRow("   ")>
    Public Sub ComputeAverageLightness_EmptyOrWhiteSpacePath_ThrowsArgumentException(filePath As String)
        Assert.ThrowsExactly(Of ArgumentException)(Sub() ImageHelper.ComputeAverageLightness(filePath))
    End Sub

    <TestMethod>
    <DataRow("missing.png")>
    <DataRow("missing.jpg")>
    <DataRow("missing.webp")>
    <DataRow("missing.ico")>
    <DataRow("missing.avif")>
    Public Sub ComputeAverageLightness_MissingFile_ThrowsFileNotFoundException(fileName As String)
        Dim filePath As String = Me.tempDir.Combine(fileName)

        Assert.ThrowsExactly(Of FileNotFoundException)(Sub() ImageHelper.ComputeAverageLightness(filePath))
    End Sub

#End Region

#Region " GDI+ Formats "

    <TestMethod>
    <DataRow("png", 128, 128, 128)>
    <DataRow("png", 200, 50, 10)>
    <DataRow("bmp", 128, 128, 128)>
    <DataRow("bmp", 10, 50, 200)>
    <DataRow("tif", 77, 150, 33)>
    <DataRow("tiff", 0, 0, 0)>
    <DataRow("png", 255, 255, 255)>
    Public Sub ComputeAverageLightness_LosslessGdiFormat_MatchesReference(extension As String, r As Integer, g As Integer, b As Integer)
        Dim format As ImageFormat = ImageHelperTests.GetGdiImageFormat(extension)
        Dim filePath As String = Me.tempDir.WriteFile($"image.{extension}",
                                                      TestImageFactory.EncodeSolidWithGdi(Color.FromArgb(255, r, g, b), format))
        Dim expected As Double = ReferenceColorMath.Lightness(CByte(r), CByte(g), CByte(b))

        Dim result As Double = ImageHelper.ComputeAverageLightness(filePath)

        Assert.AreEqual(expected, result, ReferenceColorMath.LightnessTolerance)
    End Sub

    <TestMethod>
    <DataRow("jpg")>
    <DataRow("jpeg")>
    <DataRow("JPG")>
    Public Sub ComputeAverageLightness_Jpeg_MatchesReferenceWithinLossyTolerance(extension As String)
        Dim filePath As String = Me.tempDir.WriteFile($"image.{extension}",
                                                      TestImageFactory.EncodeSolidWithGdi(Color.FromArgb(255, 128, 128, 128), ImageFormat.Jpeg))
        Dim expected As Double = ReferenceColorMath.Lightness(128, 128, 128)

        Dim result As Double = ImageHelper.ComputeAverageLightness(filePath)

        Assert.AreEqual(expected, result, ImageHelperTests.LossyTolerance)
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_SemiTransparentPng_UsesStraightColorValues()
        Dim filePath As String = Me.tempDir.WriteFile("image.png",
                                                      TestImageFactory.EncodeSolidWithGdi(Color.FromArgb(128, 255, 255, 255), ImageFormat.Png))

        Dim result As Double = ImageHelper.ComputeAverageLightness(filePath)

        Assert.AreEqual(100.0R, result, ReferenceColorMath.LightnessTolerance)
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_FullyTransparentPng_ReturnsZero()
        Dim filePath As String = Me.tempDir.WriteFile("image.png",
                                                      TestImageFactory.EncodeSolidWithGdi(Color.FromArgb(0, 255, 255, 255), ImageFormat.Png))

        Assert.AreEqual(0.0R, ImageHelper.ComputeAverageLightness(filePath), ReferenceColorMath.LightnessTolerance)
        Assert.AreEqual(100.0R, ImageHelper.ComputeAverageLightness(filePath, alphaThreshold:=0), ReferenceColorMath.LightnessTolerance)
    End Sub

    <TestMethod>
    <DataRow("png")>
    <DataRow("jpg")>
    <DataRow("bmp")>
    <DataRow("tif")>
    Public Sub ComputeAverageLightness_CorruptedGdiFile_ThrowsArgumentExceptionWithInvalidArgumentHResult(extension As String)
        Dim filePath As String = Me.tempDir.WriteFile($"broken.{extension}", TestImageFactory.CreateGarbageBytes())

        Dim ex As ArgumentException = Assert.Throws(Of ArgumentException)(Sub() ImageHelper.ComputeAverageLightness(filePath))

        Assert.AreEqual(ImageHelperTests.HResultInvalidArgument, ex.HResult,
                        "Program relies on this HRESULT to report corrupted files with a specific message.")
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_EmptyGdiFile_ThrowsArgumentException()
        Dim filePath As String = Me.tempDir.WriteFile("empty.png", Array.Empty(Of Byte)())

        Assert.Throws(Of ArgumentException)(Sub() ImageHelper.ComputeAverageLightness(filePath))
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_GdiFileWithMismatchedExtension_IsDecodedByContent()
        ' PNG content saved with a .jpg extension.
        Dim filePath As String = Me.tempDir.WriteFile("actually-a-png.jpg",
                                                      TestImageFactory.EncodeSolidWithGdi(Color.FromArgb(255, 60, 120, 180), ImageFormat.Png))

        Dim result As Double = ImageHelper.ComputeAverageLightness(filePath)

        Assert.AreEqual(ReferenceColorMath.Lightness(60, 120, 180), result, ReferenceColorMath.LightnessTolerance)
    End Sub

#End Region

#Region " SkiaSharp Formats "

    <TestMethod>
    <DataRow(128, 128, 128)>
    <DataRow(200, 50, 10)>
    <DataRow(10, 50, 200)>
    <DataRow(0, 0, 0)>
    <DataRow(255, 255, 255)>
    Public Sub ComputeAverageLightness_LosslessWebp_MatchesReference(r As Integer, g As Integer, b As Integer)
        Dim filePath As String = Me.tempDir.WriteFile("image.webp",
                                                      TestImageFactory.EncodeSolidWithSkia(Color.FromArgb(255, r, g, b), SKEncodedImageFormat.Webp))
        Dim expected As Double = ReferenceColorMath.Lightness(CByte(r), CByte(g), CByte(b))

        Dim result As Double = ImageHelper.ComputeAverageLightness(filePath)

        Assert.AreEqual(expected, result, ReferenceColorMath.LightnessTolerance)
    End Sub

    <TestMethod>
    <DataRow(128, 128, 128)>
    <DataRow(200, 50, 10)>
    <DataRow(10, 50, 200)>
    Public Sub ComputeAverageLightness_IcoWithEmbeddedPng_MatchesReference(r As Integer, g As Integer, b As Integer)
        Dim filePath As String = Me.tempDir.WriteFile("image.ico", TestImageFactory.EncodeSolidIco(Color.FromArgb(255, r, g, b)))
        Dim expected As Double = ReferenceColorMath.Lightness(CByte(r), CByte(g), CByte(b))

        Dim result As Double = ImageHelper.ComputeAverageLightness(filePath)

        Assert.AreEqual(expected, result, ReferenceColorMath.LightnessTolerance)
    End Sub

    <TestMethod>
    <DataRow("image.WEBP")>
    <DataRow("image.WebP")>
    Public Sub ComputeAverageLightness_UppercaseSkiaExtension_IsDecodedWithSkia(fileName As String)
        ' GDI+ cannot decode WebP, so this only succeeds if the extension check is case-insensitive.
        Dim filePath As String = Me.tempDir.WriteFile(fileName,
                                                      TestImageFactory.EncodeSolidWithSkia(Color.FromArgb(255, 90, 90, 90), SKEncodedImageFormat.Webp))

        Dim result As Double = ImageHelper.ComputeAverageLightness(filePath)

        Assert.AreEqual(ReferenceColorMath.Lightness(90, 90, 90), result, ReferenceColorMath.LightnessTolerance)
    End Sub

    <TestMethod>
    <DataRow(200, 50, 10)>
    <DataRow(10, 50, 200)>
    <DataRow(33, 66, 99)>
    Public Sub ComputeAverageLightness_SkiaAndGdiPaths_ProduceIdenticalResults(r As Integer, g As Integer, b As Integer)
        ' Non-gray colors make sure the SkiaSharp path does not swap the red and blue channels.
        Dim fillColor As Color = Color.FromArgb(255, r, g, b)
        Dim pngPath As String = Me.tempDir.WriteFile("image.png", TestImageFactory.EncodeSolidWithGdi(fillColor, ImageFormat.Png))
        Dim webpPath As String = Me.tempDir.WriteFile("image.webp", TestImageFactory.EncodeSolidWithSkia(fillColor, SKEncodedImageFormat.Webp))

        Dim gdiResult As Double = ImageHelper.ComputeAverageLightness(pngPath)
        Dim skiaResult As Double = ImageHelper.ComputeAverageLightness(webpPath)

        Assert.AreEqual(gdiResult, skiaResult, 0.000000001R)
    End Sub

    <TestMethod>
    <DataRow(128)>
    <DataRow(64)>
    <DataRow(200)>
    Public Sub ComputeAverageLightness_SemiTransparentWebp_IsNotDarkenedByPremultipliedAlpha(alpha As Integer)
        ' Regression test: SkiaSharp decodes as premultiplied alpha by default, which used to darken
        ' semi-transparent pixels (e.g., white at 50% alpha was measured as L* ≈ 53.6 instead of 100).
        Dim filePath As String = Me.tempDir.WriteFile("image.webp",
                                                      TestImageFactory.EncodeSolidWithSkia(Color.FromArgb(alpha, 255, 255, 255), SKEncodedImageFormat.Webp))

        Dim result As Double = ImageHelper.ComputeAverageLightness(filePath)

        Assert.AreEqual(100.0R, result, ReferenceColorMath.LightnessTolerance)
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_SemiTransparentIco_IsNotDarkenedByPremultipliedAlpha()
        Dim filePath As String = Me.tempDir.WriteFile("image.ico", TestImageFactory.EncodeSolidIco(Color.FromArgb(128, 255, 255, 255)))

        Dim result As Double = ImageHelper.ComputeAverageLightness(filePath)

        Assert.AreEqual(100.0R, result, ReferenceColorMath.LightnessTolerance)
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_SkiaFormat_AlphaThresholdIsApplied()
        Dim filePath As String = Me.tempDir.WriteFile("image.ico", TestImageFactory.EncodeSolidIco(Color.FromArgb(100, 255, 255, 255)))

        Assert.AreEqual(100.0R, ImageHelper.ComputeAverageLightness(filePath, alphaThreshold:=100), ReferenceColorMath.LightnessTolerance)
        Assert.AreEqual(0.0R, ImageHelper.ComputeAverageLightness(filePath, alphaThreshold:=101), ReferenceColorMath.LightnessTolerance)
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_SkiaFileWithMismatchedContent_IsDecodedByContent()
        ' PNG content saved with a .webp extension: SkiaSharp detects the real format from the content.
        Dim filePath As String = Me.tempDir.WriteFile("actually-a-png.webp",
                                                      TestImageFactory.EncodeSolidWithSkia(Color.FromArgb(255, 60, 120, 180), SKEncodedImageFormat.Png))

        Dim result As Double = ImageHelper.ComputeAverageLightness(filePath)

        Assert.AreEqual(ReferenceColorMath.Lightness(60, 120, 180), result, ReferenceColorMath.LightnessTolerance)
    End Sub

    <TestMethod>
    <DataRow("broken.webp")>
    <DataRow("broken.ico")>
    <DataRow("broken.avif")>
    Public Sub ComputeAverageLightness_CorruptedSkiaFile_ThrowsInvalidOperationException(fileName As String)
        Dim filePath As String = Me.tempDir.WriteFile(fileName, TestImageFactory.CreateGarbageBytes())

        Assert.ThrowsExactly(Of InvalidOperationException)(Sub() ImageHelper.ComputeAverageLightness(filePath))
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_EmptySkiaFile_ThrowsInvalidOperationException()
        Dim filePath As String = Me.tempDir.WriteFile("empty.webp", Array.Empty(Of Byte)())

        Assert.ThrowsExactly(Of InvalidOperationException)(Sub() ImageHelper.ComputeAverageLightness(filePath))
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_TruncatedWebp_ThrowsInvalidOperationException()
        Dim fullBytes As Byte() = TestImageFactory.EncodeSolidWithSkia(Color.FromArgb(255, 1, 2, 3), SKEncodedImageFormat.Webp, 64, 64)
        Dim truncatedBytes As Byte() = fullBytes.Take(fullBytes.Length \ 2).ToArray()
        Dim filePath As String = Me.tempDir.WriteFile("truncated.webp", truncatedBytes)

        Assert.ThrowsExactly(Of InvalidOperationException)(Sub() ImageHelper.ComputeAverageLightness(filePath))
    End Sub

#End Region

#Region " File Handling "

    <TestMethod>
    <DataRow("image.png")>
    <DataRow("image.webp")>
    Public Sub ComputeAverageLightness_Success_ReleasesTheFile(fileName As String)
        Dim content As Byte() = If(fileName.EndsWith(".webp", StringComparison.Ordinal),
                                   TestImageFactory.EncodeSolidWithSkia(Color.Gray, SKEncodedImageFormat.Webp),
                                   TestImageFactory.EncodeSolidWithGdi(Color.Gray, ImageFormat.Png))
        Dim filePath As String = Me.tempDir.WriteFile(fileName, content)

        ImageHelper.ComputeAverageLightness(filePath)

        ' Program moves the file right after computing its lightness, so it must not remain locked.
        Using exclusive As New FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
            Assert.IsTrue(exclusive.CanWrite)
        End Using
    End Sub

    <TestMethod>
    <DataRow("broken.png")>
    <DataRow("broken.webp")>
    Public Sub ComputeAverageLightness_Failure_ReleasesTheFile(fileName As String)
        Dim filePath As String = Me.tempDir.WriteFile(fileName, TestImageFactory.CreateGarbageBytes())

        Try
            ImageHelper.ComputeAverageLightness(filePath)
            Assert.Fail("An exception was expected for a corrupted file.")
        Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is InvalidOperationException
            ' Expected.
        End Try

        Using exclusive As New FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
            Assert.IsTrue(exclusive.CanWrite)
        End Using
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_FileOpenedForReadingByAnotherProcess_CanStillBeRead()
        Dim filePath As String = Me.tempDir.WriteFile("shared.png", TestImageFactory.EncodeSolidWithGdi(Color.White, ImageFormat.Png))

        Using otherReader As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
            Assert.AreEqual(100.0R, ImageHelper.ComputeAverageLightness(filePath), ReferenceColorMath.LightnessTolerance)
        End Using
    End Sub

    <TestMethod>
    <DataRow("image.png")>
    <DataRow("image.webp")>
    Public Sub ComputeAverageLightness_ExtendedLengthPath_IsSupported(fileName As String)
        Dim content As Byte() = If(fileName.EndsWith(".webp", StringComparison.Ordinal),
                                   TestImageFactory.EncodeSolidWithSkia(Color.White, SKEncodedImageFormat.Webp),
                                   TestImageFactory.EncodeSolidWithGdi(Color.White, ImageFormat.Png))
        Dim filePath As String = Me.tempDir.WriteFile(fileName, content)
        Dim extendedPath As String = FileSystemHelper.GetExtendedPath(filePath)

        Dim result As Double = ImageHelper.ComputeAverageLightness(extendedPath)

        Assert.StartsWith("\\?\", extendedPath)
        Assert.AreEqual(100.0R, result, ReferenceColorMath.LightnessTolerance)
    End Sub

#End Region

#Region " IsSkiaDecodedFormat "

    <TestMethod>
    <DataRow("image.webp")>
    <DataRow("image.ico")>
    <DataRow("image.avif")>
    <DataRow("IMAGE.WEBP")>
    <DataRow("C:\Folder\Image.Ico")>
    Public Sub IsSkiaDecodedFormat_SkiaFormats_ReturnsTrue(filePath As String)
        Assert.IsTrue(ImageHelper.IsSkiaDecodedFormat(filePath))
    End Sub

    <TestMethod>
    <DataRow("image.png")>
    <DataRow("image.jpg")>
    <DataRow("image.jpeg")>
    <DataRow("image.bmp")>
    <DataRow("image.tif")>
    <DataRow("image.tiff")>
    <DataRow("image")>
    <DataRow("")>
    Public Sub IsSkiaDecodedFormat_OtherFormats_ReturnsFalse(filePath As String)
        Assert.IsFalse(ImageHelper.IsSkiaDecodedFormat(filePath))
    End Sub

    <TestMethod>
    Public Sub IsSkiaDecodedFormat_Nothing_ThrowsArgumentNullException()
        Assert.ThrowsExactly(Of ArgumentNullException)(Sub() ImageHelper.IsSkiaDecodedFormat(Nothing))
    End Sub

#End Region

#Region " Private Helpers "

    ''' <summary>
    ''' Gets the GDI+ image format that corresponds to the specified file extension.
    ''' </summary>
    '''
    ''' <param name="extension">
    ''' The file extension, without the leading dot.
    ''' </param>
    '''
    ''' <returns>
    ''' The GDI+ image format.
    ''' </returns>
    Private Shared Function GetGdiImageFormat(extension As String) As ImageFormat

        Select Case extension.ToLowerInvariant()
            Case "png"
                Return ImageFormat.Png
            Case "bmp"
                Return ImageFormat.Bmp
            Case "tif", "tiff"
                Return ImageFormat.Tiff
            Case "jpg", "jpeg"
                Return ImageFormat.Jpeg
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(extension), extension, "Unsupported GDI+ test image format.")
        End Select
    End Function

#End Region

End Class

#End Region
