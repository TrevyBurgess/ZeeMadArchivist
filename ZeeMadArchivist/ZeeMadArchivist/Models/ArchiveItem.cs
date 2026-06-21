using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace CyberFeedForward.TheMadArchivist.Models;

public sealed class ArchiveItem : INotifyPropertyChanged
{
    private string? _driveLetter;

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

    public string DisplayText => string.IsNullOrEmpty(DriveLetter)
        ? Name
        : $"{Name} ({DriveLetter}:)";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshDriveLetter()
    {
        DriveLetter = FolderTools.TryFindDriveLetterForPath(Path, out var letter)
            ? letter.ToString()
            : null;
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
