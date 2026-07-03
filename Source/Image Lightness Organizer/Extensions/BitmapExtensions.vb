#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Buffers
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

#End Region

Public Module BitmapExtensions

    ''' <summary>
    ''' Precomputed look-up table that maps each sRGB encoded byte value [0, 255] 
    ''' to its corresponding linear-light (physical) value in the range [0.0, 1.0].
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Used by function: <see cref="BitmapExtensions.ComputeAverageLightness(Bitmap, Byte)"/> 
    ''' to convert sRGB pixel values to linear light for accurate luminance calculations.
    ''' </remarks>
    Private LinearSrgbLut As IReadOnlyList(Of Double)

    ''' <summary>
    ''' Computes the average CIE L* lightness of the specified <see cref="Bitmap"/>.
    ''' <para></para>
    ''' The bitmap must use <see cref="PixelFormat.Format32bppArgb"/> layout;
    ''' any other format will raise a <see cref="NotSupportedException"/>.
    ''' <para></para>
    ''' For arbitrary bitmaps, prefer <see cref="ImageExtensions.ComputeAverageLightness"/> function, 
    ''' which handles proper <see cref="Bitmap"/> format conversion automatically.
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
    Public Function ComputeAverageLightness(sourceBitmap As Bitmap, Optional alphaThreshold As Byte = 8) As Double

#If NETCOREAPP Then
        ArgumentNullException.ThrowIfNull(sourceBitmap)
#Else
        If bitmap Is Nothing Then
            Throw New ArgumentNullException(NameOf(bitmap))
        End If
#End If

        If sourceBitmap.PixelFormat <> PixelFormat.Format32bppArgb Then
            Throw New NotSupportedException(
                $"Expected PixelFormat {NameOf(PixelFormat.Format32bppArgb)}, got {sourceBitmap.PixelFormat}.")
        End If

        ' ITU-R BT.709 standard coefficients for relative luminance (Y)
        ' These weights reflect human visual sensitivity to RGB channels.
        Const LumaR As Double = 0.2126R
        Const LumaG As Double = 0.7152R
        Const LumaB As Double = 0.0722R

        ' CIE L* (L-Star / Lightness) formula constants
        ' Used to transform relative luminance (Y) into perceptual lightness (L*).
        Const LStarScale As Double = 116.0R
        Const LStarOffset As Double = 16.0R

        If BitmapExtensions.LinearSrgbLut Is Nothing Then
            BitmapExtensions.LinearSrgbLut = ColorHelper.BuildLinearSRGBLookupTable()
        End If

        Dim rect As New Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height)
        Dim bmpData As BitmapData = sourceBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
        Dim pixels As Byte() = Nothing

        Try
            Dim stride As Integer = bmpData.Stride
            If stride <= 0 Then
                Throw New NotSupportedException("Negative or zero stride is not supported.")
            End If

            Dim bufferSize As Integer = stride * sourceBitmap.Height
            pixels = ArrayPool(Of Byte).Shared.Rent(bufferSize)
            Marshal.Copy(bmpData.Scan0, pixels, 0, bufferSize)

            Dim totalL As Double = 0.0R
            Dim pixelCount As Long = 0L

            Dim y As Integer
            For y = 0 To sourceBitmap.Height - 1
                Dim rowOffset As Integer = y * stride

                Dim x As Integer
                For x = 0 To sourceBitmap.Width - 1
                    Dim idx As Integer = rowOffset + (x * 4)

                    ' Format32bppArgb byte layout: B=idx+0, G=idx+1, R=idx+2, A=idx+3
                    Dim alpha As Byte = pixels(idx + 3)
                    If alpha <= alphaThreshold Then
                        Continue For
                    End If

                    Dim r As Byte = pixels(idx + 2)
                    Dim g As Byte = pixels(idx + 1)
                    Dim b As Byte = pixels(idx)

                    ' Step 1: Calculate CIE Relative Luminance (Y) using the LUT
                    ' This represents the physical light intensity.
                    Dim relY As Double =
                        (BitmapExtensions.LinearSrgbLut(r) * LumaR) +
                        (BitmapExtensions.LinearSrgbLut(g) * LumaG) +
                        (BitmapExtensions.LinearSrgbLut(b) * LumaB)

                    ' Step 2: Calculate Perceptual Lightness (L*) 
                    ' This transforms physical light into human perceived brightness [0-100].
                    ' Note: ComputeLabF FUNCTION applies the cube root or linear slope per CIE specs.
                    totalL += (LStarScale * ColorHelper.ComputeLabF(relY)) - LStarOffset
                    pixelCount += 1L
                Next
            Next

            Return If(pixelCount = 0L, 0.0R, totalL / pixelCount)

        Finally
            sourceBitmap.UnlockBits(bmpData)
            If pixels IsNot Nothing Then
                ArrayPool(Of Byte).Shared.Return(pixels, clearArray:=False)
            End If
        End Try
    End Function

End Module

