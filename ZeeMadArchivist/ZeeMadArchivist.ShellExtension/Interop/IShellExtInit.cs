using System;
using System.Runtime.InteropServices;

namespace CyberFeedForward.TheMadArchivist.ShellExtension.Interop;

/// <summary>
/// Shell extension initialization interface.
/// </summary>
[ComImport]
[Guid("000214E8-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellExtInit
{
    /// <summary>
    /// Initializes the extension with the selected items.
    /// </summary>
    [PreserveSig]
    int Initialize(IntPtr pidlFolder, IntPtr lpdobj, uint hKeyProgID);
}
