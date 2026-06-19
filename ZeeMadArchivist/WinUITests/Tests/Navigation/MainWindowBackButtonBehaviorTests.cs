using CyberFeedForward.TheMadArchivist;
using CyberFeedForward.TheMadArchivist.Views.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Tests.Navigation;

[TestClass]
public sealed class MainWindowBackButtonBehaviorTests
{
    [TestMethod]
    public void MainWindow_BackStack_Exists_After_Navigating_To_Settings()
    {
        WinUiTestHelper.Run(() =>
        {
            var window = new MainWindow();

            var frame = (Microsoft.UI.Xaml.Controls.Frame)window.GetType()
                .GetField("MainFrame", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(window)!;

            Assert.IsNotNull(frame.Content);
            Assert.IsFalse(frame.CanGoBack);

            frame.Navigate(typeof(SettingsPage));

            Assert.IsTrue(frame.CanGoBack);
        });
    }
}
