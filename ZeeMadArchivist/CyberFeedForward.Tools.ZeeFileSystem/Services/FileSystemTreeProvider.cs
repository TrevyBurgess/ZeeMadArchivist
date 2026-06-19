using CyberFeedForward.Tools.ZeeFileSystem.Models;
using System.Collections.ObjectModel;

namespace CyberFeedForward.Tools.ZeeFileSystem.Services;

public sealed class FileSystemTreeProvider(IFileSystemService fileSystemService) : IFileSystemTreeProvider
{
    private readonly IFileSystemService _fileSystemService = fileSystemService;

    public ObservableCollection<FileSystemTreeNode> CreateRoot(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return [];
        }

        var rootEntry = new FileSystemEntry
        {
            Name = folderPath,
            FullPath = folderPath,
            IsFolder = true,
        };

        var rootNode = new FileSystemTreeNode(rootEntry);
        rootNode.Children.Add(new FileSystemTreeNode(new FileSystemEntry { Name = string.Empty, FullPath = string.Empty, IsFolder = true }));

        return [.. new ObservableCollection<FileSystemTreeNode> { rootNode }];
    }

    public void LoadChildren(FileSystemTreeNode node)
    {
        if (!node.IsFolder || node.IsLoaded)
        {
            return;
        }

        node.Children.Clear();

        foreach (var entry in _fileSystemService.GetEntries(node.FullPath))
        {
            var child = new FileSystemTreeNode(entry);

            if (child.IsFolder)
            {
                child.Children.Add(new FileSystemTreeNode(new FileSystemEntry { Name = string.Empty, FullPath = string.Empty, IsFolder = true }));
            }

            node.Children.Add(child);
        }

        node.IsLoaded = true;
    }
}
