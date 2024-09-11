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
    /// <summary>Retrieves the current input mode of a console's input buffer or the current output mode of a console screen buffer.</summary>
    /// <param name="hConsoleHandle">
    ///     A handle to the console input buffer or the console screen buffer.
    /// </param>
    /// <param name="lpMode">
    ///     An out reference that receives the current mode of the specified buffer.
    /// </param>
    /// <returns>
    ///     If the function succeeds, the return value is true. If the function fails, the return value is false. To get extended
    ///     error information, call GetLastError.
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

    [DllImport ("kernel32", EntryPoint = "SetConsoleMode", ExactSpelling = true)]
    internal static extern BOOL SetConsoleMode (nint hConsoleHandle, CONSOLE_MODE dwMode);
}
