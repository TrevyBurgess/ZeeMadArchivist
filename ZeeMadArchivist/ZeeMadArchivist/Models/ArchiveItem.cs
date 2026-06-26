using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace CyberFeedForward.TheMadArchivist.Models;

public sealed class ArchiveItem : INotifyPropertyChanged
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
            if (_driveLetter == value)
            {
                return;
            }

            _driveLetter = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public string? IconPath
    {
        get => _iconPath;
        private set
        {
            if (_iconPath == value)
            {
                return;
            }

            _iconPath = value;
            OnPropertyChanged();
        }
    }

    public string DisplayText => string.IsNullOrEmpty(DriveLetter)
        ? Name
        : $"{Name} ({DriveLetter}:)";

    public event PropertyChangedEventHandler? PropertyChanged;

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
