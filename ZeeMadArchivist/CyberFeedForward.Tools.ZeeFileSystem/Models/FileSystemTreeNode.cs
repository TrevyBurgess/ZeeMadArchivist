using System.Collections.ObjectModel;

namespace CyberFeedForward.Tools.ZeeFileSystem.Models;

public sealed class FileSystemTreeNode(FileSystemEntry entry)
{
    public FileSystemEntry Entry { get; } = entry;

    public ObservableCollection<FileSystemTreeNode> Children { get; } = [];

    public bool IsLoaded { get; set; }

    public string Name => Entry.Name;

    public string FullPath => Entry.FullPath;

    public bool IsFolder => Entry.IsFolder;
}
