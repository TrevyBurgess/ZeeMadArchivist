using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.AppTools.FileSystem;
using CyberFeedForward.TheMadArchivist.ViewModels.Controls;
using CyberFeedForward.TheMadArchivist.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace CyberFeedForward.TheMadArchivist.Views.Controls;

public sealed partial class ArchiveListControl : UserControl
{
    public ArchiveListControl()
    {
        InitializeComponent();
        ViewModel = new ArchiveListControlViewModel(new ArchivesSettingsService(LocalAppSettingsStore.Instance));

        Loaded += ArchiveListControl_OnLoaded;
    }

    private void ArchiveListControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateNewArchiveButtonEnabled();
    }

    private void UpdateNewArchiveButtonEnabled()
    {
        try
        {
            NewArchiveButton.IsEnabled = NewArchiveDialog.GetUnusedDriveLetters().Any();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            NewArchiveButton.IsEnabled = false;
        }
    }

    public ArchiveListControlViewModel ViewModel
    {
        get => (ArchiveListControlViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ArchiveListControlViewModel),
            typeof(ArchiveListControl),
            new PropertyMetadata(null));

    private void AddArchiveButton_OnClick(object _, RoutedEventArgs e)
    {
        TryAddTypedFolderPathToArchives();
    }

    private void NewArchivePathTextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Enter)
        {
            return;
        }

        e.Handled = true;
        TryAddTypedFolderPathToArchives();
    }

    private void NewArchivePathTextBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        TryAddTypedFolderPathToArchives();
    }

    private void TryAddTypedFolderPathToArchives()
    {
        if (ViewModel is null)
        {
            return;
        }

        var candidatePath = ViewModel.NewArchivePath?.Trim();
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            return;
        }

        var result = ViewModel.TryAddFolderPath(candidatePath);

        if (App.MainWindowInstance is not MainWindow mainWindow)
        {
            return;
        }

        switch (result)
        {
            case ArchiveListControlViewModel.ArchiveAddResult.Added:
                mainWindow.SetStatusText("Folder Added");
                break;
            case ArchiveListControlViewModel.ArchiveAddResult.Duplicate:
                mainWindow.SetStatusText($"Archive already exists: {candidatePath}");
                break;
            case ArchiveListControlViewModel.ArchiveAddResult.NotFound:
                mainWindow.SetStatusText($"Folder not found: {candidatePath}");
                break;
            case ArchiveListControlViewModel.ArchiveAddResult.Error:
                mainWindow.SetStatusText($"Error adding folder: {candidatePath}");
                break;
            case ArchiveListControlViewModel.ArchiveAddResult.Empty:
            default:
                break;
        }
    }

    private async void BrowseArchiveButton_OnClick(object _, RoutedEventArgs e)
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
        catch
        {
            return;
        }

        if (folder is null)
        {
            return;
        }

        var candidatePath = folder.Path;
        var result = ViewModel.TryAddFolderPath(candidatePath, clearNewArchivePathOnSuccess: false);

        if (App.MainWindowInstance is not MainWindow mainWindow)
        {
            return;
        }

        switch (result)
        {
            case ArchiveListControlViewModel.ArchiveAddResult.Added:
                mainWindow.SetStatusText("Folder Added");
                break;
            case ArchiveListControlViewModel.ArchiveAddResult.Duplicate:
                mainWindow.SetStatusText($"Archive already exists: {candidatePath}");
                break;
            case ArchiveListControlViewModel.ArchiveAddResult.NotFound:
                mainWindow.SetStatusText($"Folder not found: {candidatePath}");
                break;
            case ArchiveListControlViewModel.ArchiveAddResult.Error:
                mainWindow.SetStatusText($"Error adding folder: {candidatePath}");
                break;
            case ArchiveListControlViewModel.ArchiveAddResult.Empty:
            default:
                break;
        }
    }

    private async void RemoveArchiveItemButton_OnClick(object sender, RoutedEventArgs _)
    {
        if (sender is not FrameworkElement fe)
        {
            return;
        }

        if (fe.DataContext is not string archivePath)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Remove folder?",
            Content = $"Remove this folder from the archives list?\n\n{archivePath}",
            PrimaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        var removed = ViewModel?.RemoveArchive(archivePath) == true;
        if (!removed)
        {
            return;
        }

        if (App.MainWindowInstance is MainWindow mainWindow)
        {
            mainWindow.SetStatusText("Folder Deleted");
        }
    }

    private async void NewArchiveButton_OnClick(object _, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        UpdateNewArchiveButtonEnabled();
        if (!NewArchiveButton.IsEnabled)
        {
            return;
        }

        var dialog = new NewArchiveDialog
        {
            XamlRoot = XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        var folderPath = dialog.FolderPath;
        var driveLetter = dialog.SelectedDriveLetter;
        if (string.IsNullOrWhiteSpace(folderPath) || driveLetter is null)
        {
            return;
        }

        try
        {
            var archiveName = new DirectoryInfo(folderPath).Name;
            var mapResult = FolderTools.MapDrive(folderPath, driveLetter.Value, archiveName);
            if (mapResult != 0)
            {
                throw FolderTools.CreateMapDriveException(mapResult, driveLetter.Value, folderPath);
            }

            if (!FolderTools.TrySetDefaultAppDriveIcon(driveLetter.Value, out var driveIconError))
            {
                throw new InvalidOperationException($"Failed to set mapped drive icon. {driveIconError}");
            }

            var addResult = ViewModel.TryAddFolderPath(folderPath, clearNewArchivePathOnSuccess: false);
            if (addResult != ArchiveListControlViewModel.ArchiveAddResult.Added)
            {
                if (App.MainWindowInstance is MainWindow mainWindow)
                {
                    mainWindow.SetStatusText($"Archive not added: {folderPath}");
                }
            }
            else
            {
                if (App.MainWindowInstance is MainWindow mainWindow)
                {
                    mainWindow.SetStatusText("New Archive Created");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());

            var errorDialog = new ContentDialog
            {
                Title = "New Archive Error",
                Content = ex.Message,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot,
            };

            await errorDialog.ShowAsync();
        }
    }
}
