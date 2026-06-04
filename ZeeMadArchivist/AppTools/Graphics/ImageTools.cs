using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace CyberFeedForward.TheMadArchivist.AppTools.Graphics;

public static partial class ImageTools
{
    public static Icon ToIcon(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("Image path cannot be empty.", nameof(imagePath));
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("Image file not found.", imagePath);
        }

        using var bitmap = new Bitmap(imagePath);
        var hIcon = bitmap.GetHicon();

        try
        {
            

            using var tempIcon = Icon.FromHandle(hIcon);
            return (Icon)tempIcon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    public static void SaveIcon(Icon icon, string filePath)
    {
        ArgumentNullException.ThrowIfNull(icon);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        }

        var directory = System.IO.Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        icon.Save(fileStream);
    }

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "DestroyIcon")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr hIcon);
}
