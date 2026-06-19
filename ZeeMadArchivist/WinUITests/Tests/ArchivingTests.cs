using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Tests;

[TestClass]
public sealed class ArchivingTests
{
    [TestMethod]
    public void MapDrive_WhenFolderPathIsEmpty_ReturnsInvalidParameter()
    {
        var result = FolderTools.MapDrive(string.Empty, 'Z', "Test");
        Assert.AreEqual(87, result);
    }

    [TestMethod]
    public void MapDrive_WhenDriveLetterIsInvalid_ReturnsInvalidParameter()
    {
        var result = FolderTools.MapDrive("\\\\server\\share", '1', "Test");
        Assert.AreEqual(87, result);
    }

    [TestMethod]
    public void MapDrive_WhenNameIsEmpty_ReturnsInvalidParameter()
    {
        var result = FolderTools.MapDrive("\\\\server\\share", 'Z', string.Empty);
        Assert.AreEqual(87, result);
    }

    [TestMethod]
    public void UnmapDrive_WhenDriveLetterIsInvalid_ReturnsFalse()
    {
        var result = FolderTools.UnmapDrive('1');
        Assert.IsFalse(result);
    }
}
