using CyberFeedForward.TheMadArchivist.Views.Controls;
using CyberFeedForward.TheMadArchivist.Views.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Views.Pages;

[TestClass]
public sealed class HomePageLayoutTests
{
    [TestMethod]
    public void HomePage_HasMovableDividerBetweenPanels()
    {
        WinUiTestHelper.Run(() =>
        {
            var page = new HomePage();

            var divider = (ResizeCursorBorder)page.FindName("FolderContentsDivider");
            Assert.IsNotNull(divider);
            Assert.IsNotNull(divider.ResizeCursor);
        });
    }
}
