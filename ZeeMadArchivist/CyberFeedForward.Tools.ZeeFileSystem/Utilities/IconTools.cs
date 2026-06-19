using System.Runtime.InteropServices;

namespace CyberFeedForward.Tools.ZeeFileSystem.Utilities;

#nullable enable

public static partial class IconTools
{
    public static IntPtr LoadIconFromFile(string path)
    {
        return LoadImage(IntPtr.Zero, path, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
    }

    public static IntPtr LoadApplicationIcon()
    {
        try
        {
            var processPath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(processPath) || !File.Exists(processPath))
            {
                return IntPtr.Zero;
            }

            var largeIcons = new IntPtr[1];
            var smallIcons = new IntPtr[1];
            var extracted = ExtractIconEx(processPath, 0, largeIcons, smallIcons, 1);
            if (extracted <= 0)
            {
                return LoadShellAssociatedIcon(processPath);
            }

            if (smallIcons[0] != IntPtr.Zero)
            {
                if (largeIcons[0] != IntPtr.Zero)
                {
                    DestroyIcon(largeIcons[0]);
                }

                return smallIcons[0];
            }

            if (largeIcons[0] != IntPtr.Zero)
            {
                return largeIcons[0];
            }

            return LoadShellAssociatedIcon(processPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return IntPtr.Zero;
        }
    }

    public static void DestroyIcon(IntPtr hIcon)
    {
        if (hIcon != IntPtr.Zero)
        {
            _ = DestroyIconNative(hIcon);
        }
    }

    private static IntPtr LoadShellAssociatedIcon(string filePath)
    {
        try
        {
            var result = SHGetFileInfo(
                filePath,
                0,
                out var info,
                (uint)Marshal.SizeOf<SHFILEINFO>(),
                SHGFI_ICON | SHGFI_SMALLICON);

            if (result == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            return info.hIcon;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return IntPtr.Zero;
        }
    }

    [LibraryImport("shell32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "ExtractIconExW")]
    private static partial int ExtractIconEx(string lpszFile, int nIconIndex, [Out] IntPtr[]? phiconLarge, [Out] IntPtr[]? phiconSmall, uint nIcons);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        out SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_SMALLICON = 0x000000001;

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_DEFAULTSIZE = 0x00000040;

    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "LoadImageW")]
    private static partial IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "DestroyIcon")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIconNative(IntPtr hIcon);
}
