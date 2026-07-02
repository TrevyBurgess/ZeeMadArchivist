using CyberFeedForward.TheMadArchivist.ShellExtension.Interop;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using static CyberFeedForward.TheMadArchivist.ShellExtension.Interop.Win32;

namespace CyberFeedForward.TheMadArchivist.ShellExtension;

/// <summary>
/// Shell property sheet extension that adds a "Tags" tab to the properties dialog
/// of any file or folder.
/// </summary>
[ComVisible(true)]
[Guid("F4A9C6E2-7B5D-4B2E-9F1C-8D3E2A6B5C4D")]
[ClassInterface(ClassInterfaceType.None)]
[ProgId("CyberFeedForward.TheMadArchivist.ShellExtension.TagsPropertySheet")]
public class TagsPropertySheet : IShellExtInit, IShellPropSheetExt
{
    // Keep the delegate alive so the GC doesn't collect it while unmanaged code uses it.
    private static readonly DlgProc _dlgProc = DlgProc;

    private object? _dataObject;
    private IReadOnlyList<string> _filePaths = [];

    int IShellExtInit.Initialize(IntPtr pidlFolder, IntPtr lpdobj, uint hKeyProgID)
    {
        if (lpdobj == IntPtr.Zero)
        {
            return 0;
        }

        try
        {
            _dataObject = Marshal.GetObjectForIUnknown(lpdobj);
            _filePaths = DataObjectHelper.GetFilePaths(_dataObject);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"TagsPropertySheet.Initialize failed: {ex}");
        }

        return 0;
    }

    int IShellPropSheetExt.AddPages(IntPtr pfnAddPage, IntPtr lParam)
    {
        if (pfnAddPage == IntPtr.Zero)
        {
            return unchecked((int)0x80004003); // E_POINTER
        }

        try
        {
            var page = new TagsPropertyPage(_filePaths);
            var handle = GCHandle.Alloc(page, GCHandleType.Normal);

            var templatePointer = Marshal.AllocHGlobal(BuildDialogTemplate().Length);
            Marshal.Copy(BuildDialogTemplate(), 0, templatePointer, BuildDialogTemplate().Length);

            var titleBytes = System.Text.Encoding.Unicode.GetBytes("Tags\0");
            var titlePointer = Marshal.AllocHGlobal(titleBytes.Length);
            Marshal.Copy(titleBytes, 0, titlePointer, titleBytes.Length);

            page.DialogTemplatePointer = templatePointer;
            page.TitlePointer = titlePointer;

            var psp = new PROPSHEETPAGE
            {
                dwSize = (uint)Marshal.SizeOf(typeof(PROPSHEETPAGE)),
                dwFlags = PSP_USETITLE | PSP_DLGINDIRECT,
                hInstance = IntPtr.Zero,
                pResource = templatePointer,
                hIcon = IntPtr.Zero,
                pszTitle = titlePointer,
                pfnDlgProc = Marshal.GetFunctionPointerForDelegate(_dlgProc),
                lParam = GCHandle.ToIntPtr(handle),
                pfnCallback = IntPtr.Zero,
                pcRefParent = IntPtr.Zero,
            };

            var hPage = CreatePropertySheetPage(ref psp);
            if (hPage == IntPtr.Zero)
            {
                handle.Free();
                Marshal.FreeHGlobal(templatePointer);
                Marshal.FreeHGlobal(titlePointer);
                return unchecked((int)0x8007000E); // E_OUTOFMEMORY
            }

            var addPage = Marshal.GetDelegateForFunctionPointer<AddPropSheetPageProc>(pfnAddPage);
            if (!addPage(hPage, lParam))
            {
                DestroyPropertySheetPage(hPage);
                handle.Free();
                Marshal.FreeHGlobal(templatePointer);
                Marshal.FreeHGlobal(titlePointer);
                return unchecked((int)0x8007000E); // E_OUTOFMEMORY
            }

            // Memory is freed in WM_DESTROY.
            return 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"TagsPropertySheet.AddPages failed: {ex}");
            return unchecked((int)0x80004005); // E_FAIL
        }
    }

    int IShellPropSheetExt.ReplacePage(uint uPageID, IntPtr pfnReplaceWith, IntPtr lParam)
    {
        return unchecked((int)0x80004001); // E_NOTIMPL
    }

    /// <summary>
    /// Creates an in-memory dialog template for the property sheet page.
    /// </summary>
    private static byte[] BuildDialogTemplate()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        var template = new DLGTEMPLATE
        {
            style = WS_CHILD | WS_VISIBLE | WS_TABSTOP | DS_SETFONT | DS_CONTROL,
            dwExtendedStyle = 0,
            cdit = 0,
            x = 0,
            y = 0,
            cx = 240,
            cy = 120,
        };

        writer.Write(template.style);
        writer.Write(template.dwExtendedStyle);
        writer.Write(template.cdit);
        writer.Write(template.x);
        writer.Write(template.y);
        writer.Write(template.cx);
        writer.Write(template.cy);

        // menu (empty)
        writer.Write((ushort)0);
        // class (empty)
        writer.Write((ushort)0);
        // title (empty)
        writer.Write((ushort)0);

        // DS_SETFONT requires font information.
        writer.Write((ushort)8); // point size
        writer.Write((ushort)400); // weight
        writer.Write((byte)0); // italic
        writer.Write((byte)1); // charset
        writer.Write("MS Shell Dlg".ToCharArray());
        writer.Write((ushort)0); // null terminator

        return stream.ToArray();
    }

    private static IntPtr DlgProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_INITDIALOG:
                return OnInitDialog(hwnd, lParam);

            case WM_SIZE:
                ResizePage(hwnd);
                return IntPtr.Zero;

            case WM_DESTROY:
                OnDestroy(hwnd);
                return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private static IntPtr OnInitDialog(IntPtr hwnd, IntPtr lParam)
    {
        var handle = GCHandle.FromIntPtr(lParam);
        if (handle.Target is not TagsPropertyPage page)
        {
            return IntPtr.Zero;
        }

        try
        {
            page.CreateControl();
            SetWindowLongPtr(hwnd, GWL_USERDATA, lParam);
            SetParent(page.Handle, hwnd);
            ResizePage(hwnd);
            page.Visible = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"TagsPropertySheet.OnInitDialog failed: {ex}");
        }

        return IntPtr.Zero;
    }

    private static void ResizePage(IntPtr hwnd)
    {
        var stored = GetWindowLongPtr(hwnd, GWL_USERDATA);
        if (stored == IntPtr.Zero)
        {
            return;
        }

        var handle = GCHandle.FromIntPtr(stored);
        if (handle.Target is not TagsPropertyPage page)
        {
            return;
        }

        if (GetClientRect(hwnd, out RECT rect))
        {
            page.SetBounds(0, 0, rect.right - rect.left, rect.bottom - rect.top);
        }
    }

    private static void OnDestroy(IntPtr hwnd)
    {
        var stored = GetWindowLongPtr(hwnd, GWL_USERDATA);
        if (stored == IntPtr.Zero)
        {
            return;
        }

        SetWindowLongPtr(hwnd, GWL_USERDATA, IntPtr.Zero);

        try
        {
            var handle = GCHandle.FromIntPtr(stored);
            if (handle.Target is TagsPropertyPage page)
            {
                if (page.DialogTemplatePointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(page.DialogTemplatePointer);
                    page.DialogTemplatePointer = IntPtr.Zero;
                }

                if (page.TitlePointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(page.TitlePointer);
                    page.TitlePointer = IntPtr.Zero;
                }

                page.Dispose();
            }
            handle.Free();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"TagsPropertySheet.OnDestroy failed: {ex}");
        }
    }

    #region COM Registration

    /// <summary>
    /// Registers the shell extension for all files, folders, and drives.
    /// </summary>
    [ComRegisterFunction]
    public static void Register(Type t)
    {
        var guid = t.GUID.ToString("B");
        var name = "Zee Mad Archivist Tags";

        using var clsid = Registry.ClassesRoot.CreateSubKey(@$"CLSID\{guid}");
        clsid?.SetValue(null, name);

        using var inproc = clsid?.CreateSubKey("InprocServer32");
        inproc?.SetValue(null, "mscoree.dll");
        inproc?.SetValue("ThreadingModel", "Apartment");

        RegisterPropertySheetHandler(guid, name, "*");
        RegisterPropertySheetHandler(guid, name, "Directory");
        RegisterPropertySheetHandler(guid, name, "Drive");
    }

    /// <summary>
    /// Removes the Tags property page from the registry.
    /// </summary>
    public static void Unregister()
    {
        Unregister(typeof(TagsPropertySheet));
    }

    /// <summary>
    /// Unregisters the shell extension. Called by regasm.exe /unregister.
    /// </summary>
    [ComUnregisterFunction]
    public static void Unregister(Type t)
    {
        var guid = t.GUID.ToString("B");

        Registry.ClassesRoot.DeleteSubKeyTree(@$"CLSID\{guid}", throwOnMissingSubKey: false);
        UnregisterPropertySheetHandler(guid, "*");
        UnregisterPropertySheetHandler(guid, "Directory");
        UnregisterPropertySheetHandler(guid, "Drive");
    }

    private static void RegisterPropertySheetHandler(string guid, string name, string classKey)
    {
        using var key = Registry.ClassesRoot.CreateSubKey(@$"{classKey}\shellex\PropertySheetHandlers\{guid}");
        key?.SetValue(null, name);
    }

    private static void UnregisterPropertySheetHandler(string guid, string classKey)
    {
        Registry.ClassesRoot.DeleteSubKeyTree(@$"{classKey}\shellex\PropertySheetHandlers\{guid}", throwOnMissingSubKey: false);
    }

    #endregion
}
