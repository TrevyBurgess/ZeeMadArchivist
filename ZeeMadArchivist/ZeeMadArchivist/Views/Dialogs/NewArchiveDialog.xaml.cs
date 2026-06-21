using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace CyberFeedForward.TheMadArchivist.Views.Dialogs;

public sealed partial class NewArchiveDialog : ContentDialog
{
    private readonly Func<IEnumerable<char>> _getUnusedDriveLetters;

    public NewArchiveDialog(Func<IEnumerable<char>>? getUnusedDriveLetters = null)
    {
        InitializeComponent();
        _getUnusedDriveLetters = getUnusedDriveLetters ?? GetUnusedDriveLetters;

        var letters = _getUnusedDriveLetters().ToArray();
        foreach (var letter in letters)
        {
            DriveLetterComboBox.Items.Add(letter + ":");
        }

        if (DriveLetterComboBox.Items.Count > 0)
        {
            DriveLetterComboBox.SelectedIndex = 0;
        }

        var defaultIconPath = FolderTools.GetDefaultAppIconPath() ?? string.Empty;
        IconPathTextBox.Text = defaultIconPath;

        PrimaryButtonClick += NewArchiveDialog_OnPrimaryButtonClick;
    }

    public string FolderPath => FolderPathTextBox.Text?.Trim() ?? string.Empty;

    public string ArchiveName => ArchiveNameTextBox.Text?.Trim() ?? string.Empty;

    public string IconPath => IconPathTextBox.Text?.Trim() ?? string.Empty;

    private async void BrowseFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        if (App.MainWindowInstance is null)
        {
            return;
        }

        var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
        InitializeWithWindow.Initialize(picker, hwnd);

        StorageFolder? folder;
        try
        {
            folder = await picker.PickSingleFolderAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return;
        }

        if (folder is null)
        {
            return;
        }

        FolderPathTextBox.Text = folder.Path;
        ArchiveNameTextBox.Text = folder.Name;

        HideError();
    }

    private async void BrowseIconButton_OnClick(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.ViewMode = PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        picker.FileTypeFilter.Add(".ico");

        if (App.MainWindowInstance is null)
        {
            return;
        }

        var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
        InitializeWithWindow.Initialize(picker, hwnd);

        StorageFile? file;
        try
        {
            file = await picker.PickSingleFileAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return;
        }

        if (file is null)
        {
            return;
        }

        IconPathTextBox.Text = file.Path;
        HideError();
    }

    public char? SelectedDriveLetter
    {
        get
        {
            if (DriveLetterComboBox.SelectedItem is not string s)
            {
                return null;
            }

            var trimmed = s.Trim();
            if (trimmed.Length < 1)
            {
                return null;
            }

            return char.ToUpperInvariant(trimmed[0]);
        }
    }

    private void NewArchiveDialog_OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var folderPath = FolderPath;
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            ShowError("Folder path is required.");
            args.Cancel = true;
            return;
        }

        if (!Directory.Exists(folderPath))
        {
            ShowError("Folder path does not exist.");
            args.Cancel = true;
            return;
        }

        if (SelectedDriveLetter is null)
        {
            ShowError("Drive letter is required.");
            args.Cancel = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(ArchiveName))
        {
            ShowError("Archive name is required.");
            args.Cancel = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(IconPath))
        {
            ShowError("Icon path is required.");
            args.Cancel = true;
            return;
        }

        if (!File.Exists(IconPath))
        {
            ShowError("Icon file does not exist.");
            args.Cancel = true;
            return;
        }

        HideError();
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorTextBlock.Text = string.Empty;
        ErrorTextBlock.Visibility = Visibility.Collapsed;
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

        for (var c = start; c <= 'Z'; c++)
        {
            if (!used.Contains(c))
            {
                yield return c;
            }
        }
    }
}
