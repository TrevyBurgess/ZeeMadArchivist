using CyberFeedForward.TheMadArchivist.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace CyberFeedForward.TheMadArchivist.Views.Dialogs;

public sealed partial class NewArchiveDialog : ContentDialog
{
    public NewArchiveDialogViewModel ViewModel { get; }

    public NewArchiveDialog(NewArchiveDialogViewModel.GetUnusedDriveLettersDelegate? getUnusedDriveLetters = null)
    {
        ViewModel = new NewArchiveDialogViewModel(getUnusedDriveLetters);
        InitializeComponent();
        PrimaryButtonClick += NewArchiveDialog_OnPrimaryButtonClick;
    }

    public string FolderPath => ViewModel.FolderPath;
    public string ArchiveName => ViewModel.ArchiveName;
    public string IconPath => ViewModel.IconPath;
    public char? SelectedDriveLetter => ViewModel.ParsedDriveLetter;

    private async void BrowseFolderButton_OnClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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
            Trace.TraceError(ex.ToString());
            return;
        }

        if (folder is null)
        {
            return;
        }

        ViewModel.ArchiveName = folder.Name;
        ViewModel.FolderPath = folder.Path;
        ViewModel.ErrorMessage = null;
    }

    private async void BrowseIconButton_OnClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.Thumbnail,
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };
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
            Trace.TraceError(ex.ToString());
            return;
        }

        if (file is null)
        {
            return;
        }

        ViewModel.IconPath = file.Path;
        ViewModel.ErrorMessage = null;
    }

    private void NewArchiveDialog_OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!ViewModel.Validate())
        {
            args.Cancel = true;
        }
    }
}
