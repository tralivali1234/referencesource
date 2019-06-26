' Copyright (c) Microsoft Corporation.  All rights reserved.
Option Explicit On
Option Strict Off

Imports System.Security

Namespace Microsoft.VisualBasic.MyServices.Internal

    '''**************************************************************************
    ''' ;SkuSafeHttpContext
    ''' <summary>
    ''' Returns the current HTTPContext or nothing if we are not running in a 
    ''' web context.
    ''' </summary>
    ''' <remarks>
    ''' With the FX dividing into Client and Full skus, we may not always have
    ''' access to the System.Web types.  So we have to test for the presence
    ''' of System.Web.Httpcontext before trying to access it.
    ''' </remarks>
    <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
    Friend Class SkuSafeHttpContext
        Public Shared ReadOnly Property Current() As Object
            Get
                'Return the equivalent of System.Web.HttpContext.Current
                If m_HttpContextCurrent IsNot Nothing Then
                    Return m_HttpContextCurrent.GetValue(Nothing, Nothing)
                Else
                    Return Nothing
                End If
            End Get
        End Property

        '''**************************************************************************
        ''' ;InitContext
        ''' <summary>
        ''' Initialize the field that holds the type that allows us to access
        ''' System.Web.HttpContext
        ''' </summary>
        ''' <remarks>
        ''' The [COR_*] things are substituted by a Perl script launched from
        ''' Microsoft.VisualBasic.Build.vbproj
        ''' </remarks>
        Private Shared Function InitContext() As System.Reflection.PropertyInfo
            Dim HttpContextType As System.Type
            HttpContextType = System.Type.GetType(
"System.Web.HttpContext,System.Web,Version=[COR_BUILD_MAJOR].[COR_BUILD_MINOR].[CLR_OFFICIAL_ASSEMBLY_NUMBER].0,Culture=neutral,PublicKeyToken=B03F5F7F11D50A3A")

            If HttpContextType IsNot Nothing Then
                Return HttpContextType.GetProperty("Current")
            Else
                Return Nothing
            End If
        End Function

        'This class isn't meant to be constructed.
        'Shut FXCOP up by providing a private ctor so the compiler doesn't synth a public one.
        Private Sub New()
        End Sub

        Private Shared m_HttpContextCurrent As System.Reflection.PropertyInfo = InitContext()
    End Class

    '''**************************************************************************
    ''' ;ContextValue
    ''' <summary>
    ''' Stores an object in a context appropriate for the environment we are 
    ''' running in (web/windows)
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <remarks>
    ''' "Thread appropriate" means that if we are running on ASP.Net the object will be stored in the 
    ''' context of the current request (meaning the object is stored per request on the web).  Otherwise, 
    ''' the object is stored per CallContext.  Note that an instance of this class can only be associated
    ''' with the one item to be stored/retrieved at a time.
    ''' </remarks>
    <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
    Public Class ContextValue(Of T)
        Public Sub New()
            m_ContextKey = System.Guid.NewGuid.ToString
        End Sub

        '''**************************************************************************
        ''' ;Value
        ''' <summary>
        ''' Get the object from the correct thread-appropriate location
        ''' </summary>
        ''' <value></value>
        Public Property Value() As T 'No Synclocks required because we are operating upon instance data and the object is not shared across threads
            <SecuritySafeCritical()> _
            Get
                Dim Context As Object = SkuSafeHttpContext.Current()
                If Context IsNot Nothing Then 'we are running on the web
                    Return DirectCast(Context.Items(m_ContextKey), T) 'Note, Context.Items() can return Nothing and that's ok
                Else 'we are running in a DLL
                    Return DirectCast(System.Runtime.Remoting.Messaging.CallContext.GetData(m_ContextKey), T) 'Note, CallContext.GetData() can return Nothing and that's ok
                End If
            End Get
            <SecuritySafeCritical()> _
            Set(ByVal value As T)
                Dim Context As Object = SkuSafeHttpContext.Current()
                If Context IsNot Nothing Then 'we are running on the web
                    Context.Items(m_ContextKey) = value
                Else 'we are running in a DLL
                    System.Runtime.Remoting.Messaging.CallContext.SetData(m_ContextKey, value)
                End If
            End Set
        End Property

        '= PRIVATE ============================================================

        Private ReadOnly m_ContextKey As String 'An item is stored in the dictionary by a guid which this string maintains

    End Class 'ContextValue

End Namespace
