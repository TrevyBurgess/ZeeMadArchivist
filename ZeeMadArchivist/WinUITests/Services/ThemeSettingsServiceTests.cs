using CyberFeedForward.TheMadArchivist.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace UnitTests.Services;

[TestClass]
public sealed class ThemeSettingsServiceTests
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
    public void IsDarkModeEnabled_WhenNotSet_ReturnsFalse()
    {
        var store = new InMemorySettingsStore();
        var service = new ThemeSettingsService(store);

        Assert.IsFalse(service.IsDarkModeEnabled());
    }

    [TestMethod]
    public void GetThemeMode_WhenNotSet_ReturnsSystemDefault()
    {
        var store = new InMemorySettingsStore();
        var service = new ThemeSettingsService(store);

        Assert.AreEqual(AppThemeMode.SystemDefault, service.GetThemeMode());
    }

    [TestMethod]
    public void SetThemeMode_WhenSetToDark_PersistsValue()
    {
        var store = new InMemorySettingsStore();
        var service = new ThemeSettingsService(store);

        service.SetThemeMode(AppThemeMode.Dark);

        Assert.AreEqual(AppThemeMode.Dark, service.GetThemeMode());
        Assert.IsTrue(service.IsDarkModeEnabled());
    }

    [TestMethod]
    public void SetThemeMode_WhenSetToLight_PersistsValue()
    {
        var store = new InMemorySettingsStore();
        var service = new ThemeSettingsService(store);

        service.SetThemeMode(AppThemeMode.Light);

        Assert.AreEqual(AppThemeMode.Light, service.GetThemeMode());
        Assert.IsFalse(service.IsDarkModeEnabled());
    }

    [TestMethod]
    public void SetDarkModeEnabled_WhenSetToTrue_PersistsValue()
    {
        var store = new InMemorySettingsStore();
        var service = new ThemeSettingsService(store);

        service.SetDarkModeEnabled(true);

        Assert.IsTrue(service.IsDarkModeEnabled());
    }

    [TestMethod]
    public void SetDarkModeEnabled_WhenSetToFalse_PersistsValue()
    {
        var store = new InMemorySettingsStore();
        var service = new ThemeSettingsService(store);

        service.SetDarkModeEnabled(true);
        service.SetDarkModeEnabled(false);

        Assert.IsFalse(service.IsDarkModeEnabled());
    }
}
