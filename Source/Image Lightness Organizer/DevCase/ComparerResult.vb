
#If Not NETCOREAPP Then

' ***********************************************************************
' Author   : ElektroStudios
' Modified : 06-January-2016
' ***********************************************************************

#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic

#End Region

#Region " Comparer Result "

' ReSharper disable once CheckNamespace

Namespace DevCase.Runtime.TypeComparers

    ''' <summary>
    ''' Specifies a result for <see cref="IComparer.Compare"/> function.
    ''' </summary>
    Public Enum ComparerResult As Integer

        ''' <summary>
        ''' First object precedes second object in the sort order, 
        ''' or first is <see langword="null"/> and second is not <see langword="null"/>.
        ''' </summary>
        LessThan = -1


        ''' <summary>
        ''' First object is equal to second object, 
        ''' or first and second are <see langword="null"/>.
        ''' </summary>
        Equals = 0

        ''' <summary>
        ''' First object follows second object in the sort order, 
        ''' or second is <see langword="null"/> and first is not <see langword="null"/>.
        ''' </summary>
        GreaterThan = 1

    End Enum

End Namespace

#End Region

#End If