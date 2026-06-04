using CyberFeedForward.TheMadArchivist.AppTools.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace UnitTests.FileSystem;

[TestClass]
public sealed class FolderToolsDriveIconTests
{
    [TestMethod]
    public void GetDefaultAppIconPath_WhenAppIconCopiedToOutput_ReturnsExistingFile()
    {
        var result = FolderTools.GetDefaultAppIconPath();

        Assert.IsFalse(string.IsNullOrWhiteSpace(result));
        Assert.IsTrue(File.Exists(result));
        Assert.AreEqual("AppIcon.ico", Path.GetFileName(result));
    }

    [TestMethod]
    public void TrySetDriveIcon_WhenDriveLetterInvalid_ReturnsFalseWithError()
    {
        var result = FolderTools.TrySetDriveIcon('1', "C:\\Icons\\AppIcon.ico", out var errorMessage);

        Assert.IsFalse(result);
        Assert.AreEqual("Drive letter is invalid.", errorMessage);
    }

    [TestMethod]
    public void TrySetDriveIcon_WhenIconFileMissing_ReturnsFalseWithError()
    {
        var missingIconPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "AppIcon.ico");

        var result = FolderTools.TrySetDriveIcon('Z', missingIconPath, out var errorMessage);

        Assert.IsFalse(result);
        Assert.AreEqual("Icon file does not exist.", errorMessage);
    }

    [TestMethod]
    public void GetMapDriveErrorMessage_WhenDriveAlreadyAssigned_ReturnsFriendlyMessage()
    {
        var result = FolderTools.GetMapDriveErrorMessage(85, 'Z', "C:\\Archives");

        Assert.AreEqual("Z: is already in use. Choose a different drive letter and try again.", result);
    }

    [TestMethod]
    public void CreateMapDriveException_WhenMapFails_ContainsFriendlyMessageAndWin32Details()
    {
        var result = FolderTools.CreateMapDriveException(67, 'Z', "C:\\Archives");

        Assert.AreEqual("Windows could not find or access C:\\Archives. Check that the folder exists and try again.", result.Message);
        Assert.IsNotNull(result.InnerException);
    }
}
