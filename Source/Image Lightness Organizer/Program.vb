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

Imports DevCase.Runtime.TypeComparers

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
    ''' The synchronization object used to ensure thread-safe console output when processing files in parallel.
    ''' </summary>
    Private ReadOnly syncLockObject As New Object()

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
    Public Sub Main(args As String())

        Thread.CurrentThread.CurrentCulture = Program.CultureInfoEnUs
        Thread.CurrentThread.CurrentUICulture = Program.CultureInfoEnUs

        Console.OutputEncoding = Encoding.UTF8
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
        Program.WriteColoredLine(" " & consoletitle, ConsoleColor.Cyan)
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
            Dim executableName As String = Process.GetCurrentProcess().ProcessName & ".exe"
            Dim exitMsg As String = "[ERROR] Source directory path argument is required." &
                                    Environment.NewLine & Environment.NewLine &
                                    $"Usage: {executableName} <directory_path>"
            Program.ExitWithMessage(exitMsg, exitCode:=2, ConsoleColor.Red)
        End If

        Dim totalMovedFiles As Integer = 0
        Dim totalFailedFiles As Integer = 0

        Dim sourceDirPath As String = args(0)
        Try
            If Not Directory.Exists(sourceDirPath) Then
                Dim exitMsg As String = $"[ERROR] The specified directory path does not exist: {sourceDirPath}"
                Program.ExitWithMessage(exitMsg, exitCode:=3, ConsoleColor.Red)
            End If

            sourceDirPath = Path.GetFullPath(args(0))

            Dim destRoot As String = Path.Combine(sourceDirPath, Program.OutputFolderName)

            Dim naturalSortOrderComparer As New StringNaturalComparer()

            Dim sourceFiles As New SortedSet(Of String)(
                Directory.GetFiles(sourceDirPath, "*.*", SearchOption.TopDirectoryOnly).
                          Where(Function(f As String)
                                    If Path.HasExtension(f) Then
                                        Dim ext As String = Path.GetExtension(f)
                                        Return Program.SupportedImageFileExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)
                                    Else
                                        Return False
                                    End If
                                End Function), naturalSortOrderComparer)

            Dim totalSourceFileCount As Integer =
                If(sourceFiles Is Nothing, 0, sourceFiles.Count)

            If totalSourceFileCount = 0 Then
                Dim exitMsg As String = $"[ERROR] No supported image files were found in source directory: {sourceDirPath}"
                Program.ExitWithMessage(exitMsg, exitCode:=4, ConsoleColor.Red)
            End If

            Program.WriteColoredLine($"Source directory path: {sourceDirPath}", ConsoleColor.DarkCyan)
            Program.WriteColoredLine($"Dest.  directory path: {destRoot}", ConsoleColor.DarkCyan)
            Program.WriteColoredLine($"Supported files found: {totalSourceFileCount:N0} image files", ConsoleColor.DarkCyan)
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
                Program.WriteColoredLine($"[INFO] CPU Cores: {Environment.ProcessorCount}", ConsoleColor.Magenta)
            End If
            Program.WriteColoredLine($"[INFO] OS Architecture: {If(IntPtr.Size = 4, "x86", "x64")}", ConsoleColor.Magenta)
            If Not Program.isX86 Then
                Program.WriteColoredLine($"[INFO] Available Memory Budget: {availableMemoryGB:F2} GB", ConsoleColor.Magenta)
            End If
            Program.WriteColoredLine($"[INFO] Selected Max Degree Of Parallelism: {maxParallelism}", ConsoleColor.Magenta)
            Console.WriteLine()

            Program.WriteColoredLine("Please note that files are processed in parallel, so output may appear disordered.", ConsoleColor.White)
            If Program.isX86 Then
                Console.WriteLine()
                Program.WriteColoredLine("[WARNING] To prevent an application crash due to memory overflow during image decompression, 32-bit mode restricts processing to a single file at a time. Expect very longer execution time to complete.", ConsoleColor.Yellow)
            End If
            Console.WriteLine()

#If Debug Then
            Program.WriteColoredLine("Press 'Y' key to start processing the files, or 'Escape' key to exit...", ConsoleColor.Yellow)
            Program.WriteColoredLine("[!] This message only appears in DEBUG mode to prevent accidental execution.", ConsoleColor.Yellow)
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
                            Dim averageL As Double = UtilImage.ComputeAverageLightness(filePath)
                            Dim folderName As String = Program.LightnessToFolderName(averageL)
                            Dim destinationDirectory As String = Path.Combine(destRoot, folderName)
                            Dim destinationFilePath As String = Path.Combine(destinationDirectory, fileName)
                            If Not Directory.Exists(destinationDirectory) Then
                                SyncLock Program.syncLockObject
                                    Directory.CreateDirectory(destinationDirectory)
                                End SyncLock
                            End If

                            If File.Exists(destinationFilePath) Then
                                Program.WriteColoredLine($"[{Interlocked.Increment(currentFileIndex).ToString("N0").PadLeft(currentFileIndexPadding, " "c):N0} / {totalSourceFileCount:N0}] [WARN] Cannot move file. The destination file already exists in output directory: {destinationFilePath}.", ConsoleColor.Yellow)
                                totalFailedFiles += 1
                            Else
                                File.Move(filePath, destinationFilePath)
                                Program.WriteColoredLine($"[{Interlocked.Increment(currentFileIndex).ToString("N0").PadLeft(currentFileIndexPadding, " "c):N0} / {totalSourceFileCount:N0}] [SUCCESS] {Path.GetFileName(filePath)} -> {folderName} (L*={averageL:F1})", ConsoleColor.Green)
                                totalMovedFiles += 1
                            End If

                        Catch ex As ArgumentException When (ex.HResult = -2147024809)
                            Program.WriteColoredLine($"[{Interlocked.Increment(currentFileIndex).ToString("N0").PadLeft(currentFileIndexPadding, " "c):N0} / {totalSourceFileCount:N0}] [ERROR] {Path.GetFileName(filePath)} -> HResult: {ex.HResult} (0x{ex.HResult:X8}) - Corrupted file or unsupported image format (file extension may not be correct).", ConsoleColor.Red)
                            totalFailedFiles += 1

                        Catch ex As Exception
                            Program.WriteColoredLine($"[{Interlocked.Increment(currentFileIndex).ToString("N0").PadLeft(currentFileIndexPadding, " "c):N0} / {totalSourceFileCount:N0}] [ERROR] {Path.GetFileName(filePath)} -> HResult: {ex.HResult} (0x{ex.HResult:X8}) - {ex.Message}", ConsoleColor.Red)
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
            Program.ExitWithMessage($"FATAL ERROR 0x{ex.HResult:X8}: {ex.Message}", exitCode:=ex.HResult, ConsoleColor.Red)

        End Try

        Dim exitCode As Integer = If(totalFailedFiles = 0, 0, 1)
        Dim exitColor As ConsoleColor = If(exitCode = 0, ConsoleColor.Green, ConsoleColor.Red)
        Program.ExitWithMessage($"All files have been processed. Success: {totalMovedFiles:N0}, Failed: {totalFailedFiles:N0}.", exitCode, exitColor)
    End Sub

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Maps an average CIE L* value (0–100) to a folder name representing a lightness range (10% increments).
    ''' </summary>
    ''' 
    ''' <param name="averageL">
    ''' The average CIE L* lightness value (0 to 100).
    ''' </param>
    ''' 
    ''' <returns>
    ''' A folder name indicating the lightness range:
    ''' - "Light 00%-10%" for very dark images (L* = 0-10)
    ''' - "Light 10%-20%", "Light 20%-30%", ..., "Light 80%-90%" for intermediate values
    ''' - "Light 90%-100%" for very bright colors (L* >= 90)
    ''' </returns>
    ''' 
    ''' <remarks>
    ''' Folder names are sorted alphabetically from darkest to brightest,
    ''' allowing easy visual sorting of images by luminosity in 10% lightness intervals.
    ''' </remarks>
    Private Function LightnessToFolderName(averageL As Double) As String

        Dim pct As Integer = CInt(Math.Round(averageL))
        pct = Math.Max(0, Math.Min(100, pct))

        If pct = 0 Then
            Return "Light 00%-10%"
        End If

        If pct >= 90 Then
            Return "Light 90%-100%"
        End If

        Dim groupBase As Integer = (pct \ 10) * 10
        Dim lo As Integer = groupBase
        Dim hi As Integer = groupBase + 10

        Return $"Light {lo:D2}-{hi:D2}%"
    End Function

    ''' <summary>
    ''' Writes a message to the console in a specified foreground color, 
    ''' then resets the color back to the original.
    ''' </summary>
    ''' 
    ''' <param name="message">
    ''' The message to display. If empty or null, no message is displayed.
    ''' </param>
    ''' 
    ''' <param name="foreColor">
    ''' The console foreground color to use when displaying the message. 
    ''' <para></para>
    ''' After writing the message, the console color is reset to its original value.
    ''' </param>
    <DebuggerStepThrough>
    Private Sub WriteColoredLine(message As String, foreColor As ConsoleColor)

        SyncLock Program.syncLockObject
            Dim originalForeColor As ConsoleColor = Console.ForegroundColor
            Console.ForegroundColor = foreColor
            Console.WriteLine(message)
            Console.ForegroundColor = originalForeColor
        End SyncLock
    End Sub

    ''' <summary>
    ''' Displays a message to the console and exits the application with the specified exit code.
    ''' </summary>
    ''' 
    ''' <param name="message">
    ''' The message to display before exiting. If empty or null, no message is displayed.
    ''' </param>
    ''' 
    ''' <param name="exitCode">
    ''' The exit code to return to the operating system. Typically 0 for success, non-zero for errors.
    ''' </param>
    ''' 
    ''' <param name="foreColor">
    ''' The console foreground color to use when displaying the message. 
    ''' <para></para>
    ''' After writing the message, the console color is reset to its original value.
    ''' </param>
    <DebuggerStepThrough>
    Private Sub ExitWithMessage(message As String, exitCode As Integer, foreColor As ConsoleColor)

        SyncLock Program.syncLockObject
            If Not String.IsNullOrEmpty(message) Then
                WriteColoredLine(message, foreColor)
                Console.WriteLine()
            End If

            Console.WriteLine($"Exiting application with exit code: {exitCode} (0x{exitCode:X8}) ...")
#If DEBUG Then
            Console.WriteLine()
            Program.WriteColoredLine("[!] This message only appears in DEBUG mode to prevent accidental termination.", ConsoleColor.Yellow)
            Console.WriteLine("Press any key to exit...")
            Console.ReadKey(intercept:=True)
#End If
            Environment.Exit(exitCode)
        End SyncLock
    End Sub

#End Region

End Module