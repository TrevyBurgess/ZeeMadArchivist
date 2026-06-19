using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using CyberFeedForward.Tools.ZeeFileSystem.Utilities;

namespace UnitTests.Tools;

[TestClass]
public sealed class FolderToolsTests
{
    [TestMethod]
    public void UpdateFolderIcon_WhenIconPathIsEmpty_ReturnsFalse()
    {
        var result = global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.FolderTools.UpdateFolderIcon(string.Empty, "C:\\");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void UpdateFolderIcon_WhenFolderPathIsEmpty_ReturnsFalse()
    {
        var result = global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.FolderTools.UpdateFolderIcon("C:\\temp.ico", string.Empty);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void UpdateFolderIcon_WhenPathsDoNotExist_ReturnsFalse()
    {
        var result = global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.FolderTools.UpdateFolderIcon("C:\\this-file-should-not-exist-12345.ico", "C:\\this-folder-should-not-exist-12345");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void UpdateFolderIcon_WhenValid_CreatesDesktopIni()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        var tempIcon = Path.Combine(tempFolder, "icon.ico");

        Directory.CreateDirectory(tempFolder);

        try
        {
            using (var icon = SystemIcons.Application)
            using (var fs = new FileStream(tempIcon, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                icon.Save(fs);
            }

            var result = global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.FolderTools.UpdateFolderIcon(tempIcon, tempFolder);

            Assert.IsTrue(result);
            Assert.IsTrue(File.Exists(Path.Combine(tempFolder, "desktop.ini")));
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                var desktopIniPath = Path.Combine(tempFolder, "desktop.ini");
                if (File.Exists(desktopIniPath))
                {
                    File.SetAttributes(desktopIniPath, FileAttributes.Normal);
                }

                File.SetAttributes(tempFolder, FileAttributes.Directory);
                Directory.Delete(tempFolder, recursive: true);
            }
        }
    }

    [TestMethod]
    public void LoadDefaultIcons_WhenSourceFolderMissing_ReturnsZero()
    {
        var destFolder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        var sourceFolder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");

        try
        {
            var copied = global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.FolderTools.LoadDefaultIcons(destFolder, iconsFolderPath: sourceFolder);
            Assert.AreEqual(0, copied);
        }
        finally
        {
            if (Directory.Exists(destFolder))
            {
                Directory.Delete(destFolder, recursive: true);
            }
        }
    }

    [TestMethod]
    public void LoadDefaultIcons_WhenDestinationMissing_CreatesDestinationAndCopiesFiles()
    {
        var baseFolder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        var sourceFolder = Path.Combine(baseFolder, "Icons");
        var destFolder = Path.Combine(baseFolder, "CustomIcons");
        Directory.CreateDirectory(sourceFolder);

        var file1 = Path.Combine(sourceFolder, "a.ico");
        var file2 = Path.Combine(sourceFolder, "b.ico");
        File.WriteAllBytes(file1, [1, 2, 3]);
        File.WriteAllBytes(file2, [4, 5, 6]);

        try
        {
            var copied = global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.FolderTools.LoadDefaultIcons(destFolder, iconsFolderPath: sourceFolder);

            Assert.AreEqual(2, copied);
            Assert.IsTrue(Directory.Exists(destFolder));
            Assert.IsTrue(File.Exists(Path.Combine(destFolder, "a.ico")));
            Assert.IsTrue(File.Exists(Path.Combine(destFolder, "b.ico")));
        }
        finally
        {
            if (Directory.Exists(baseFolder))
            {
                Directory.Delete(baseFolder, recursive: true);
            }
        }
    }

    [TestMethod]
    public void LoadDefaultIcons_WhenFileAlreadyExists_SkipsExistingFile()
    {
        var baseFolder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        var sourceFolder = Path.Combine(baseFolder, "Icons");
        var destFolder = Path.Combine(baseFolder, "CustomIcons");
        Directory.CreateDirectory(sourceFolder);
        Directory.CreateDirectory(destFolder);

        var sourceFile1 = Path.Combine(sourceFolder, "a.ico");
        var sourceFile2 = Path.Combine(sourceFolder, "b.ico");
        File.WriteAllBytes(sourceFile1, [1, 2, 3]);
        File.WriteAllBytes(sourceFile2, [4, 5, 6]);

        var existingDest = Path.Combine(destFolder, "a.ico");
        File.WriteAllBytes(existingDest, [9, 9, 9]);

        try
        {
            var copied = global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.FolderTools.LoadDefaultIcons(destFolder, iconsFolderPath: sourceFolder);

            Assert.AreEqual(1, copied);
            CollectionAssert.AreEqual("\t\t\t"u8.ToArray(), File.ReadAllBytes(existingDest));
            Assert.IsTrue(File.Exists(Path.Combine(destFolder, "b.ico")));
        }
        finally
        {
            if (Directory.Exists(baseFolder))
            {
                Directory.Delete(baseFolder, recursive: true);
            }
        }
    }

    [TestMethod]
    public void TryRenameIconFile_WhenValid_RenamesFile()
    {
        var baseFolder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        var customIcons = Path.Combine(baseFolder, "CustomIcons");
        Directory.CreateDirectory(customIcons);

        var original = Path.Combine(customIcons, "Old.ico");
        File.WriteAllBytes(original, [1, 2, 3]);

        try
        {
            var ok = global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.FolderTools.TryRenameIconFile(customIcons, original, "New", out var error);
            Assert.IsTrue(ok);
            Assert.AreEqual(string.Empty, error);
            Assert.IsFalse(File.Exists(original));
            Assert.IsTrue(File.Exists(Path.Combine(customIcons, "New.ico")));
        }
        finally
        {
            if (Directory.Exists(baseFolder))
            {
                Directory.Delete(baseFolder, recursive: true);
            }
        }
    }

    [TestMethod]
    public void TryRenameIconFile_WhenDuplicateName_ReturnsFalse()
    {
        var baseFolder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        var customIcons = Path.Combine(baseFolder, "CustomIcons");
        Directory.CreateDirectory(customIcons);

        var original = Path.Combine(customIcons, "Old.ico");
        var existing = Path.Combine(customIcons, "New.ico");
        File.WriteAllBytes(original, [1, 2, 3]);
        File.WriteAllBytes(existing, [9, 9, 9]);

        try
        {
            var ok = global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.FolderTools.TryRenameIconFile(customIcons, original, "New", out var error);
            Assert.IsFalse(ok);
            Assert.AreNotEqual(string.Empty, error);
            Assert.IsTrue(File.Exists(original));
            CollectionAssert.AreEqual("\t\t\t"u8.ToArray(), File.ReadAllBytes(existing));
        }
        finally
        {
            if (Directory.Exists(baseFolder))
            {
                Directory.Delete(baseFolder, recursive: true);
            }
        }
    }

    [TestMethod]
    public void TryRenameIconFile_WhenInvalidName_ReturnsFalse()
    {
        var baseFolder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        var customIcons = Path.Combine(baseFolder, "CustomIcons");
        Directory.CreateDirectory(customIcons);

        var original = Path.Combine(customIcons, "Old.ico");
        File.WriteAllBytes(original, [1, 2, 3]);

        try
        {
            var ok = global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.FolderTools.TryRenameIconFile(customIcons, original, "New<", out var error);
            Assert.IsFalse(ok);
            Assert.AreNotEqual(string.Empty, error);
            Assert.IsTrue(File.Exists(original));
        }
        finally
        {
            if (Directory.Exists(baseFolder))
            {
                Directory.Delete(baseFolder, recursive: true);
            }
        }
    }

    [TestMethod]
    public void TryRenameIconFile_WhenOriginalOutsideCustomIcons_ReturnsFalse()
    {
        var baseFolder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        var customIcons = Path.Combine(baseFolder, "CustomIcons");
        var otherFolder = Path.Combine(baseFolder, "Other");
        Directory.CreateDirectory(customIcons);
        Directory.CreateDirectory(otherFolder);

        var original = Path.Combine(otherFolder, "Old.ico");
        File.WriteAllBytes(original, [1, 2, 3]);

        try
        {
            var ok = global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.FolderTools.TryRenameIconFile(customIcons, original, "New", out var error);
            Assert.IsFalse(ok);
            Assert.AreNotEqual(string.Empty, error);
            Assert.IsTrue(File.Exists(original));
        }
        finally
        {
            if (Directory.Exists(baseFolder))
            {
                Directory.Delete(baseFolder, recursive: true);
            }
        }
    }

    [TestMethod]
    public void TryOpenFolderInExplorer_WhenPathIsEmpty_ReturnsFalse()
    {
        var ok = global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.FolderTools.TryOpenFolderInExplorer(string.Empty, out var error);
        Assert.IsFalse(ok);
        Assert.AreNotEqual(string.Empty, error);
    }

    [TestMethod]
    public void TryOpenFolderInExplorer_WhenValid_InvokesExplorerWithFolderPath()
    {
        var baseFolder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        Directory.CreateDirectory(baseFolder);

        ProcessStartInfo? started = null;

        try
        {
            var ok = FolderTools.TryOpenFolderInExplorer(
                baseFolder,
                out var error,
                processStart: psi =>
                {
                    started = psi;
                    return Process.GetCurrentProcess();
                });

            Assert.IsTrue(ok);
            Assert.AreEqual(string.Empty, error);
            Assert.IsNotNull(started);
            Assert.AreEqual("explorer.exe", started!.FileName);
            Assert.IsTrue(started.Arguments.Contains(baseFolder, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(baseFolder))
            {
                Directory.Delete(baseFolder, recursive: true);
            }
        }
    }
}
