#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Drawing
Imports System.Drawing.Imaging

#End Region

#Region " BitmapExtensionsTests "

''' <summary>
''' Unit tests for <see cref="BitmapExtensions"/> module.
''' </summary>
<TestClass>
<OSCondition(OperatingSystems.Windows)>
Public NotInheritable Class BitmapExtensionsTests

#Region " Constants "

    ''' <summary>
    ''' The tolerance used for comparisons that are mathematically exact.
    ''' </summary>
    Private Const ExactTolerance As Double = 0.000000001R

#End Region

#Region " Argument Validation "

    <TestMethod>
    Public Sub ComputeAverageLightness_NullBitmap_ThrowsArgumentNullException()
        Dim ex As ArgumentNullException =
            Assert.ThrowsExactly(Of ArgumentNullException)(Sub() BitmapExtensions.ComputeAverageLightness(CType(Nothing, Bitmap)))

        Assert.AreEqual("sourceBitmap", ex.ParamName)
    End Sub

    <TestMethod>
    <DataRow(PixelFormat.Format24bppRgb)>
    <DataRow(PixelFormat.Format32bppRgb)>
    <DataRow(PixelFormat.Format32bppPArgb)>
    <DataRow(PixelFormat.Format16bppRgb565)>
    <DataRow(PixelFormat.Format8bppIndexed)>
    Public Sub ComputeAverageLightness_UnsupportedPixelFormat_ThrowsNotSupportedException(pixelFormat As PixelFormat)
        Using bmp As New Bitmap(4, 4, pixelFormat)
            Assert.ThrowsExactly(Of NotSupportedException)(Sub() bmp.ComputeAverageLightness())
        End Using
    End Sub

#End Region

#Region " Solid Colors "

    <TestMethod>
    Public Sub ComputeAverageLightness_Black_ReturnsZero()
        Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(8, 8, Color.FromArgb(255, 0, 0, 0))
            Assert.AreEqual(0.0R, bmp.ComputeAverageLightness(), BitmapExtensionsTests.ExactTolerance)
        End Using
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_White_ReturnsOneHundred()
        Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(8, 8, Color.FromArgb(255, 255, 255, 255))
            Assert.AreEqual(100.0R, bmp.ComputeAverageLightness(), BitmapExtensionsTests.ExactTolerance)
        End Using
    End Sub

    <TestMethod>
    <DataRow(255, 0, 0, 53.23R)>   ' Pure red.
    <DataRow(0, 255, 0, 87.74R)>   ' Pure green.
    <DataRow(0, 0, 255, 32.30R)>   ' Pure blue.
    <DataRow(255, 255, 0, 97.14R)> ' Yellow.
    <DataRow(119, 119, 119, 50.04R)>
    Public Sub ComputeAverageLightness_KnownColors_ReturnsWellKnownLightness(r As Integer, g As Integer, b As Integer, expected As Double)
        Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(8, 8, Color.FromArgb(255, r, g, b))
            Assert.AreEqual(expected, bmp.ComputeAverageLightness(), 0.01R)
        End Using
    End Sub

    <TestMethod>
    <DataRow(0, 0, 0)>
    <DataRow(1, 2, 3)>
    <DataRow(10, 10, 10)>      ' Linear segment of both the sRGB and the CIE L* curves.
    <DataRow(50, 100, 150)>
    <DataRow(128, 128, 128)>
    <DataRow(200, 50, 10)>
    <DataRow(10, 50, 200)>
    <DataRow(254, 254, 254)>
    <DataRow(255, 255, 255)>
    Public Sub ComputeAverageLightness_SolidColor_MatchesReferenceImplementation(r As Integer, g As Integer, b As Integer)
        Dim expected As Double = ReferenceColorMath.Lightness(CByte(r), CByte(g), CByte(b))

        Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(8, 8, Color.FromArgb(255, r, g, b))
            Assert.AreEqual(expected, bmp.ComputeAverageLightness(), ReferenceColorMath.LightnessTolerance)
        End Using
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_RedAndBlueChannels_AreNotSwapped()
        ' Red and blue have very different luminance weights (0.2126 vs. 0.0722),
        ' so a channel order mistake in the BGRA byte layout would be detected here.
        Using redBmp As Bitmap = TestImageFactory.CreateSolidBitmap(4, 4, Color.FromArgb(255, 255, 0, 0)),
              blueBmp As Bitmap = TestImageFactory.CreateSolidBitmap(4, 4, Color.FromArgb(255, 0, 0, 255))

            Assert.IsGreaterThan(blueBmp.ComputeAverageLightness() + 20.0R, redBmp.ComputeAverageLightness())
        End Using
    End Sub

#End Region

#Region " Averaging "

    <TestMethod>
    Public Sub ComputeAverageLightness_HalfBlackHalfWhite_ReturnsFifty()
        Using bmp As Bitmap = TestImageFactory.CreateSplitBitmap(16, 8, Color.Black, Color.White)
            Assert.AreEqual(50.0R, bmp.ComputeAverageLightness(), BitmapExtensionsTests.ExactTolerance)
        End Using
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_AllGrayLevels_ReturnsMeanOfPerPixelLightness()
        Using bmp As New Bitmap(256, 3, PixelFormat.Format32bppArgb)
            Dim expectedSum As Double = 0.0R
            For x As Integer = 0 To 255
                For y As Integer = 0 To 2
                    bmp.SetPixel(x, y, Color.FromArgb(255, x, x, x))
                Next
                expectedSum += ReferenceColorMath.Lightness(CByte(x), CByte(x), CByte(x))
            Next
            Dim expected As Double = expectedSum / 256.0R

            Assert.AreEqual(expected, bmp.ComputeAverageLightness(), ReferenceColorMath.LightnessTolerance)
        End Using
    End Sub

    <TestMethod>
    <DataRow(1, 1)>
    <DataRow(3, 7)>
    <DataRow(257, 1)>
    <DataRow(1, 257)>
    <DataRow(101, 99)>
    Public Sub ComputeAverageLightness_ArbitraryDimensions_ProcessesEveryPixel(width As Integer, height As Integer)
        ' Only the last pixel is white: if any row or column were skipped (e.g., a stride bug),
        ' the result would not match the expected weighted average.
        Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(width, height, Color.Black)
            bmp.SetPixel(width - 1, height - 1, Color.White)
            Dim expected As Double = 100.0R / (width * height)

            Assert.AreEqual(expected, bmp.ComputeAverageLightness(), BitmapExtensionsTests.ExactTolerance)
        End Using
    End Sub

#End Region

#Region " Alpha Threshold "

    <TestMethod>
    Public Sub ComputeAverageLightness_FullyTransparent_ReturnsZero()
        Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(8, 8, Color.FromArgb(0, 255, 255, 255))
            Assert.AreEqual(0.0R, bmp.ComputeAverageLightness(), BitmapExtensionsTests.ExactTolerance)
        End Using
    End Sub

    <TestMethod>
    <DataRow(8, 8, 50.0R)>     ' Alpha equal to the threshold: included.
    <DataRow(8, 7, 0.0R)>      ' Alpha below the threshold: excluded.
    <DataRow(8, 9, 50.0R)>
    <DataRow(0, 0, 50.0R)>     ' Threshold 0 includes every pixel, even fully transparent ones.
    <DataRow(1, 0, 0.0R)>
    <DataRow(255, 254, 0.0R)>  ' Threshold 255 includes only fully opaque pixels.
    <DataRow(255, 255, 50.0R)>
    Public Sub ComputeAverageLightness_AlphaThreshold_ExcludesOnlyPixelsStrictlyBelowThreshold(threshold As Integer, whiteAlpha As Integer, expected As Double)
        ' Left half: opaque black (always included). Right half: white with the specified alpha.
        Using bmp As Bitmap = TestImageFactory.CreateSplitBitmap(16, 4, Color.FromArgb(255, 0, 0, 0), Color.FromArgb(whiteAlpha, 255, 255, 255))
            Assert.AreEqual(expected, bmp.ComputeAverageLightness(CByte(threshold)), BitmapExtensionsTests.ExactTolerance)
        End Using
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_ThresholdMaxValueOnOpaqueImage_IsNotZero()
        ' Regression test: a threshold of 255 used to exclude every pixel (alpha <= 255 is always true).
        Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(8, 8, Color.White)
            Assert.AreEqual(100.0R, bmp.ComputeAverageLightness(Byte.MaxValue), BitmapExtensionsTests.ExactTolerance)
        End Using
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_DefaultThreshold_IsEight()
        Using included As Bitmap = TestImageFactory.CreateSplitBitmap(16, 4, Color.Black, Color.FromArgb(8, 255, 255, 255)),
              excluded As Bitmap = TestImageFactory.CreateSplitBitmap(16, 4, Color.Black, Color.FromArgb(7, 255, 255, 255))

            Assert.AreEqual(included.ComputeAverageLightness(8), included.ComputeAverageLightness(), BitmapExtensionsTests.ExactTolerance)
            Assert.AreEqual(50.0R, included.ComputeAverageLightness(), BitmapExtensionsTests.ExactTolerance)
            Assert.AreEqual(0.0R, excluded.ComputeAverageLightness(), BitmapExtensionsTests.ExactTolerance)
        End Using
    End Sub

    <TestMethod>
    <DataRow(16)>
    <DataRow(128)>
    <DataRow(254)>
    Public Sub ComputeAverageLightness_SemiTransparentPixels_UseStraightColorValues(alpha As Integer)
        ' Format32bppArgb stores straight (non-premultiplied) color, so alpha must not darken the result.
        Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(8, 8, Color.FromArgb(alpha, 255, 255, 255))
            Assert.AreEqual(100.0R, bmp.ComputeAverageLightness(), BitmapExtensionsTests.ExactTolerance)
        End Using
    End Sub

#End Region

#Region " Side Effects and Concurrency "

    <TestMethod>
    Public Sub ComputeAverageLightness_DoesNotModifyNorLockTheBitmap()
        Using bmp As Bitmap = TestImageFactory.CreateSplitBitmap(10, 10, Color.FromArgb(200, 10, 20, 30), Color.FromArgb(255, 220, 230, 240))
            Dim before As Color = bmp.GetPixel(0, 0)

            bmp.ComputeAverageLightness()

            ' GetPixel would throw if the bits were left locked.
            Assert.AreEqual(before.ToArgb(), bmp.GetPixel(0, 0).ToArgb())
            Assert.AreEqual(Color.FromArgb(255, 220, 230, 240).ToArgb(), bmp.GetPixel(9, 9).ToArgb())
            Assert.AreEqual(PixelFormat.Format32bppArgb, bmp.PixelFormat)
        End Using
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_RepeatedCalls_ReturnSameResult()
        Using bmp As Bitmap = TestImageFactory.CreateSplitBitmap(32, 32, Color.FromArgb(255, 30, 60, 90), Color.FromArgb(255, 200, 150, 100))
            Dim first As Double = bmp.ComputeAverageLightness()

            For i As Integer = 1 To 10
                Assert.AreEqual(first, bmp.ComputeAverageLightness())
            Next
        End Using
    End Sub

    <TestMethod>
    Public Sub ComputeAverageLightness_ConcurrentCallsOnDifferentBitmaps_AreThreadSafe()
        Const BitmapCount As Integer = 64
        Dim results As Double() = New Double(BitmapCount - 1) {}
        Dim expected As Double() = New Double(BitmapCount - 1) {}

        Parallel.For(0, BitmapCount,
                     Sub(i As Integer)
                         Dim level As Integer = (i * 4) Mod 256
                         expected(i) = ReferenceColorMath.Lightness(CByte(level), CByte(level), CByte(level))
                         Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(64, 64, Color.FromArgb(255, level, level, level))
                             results(i) = bmp.ComputeAverageLightness()
                         End Using
                     End Sub)

        For i As Integer = 0 To BitmapCount - 1
            Assert.AreEqual(expected(i), results(i), ReferenceColorMath.LightnessTolerance, $"Mismatch at index {i}.")
        Next
    End Sub

#End Region

End Class

#End Region
