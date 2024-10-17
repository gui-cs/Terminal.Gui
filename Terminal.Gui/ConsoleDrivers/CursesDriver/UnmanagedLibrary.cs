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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Unix.Terminal;

/// <summary>
///     Represents a dynamically loaded unmanaged library in a (partially) platform independent manner. First, the
///     native library is loaded using dlopen (on Unix systems) or using LoadLibrary (on Windows). dlsym or GetProcAddress
///     are then used to obtain symbol addresses. <c>Marshal.GetDelegateForFunctionPointer</c> transforms the addresses
///     into delegates to native methods. See
///     http://stackoverflow.com/questions/13461989/p-invoke-to-dynamically-loaded-library-on-mono.
/// </summary>
internal class UnmanagedLibrary
{
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
            foreach (string path in libraryPathAlternatives)
            {
                if (File.Exists (path))
                {
                    LibraryPath = path;
                    break;
                }
            }

            if (LibraryPath is null)
                throw new FileNotFoundException ($"Error loading native library. Not found in any of the possible locations: {string.Join (",", libraryPathAlternatives)}");

            NativeLibraryHandle = NativeLibrary.Load (LibraryPath);
        }
        else
        {
            foreach (string lib in libraryPathAlternatives)
            {
                NativeLibraryHandle = NativeLibrary.Load (lib);
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
        return NativeLibrary.GetExport(NativeLibraryHandle, symbolName);
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
}
