using CyberFeedForward.TheMadArchivist.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace UnitTests.Services;

[TestClass]
public sealed class ArchivesSettingsServiceTests
{
    private sealed class InMemorySettingsStore : IAppSettingsStore
    {
        private readonly Dictionary<string, bool> _boolValues = [];
        private readonly Dictionary<string, int> _intValues = [];
        private readonly Dictionary<string, string> _stringValues = [];

        public bool TryGetBool(string key, out bool value) => _boolValues.TryGetValue(key, out value);

        public void SetBool(string key, bool value) => _boolValues[key] = value;

        public bool TryGetInt(string key, out int value) => _intValues.TryGetValue(key, out value);

        public void SetInt(string key, int value) => _intValues[key] = value;

        public bool TryGetString(string key, out string value) => _stringValues.TryGetValue(key, out value!);

        public void SetString(string key, string value) => _stringValues[key] = value;
    }

    [TestMethod]
    public void GetArchives_WhenUnset_ReturnsEmpty()
    {
        var store = new InMemorySettingsStore();
        var service = new ArchivesSettingsService(store);

        var archives = service.GetArchives();

        Assert.IsEmpty(archives);
    }

    [TestMethod]
    public void SaveArchives_ThenGetArchives_RoundTripsAndDedupes()
    {
        var store = new InMemorySettingsStore();
        var service = new ArchivesSettingsService(store);

        service.SaveArchives(
        [
            "C:\\Temp\\A.zip",
            " C:\\Temp\\A.zip ",
            "C:\\Temp\\B.zip",
            "",
        ]);

        var archives = service.GetArchives();

        Assert.HasCount(2, archives);
        Assert.AreEqual("C:\\Temp\\A.zip", archives[0]);
        Assert.AreEqual("C:\\Temp\\B.zip", archives[1]);
    }
}
