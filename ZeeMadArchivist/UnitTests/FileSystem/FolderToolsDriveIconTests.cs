using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

    [TestMethod]
    public void MapDrive_WhenLocalFolderExists_MapsAndUnmapsDrive()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{System.Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        var usedLetters = new HashSet<char>(DriveInfo.GetDrives()
            .Select(d => char.ToUpperInvariant(d.Name[0]))
            .Where(c => c is >= 'A' and <= 'Z'));

        var availableLetter = Enumerable.Range('D', 'Z' - 'D' + 1)
            .Select(c => (char)c)
            .FirstOrDefault(c => !usedLetters.Contains(c));

        if (availableLetter == default)
        {
            Assert.Inconclusive("No unused drive letters available for test.");
            return;
        }

        try
        {
            var result = FolderTools.MapDrive(tempFolder, availableLetter, "TestArchive");
            Assert.AreEqual(0, result, $"MapDrive failed with error {result}.");
            Assert.IsTrue(DriveInfo.GetDrives().Any(d => char.ToUpperInvariant(d.Name[0]) == availableLetter), "Expected drive letter to be mapped.");

            var unmapped = FolderTools.UnmapDrive(availableLetter);
            Assert.IsTrue(unmapped, "Expected drive to be unmapped.");
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, recursive: true);
            }
        }
    }
}
