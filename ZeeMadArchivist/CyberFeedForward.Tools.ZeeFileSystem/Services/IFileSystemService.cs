using CyberFeedForward.Tools.ZeeFileSystem.Models;

namespace CyberFeedForward.Tools.ZeeFileSystem.Services;

public interface IFileSystemService
{
    IReadOnlyList<FileSystemEntry> GetEntries(string folderPath);
}
