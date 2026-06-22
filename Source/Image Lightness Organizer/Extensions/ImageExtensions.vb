#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Runtime.CompilerServices

#End Region

Public Module ImageExtensions

    ''' <summary>
    ''' Computes the average CIE L* lightness of the specified <see cref="Image"/>.
    ''' </summary>
    ''' 
    ''' <param name="bitmap">
    ''' Source bitmap in <see cref="PixelFormat.Format32bppArgb"/> format.
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
    <Extension>
    Public Function ComputeAverageLightness(sourceImage As Image, Optional alphaThreshold As Byte = 8) As Double

#If NETCOREAPP Then
        ArgumentNullException.ThrowIfNull(sourceImage)
#Else
        If sourceImage Is Nothing Then
            Throw New ArgumentNullException(NameOf(sourceImage))
        End If
#End If

        Dim bmp As Bitmap = TryCast(sourceImage, Bitmap)
        Dim mustDispose As Boolean = False

        If bmp Is Nothing OrElse bmp.PixelFormat <> PixelFormat.Format32bppArgb Then
            bmp = New Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppArgb)
            Using gfx As Graphics = Graphics.FromImage(bmp)
                gfx.CompositingMode = CompositingMode.SourceCopy
                gfx.InterpolationMode = InterpolationMode.NearestNeighbor
                gfx.PixelOffsetMode = PixelOffsetMode.HighSpeed
                gfx.CompositingQuality = CompositingQuality.HighSpeed

                gfx.DrawImage(sourceImage, 0, 0, sourceImage.Width, sourceImage.Height)
            End Using
            mustDispose = True
        End If

        Try
            Return BitmapExtensions.ComputeAverageLightness(bmp, alphaThreshold)
        Finally
            If mustDispose Then
                bmp.Dispose()
            End If
        End Try

    End Function

End Module
