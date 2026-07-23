using CyberFeedForward.TheMadArchivist.ViewModels;
using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using System;
using System.IO;

namespace CyberFeedForward.TheMadArchivist.Models;

public sealed class ArchiveItem : ViewModelBase
{
    private string? _driveLetter;
    private string? _driveIconPath;
    private string? _customIconPath;

    public ArchiveItem(string path)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Name = GetArchiveName(path);
        RefreshDriveLetter();
    }

    public string Path { get; }

    public string Name { get; }

    public string? DriveLetter
    {
        get => _driveLetter;
        private set
        {
            if (SetField(ref _driveLetter, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public string? IconPath => _customIconPath ?? _driveIconPath ?? FolderTools.GetDefaultAppIconPath();

    public string? CustomIconPath
    {
        get => _customIconPath;
        set
        {
            if (SetField(ref _customIconPath, value))
            {
                OnPropertyChanged(nameof(IconPath));
            }
        }
    }

    public string DisplayText => string.IsNullOrEmpty(DriveLetter)
        ? Name
        : $"{Name} ({DriveLetter}:)";

    public void RefreshDriveLetter()
    {
        if (FolderTools.TryFindDriveLetterForPath(Path, out var letter))
        {
            DriveLetter = letter.ToString();
            var driveIconPath = FolderTools.TryGetDriveIconPath(letter, out var ip) ? ip : null;
            _ = SetField(ref _driveIconPath, driveIconPath, nameof(IconPath));
        }
        else
        {
            DriveLetter = null;
            _ = SetField(ref _driveIconPath, null, nameof(IconPath));
        }
    }

    private static string GetArchiveName(string path)
    {
        var trimmed = path.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return path;
        }

        var name = new DirectoryInfo(trimmed).Name;
        return string.IsNullOrWhiteSpace(name) ? trimmed : name;
    }
}
