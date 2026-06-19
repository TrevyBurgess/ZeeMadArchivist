using CyberFeedForward.TheMadArchivist.Views.Pages;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Tests.Navigation;

[TestClass]
public sealed class SettingsNavigationTests
{
    [TestMethod]
    public void Navigate_To_SettingsPage_Succeeds()
    {
        WinUiTestHelper.Run(() =>
        {
            var frame = new Frame();

            var navigated = frame.Navigate(typeof(SettingsPage));

            Assert.IsTrue(navigated);
            Assert.IsNotNull(frame.Content);
            Assert.IsInstanceOfType<SettingsPage>(frame.Content);
        });
    }
}
