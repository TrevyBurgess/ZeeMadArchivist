using CyberFeedForward.TheMadArchivist.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace UnitTests.Services;

[TestClass]
public sealed class FileSystemServiceTests
{
    [TestMethod]
    public void GetEntries_WhenFolderDoesNotExist_ReturnsEmpty()
    {
        var service = new FileSystemService();
        var entries = service.GetEntries("Z:\\this-path-should-not-exist-123456789");
        Assert.HasCount(0, entries);
    }

    [TestMethod]
    public void GetEntries_WhenFolderHasFilesAndFolders_ReturnsFoldersFirstThenFilesSorted()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "b_folder"));
            Directory.CreateDirectory(Path.Combine(root, "a_folder"));
            File.WriteAllText(Path.Combine(root, "b.txt"), "b");
            File.WriteAllText(Path.Combine(root, "a.txt"), "a");

            var service = new FileSystemService();
            var entries = service.GetEntries(root);

            Assert.HasCount(4, entries);

            Assert.IsTrue(entries[0].IsFolder);
            Assert.IsTrue(entries[1].IsFolder);
            Assert.IsFalse(entries[2].IsFolder);
            Assert.IsFalse(entries[3].IsFolder);

            Assert.AreEqual("a_folder", entries[0].Name);
            Assert.AreEqual("b_folder", entries[1].Name);
            Assert.AreEqual("a.txt", entries[2].Name);
            Assert.AreEqual("b.txt", entries[3].Name);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
