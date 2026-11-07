
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.InteropServices

Imports SkiaSharp

#End Region

Friend Module ImageHelper

#Region " Fields "

    ''' <summary>
    ''' The image file extensions that GDI+ cannot decode, and therefore are decoded with SkiaSharp.
    ''' </summary>
    Private ReadOnly SkiaDecodedFileExtensions As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
        ".avif",
        ".ico",
        ".webp"
    }

#End Region

#Region " Static Methods "

    ''' <summary>
    ''' Computes the average CIE L* lightness of the specified image file.
    ''' </summary>
    '''
    ''' <example> This is a code example.
    ''' <code language="VB">
    ''' Dim imageFile As String = "C:\Wallpaper.jpg"
    ''' Dim avgLightness As Double = ImageHelper.ComputeAverageLightness(imageFile)
    '''
    ''' Console.WriteLine($"Image File: {Path.GetFileName(imageFile)}")
    ''' Console.WriteLine($"Average Lightness (CIE L*): {(avgLightness / 100.0R):P2}")
    ''' </code>
    ''' </example>
    '''
    ''' <param name="filePath">
    ''' Full path to the source image file.
    ''' </param>
    '''
    ''' <param name="alphaThreshold">
    ''' Optional alpha threshold below which a pixel is considered fully transparent and excluded from the lightness average.
    ''' <para></para>
    ''' Pixels whose alpha value is strictly lower than this threshold are excluded.
    ''' <para></para>
    ''' Range: 0–255. A value of 0 includes all pixels; 255 includes only fully opaque pixels.
    ''' <para></para>
    ''' Default value is 8.
    ''' </param>
    '''
    ''' <returns>
    ''' Average CIE L* value in the range [0.0, 100.0], or 0.0 if all pixels are below the alpha threshold.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="filePath"/> is <see langword="Nothing"/>.
    ''' </exception>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="filePath"/> is empty or white space,
    ''' or when GDI+ cannot decode the image (corrupted file or unsupported format).
    ''' </exception>
    '''
    ''' <exception cref="FileNotFoundException">
    ''' Thrown when the specified file does not exist.
    ''' </exception>
    '''
    ''' <exception cref="InvalidOperationException">
    ''' Thrown when SkiaSharp cannot decode an <c>.avif</c>, <c>.ico</c> or <c>.webp</c> image.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function ComputeAverageLightness(filePath As String, Optional alphaThreshold As Byte = 8) As Double

        ArgumentException.ThrowIfNullOrWhiteSpace(filePath)

        Return If(ImageHelper.IsSkiaDecodedFormat(filePath),
                  ImageHelper.ComputeAverageLightnessWithSkia(filePath, alphaThreshold),
                  ImageHelper.ComputeAverageLightnessWithGdi(filePath, alphaThreshold))
    End Function

    ''' <summary>
    ''' Determines whether the specified image file must be decoded with SkiaSharp
    ''' instead of GDI+, based on its file extension.
    ''' </summary>
    '''
    ''' <param name="filePath">
    ''' The path to the image file.
    ''' </param>
    '''
    ''' <returns>
    ''' <see langword="True"/> for <c>.avif</c>, <c>.ico</c> and <c>.webp</c> files (case-insensitive);
    ''' otherwise, <see langword="False"/>.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="filePath"/> is <see langword="Nothing"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function IsSkiaDecodedFormat(filePath As String) As Boolean

        ArgumentNullException.ThrowIfNull(filePath)

        Dim fileExtension As String = Path.GetExtension(filePath)
        Return ImageHelper.SkiaDecodedFileExtensions.Contains(fileExtension)
    End Function

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Computes the average CIE L* lightness of an image file decoded with GDI+.
    ''' </summary>
    '''
    ''' <param name="filePath">
    ''' Full path to the source image file.
    ''' </param>
    '''
    ''' <param name="alphaThreshold">
    ''' Alpha threshold below which a pixel is excluded from the lightness average.
    ''' </param>
    '''
    ''' <returns>
    ''' Average CIE L* value in the range [0.0, 100.0].
    ''' </returns>
    <DebuggerStepThrough>
    Private Function ComputeAverageLightnessWithGdi(filePath As String, alphaThreshold As Byte) As Double

        Using sourceStream As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
            Using sourceImage As Image = Image.FromStream(sourceStream, useEmbeddedColorManagement:=False, validateImageData:=True)
                Return ImageExtensions.ComputeAverageLightness(sourceImage, alphaThreshold)
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Computes the average CIE L* lightness of an image file decoded with SkiaSharp.
    ''' </summary>
    '''
    ''' <param name="filePath">
    ''' Full path to the source image file.
    ''' </param>
    '''
    ''' <param name="alphaThreshold">
    ''' Alpha threshold below which a pixel is excluded from the lightness average.
    ''' </param>
    '''
    ''' <returns>
    ''' Average CIE L* value in the range [0.0, 100.0].
    ''' </returns>
    '''
    ''' <remarks>
    ''' The image is decoded only once, explicitly as <see cref="SKColorType.Bgra8888"/> with
    ''' <see cref="SKAlphaType.Unpremul"/> alpha, which matches the memory layout of
    ''' GDI+ <see cref="PixelFormat.Format32bppArgb"/> regardless of the platform's native color type.
    ''' <para></para>
    ''' Pixels are copied row by row, honoring the row stride of both bitmaps.
    ''' </remarks>
    '''
    ''' <exception cref="InvalidOperationException">
    ''' Thrown when SkiaSharp cannot create a codec for the file, or fails to decode its pixels.
    ''' </exception>
    <DebuggerStepThrough>
    Private Function ComputeAverageLightnessWithSkia(filePath As String, alphaThreshold As Byte) As Double

        Using stream As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)

            Dim codecResult As SKCodecResult = SKCodecResult.Success
            Using codec As SKCodec = SKCodec.Create(stream, codecResult)

                If (codecResult <> SKCodecResult.Success) OrElse (codec Is Nothing) Then
                    Throw New InvalidOperationException($"SkiaSharp failed to create SKCodec. Codec result: {codecResult}")
                End If

                Dim decodeInfo As New SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul)

                Using skiaBitmap As New SKBitmap(decodeInfo)

                    Dim decodeResult As SKCodecResult = codec.GetPixels(decodeInfo, skiaBitmap.GetPixels())
                    If decodeResult <> SKCodecResult.Success Then
                        Throw New InvalidOperationException($"SkiaSharp failed to decode the image pixels. Codec result: {decodeResult}")
                    End If

                    Using gdiBitmap As New Bitmap(decodeInfo.Width, decodeInfo.Height, PixelFormat.Format32bppArgb)

                        Dim rect As New Rectangle(0, 0, gdiBitmap.Width, gdiBitmap.Height)
                        Dim bmpData As BitmapData = gdiBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb)

                        Try
                            Dim sourcePointer As IntPtr = skiaBitmap.GetPixels()
                            Dim sourceStride As Integer = skiaBitmap.RowBytes
                            Dim destinationPointer As IntPtr = bmpData.Scan0
                            Dim destinationStride As Integer = bmpData.Stride
                            Dim rowLength As Integer = decodeInfo.Width * 4

                            ' Managed row buffer to bridge both native pointers safely under Option Strict On.
                            Dim rowBuffer As Byte() = New Byte(rowLength - 1) {}

                            For y As Integer = 0 To decodeInfo.Height - 1
                                Marshal.Copy(IntPtr.Add(sourcePointer, y * sourceStride), rowBuffer, 0, rowLength)
                                Marshal.Copy(rowBuffer, 0, IntPtr.Add(destinationPointer, y * destinationStride), rowLength)
                            Next

                        Finally
                            gdiBitmap.UnlockBits(bmpData)
                        End Try

                        Return BitmapExtensions.ComputeAverageLightness(gdiBitmap, alphaThreshold)
                    End Using
                End Using
            End Using
        End Using
    End Function

#End Region

End Module
