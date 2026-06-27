using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CyberFeedForward.TheMadArchivist.ViewModels.Dialogs;

public sealed class NewArchiveDialogViewModel : INotifyPropertyChanged
{
    public delegate IEnumerable<char> GetUnusedDriveLettersDelegate();

    private readonly GetUnusedDriveLettersDelegate _getUnusedDriveLetters;

    private string _folderPath = string.Empty;
    private string _archiveName = string.Empty;
    private string _iconPath = string.Empty;
    private string? _selectedDriveLetter;
    private string? _errorMessage;

    public NewArchiveDialogViewModel(GetUnusedDriveLettersDelegate? getUnusedDriveLetters = null)
    {
        _getUnusedDriveLetters = getUnusedDriveLetters ?? GetUnusedDriveLetters;

        IconPath = FolderTools.GetDefaultAppIconPath() ?? string.Empty;

        RefreshDriveLetters();
    }

    public ObservableCollection<string> AvailableDriveLetters { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public string FolderPath
    {
        get => _folderPath;
        set
        {
            if (_folderPath == value) return;
            _folderPath = value;
            OnPropertyChanged();
        }
    }

    public string ArchiveName
    {
        get => _archiveName;
        set
        {
            if (_archiveName == value) return;
            _archiveName = value;
            OnPropertyChanged();
        }
    }

    public string IconPath
    {
        get => _iconPath;
        set
        {
            if (_iconPath == value) return;
            _iconPath = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedDriveLetter
    {
        get => _selectedDriveLetter;
        set
        {
            if (_selectedDriveLetter == value) return;
            _selectedDriveLetter = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public char? ParsedDriveLetter
    {
        get
        {
            var s = SelectedDriveLetter?.Trim();
            if (string.IsNullOrEmpty(s)) return null;
            var c = char.ToUpperInvariant(s[0]);
            return c is >= 'A' and <= 'Z' ? c : null;
        }
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(FolderPath))
        {
            ErrorMessage = "Folder path is required.";
            return false;
        }

        if (!Directory.Exists(FolderPath))
        {
            ErrorMessage = "Folder path does not exist.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ArchiveName))
        {
            ErrorMessage = "Archive name is required.";
            return false;
        }

        if (ParsedDriveLetter is null)
        {
            ErrorMessage = "Drive letter is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(IconPath))
        {
            ErrorMessage = "Icon path is required.";
            return false;
        }

        if (!File.Exists(IconPath))
        {
            ErrorMessage = "Icon file does not exist.";
            return false;
        }

        ErrorMessage = null;
        return true;
    }

    public void RefreshDriveLetters()
    {
        var current = SelectedDriveLetter;
        AvailableDriveLetters.Clear();

        foreach (var letter in _getUnusedDriveLetters())
        {
            AvailableDriveLetters.Add(letter + ":");
        }

        SelectedDriveLetter = AvailableDriveLetters.Contains(current ?? string.Empty)
            ? current
            : AvailableDriveLetters.FirstOrDefault();
    }

    public static IEnumerable<char> GetUnusedDriveLetters()
    {
        var used = DriveInfo.GetDrives()
            .Select(d => char.ToUpperInvariant(d.Name[0]))
            .Where(c => c is >= 'A' and <= 'Z');

        return GetUnusedDriveLetters(used);
    }

    public static IEnumerable<char> GetUnusedDriveLetters(IEnumerable<char> usedDriveLetters, char startLetter = 'D')
    {
        ArgumentNullException.ThrowIfNull(usedDriveLetters);

        var used = new HashSet<char>(usedDriveLetters
            .Select(char.ToUpperInvariant)
            .Where(c => c is >= 'A' and <= 'Z'));

        var start = char.ToUpperInvariant(startLetter);
        if (start is < 'A' or > 'Z')
        {
            throw new ArgumentOutOfRangeException(nameof(startLetter));
        }

        for (var c = 'Z'; c >= start; c--)
        {
            if (!used.Contains(c))
            {
                yield return c;
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
