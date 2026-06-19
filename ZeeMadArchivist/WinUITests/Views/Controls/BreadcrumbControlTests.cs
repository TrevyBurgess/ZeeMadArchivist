using CyberFeedForward.TheMadArchivist.Views.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace UnitTests.Views.Controls;

[TestClass]
public sealed class BreadcrumbControlTests
{
    [TestMethod]
    public void SettingFolderPath_SetsTextToLastSegment()
    {
        WinUiTestHelper.Run(() =>
        {
            var control = new Breadcrumb
            {
                FolderPath = "C:\\Temp\\MyFolder\\"
            };

            Assert.AreEqual("MyFolder", control.Text);
        });
    }

    [TestMethod]
    public void SettingFolderPath_ToNull_SetsTextToEmpty()
    {
        WinUiTestHelper.Run(() =>
        {
            var control = new Breadcrumb
            {
                FolderPath = null
            };

            Assert.AreEqual(string.Empty, control.Text);
        });
    }

    [TestMethod]
    public void Items_CanBeSetAndRetrieved()
    {
        WinUiTestHelper.Run(() =>
        {
            var control = new Breadcrumb();
            var items = new List<string> { "A", "B" };

            control.Items = items;

            Assert.AreSame(items, control.Items);
        });
    }
}
