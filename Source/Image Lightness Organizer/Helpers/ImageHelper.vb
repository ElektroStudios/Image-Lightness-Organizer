
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

#Region " Static Methods "

    ''' <summary>
    ''' Computes the average CIE L* lightness of the specified image file.
    ''' </summary>
    ''' 
    ''' <example> This is a code example.
    ''' <code language="VB">
    ''' Dim imageFile As String = "C:\Wallpaper.jpg"
    ''' Dim avgLightness As Double = UtilImage.ComputeAverageLightness(imageFile)
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
    ''' Range: 0–255. A value of 0 includes all pixels; 255 includes only fully opaque pixels.
    ''' <para></para>
    ''' Default value is 8.
    ''' </param>
    ''' 
    ''' <returns>
    ''' Average CIE L* value in the range [0.0, 100.0], or 0.0 if all pixels are below the alpha threshold.
    ''' </returns>
    <DebuggerStepThrough>
    Friend Function ComputeAverageLightness(filePath As String, Optional alphaThreshold As Byte = 8) As Double

        Dim fileExtension As String = Path.GetExtension(filePath).ToLowerInvariant()

        If fileExtension = ".avif" OrElse
           fileExtension = ".ico" OrElse
           fileExtension = ".webp" Then

            Dim codecResult As SKCodecResult = SKCodecResult.Success

            Using stream As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)

                Using codec As SKCodec = SKCodec.Create(stream, codecResult)

                    If (codecResult <> SKCodecResult.Success) OrElse (codec Is Nothing) Then
                        Throw New InvalidOperationException($"SkiaSharp failed to create SKCodec. Codec result: {codecResult}")
                    End If
                End Using
            End Using

            Using skiaBitmap As SKBitmap = SKBitmap.Decode(filePath)

                Using gdiBitmap As New Bitmap(skiaBitmap.Width, skiaBitmap.Height, PixelFormat.Format32bppArgb)

                    Dim rect As New Rectangle(0, 0, gdiBitmap.Width, gdiBitmap.Height)
                    Dim bmpData As BitmapData = gdiBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb)

                    Try
                        ' Get the native memory pointer from Skia's decoded pixels
                        Dim sourcePointer As IntPtr = skiaBitmap.GetPixels()
                        Dim destinationPointer As IntPtr = bmpData.Scan0

                        ' Calculate total bytes to copy (Width * Height * 4 bytes per pixel for 32bpp)
                        Dim totalBytes As Integer = skiaBitmap.Width * skiaBitmap.Height * 4

                        ' Allocate a temporary managed buffer to bridge the native pointers safely under Option Strict On
                        Dim pixelBuffer As Byte() = New Byte(totalBytes - 1) {}

                        ' Copy from Skia native memory to managed array
                        Marshal.Copy(sourcePointer, pixelBuffer, 0, totalBytes)

                        ' Copy from managed array to GDI+ locked native memory
                        Marshal.Copy(pixelBuffer, 0, destinationPointer, totalBytes)

                    Finally
                        ' Always unlock the bits
                        gdiBitmap.UnlockBits(bmpData)
                    End Try

                    ' Pass the correctly populated Bitmap to your existing CIE L* calculator
                    Return ImageExtensions.ComputeAverageLightness(gdiBitmap, alphaThreshold)
                End Using
            End Using

        Else
            ' Fallback for GDI+ supported formats.
            Using sourceStream As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)

                Using sourceImage As Image = Image.FromStream(sourceStream, useEmbeddedColorManagement:=False, validateImageData:=True)
                    Return ImageExtensions.ComputeAverageLightness(sourceImage, alphaThreshold)
                End Using
            End Using

        End If

    End Function

#End Region

End Module

