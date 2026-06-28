using CyberFeedForward.TheMadArchivist.Models;
using CyberFeedForward.TheMadArchivist.Properties;
using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.ViewModels;
using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CyberFeedForward.TheMadArchivist.ViewModels.Controls;

public sealed partial class ArchiveListControlViewModel : ViewModelBase
{
    public delegate int MapDriveDelegate(string folderPath, char driveLetter, string name);
    public delegate bool TrySetDriveIconDelegate(char driveLetter, string iconPath, out string errorMessage);
    public delegate bool UnmapDriveForPathDelegate(string path);

    private readonly ArchivesSettingsService _archivesSettingsService;
    private readonly Func<string, bool> _directoryExists;
    private readonly MapDriveDelegate _mapDrive;
    private readonly TrySetDriveIconDelegate _trySetDriveIcon;
    private readonly UnmapDriveForPathDelegate _unmapDriveForPath;
    private string _newArchivePath = string.Empty;
    private ArchiveItem? _selectedArchive;

    public enum ArchiveAddResult
    {
        Added,
        Empty,
        Duplicate,
        NotFound,
        Error,
    }

    public ArchiveListControlViewModel(
        ArchivesSettingsService archivesSettingsService,
        Func<string, bool>? directoryExists = null,
        MapDriveDelegate? mapDrive = null,
        TrySetDriveIconDelegate? trySetDriveIcon = null,
        UnmapDriveForPathDelegate? unmapDriveForPath = null)
    {
        _archivesSettingsService = archivesSettingsService ?? throw new ArgumentNullException(nameof(archivesSettingsService));
        _directoryExists = directoryExists ?? Directory.Exists;
        _mapDrive = mapDrive ?? FolderTools.MapDrive;
        _trySetDriveIcon = trySetDriveIcon ?? FolderTools.TrySetDriveIcon;
        _unmapDriveForPath = unmapDriveForPath ?? FolderTools.TryUnmapDriveForPath;

        Archives = new ObservableCollection<ArchiveItem>(_archivesSettingsService.GetArchives().Select(p => new ArchiveItem(p)));
        Archives.CollectionChanged += Archives_OnCollectionChanged;
        ApplyDriveMetadataForExistingArchives();

        //var isFirstRun = FirstRunService.Instance.ShouldRunFirstRunExperience();
        //if (Archives.Count == 0 && !isFirstRun)
        //{
        //    var rootPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        //    var documentsPath = Path.Combine(rootPath, Resources.DefaultArchiveName);

        //    if (!Directory.Exists(documentsPath))
        //    {
        //        Directory.CreateDirectory(documentsPath);
        //    }

        //    Archives.Add(new ArchiveItem(documentsPath));
        //}
    }

    public ObservableCollection<ArchiveItem> Archives { get; }

    public string NewArchivePath
    {
        get => _newArchivePath;
        set
        {
            if (SetField(ref _newArchivePath, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(IsAddEnabled));
            }
        }
    }

    public bool IsAddEnabled => !string.IsNullOrWhiteSpace(NewArchivePath);

    public ArchiveItem? SelectedArchive
    {
        get => _selectedArchive;
        set => SetField(ref _selectedArchive, value);
    }

    public bool AddArchive()
    {
        var result = TryAddFolderPath(NewArchivePath);
        return result == ArchiveAddResult.Added;
    }

    public bool TryCreateNewArchive(string folderPath, string archiveName, char driveLetter, string iconPath, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            errorMessage = "The selected folder path is invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(archiveName))
        {
            errorMessage = "The archive name is invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(iconPath))
        {
            errorMessage = "The icon path is invalid.";
            return false;
        }

        try
        {
            var fullArchivePath = Path.GetFullPath(Path.Combine(folderPath, archiveName));
            Directory.CreateDirectory(fullArchivePath);

            var mapResult = _mapDrive(fullArchivePath, driveLetter, archiveName);
            if (mapResult != 0)
            {
                errorMessage = FolderTools.GetMapDriveErrorMessage(mapResult, driveLetter, fullArchivePath);
                Trace.TraceError($"MapDrive failed with code {mapResult}: {errorMessage}");
                return false;
            }

            var fullIconPath = Path.GetFullPath(iconPath);
            if (!File.Exists(fullIconPath))
            {
                errorMessage = "Icon file does not exist.";
                return false;
            }

            if (!_trySetDriveIcon(driveLetter, fullIconPath, out var driveIconError))
            {
                errorMessage = $"Failed to set mapped drive icon. {driveIconError}";
                Trace.TraceError(errorMessage);
                return false;
            }

            var addResult = TryAddFolderPath(fullArchivePath, clearNewArchivePathOnSuccess: false);
            return addResult == ArchiveAddResult.Added;
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            errorMessage = ex.Message;
            return false;
        }
    }

    public ArchiveAddResult TryAddFolderPath(string? folderPath, bool clearNewArchivePathOnSuccess = true)
    {
        var next = folderPath?.Trim();
        if (string.IsNullOrWhiteSpace(next))
        {
            return ArchiveAddResult.Empty;
        }

        try
        {
            if (!_directoryExists(next))
            {
                return ArchiveAddResult.NotFound;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return ArchiveAddResult.Error;
        }

        if (IsExistingArchive(next))
        {
            return ArchiveAddResult.Duplicate;
        }

        InsertArchiveSorted(next);
        if (clearNewArchivePathOnSuccess)
        {
            NewArchivePath = string.Empty;
        }
        return ArchiveAddResult.Added;
    }

    private void InsertArchiveSorted(string archivePath)
    {
        var item = new ArchiveItem(archivePath);
        if (Archives.Count == 0)
        {
            Archives.Add(item);
            return;
        }

        var comparer = StringComparer.OrdinalIgnoreCase;
        for (var i = 0; i < Archives.Count; i++)
        {
            if (comparer.Compare(item.Name, Archives[i].Name) < 0)
            {
                Archives.Insert(i, item);
                return;
            }
        }

        Archives.Add(item);
    }

    public bool IsExistingArchive(string? archivePath)
    {
        var next = archivePath?.Trim();
        if (string.IsNullOrWhiteSpace(next))
        {
            return false;
        }

        return Archives.Any(a => string.Equals(a.Path, next, StringComparison.OrdinalIgnoreCase));
    }

    public void RemoveSelectedArchive()
    {
        if (SelectedArchive is null)
        {
            return;
        }

        _unmapDriveForPath(SelectedArchive.Path);
        Archives.Remove(SelectedArchive);
        SelectedArchive = null;
    }

    public bool RemoveArchive(string? archivePath)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
        {
            return false;
        }

        var toRemove = Archives.FirstOrDefault(a => string.Equals(a.Path, archivePath, StringComparison.OrdinalIgnoreCase));
        if (toRemove is null)
        {
            return false;
        }

        _unmapDriveForPath(toRemove.Path);
        Archives.Remove(toRemove);

        if (ReferenceEquals(SelectedArchive, toRemove))
        {
            SelectedArchive = null;
        }

        return true;
    }

    public void ReloadArchives()
    {
        var refreshed = _archivesSettingsService.GetArchives();
        Archives.Clear();
        foreach (var archive in refreshed)
        {
            Archives.Add(new ArchiveItem(archive));
        }
    }

    private void ApplyDriveMetadataForExistingArchives()
    {
        var defaultIconPath = FolderTools.GetDefaultAppIconPath();

        foreach (var archive in Archives)
        {
            if (!FolderTools.TryFindDriveLetterForPath(archive.Path, out var driveLetter))
            {
                driveLetter = TryRemapArchive(archive);
                if (driveLetter == default)
                {
                    continue;
                }
            }

            if (!FolderTools.TryGetDriveIconPath(driveLetter, out _) && defaultIconPath is not null)
            {
                if (!_trySetDriveIcon(driveLetter, defaultIconPath, out var iconError))
                {
                    Trace.TraceError($"Failed to set icon for drive {driveLetter}: {iconError}");
                }
            }

            archive.RefreshDriveLetter();
        }
    }

    private char TryRemapArchive(ArchiveItem archive)
    {
        if (!_directoryExists(archive.Path))
        {
            return default;
        }

        var usedLetters = DriveInfo.GetDrives()
            .Select(d => char.ToUpperInvariant(d.Name[0]))
            .Where(c => c is >= 'A' and <= 'Z');

        var letter = DriveLetterHelper.GetUnusedDriveLetters(usedLetters).FirstOrDefault();
        if (letter == default)
        {
            Trace.TraceError($"No available drive letter to remap archive: {archive.Path}");
            return default;
        }

        var result = _mapDrive(archive.Path, letter, archive.Name);
        if (result != 0)
        {
            Trace.TraceError($"Failed to remap archive '{archive.Path}' to {letter}: error {result}");
            return default;
        }

        return letter;
    }

    private void Archives_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _archivesSettingsService.SaveArchives([.. Archives.Select(a => a.Path)]);
    }

}
