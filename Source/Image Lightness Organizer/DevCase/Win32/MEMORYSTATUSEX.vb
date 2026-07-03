
Imports System.Runtime.InteropServices

<StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
Friend Structure MEMORYSTATUSEX
    Public dwLength As UInteger
    Public dwMemoryLoad As UInteger
    Public ullTotalPhys As ULong
    Public ullAvailPhys As ULong
    Public ullTotalPageFile As ULong
    Public ullAvailPageFile As ULong
    Public ullTotalVirtual As ULong
    Public ullAvailVirtual As ULong
    Public ullAvailExtendedVirtual As ULong
End Structure
