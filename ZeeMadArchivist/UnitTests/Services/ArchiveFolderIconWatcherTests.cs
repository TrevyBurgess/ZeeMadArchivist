using CyberFeedForward.TheMadArchivist.Models;
using CyberFeedForward.TheMadArchivist.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace UnitTests.Services;

[TestClass]
public sealed class ArchiveFolderIconWatcherTests
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

    private static CustomIconsSettingsService CreateSettingsService(string customIconsFolderPath)
    {
        var store = new FakeAppSettingsStore();
        store.SetString("CustomIcons.FolderPath", customIconsFolderPath);
        return new CustomIconsSettingsService(store);
    }

    [TestMethod]
    public void Start_WhenSubfolderMatchesIconName_SetsCustomIconPath()
    {
        var archives = new ObservableCollection<ArchiveItem>
        {
            new("C:\\Archives\\Archive1"),
        };

        var settings = CreateSettingsService("C:\\CustomIcons");
        var directoryExists = new Func<string, bool>(_ => true);
        var enumerateDirectories = new Func<string, IEnumerable<string>>(_ => ["C:\\Archives\\Archive1\\Movies"]);
        var enumerateFiles = new Func<string, IEnumerable<string>>(_ => ["C:\\CustomIcons\\Movies.ico"]);

        var watcher = new ArchiveFolderIconWatcher(
            archives,
            settings,
            directoryExists,
            enumerateDirectories,
            enumerateFiles,
            action => action(),
            TimeSpan.Zero);

        watcher.Start();

        Assert.AreEqual("C:\\CustomIcons\\Movies.ico", archives[0].CustomIconPath);
    }

    [TestMethod]
    public void Start_WhenNoSubfolderMatches_SetsCustomIconPathToNull()
    {
        var archives = new ObservableCollection<ArchiveItem>
        {
            new("C:\\Archives\\Archive1"),
        };

        var settings = CreateSettingsService("C:\\CustomIcons");
        var directoryExists = new Func<string, bool>(_ => true);
        var enumerateDirectories = new Func<string, IEnumerable<string>>(_ => ["C:\\Archives\\Archive1\\Documents"]);
        var enumerateFiles = new Func<string, IEnumerable<string>>(_ => ["C:\\CustomIcons\\Movies.ico"]);

        var watcher = new ArchiveFolderIconWatcher(
            archives,
            settings,
            directoryExists,
            enumerateDirectories,
            enumerateFiles,
            action => action(),
            TimeSpan.Zero);

        watcher.Start();

        Assert.IsNull(archives[0].CustomIconPath);
    }

    [TestMethod]
    public void Start_IgnoresNonIcoFiles()
    {
        var archives = new ObservableCollection<ArchiveItem>
        {
            new("C:\\Archives\\Archive1"),
        };

        var settings = CreateSettingsService("C:\\CustomIcons");
        var directoryExists = new Func<string, bool>(_ => true);
        var enumerateDirectories = new Func<string, IEnumerable<string>>(_ => ["C:\\Archives\\Archive1\\Movies"]);
        var enumerateFiles = new Func<string, IEnumerable<string>>(_ => ["C:\\CustomIcons\\Movies.png"]);

        var watcher = new ArchiveFolderIconWatcher(
            archives,
            settings,
            directoryExists,
            enumerateDirectories,
            enumerateFiles,
            action => action(),
            TimeSpan.Zero);

        watcher.Start();

        Assert.IsNull(archives[0].CustomIconPath);
    }

    [TestMethod]
    public void Start_WhenMultipleSubfoldersAndFirstMatches_SetsCustomIconPath()
    {
        var archives = new ObservableCollection<ArchiveItem>
        {
            new("C:\\Archives\\Archive1"),
        };

        var settings = CreateSettingsService("C:\\CustomIcons");
        var directoryExists = new Func<string, bool>(_ => true);
        var enumerateDirectories = new Func<string, IEnumerable<string>>(_ =>
            ["C:\\Archives\\Archive1\\Music", "C:\\Archives\\Archive1\\Movies"]);
        var enumerateFiles = new Func<string, IEnumerable<string>>(_ =>
            ["C:\\CustomIcons\\Music.ico", "C:\\CustomIcons\\Movies.ico"]);

        var watcher = new ArchiveFolderIconWatcher(
            archives,
            settings,
            directoryExists,
            enumerateDirectories,
            enumerateFiles,
            action => action(),
            TimeSpan.Zero);

        watcher.Start();

        Assert.AreEqual("C:\\CustomIcons\\Music.ico", archives[0].CustomIconPath);
    }

    [TestMethod]
    public void Dispose_CanBeCalledMultipleTimesWithoutThrowing()
    {
        var archives = new ObservableCollection<ArchiveItem>();
        var settings = CreateSettingsService("C:\\CustomIcons");

        var watcher = new ArchiveFolderIconWatcher(archives, settings, _ => false, _ => [], _ => [], action => action(), TimeSpan.Zero);

        watcher.Dispose();
        watcher.Dispose();

        Assert.AreEqual(0, archives.Count);
    }

    [TestMethod]
    public void Start_RespectsCaseInsensitiveMatching()
    {
        var archives = new ObservableCollection<ArchiveItem>
        {
            new("C:\\Archives\\Archive1"),
        };

        var settings = CreateSettingsService("C:\\CustomIcons");
        var directoryExists = new Func<string, bool>(_ => true);
        var enumerateDirectories = new Func<string, IEnumerable<string>>(_ => ["C:\\Archives\\Archive1\\movies"]);
        var enumerateFiles = new Func<string, IEnumerable<string>>(_ => ["C:\\CustomIcons\\MOVIES.ico"]);

        var watcher = new ArchiveFolderIconWatcher(
            archives,
            settings,
            directoryExists,
            enumerateDirectories,
            enumerateFiles,
            action => action(),
            TimeSpan.Zero);

        watcher.Start();

        Assert.AreEqual("C:\\CustomIcons\\MOVIES.ico", archives[0].CustomIconPath);
    }
}
