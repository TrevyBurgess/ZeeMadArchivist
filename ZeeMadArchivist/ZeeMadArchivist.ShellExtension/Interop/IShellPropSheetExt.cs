using System;
using System.Runtime.InteropServices;

namespace CyberFeedForward.TheMadArchivist.ShellExtension.Interop;

/// <summary>
/// Interface implemented by property sheet extensions.
/// </summary>
[ComImport]
[Guid("000214E9-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellPropSheetExt
{
    /// <summary>
    /// Adds a page to the property sheet.
    /// </summary>
    [PreserveSig]
    int AddPages(IntPtr pfnAddPage, IntPtr lParam);

    /// <summary>
    /// Replaces a page in the property sheet. Not implemented for this extension.
    /// </summary>
    [PreserveSig]
    int ReplacePage(uint uPageID, IntPtr pfnReplaceWith, IntPtr lParam);
}
