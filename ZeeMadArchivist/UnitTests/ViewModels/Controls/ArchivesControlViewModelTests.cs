using CyberFeedForward.TheMadArchivist.Models;
using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.ViewModels.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

    private static readonly System.Reflection.FieldInfo FirstRunServiceStoreField = typeof(FirstRunService).GetField("_store", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

    private IAppSettingsStore? _originalFirstRunStore;

    [TestInitialize]
    public void TestInitialize()
    {
        _originalFirstRunStore = (IAppSettingsStore?)FirstRunServiceStoreField.GetValue(FirstRunService.Instance);
        var store = new InMemorySettingsStore();
        store.SetBool("App.FirstRun.Completed", true);
        FirstRunServiceStoreField.SetValue(FirstRunService.Instance, store);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        FirstRunServiceStoreField.SetValue(FirstRunService.Instance, _originalFirstRunStore);
    }

    private static ArchiveListControlViewModel CreateViewModel(
        InMemorySettingsStore store,
        Func<string, bool>? directoryExists = null,
        ArchiveListControlViewModel.UnmapDriveForPathDelegate? unmapDriveForPath = null)
    {
        return new ArchiveListControlViewModel(
            new ArchivesSettingsService(store),
            directoryExists ?? (_ => true),
            unmapDriveForPath: unmapDriveForPath ?? (_ => true));
    }

    [TestMethod]
    public void AddArchive_PersistsToStore()
    {
        var store = new InMemorySettingsStore();
        var vm = CreateViewModel(store);
        vm.NewArchivePath = "C:\\Temp\\Archive1.zip";
        vm.AddArchive();

        Assert.HasCount(2, vm.Archives);
        Assert.IsTrue(store.TryGetString("Archives.Paths", out var stored));
        Assert.IsFalse(string.IsNullOrWhiteSpace(stored));
    }

    [TestMethod]
    public void AddArchive_WhenDuplicate_Ignores()
    {
        var store = new InMemorySettingsStore();
        var vm = CreateViewModel(store);
        vm.NewArchivePath = "C:\\Temp\\Archive1.zip";
        vm.AddArchive();

        vm.NewArchivePath = "c:\\temp\\archive1.zip";
        vm.AddArchive();

        Assert.HasCount(2, vm.Archives);
    }

    [TestMethod]
    public void IsExistingArchive_WhenNotPresent_ReturnsFalse()
    {
        var store = new InMemorySettingsStore();
        var vm = CreateViewModel(store);

        Assert.IsFalse(vm.IsExistingArchive("C:\\Temp\\DoesNotExist"));
    }

    [TestMethod]
    public void IsExistingArchive_WhenPresent_ReturnsTrue_IgnoresCaseAndWhitespace()
    {
        var store = new InMemorySettingsStore();
        var vm = CreateViewModel(store);
        vm.NewArchivePath = "C:\\Temp\\Archive1";
        vm.AddArchive();

        Assert.IsTrue(vm.IsExistingArchive("  c:\\temp\\archive1  "));
    }

    [TestMethod]
    public void IsAddEnabled_WhenPathEmpty_IsFalse_WhenNotEmpty_IsTrue()
    {
        var store = new InMemorySettingsStore();
        var vm = CreateViewModel(store);
        vm.NewArchivePath = "";
        Assert.IsFalse(vm.IsAddEnabled);

        vm.NewArchivePath = "C:\\Temp\\SomeFolder";
        Assert.IsTrue(vm.IsAddEnabled);
    }

    [TestMethod]
    public void TryAddFolderPath_WhenClearNewArchivePathOnSuccessFalse_DoesNotChangeNewArchivePath()
    {
        var store = new InMemorySettingsStore();
        var vm = CreateViewModel(store);
        vm.NewArchivePath = "C:\\Temp\\UserTyped";

        var result = vm.TryAddFolderPath("C:\\Temp\\PickedFolder", clearNewArchivePathOnSuccess: false);

        Assert.AreEqual(ArchiveListControlViewModel.ArchiveAddResult.Added, result);
        Assert.AreEqual("C:\\Temp\\UserTyped", vm.NewArchivePath);
        Assert.IsTrue(vm.Archives.Any(a => string.Equals(a.Path, "C:\\Temp\\PickedFolder", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void RemoveArchive_WhenPresent_Removes()
    {
        var store = new InMemorySettingsStore();
        var vm = CreateViewModel(store);
        vm.NewArchivePath = "C:\\Temp\\Archive1";
        vm.AddArchive();
        vm.NewArchivePath = "C:\\Temp\\Archive2";
        vm.AddArchive();

        var removed = vm.RemoveArchive("C:\\Temp\\Archive1");

        Assert.IsTrue(removed);
        Assert.HasCount(2, vm.Archives);
        Assert.IsFalse(vm.Archives.Any(a => string.Equals(a.Path, "C:\\Temp\\Archive1", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(vm.Archives.Any(a => string.Equals(a.Path, "C:\\Temp\\Archive2", StringComparison.OrdinalIgnoreCase)));

        var rootPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        var defaultArchivePath = Path.Combine(rootPath, "MyArchive");
        Assert.IsTrue(vm.Archives.Any(a => string.Equals(a.Path, defaultArchivePath, StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Constructor_WhenNoArchivesStored_DefaultsToDocumentsFolder()
    {
        var store = new InMemorySettingsStore();
        var rootPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        var expected = Path.Combine(rootPath, "MyArchive");
        var vm = CreateViewModel(store, directoryExists: p => string.Equals(p, expected, System.StringComparison.OrdinalIgnoreCase));

        Assert.HasCount(1, vm.Archives);
        Assert.AreEqual(expected, vm.Archives[0].Path);
    }

    [TestMethod]
    public void RemoveArchive_WhenMappedDrive_UnmapsDrive()
    {
        var store = new InMemorySettingsStore();
        var unmappedPath = string.Empty;
        var vm = CreateViewModel(
            store,
            unmapDriveForPath: path =>
            {
                unmappedPath = path;
                return true;
            });
        vm.NewArchivePath = "C:\\Temp\\Archive1";
        vm.AddArchive();

        vm.RemoveArchive("C:\\Temp\\Archive1");

        Assert.AreEqual("C:\\Temp\\Archive1", unmappedPath);
        Assert.IsFalse(vm.Archives.Any(a => string.Equals(a.Path, "C:\\Temp\\Archive1", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void RemoveSelectedArchive_WhenMappedDrive_UnmapsDrive()
    {
        var store = new InMemorySettingsStore();
        var unmappedPath = string.Empty;
        var vm = CreateViewModel(
            store,
            unmapDriveForPath: path =>
            {
                unmappedPath = path;
                return true;
            });
        vm.NewArchivePath = "C:\\Temp\\Archive1";
        vm.AddArchive();
        vm.SelectedArchive = vm.Archives.First(a => string.Equals(a.Path, "C:\\Temp\\Archive1", StringComparison.OrdinalIgnoreCase));

        vm.RemoveSelectedArchive();

        Assert.AreEqual("C:\\Temp\\Archive1", unmappedPath);
        Assert.IsFalse(vm.Archives.Any(a => string.Equals(a.Path, "C:\\Temp\\Archive1", StringComparison.OrdinalIgnoreCase)));
    }
}
