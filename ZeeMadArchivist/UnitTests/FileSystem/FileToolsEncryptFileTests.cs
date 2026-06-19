using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace UnitTests.FileSystem;

[TestClass]
public sealed class FileToolsEncryptFileTests
{
    [TestMethod]
    public void EncryptFile_WhenInputPathIsEmpty_Throws()
    {
        try
        {
            global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.EncryptionTools.EncryptFile("", "out.bin");
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException)
        {
        }
    }

    [TestMethod]
    public void EncryptFile_WhenOutputPathIsEmpty_Throws()
    {
        var inputPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.txt");

        try
        {
            File.WriteAllText(inputPath, "hello");

            try
            {
                global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.EncryptionTools.EncryptFile(inputPath, "");
                Assert.Fail("Expected ArgumentException was not thrown.");
            }
            catch (ArgumentException)
            {
            }
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
        }
    }

    [TestMethod]
    public void EncryptFile_WhenInputFileDoesNotExist_Throws()
    {
        var inputPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.txt");
        var outputPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.enc");

        try
        {
            global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.EncryptionTools.EncryptFile(inputPath, outputPath);
            Assert.Fail("Expected FileNotFoundException was not thrown.");
        }
        catch (FileNotFoundException)
        {
        }
    }

    [TestMethod]
    public void EncryptFile_WhenValid_CreatesEncryptedFile()
    {
        var inputPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.txt");
        var outputPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.enc");

        try
        {
            File.WriteAllBytes(inputPath, [1, 2, 3, 4, 5, 6, 7]);

            global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.EncryptionTools.EncryptFile(inputPath, outputPath);

            Assert.IsTrue(File.Exists(outputPath));
            Assert.IsGreaterThan(0, new FileInfo(outputPath).Length);

            var encryptedBytes = File.ReadAllBytes(outputPath);
            CollectionAssert.AreNotEqual(File.ReadAllBytes(inputPath), encryptedBytes);
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }
}
