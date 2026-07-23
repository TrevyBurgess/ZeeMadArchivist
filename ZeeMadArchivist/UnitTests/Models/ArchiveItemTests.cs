using CyberFeedForward.TheMadArchivist.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.ComponentModel;

namespace UnitTests.Models;

[TestClass]
public sealed class ArchiveItemTests
{
    [TestMethod]
    public void CustomIconPath_WhenSet_UpdatesIconPathAndRaisesPropertyChanged()
    {
        var item = new ArchiveItem("C:\\Archives\\Archive1");
        var raisedProperties = new List<string?>();
        item.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName);

        item.CustomIconPath = "C:\\Icons\\Movies.ico";

        Assert.AreEqual("C:\\Icons\\Movies.ico", item.CustomIconPath);
        Assert.AreEqual("C:\\Icons\\Movies.ico", item.IconPath);
        Assert.IsTrue(raisedProperties.Contains("CustomIconPath"));
        Assert.IsTrue(raisedProperties.Contains("IconPath"));
    }

    [TestMethod]
    public void CustomIconPath_WhenSetToSameValue_DoesNotRaisePropertyChanged()
    {
        var item = new ArchiveItem("C:\\Archives\\Archive1")
        {
            CustomIconPath = "C:\\Icons\\Movies.ico",
        };

        var raised = false;
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ArchiveItem.IconPath))
            {
                raised = true;
            }
        };

        item.CustomIconPath = "C:\\Icons\\Movies.ico";

        Assert.IsFalse(raised);
    }

    [TestMethod]
    public void RefreshDriveLetter_DoesNotOverwriteCustomIconPath()
    {
        var item = new ArchiveItem("C:\\Archives\\Archive1")
        {
            CustomIconPath = "C:\\Icons\\Movies.ico",
        };

        item.RefreshDriveLetter();

        Assert.AreEqual("C:\\Icons\\Movies.ico", item.CustomIconPath);
    }

    [TestMethod]
    public void DisplayText_WhenDriveLetterEmpty_ReturnsName()
    {
        var item = new ArchiveItem("C:\\Archives\\Archive1");

        Assert.IsTrue(string.IsNullOrEmpty(item.DriveLetter));
        Assert.AreEqual("Archive1", item.DisplayText);
    }

    [TestMethod]
    public void Name_IsDerivedFromPath()
    {
        var item = new ArchiveItem("C:\\Archives\\Archive1");

        Assert.AreEqual("Archive1", item.Name);
    }
}
