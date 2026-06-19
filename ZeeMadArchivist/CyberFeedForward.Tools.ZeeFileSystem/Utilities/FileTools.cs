using System.Drawing;

namespace CyberFeedForward.Tools.ZeeFileSystem.Utilities;

public static class FileTools
{
    public static void SaveIcon(Icon icon, string filePath)
    {
        ArgumentNullException.ThrowIfNull(icon);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        icon.Save(fileStream);
    }

    public static bool IsIdentical(string filePath1, string filePath2)
    {
        if (string.IsNullOrWhiteSpace(filePath1) || string.IsNullOrWhiteSpace(filePath2))
        {
            return false;
        }

        if (string.Equals(filePath1, filePath2, StringComparison.OrdinalIgnoreCase))
        {
            return File.Exists(filePath1);
        }

        var fileInfo1 = new FileInfo(filePath1);
        var fileInfo2 = new FileInfo(filePath2);

        if (!fileInfo1.Exists || !fileInfo2.Exists)
        {
            return false;
        }

        if (fileInfo1.Length != fileInfo2.Length)
        {
            return false;
        }

        const int bufferSize = 1024 * 1024;

        using var stream1 = new FileStream(filePath1, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
        using var stream2 = new FileStream(filePath2, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);

        var buffer1 = new byte[bufferSize];
        var buffer2 = new byte[bufferSize];

        while (true)
        {
            var read1 = stream1.Read(buffer1, 0, buffer1.Length);
            var read2 = stream2.Read(buffer2, 0, buffer2.Length);

            if (read1 != read2)
            {
                return false;
            }

            if (read1 == 0)
            {
                return true;
            }

            if (!buffer1.AsSpan(0, read1).SequenceEqual(buffer2.AsSpan(0, read2)))
            {
                return false;
            }
        }
    }
}
