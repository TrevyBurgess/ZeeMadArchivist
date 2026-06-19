using CyberFeedForward.TheMadArchivist.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace UnitTests.Services;

[TestClass]
public sealed class CommandBarSettingsServiceTests
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

        public void Clear()
        {
            _boolValues.Clear();
            _intValues.Clear();
            _stringValues.Clear();
        }
    }

    [TestMethod]
    public void IsCommandBarOnLeft_WhenNotSet_ReturnsTrue()
    {
        var store = new InMemorySettingsStore();
        var service = new CommandBarSettingsService(store);

        Assert.IsTrue(service.IsCommandBarOnLeft());
    }

    [TestMethod]
    public void SetCommandBarOnLeft_WhenSetToFalse_PersistsValue()
    {
        var store = new InMemorySettingsStore();
        var service = new CommandBarSettingsService(store);

        service.SetCommandBarOnLeft(false);

        Assert.IsFalse(service.IsCommandBarOnLeft());
    }

    [TestMethod]
    public void SetCommandBarOnLeft_WhenSetToTrue_PersistsValue()
    {
        var store = new InMemorySettingsStore();
        var service = new CommandBarSettingsService(store);

        service.SetCommandBarOnLeft(false);
        service.SetCommandBarOnLeft(true);

        Assert.IsTrue(service.IsCommandBarOnLeft());
    }
}
