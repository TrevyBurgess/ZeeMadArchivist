using CyberFeedForward.TheMadArchivist.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace UnitTests.Services;

[TestClass]
public sealed class WindowPlacementSettingsServiceTests
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
    public void TryGetPlacement_WhenNotSet_ReturnsFalse()
    {
        var store = new InMemorySettingsStore();
        var service = new WindowPlacementSettingsService(store);

        Assert.IsFalse(service.TryGetPlacement(out _));
    }

    [TestMethod]
    public void SavePlacement_ThenTryGetPlacement_RoundTrips()
    {
        var store = new InMemorySettingsStore();
        var service = new WindowPlacementSettingsService(store);

        var placement = new WindowPlacement(10, 20, 800, 600);
        service.SavePlacement(placement);

        Assert.IsTrue(service.TryGetPlacement(out var restored));
        Assert.AreEqual(placement, restored);
    }

    [TestMethod]
    public void SavePlacement_WhenWidthIsInvalid_Throws()
    {
        var store = new InMemorySettingsStore();
        var service = new WindowPlacementSettingsService(store);

        try
        {
            service.SavePlacement(new WindowPlacement(0, 0, 0, 600));
            Assert.Fail("Expected ArgumentOutOfRangeException was not thrown.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }

    [TestMethod]
    public void SavePlacement_WhenHeightIsInvalid_Throws()
    {
        var store = new InMemorySettingsStore();
        var service = new WindowPlacementSettingsService(store);

        try
        {
            service.SavePlacement(new WindowPlacement(0, 0, 800, 0));
            Assert.Fail("Expected ArgumentOutOfRangeException was not thrown.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }
}
