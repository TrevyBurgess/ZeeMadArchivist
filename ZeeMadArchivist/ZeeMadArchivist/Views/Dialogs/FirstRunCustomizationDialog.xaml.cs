using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.Utilities;
using CyberFeedForward.TheMadArchivist.ViewModels.Dialogs;
using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
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
    }

    private void FirstRunCustomizationDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        ViewModel.ErrorMessage = null;

        if (!ViewModel.TrySave())
        {
            args.Cancel = true;
            return;
        }

        ApplyUiChanges();
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
        var picker = new FileOpenPicker();
        picker.ViewMode = PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
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
