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

#Region " TestImageFactory "

''' <summary>
''' Provides methods to create in-memory bitmaps and image files with known pixel values for testing purposes.
''' </summary>
Friend Module TestImageFactory

#Region " GDI+ Bitmaps "

    ''' <summary>
    ''' Creates a GDI+ bitmap filled with a single color.
    ''' </summary>
    '''
    ''' <param name="width">
    ''' The bitmap width, in pixels.
    ''' </param>
    '''
    ''' <param name="height">
    ''' The bitmap height, in pixels.
    ''' </param>
    '''
    ''' <param name="fillColor">
    ''' The fill color (including alpha).
    ''' </param>
    '''
    ''' <param name="pixelFormat">
    ''' The pixel format of the bitmap. Must be a non-indexed format.
    ''' </param>
    '''
    ''' <returns>
    ''' The new bitmap. The caller is responsible for disposing it.
    ''' </returns>
    Friend Function CreateSolidBitmap(width As Integer, height As Integer, fillColor As Color,
                                      Optional pixelFormat As PixelFormat = PixelFormat.Format32bppArgb) As Bitmap

        Dim bmp As New Bitmap(width, height, pixelFormat)
        For y As Integer = 0 To height - 1
            For x As Integer = 0 To width - 1
                bmp.SetPixel(x, y, fillColor)
            Next
        Next
        Return bmp
    End Function

    ''' <summary>
    ''' Creates a GDI+ bitmap whose left half is filled with one color and whose right half is filled with another color.
    ''' </summary>
    '''
    ''' <param name="width">
    ''' The bitmap width, in pixels. Must be an even number.
    ''' </param>
    '''
    ''' <param name="height">
    ''' The bitmap height, in pixels.
    ''' </param>
    '''
    ''' <param name="leftColor">
    ''' The color of the left half.
    ''' </param>
    '''
    ''' <param name="rightColor">
    ''' The color of the right half.
    ''' </param>
    '''
    ''' <param name="pixelFormat">
    ''' The pixel format of the bitmap. Must be a non-indexed format.
    ''' </param>
    '''
    ''' <returns>
    ''' The new bitmap. The caller is responsible for disposing it.
    ''' </returns>
    Friend Function CreateSplitBitmap(width As Integer, height As Integer, leftColor As Color, rightColor As Color,
                                      Optional pixelFormat As PixelFormat = PixelFormat.Format32bppArgb) As Bitmap

        If width Mod 2 <> 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(width), width, "The width must be an even number.")
        End If

        Dim bmp As New Bitmap(width, height, pixelFormat)
        Dim halfWidth As Integer = width \ 2
        For y As Integer = 0 To height - 1
            For x As Integer = 0 To width - 1
                bmp.SetPixel(x, y, If(x < halfWidth, leftColor, rightColor))
            Next
        Next
        Return bmp
    End Function

#End Region

#Region " Image Files "

    ''' <summary>
    ''' Encodes a solid color image with GDI+ and returns the encoded bytes.
    ''' </summary>
    '''
    ''' <param name="fillColor">
    ''' The fill color (including alpha).
    ''' </param>
    '''
    ''' <param name="format">
    ''' The GDI+ image format (e.g., <see cref="ImageFormat.Png"/>).
    ''' </param>
    '''
    ''' <param name="width">
    ''' The image width, in pixels.
    ''' </param>
    '''
    ''' <param name="height">
    ''' The image height, in pixels.
    ''' </param>
    '''
    ''' <returns>
    ''' The encoded image bytes.
    ''' </returns>
    Friend Function EncodeSolidWithGdi(fillColor As Color, format As ImageFormat,
                                       Optional width As Integer = 16, Optional height As Integer = 16) As Byte()

        ArgumentNullException.ThrowIfNull(format)

        Using bmp As Bitmap = TestImageFactory.CreateSolidBitmap(width, height, fillColor)
            Using ms As New MemoryStream()
                bmp.Save(ms, format)
                Return ms.ToArray()
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Encodes a solid color image with SkiaSharp and returns the encoded bytes.
    ''' </summary>
    '''
    ''' <param name="fillColor">
    ''' The fill color (including alpha).
    ''' </param>
    '''
    ''' <param name="format">
    ''' The SkiaSharp image format. Supported values: <see cref="SKEncodedImageFormat.Png"/> and
    ''' <see cref="SKEncodedImageFormat.Webp"/> (encoded as lossless WebP).
    ''' </param>
    '''
    ''' <param name="width">
    ''' The image width, in pixels.
    ''' </param>
    '''
    ''' <param name="height">
    ''' The image height, in pixels.
    ''' </param>
    '''
    ''' <returns>
    ''' The encoded image bytes.
    ''' </returns>
    Friend Function EncodeSolidWithSkia(fillColor As Color, format As SKEncodedImageFormat,
                                        Optional width As Integer = 16, Optional height As Integer = 16) As Byte()

        Dim info As New SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul)

        Using skBitmap As New SKBitmap(info)
            Dim skColor As New SKColor(fillColor.R, fillColor.G, fillColor.B, fillColor.A)
            For y As Integer = 0 To height - 1
                For x As Integer = 0 To width - 1
                    skBitmap.SetPixel(x, y, skColor)
                Next
            Next

            Using pixmap As SKPixmap = skBitmap.PeekPixels()
                Dim data As SKData
                Select Case format
                    Case SKEncodedImageFormat.Png
                        data = pixmap.Encode(SKPngEncoderOptions.Default)
                    Case SKEncodedImageFormat.Webp
                        data = pixmap.Encode(New SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100.0F))
                    Case Else
                        Throw New NotSupportedException($"Unsupported test image format: {format}")
                End Select

                If data Is Nothing Then
                    Throw New InvalidOperationException($"SkiaSharp failed to encode the test image as {format}.")
                End If

                Using data
                    Return data.ToArray()
                End Using
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Wraps a PNG image into a single-entry Windows icon (.ico) file container.
    ''' </summary>
    '''
    ''' <param name="pngBytes">
    ''' The PNG encoded image bytes. The image dimensions must be between 1 and 256 pixels.
    ''' </param>
    '''
    ''' <param name="width">
    ''' The PNG image width, in pixels.
    ''' </param>
    '''
    ''' <param name="height">
    ''' The PNG image height, in pixels.
    ''' </param>
    '''
    ''' <returns>
    ''' The icon file bytes.
    ''' </returns>
    Friend Function WrapPngInIco(pngBytes As Byte(), width As Integer, height As Integer) As Byte()

        ArgumentNullException.ThrowIfNull(pngBytes)
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1)
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, 256)
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1)
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, 256)

        Const HeaderSize As Integer = 6
        Const DirectoryEntrySize As Integer = 16

        Using ms As New MemoryStream()
            Using writer As New BinaryWriter(ms)
                ' ICONDIR
                writer.Write(CUShort(0))                             ' Reserved
                writer.Write(CUShort(1))                             ' Type: 1 = Icon
                writer.Write(CUShort(1))                             ' Image count

                ' ICONDIRENTRY (0 means 256 pixels)
                writer.Write(If(width = 256, CByte(0), CByte(width)))
                writer.Write(If(height = 256, CByte(0), CByte(height)))
                writer.Write(CByte(0))                               ' Color count
                writer.Write(CByte(0))                               ' Reserved
                writer.Write(CUShort(1))                             ' Color planes
                writer.Write(CUShort(32))                            ' Bits per pixel
                writer.Write(CUInt(pngBytes.Length))                 ' Image data size
                writer.Write(CUInt(HeaderSize + DirectoryEntrySize)) ' Image data offset

                writer.Write(pngBytes)
            End Using
            Return ms.ToArray()
        End Using
    End Function

    ''' <summary>
    ''' Creates a solid color icon (.ico) file content, with an embedded PNG image.
    ''' </summary>
    '''
    ''' <param name="fillColor">
    ''' The fill color (including alpha).
    ''' </param>
    '''
    ''' <param name="size">
    ''' The icon width and height, in pixels.
    ''' </param>
    '''
    ''' <returns>
    ''' The icon file bytes.
    ''' </returns>
    Friend Function EncodeSolidIco(fillColor As Color, Optional size As Integer = 32) As Byte()

        Dim pngBytes As Byte() = TestImageFactory.EncodeSolidWithSkia(fillColor, SKEncodedImageFormat.Png, size, size)
        Return TestImageFactory.WrapPngInIco(pngBytes, size, size)
    End Function

    ''' <summary>
    ''' Creates deterministic pseudo-random garbage bytes that do not represent any valid image format.
    ''' </summary>
    '''
    ''' <param name="length">
    ''' The number of bytes to create.
    ''' </param>
    '''
    ''' <returns>
    ''' The garbage bytes.
    ''' </returns>
    Friend Function CreateGarbageBytes(Optional length As Integer = 1024) As Byte()

        Dim bytes As Byte() = New Byte(length - 1) {}
        Dim rng As New Random(Seed:=12345)
        rng.NextBytes(bytes)

        ' Ensure the content does not start with any known image signature.
        bytes(0) = CByte(AscW("X"c))
        bytes(1) = CByte(AscW("Y"c))
        bytes(2) = CByte(AscW("Z"c))
        Return bytes
    End Function

#End Region

End Module

#End Region
