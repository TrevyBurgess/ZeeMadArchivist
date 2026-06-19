using CyberFeedForward.TheMadArchivist.Models;
using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.ViewModels.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace UnitTests.ViewModels.Controls;

[TestClass]
public sealed class BreadcrumbBarViewModelTests
{
    private sealed class FakeFileSystemService(Dictionary<string, IReadOnlyList<FileSystemEntry>> entriesByPath) : IFileSystemService
    {
        private readonly Dictionary<string, IReadOnlyList<FileSystemEntry>> _entriesByPath = entriesByPath;

        public IReadOnlyList<FileSystemEntry> GetEntries(string folderPath)
        {
            return _entriesByPath.TryGetValue(folderPath, out var entries)
                ? entries
                : [];
        }
    }

    [TestMethod]
    public void BuildCumulativePaths_Null_ReturnsEmpty()
    {
        var paths = BreadcrumbBarViewModel.BuildCumulativePaths(null);
        Assert.IsEmpty(paths);
    }

    [TestMethod]
    public void BuildCumulativePaths_WindowsPath_ReturnsRootAndSegments()
    {
        var paths = BreadcrumbBarViewModel.BuildCumulativePaths("C:\\A\\B");

        Assert.AreEqual("C:\\", paths[0]);
        Assert.AreEqual("C:\\A", paths[1]);
        Assert.AreEqual("C:\\A\\B", paths[2]);
    }

    [TestMethod]
    public void SettingFolderPath_BuildsSegments_AndLoadsSubFolderNames()
    {
        var fs = new FakeFileSystemService(new Dictionary<string, IReadOnlyList<FileSystemEntry>>
        {
            ["C:\\"] = [],
            ["C:\\A"] =
            [
                new() { Name = "Sub1", FullPath = "C:\\A\\Sub1", IsFolder = true },
                new() { Name = "File1.txt", FullPath = "C:\\A\\File1.txt", IsFolder = false },
            ],
            ["C:\\A\\B"] = [],
        });

        var vm = new BreadcrumbBarViewModel(fs)
        {
            FolderPath = "C:\\A\\B"
        };

        Assert.HasCount(3, vm.Segments);
        Assert.AreEqual("C:\\", vm.Segments[0].FolderPath);
        Assert.IsEmpty(vm.Segments[0].Items);

        Assert.AreEqual("C:\\A", vm.Segments[1].FolderPath);
        Assert.HasCount(1, vm.Segments[1].Items);
        Assert.AreEqual("Sub1", vm.Segments[1].Items[0]);

        Assert.AreEqual("C:\\A\\B", vm.Segments[2].FolderPath);
        Assert.IsEmpty(vm.Segments[2].Items);
    }
}
