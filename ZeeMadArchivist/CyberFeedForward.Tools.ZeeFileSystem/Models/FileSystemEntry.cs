namespace CyberFeedForward.Tools.ZeeFileSystem.Models;

public sealed class FileSystemEntry
{
    public string Name { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public bool IsFolder { get; set; }
}
