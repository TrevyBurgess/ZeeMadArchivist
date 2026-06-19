using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace UnitTests.FileSystem;

[TestClass]
public sealed class FileToolsIsIdenticalTests
{
    [TestMethod]
    public void IsIdentical_WhenSamePathAndFileExists_ReturnsTrue()
    {
        var path = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.bin");

        try
        {
            File.WriteAllBytes(path, [1, 2, 3]);

            Assert.IsTrue(FileTools.IsIdentical(path, path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void IsIdentical_WhenSamePathDifferentCasingAndFileExists_ReturnsTrue()
    {
        var path = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.bin");

        try
        {
            File.WriteAllBytes(path, [1, 2, 3]);

            var pathUpper = path.ToUpperInvariant();
            var pathLower = path.ToLowerInvariant();

            Assert.IsTrue(FileTools.IsIdentical(pathUpper, pathLower));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void IsIdentical_WhenEitherPathIsEmpty_ReturnsFalse()
    {
        Assert.IsFalse(FileTools.IsIdentical("", "a"));
        Assert.IsFalse(FileTools.IsIdentical("a", ""));
        Assert.IsFalse(FileTools.IsIdentical(" ", "a"));
        Assert.IsFalse(FileTools.IsIdentical("a", " "));
    }

    [TestMethod]
    public void IsIdentical_WhenFileDoesNotExist_ReturnsFalse()
    {
        var path1 = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}_1.bin");
        var path2 = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}_2.bin");

        Assert.IsFalse(FileTools.IsIdentical(path1, path2));
    }

    [TestMethod]
    public void IsIdentical_WhenLengthsDiffer_ReturnsFalse()
    {
        var path1 = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}_1.bin");
        var path2 = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}_2.bin");

        try
        {
            File.WriteAllBytes(path1, [1, 2, 3]);
            File.WriteAllBytes(path2, [1, 2, 3, 4]);

            Assert.IsFalse(FileTools.IsIdentical(path1, path2));
        }
        finally
        {
            if (File.Exists(path1)) File.Delete(path1);
            if (File.Exists(path2)) File.Delete(path2);
        }
    }

    [TestMethod]
    public void IsIdentical_WhenContentsAreSame_ReturnsTrue()
    {
        var path1 = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}_1.bin");
        var path2 = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}_2.bin");

        try
        {
            var data = new byte[1024 * 32];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 251);
            }

            File.WriteAllBytes(path1, data);
            File.WriteAllBytes(path2, data);

            Assert.IsTrue(FileTools.IsIdentical(path1, path2));
        }
        finally
        {
            if (File.Exists(path1)) File.Delete(path1);
            if (File.Exists(path2)) File.Delete(path2);
        }
    }

    [TestMethod]
    public void IsIdentical_WhenContentsDiffer_ReturnsFalse()
    {
        var path1 = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}_1.bin");
        var path2 = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}_2.bin");

        try
        {
            File.WriteAllBytes(path1, [1, 2, 3, 4]);
            File.WriteAllBytes(path2, [1, 2, 9, 4]);

            Assert.IsFalse(FileTools.IsIdentical(path1, path2));
        }
        finally
        {
            if (File.Exists(path1)) File.Delete(path1);
            if (File.Exists(path2)) File.Delete(path2);
        }
    }

    [TestMethod]
    public void IsIdentical_WhenBothFilesAreZeroLength_ReturnsTrue()
    {
        var path1 = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}_1.bin");
        var path2 = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}_2.bin");

        try
        {
            File.WriteAllBytes(path1, []);
            File.WriteAllBytes(path2, []);

            Assert.IsTrue(FileTools.IsIdentical(path1, path2));
        }
        finally
        {
            if (File.Exists(path1)) File.Delete(path1);
            if (File.Exists(path2)) File.Delete(path2);
        }
    }

    [TestMethod]
    public void IsIdentical_WhenOnePathIsDirectory_ReturnsFalse()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        var file = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.bin");

        Directory.CreateDirectory(folder);

        try
        {
            File.WriteAllBytes(file, [1, 2, 3]);

            Assert.IsFalse(FileTools.IsIdentical(folder, file));
            Assert.IsFalse(FileTools.IsIdentical(file, folder));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
            if (File.Exists(file)) File.Delete(file);
        }
    }
}
