using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace UnitTests.Tools;

[TestClass]
public sealed class ImageToolsTests
{
    [TestMethod]
    public void ToIcon_WhenPathIsInvalid_Throws()
    {
        try
        {
            ImageTools.ToIcon("");
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException)
        {
        }
    }

    [TestMethod]
    public void ToIcon_WhenFileDoesNotExist_Throws()
    {
        try
        {
            ImageTools.ToIcon("C:\\this-file-should-not-exist-12345.png");
            Assert.Fail("Expected FileNotFoundException was not thrown.");
        }
        catch (FileNotFoundException)
        {
        }
    }

    [TestMethod]
    public void ToIcon_WhenFileExists_ReturnsIcon()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}.png");

        try
        {
            using (var bmp = new Bitmap(32, 32))
            {
                using var g = Graphics.FromImage(bmp);
                g.Clear(Color.Red);
                bmp.Save(tempPath, ImageFormat.Png);
            }

            using var icon = ImageTools.ToIcon(tempPath);

            Assert.IsNotNull(icon);
            Assert.IsGreaterThan(0, icon.Width);
            Assert.IsGreaterThan(0, icon.Height);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
