using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CyberFeedForward.TheMadArchivist.ViewModels.Dialogs;

public sealed partial class NewArchiveDialogViewModel : ViewModelBase
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

    public string FolderPath
    {
        get => _folderPath;
        set => SetField(ref _folderPath, value);
    }

    public string ArchiveName
    {
        get => _archiveName;
        set => SetField(ref _archiveName, value);
    }

    public string IconPath
    {
        get => _iconPath;
        set => SetField(ref _iconPath, value);
    }

    public string? SelectedDriveLetter
    {
        get => _selectedDriveLetter;
        set => SetField(ref _selectedDriveLetter, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public char? ParsedDriveLetter => DriveLetterHelper.ParseDriveLetter(SelectedDriveLetter);

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
        => DriveLetterHelper.GetUnusedDriveLetters();

    public static IEnumerable<char> GetUnusedDriveLetters(IEnumerable<char> usedDriveLetters, char startLetter = 'D')
        => DriveLetterHelper.GetUnusedDriveLetters(usedDriveLetters, startLetter);
}
