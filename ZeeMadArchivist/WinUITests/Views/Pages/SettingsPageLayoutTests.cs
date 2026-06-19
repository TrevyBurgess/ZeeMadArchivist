using CyberFeedForward.TheMadArchivist.Views.Pages;
using CyberFeedForward.TheMadArchivist.Views.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Views.Pages;

[TestClass]
public sealed class SettingsPageLayoutTests
{
    [TestMethod]
    public void SettingsPage_HasSettingsGroupsTabViewWithThreeTabs()
    {
        WinUiTestHelper.Run(() =>
        {
            var page = new SettingsPage();

            var tabView = (TabView)page.FindName("SettingsGroups");
            Assert.IsNotNull(tabView);

            var generalTab = (TabViewItem)page.FindName("GeneralSettingsTab");
            Assert.IsNotNull(generalTab);
            Assert.AreEqual("General", generalTab.Header);

            var archivesTab = (TabViewItem)page.FindName("ArchivesSettingsTab");
            Assert.IsNotNull(archivesTab);
            Assert.AreEqual("Archives", archivesTab.Header);

            var iconsTab = (TabViewItem)page.FindName("IconsSettingsTab");
            Assert.IsNotNull(iconsTab);
            Assert.AreEqual("Icons", iconsTab.Header);

            var namedIconControl = (NamedIconControl)page.FindName("NamedIconControl");
            Assert.IsNotNull(namedIconControl);

            Assert.HasCount(3, tabView.TabItems);
        });
    }
}
