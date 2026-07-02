using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.ViewModels.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace UnitTests.ViewModels.Dialogs;

[TestClass]
public sealed class FirstRunCustomizationDialogViewModelTests
{
    private sealed class FakeAppSettingsStore : IAppSettingsStore
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
            if (_stringValues.TryGetValue(key, out var stored))
            {
                value = stored;
                return true;
            }

            value = string.Empty;
            return false;
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

    private static FirstRunCustomizationDialogViewModel CreateViewModel(
        IAppSettingsStore? store = null,
        FirstRunCustomizationDialogViewModel.GetUnusedDriveLettersDelegate? getUnusedDriveLetters = null,
        FirstRunCustomizationDialogViewModel.MapDriveDelegate? mapDrive = null,
        FirstRunCustomizationDialogViewModel.TrySetDriveIconDelegate? trySetDriveIcon = null,
        FirstRunCustomizationDialogViewModel.RegisterTagsPropertyPageDelegate? registerTagsPropertyPage = null)
    {
        store ??= new FakeAppSettingsStore();
        return new FirstRunCustomizationDialogViewModel(
            store,
            new ThemeSettingsService(store),
            new CommandBarSettingsService(store),
            new StartupSettingsService(
                getExecutablePath: () => "C:\\App\\ZeeMadArchivist.exe",
                tryReadRunValue: () => (null, false),
                writeRunValue: _ => { },
                deleteRunValue: () => { }),
            new ArchivesSettingsService(store),
            new CustomIconsSettingsService(store),
            getUnusedDriveLetters ?? (() => ['Z', 'Y']),
            mapDrive ?? ((path, letter, name) => 0),
            trySetDriveIcon ?? ((char letter, string iconPath, out string error) =>
            {
                error = null!;
                return true;
            }),
            registerTagsPropertyPage ?? (dllPath => true));
    }

    [TestMethod]
    public void LoadDefaults_SetsExpectedValues()
    {
        var viewModel = CreateViewModel();

        viewModel.LoadDefaults();

        Assert.AreEqual("Welcome to Zee Mad Archivist!", viewModel.Title);
        Assert.AreEqual(0, viewModel.ThemeModeIndex);
        Assert.AreEqual(AppThemeMode.SystemDefault, viewModel.ThemeMode);
        Assert.IsTrue(viewModel.IsCommandBarOnLeft);
        Assert.IsTrue(viewModel.SetStartup);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.InitialArchivePath));
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.InitialCustomIconsPath));
        Assert.AreEqual("Archive", viewModel.ArchiveName);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.SelectedIconPath));
        Assert.IsNull(viewModel.ErrorMessage);
        Assert.HasCount(2, viewModel.AvailableDriveLetters);
        Assert.AreEqual("Z:", viewModel.AvailableDriveLetters[0]);
        Assert.AreEqual("Z:", viewModel.SelectedDriveLetter);
        Assert.IsTrue(viewModel.RegisterTagsPropertyPage);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.TagsPropertyPageDllPath));
    }

    [TestMethod]
    public void TrySave_WhenRegisterTagsPropertyPageEnabled_CallsRegisterDelegate()
    {
        string? registeredPath = null;
        var store = new FakeAppSettingsStore();
        var viewModel = CreateViewModel(
            store,
            registerTagsPropertyPage: path =>
            {
                registeredPath = path;
                return true;
            });
        viewModel.LoadDefaults();
        viewModel.RegisterTagsPropertyPage = true;
        viewModel.TagsPropertyPageDllPath = "C:\\Tags\\ZeeMadArchivist.ShellExtension.dll";

        var result = viewModel.TrySave();

        Assert.IsTrue(result);
        Assert.AreEqual("C:\\Tags\\ZeeMadArchivist.ShellExtension.dll", registeredPath);
    }

    [TestMethod]
    public void TrySave_WhenRegisterTagsPropertyPageDisabled_DoesNotCallRegisterDelegate()
    {
        bool wasCalled = false;
        var store = new FakeAppSettingsStore();
        var viewModel = CreateViewModel(
            store,
            registerTagsPropertyPage: path =>
            {
                wasCalled = true;
                return true;
            });
        viewModel.LoadDefaults();
        viewModel.RegisterTagsPropertyPage = false;
        viewModel.TagsPropertyPageDllPath = "C:\\Tags\\ZeeMadArchivist.ShellExtension.dll";

        var result = viewModel.TrySave();

        Assert.IsTrue(result);
        Assert.IsFalse(wasCalled);
    }

    [TestMethod]
    public void TrySave_WhenRegisterTagsPropertyPageFails_SetsErrorMessage()
    {
        var store = new FakeAppSettingsStore();
        var viewModel = CreateViewModel(
            store,
            registerTagsPropertyPage: path => false);
        viewModel.LoadDefaults();
        viewModel.RegisterTagsPropertyPage = true;
        viewModel.TagsPropertyPageDllPath = "C:\\Tags\\ZeeMadArchivist.ShellExtension.dll";

        var result = viewModel.TrySave();

        Assert.IsTrue(result);
        Assert.IsFalse(viewModel.TagsRegistrationSucceeded);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.TagsRegistrationErrorMessage));
    }

    [TestMethod]
    public void ThemeModeIndex_ConvertsToThemeMode()
    {
        var viewModel = CreateViewModel();

        viewModel.ThemeModeIndex = 1;
        Assert.AreEqual(AppThemeMode.Light, viewModel.ThemeMode);

        viewModel.ThemeModeIndex = 2;
        Assert.AreEqual(AppThemeMode.Dark, viewModel.ThemeMode);

        viewModel.ThemeModeIndex = 0;
        Assert.AreEqual(AppThemeMode.SystemDefault, viewModel.ThemeMode);
    }

    [TestMethod]
    public void ThemeMode_ConvertsToThemeModeIndex()
    {
        var viewModel = CreateViewModel();

        viewModel.ThemeMode = AppThemeMode.Light;
        Assert.AreEqual(1, viewModel.ThemeModeIndex);

        viewModel.ThemeMode = AppThemeMode.Dark;
        Assert.AreEqual(2, viewModel.ThemeModeIndex);

        viewModel.ThemeMode = AppThemeMode.SystemDefault;
        Assert.AreEqual(0, viewModel.ThemeModeIndex);
    }

    [TestMethod]
    public void TrySave_WhenValid_PersistsSettings()
    {
        var store = new FakeAppSettingsStore();
        var viewModel = CreateViewModel(store);
        viewModel.LoadDefaults();
        viewModel.ThemeModeIndex = 2;
        viewModel.IsCommandBarOnLeft = false;
        viewModel.SetStartup = false;
        viewModel.InitialArchivePath = "C:\\Temp";
        viewModel.ArchiveName = "Archive";
        viewModel.InitialCustomIconsPath = "C:\\Temp\\Icons";

        var result = viewModel.TrySave();

        Assert.IsTrue(result);
        Assert.IsNull(viewModel.ErrorMessage);
        Assert.AreEqual(2, store.TryGetInt("Theme.Mode", out var themeMode) ? themeMode : -1);
        Assert.IsTrue(store.TryGetBool("Layout.CommandBarOnLeft", out var commandBarOnLeft) && !commandBarOnLeft);
        Assert.IsFalse(store.TryGetBool("Settings.SetStartup", out var startup) && startup);
        Assert.IsTrue(store.TryGetString("Archives.Paths", out var archivesJson));
        Assert.IsTrue(archivesJson.Contains("C:\\\\Temp\\\\Archive", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(store.TryGetString("CustomIcons.FolderPath", out var iconsPath));
        Assert.IsTrue(iconsPath.Contains("Temp", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(iconsPath.Contains("Icons", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TrySave_AppendsInitialArchivePathToExistingArchives()
    {
        var store = new FakeAppSettingsStore();
        store.SetString("Archives.Paths", "[\"C:\\\\Temp\\\\ExistingArchive\"]");
        var viewModel = CreateViewModel(store);
        viewModel.LoadDefaults();
        viewModel.InitialArchivePath = "C:\\Temp";
        viewModel.ArchiveName = "NewArchive";

        var result = viewModel.TrySave();

        Assert.IsTrue(result);
        Assert.IsTrue(store.TryGetString("Archives.Paths", out var archivesJson));
        Assert.IsTrue(archivesJson.Contains("ExistingArchive", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(archivesJson.Contains("NewArchive", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TrySave_WhenEmptyPaths_DoesNotPersistEmptyPaths()
    {
        var store = new FakeAppSettingsStore();
        var viewModel = CreateViewModel(store);
        viewModel.LoadDefaults();
        viewModel.InitialArchivePath = "   ";
        viewModel.ArchiveName = "   ";
        viewModel.InitialCustomIconsPath = "   ";

        var result = viewModel.TrySave();

        Assert.IsTrue(result);
        Assert.IsFalse(store.TryGetString("Archives.Paths", out _));
        Assert.IsFalse(store.TryGetString("CustomIcons.FolderPath", out _));
    }

    [TestMethod]
    public void TrySave_WhenStartupServiceThrows_SetsErrorMessage()
    {
        var store = new FakeAppSettingsStore();
        var startupService = new StartupSettingsService(
            getExecutablePath: () => "C:\\App\\ZeeMadArchivist.exe",
            tryReadRunValue: () => (null, false),
            writeRunValue: _ => throw new InvalidOperationException("Registry access denied"),
            deleteRunValue: () => { });
        var viewModel = new FirstRunCustomizationDialogViewModel(
            store,
            new ThemeSettingsService(store),
            new CommandBarSettingsService(store),
            startupService,
            new ArchivesSettingsService(store),
            new CustomIconsSettingsService(store));
        viewModel.LoadDefaults();

        var result = viewModel.TrySave();

        Assert.IsFalse(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.ErrorMessage));
    }

    [TestMethod]
    public void GetDefaultArchivePath_ReturnsPathUnderDocuments()
    {
        var path = FirstRunCustomizationDialogViewModel.GetDefaultArchivePath();

        Assert.IsFalse(string.IsNullOrWhiteSpace(path));
        Assert.IsTrue(path.EndsWith("ZeeMadArchivist", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void GetDefaultCustomIconsPath_ReturnsPathUnderDocuments()
    {
        var path = FirstRunCustomizationDialogViewModel.GetDefaultCustomIconsPath();

        Assert.IsFalse(string.IsNullOrWhiteSpace(path));
        Assert.IsTrue(path.EndsWith("CustomIcons", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TrySave_WhenMapDriveFails_SetsErrorMessage()
    {
        var store = new FakeAppSettingsStore();
        var viewModel = CreateViewModel(
            store,
            mapDrive: (path, letter, name) => 85);
        viewModel.LoadDefaults();
        viewModel.InitialArchivePath = "C:\\Temp";
        viewModel.ArchiveName = "Archive";

        var result = viewModel.TrySave();

        Assert.IsFalse(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.ErrorMessage));
    }

    [TestMethod]
    public void TrySave_WhenSetDriveIconFails_SetsErrorMessage()
    {
        var store = new FakeAppSettingsStore();
        var viewModel = CreateViewModel(
            store,
            trySetDriveIcon: (char letter, string iconPath, out string error) =>
            {
                error = "Icon file missing";
                return false;
            });
        viewModel.LoadDefaults();
        viewModel.InitialArchivePath = "C:\\Temp";
        viewModel.ArchiveName = "Archive";

        var result = viewModel.TrySave();

        Assert.IsFalse(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.ErrorMessage));
        Assert.IsTrue(viewModel.ErrorMessage.Contains("Icon file missing", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TrySave_WhenDriveLetterSelected_MapsDriveAndSetsIcon()
    {
        char mappedLetter = default;
        string? mappedPath = null;
        string? mappedName = null;
        char iconLetter = default;
        string? iconPath = null;
        var store = new FakeAppSettingsStore();
        var viewModel = CreateViewModel(
            store,
            mapDrive: (path, letter, name) =>
            {
                mappedPath = path;
                mappedLetter = letter;
                mappedName = name;
                return 0;
            },
            trySetDriveIcon: (char letter, string path, out string error) =>
            {
                iconLetter = letter;
                iconPath = path;
                error = null!;
                return true;
            });
        viewModel.LoadDefaults();
        viewModel.InitialArchivePath = "C:\\Temp";
        viewModel.ArchiveName = "Archive";
        viewModel.SelectedDriveLetter = "Z:";

        var result = viewModel.TrySave();

        Assert.IsTrue(result);
        Assert.AreEqual('Z', mappedLetter);
        Assert.AreEqual('Z', iconLetter);
        Assert.IsFalse(string.IsNullOrWhiteSpace(mappedPath));
        Assert.AreEqual("Archive", mappedName);
        Assert.IsFalse(string.IsNullOrWhiteSpace(iconPath));
    }
}
