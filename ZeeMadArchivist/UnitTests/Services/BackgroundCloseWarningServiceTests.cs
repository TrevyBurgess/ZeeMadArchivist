using CyberFeedForward.TheMadArchivist.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace UnitTests.Services;

[TestClass]
public sealed class BackgroundCloseWarningServiceTests
{
    private sealed class FakeAppSettingsStore : IAppSettingsStore
    {
        private readonly System.Collections.Generic.Dictionary<string, object?> _values = new(StringComparer.Ordinal);

        public bool TryGetBool(string key, out bool value)
        {
            if (_values.TryGetValue(key, out var stored) && stored is bool b)
            {
                value = b;
                return true;
            }

            value = false;
            return false;
        }

        public void SetBool(string key, bool value)
        {
            _values[key] = value;
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
            _values.Clear();
        }
    }

    [TestMethod]
    public void ShouldShowWarning_WhenNeverWarned_ReturnsTrue()
    {
        var store = new FakeAppSettingsStore();
        var svc = new BackgroundCloseWarningService(store);

        Assert.IsTrue(svc.ShouldShowWarning());
    }

    [TestMethod]
    public void ShouldShowWarning_AfterMarkWarned_ReturnsFalse()
    {
        var store = new FakeAppSettingsStore();
        var svc = new BackgroundCloseWarningService(store);

        svc.MarkWarned();

        Assert.IsFalse(svc.ShouldShowWarning());
    }
}
