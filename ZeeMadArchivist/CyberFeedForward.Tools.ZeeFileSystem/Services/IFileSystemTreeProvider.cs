using CyberFeedForward.Tools.ZeeFileSystem.Models;
using System.Collections.ObjectModel;

namespace CyberFeedForward.Tools.ZeeFileSystem.Services;

public interface IFileSystemTreeProvider
{
    ObservableCollection<FileSystemTreeNode> CreateRoot(string folderPath);

    void LoadChildren(FileSystemTreeNode node);
}
