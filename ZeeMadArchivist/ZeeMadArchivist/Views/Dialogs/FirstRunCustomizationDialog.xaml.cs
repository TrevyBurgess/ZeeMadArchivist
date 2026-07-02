using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.Utilities;
using CyberFeedForward.TheMadArchivist.ViewModels.Dialogs;
using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace CyberFeedForward.TheMadArchivist.Views.Dialogs;

public sealed partial class FirstRunCustomizationDialog : ContentDialog
{
    private readonly FrameworkElement? _themeRootElement;

    public FirstRunCustomizationDialog()
        : this(LocalAppSettingsStore.Instance, App.MainWindowInstance?.Content as FrameworkElement)
    {
    }

    public FirstRunCustomizationDialog(IAppSettingsStore settingsStore, FrameworkElement? themeRootElement)
    {
        _themeRootElement = themeRootElement;
        ViewModel = new FirstRunCustomizationDialogViewModel(settingsStore);

        InitializeComponent();
        LoadSettings();
    }

    public FirstRunCustomizationDialogViewModel ViewModel { get; }

    private void LoadSettings()
    {
        ViewModel.LoadDefaults();
        MainContent.Title = ViewModel.Title;
        Closing += FirstRunCustomizationDialog_Closing;
        ViewModel.PropertyChanged += ViewModel_OnPropertyChanged;
        _ = UpdateIconPreviewAsync(ViewModel.SelectedIconPath);
    }

    private void ViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedIconPath))
        {
            _ = UpdateIconPreviewAsync(ViewModel.SelectedIconPath);
        }
    }

    private async Task UpdateIconPreviewAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            InitialIconPreviewImage.Source = null;
            InitialIconPreviewImage.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            using var fileStream = File.OpenRead(path);
            using var rasStream = fileStream.AsRandomAccessStream();
            await bitmap.SetSourceAsync(rasStream);
            InitialIconPreviewImage.Source = bitmap;
            InitialIconPreviewImage.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            InitialIconPreviewImage.Source = null;
            InitialIconPreviewImage.Visibility = Visibility.Collapsed;
        }
    }

    private async void FirstRunCustomizationDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        ViewModel.ErrorMessage = null;

        if (!ViewModel.TrySave())
        {
            args.Cancel = true;
            return;
        }

        ApplyUiChanges();
        await ShowTagsRegistrationResultAsync();
    }

    private async Task ShowTagsRegistrationResultAsync()
    {
        if (!ViewModel.RegisterTagsPropertyPage || string.IsNullOrWhiteSpace(ViewModel.TagsRegistrationErrorMessage))
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Tags Property Page",
            Content = ViewModel.TagsRegistrationErrorMessage,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot,
        };

        App.DialogShowing = true;
        await dialog.ShowAsync();
        App.DialogShowing = false;
    }

    private void ApplyUiChanges()
    {
        var themeMode = ViewModel.ThemeMode;
        var rootElement = _themeRootElement ?? (App.MainWindowInstance?.Content as FrameworkElement);
        if (rootElement is not null)
        {
            AppThemeManager.ApplyThemeMode(rootElement, themeMode);
        }

        if (App.MainWindowInstance is MainWindow mainWindow)
        {
            mainWindow.SetCommandBarOnLeft(ViewModel.IsCommandBarOnLeft);
        }

        FolderTools.LoadDefaultIcons(ViewModel.InitialCustomIconsPath);
    }

    private async void BrowseInitialArchivePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (folder is not null)
        {
            ViewModel.InitialArchivePath = folder.Path;
            ViewModel.ErrorMessage = null;
        }
    }

    private async void BrowseInitialCustomIconsPathButton_OnClick(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (folder is null)
        {
            return;
        }

        ViewModel.InitialCustomIconsPath = folder.Path;
        ViewModel.ErrorMessage = null;
    }

    private async void BrowseInitialIconPathButton_OnClick(object sender, RoutedEventArgs e)
    {
        var file = await PickIconFileAsync();
        if (file is null)
        {
            return;
        }

        ViewModel.SelectedIconPath = file.Path;
        ViewModel.ErrorMessage = null;
    }

    private static async Task<StorageFolder?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        if (App.MainWindowInstance is null)
        {
            return null;
        }

        var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
        InitializeWithWindow.Initialize(picker, hwnd);

        try
        {
            return await picker.PickSingleFolderAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return null;
        }
    }

    private static async Task<StorageFile?> PickIconFileAsync()
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.Thumbnail,
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };
        picker.FileTypeFilter.Add(".ico");

        if (App.MainWindowInstance is null)
        {
            return null;
        }

        var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
        InitializeWithWindow.Initialize(picker, hwnd);

        try
        {
            return await picker.PickSingleFileAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return null;
        }
    }
}
