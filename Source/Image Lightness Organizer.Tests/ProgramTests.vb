#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " ProgramTests "

''' <summary>
''' Unit tests for the pure helper functions of <see cref="Program"/> module.
''' </summary>
<TestClass>
Public NotInheritable Class ProgramTests

#Region " IsSupportedImageFile "

    <TestMethod>
    <DataRow("image.jpg")>
    <DataRow("image.jpeg")>
    <DataRow("image.tif")>
    <DataRow("image.tiff")>
    <DataRow("image.bmp")>
    <DataRow("image.png")>
    <DataRow("image.webp")>
    <DataRow("image.ico")>
    <DataRow("image.avif")>
    <DataRow("IMAGE.JPG")>
    <DataRow("Image.PnG")>
    <DataRow("my.holiday.photo.WebP")>
    <DataRow("C:\Pictures\Wallpaper.jpg")>
    <DataRow("\\?\C:\Pictures\Wallpaper.jpeg")>
    <DataRow("\\Server\Share\Folder\Icon.ico")>
    Public Sub IsSupportedImageFile_SupportedExtension_ReturnsTrue(filePath As String)
        Assert.IsTrue(Program.IsSupportedImageFile(filePath))
    End Sub

    <TestMethod>
    <DataRow("image.gif")>
    <DataRow("image.txt")>
    <DataRow("image.jfif")>
    <DataRow("image.heic")>
    <DataRow("image.svg")>
    <DataRow("image.jpgx")>
    <DataRow("image.jpg.txt")>
    <DataRow("image")>
    <DataRow("image.")>
    <DataRow("jpg")>
    <DataRow("")>
    <DataRow("C:\Pictures.png\Readme")>
    Public Sub IsSupportedImageFile_UnsupportedOrMissingExtension_ReturnsFalse(filePath As String)
        Assert.IsFalse(Program.IsSupportedImageFile(filePath))
    End Sub

    <TestMethod>
    Public Sub IsSupportedImageFile_Nothing_ThrowsArgumentNullException()
        Assert.ThrowsExactly(Of ArgumentNullException)(Sub() Program.IsSupportedImageFile(Nothing))
    End Sub

#End Region

#Region " TryParseStepPercentage "

    <TestMethod>
    <DataRow("1", 1)>
    <DataRow("2", 2)>
    <DataRow("5", 5)>
    <DataRow("9", 9)>
    <DataRow("10", 10)>
    <DataRow("05", 5)>
    <DataRow("+7", 7)>
    <DataRow(" 3 ", 3)>
    Public Sub TryParseStepPercentage_ValidValue_ReturnsTrueAndParsedValue(value As String, expected As Integer)
        Dim result As Integer = -1

        Dim success As Boolean = Program.TryParseStepPercentage(value, result)

        Assert.IsTrue(success)
        Assert.AreEqual(expected, result)
    End Sub

    <TestMethod>
    <DataRow("0")>
    <DataRow("11")>
    <DataRow("-1")>
    <DataRow("100")>
    <DataRow("abc")>
    <DataRow("")>
    <DataRow("   ")>
    <DataRow("5.5")>
    <DataRow("5,0")>
    <DataRow("1e1")>
    <DataRow("0x5")>
    <DataRow("2147483648")>
    <DataRow(Nothing)>
    Public Sub TryParseStepPercentage_InvalidValue_ReturnsFalseAndZero(value As String)
        Dim result As Integer = -1

        Dim success As Boolean = Program.TryParseStepPercentage(value, result)

        Assert.IsFalse(success)
        Assert.AreEqual(0, result)
    End Sub

    <TestMethod>
    Public Sub TryParseStepPercentage_AcceptsTheWholeAllowedRange()
        For stepValue As Integer = ColorHelper.MinStepSize To ColorHelper.MaxStepSize
            Dim result As Integer

            Assert.IsTrue(Program.TryParseStepPercentage(stepValue.ToString(Globalization.CultureInfo.InvariantCulture), result))
            Assert.AreEqual(stepValue, result)
            ' Every accepted step value must also be accepted by the folder name function.
            Assert.IsFalse(String.IsNullOrEmpty(ColorHelper.LightnessToFolderName(50.0R, result)))
        Next
    End Sub

#End Region

#Region " CalculateMaxDegreeOfParallelism "

    <TestMethod>
    <DataRow(16.0R, 1.0R, 8, 8)>      ' Plenty of memory: limited by CPU cores.
    <DataRow(4.0R, 1.0R, 8, 4)>       ' Limited by memory.
    <DataRow(4.9R, 1.0R, 8, 4)>       ' Partial gigabytes are floored.
    <DataRow(8.0R, 2.0R, 16, 4)>      ' Custom memory budget per thread.
    <DataRow(1.5R, 0.5R, 16, 3)>
    <DataRow(0.5R, 1.0R, 8, 1)>       ' Very low memory: at least 1 thread.
    <DataRow(0.0R, 1.0R, 8, 1)>
    <DataRow(64.0R, 1.0R, 1, 1)>      ' Single core.
    <DataRow(1.0E+300R, 1.0R, 32, 32)> ' Huge ratio must not overflow.
    Public Sub CalculateMaxDegreeOfParallelism_X64_ReturnsExpectedValue(availableMemoryGB As Double,
                                                                        memoryPerThreadGB As Double,
                                                                        processorCount As Integer,
                                                                        expected As Integer)
        Dim result As Integer = Program.CalculateMaxDegreeOfParallelism(availableMemoryGB, memoryPerThreadGB, processorCount, isX86Process:=False)

        Assert.AreEqual(expected, result)
    End Sub

    <TestMethod>
    <DataRow(64.0R, 1.0R, 16)>
    <DataRow(0.0R, 1.0R, 1)>
    <DataRow(1.0R, 1.0R, 128)>
    Public Sub CalculateMaxDegreeOfParallelism_X86_AlwaysReturnsOne(availableMemoryGB As Double, memoryPerThreadGB As Double, processorCount As Integer)
        Dim result As Integer = Program.CalculateMaxDegreeOfParallelism(availableMemoryGB, memoryPerThreadGB, processorCount, isX86Process:=True)

        Assert.AreEqual(1, result)
    End Sub

    <TestMethod>
    Public Sub CalculateMaxDegreeOfParallelism_ResultIsAlwaysWithinBounds()
        For Each processorCount As Integer In {1, 2, 3, 4, 8, 12, 16, 64}
            For memory As Double = 0.0R To 128.0R Step 0.25R
                Dim result As Integer = Program.CalculateMaxDegreeOfParallelism(memory, 1.0R, processorCount, isX86Process:=False)

                Assert.IsGreaterThanOrEqualTo(1, result)
                Assert.IsLessThanOrEqualTo(processorCount, result)
            Next
        Next
    End Sub

    <TestMethod>
    <DataRow(Double.NaN)>
    <DataRow(Double.PositiveInfinity)>
    <DataRow(Double.NegativeInfinity)>
    <DataRow(-0.001R)>
    <DataRow(-16.0R)>
    Public Sub CalculateMaxDegreeOfParallelism_InvalidAvailableMemory_ThrowsArgumentOutOfRangeException(availableMemoryGB As Double)
        Dim ex As ArgumentOutOfRangeException =
            Assert.ThrowsExactly(Of ArgumentOutOfRangeException)(
                Sub() Program.CalculateMaxDegreeOfParallelism(availableMemoryGB, 1.0R, 8, isX86Process:=False))

        Assert.AreEqual("availableMemoryGB", ex.ParamName)
    End Sub

    <TestMethod>
    <DataRow(0.0R)>
    <DataRow(-1.0R)>
    <DataRow(Double.NaN)>
    <DataRow(Double.PositiveInfinity)>
    Public Sub CalculateMaxDegreeOfParallelism_InvalidMemoryPerThread_ThrowsArgumentOutOfRangeException(memoryPerThreadGB As Double)
        Dim ex As ArgumentOutOfRangeException =
            Assert.ThrowsExactly(Of ArgumentOutOfRangeException)(
                Sub() Program.CalculateMaxDegreeOfParallelism(16.0R, memoryPerThreadGB, 8, isX86Process:=False))

        Assert.AreEqual("memoryRequiredPerThreadGB", ex.ParamName)
    End Sub

    <TestMethod>
    <DataRow(0)>
    <DataRow(-1)>
    <DataRow(Integer.MinValue)>
    Public Sub CalculateMaxDegreeOfParallelism_InvalidProcessorCount_ThrowsArgumentOutOfRangeException(processorCount As Integer)
        Dim ex As ArgumentOutOfRangeException =
            Assert.ThrowsExactly(Of ArgumentOutOfRangeException)(
                Sub() Program.CalculateMaxDegreeOfParallelism(16.0R, 1.0R, processorCount, isX86Process:=False))

        Assert.AreEqual("processorCount", ex.ParamName)
    End Sub

    <TestMethod>
    Public Sub CalculateMaxDegreeOfParallelism_InvalidArguments_ThrowEvenForX86()
        ' Argument validation must not be skipped by the x86 shortcut (fail-fast).
        Assert.ThrowsExactly(Of ArgumentOutOfRangeException)(
            Sub() Program.CalculateMaxDegreeOfParallelism(Double.NaN, 1.0R, 8, isX86Process:=True))
        Assert.ThrowsExactly(Of ArgumentOutOfRangeException)(
            Sub() Program.CalculateMaxDegreeOfParallelism(16.0R, 0.0R, 8, isX86Process:=True))
        Assert.ThrowsExactly(Of ArgumentOutOfRangeException)(
            Sub() Program.CalculateMaxDegreeOfParallelism(16.0R, 1.0R, 0, isX86Process:=True))
    End Sub

#End Region

End Class

#End Region
