#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

Public NotInheritable Class UtilColor

    ''' <summary>
    ''' Applies the CIE XYZn to L*a*b* (CIELAB) transfer function (also known as the f-function)
    ''' used in the conversion from relative luminance (<c>Y</c>) to perceptual lightness (<c>L*</c>).
    ''' </summary>
    ''' 
    ''' <example> This example shows how to calculate the Perceptual Lightness (<c>L*</c>) of a 18% "Middle Gray" value:
    ''' <code language="VB">
    ''' Dim relativeY As Double = 0.18R ' 18% reflectance
    ''' Dim fValue As Double = ComputeLabF(relativeY)
    ''' 
    ''' ' Formula for L* (Lightness): 116 * f(Y) - 16
    ''' Dim lightness As Double = (116.0R * fValue) - 16.0R
    ''' 
    ''' Console.WriteLine($"Relative Luminance (Y): {relativeY}")
    ''' Console.WriteLine($"Perceptual Lightness (L*): {lightness:F2}")
    ''' </code>
    ''' </example>
    ''' 
    ''' <param name="value">
    ''' The relative luminance (<c>Y</c>) or color component, expected in the normalized range <c>[0.0, 1.0]</c>.
    ''' </param>
    ''' 
    ''' <returns>
    ''' The transformed value <c>f(Y)</c> to be used in <c>L*</c>, <c>a*</c>, or <c>b*</c> calculations.
    ''' </returns>
    ''' 
    ''' <remarks>
    ''' This function is part of the CIE L*a*b* (CIELAB) color space definition. 
    ''' It models how humans perceive brightness differences.
    ''' <para></para>
    ''' 
    ''' The constants used by this function are defined by the CIE (International Commission on Illumination):
    ''' <para></para>
    ''' - <b>Epsilon (ε)</b>: The transition point between linear and non-linear behavior, calculated as <c>(6/29)^3 ≈ 0.008856</c>.
    ''' <para></para>
    ''' - <b>Kappa (κ)</b>: The slope of the linear segment near black, calculated as <c>(29/3)^3 ≈ 903.3</c>.
    ''' <para></para>
    ''' 
    ''' For more technical details, see:
    ''' <para></para>
    ''' <see href="https://en.wikipedia.org/wiki/CIELAB_color_space#Forward_transformation"/>
    ''' <para></para>
    ''' <see href="https://en.wikipedia.org/wiki/Relative_luminance"/>
    ''' </remarks>
    <DebuggerStepThrough>
    Public Shared Function ComputeLabF(value As Double) As Double

        Const Epsilon As Double = 0.008856R
        Const Kappa As Double = 903.3R

        ' CIE L* (L-Star / Lightness) formula constants
        Const LStarScale As Double = 116.0R
        Const LStarOffset As Double = 16.0R

        ' If the value is above Epsilon, we use the power function (cube root).
        ' If the value is below Epsilon, we use a linear slope (Kappa) to avoid
        ' infinite gradients at the zero point, and handle image noise better.
        Return If(value > Epsilon,
                  Math.Pow(value, 1.0R / 3.0R),
                  (Kappa * value + LStarOffset) / LStarScale)
    End Function

    ''' <summary>
    ''' Precomputes a look-up table (LUT) containing the linearized sRGB values [0.0, 1.0] for each possible 8-bit channel value [0, 255].
    ''' </summary>
    ''' 
    ''' <example>
    ''' This example demonstrates how to use the look-up table (LUT) to linearize a standard 8-bit RGB channel value:
    ''' <code language="VB">
    ''' Dim lut As IReadOnlyList(Of Double) = BuildLinearSRGBLookupTable()
    ''' 
    ''' ' Get the linear intensity of a middle-gray byte (127)
    ''' Dim grayByte As Integer = 127
    ''' Dim linearLight As Double = lut(grayByte)
    ''' 
    ''' ' Output the result (approx. 0.21 or 21% light intensity)
    ''' Console.WriteLine($"Byte Value: {grayByte}")
    ''' Console.WriteLine($"Linear Light Intensity: {linearLight:P2}")
    ''' </code>
    ''' </example>
    ''' 
    ''' <returns>
    ''' A readon-only list of 256 <see cref="Double"/> values representing normalized linear light intensities in the range [0.0, 1.0].
    ''' </returns>
    ''' 
    ''' <remarks>
    ''' Standard digital images (sRGB) are gamma-corrected to optimize bit depth for human perception. 
    ''' Before performing any physical light calculations (like luminosity), we must "undo" this correction. 
    ''' This process is known as <b>Gamma Expansion</b>.
    ''' <para></para>
    ''' For more technical details, see: <see href="https://en.wikipedia.org/wiki/SRGB#The_forward_transformation_(gamma_compression)"/>
    ''' </remarks>
    <DebuggerStepThrough>
    Public Shared Function BuildLinearSRGBLookupTable() As IReadOnlyList(Of Double)

        ' --- sRGB Standard Constants ---

        ' The exponent used for the power-law (gamma) section. 
        ' While often simplified to 2.2, the official sRGB standard uses 2.4.
        Const GammaExponent As Double = 2.4R

        ' The threshold that separates the linear slope from the curve.
        Const GammaThreshold As Double = 0.04045R

        ' The divisor used for the linear segment near black.
        Const LinearSlope As Double = 12.92R

        ' The offset (magic number) used to align the linear and curve segments.
        Const Offset As Double = 0.055R

        ' --- Implementation ---

        Dim lut As Double() = New Double(255) {}

        For i As Integer = 0 To 255
            ' Normalize the 8-bit integer [0, 255] to a double [0.0, 1.0]
            Dim v As Double = i / 255.0R

            ' The "Piecewise" function:
            ' If the value is very dark (below threshold), use a simple linear division;
            ' Otherwise, use the exponential formula (Gamma Expansion).
            lut(i) = If(v <= GammaThreshold,
                        v / LinearSlope,
                        Math.Pow((v + Offset) / (1.0R + Offset), GammaExponent))
        Next

        Return Array.AsReadOnly(lut)
    End Function

End Class
