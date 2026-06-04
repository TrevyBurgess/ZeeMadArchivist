using CyberFeedForward.TheMadArchivist.ViewModels.Controls;
using CyberFeedForward.TheMadArchivist.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace UnitTests.ViewModels.Controls;

[TestClass]
public sealed class NamedIconControlViewModelTests
{
    private sealed class FakeAppSettingsStore : IAppSettingsStore
    {
        private readonly System.Collections.Generic.Dictionary<string, object?> _values = new(StringComparer.Ordinal);

        public bool TryGetBool(string key, out bool value)
        {
            value = false;
            return false;
        }

        public void SetBool(string key, bool value)
        {
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
            if (_values.TryGetValue(key, out var stored) && stored is string s)
            {
                value = s;
                return true;
            }

            value = string.Empty;
            return false;
        }

        public void SetString(string key, string value)
        {
            _values[key] = value;
        }
    }

    [TestMethod]
    public void PropertyChanged_HasExpectedHandlerType()
    {
        var vm = new NamedIconControlViewModel();
        Assert.IsNotNull(vm as INotifyPropertyChanged);
    }

    [TestMethod]
    public void LoadFromJsonFile_FileMissing_DoesNotThrow_AndLeavesItemsEmpty()
    {
        var vm = new NamedIconControlViewModel(
            fileExists: _ => false,
            readAllText: _ => throw new InvalidOperationException("Should not read"));

        vm.LoadFromJsonFile("C:\\does-not-exist.json");

        Assert.AreEqual(0, vm.Items.Count);
        Assert.IsFalse(vm.IsSaveEnabled);
    }

    [TestMethod]
    public void LoadFromJsonFile_ValidJson_LoadsRows_AndIsNotDirty()
    {
        var json = "{\"items\":[{\"iconPath\":\"C:/icons/a.png\",\"text\":\"Alpha\"},{\"iconPath\":\"C:/icons/b.png\",\"text\":\"Beta\"}]}";
        var vm = new NamedIconControlViewModel(
            fileExists: _ => true,
            readAllText: _ => json);

        vm.LoadFromJsonFile("C:\\data.json");

        Assert.AreEqual(2, vm.Items.Count);
        Assert.AreEqual("C:/icons/a.png", vm.Items[0].IconPath);
        Assert.AreEqual("Alpha", vm.Items[0].Text);
        Assert.IsFalse(vm.IsSaveEnabled);
    }

    [TestMethod]
    public void EditingRowText_MarksDirty()
    {
        var json = "{\"items\":[{\"iconPath\":\"C:/icons/a.png\",\"text\":\"Alpha\"}]}";
        var vm = new NamedIconControlViewModel(
            fileExists: _ => true,
            readAllText: _ => json);

        vm.LoadFromJsonFile("C:\\data.json");
        Assert.IsFalse(vm.IsSaveEnabled);

        vm.Items[0].Text = "Alpha2";
        Assert.IsTrue(vm.IsSaveEnabled);
    }

    [TestMethod]
    public void SaveToJsonFile_WritesIndentedJson_AndClearsDirty()
    {
        string? writtenPath = null;
        string? writtenContent = null;
        string? createdDir = null;

        var vm = new NamedIconControlViewModel(
            createDirectory: d => createdDir = d,
            writeAllText: (p, c) =>
            {
                writtenPath = p;
                writtenContent = c;
            });

        vm.Items.Add(new NamedIconRowViewModel("C:/icons/a.png", "Alpha"));
        Assert.IsTrue(vm.IsSaveEnabled);

        vm.SaveToJsonFile("C:\\Temp\\NamedIconControl.json");

        Assert.AreEqual("C:\\Temp", createdDir);
        Assert.AreEqual("C:\\Temp\\NamedIconControl.json", writtenPath);
        Assert.IsNotNull(writtenContent);
        Assert.IsTrue(writtenContent!.Contains("\"items\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(writtenContent.Contains("\"iconPath\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(vm.IsSaveEnabled);
    }

    [TestMethod]
    public void SaveToProgramData_UsesCommonAppDataFolder()
    {
        var programData = Path.Combine(Path.GetTempPath(), "NamedIconControlTests", Guid.NewGuid().ToString("N"));

        string? finalWrittenPath = null;
        var vm = new NamedIconControlViewModel(
            getProgramDataFolder: () => programData,
            createDirectory: _ => { },
            writeAllText: (p, _) => finalWrittenPath = p);

        vm.Items.Add(new NamedIconRowViewModel("C:/icons/a.png", "Alpha"));
        vm.SaveToProgramData();

        Assert.AreEqual(Path.Combine(programData, "TheMadArchivist", "NamedIconControl.json"), finalWrittenPath);
    }

    [TestMethod]
    public void LoadFromProgramData_DefaultsCustomIconsToDocumentsSubfolder_AndCreatesFolder()
    {
        var docs = "C:\\Users\\Test\\Documents";
        var expected = Path.Combine(docs, "ZeeMadArchivist", "CustomIcons");

        var store = new FakeAppSettingsStore();
        var settings = new CustomIconsSettingsService(store);

        string? createdPath = null;
        var vm = new NamedIconControlViewModel(
            customIconsSettingsService: settings,
            getDocumentsFolder: () => docs,
            directoryExists: _ => false,
            createDirectory: p => createdPath = p,
            fileExists: _ => false);

        vm.LoadFromProgramData();

        Assert.AreEqual(expected, vm.CustomIconsFolderPath);
        Assert.AreEqual(expected, createdPath);
    }

    [TestMethod]
    public void SettingCustomIconsFolderPath_EnablesSave_AndSavePersistsToSettings_AndCreatesFolderIfMissing()
    {
        var store = new FakeAppSettingsStore();
        var settings = new CustomIconsSettingsService(store);

        var created = 0;
        var vm = new NamedIconControlViewModel(
            customIconsSettingsService: settings,
            directoryExists: _ => false,
            createDirectory: _ => created++,
            fileExists: _ => false)
        {
            CustomIconsFolderPath = "C:\\Temp\\CustomIcons"
        };

        Assert.AreEqual("C:\\Temp\\CustomIcons", vm.CustomIconsFolderPath);
        Assert.IsTrue(vm.IsCustomIconsPathSaveEnabled);

        vm.SaveCustomIconsFolderPath();

        Assert.AreEqual(1, created);
        Assert.AreEqual("C:\\Temp\\CustomIcons", settings.GetCustomIconsFolderPath());
        Assert.IsFalse(vm.IsCustomIconsPathSaveEnabled);
    }

    [TestMethod]
    public void SaveCustomIconsFolderPath_WhenSavedFolderExistsAndDestinationMissing_RenamesFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "NamedIconControlTests", Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "CustomIcons");
        var destination = Path.Combine(root, "RenamedCustomIcons");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "alpha.ico"), "alpha");

        try
        {
            var store = new FakeAppSettingsStore();
            var settings = new CustomIconsSettingsService(store);
            settings.SetCustomIconsFolderPath(source);

            var vm = new NamedIconControlViewModel(customIconsSettingsService: settings);
            vm.LoadFromProgramData();
            vm.CustomIconsFolderPath = destination;

            var result = vm.SaveCustomIconsFolderPath();

            Assert.AreEqual(NamedIconControlViewModel.CustomIconsFolderSaveResult.Saved, result);
            Assert.IsFalse(Directory.Exists(source));
            Assert.IsTrue(File.Exists(Path.Combine(destination, "alpha.ico")));
            Assert.AreEqual(destination, settings.GetCustomIconsFolderPath());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public void SaveCustomIconsFolderPath_WhenDestinationExistsWithoutMerge_ReturnsDestinationExists()
    {
        var root = Path.Combine(Path.GetTempPath(), "NamedIconControlTests", Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "CustomIcons");
        var destination = Path.Combine(root, "ExistingCustomIcons");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(destination);
        File.WriteAllText(Path.Combine(source, "alpha.ico"), "alpha");

        try
        {
            var store = new FakeAppSettingsStore();
            var settings = new CustomIconsSettingsService(store);
            settings.SetCustomIconsFolderPath(source);

            var vm = new NamedIconControlViewModel(customIconsSettingsService: settings);
            vm.LoadFromProgramData();
            vm.CustomIconsFolderPath = destination;

            var result = vm.SaveCustomIconsFolderPath();

            Assert.AreEqual(NamedIconControlViewModel.CustomIconsFolderSaveResult.DestinationExists, result);
            Assert.IsTrue(File.Exists(Path.Combine(source, "alpha.ico")));
            Assert.AreEqual(source, settings.GetCustomIconsFolderPath());
            Assert.IsTrue(vm.IsCustomIconsPathSaveEnabled);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public void SaveCustomIconsFolderPath_WhenDestinationExistsWithMerge_MergesAndDeletesSource()
    {
        var root = Path.Combine(Path.GetTempPath(), "NamedIconControlTests", Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "CustomIcons");
        var destination = Path.Combine(root, "ExistingCustomIcons");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(destination);
        File.WriteAllText(Path.Combine(source, "alpha.ico"), "alpha");
        File.WriteAllText(Path.Combine(destination, "beta.ico"), "beta");

        try
        {
            var store = new FakeAppSettingsStore();
            var settings = new CustomIconsSettingsService(store);
            settings.SetCustomIconsFolderPath(source);

            var vm = new NamedIconControlViewModel(customIconsSettingsService: settings);
            vm.LoadFromProgramData();
            vm.CustomIconsFolderPath = destination;

            var result = vm.SaveCustomIconsFolderPath(mergeIfDestinationExists: true);

            Assert.AreEqual(NamedIconControlViewModel.CustomIconsFolderSaveResult.Saved, result);
            Assert.IsFalse(Directory.Exists(source));
            Assert.IsTrue(File.Exists(Path.Combine(destination, "alpha.ico")));
            Assert.IsTrue(File.Exists(Path.Combine(destination, "beta.ico")));
            Assert.AreEqual(destination, settings.GetCustomIconsFolderPath());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public void IconList_WhenCustomIconsDirectoryExists_IsPopulatedAndSortedByFileName()
    {
        var store = new FakeAppSettingsStore();
        var settings = new CustomIconsSettingsService(store);

        var vm = new NamedIconControlViewModel(
            customIconsSettingsService: settings,
            directoryExists: _ => true,
            enumerateFiles: _ =>
            [
                "C:\\Icons\\b.ico",
                "C:\\Icons\\a.ico",
            ],
            fileExists: _ => false)
        {
            CustomIconsFolderPath = "C:\\Icons"
        };

        Assert.AreEqual(2, vm.IconList.Count);
        Assert.AreEqual("a", vm.IconList[0].Name);
        Assert.AreEqual("b", vm.IconList[1].Name);
    }

    [TestMethod]
    public void IconList_WhenCustomIconsDirectoryExists_OnlyIncludesIcoFiles()
    {
        var store = new FakeAppSettingsStore();
        var settings = new CustomIconsSettingsService(store);

        var vm = new NamedIconControlViewModel(
            customIconsSettingsService: settings,
            directoryExists: _ => true,
            enumerateFiles: _ =>
            [
                "C:\\Icons\\a.ico",
                "C:\\Icons\\b.png",
                "C:\\Icons\\c.txt",
                "C:\\Icons\\d.ICO",
            ],
            fileExists: _ => false)
        {
            CustomIconsFolderPath = "C:\\Icons"
        };

        Assert.AreEqual(2, vm.IconList.Count);
        Assert.AreEqual("a", vm.IconList[0].Name);
        Assert.AreEqual("d", vm.IconList[1].Name);
    }

    [TestMethod]
    public void IconList_WhenCustomIconsDirectoryMissing_IsEmpty()
    {
        var store = new FakeAppSettingsStore();
        var settings = new CustomIconsSettingsService(store);

        var vm = new NamedIconControlViewModel(
            customIconsSettingsService: settings,
            directoryExists: _ => false,
            enumerateFiles: _ => ["C:\\Icons\\a.png"],
            fileExists: _ => false)
        {
            CustomIconsFolderPath = "C:\\Icons"
        };

        Assert.AreEqual(0, vm.IconList.Count);
    }

    [TestMethod]
    public async Task ImportImagesAsIconsAsync_WhenIconExists_AndDecisionIsSkip_Skips()
    {
        var store = new FakeAppSettingsStore();
        var settings = new CustomIconsSettingsService(store);

        var savedPaths = new List<string>();
        var vm = new NamedIconControlViewModel(
            customIconsSettingsService: settings,
            directoryExists: _ => true,
            createDirectory: _ => { })
        {
            CustomIconsFolderPath = "C:\\Icons"
        };

        var result = await vm.ImportImagesAsIconsAsync(
            ["C:\\Images\\a.png"],
            decideOverwriteAsync: _ => Task.FromResult(NamedIconControlViewModel.IconOverwriteDecision.Skip),
            fileExists: _ => true,
            toIcon: _ => new Icon(SystemIcons.Application, 16, 16),
            saveIcon: (_, path) => savedPaths.Add(path));

        Assert.AreEqual(0, result.ImportedCount);
        Assert.AreEqual(1, result.SkippedCount);
        Assert.IsFalse(result.WasCancelled);
        Assert.AreEqual(0, savedPaths.Count);
    }

    [TestMethod]
    public async Task ImportImagesAsIconsAsync_WhenIconExists_AndDecisionIsOverwrite_Overwrites()
    {
        var store = new FakeAppSettingsStore();
        var settings = new CustomIconsSettingsService(store);

        var savedPaths = new List<string>();
        var vm = new NamedIconControlViewModel(
            customIconsSettingsService: settings,
            directoryExists: _ => true,
            createDirectory: _ => { })
        {
            CustomIconsFolderPath = "C:\\Icons"
        };

        var result = await vm.ImportImagesAsIconsAsync(
            ["C:\\Images\\a.png"],
            decideOverwriteAsync: _ => Task.FromResult(NamedIconControlViewModel.IconOverwriteDecision.Overwrite),
            fileExists: _ => true,
            toIcon: _ => new Icon(SystemIcons.Application, 16, 16),
            saveIcon: (_, path) => savedPaths.Add(path));

        Assert.AreEqual(1, result.ImportedCount);
        Assert.AreEqual(0, result.SkippedCount);
        Assert.IsFalse(result.WasCancelled);
        Assert.AreEqual(1, savedPaths.Count);
        Assert.AreEqual("C:\\Icons\\a.ico", savedPaths[0]);
    }

    [TestMethod]
    public async Task ImportImagesAsIconsAsync_WhenIconExists_AndDecisionIsCancel_Cancels()
    {
        var store = new FakeAppSettingsStore();
        var settings = new CustomIconsSettingsService(store);

        var savedPaths = new List<string>();
        var vm = new NamedIconControlViewModel(
            customIconsSettingsService: settings,
            directoryExists: _ => true,
            createDirectory: _ => { })
        {
            CustomIconsFolderPath = "C:\\Icons"
        };

        var result = await vm.ImportImagesAsIconsAsync(
            ["C:\\Images\\a.png"],
            decideOverwriteAsync: _ => Task.FromResult(NamedIconControlViewModel.IconOverwriteDecision.Cancel),
            fileExists: _ => true,
            toIcon: _ => new Icon(SystemIcons.Application, 16, 16),
            saveIcon: (_, path) => savedPaths.Add(path));

        Assert.AreEqual(0, result.ImportedCount);
        Assert.AreEqual(0, result.SkippedCount);
        Assert.IsTrue(result.WasCancelled);
        Assert.AreEqual(0, savedPaths.Count);
    }

    [TestMethod]
    public async Task ImportImagesAsIconsAsync_WhenToIconThrows_AddsErrorAndContinues()
    {
        var store = new FakeAppSettingsStore();
        var settings = new CustomIconsSettingsService(store);

        var savedPaths = new List<string>();
        var vm = new NamedIconControlViewModel(
            customIconsSettingsService: settings,
            directoryExists: _ => true,
            createDirectory: _ => { })
        {
            CustomIconsFolderPath = "C:\\Icons"
        };

        var result = await vm.ImportImagesAsIconsAsync(
            ["C:\\Images\\a.png", "C:\\Images\\b.png"],
            decideOverwriteAsync: _ => Task.FromResult(NamedIconControlViewModel.IconOverwriteDecision.Overwrite),
            fileExists: _ => false,
            toIcon: path =>
            {
                if (path.EndsWith("a.png", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("bad image");
                }

                return new Icon(SystemIcons.Application, 16, 16);
            },
            saveIcon: (_, path) => savedPaths.Add(path));

        Assert.AreEqual(1, result.ImportedCount);
        Assert.AreEqual(0, result.SkippedCount);
        Assert.IsFalse(result.WasCancelled);
        Assert.AreEqual(1, savedPaths.Count);
        Assert.AreEqual(1, result.Errors.Count);
    }

    [TestMethod]
    public void SettingCustomIconsFolderPath_WhenDirectoryMissing_DoesNotCreateDirectory()
    {
        var store = new FakeAppSettingsStore();
        var settings = new CustomIconsSettingsService(store);

        var created = 0;
        var vm = new NamedIconControlViewModel(
            customIconsSettingsService: settings,
            directoryExists: _ => false,
            createDirectory: _ => created++,
            enumerateFiles: _ => [],
            fileExists: _ => false);

        vm.CustomIconsFolderPath = "C:\\Icons";

        Assert.AreEqual(0, created);
    }

    [TestMethod]
    public void RefreshIcons_WhenDirectoryMissing_DoesNotCreateDirectory()
    {
        var store = new FakeAppSettingsStore();
        var settings = new CustomIconsSettingsService(store);

        var created = 0;
        var vm = new NamedIconControlViewModel(
            customIconsSettingsService: settings,
            directoryExists: _ => false,
            createDirectory: _ => created++,
            enumerateFiles: _ => [],
            fileExists: _ => false)
        {
            CustomIconsFolderPath = "C:\\Icons"
        };

        Assert.AreEqual(0, created);

        vm.RefreshIcons();

        Assert.AreEqual(0, created);
    }

    [TestMethod]
    public void IconList_WhenCustomIconsFolderChangesOnDisk_Refreshes()
    {
        var store = new FakeAppSettingsStore();
        var settings = new CustomIconsSettingsService(store);

        var watcher = new FakeCustomIconsFolderWatcher();

        string[] files = ["C:\\Icons\\a.ico"];

        var vm = new NamedIconControlViewModel(
            customIconsSettingsService: settings,
            directoryExists: _ => true,
            createDirectory: _ => { },
            enumerateFiles: _ => files,
            dispatchToUiThread: action => action(),
            createCustomIconsFolderWatcher: _ => watcher,
            customIconsWatcherDebounceDelay: TimeSpan.Zero)
        {
            CustomIconsFolderPath = "C:\\Icons"
        };

        Assert.AreEqual(1, vm.IconList.Count);
        Assert.AreEqual("a", vm.IconList[0].Name);

        files = ["C:\\Icons\\a.ico", "C:\\Icons\\b.ico"];
        watcher.RaiseIconsChanged();

        Assert.AreEqual(2, vm.IconList.Count);
        Assert.AreEqual("a", vm.IconList[0].Name);
        Assert.AreEqual("b", vm.IconList[1].Name);
    }

    private sealed class FakeCustomIconsFolderWatcher : NamedIconControlViewModel.ICustomIconsFolderWatcher
    {
        public event EventHandler? IconsChanged;

        public void Start()
        {
        }

        public void Dispose()
        {
        }

        public void RaiseIconsChanged()
        {
            IconsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [TestMethod]
    public void IconListItemViewModel_OriginalName_IsInitialFileName_AndCanRevert()
    {
        var item = new IconListItemViewModel("C:\\Icons\\TestIcon.ico");

        Assert.AreEqual("TestIcon", item.OriginalName);
        Assert.AreEqual("TestIcon", item.Name);

        item.Name = "Changed";
        Assert.AreEqual("Changed", item.Name);

        item.Name = item.OriginalName;
        Assert.AreEqual("TestIcon", item.Name);
    }

    [TestMethod]
    public void IconListItemViewModel_IsDirty_IsFalseUntilNameChanges_AndFalseAfterRevert()
    {
        var item = new IconListItemViewModel("C:\\Icons\\TestIcon.ico");

        Assert.IsFalse(item.IsDirty);

        item.Name = "Changed";
        Assert.IsTrue(item.IsDirty);

        item.Name = item.OriginalName;
        Assert.IsFalse(item.IsDirty);
    }
}
