using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.ViewModels.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace UnitTests.ViewModels.Controls;

[TestClass]
public sealed class ArchiveListControlViewModelNewArchiveTests
{
    private sealed class FakeAppSettingsStore : IAppSettingsStore
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
        var store = new FakeAppSettingsStore();
        store.SetBool("App.FirstRun.Completed", true);
        FirstRunServiceStoreField.SetValue(FirstRunService.Instance, store);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        FirstRunServiceStoreField.SetValue(FirstRunService.Instance, _originalFirstRunStore);
    }

    private static ArchiveListControlViewModel CreateViewModel(
        IAppSettingsStore? store = null,
        ArchiveListControlViewModel.MapDriveDelegate? mapDrive = null,
        ArchiveListControlViewModel.TrySetDriveIconDelegate? trySetDriveIcon = null)
    {
        store ??= new FakeAppSettingsStore();
        return new ArchiveListControlViewModel(
            new ArchivesSettingsService(store),
            _ => true,
            mapDrive,
            trySetDriveIcon);
    }

    [TestMethod]
    public void TryCreateNewArchive_WhenSuccessful_ReturnsTrueAndAddsArchive()
    {
        var store = new FakeAppSettingsStore();
        var vm = CreateViewModel(
            store,
            (path, letter, name) => 0,
            (char letter, out string error) =>
            {
                error = null!;
                return true;
            });

        var result = vm.TryCreateNewArchive("C:\\Temp\\Archive1", 'Z', out var errorMessage);

        Assert.IsTrue(result);
        Assert.IsNull(errorMessage);
        Assert.IsTrue(vm.Archives.Contains("C:\\Temp\\Archive1"));
        Assert.IsTrue(store.TryGetString("Archives.Paths", out var stored));
        Assert.IsTrue(stored.Contains("Archive1", System.StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TryCreateNewArchive_WhenMapDriveFails_ReturnsFalseWithError()
    {
        var vm = CreateViewModel(
            mapDrive: (path, letter, name) => 85,
            trySetDriveIcon: (char letter, out string error) =>
            {
                error = null!;
                return true;
            });

        var result = vm.TryCreateNewArchive("C:\\Temp\\Archive1", 'Z', out var errorMessage);

        Assert.IsFalse(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(errorMessage));
    }

    [TestMethod]
    public void TryCreateNewArchive_WhenIconSetFails_ReturnsFalseWithError()
    {
        var vm = CreateViewModel(
            mapDrive: (path, letter, name) => 0,
            trySetDriveIcon: (char letter, out string error) =>
            {
                error = "Icon file missing";
                return false;
            });

        var result = vm.TryCreateNewArchive("C:\\Temp\\Archive1", 'Z', out var errorMessage);

        Assert.IsFalse(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(errorMessage));
        Assert.IsTrue(errorMessage.Contains("Icon file missing", System.StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TryCreateNewArchive_WhenArchiveAlreadyExists_ReturnsFalse()
    {
        var vm = CreateViewModel(
            mapDrive: (path, letter, name) => 0,
            trySetDriveIcon: (char letter, out string error) =>
            {
                error = null!;
                return true;
            });

        vm.TryCreateNewArchive("C:\\Temp\\Archive1", 'Z', out _);
        var result = vm.TryCreateNewArchive("C:\\Temp\\Archive1", 'Y', out var errorMessage);

        Assert.IsFalse(result);
        Assert.IsNull(errorMessage);
    }

    [TestMethod]
    public void TryCreateNewArchive_WhenFolderPathIsEmpty_ReturnsFalseWithError()
    {
        var vm = CreateViewModel(
            mapDrive: (path, letter, name) => 0,
            trySetDriveIcon: (char letter, out string error) =>
            {
                error = null!;
                return true;
            });

        var result = vm.TryCreateNewArchive("   ", 'Z', out var errorMessage);

        Assert.IsFalse(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(errorMessage));
    }

    [TestMethod]
    public void TryCreateNewArchive_WhenMapDriveThrows_ReturnsFalseWithError()
    {
        var vm = CreateViewModel(
            mapDrive: (path, letter, name) => throw new System.InvalidOperationException("Drive mapping unavailable"),
            trySetDriveIcon: (char letter, out string error) =>
            {
                error = null!;
                return true;
            });

        var result = vm.TryCreateNewArchive("C:\\Temp\\Archive1", 'Z', out var errorMessage);

        Assert.IsFalse(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(errorMessage));
        Assert.IsTrue(errorMessage.Contains("Drive mapping unavailable", System.StringComparison.OrdinalIgnoreCase));
    }
}
