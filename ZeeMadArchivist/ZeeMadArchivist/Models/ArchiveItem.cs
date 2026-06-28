using CyberFeedForward.TheMadArchivist.ViewModels;
using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using System;
using System.IO;

namespace CyberFeedForward.TheMadArchivist.Models;

public sealed class ArchiveItem : ViewModelBase
{
    private string? _driveLetter;
    private string? _iconPath;

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

    public string? IconPath
    {
        get => _iconPath;
        private set => SetField(ref _iconPath, value);
    }

    public string DisplayText => string.IsNullOrEmpty(DriveLetter)
        ? Name
        : $"{Name} ({DriveLetter}:)";

    public void RefreshDriveLetter()
    {
        if (FolderTools.TryFindDriveLetterForPath(Path, out var letter))
        {
            DriveLetter = letter.ToString();
            IconPath = FolderTools.TryGetDriveIconPath(letter, out var ip) ? ip : null;
        }
        else
        {
            DriveLetter = null;
            IconPath = null;
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
