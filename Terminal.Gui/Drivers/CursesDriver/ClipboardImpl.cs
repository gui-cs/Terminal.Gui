using System.Runtime.InteropServices;
using Unix.Terminal;

namespace Terminal.Gui.Drivers;

/// <summary>A clipboard implementation for Linux. This implementation uses the xclip command to access the clipboard.</summary>
/// <remarks>If xclip is not installed, this implementation will not work.</remarks>
internal class CursesClipboard : ClipboardBase
{
    private string _xclipPath = string.Empty;
    public CursesClipboard () { IsSupported = CheckSupport (); }
    public override bool IsSupported { get; }

    protected override string GetClipboardDataImpl ()
    {
        string tempFileName = Path.GetTempFileName ();
        var xclipargs = "-selection clipboard -o";

        try
        {
            (int exitCode, string result) =
                ClipboardProcessRunner.Bash ($"{_xclipPath} {xclipargs} > {tempFileName}", waitForOutput: false);

            if (exitCode == 0)
            {
                if (Application.Driver is CursesDriver)
                {
                    Curses.raw ();
                    Curses.noecho ();
                }

                return File.ReadAllText (tempFileName);
            }
        }
        catch (Exception e)
        {
            throw new NotSupportedException ($"\"{_xclipPath} {xclipargs}\" failed.", e);
        }
        finally
        {
            File.Delete (tempFileName);
        }

        return string.Empty;
    }

    protected override void SetClipboardDataImpl (string text)
    {
        var xclipargs = "-selection clipboard -i";

        try
        {
            (int exitCode, _) = ClipboardProcessRunner.Bash ($"{_xclipPath} {xclipargs}", text);

            if (exitCode == 0 && Application.Driver is CursesDriver)
            {
                Curses.raw ();
                Curses.noecho ();
            }
        }
        catch (Exception e)
        {
            throw new NotSupportedException ($"\"{_xclipPath} {xclipargs} < {text}\" failed", e);
        }
    }

    private bool CheckSupport ()
    {
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
        try
        {
            (int exitCode, string result) = ClipboardProcessRunner.Bash ("which xclip", waitForOutput: true);

            if (exitCode == 0 && result.FileExists ())
            {
                _xclipPath = result;

                return true;
            }
        }
        catch (Exception)
        {
            // Permissions issue.
        }
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
        return false;
    }
}

/// <summary>
///     A clipboard implementation for MacOSX. This implementation uses the Mac clipboard API (via P/Invoke) to
///     copy/paste. The existence of the Mac pbcopy and pbpaste commands is used to determine if copy/paste is supported.
/// </summary>
internal class MacOSXClipboard : ClipboardBase
{
    private readonly nint _allocRegister = sel_registerName ("alloc");
    private readonly nint _clearContentsRegister = sel_registerName ("clearContents");
    private readonly nint _generalPasteboard;
    private readonly nint _generalPasteboardRegister = sel_registerName ("generalPasteboard");
    private readonly nint _initWithUtf8Register = sel_registerName ("initWithUTF8String:");
    private readonly nint _nsPasteboard = objc_getClass ("NSPasteboard");
    private readonly nint _nsString = objc_getClass ("NSString");
    private readonly nint _nsStringPboardType;
    private readonly nint _setStringRegister = sel_registerName ("setString:forType:");
    private readonly nint _stringForTypeRegister = sel_registerName ("stringForType:");
    private readonly nint _utf8Register = sel_registerName ("UTF8String");
    private readonly nint _utfTextType;

    public MacOSXClipboard ()
    {
        _utfTextType = objc_msgSend (
                                     objc_msgSend (_nsString, _allocRegister),
                                     _initWithUtf8Register,
                                     "public.utf8-plain-text"
                                    );

        _nsStringPboardType = objc_msgSend (
                                            objc_msgSend (_nsString, _allocRegister),
                                            _initWithUtf8Register,
                                            "NSStringPboardType"
                                           );
        _generalPasteboard = objc_msgSend (_nsPasteboard, _generalPasteboardRegister);
        IsSupported = CheckSupport ();
    }

    public override bool IsSupported { get; }

    protected override string GetClipboardDataImpl ()
    {
        nint ptr = objc_msgSend (_generalPasteboard, _stringForTypeRegister, _nsStringPboardType);
        nint charArray = objc_msgSend (ptr, _utf8Register);

        return Marshal.PtrToStringAnsi (charArray);
    }

    protected override void SetClipboardDataImpl (string text)
    {
        nint str = default;

        try
        {
            str = objc_msgSend (objc_msgSend (_nsString, _allocRegister), _initWithUtf8Register, text);
            objc_msgSend (_generalPasteboard, _clearContentsRegister);
            objc_msgSend (_generalPasteboard, _setStringRegister, str, _utfTextType);
        }
        finally
        {
            if (str != default (nint))
            {
                objc_msgSend (str, sel_registerName ("release"));
            }
        }
    }

    private bool CheckSupport ()
    {
        (int exitCode, string result) = ClipboardProcessRunner.Bash ("which pbcopy", waitForOutput: true);

        if (exitCode != 0 || !result.FileExists ())
        {
            return false;
        }

        (exitCode, result) = ClipboardProcessRunner.Bash ("which pbpaste", waitForOutput: true);

        return exitCode == 0 && result.FileExists ();
    }

    [DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern nint objc_getClass (string className);

    [DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern nint objc_msgSend (nint receiver, nint selector);

    [DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern nint objc_msgSend (nint receiver, nint selector, string arg1);

    [DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern nint objc_msgSend (nint receiver, nint selector, nint arg1);

    [DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern nint objc_msgSend (nint receiver, nint selector, nint arg1, nint arg2);

    [DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern nint sel_registerName (string selectorName);
}

/// <summary>
///     A clipboard implementation for Linux, when running under WSL. This implementation uses the Windows clipboard
///     to store the data, and uses Windows' powershell.exe (launched via WSL interop services) to set/get the Windows
///     clipboard.
/// </summary>
internal class WSLClipboard : ClipboardBase
{
    private static string _powershellPath = string.Empty;
    public override bool IsSupported => CheckSupport ();

    protected override string GetClipboardDataImpl ()
    {
        if (!IsSupported)
        {
            return string.Empty;
        }

        (int exitCode, string output) =
            ClipboardProcessRunner.Process (_powershellPath, "-noprofile -command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; Get-Clipboard\"");

        if (exitCode == 0)
        {
            if (Application.Driver is CursesDriver)
            {
                Curses.raw ();
                Curses.noecho ();
            }

            if (output.EndsWith ("\r\n"))
            {
                output = output.Substring (0, output.Length - 2);
            }

            return output;
        }

        return string.Empty;
    }

    protected override void SetClipboardDataImpl (string text)
    {
        if (!IsSupported)
        {
            return;
        }

        (int exitCode, string output) = ClipboardProcessRunner.Process (
                                                                        _powershellPath,
                                                                        $"-noprofile -command \"Set-Clipboard -Value \\\"{text}\\\"\""
                                                                       );

        if (exitCode == 0)
        {
            if (Application.Driver is CursesDriver)
            {
                Curses.raw ();
                Curses.noecho ();
            }
        }
    }

    private bool CheckSupport ()
    {
        if (string.IsNullOrEmpty (_powershellPath))
        {
            // Specify pwsh.exe (not pwsh) to ensure we get the Windows version (invoked via WSL)
            (int exitCode, string result) = ClipboardProcessRunner.Bash ("which pwsh.exe", waitForOutput: true);

            if (exitCode > 0)
            {
                (exitCode, result) = ClipboardProcessRunner.Bash ("which powershell.exe", waitForOutput: true);
            }

            if (exitCode == 0)
            {
                _powershellPath = result;
            }
        }

        return !string.IsNullOrEmpty (_powershellPath);
    }
}
