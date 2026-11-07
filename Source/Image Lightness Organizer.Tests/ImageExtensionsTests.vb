#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Drawing
Imports System.Drawing.Imaging

#End Region

#Region " ImageExtensionsTests "

''' <summary>
''' Unit tests for <see cref="ImageExtensions"/> module.
''' </summary>
<TestClass>
<OSCondition(OperatingSystems.Windows)>
Public NotInheritable Class ImageExtensionsTests

#Region " Constants "

    ''' <summary>
    ''' The tolerance used for comparisons that are mathematically exact.
    ''' </summary>
    Private Const ExactTolerance As Double = 0.000000001R

#End Region

#Region " Tests "

    <TestMethod>
    Public Sub ComputeAverageLightness_NullImage_ThrowsArgumentNullException()
        Dim ex As ArgumentNullException =
            Assert.ThrowsExactly(Of ArgumentNullException)(Sub() ImageExtensions.ComputeAverageLightness(CType(Nothing, Image)))

        Assert.AreEqual("sourceImage", ex.ParamName)
    End Sub

    <TestMethod>
    <DataRow(PixelFormat.Format24bppRgb)>
    <DataRow(PixelFormat.Format32bppRgb)>
    <DataRow(PixelFormat.Format32bppPArgb)>
    <DataRow(PixelFormat.Format32bppArgb)>
    <DataRow(PixelFormat.Format16bppRgb565)>
    <DataRow(PixelFormat.Format16bppRgb555)>
    Public Sub ComputeAverageLightness_AnyNonIndexedFormat_IsConvertedAndComputed(pixelFormat As PixelFormat)
        ' Pure black and pure white are represented exactly in every one of these formats.
        Using bmp As Bitmap = TestImageFactory.CreateSplitBitmap(16, 8, Color.Black, Color.White, pixelFormat)
            Dim result As Double = DirectCast(bmp, Image).ComputeAverageLightness()

            Assert.AreEqual(50.0R, result, ImageExtensionsTests.ExactTolerance)
        End Using
    End Sub

    <TestMethod>
    <DataRow(128, 128, 128)>
    <DataRow(200, 50, 10)>
    <DataRow(10, 50, 200)>
    Public Sub ComputeAverageLightness_24bppImage_MatchesEquivalent32bppImage(r As Integer, g As Integer, b As Integer)
        Dim fillColor As Color = Color.FromArgb(255, r, g, b)

        Using bmp24 As Bitmap = TestImageFactory.CreateSolidBitmap(8, 8, fillColor, PixelFormat.Format24bppRgb),
              bmp32 As Bitmap = TestImageFactory.CreateSolidBitmap(8, 8, fillColor, PixelFormat.Format32bppArgb)

            Dim result24 As Double = DirectCast(bmp24, Image).ComputeAverageLightness()
            Dim result32 As Double = DirectCast(bmp32, Image).ComputeAverageLightness()

            Assert.AreEqual(result32, result24, ImageExtensionsTests.ExactTolerance)
            Assert.AreEqual(ReferenceColorMath.Lightness(CByte(r), CByte(g), CByte(b)), result24, ReferenceColorMath.LightnessTolerance)
        End Using
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_IndexedImage_UsesPaletteColors()
        Using bmp As New Bitmap(8, 8, PixelFormat.Format8bppIndexed)
            ' A new indexed bitmap is zero-initialized, so every pixel references palette entry 0.
            Dim paletteColor As Color = bmp.Palette.Entries(0)
            Dim expected As Double = ReferenceColorMath.Lightness(paletteColor.R, paletteColor.G, paletteColor.B)

            Dim result As Double = DirectCast(bmp, Image).ComputeAverageLightness()

            Assert.AreEqual(expected, result, ReferenceColorMath.LightnessTolerance)
        End Using
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_ConvertedImage_IsNotDisposedNorModified()
        Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(8, 8, Color.FromArgb(255, 10, 20, 30), PixelFormat.Format24bppRgb)

            DirectCast(bmp, Image).ComputeAverageLightness()

            ' Accessing these members would throw if the source image had been disposed.
            Assert.AreEqual(8, bmp.Width)
            Assert.AreEqual(PixelFormat.Format24bppRgb, bmp.PixelFormat)
            Assert.AreEqual(Color.FromArgb(255, 10, 20, 30).ToArgb(), bmp.GetPixel(3, 3).ToArgb())
        End Using
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_32bppArgbImage_IsNotDisposed()
        Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(8, 8, Color.White)

            DirectCast(bmp, Image).ComputeAverageLightness()

            Assert.AreEqual(Color.White.ToArgb(), bmp.GetPixel(0, 0).ToArgb())
        End Using
    End Sub

    <TestMethod>
    <DataRow(0, 100.0R)>  ' Threshold 0: the fully transparent white pixels are included.
    <DataRow(8, 0.0R)>    ' Default threshold: the fully transparent pixels are excluded.
    Public Sub ComputeAverageLightness_AlphaThreshold_IsForwarded(threshold As Integer, expected As Double)
        Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(8, 8, Color.FromArgb(0, 255, 255, 255))
            Dim result As Double = DirectCast(bmp, Image).ComputeAverageLightness(CByte(threshold))

            Assert.AreEqual(expected, result, ImageExtensionsTests.ExactTolerance)
        End Using
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_ImageExtension_MatchesBitmapExtensionFor32bppArgb()
        Using bmp As Bitmap = TestImageFactory.CreateSplitBitmap(20, 10, Color.FromArgb(255, 15, 99, 180), Color.FromArgb(120, 240, 10, 60))
            Dim viaImage As Double = ImageExtensions.ComputeAverageLightness(bmp)
            Dim viaBitmap As Double = BitmapExtensions.ComputeAverageLightness(bmp)

            Assert.AreEqual(viaBitmap, viaImage)
        End Using
    End Sub

#End Region

End Class

#End Region
