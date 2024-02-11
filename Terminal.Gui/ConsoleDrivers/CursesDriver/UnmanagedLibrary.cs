// Copyright 2015 gRPC authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//	 http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#define GUICS
using System.Runtime.InteropServices;

namespace Unix.Terminal;

/// <summary>
///     Represents a dynamically loaded unmanaged library in a (partially) platform independent manner. First, the
///     native library is loaded using dlopen (on Unix systems) or using LoadLibrary (on Windows). dlsym or GetProcAddress
///     are then used to obtain symbol addresses. <c>Marshal.GetDelegateForFunctionPointer</c> transforms the addresses
///     into delegates to native methods. See
///     http://stackoverflow.com/questions/13461989/p-invoke-to-dynamically-loaded-library-on-mono.
/// </summary>
class UnmanagedLibrary
{
    private const string UnityEngineApplicationClassName = "UnityEngine.Application, UnityEngine";
    private const string XamarinAndroidObjectClassName = "Java.Lang.Object, Mono.Android";
    private const string XamarinIOSObjectClassName = "Foundation.NSObject, Xamarin.iOS";
    private static readonly bool IsWindows;
    private static readonly bool IsLinux;
    private static readonly bool Is64Bit;
#if GUICS
    private static readonly bool IsMono;
#else
		static bool IsMono, IsUnity, IsXamarinIOS, IsXamarinAndroid, IsXamarin;
#endif
    private static bool IsNetCore;

    public static bool IsMacOSPlatform { get; }

    [DllImport ("libc")] private extern static int uname (nint buf);

    private static string GetUname ()
    {
        nint buffer = Marshal.AllocHGlobal (8192);

        try
        {
            if (uname (buffer) == 0)
            {
                return Marshal.PtrToStringAnsi (buffer);
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            if (buffer != nint.Zero)
            {
                Marshal.FreeHGlobal (buffer);
            }
        }
    }

    static UnmanagedLibrary ()
    {
        PlatformID platform = Environment.OSVersion.Platform;

        IsMacOSPlatform = platform == PlatformID.Unix && GetUname () == "Darwin";
        IsLinux = platform == PlatformID.Unix && !IsMacOSPlatform;

        IsWindows = (platform == PlatformID.Win32NT)
                    || (platform == PlatformID.Win32S)
                    || (platform == PlatformID.Win32Windows);
        Is64Bit = Marshal.SizeOf (typeof (nint)) == 8;
        IsMono = Type.GetType ("Mono.Runtime") != null;

        if (!IsMono)
        {
            IsNetCore = Type.GetType ("System.MathF") != null;
        }
#if GUICS

        //IsUnity = IsXamarinIOS = IsXamarinAndroid = IsXamarin = false;
#else
			IsUnity = Type.GetType (UnityEngineApplicationClassName) != null;
			IsXamarinIOS = Type.GetType (XamarinIOSObjectClassName) != null;
			IsXamarinAndroid = Type.GetType (XamarinAndroidObjectClassName) != null;
			IsXamarin = IsXamarinIOS || IsXamarinAndroid;
#endif
    }

    // flags for dlopen
    private const int RTLD_LAZY = 1;
    private const int RTLD_GLOBAL = 8;
    public readonly string LibraryPath;

    public nint NativeLibraryHandle { get; }

    //
    // if isFullPath is set to true, the provided array of libraries are full paths
    // and are tested for the file existing, otherwise the file is merely the name
    // of the shared library that we pass to dlopen
    //
    public UnmanagedLibrary (string [] libraryPathAlternatives, bool isFullPath)
    {
        if (isFullPath)
        {
            LibraryPath = FirstValidLibraryPath (libraryPathAlternatives);
            NativeLibraryHandle = PlatformSpecificLoadLibrary (LibraryPath);
        }
        else
        {
            foreach (string lib in libraryPathAlternatives)
            {
                NativeLibraryHandle = PlatformSpecificLoadLibrary (lib);

                if (NativeLibraryHandle != nint.Zero)
                {
                    LibraryPath = lib;

                    break;
                }
            }
        }

        if (NativeLibraryHandle == nint.Zero)
        {
            throw new IOException ($"Error loading native library \"{string.Join (", ", libraryPathAlternatives)}\"");
        }
    }

    /// <summary>Loads symbol in a platform specific way.</summary>
    /// <param name="symbolName"></param>
    /// <returns></returns>
    public nint LoadSymbol (string symbolName)
    {
        if (IsWindows)
        {
            // See http://stackoverflow.com/questions/10473310 for background on this.
            if (Is64Bit)
            {
                return Windows.GetProcAddress (NativeLibraryHandle, symbolName);
            }

            // Yes, we could potentially predict the size... but it's a lot simpler to just try
            // all the candidates. Most functions have a suffix of @0, @4 or @8 so we won't be trying
            // many options - and if it takes a little bit longer to fail if we've really got the wrong
            // library, that's not a big problem. This is only called once per function in the native library.
            symbolName = "_" + symbolName + "@";

            for (var stackSize = 0; stackSize < 128; stackSize += 4)
            {
                nint candidate = Windows.GetProcAddress (NativeLibraryHandle, symbolName + stackSize);

                if (candidate != nint.Zero)
                {
                    return candidate;
                }
            }

            // Fail.
            return nint.Zero;
        }

        if (IsLinux)
        {
            if (IsMono)
            {
                return Mono.dlsym (NativeLibraryHandle, symbolName);
            }

            if (IsNetCore)
            {
                return CoreCLR.dlsym (NativeLibraryHandle, symbolName);
            }

            return Linux.dlsym (NativeLibraryHandle, symbolName);
        }

        if (IsMacOSPlatform)
        {
            return MacOSX.dlsym (NativeLibraryHandle, symbolName);
        }

        throw new InvalidOperationException ("Unsupported platform.");
    }

    public T GetNativeMethodDelegate<T> (string methodName)
        where T : class
    {
        nint ptr = LoadSymbol (methodName);

        if (ptr == nint.Zero)
        {
            throw new MissingMethodException (string.Format ("The native method \"{0}\" does not exist", methodName));
        }

        return Marshal.GetDelegateForFunctionPointer<T> (ptr); // non-generic version is obsolete
    }

    /// <summary>Loads library in a platform specific way.</summary>
    private static nint PlatformSpecificLoadLibrary (string libraryPath)
    {
        if (IsWindows)
        {
            return Windows.LoadLibrary (libraryPath);
        }

        if (IsLinux)
        {
            if (IsMono)
            {
                return Mono.dlopen (libraryPath, RTLD_GLOBAL + RTLD_LAZY);
            }

            if (IsNetCore)
            {
                try
                {
                    return CoreCLR.dlopen (libraryPath, RTLD_GLOBAL + RTLD_LAZY);
                }
                catch (Exception)
                {
                    IsNetCore = false;
                }
            }

            return Linux.dlopen (libraryPath, RTLD_GLOBAL + RTLD_LAZY);
        }

        if (IsMacOSPlatform)
        {
            return MacOSX.dlopen (libraryPath, RTLD_GLOBAL + RTLD_LAZY);
        }

        throw new InvalidOperationException ("Unsupported platform.");
    }

    private static string FirstValidLibraryPath (string [] libraryPathAlternatives)
    {
        foreach (string path in libraryPathAlternatives)
        {
            if (File.Exists (path))
            {
                return path;
            }
        }

        throw new FileNotFoundException (
                                         string.Format (
                                                        "Error loading native library. Not found in any of the possible locations: {0}",
                                                        string.Join (",", libraryPathAlternatives)
                                                       )
                                        );
    }

    private static class Windows
    {
        [DllImport ("kernel32.dll")] internal extern static nint GetProcAddress (nint hModule, string procName);
        [DllImport ("kernel32.dll")] internal extern static nint LoadLibrary (string filename);
    }

    private static class Linux
    {
        [DllImport ("libdl.so")] internal extern static nint dlopen (string filename, int flags);
        [DllImport ("libdl.so")] internal extern static nint dlsym (nint handle, string symbol);
    }

    private static class MacOSX
    {
        [DllImport ("libSystem.dylib")] internal extern static nint dlopen (string filename, int flags);
        [DllImport ("libSystem.dylib")] internal extern static nint dlsym (nint handle, string symbol);
    }

    /// <summary>
    ///     On Linux systems, using using dlopen and dlsym results in DllNotFoundException("libdl.so not found") if
    ///     libc6-dev is not installed. As a workaround, we load symbols for dlopen and dlsym from the current process as on
    ///     Linux Mono sure is linked against these symbols.
    /// </summary>
    private static class Mono
    {
        [DllImport ("__Internal")] internal extern static nint dlopen (string filename, int flags);
        [DllImport ("__Internal")] internal extern static nint dlsym (nint handle, string symbol);
    }

    /// <summary>
    ///     Similarly as for Mono on Linux, we load symbols for dlopen and dlsym from the "libcoreclr.so", to avoid the
    ///     dependency on libc-dev Linux.
    /// </summary>
    private static class CoreCLR
    {
        // Custom resolver to support true single-file apps
        // (those which run directly from bundle; in-memory).
        //	 -1 on Unix means self-referencing binary (libcoreclr.so)
        //	 0 means fallback to CoreCLR's internal resolution
        // Note: meaning of -1 stay the same even for non-single-file form factors.
        static CoreCLR ()
        {
            NativeLibrary.SetDllImportResolver (
                                                typeof (CoreCLR).Assembly,
                                                (libraryName, assembly, searchPath) =>
                                                    libraryName == "libcoreclr.so" ? -1 : nint.Zero
                                               );
        }

        [DllImport ("libcoreclr.so")] internal extern static nint dlopen (string filename, int flags);
        [DllImport ("libcoreclr.so")] internal extern static nint dlsym (nint handle, string symbol);
    }
}
