using CyberFeedForward.TheMadArchivist.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnitTests.Services;

[TestClass]
public sealed class FileSystemTreeProviderTests
{
    [TestMethod]
    public void LoadChildren_WhenFolderHasContents_LoadsChildNodes()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "FolderA"));
            File.WriteAllText(Path.Combine(root, "FileA.txt"), "a");

            var service = new FileSystemService();
            var provider = new FileSystemTreeProvider(service);

            var rootNodes = provider.CreateRoot(root);
            Assert.HasCount(1, rootNodes);

            var rootNode = rootNodes[0];
            provider.LoadChildren(rootNode);

            Assert.IsTrue(rootNode.IsLoaded);
            Assert.HasCount(2, rootNode.Children);

            var names = rootNode.Children.Select(c => c.Name).ToList();
            CollectionAssert.AreEquivalent(new List<string> { "FolderA", "FileA.txt" }, names);
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
