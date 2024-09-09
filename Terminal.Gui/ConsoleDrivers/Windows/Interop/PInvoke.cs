namespace Terminal.Gui.ConsoleDrivers.Windows.Interop;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using Resources;
using static System.Runtime.InteropServices.Marshal;

/// <summary>
///     Windows PInvoke-related proxy and helper methods. The actual PInvoke DllImports are super-secret and only intended to be
///     called from code in this file.
/// </summary>
/// <remarks>Marked unsafe because the out reference will not be initialized unless the operation succeeds.</remarks>
[SupportedOSPlatform ("WINDOWS")]
internal static unsafe class PInvoke
{
    /// <summary>Sets the current size and position of a console screen buffer's window.</summary>
    /// <param name="hConsoleOutput">A <see cref="SafeFileHandle"/> to the console screen buffer.</param>
    /// <param name="bAbsolute">
    ///     If this parameter is <see langword="true"/>, the coordinates specify the new upper-left and lower-right corners of the
    ///     window.<br/>
    ///     If it is <see langword="false"/>, the coordinates are relative to the current window-corner coordinates.
    /// </param>
    /// <param name="lpConsoleWindow">
    ///     A <see langword="readonly"/> reference to a <see cref="SmallRect"/> structure that specifies the new upper-left and
    ///     lower-right corners of the window.
    /// </param>
    /// <returns>
    ///     If the method succeeds, the return value is <see langword="true"/>.<br/>
    ///     If the method fails, the return value is <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The method fails if <paramref name="lpConsoleWindow"/> extends beyond the boundaries of the console screen buffer.<br/>
    ///         This means that the <see cref="SmallRect.Top"/> and <see cref="SmallRect.Left"/> members of
    ///         <paramref name="lpConsoleWindow"/> (or the calculated top and left coordinates, if <paramref name="bAbsolute"/> is
    ///         <see langword="false"/>) cannot be less than zero.<br/>
    ///         Similarly, the <see cref="SmallRect.Bottom"/> and <see cref="SmallRect.Right"/> members (or the calculated bottom and
    ///         right coordinates) cannot be greater than (screen buffer height – 1) and (screen buffer width – 1), respectively.
    ///     </para>
    ///     <para>
    ///         The method also fails if the <see cref="SmallRect.Right"/> member (or calculated right coordinate) is less than or
    ///         equal to the <see cref="SmallRect.Left"/> member (or calculated left coordinate) or if the <see cref="SmallRect.Bottom"/>
    ///         member (or calculated bottom coordinate) is less than or equal to the <see cref="SmallRect.Top"/> member (or calculated
    ///         top coordinate).
    ///     </para>
    ///     <para>
    ///         For consoles with more than one screen buffer, changing the window location for one screen buffer does not affect the
    ///         window locations of the other screen buffers.
    ///     </para>
    ///     <para>
    ///         To determine the current size and position of a screen buffer's window, use the <see cref="GetConsoleScreenBufferInfo"/>
    ///         method.<br/>
    ///         This method also returns the maximum size of the window, given the current screen buffer size, the current font size, and
    ///         the screen size.<br/>
    ///         The <see cref="GetLargestConsoleWindowSize"/> method returns the maximum window size given the current font and screen
    ///         sizes, but it does not consider the size of the console screen buffer.
    ///     </para>
    ///     <para>
    ///         SetConsoleWindowInfo can be used to scroll the contents of the console screen buffer by shifting the position of the
    ///         window rectangle without changing its size.
    ///     </para>
    /// </remarks>
    [SkipLocalsInit]
    public static bool SetConsoleWindowInfo (SafeFileHandle hConsoleOutput, bool bAbsolute, in SmallRect lpConsoleWindow)
    {
        // Semi-redundant with the attribute, but same IL in release build and easier to debug in debug builds.
        Unsafe.SkipInit (out nint consoleHandle);
        Unsafe.SkipInit (out BOOL result);
        Unsafe.SkipInit (out BOOL absolute);

        ValidateSafeFileHandle (hConsoleOutput);

        SafeHandleMarshaller<SafeFileHandle>.ManagedToUnmanagedIn consoleHandleNativeMarshaller = new ();

        try
        {
            consoleHandleNativeMarshaller.FromManaged (hConsoleOutput);

            fixed (SmallRect* lpConsoleWindowNative = &lpConsoleWindow)
            {
                SetLastSystemError (0);

                consoleHandle = consoleHandleNativeMarshaller.ToUnmanaged ();
                absolute = new (bAbsolute);

                result = UnsafeNativeMethods.SetConsoleWindowInfo (consoleHandle, absolute, lpConsoleWindowNative);

                if (result)
                {
                    return true;
                }

                SetLastPInvokeError (GetLastSystemError ());

                return false;
            }
        }
        finally
        {
            consoleHandleNativeMarshaller.Free ();
        }
    }

    /// <summary>Retrieves the current input mode of a console's input buffer or the current output mode of a console screen buffer.</summary>
    /// <param name="hConsoleHandle">
    ///     A handle to the console input buffer or the console screen buffer.
    /// </param>
    /// <param name="lpMode">
    ///     An out reference that receives the current mode of the specified buffer.
    /// </param>
    /// <returns>
    ///     If the method succeeds, the return value is <see langword="true"/>.<br/>
    ///     If the method fails, the return value is <see langword="false"/>.
    /// </returns>
    /// <seealso href="https://learn.microsoft.com/windows/console/getconsolemode">Read more on Microsoft Learn</seealso>
    [SkipLocalsInit]
    internal static bool GetConsoleMode (SafeFileHandle hConsoleHandle, out CONSOLE_MODE lpMode)
    {
        Unsafe.SkipInit (out lpMode);
        ValidateSafeFileHandle (hConsoleHandle);

        SafeHandleMarshaller<SafeFileHandle>.ManagedToUnmanagedIn consoleHandleNativeMarshaller = new ();

        try
        {
            fixed (CONSOLE_MODE* consoleModeValuePointer = &lpMode)
            {
                consoleHandleNativeMarshaller.FromManaged (hConsoleHandle);

                if (UnsafeNativeMethods.GetConsoleMode (consoleHandleNativeMarshaller.ToUnmanaged (), consoleModeValuePointer))
                {
                    return true;
                }

                // If we made it here, something went wrong.
                SetLastPInvokeError (GetLastSystemError ());

                return false;
            }
        }
        finally
        {
            consoleHandleNativeMarshaller.Free ();
        }
    }

    /// <summary>
    ///     Sets the <see cref="FileStream"/> to be the currently active console screen buffer.
    /// </summary>
    /// <param name="outputStream">The <see cref="FileStream"/> holding the console output buffer handle.</param>
    /// <returns>
    ///     If the method succeeds, the return value is <see langword="true"/>.<br/>
    ///     If the method fails, the return value is <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         A console can have multiple screen buffers.<br/>
    ///         <see cref="SetConsoleActiveScreenBuffer"/> determines which one is displayed.
    ///     </para>
    ///     <para>
    ///         You can write to an inactive screen buffer and then use <see cref="SetConsoleActiveScreenBuffer"/> to display the
    ///         buffer's contents.
    ///     </para>
    ///     <para>
    ///         This API is not recommended, but it does have an approximate virtual terminal equivalent in the alternate screen buffer
    ///         sequence.<br/>
    ///         See
    ///         <see href="https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences#alternate-screen-buffer"/>
    ///         for details on the virtual terminal sequence alternative.
    ///     </para>
    ///     <para>
    ///         Setting the alternate screen buffer can provide an application with a separate, isolated space for drawing over the
    ///         course of its session runtime while preserving the content that was displayed by the application's invoker.<br/>
    ///         This maintains that drawing information for simple restoration on process exit.
    ///     </para>
    /// </remarks>
    [SkipLocalsInit]
    internal static bool SetConsoleActiveScreenBuffer (FileStream outputStream)
    {
        Unsafe.SkipInit (out nint consoleHandle);
        Unsafe.SkipInit (out BOOL result);

        ValidateSafeFileHandle (outputStream.SafeFileHandle);

        SafeHandleMarshaller<SafeFileHandle>.ManagedToUnmanagedIn consoleHandleNativeMarshaller = new ();

        try
        {
            SetLastSystemError (0);

            consoleHandleNativeMarshaller.FromManaged (outputStream.SafeFileHandle);
            consoleHandle = consoleHandleNativeMarshaller.ToUnmanaged ();

            result = UnsafeNativeMethods.SetConsoleActiveScreenBuffer (consoleHandle);

            if (result)
            {
                return true;
            }

            SetLastPInvokeError (GetLastSystemError ());

            return false;
        }
        finally
        {
            // CleanupCallerAllocated - Perform cleanup of caller allocated resources.
            consoleHandleNativeMarshaller.Free ();
        }
    }

    /// <summary>Sets the input mode of a console's input buffer or the output mode of a console screen buffer.</summary>
    /// <param name="hConsoleHandle">
    ///     A handle to the console input buffer or a console screen buffer. The handle must have the **GENERIC\_READ** access right.
    /// </param>
    /// <param name="dwMode">The input or output mode to be set.</param>
    /// <returns>
    ///     If the function succeeds, the return value is nonzero.<br/>
    ///     If the function fails, the return value is zero.<br/>
    ///     To get extended error information, call <see cref="Marshal.GetLastPInvokeError"/>.
    /// </returns>
    /// <remarks>
    ///     To determine the current mode of a console input buffer or a screen buffer, use the
    ///     <see cref="GetConsoleMode(SafeFileHandle,out CONSOLE_MODE)"/> method.
    /// </remarks>
    /// <remarks>This implementation behaves as if <see cref="LibraryImportAttribute.SetLastError"/> were set to <see langword="true"/>.</remarks>
    /// <exception cref="ArgumentNullException">If the supplied <paramref name="hConsoleHandle"/> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     If the supplied <paramref name="hConsoleHandle"/> is invalid or the resource it points to has
    ///     been closed.
    /// </exception>
    [SkipLocalsInit]
    internal static bool SetConsoleMode (SafeFileHandle hConsoleHandle, ref readonly CONSOLE_MODE dwMode)
    {
        ValidateSafeFileHandle (hConsoleHandle);

        SafeHandleMarshaller<SafeFileHandle>.ManagedToUnmanagedIn consoleHandleNativeMarshaller = new ();

        try
        {
            consoleHandleNativeMarshaller.FromManaged (hConsoleHandle);

            // Clear this in case it is non-zero from something else.
            SetLastSystemError (0);

            if (UnsafeNativeMethods.SetConsoleMode (consoleHandleNativeMarshaller.ToUnmanaged (), dwMode))
            {
                return true;
            }

            SetLastPInvokeError (GetLastSystemError ());

            return false;
        }
        finally
        {
            consoleHandleNativeMarshaller.Free ();
        }
    }

    /// <exception cref="ArgumentException">If the handle is invalid or closed.</exception>
    /// <exception cref="ArgumentNullException">If the handle is null.</exception>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    private static void ValidateSafeFileHandle (SafeFileHandle hConsoleHandle)
    {
        ArgumentNullException.ThrowIfNull (hConsoleHandle, nameof (hConsoleHandle));
        ThrowHelpers.ThrowExceptionIfTrue (hConsoleHandle.IsInvalid, ThrowHandleInvalidArgumentException);
        ThrowHelpers.ThrowExceptionIfTrue (hConsoleHandle.IsClosed, ThrowHandleClosedArgumentException);

        return;

        [DoesNotReturn]
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        static void ThrowHandleInvalidArgumentException ()
        {
            throw new ArgumentException (
                                         Strings.Win32_PInvoke_SetConsoleMode_ArgumentException,
                                         nameof (hConsoleHandle),
                                         new IOException (Strings.Win32_PInvoke_SetConsoleMode_HandleInvalid));
        }

        [DoesNotReturn]
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        static void ThrowHandleClosedArgumentException ()
        {
            throw new ArgumentException (
                                         Strings.Win32_PInvoke_SetConsoleMode_ArgumentException,
                                         nameof (hConsoleHandle),
                                         new IOException (Strings.Win32_PInvoke_SetConsoleMode_HandleClosed));
        }
    }
}

file static class ThrowHelpers
{
    /// <summary>
    ///     Throws an exception created by the provided delegate, if the provided <paramref name="condition"/> is <see langword="true"/>.
    /// </summary>
    /// <param name="condition">The condition upon which, if true, the exception will be thrown. If false, method simply returns.</param>
    /// <param name="thrower">A delegate that creates the exception to throw.</param>
    /// <remarks>
    ///     Supplying the exception via a delegate enables custom definition of exceptions at point of use while not constructing the
    ///     exception til throw time and without requiring this method to know anything about it, which preserves the behavior of the
    ///     <see langword="if"/> statements this otherwise replaces.
    /// </remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    internal static void ThrowExceptionIfTrue ([DoesNotReturnIf (true)] bool condition, Action thrower)
    {
        if (condition)
        {
            thrower ();
        }
    }
}

[SupportedOSPlatform ("windows")]
file static unsafe class UnsafeNativeMethods
{
    [DllImport ("kernel32", EntryPoint = "GetConsoleMode", ExactSpelling = true)]
    internal static extern BOOL GetConsoleMode (nint hConsoleHandle, CONSOLE_MODE* lpMode);

    [DllImport ("kernel32", EntryPoint = "SetConsoleActiveScreenBuffer", ExactSpelling = true)]
    internal static extern BOOL SetConsoleActiveScreenBuffer (nint hConsoleOutput);

    [DllImport ("kernel32", EntryPoint = "SetConsoleMode", ExactSpelling = true)]
    internal static extern BOOL SetConsoleMode (nint hConsoleHandle, CONSOLE_MODE dwMode);

    [DllImport ("kernel32", EntryPoint = "SetConsoleWindowInfo", ExactSpelling = true)]
    internal static extern BOOL SetConsoleWindowInfo (nint hConsoleOutput, BOOL bAbsolute, SmallRect* lpConsoleWindow);
}
