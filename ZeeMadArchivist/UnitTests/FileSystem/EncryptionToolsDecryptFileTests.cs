using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Security.Cryptography;

namespace UnitTests.FileSystem;

[TestClass]
public sealed class EncryptionToolsDecryptFileTests
{
    [TestMethod]
    public void DecryptFile_WhenRoundTrip_ReturnsOriginalBytes()
    {
        var inputPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.bin");
        var encryptedPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.enc");
        var outputPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.bin");

        try
        {
            var inputBytes = new byte[1024 * 64];
            for (var i = 0; i < inputBytes.Length; i++)
            {
                inputBytes[i] = (byte)(i % 251);
            }

            File.WriteAllBytes(inputPath, inputBytes);

            global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.EncryptionTools.EncryptFile(inputPath, encryptedPath);
            global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.EncryptionTools.DecryptFile(encryptedPath, outputPath);

            var outputBytes = File.ReadAllBytes(outputPath);
            CollectionAssert.AreEqual(inputBytes, outputBytes);
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(encryptedPath)) File.Delete(encryptedPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [TestMethod]
    public void DecryptFile_WhenEncryptedFileIsTampered_Throws()
    {
        var inputPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.bin");
        var encryptedPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.enc");
        var outputPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.bin");

        try
        {
            File.WriteAllBytes(inputPath, [1, 2, 3, 4, 5]);
            global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.EncryptionTools.EncryptFile(inputPath, encryptedPath);

            var bytes = File.ReadAllBytes(encryptedPath);
            bytes[Math.Min(10, bytes.Length - 1)] ^= 0xFF;
            File.WriteAllBytes(encryptedPath, bytes);

            try
            {
                global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.EncryptionTools.DecryptFile(encryptedPath, outputPath);
                Assert.Fail("Expected CryptographicException was not thrown.");
            }
            catch (CryptographicException)
            {
            }
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(encryptedPath)) File.Delete(encryptedPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [TestMethod]
    public void DecryptFile_WhenHeaderIsInvalid_Throws()
    {
        var encryptedPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.enc");
        var outputPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.bin");

        try
        {
            File.WriteAllBytes(encryptedPath, [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

            try
            {
                global::CyberFeedForward.Tools.ZeeFileSystem.Utilities.EncryptionTools.DecryptFile(encryptedPath, outputPath);
                Assert.Fail("Expected CryptographicException was not thrown.");
            }
            catch (CryptographicException)
            {
            }
        }
        finally
        {
            if (File.Exists(encryptedPath)) File.Delete(encryptedPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }
}
