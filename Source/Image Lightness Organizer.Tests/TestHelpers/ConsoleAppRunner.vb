#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO
Imports System.Text

#End Region

#Region " ConsoleAppRunner "

''' <summary>
''' Launches the Image Lightness Organizer console application as a separate process and captures its result.
''' </summary>
Friend Module ConsoleAppRunner

#Region " Constants "

    ''' <summary>
    ''' The assembly name of the application under test.
    ''' </summary>
    Private Const AppAssemblyName As String = "Image_Lightness_Organizer"

#End Region

#Region " Fields "

    ''' <summary>
    ''' The maximum time to wait for the application to exit.
    ''' </summary>
    Private ReadOnly ProcessTimeout As TimeSpan = TimeSpan.FromMinutes(2)

#End Region

#Region " Nested Types "

    ''' <summary>
    ''' Represents the result of an application run.
    ''' </summary>
    Friend NotInheritable Class AppRunResult

        ''' <summary>
        ''' Gets the process exit code.
        ''' </summary>
        Friend ReadOnly Property ExitCode As Integer

        ''' <summary>
        ''' Gets the captured standard output.
        ''' </summary>
        Friend ReadOnly Property StandardOutput As String

        ''' <summary>
        ''' Gets the captured standard error.
        ''' </summary>
        Friend ReadOnly Property StandardError As String

        ''' <summary>
        ''' Initializes a new instance of the <see cref="AppRunResult"/> class.
        ''' </summary>
        '''
        ''' <param name="exitCode">
        ''' The process exit code.
        ''' </param>
        '''
        ''' <param name="standardOutput">
        ''' The captured standard output.
        ''' </param>
        '''
        ''' <param name="standardError">
        ''' The captured standard error.
        ''' </param>
        Friend Sub New(exitCode As Integer, standardOutput As String, standardError As String)
            Me.ExitCode = exitCode
            Me.StandardOutput = standardOutput
            Me.StandardError = standardError
        End Sub

        ''' <summary>
        ''' Returns a diagnostic description of this result, used in assertion messages.
        ''' </summary>
        '''
        ''' <returns>
        ''' The diagnostic description.
        ''' </returns>
        Public Overrides Function ToString() As String
            Return $"Exit code: {Me.ExitCode}{Environment.NewLine}--- STDOUT ---{Environment.NewLine}{Me.StandardOutput}{Environment.NewLine}--- STDERR ---{Environment.NewLine}{Me.StandardError}"
        End Function

    End Class

#End Region

#Region " Methods "

    ''' <summary>
    ''' Runs the application with the specified command-line arguments, and waits for it to exit.
    ''' </summary>
    '''
    ''' <param name="arguments">
    ''' The command-line arguments.
    ''' </param>
    '''
    ''' <returns>
    ''' The run result.
    ''' </returns>
    '''
    ''' <exception cref="TimeoutException">
    ''' Thrown when the application does not exit within the allowed time.
    ''' </exception>
    Friend Function Run(ParamArray arguments As String()) As AppRunResult

        ArgumentNullException.ThrowIfNull(arguments)

        Dim startInfo As ProcessStartInfo = ConsoleAppRunner.CreateStartInfo()
        For Each argument As String In arguments
            startInfo.ArgumentList.Add(argument)
        Next

        Using proc As New Process()
            proc.StartInfo = startInfo

            Dim stdOut As New StringBuilder()
            Dim stdErr As New StringBuilder()
            AddHandler proc.OutputDataReceived, Sub(sender As Object, e As DataReceivedEventArgs)
                                                    If e.Data IsNot Nothing Then
                                                        SyncLock stdOut
                                                            stdOut.AppendLine(e.Data)
                                                        End SyncLock
                                                    End If
                                                End Sub
            AddHandler proc.ErrorDataReceived, Sub(sender As Object, e As DataReceivedEventArgs)
                                                   If e.Data IsNot Nothing Then
                                                       SyncLock stdErr
                                                           stdErr.AppendLine(e.Data)
                                                       End SyncLock
                                                   End If
                                               End Sub

            If Not proc.Start() Then
                Throw New InvalidOperationException($"Failed to start process: {startInfo.FileName}")
            End If

            ' Standard input is redirected and closed so the application never blocks waiting for a key.
            proc.StandardInput.Close()
            proc.BeginOutputReadLine()
            proc.BeginErrorReadLine()

            If Not proc.WaitForExit(ConsoleAppRunner.ProcessTimeout) Then
                proc.Kill(entireProcessTree:=True)
                Throw New TimeoutException($"The application did not exit within {ConsoleAppRunner.ProcessTimeout}.{Environment.NewLine}{stdOut}")
            End If

            ' Ensures that all asynchronous output events have been processed.
            proc.WaitForExit()

            Return New AppRunResult(proc.ExitCode, stdOut.ToString(), stdErr.ToString())
        End Using
    End Function

    ''' <summary>
    ''' Creates the <see cref="ProcessStartInfo"/> to launch the application under test.
    ''' </summary>
    '''
    ''' <returns>
    ''' The process start information.
    ''' </returns>
    '''
    ''' <remarks>
    ''' The application executable (apphost) is used when available in the test output directory;
    ''' otherwise, the application assembly is launched through the <c>dotnet</c> host.
    ''' </remarks>
    '''
    ''' <exception cref="FileNotFoundException">
    ''' Thrown when neither the application executable nor its assembly can be found.
    ''' </exception>
    Private Function CreateStartInfo() As ProcessStartInfo

        Dim baseDirectory As String = AppContext.BaseDirectory
        Dim exePath As String = Path.Combine(baseDirectory, $"{ConsoleAppRunner.AppAssemblyName}.exe")
        Dim dllPath As String = Path.Combine(baseDirectory, $"{ConsoleAppRunner.AppAssemblyName}.dll")
        Dim runtimeConfigPath As String = Path.Combine(baseDirectory, $"{ConsoleAppRunner.AppAssemblyName}.runtimeconfig.json")

        Dim startInfo As ProcessStartInfo
        If OperatingSystem.IsWindows() AndAlso File.Exists(exePath) Then
            startInfo = New ProcessStartInfo(exePath)

        ElseIf File.Exists(dllPath) AndAlso File.Exists(runtimeConfigPath) Then
            Dim dotnetHostPath As String = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")
            If String.IsNullOrWhiteSpace(dotnetHostPath) Then
                dotnetHostPath = "dotnet"
            End If
            startInfo = New ProcessStartInfo(dotnetHostPath)
            startInfo.ArgumentList.Add("exec")
            startInfo.ArgumentList.Add(dllPath)

        Else
            Throw New FileNotFoundException($"The application under test was not found in the test output directory: {baseDirectory}", exePath)
        End If

        startInfo.UseShellExecute = False
        startInfo.CreateNoWindow = True
        startInfo.RedirectStandardInput = True
        startInfo.RedirectStandardOutput = True
        startInfo.RedirectStandardError = True
        startInfo.StandardOutputEncoding = New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False)
        startInfo.StandardErrorEncoding = New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False)
        startInfo.WorkingDirectory = baseDirectory
        Return startInfo
    End Function

#End Region

End Module

#End Region
