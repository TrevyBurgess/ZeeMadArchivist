using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.ViewModels.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.ViewModels.Pages;

[TestClass]
public sealed class SettingsPageViewModelTests
{
    private sealed class FakeAppSettingsStore : IAppSettingsStore
    {
        private readonly System.Collections.Generic.Dictionary<string, bool> _boolValues = [];

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
            value = 0;
            return false;
        }

        public void SetInt(string key, int value)
        {
        }

        public bool TryGetString(string key, out string value)
        {
            value = string.Empty;
            return false;
        }

        public void SetString(string key, string value)
        {
        }

        public void Clear()
        {
            _boolValues.Clear();
        }
    }

    [TestMethod]
    public void TrySetStartupEnabled_WhenServiceSucceeds_ReturnsTrueAndUpdatesSetStartup()
    {
        var store = new FakeAppSettingsStore();
        var theme = new ThemeSettingsService(store);
        var cmd = new CommandBarSettingsService(store);

        var startup = new StartupSettingsService(
            getExecutablePath: () => "C:\\App\\ZeeMadArchivist.exe",
            tryReadRunValue: () => (null, false),
            writeRunValue: _ => { },
            deleteRunValue: () => { });

        var vm = new SettingsPageViewModel(theme, cmd, startup, themeRootElement: null, settingsStore: store);

        var ok = vm.TrySetStartupEnabled(true, out var errorMessage);

        Assert.IsTrue(ok);
        Assert.IsTrue(string.IsNullOrWhiteSpace(errorMessage));
    }

    [TestMethod]
    public void TrySetStartupEnabled_WhenServiceThrows_ReturnsFalseAndProvidesMessage()
    {
        var store = new FakeAppSettingsStore();
        var theme = new ThemeSettingsService(store);
        var cmd = new CommandBarSettingsService(store);

        var startup = new StartupSettingsService(
            getExecutablePath: () => "C:\\App\\ZeeMadArchivist.exe",
            tryReadRunValue: () => (null, false),
            writeRunValue: _ => throw new System.InvalidOperationException("no access"),
            deleteRunValue: () => { });

        var vm = new SettingsPageViewModel(theme, cmd, startup, themeRootElement: null, settingsStore: store);

        var ok = vm.TrySetStartupEnabled(true, out var errorMessage);

        Assert.IsFalse(ok);
        Assert.IsTrue(errorMessage?.Length > 0);
    }

    [TestMethod]
    public void SetStartup_WhenUserDisables_IsRememberedAcrossViewModelRecreation()
    {
        var store = new FakeAppSettingsStore();
        var theme = new ThemeSettingsService(store);
        var cmd = new CommandBarSettingsService(store);

        var startupEnabled = true;
        var startup = new StartupSettingsService(
            getExecutablePath: () => "C:\\App\\ZeeMadArchivist.exe",
            tryReadRunValue: () => (startupEnabled ? "C:\\App\\ZeeMadArchivist.exe" : null, startupEnabled),
            writeRunValue: _ => startupEnabled = true,
            deleteRunValue: () => startupEnabled = false);

        var first = new SettingsPageViewModel(theme, cmd, startup, themeRootElement: null, settingsStore: store);

        var ok = first.TrySetStartupEnabled(false, out var errorMessage);

        Assert.IsTrue(ok);
        Assert.IsTrue(string.IsNullOrWhiteSpace(errorMessage));
        Assert.IsFalse(first.SetStartup);

        var second = new SettingsPageViewModel(theme, cmd, startup, themeRootElement: null, settingsStore: store);

        Assert.IsFalse(second.SetStartup);
    }
}
