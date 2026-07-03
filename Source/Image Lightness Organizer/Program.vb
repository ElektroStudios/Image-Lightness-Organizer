#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading

#If Not NETCOREAPP Then
Imports DevCase.Runtime.TypeComparers
#End If

#End Region

Public Module Program

#Region " Fields "

    ''' <summary>
    ''' The set of supported image file extensions for processing.
    ''' </summary>
    Private ReadOnly SupportedImageFileExtensions As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
            ".jpg", ".jpeg",
            ".tiff", ".tif",
            ".bmp",
            ".png",
            ".webp",
            ".ico",
            ".avif"
        }

    ''' <summary>
    ''' The name of the output folder where sorted images will be organized by lightness.
    ''' </summary>
    Private Const OutputFolderName As String = "@Sorted by lightness"

    ''' <summary>
    ''' The default percentage step size used for folder name ranges when no custom argument is provided.
    ''' <para></para>
    ''' For example, a default value of 10 organizes images into folders with 10% increments (e.g., "Light 00%-10%", "Light 10%-20%").
    ''' </summary>
    Private Const DefaultStepPercentage As Integer = 10

    ''' <summary>
    ''' The maximum estimated amount of physical memory, in Gigabytes (GB), required by a single thread to process an image.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' This value is used as a safety budget constraint to dynamically calculate the <see cref="ParallelOptions.MaxDegreeOfParallelism"/> 
    ''' and prevent <see cref="OutOfMemoryException"/> crashes, particularly under the x86 architecture constraints.
    ''' </remarks>
    Private Const MaxEstimatedMemoryRequiredPerThreadGB As Double = 1.0

    ''' <summary>
    ''' Gets a value indicating whether the current application process is running under the x86 (32-bit) architecture.
    ''' <para></para>
    ''' It is primarily used to apply memory safety constraints and prevent address space fragmentation crashes.
    ''' </summary>
    Private ReadOnly isX86 As Boolean = (IntPtr.Size = 4)

    ''' <summary>
    ''' The <see cref="CultureInfo"/> instance representing the "en-US" culture.
    ''' </summary>
    Private ReadOnly CultureInfoEnUs As New CultureInfo("en-US")

    ''' <summary>
    ''' The UTF-8 encoding instance used for console output, configured to not emit a BOM (Byte Order Mark).
    ''' </summary>
    Private ReadOnly ConsoleEncoding As New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False)

    ''' <summary>
    ''' The synchronization object used to ensure thread-safe console output when processing files in parallel.
    ''' </summary>
    Friend ReadOnly syncLockObject As New Object()

#End Region

#Region " Entry Point "

    ''' <summary>
    ''' The main entry point of the application.
    ''' </summary>
    ''' 
    ''' <param name="args">
    ''' The command-line arguments passed to the application.
    ''' <para></para>
    ''' The first argument (args(0)) is expected to be the path to the 
    ''' source directory containing image files to process.
    ''' </param>
    <DebuggerStepperBoundary>
    Public Sub Main(args As String())

        Thread.CurrentThread.CurrentCulture = Program.CultureInfoEnUs
        Thread.CurrentThread.CurrentUICulture = Program.CultureInfoEnUs

        Console.OutputEncoding = Program.ConsoleEncoding
        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.White

#If NETCOREAPP Then
        Dim versionInfo As FileVersionInfo = FileVersionInfo.GetVersionInfo(Environment.ProcessPath)
        Dim version As String = versionInfo.FileVersion
        Dim assemblyTitle As String = versionInfo.FileDescription
#Else
        Dim version As String = My.Application.Info.Version.ToString(fieldCount:=3)
        Dim assemblyTitle As String = My.Application.Info.Title
#End If

        Dim consoletitle As String = $"{assemblyTitle} {version} ─ by ElektroStudios"
#If Debug Then
        Console.Title = consoletitle
#End If
        ConsoleHelper.WriteColoredLine(" " & consoletitle, ConsoleColor.Cyan)
        Console.WriteLine("╭─────────────────────────────────────────────────────────────────────────────────────╮")
        Console.WriteLine("│ Purpose:                                                                            │")
        Console.WriteLine("│   This application processes image files and organizes them into folders            │")
        Console.WriteLine("│   based on their average lightness value in the CIE L* color space.                 │")
        Console.WriteLine("│                                                                                     │")
        Console.WriteLine("│ Processing:                                                                         │")
        Console.WriteLine("│   • Processes supported image files in the source directory (top-level files only). │")
        Console.WriteLine("│   • Computes the average lightness (L*) for each image.                             │")
        Console.WriteLine("│   • Creates output folder '@Sorted by lightness' in the source directory.           │")
        Console.WriteLine("│   • Organizes the images into sub folders: 'Light 00%-10%', ..., 'Light 90%-100%'   │")
        Console.WriteLine("│                                                                                     │")
        Console.WriteLine("│ Supported image file extensions: jpg/jpeg, tiff/tif, bmp, png, webp, ico and avif.  │")
        Console.WriteLine("╰─────────────────────────────────────────────────────────────────────────────────────╯")
        Console.WriteLine()

        If args.Length = 0 Then
            Program.ShowUsage()
            ConsoleHelper.ExitWithMessage("[ERROR] Missing argument(s). See usage above.", exitCode:=2, ConsoleColor.Red)
        End If

        Dim totalMovedFiles As Integer = 0
        Dim totalFailedFiles As Integer = 0

        Dim stepPercentage As Integer = Program.DefaultStepPercentage
        If args.Length >= 2 Then
            If Not Integer.TryParse(args(1), NumberStyles.Integer, Program.CultureInfoEnUs, stepPercentage) OrElse stepPercentage < 1 OrElse stepPercentage > 10 Then
                Program.ShowUsage()
                ConsoleHelper.ExitWithMessage("[ERROR] The percentage step value must be an integer between 1 and 10.", exitCode:=5, ConsoleColor.Red)
            End If
        End If

        Dim sourceDirPath As String = args(0)
        Try
            Dim sourceDirPathExtended As String = Path.GetFullPath(sourceDirPath)
            sourceDirPathExtended = FileSystemHelper.GetExtendedPath(sourceDirPathExtended)

            If Not Directory.Exists(sourceDirPath) Then
                Dim exitMsg As String = $"[ERROR] The specified directory path does not exist: {sourceDirPath}"
                ConsoleHelper.ExitWithMessage(exitMsg, exitCode:=3, ConsoleColor.Red)
            End If

            Dim destRoot As String = Path.Combine(sourceDirPathExtended, Program.OutputFolderName)

#If NETCOREAPP Then
            Dim filePathComparer As StringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering)
#Else
            Dim filePathComparer As New StringNaturalComparer()
#End If

            Dim sourceFiles As New SortedSet(Of String)(
                Directory.GetFiles(sourceDirPathExtended, "*.*", SearchOption.TopDirectoryOnly).
                          Where(Function(f As String)
                                    If Path.HasExtension(f) Then
                                        Dim ext As String = Path.GetExtension(f)
                                        Return Program.SupportedImageFileExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)
                                    Else
                                        Return False
                                    End If
                                End Function), filePathComparer)

            Dim totalSourceFileCount As Integer =
                If(sourceFiles Is Nothing, 0, sourceFiles.Count)

            If totalSourceFileCount = 0 Then
                Dim exitMsg As String = $"[ERROR] No supported image files were found in source directory: {sourceDirPath}"
                ConsoleHelper.ExitWithMessage(exitMsg, exitCode:=4, ConsoleColor.Red)
            End If

            ConsoleHelper.WriteColoredLine($"Source directory path  : {sourceDirPath}", ConsoleColor.DarkCyan)
            ConsoleHelper.WriteColoredLine($"Output directory path  : {destRoot}", ConsoleColor.DarkCyan)
            ConsoleHelper.WriteColoredLine($"Supported files found  : {totalSourceFileCount:N0} image files", ConsoleColor.DarkCyan)
            ConsoleHelper.WriteColoredLine($"Interval percentage    : {stepPercentage}% steps", ConsoleColor.DarkCyan)
            Console.WriteLine()

            Dim availableMemoryGB As Double

            If Program.isX86 Then
                ' x86 architecture safety ceiling (~1.0 GB).
                availableMemoryGB = 1.0
            Else
                ' x64 architecture: Query the OS for actual, real-time free physical RAM.
                Dim memStatus As New MEMORYSTATUSEX()
                memStatus.dwLength = Convert.ToUInt32(Marshal.SizeOf(memStatus))

                If NativeMethods.GlobalMemoryStatusEx(memStatus) Then
                    ' Convert bytes to Gigabytes explicitly.
                    availableMemoryGB = Convert.ToDouble(memStatus.ullAvailPhys) / 1024.0 / 1024.0 / 1024.0
                Else
                    Dim errorCode As Integer = Marshal.GetLastPInvokeError()
                    Throw New Win32Exception(errorCode, $"Failed to retrieve system memory status. Function 'GlobalMemoryStatusEx' returned Win32 error code {errorCode}.")
                End If
            End If
            ConsoleHelper.WriteColoredLine($"Available total memory : {availableMemoryGB:F2} GB", ConsoleColor.DarkCyan)

            ' Calculate safe parallelism degree based on memory constraints.
            Dim maxParallelism As Integer
            Dim safeParallelismByMemory As Integer
            If Program.isX86 Then
                maxParallelism = 1
                safeParallelismByMemory = 1

            Else
                safeParallelismByMemory =
                    Convert.ToInt32(Math.Floor(availableMemoryGB / Program.MaxEstimatedMemoryRequiredPerThreadGB))

                ' Ensure at least 1 thread runs if memory is extremely low.
                If safeParallelismByMemory < 1 Then
                    safeParallelismByMemory = 1
                End If

                ' Cap the parallelism so it never exceeds the CPU core count.
                maxParallelism = Environment.ProcessorCount
                If safeParallelismByMemory < maxParallelism Then
                    maxParallelism = safeParallelismByMemory
                End If
            End If

            If Not Program.isX86 Then
                ConsoleHelper.WriteColoredLine($"[INFO] CPU Cores: {Environment.ProcessorCount}", ConsoleColor.Magenta)
            End If
            ConsoleHelper.WriteColoredLine($"[INFO] OS Architecture: {If(IntPtr.Size = 4, "x86", "x64")}", ConsoleColor.Magenta)
            If Not Program.isX86 Then
                ConsoleHelper.WriteColoredLine($"[INFO] Available Memory Budget: {availableMemoryGB:F2} GB", ConsoleColor.Magenta)
            End If
            ConsoleHelper.WriteColoredLine($"[INFO] Selected Max Degree Of Parallelism: {maxParallelism}", ConsoleColor.Magenta)
            Console.WriteLine()

            ConsoleHelper.WriteColoredLine("Please note that files are processed in parallel, so output may appear disordered.", ConsoleColor.White)
            If Program.isX86 Then
                Console.WriteLine()
                ConsoleHelper.WriteColoredLine("[WARNING] To prevent an application crash due to memory overflow during image decompression, 32-bit mode restricts processing to a single file at a time. Expect very longer execution time to complete.", ConsoleColor.Yellow)
            End If
            Console.WriteLine()

#If DEBUG Then
            ConsoleHelper.WriteColoredLine("Press 'Y' key to start processing the files, or 'Escape' key to exit...", ConsoleColor.Yellow)
            ConsoleHelper.WriteColoredLine("[!] This message only appears in DEBUG mode to prevent accidental execution.", ConsoleColor.Yellow)
            Console.WriteLine()
            Do
                Dim keyInfo As ConsoleKeyInfo = Console.ReadKey(intercept:=True)
                If keyInfo.Key = ConsoleKey.Y Then
                    Exit Do
                ElseIf keyInfo.Key = ConsoleKey.Escape Then
                    Environment.Exit(0)
                End If
            Loop
            Console.WriteLine()
#End If

            Dim currentFileIndexPadding As Integer = totalSourceFileCount.ToString().Length
            Dim currentFileIndex As Integer = 0

            Dim parallelOptions As New ParallelOptions() With {
                .MaxDegreeOfParallelism = maxParallelism
            }

            Parallel.ForEach(sourceFiles, parallelOptions,
                    Sub(filePath As String)
                        Try
                            Dim fileName As String = Path.GetFileName(filePath)
                            Dim averageL As Double = ImageHelper.ComputeAverageLightness(filePath)
                            Dim folderName As String = ColorHelper.LightnessToFolderName(averageL, stepPercentage)
                            Dim destinationDirectory As String = Path.Combine(destRoot, folderName)
                            Dim destinationFilePath As String = Path.Combine(destinationDirectory, fileName)
                            If Not Directory.Exists(destinationDirectory) Then
                                SyncLock Program.syncLockObject
                                    Directory.CreateDirectory(destinationDirectory)
                                End SyncLock
                            End If

                            If File.Exists(destinationFilePath) Then
                                ConsoleHelper.WriteColoredLine($"[{Interlocked.Increment(currentFileIndex).ToString("N0").PadLeft(currentFileIndexPadding, " "c):N0} / {totalSourceFileCount:N0}] [WARN] Cannot move file. The destination file already exists in output directory: {destinationFilePath}.", ConsoleColor.Yellow)
                                totalFailedFiles += 1
                            Else
                                File.Move(filePath, destinationFilePath)
                                ConsoleHelper.WriteColoredLine($"[{Interlocked.Increment(currentFileIndex).ToString("N0").PadLeft(currentFileIndexPadding, " "c):N0} / {totalSourceFileCount:N0}] [SUCCESS] {Path.GetFileName(filePath)} -> {folderName} (L*={averageL:F1})", ConsoleColor.Green)
                                totalMovedFiles += 1
                            End If

                        Catch ex As ArgumentException When (ex.HResult = -2147024809)
                            ConsoleHelper.WriteColoredLine($"[{Interlocked.Increment(currentFileIndex).ToString("N0").PadLeft(currentFileIndexPadding, " "c):N0} / {totalSourceFileCount:N0}] [ERROR] {Path.GetFileName(filePath)} -> HResult: {ex.HResult} (0x{ex.HResult}) - Corrupted file or unsupported image format (file extension may not be correct).", ConsoleColor.Red)
                            totalFailedFiles += 1

                        Catch ex As Exception
                            ConsoleHelper.WriteColoredLine($"[{Interlocked.Increment(currentFileIndex).ToString("N0").PadLeft(currentFileIndexPadding, " "c):N0} / {totalSourceFileCount:N0}] [ERROR] {Path.GetFileName(filePath)} -> HResult: {ex.HResult} (0x{ex.HResult}) - {ex.Message}", ConsoleColor.Red)
                            totalFailedFiles += 1

                        Finally
                            If Program.isX86 Then
                                ' Force aggressive garbage collection in x86 to fight fragmentation.
                                GC.Collect()
                                GC.WaitForPendingFinalizers()
                            End If
#If DEBUG Then
                            Thread.CurrentThread.Join(0) ' Prevents ContextSwitchDeadlock on long-running iterations.
#End If
                        End Try

                    End Sub
                )
            Console.WriteLine()

        Catch ex As Exception
            Console.WriteLine()
            ConsoleHelper.ExitWithMessage($"FATAL ERROR 0x{ex.HResult}: {ex.Message}", exitCode:=ex.HResult, ConsoleColor.Red)

        End Try

        Dim exitCode As Integer = If(totalFailedFiles = 0, 0, 1)
        Dim exitColor As ConsoleColor = If(exitCode = 0, ConsoleColor.Green, ConsoleColor.Red)
        ConsoleHelper.ExitWithMessage($"All files have been processed. Success: {totalMovedFiles:N0}, Failed: {totalFailedFiles:N0}.", exitCode, exitColor)
    End Sub

#End Region

#Region " Private Methods "


    ''' <summary>
    ''' Prints command-line usage information to the console. 
    ''' <para></para>
    ''' Called whenever a mandatory or optional argument is missing or invalid.
    ''' </summary>
    <DebuggerStepThrough>
    Private Sub ShowUsage()

        Dim executableName As String = $"{Process.GetCurrentProcess().ProcessName}.exe"

        ConsoleHelper.WriteColoredLine("Usage:", ConsoleColor.DarkCyan)
        Console.WriteLine($"  {executableName} <directory_path> [percentage_step]")
        Console.WriteLine()
        ConsoleHelper.WriteColoredLine("Arguments:", ConsoleColor.DarkCyan)
        Console.WriteLine("  directory_path    Path to the directory containing image files to be processed.")
        Console.WriteLine("  percentage_step   Optional. An integer from 1 to 10 indicating folder percentage ranges (Default is 10).")
        Console.WriteLine()
        ConsoleHelper.WriteColoredLine("Examples:", ConsoleColor.DarkCyan)
        Console.WriteLine($"  {executableName} ""C:\MyImages""")
        Console.WriteLine($"  {executableName} ""C:\MyImages"" 5")
        Console.WriteLine()
    End Sub

#End Region

End Module