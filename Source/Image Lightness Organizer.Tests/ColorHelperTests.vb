#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Globalization
Imports System.Text.RegularExpressions

#End Region

#Region " ColorHelperTests "

''' <summary>
''' Unit tests for <see cref="ColorHelper"/> module.
''' </summary>
<TestClass>
Public NotInheritable Class ColorHelperTests

#Region " Constants "

    ''' <summary>
    ''' The tolerance used for exact floating-point comparisons.
    ''' </summary>
    Private Const ExactTolerance As Double = 0.000000000001R

    ''' <summary>
    ''' The regular expression that matches a valid lightness folder name, capturing its lower and upper bounds.
    ''' </summary>
    Private Shared ReadOnly FolderNameRegex As New Regex("^Light (?<lo>\d{2,3})%-(?<hi>\d{2,3})%$", RegexOptions.CultureInvariant)

#End Region

#Region " ComputeLabF "

    <TestMethod>
    Public Sub ComputeLabF_Zero_ReturnsLinearSegmentOrigin()
        Dim result As Double = ColorHelper.ComputeLabF(0.0R)

        Assert.AreEqual(16.0R / 116.0R, result, ColorHelperTests.ExactTolerance)
    End Sub

    <TestMethod>
    Public Sub ComputeLabF_One_ReturnsOne()
        Dim result As Double = ColorHelper.ComputeLabF(1.0R)

        Assert.AreEqual(1.0R, result, ColorHelperTests.ExactTolerance)
    End Sub

    <TestMethod>
    <DataRow(0.125R, 0.5R)>
    <DataRow(0.216R, 0.6R)>
    <DataRow(0.512R, 0.8R)>
    <DataRow(0.729R, 0.9R)>
    Public Sub ComputeLabF_AboveEpsilon_ReturnsCubeRoot(value As Double, expected As Double)
        Dim result As Double = ColorHelper.ComputeLabF(value)

        Assert.AreEqual(expected, result, ColorHelperTests.ExactTolerance)
    End Sub

    <TestMethod>
    <DataRow(0.0001R)>
    <DataRow(0.001R)>
    <DataRow(0.005R)>
    <DataRow(0.008856R)>
    Public Sub ComputeLabF_AtOrBelowEpsilon_UsesLinearSlope(value As Double)
        Dim expected As Double = ((903.3R * value) + 16.0R) / 116.0R

        Dim result As Double = ColorHelper.ComputeLabF(value)

        Assert.AreEqual(expected, result, ColorHelperTests.ExactTolerance)
    End Sub

    <TestMethod>
    Public Sub ComputeLabF_IsContinuousAtEpsilon()
        Const Epsilon As Double = 0.008856R
        Dim linearValue As Double = ColorHelper.ComputeLabF(Epsilon)
        Dim curveValue As Double = ColorHelper.ComputeLabF(Math.BitIncrement(Epsilon))

        Assert.AreEqual(linearValue, curveValue, 0.0001R,
                        "The linear and the cube-root segments must join at epsilon without a visible discontinuity.")
    End Sub

    <TestMethod>
    Public Sub ComputeLabF_IsStrictlyIncreasingOverUnitRange()
        Dim previous As Double = ColorHelper.ComputeLabF(0.0R)

        For i As Integer = 1 To 10000
            Dim value As Double = i / 10000.0R
            Dim current As Double = ColorHelper.ComputeLabF(value)

            Assert.IsGreaterThan(previous, current, $"f(Y) must be strictly increasing. Failed at Y={value}.")
            previous = current
        Next
    End Sub

    <TestMethod>
    Public Sub ComputeLabF_MiddleGray18Percent_ProducesLightnessOfAboutFifty()
        ' 18% reflectance "middle gray" corresponds to L* ≈ 49.50
        Dim lightness As Double = (116.0R * ColorHelper.ComputeLabF(0.18R)) - 16.0R

        Assert.AreEqual(49.496R, lightness, 0.001R)
    End Sub

    <TestMethod>
    Public Sub ComputeLabF_MatchesReferenceLightnessAcrossUnitRange()
        For i As Integer = 0 To 1000
            Dim relativeY As Double = i / 1000.0R
            Dim expected As Double = ReferenceColorMath.LuminanceToLightness(relativeY)

            Dim actual As Double = (116.0R * ColorHelper.ComputeLabF(relativeY)) - 16.0R

            Assert.AreEqual(expected, actual, ReferenceColorMath.LightnessTolerance, $"Mismatch at Y={relativeY}.")
        Next
    End Sub

#End Region

#Region " BuildLinearSRGBLookupTable "

    <TestMethod>
    Public Sub BuildLinearSRGBLookupTable_Contains256Entries()
        Dim lut As IReadOnlyList(Of Double) = ColorHelper.BuildLinearSRGBLookupTable()

        Assert.HasCount(256, lut)
    End Sub

    <TestMethod>
    Public Sub BuildLinearSRGBLookupTable_Endpoints_AreZeroAndOne()
        Dim lut As IReadOnlyList(Of Double) = ColorHelper.BuildLinearSRGBLookupTable()

        Assert.AreEqual(0.0R, lut(0), ColorHelperTests.ExactTolerance)
        Assert.AreEqual(1.0R, lut(255), ColorHelperTests.ExactTolerance)
    End Sub

    <TestMethod>
    Public Sub BuildLinearSRGBLookupTable_MatchesReferenceGammaExpansion()
        Dim lut As IReadOnlyList(Of Double) = ColorHelper.BuildLinearSRGBLookupTable()

        For i As Integer = 0 To 255
            Dim expected As Double = ReferenceColorMath.SrgbToLinear(CByte(i))

            Assert.AreEqual(expected, lut(i), ColorHelperTests.ExactTolerance, $"Mismatch at index {i}.")
        Next
    End Sub

    <TestMethod>
    <DataRow(10, 0.0030352698R)>   ' Linear segment (10/255 = 0.0392 <= 0.04045).
    <DataRow(11, 0.0033465358R)>   ' Power-law segment (11/255 = 0.0431 > 0.04045).
    <DataRow(128, 0.2158605001R)>
    <DataRow(188, 0.5028864580R)>
    Public Sub BuildLinearSRGBLookupTable_KnownValues(index As Integer, expected As Double)
        Dim lut As IReadOnlyList(Of Double) = ColorHelper.BuildLinearSRGBLookupTable()

        Assert.AreEqual(expected, lut(index), 0.0000000001R)
    End Sub

    <TestMethod>
    Public Sub BuildLinearSRGBLookupTable_IsStrictlyIncreasing()
        Dim lut As IReadOnlyList(Of Double) = ColorHelper.BuildLinearSRGBLookupTable()

        For i As Integer = 1 To 255
            Assert.IsGreaterThan(lut(i - 1), lut(i), $"The LUT must be strictly increasing. Failed at index {i}.")
        Next
    End Sub

    <TestMethod>
    Public Sub BuildLinearSRGBLookupTable_IsReadOnly()
        Dim lut As IReadOnlyList(Of Double) = ColorHelper.BuildLinearSRGBLookupTable()
        Dim asList As IList(Of Double) = TryCast(lut, IList(Of Double))

        Assert.IsNotNull(asList, "The LUT is expected to be exposed as a read-only collection wrapper.")
        Assert.IsTrue(asList.IsReadOnly)
        Assert.ThrowsExactly(Of NotSupportedException)(Sub() asList(0) = 123.0R)
    End Sub

    <TestMethod>
    Public Sub BuildLinearSRGBLookupTable_ReturnsIndependentInstances()
        Dim first As IReadOnlyList(Of Double) = ColorHelper.BuildLinearSRGBLookupTable()
        Dim second As IReadOnlyList(Of Double) = ColorHelper.BuildLinearSRGBLookupTable()

        Assert.AreNotSame(first, second)
        CollectionAssert.AreEqual(first.ToArray(), second.ToArray())
    End Sub

#End Region

#Region " LightnessToFolderName "

    <TestMethod>
    <DataRow(0.0R, 10, "Light 00%-10%")>
    <DataRow(0.4R, 10, "Light 00%-10%")>
    <DataRow(5.0R, 10, "Light 00%-10%")>
    <DataRow(9.4R, 10, "Light 00%-10%")>
    <DataRow(9.6R, 10, "Light 10%-20%")>
    <DataRow(10.0R, 10, "Light 10%-20%")>
    <DataRow(19.6R, 10, "Light 20%-30%")>
    <DataRow(55.0R, 10, "Light 50%-60%")>
    <DataRow(89.4R, 10, "Light 80%-90%")>
    <DataRow(90.0R, 10, "Light 90%-100%")>
    <DataRow(99.9R, 10, "Light 90%-100%")>
    <DataRow(100.0R, 10, "Light 90%-100%")>
    Public Sub LightnessToFolderName_DefaultStep_ReturnsExpectedRange(averageL As Double, stepSize As Integer, expected As String)
        Dim result As String = ColorHelper.LightnessToFolderName(averageL, stepSize)

        Assert.AreEqual(expected, result)
    End Sub

    <TestMethod>
    <DataRow(0.0R, 5, "Light 00%-05%")>
    <DataRow(4.0R, 5, "Light 00%-05%")>
    <DataRow(7.0R, 5, "Light 05%-10%")>
    <DataRow(53.58R, 5, "Light 50%-55%")>
    <DataRow(97.0R, 5, "Light 95%-100%")>
    <DataRow(100.0R, 5, "Light 95%-100%")>
    <DataRow(0.0R, 1, "Light 00%-01%")>
    <DataRow(42.0R, 1, "Light 42%-43%")>
    <DataRow(99.0R, 1, "Light 99%-100%")>
    <DataRow(100.0R, 1, "Light 99%-100%")>
    <DataRow(25.0R, 2, "Light 24%-26%")>
    <DataRow(99.0R, 2, "Light 98%-100%")>
    Public Sub LightnessToFolderName_CustomStep_ReturnsExpectedRange(averageL As Double, stepSize As Integer, expected As String)
        Dim result As String = ColorHelper.LightnessToFolderName(averageL, stepSize)

        Assert.AreEqual(expected, result)
    End Sub

    <TestMethod>
    <DataRow(95.0R, 3, "Light 93%-96%")>
    <DataRow(96.0R, 3, "Light 96%-99%")>
    <DataRow(98.0R, 3, "Light 96%-99%")>
    <DataRow(99.0R, 3, "Light 99%-100%")>
    <DataRow(100.0R, 3, "Light 99%-100%")>
    <DataRow(91.0R, 7, "Light 91%-98%")>
    <DataRow(97.0R, 7, "Light 91%-98%")>
    <DataRow(98.0R, 7, "Light 98%-100%")>
    <DataRow(100.0R, 7, "Light 98%-100%")>
    <DataRow(97.0R, 8, "Light 96%-100%")>
    <DataRow(90.0R, 9, "Light 90%-99%")>
    <DataRow(99.0R, 9, "Light 99%-100%")>
    Public Sub LightnessToFolderName_StepNotDivisorOf100_LastRangeIsCappedAndNeverOverlaps(averageL As Double, stepSize As Integer, expected As String)
        Dim result As String = ColorHelper.LightnessToFolderName(averageL, stepSize)

        Assert.AreEqual(expected, result)
    End Sub

    <TestMethod>
    <DataRow(-0.1R, 10, "Light 00%-10%")>
    <DataRow(-50.0R, 10, "Light 00%-10%")>
    <DataRow(100.1R, 10, "Light 90%-100%")>
    <DataRow(250.0R, 5, "Light 95%-100%")>
    <DataRow(Double.MaxValue, 10, "Light 90%-100%")>
    <DataRow(Double.MinValue, 10, "Light 00%-10%")>
    Public Sub LightnessToFolderName_OutOfRangeValues_AreClamped(averageL As Double, stepSize As Integer, expected As String)
        Dim result As String = ColorHelper.LightnessToFolderName(averageL, stepSize)

        Assert.AreEqual(expected, result)
    End Sub

    <TestMethod>
    <DataRow(1)>
    <DataRow(3)>
    <DataRow(5)>
    <DataRow(10)>
    Public Sub LightnessToFolderName_ZeroAndNearZero_MapToTheSameFolder(stepSize As Integer)
        ' Regression test: pure black (L*=0) and very dark values used to produce two differently
        ' formatted folder names ("Light 00%-10%" vs. "Light 00-10%") for the same range.
        Dim pureBlack As String = ColorHelper.LightnessToFolderName(0.0R, stepSize)
        Dim nearBlack As String = ColorHelper.LightnessToFolderName(stepSize - 0.6R, stepSize)

        Assert.AreEqual(pureBlack, nearBlack)
    End Sub

    <TestMethod>
    Public Sub LightnessToFolderName_AllSteps_UseConsistentFormat()
        For stepSize As Integer = ColorHelper.MinStepSize To ColorHelper.MaxStepSize
            For pct As Integer = 0 To 100
                Dim folderName As String = ColorHelper.LightnessToFolderName(pct, stepSize)

                Assert.IsTrue(ColorHelperTests.FolderNameRegex.IsMatch(folderName),
                              $"Unexpected folder name format '{folderName}' (L*={pct}, step={stepSize}).")
            Next
        Next
    End Sub

    <TestMethod>
    Public Sub LightnessToFolderName_AllSteps_RangesContainTheValueAndRespectTheStep()
        For stepSize As Integer = ColorHelper.MinStepSize To ColorHelper.MaxStepSize
            For pct As Integer = 0 To 100
                Dim folderName As String = ColorHelper.LightnessToFolderName(pct, stepSize)
                Dim bounds As (Lo As Integer, Hi As Integer) = ColorHelperTests.ParseBounds(folderName)
                Dim context As String = $"'{folderName}' (L*={pct}, step={stepSize})"

                Assert.IsLessThanOrEqualTo(pct, bounds.Lo, $"Lower bound exceeds the value: {context}.")
                Assert.IsTrue(pct < bounds.Hi OrElse (pct = 100 AndAlso bounds.Hi = 100), $"Upper bound does not contain the value: {context}.")
                Assert.IsGreaterThanOrEqualTo(1, bounds.Hi - bounds.Lo, $"Empty range: {context}.")
                Assert.IsLessThanOrEqualTo(stepSize, bounds.Hi - bounds.Lo, $"Range wider than the step: {context}.")
                Assert.AreEqual(0, bounds.Lo Mod stepSize, $"Lower bound is not a multiple of the step: {context}.")
            Next
        Next
    End Sub

    <TestMethod>
    Public Sub LightnessToFolderName_AllSteps_RangesAreContiguousAndNonOverlapping()
        For stepValue As Integer = ColorHelper.MinStepSize To ColorHelper.MaxStepSize
            Dim stepSize As Integer = stepValue
            Dim distinctRanges As List(Of (Lo As Integer, Hi As Integer)) =
                Enumerable.Range(0, 101).
                           Select(Function(pct As Integer) ColorHelper.LightnessToFolderName(pct, stepSize)).
                           Distinct(StringComparer.Ordinal).
                           Select(Function(name As String) ColorHelperTests.ParseBounds(name)).
                           OrderBy(Function(range As (Lo As Integer, Hi As Integer)) range.Lo).
                           ToList()

            Dim expectedRangeCount As Integer = Convert.ToInt32(Math.Ceiling(100.0R / stepSize))
            Assert.HasCount(expectedRangeCount, distinctRanges, $"Unexpected range count for step {stepSize}.")
            Assert.AreEqual(0, distinctRanges.First().Lo, $"The first range must start at 0% (step {stepSize}).")
            Assert.AreEqual(100, distinctRanges.Last().Hi, $"The last range must end at 100% (step {stepSize}).")

            For i As Integer = 1 To distinctRanges.Count - 1
                Assert.AreEqual(distinctRanges(i - 1).Hi, distinctRanges(i).Lo,
                                $"Ranges must be contiguous and non-overlapping (step {stepSize}, index {i}).")
            Next
        Next
    End Sub

    <TestMethod>
    <DataRow(0)>
    <DataRow(-1)>
    <DataRow(11)>
    <DataRow(100)>
    <DataRow(Integer.MinValue)>
    <DataRow(Integer.MaxValue)>
    Public Sub LightnessToFolderName_InvalidStep_ThrowsArgumentOutOfRangeException(stepSize As Integer)
        Dim ex As ArgumentOutOfRangeException =
            Assert.ThrowsExactly(Of ArgumentOutOfRangeException)(Sub() ColorHelper.LightnessToFolderName(50.0R, stepSize))

        Assert.AreEqual("stepSize", ex.ParamName)
    End Sub

    <TestMethod>
    <DataRow(Double.NaN)>
    <DataRow(Double.PositiveInfinity)>
    <DataRow(Double.NegativeInfinity)>
    Public Sub LightnessToFolderName_NonFiniteLightness_ThrowsArgumentOutOfRangeException(averageL As Double)
        Dim ex As ArgumentOutOfRangeException =
            Assert.ThrowsExactly(Of ArgumentOutOfRangeException)(Sub() ColorHelper.LightnessToFolderName(averageL, 10))

        Assert.AreEqual("averageL", ex.ParamName)
    End Sub

    <TestMethod>
    Public Sub LightnessToFolderName_IsCultureInvariant()
        Dim originalCulture As CultureInfo = CultureInfo.CurrentCulture
        Try
            For Each cultureName As String In {"es-ES", "ar-SA", "fa-IR", "hi-IN", "th-TH"}
                CultureInfo.CurrentCulture = New CultureInfo(cultureName)

                Assert.AreEqual("Light 50%-60%", ColorHelper.LightnessToFolderName(55.0R, 10), $"Culture: {cultureName}")
            Next
        Finally
            CultureInfo.CurrentCulture = originalCulture
        End Try
    End Sub

#End Region

#Region " Private Helpers "

    ''' <summary>
    ''' Parses the lower and upper percentage bounds from a lightness folder name.
    ''' </summary>
    '''
    ''' <param name="folderName">
    ''' The folder name (e.g., "Light 10%-20%").
    ''' </param>
    '''
    ''' <returns>
    ''' The parsed lower and upper bounds.
    ''' </returns>
    Private Shared Function ParseBounds(folderName As String) As (Lo As Integer, Hi As Integer)

        Dim match As Match = ColorHelperTests.FolderNameRegex.Match(folderName)
        If Not match.Success Then
            Throw New FormatException($"Invalid lightness folder name: '{folderName}'.")
        End If

        Return (Integer.Parse(match.Groups("lo").Value, CultureInfo.InvariantCulture),
                Integer.Parse(match.Groups("hi").Value, CultureInfo.InvariantCulture))
    End Function

#End Region

End Class

#End Region
