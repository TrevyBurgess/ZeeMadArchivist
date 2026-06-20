using CyberFeedForward.TheMadArchivist.Properties;
using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CyberFeedForward.TheMadArchivist.ViewModels.Controls;

public sealed partial class ArchiveListControlViewModel : INotifyPropertyChanged
{
    public delegate int MapDriveDelegate(string folderPath, char driveLetter, string name);
    public delegate bool TrySetDriveIconDelegate(char driveLetter, out string errorMessage);

    private readonly ArchivesSettingsService _archivesSettingsService;
    private readonly Func<string, bool> _directoryExists;
    private readonly MapDriveDelegate _mapDrive;
    private readonly TrySetDriveIconDelegate _trySetDefaultAppDriveIcon;
    private string _newArchivePath = string.Empty;
    private string? _selectedArchive;

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
        TrySetDriveIconDelegate? trySetDefaultAppDriveIcon = null)
    {
        _archivesSettingsService = archivesSettingsService ?? throw new ArgumentNullException(nameof(archivesSettingsService));
        _directoryExists = directoryExists ?? Directory.Exists;
        _mapDrive = mapDrive ?? FolderTools.MapDrive;
        _trySetDefaultAppDriveIcon = trySetDefaultAppDriveIcon ?? FolderTools.TrySetDefaultAppDriveIcon;

        Archives = new ObservableCollection<string>(_archivesSettingsService.GetArchives());
        Archives.CollectionChanged += Archives_OnCollectionChanged;

        var isFirstRun = FirstRunService.Instance.ShouldRunFirstRunExperience();

        if (Archives.Count == 0 && !isFirstRun)
        {
            var rootPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var documentsPath = Path.Combine(rootPath, Resources.DefaultArchiveName);

            if (!Directory.Exists(documentsPath))
            {
                Directory.CreateDirectory(documentsPath);
            }

            Archives.Add(documentsPath);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> Archives { get; }

    public string NewArchivePath
    {
        get => _newArchivePath;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_newArchivePath, next, StringComparison.Ordinal))
            {
                return;
            }

            _newArchivePath = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAddEnabled));
        }
    }

    public bool IsAddEnabled => !string.IsNullOrWhiteSpace(NewArchivePath);

    public string? SelectedArchive
    {
        get => _selectedArchive;
        set
        {
            if (string.Equals(_selectedArchive, value, StringComparison.Ordinal))
            {
                return;
            }

            _selectedArchive = value;
            OnPropertyChanged();
        }
    }

    public bool AddArchive()
    {
        var result = TryAddFolderPath(NewArchivePath);
        return result == ArchiveAddResult.Added;
    }

    public bool TryCreateNewArchive(string folderPath, char driveLetter, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            errorMessage = "The selected folder path is invalid.";
            return false;
        }

        try
        {
            var archiveName = new DirectoryInfo(folderPath).Name;
            var mapResult = _mapDrive(folderPath, driveLetter, archiveName);
            if (mapResult != 0)
            {
                errorMessage = FolderTools.GetMapDriveErrorMessage(mapResult, driveLetter, folderPath);
                Trace.TraceError($"MapDrive failed with code {mapResult}: {errorMessage}");
                return false;
            }

            if (!_trySetDefaultAppDriveIcon(driveLetter, out var driveIconError))
            {
                errorMessage = $"Failed to set mapped drive icon. {driveIconError}";
                Trace.TraceError(errorMessage);
                return false;
            }

            var addResult = TryAddFolderPath(folderPath, clearNewArchivePathOnSuccess: false);
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
        if (Archives.Count == 0)
        {
            Archives.Add(archivePath);
            return;
        }

        var comparer = StringComparer.OrdinalIgnoreCase;
        for (var i = 0; i < Archives.Count; i++)
        {
            if (comparer.Compare(archivePath, Archives[i]) < 0)
            {
                Archives.Insert(i, archivePath);
                return;
            }
        }

        Archives.Add(archivePath);
    }

    public bool IsExistingArchive(string? archivePath)
    {
        var next = archivePath?.Trim();
        if (string.IsNullOrWhiteSpace(next))
        {
            return false;
        }

        return Archives.Any(a => string.Equals(a, next, StringComparison.OrdinalIgnoreCase));
    }

    public void RemoveSelectedArchive()
    {
        if (string.IsNullOrWhiteSpace(SelectedArchive))
        {
            return;
        }

        var toRemove = Archives.FirstOrDefault(a => string.Equals(a, SelectedArchive, StringComparison.OrdinalIgnoreCase));
        if (toRemove is null)
        {
            return;
        }

        Archives.Remove(toRemove);
        SelectedArchive = null;
    }

    public bool RemoveArchive(string? archivePath)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
        {
            return false;
        }

        var toRemove = Archives.FirstOrDefault(a => string.Equals(a, archivePath, StringComparison.OrdinalIgnoreCase));
        if (toRemove is null)
        {
            return false;
        }

        Archives.Remove(toRemove);

        if (string.Equals(SelectedArchive, toRemove, StringComparison.OrdinalIgnoreCase))
        {
            SelectedArchive = null;
        }

        return true;
    }

    private void Archives_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _archivesSettingsService.SaveArchives(Archives);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
