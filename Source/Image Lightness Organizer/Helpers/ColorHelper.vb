
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

Public Module ColorHelper

#Region " Constants "

    ''' <summary>
    ''' The minimum allowed percentage step size for <see cref="ColorHelper.LightnessToFolderName"/> function.
    ''' </summary>
    Friend Const MinStepSize As Integer = 1

    ''' <summary>
    ''' The maximum allowed percentage step size for <see cref="ColorHelper.LightnessToFolderName"/> function.
    ''' </summary>
    Friend Const MaxStepSize As Integer = 10

#End Region

#Region " Static Methods "

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
    Friend Function ComputeLabF(value As Double) As Double

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
    ''' A read-only list of 256 <see cref="Double"/> values representing normalized linear light intensities in the range [0.0, 1.0].
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
    Friend Function BuildLinearSRGBLookupTable() As IReadOnlyList(Of Double)

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

    ''' <summary>
    ''' Maps an average CIE L* value (0–100) to a folder name representing a lightness range determined by the specified step value.
    ''' </summary>
    ''' 
    ''' <param name="averageL">
    ''' The average CIE L* lightness value (0 to 100).
    ''' </param>
    ''' 
    ''' <param name="stepSize">
    ''' The percentage interval step size used to group the ranges (from 1 to 10).
    ''' <para></para>
    ''' For example, a value of 5 creates folder names in 5% increments, while 10 creates them in 10% increments.
    ''' </param>
    ''' 
    ''' <returns>
    ''' A folder name indicating the lightness range:
    ''' <para></para>
    ''' - "Light 00%-[stepSize]%" for very dark images (L* = 0 to stepSize)
    ''' <para></para>
    ''' - Intermediate interval values scaled by the step size (e.g., "Light 05%-10%", "Light 10%-15%" for a step size of 5)
    ''' <para></para>
    ''' - "Light [lastGroupBase]%-100%" for the brightest images, where the upper bound is always capped to 100%
    ''' (e.g., "Light 90%-100%" for a step size of 10, or "Light 98%-100%" for a step size of 7).
    ''' </returns>
    '''
    ''' <remarks>
    ''' The lightness value is clamped to the range [0, 100] and rounded to the nearest integer percentage.
    ''' <para></para>
    ''' Each range includes its lower bound and excludes its upper bound,
    ''' except the last range, which also includes 100%.
    ''' <para></para>
    ''' The returned ranges never overlap, even when <paramref name="stepSize"/> is not an exact divisor of 100.
    ''' </remarks>
    '''
    ''' <exception cref="ArgumentOutOfRangeException">
    ''' Thrown when <paramref name="averageL"/> is not a finite number,
    ''' or when <paramref name="stepSize"/> is not in the range 1 to 10.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function LightnessToFolderName(averageL As Double, stepSize As Integer) As String

        If Not Double.IsFinite(averageL) Then
            Throw New ArgumentOutOfRangeException(NameOf(averageL), averageL, "The lightness value must be a finite number.")
        End If

        If stepSize < ColorHelper.MinStepSize OrElse stepSize > ColorHelper.MaxStepSize Then
            Throw New ArgumentOutOfRangeException(NameOf(stepSize), stepSize,
                $"The step size must be an integer between {ColorHelper.MinStepSize} and {ColorHelper.MaxStepSize}.")
        End If

        ' Clamp before rounding to avoid overflow on out-of-range values.
        Dim clampedL As Double = Math.Max(0.0R, Math.Min(100.0R, averageL))
        Dim pct As Integer = Convert.ToInt32(Math.Round(clampedL))

        ' 100% belongs to the last range (the one that contains 99%).
        Dim groupBase As Integer = (Math.Min(pct, 99) \ stepSize) * stepSize
        Dim lo As Integer = groupBase
        Dim hi As Integer = Math.Min(groupBase + stepSize, 100)

        Return $"Light {lo:D2}%-{hi:D2}%"
    End Function

#End Region

End Module
