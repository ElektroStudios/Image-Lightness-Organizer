
#If Not NETCOREAPP Then

#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Diagnostics

#End Region

#Region " String Natural Comparer "

' ReSharper disable once CheckNamespace

Namespace DevCase.Runtime.TypeComparers

    ''' <summary>
    ''' Performs a natural sort order string comparison between two <see cref="String"/> objects.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para></para>
    ''' For more information, see: 
    ''' <see href="https://en.wikipedia.org/wiki/Natural_sort_order"/>
    ''' <para></para>
    ''' <seealso cref="IComparer(Of String)"/>
    ''' </remarks>
    Public NotInheritable Class StringNaturalComparer : Implements IComparer(Of String)

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="StringNaturalComparer"/> class.
        ''' </summary>
        <DebuggerNonUserCode>
        Public Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <summary>
        ''' Compares two <see cref="String"/> objects using natural sort order, 
        ''' and returns a value indicating whether one is less than, 
        ''' equal to, or greater than the other.
        ''' </summary>
        ''' 
        ''' <remarks>
        ''' For more information, see: 
        ''' <see href="https://en.wikipedia.org/wiki/Natural_sort_order"/>
        ''' </remarks>
        '''
        ''' <param name="first">
        ''' The first <see cref="String"/> object to compare.
        ''' </param>
        ''' 
        ''' <param name="second">
        ''' The second <see cref="String"/> object to compare.
        ''' </param>
        '''
        ''' <returns>
        ''' <list type="bullet">
        '''   <item>
        '''     <term><c>-1</c> (<see cref="ComparerResult.LessThan"/>)</term>
        '''     <description>
        '''       <paramref name="first"/> precedes <paramref name="second"/> in the sort order, 
        '''       or <paramref name="first"/> is <see langword="null"/> and <paramref name="second"/> is not <see langword="null"/>.
        '''       <para></para>
        '''     </description>
        '''   </item>
        '''   
        '''   <item> 
        '''     <term><c>0</c> (<see cref="ComparerResult.Equals"/>)</term>
        '''     <description>
        '''       <paramref name="first"/> is equal to <paramref name="second"/>,
        '''       or <paramref name="first"/> and <paramref name="second"/> are <see langword="null"/>.
        '''       <para></para>
        '''     </description>
        '''   </item>
        '''   
        '''   <item>
        '''     <term><c>1</c> (<see cref="ComparerResult.GreaterThan"/>)</term>
        '''     <description>
        '''       <paramref name="first"/> follows <paramref name="second"/> in the sort order, 
        '''       or <paramref name="second"/> is <see langword="null"/> and <paramref name="first"/> is not <see langword="null"/>.
        '''       <para></para>
        '''     </description>
        '''   </item>
        ''' </list>
        ''' </returns>
        <DebuggerStepThrough>
        Public Function Compare(first As String, second As String) As Integer Implements IComparer(Of String).Compare

            If (first Is Nothing) AndAlso (second Is Nothing) Then
                Return ComparerResult.Equals

            ElseIf (first Is Nothing) AndAlso (second IsNot Nothing) Then
                Return ComparerResult.LessThan

            ElseIf (first IsNot Nothing) AndAlso (second Is Nothing) Then
                Return ComparerResult.GreaterThan

            End If

            Return NativeMethods.StrCmpLogicalW(first, second)
        End Function

#End Region

    End Class

End Namespace

#End Region

#End If
