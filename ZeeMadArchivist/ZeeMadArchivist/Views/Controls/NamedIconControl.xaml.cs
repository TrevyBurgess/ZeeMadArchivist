using CyberFeedForward.TheMadArchivist.ViewModels.Controls;
using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace CyberFeedForward.TheMadArchivist.Views.Controls;

public sealed partial class NamedIconControl : UserControl
{
    public NamedIconControl()
    {
        InitializeComponent();
        ViewModel = new NamedIconControlViewModel();
        ViewModel.LoadFromProgramData();
    }

    public NamedIconControlViewModel ViewModel
    {
        get => (NamedIconControlViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(NamedIconControlViewModel),
            typeof(NamedIconControl),
            new PropertyMetadata(null));

    private async void CustomIconsBrowseButton_OnClick(object sender, RoutedEventArgs e)
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

        if (ViewModel is null)
        {
            return;
        }

        ViewModel.CustomIconsFolderPath = folder.Path;
    }

    private async void CustomIconsSaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var result = ViewModel.SaveCustomIconsFolderPath();
        if (result == NamedIconControlViewModel.CustomIconsFolderSaveResult.Saved)
        {
            return;
        }

        if (result == NamedIconControlViewModel.CustomIconsFolderSaveResult.DestinationExists)
        {
            var mergeDialog = new ContentDialog
            {
                Title = "Custom Icons Folder Exists",
                Content = "The selected custom icons folder already exists. Do you want to merge the current custom icons into it?",
                PrimaryButtonText = "Merge",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot,
            };

            var mergeResult = await mergeDialog.ShowAsync();
            if (mergeResult != ContentDialogResult.Primary)
            {
                return;
            }

            result = ViewModel.SaveCustomIconsFolderPath(mergeIfDestinationExists: true);
            if (result == NamedIconControlViewModel.CustomIconsFolderSaveResult.Saved)
            {
                return;
            }
        }

        var errorDialog = new ContentDialog
        {
            Title = "Save Custom Icons Folder Failed",
            Content = "The custom icons folder could not be saved. Check that the folder is accessible and try again.",
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };

        await errorDialog.ShowAsync();
    }

    private void LoadDefaultIconsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        FolderTools.LoadDefaultIcons(ViewModel.CustomIconsFolderPath);

        ViewModel.RefreshIcons();
    }

    private async void CustomIconsOpenFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var ok = FolderTools.TryOpenFolderInExplorer(ViewModel.CustomIconsFolderPath, out var errorMessage);
        if (ok)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Open Folder Failed",
            Content = string.IsNullOrWhiteSpace(errorMessage) ? "Unable to open folder." : errorMessage,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot,
        };

        await dialog.ShowAsync();
    }

    private async void ImportImagesButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (App.MainWindowInstance is null)
        {
            return;
        }

        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".bmp");
        picker.FileTypeFilter.Add(".gif");
        picker.FileTypeFilter.Add(".tiff");
        picker.FileTypeFilter.Add(".tif");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
        InitializeWithWindow.Initialize(picker, hwnd);

        IReadOnlyList<StorageFile> files;
        try
        {
            files = await picker.PickMultipleFilesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return;
        }

        if (files is null || files.Count == 0)
        {
            return;
        }

        var imagePaths = new List<string>();
        foreach (var f in files)
        {
            if (f is null || string.IsNullOrWhiteSpace(f.Path))
            {
                continue;
            }

            imagePaths.Add(f.Path);
        }

        var importResult = await ViewModel.ImportImagesAsIconsAsync(
            imagePaths,
            async destIconPath =>
            {
                var baseName = Path.GetFileNameWithoutExtension(destIconPath);
                var overwriteDialog = new ContentDialog
                {
                    Title = "Icon Already Exists",
                    Content = $"'{baseName}.ico' already exists in the CustomIcons folder. Overwrite it?",
                    PrimaryButtonText = "Overwrite",
                    SecondaryButtonText = "Skip",
                    CloseButtonText = "Cancel",
                    XamlRoot = XamlRoot,
                };

                var dialogResult = await overwriteDialog.ShowAsync();
                return dialogResult switch
                {
                    ContentDialogResult.Primary => NamedIconControlViewModel.IconOverwriteDecision.Overwrite,
                    ContentDialogResult.Secondary => NamedIconControlViewModel.IconOverwriteDecision.Skip,
                    _ => NamedIconControlViewModel.IconOverwriteDecision.Cancel,
                };
            });

        if (importResult.Errors.Count > 0)
        {
            var dialog = new ContentDialog
            {
                Title = "Import Completed With Errors",
                Content = string.Join(Environment.NewLine, importResult.Errors),
                CloseButtonText = "OK",
                XamlRoot = XamlRoot,
            };

            await dialog.ShowAsync();
        }
    }

    private async void IconListRowSaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (sender is not Button button)
        {
            return;
        }

        if (button.DataContext is not IconListItemViewModel row)
        {
            return;
        }

        var newBaseName = row.Name;
        var originalFilePath = row.FilePath;

        var oldBaseName = string.IsNullOrWhiteSpace(originalFilePath)
            ? string.Empty
            : Path.GetFileNameWithoutExtension(originalFilePath);

        if (string.Equals(oldBaseName, newBaseName, StringComparison.Ordinal))
        {
            return;
        }

        var ok = FolderTools.TryRenameIconFile(
            ViewModel.CustomIconsFolderPath,
            originalFilePath,
            newBaseName,
            out var errorMessage);

        if (ok)
        {
            ViewModel.RefreshIcons();
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Rename Failed",
            Content = string.IsNullOrWhiteSpace(errorMessage) ? "Unable to rename icon." : errorMessage,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot,
        };

        await dialog.ShowAsync();
    }

    private void IconListRowRevertButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.DataContext is not IconListItemViewModel row)
        {
            return;
        }

        row.Name = row.OriginalName;
    }
}
