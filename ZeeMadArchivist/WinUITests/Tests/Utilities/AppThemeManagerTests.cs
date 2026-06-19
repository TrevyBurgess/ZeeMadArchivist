using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Tests.Utilities;

[TestClass]
public sealed class AppThemeManagerTests
{
    [TestMethod]
    public void ApplyDarkMode_WhenEnabled_SetsRequestedThemeToDark()
    {
        WinUiTestHelper.Run(() =>
        {
            var root = new Grid();

            AppThemeManager.ApplyDarkMode(root, isDarkModeEnabled: true);

            Assert.AreEqual(ElementTheme.Dark, root.RequestedTheme);
        });
    }

    [TestMethod]
    public void ApplyDarkMode_WhenDisabled_SetsRequestedThemeToLight()
    {
        WinUiTestHelper.Run(() =>
        {
            var root = new Grid();

            AppThemeManager.ApplyDarkMode(root, isDarkModeEnabled: false);

            Assert.AreEqual(ElementTheme.Light, root.RequestedTheme);
        });
    }

    [TestMethod]
    public void ApplyThemeMode_WhenSystemDefault_SetsRequestedThemeToDefault()
    {
        WinUiTestHelper.Run(() =>
        {
            var root = new Grid();

            AppThemeManager.ApplyThemeMode(root, AppThemeMode.SystemDefault);

            Assert.AreEqual(ElementTheme.Default, root.RequestedTheme);
        });
    }
}
