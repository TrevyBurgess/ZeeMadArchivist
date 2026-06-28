using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CyberFeedForward.TheMadArchivist.ShellExtension.Interop;

internal static class Win32
{
    public const uint WM_INITDIALOG = 0x0110;
    public const uint WM_SIZE = 0x0005;
    public const uint WM_DESTROY = 0x0002;
    public const uint WM_NOTIFY = 0x004E;

    public const int GWL_USERDATA = -21;

    public const uint WS_CHILD = 0x40000000;
    public const uint WS_VISIBLE = 0x10000000;
    public const uint WS_TABSTOP = 0x00010000;
    public const uint DS_SETFONT = 0x00000040;
    public const uint DS_CONTROL = 0x00000400;

    public const uint PSP_USETITLE = 0x00000008;
    public const uint PSP_USEREFPARENT = 0x00000040;
    public const uint PSP_DLGINDIRECT = 0x00000100;
    public const uint PSP_USECALLBACK = 0x00000004;

    public const uint PSN_APPLY = 0xFFFFFFFE;
    public const uint PSN_KILLACTIVE = 0xFFFFFFFD;
    public const uint PSN_SETACTIVE = 0xFFFFFFFC;
    public const uint PSN_RESET = 0xFFFFFFFB;

    public const uint WM_USER = 0x0400;
    public const uint WM_GETFONT = 0x0031;

    public const int CF_HDROP = 15;

    public const int MAX_PATH = 260;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLong64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLong64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr CreatePropertySheetPage(ref PROPSHEETPAGE psp);

    [DllImport("user32.dll")]
    public static extern bool DestroyPropertySheetPage(IntPtr hPage);

    [DllImport("shell32.dll")]
    public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, [Out] char[]? lpszFile, uint cch);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, IntPtr[] apidl, uint dwFlags);

    [DllImport("shell32.dll")]
    public static extern void ILFree(IntPtr pidl);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLong64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? GetWindowLong64(hWnd, nIndex)
            : GetWindowLong32(hWnd, nIndex);
    }

    public static int LOWORD(IntPtr value) => (ushort)(value.ToInt64() & 0xFFFF);
    public static int HIWORD(IntPtr value) => (ushort)((value.ToInt64() >> 16) & 0xFFFF);
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct DLGTEMPLATE
{
    public uint style;
    public uint dwExtendedStyle;
    public short cdit;
    public short x;
    public short y;
    public short cx;
    public short cy;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PROPSHEETPAGE
{
    public uint dwSize;
    public uint dwFlags;
    public IntPtr hInstance;
    public IntPtr pResource;
    public IntPtr hIcon;
    public IntPtr pszTitle;
    public IntPtr pfnDlgProc;
    public IntPtr lParam;
    public IntPtr pfnCallback;
    public IntPtr pcRefParent;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NMHDR
{
    public IntPtr hwndFrom;
    public IntPtr idFrom;
    public uint code;
}

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate IntPtr DlgProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
[return: MarshalAs(UnmanagedType.Bool)]
internal delegate bool AddPropSheetPageProc(IntPtr hPage, IntPtr lParam);
