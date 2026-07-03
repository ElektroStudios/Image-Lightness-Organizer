#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Runtime.InteropServices
Imports System.Security

#End Region

''' <summary>
''' Platform Invocation methods (P/Invoke), access unmanaged code.
''' </summary>
<SuppressUnmanagedCodeSecurity>
Friend Module NativeMethods

#Region " shlwapi.dll "

#If Not NETCOREAPP Then

    <DllImport("shlwapi.dll", SetLastError:=False, CharSet:=CharSet.Unicode, ExactSpelling:=True)>
    Friend Function StrCmpLogicalW(first As String, second As String) As Integer
    End Function
    
#End If

    <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Friend Function GlobalMemoryStatusEx(ByRef refBuffer As MEMORYSTATUSEX) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

#End Region

End Module
