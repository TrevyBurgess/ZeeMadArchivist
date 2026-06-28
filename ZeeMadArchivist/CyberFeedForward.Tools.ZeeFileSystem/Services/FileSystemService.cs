using CyberFeedForward.Tools.ZeeFileSystem.Models;
using System.Diagnostics;

namespace CyberFeedForward.Tools.ZeeFileSystem.Services;

public sealed class FileSystemService : IFileSystemService
{
    public IReadOnlyList<FileSystemEntry> GetEntries(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return [];
        }

        try
        {
            var directories = Directory.EnumerateDirectories(folderPath)
                .Select(path => new FileSystemEntry { Name = Path.GetFileName(path), FullPath = path, IsFolder = true });

            var files = Directory.EnumerateFiles(folderPath)
                .Select(path => new FileSystemEntry { Name = Path.GetFileName(path), FullPath = path, IsFolder = false });

            return [.. directories
                .Concat(files)
                .OrderByDescending(e => e.IsFolder)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)];
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return [];
        }
    }
}
