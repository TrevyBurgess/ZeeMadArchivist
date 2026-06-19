using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.ViewModels.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace UnitTests.ViewModels;

[TestClass]
public sealed class SettingsPageViewModelTests
{
    private sealed class InMemorySettingsStore : IAppSettingsStore
    {
        private readonly Dictionary<string, bool> _boolValues = [];
        private readonly Dictionary<string, int> _intValues = [];
        private readonly Dictionary<string, string> _stringValues = [];

        public bool TryGetBool(string key, out bool value)
        {
            return _boolValues.TryGetValue(key, out value);
        }

        public void SetBool(string key, bool value)
        {
            _boolValues[key] = value;
        }

        public bool TryGetInt(string key, out int value)
        {
            return _intValues.TryGetValue(key, out value);
        }

        public void SetInt(string key, int value)
        {
            _intValues[key] = value;
        }

        public bool TryGetString(string key, out string value)
        {
            return _stringValues.TryGetValue(key, out value!);
        }

        public void SetString(string key, string value)
        {
            _stringValues[key] = value;
        }
    }

    [TestMethod]
    public void IsDarkModeEnabled_WhenSet_UpdatesRootElementTheme()
    {
        WinUiTestHelper.Run(() =>
        {
            var store = new InMemorySettingsStore();
            var service = new ThemeSettingsService(store);
            var commandBarSettings = new CommandBarSettingsService(store);
            var root = new Grid();

            var viewModel = new SettingsPageViewModel(service, commandBarSettings, root)
            {
                ThemeMode = AppThemeMode.Dark
            };

            Assert.AreEqual(ElementTheme.Dark, root.RequestedTheme);
        });
    }

    [TestMethod]
    public void ThemeMode_WhenSetToSystemDefault_UpdatesRootElementTheme()
    {
        WinUiTestHelper.Run(() =>
        {
            var store = new InMemorySettingsStore();
            var service = new ThemeSettingsService(store);
            var commandBarSettings = new CommandBarSettingsService(store);
            var root = new Grid();

            var viewModel = new SettingsPageViewModel(service, commandBarSettings, root)
            {
                ThemeMode = AppThemeMode.SystemDefault
            };

            Assert.AreEqual(ElementTheme.Default, root.RequestedTheme);
        });
    }
}
