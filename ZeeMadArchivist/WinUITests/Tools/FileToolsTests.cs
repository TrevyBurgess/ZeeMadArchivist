using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;

namespace UnitTests.Tools;

[TestClass]
public sealed class FileToolsTests
{
    [TestMethod]
    public void SaveIcon_WhenIconIsNull_Throws()
    {
        try
        {
            FileTools.SaveIcon(null!, "test.ico");
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException)
        {
        }
    }

    [TestMethod]
    public void SaveIcon_WhenFilePathIsEmpty_Throws()
    {
        using var icon = SystemIcons.Application;

        try
        {
            FileTools.SaveIcon(icon, "");
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException)
        {
        }
    }

    [TestMethod]
    public void SaveIcon_WhenValid_SavesFile()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.ico");

        try
        {
            using var icon = SystemIcons.Application;
            FileTools.SaveIcon(icon, tempPath);

            Assert.IsTrue(File.Exists(tempPath));
            var length = new FileInfo(tempPath).Length;
            Assert.IsGreaterThan(0, length);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [TestMethod]
    public void SaveIcon_WhenDirectoryDoesNotExist_CreatesDirectoryAndSavesFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        var tempPath = Path.Combine(tempDir, "test.ico");

        try
        {
            using var icon = SystemIcons.Application;
            FileTools.SaveIcon(icon, tempPath);

            Assert.IsTrue(Directory.Exists(tempDir));
            Assert.IsTrue(File.Exists(tempPath));
            var length = new FileInfo(tempPath).Length;
            Assert.IsGreaterThan(0, length);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir);
            }
        }
    }
}
