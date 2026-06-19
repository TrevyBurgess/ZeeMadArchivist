using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.ViewModels.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace UnitTests.ViewModels.Controls;

[TestClass]
public sealed class ArchiveListControlViewModelTests
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

        public void Clear()
        {
            _boolValues.Clear();
            _intValues.Clear();
            _stringValues.Clear();
        }
    }

    [TestMethod]
    public void AddArchive_PersistsToStore()
    {
        var store = new InMemorySettingsStore();
        var service = new ArchivesSettingsService(store);
        var vm = new ArchiveListControlViewModel(service, _ => true)
        {
            NewArchivePath = "C:\\Temp\\Archive1.zip"
        };
        vm.AddArchive();

        Assert.HasCount(2, vm.Archives);
        Assert.IsTrue(store.TryGetString("Archives.Paths", out var stored));
        Assert.IsFalse(string.IsNullOrWhiteSpace(stored));
    }

    [TestMethod]
    public void AddArchive_WhenDuplicate_Ignores()
    {
        var store = new InMemorySettingsStore();
        var service = new ArchivesSettingsService(store);
        var vm = new ArchiveListControlViewModel(service, _ => true)
        {
            NewArchivePath = "C:\\Temp\\Archive1.zip"
        };
        vm.AddArchive();

        vm.NewArchivePath = "c:\\temp\\archive1.zip";
        vm.AddArchive();

        Assert.HasCount(2, vm.Archives);
    }

    [TestMethod]
    public void IsExistingArchive_WhenNotPresent_ReturnsFalse()
    {
        var store = new InMemorySettingsStore();
        var service = new ArchivesSettingsService(store);
        var vm = new ArchiveListControlViewModel(service, _ => true);

        Assert.IsFalse(vm.IsExistingArchive("C:\\Temp\\DoesNotExist"));
    }

    [TestMethod]
    public void IsExistingArchive_WhenPresent_ReturnsTrue_IgnoresCaseAndWhitespace()
    {
        var store = new InMemorySettingsStore();
        var service = new ArchivesSettingsService(store);
        var vm = new ArchiveListControlViewModel(service, _ => true)
        {
            NewArchivePath = "C:\\Temp\\Archive1"
        };
        vm.AddArchive();

        Assert.IsTrue(vm.IsExistingArchive("  c:\\temp\\archive1  "));
    }

    [TestMethod]
    public void IsAddEnabled_WhenPathEmpty_IsFalse_WhenNotEmpty_IsTrue()
    {
        var store = new InMemorySettingsStore();
        var service = new ArchivesSettingsService(store);
        var vm = new ArchiveListControlViewModel(service, _ => true)
        {
            NewArchivePath = ""
        };
        Assert.IsFalse(vm.IsAddEnabled);

        vm.NewArchivePath = "C:\\Temp\\SomeFolder";
        Assert.IsTrue(vm.IsAddEnabled);
    }

    [TestMethod]
    public void TryAddFolderPath_WhenClearNewArchivePathOnSuccessFalse_DoesNotChangeNewArchivePath()
    {
        var store = new InMemorySettingsStore();
        var service = new ArchivesSettingsService(store);
        var vm = new ArchiveListControlViewModel(service, _ => true)
        {
            NewArchivePath = "C:\\Temp\\UserTyped"
        };

        var result = vm.TryAddFolderPath("C:\\Temp\\PickedFolder", clearNewArchivePathOnSuccess: false);

        Assert.AreEqual(ArchiveListControlViewModel.ArchiveAddResult.Added, result);
        Assert.AreEqual("C:\\Temp\\UserTyped", vm.NewArchivePath);
        CollectionAssert.Contains(vm.Archives, "C:\\Temp\\PickedFolder");
    }

    [TestMethod]
    public void RemoveArchive_WhenPresent_Removes()
    {
        var store = new InMemorySettingsStore();
        var service = new ArchivesSettingsService(store);
        var vm = new ArchiveListControlViewModel(service, _ => true)
        {
            NewArchivePath = "C:\\Temp\\Archive1"
        };
        vm.AddArchive();
        vm.NewArchivePath = "C:\\Temp\\Archive2";
        vm.AddArchive();

        var removed = vm.RemoveArchive("C:\\Temp\\Archive1");

        Assert.IsTrue(removed);
        Assert.HasCount(2, vm.Archives);
        CollectionAssert.DoesNotContain(vm.Archives, "C:\\Temp\\Archive1");
        CollectionAssert.Contains(vm.Archives, "C:\\Temp\\Archive2");

        var rootPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        var defaultArchivePath = Path.Combine(rootPath, "MyArchive");
        CollectionAssert.Contains(vm.Archives, defaultArchivePath);
    }

    [TestMethod]
    public void Constructor_WhenNoArchivesStored_DefaultsToDocumentsFolder()
    {
        var store = new InMemorySettingsStore();
        var service = new ArchivesSettingsService(store);

        var rootPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        var expected = Path.Combine(rootPath, "MyArchive");
        var vm = new ArchiveListControlViewModel(service, p => string.Equals(p, expected, System.StringComparison.OrdinalIgnoreCase));

        Assert.HasCount(1, vm.Archives);
        Assert.AreEqual(expected, vm.Archives[0]);
    }
}
