#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " ReferenceColorMath "

''' <summary>
''' Independent reference implementation of the sRGB to CIE L* conversion,
''' written directly from the specifications (without look-up tables) to validate the production code.
''' </summary>
'''
''' <remarks>
''' This implementation intentionally uses the exact rational CIE constants
''' (ε = 216/24389, κ = 24389/27) instead of the rounded constants used by the production code
''' (0.008856 and 903.3), so both implementations are expected to agree within a small tolerance
''' (see <see cref="ReferenceColorMath.LightnessTolerance"/>).
''' </remarks>
Friend Module ReferenceColorMath

#Region " Constants "

    ''' <summary>
    ''' The maximum allowed difference between the reference CIE L* values and the production CIE L* values.
    ''' </summary>
    Friend Const LightnessTolerance As Double = 0.001R

    ''' <summary>
    ''' The exact CIE epsilon (ε) constant: (6/29)^3 = 216/24389.
    ''' </summary>
    Private Const CieEpsilon As Double = 216.0R / 24389.0R

    ''' <summary>
    ''' The exact CIE kappa (κ) constant: (29/3)^3 = 24389/27.
    ''' </summary>
    Private Const CieKappa As Double = 24389.0R / 27.0R

#End Region

#Region " Methods "

    ''' <summary>
    ''' Converts an 8-bit sRGB channel value to its linear-light value (gamma expansion), per IEC 61966-2-1.
    ''' </summary>
    '''
    ''' <param name="channel">
    ''' The 8-bit sRGB channel value.
    ''' </param>
    '''
    ''' <returns>
    ''' The linear-light value in the range [0.0, 1.0].
    ''' </returns>
    Friend Function SrgbToLinear(channel As Byte) As Double

        Dim normalized As Double = channel / 255.0R
        Return If(normalized <= 0.04045R,
                  normalized / 12.92R,
                  Math.Pow((normalized + 0.055R) / 1.055R, 2.4R))
    End Function

    ''' <summary>
    ''' Computes the ITU-R BT.709 relative luminance (Y) of an sRGB color.
    ''' </summary>
    '''
    ''' <param name="r">
    ''' The red channel value.
    ''' </param>
    '''
    ''' <param name="g">
    ''' The green channel value.
    ''' </param>
    '''
    ''' <param name="b">
    ''' The blue channel value.
    ''' </param>
    '''
    ''' <returns>
    ''' The relative luminance in the range [0.0, 1.0].
    ''' </returns>
    Friend Function RelativeLuminance(r As Byte, g As Byte, b As Byte) As Double

        Return (0.2126R * ReferenceColorMath.SrgbToLinear(r)) +
               (0.7152R * ReferenceColorMath.SrgbToLinear(g)) +
               (0.0722R * ReferenceColorMath.SrgbToLinear(b))
    End Function

    ''' <summary>
    ''' Converts a relative luminance (Y) value to CIE L* lightness.
    ''' </summary>
    '''
    ''' <param name="relativeY">
    ''' The relative luminance value in the range [0.0, 1.0].
    ''' </param>
    '''
    ''' <returns>
    ''' The CIE L* lightness value in the range [0.0, 100.0].
    ''' </returns>
    Friend Function LuminanceToLightness(relativeY As Double) As Double

        Return If(relativeY > ReferenceColorMath.CieEpsilon,
                  (116.0R * Math.Cbrt(relativeY)) - 16.0R,
                  ReferenceColorMath.CieKappa * relativeY)
    End Function

    ''' <summary>
    ''' Computes the CIE L* lightness of an sRGB color.
    ''' </summary>
    '''
    ''' <param name="r">
    ''' The red channel value.
    ''' </param>
    '''
    ''' <param name="g">
    ''' The green channel value.
    ''' </param>
    '''
    ''' <param name="b">
    ''' The blue channel value.
    ''' </param>
    '''
    ''' <returns>
    ''' The CIE L* lightness value in the range [0.0, 100.0].
    ''' </returns>
    Friend Function Lightness(r As Byte, g As Byte, b As Byte) As Double

        Return ReferenceColorMath.LuminanceToLightness(ReferenceColorMath.RelativeLuminance(r, g, b))
    End Function

#End Region

End Module

#End Region
