using CyberFeedForward.TheMadArchivist.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace UnitTests.Services;

[TestClass]
public sealed class FirstRunServiceTests
{
    [TestMethod]
    public void ShouldRunFirstRunExperience_WhenCompletionFlagMissing_ReturnsTrue()
    {
        var service = FirstRunService.Instance;
        service.Store = new FakeAppSettingsStore();

        var result = service.ShouldRunFirstRunExperience();

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldRunFirstRunExperience_WhenCompletionFlagFalse_ReturnsTrue()
    {
        var store = new FakeAppSettingsStore();
        store.SetBool("App.FirstRun.Completed", false);

        var service = FirstRunService.Instance;
        service.Store = store;

        var result = service.ShouldRunFirstRunExperience();

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldRunFirstRunExperience_WhenCompletionFlagTrue_ReturnsFalse()
    {
        var store = new FakeAppSettingsStore();
        store.SetBool("App.FirstRun.Completed", true);

        var service = FirstRunService.Instance;
        service.Store = store;

        var result = service.ShouldRunFirstRunExperience();

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void MarkFirstRunExperienceCompleted_PersistsCompletionFlag()
    {
        var service = FirstRunService.Instance;
        service.Store = new FakeAppSettingsStore();

        service.MarkFirstRunExperienceCompleted();

        Assert.IsFalse(service.ShouldRunFirstRunExperience());
    }

    [TestMethod]
    public void ResetFirstRunExperience_PersistsIncompleteFlag()
    {
        var service = FirstRunService.Instance;
        service.Store = new FakeAppSettingsStore();

        service.MarkFirstRunExperienceCompleted();

        service.ResetFirstRunExperience();

        Assert.IsTrue(service.ShouldRunFirstRunExperience());
    }

    private sealed class FakeAppSettingsStore : IAppSettingsStore
    {
        private readonly Dictionary<string, bool> _boolValues = [];

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
    }
}
