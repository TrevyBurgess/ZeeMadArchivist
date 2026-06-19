using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.Utilities;
using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace CyberFeedForward.TheMadArchivist.Views.Dialogs;

public sealed partial class FirstRunCustomizationDialog : ContentDialog
{
    private readonly ThemeSettingsService _themeSettingsService;
    private readonly CommandBarSettingsService _commandBarSettingsService;
    private readonly StartupSettingsService _startupSettingsService;
    private readonly ArchivesSettingsService _archivesSettingsService;
    private readonly CustomIconsSettingsService _customIconsSettingsService;
    private readonly IAppSettingsStore _settingsStore;
    private readonly FrameworkElement? _themeRootElement;

    public FirstRunCustomizationDialog()
        : this(LocalAppSettingsStore.Instance, App.MainWindowInstance?.Content as FrameworkElement)
    {
    }

    public FirstRunCustomizationDialog(IAppSettingsStore settingsStore, FrameworkElement? themeRootElement)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _themeSettingsService = new ThemeSettingsService(_settingsStore);
        _commandBarSettingsService = new CommandBarSettingsService(_settingsStore);
        _startupSettingsService = new StartupSettingsService();
        _archivesSettingsService = new ArchivesSettingsService(_settingsStore);
        _customIconsSettingsService = new CustomIconsSettingsService(_settingsStore);
        _themeRootElement = themeRootElement;

        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        MainContent.Title = "Welcome to The Mad Archivist!";

        ThemeModeComboBox.SelectedIndex = 0;
        CommandBarLocationToggleSwitch.IsOn = true;
        InitialArchivePathTextBox.Text = GetDefaultArchivePath();
        InitialCustomIconsPathTextBox.Text = GetDefaultCustomIconsPath();
        StartupToggleSwitch.IsOn = true;

        Closing += FirstRunCustomizationDialog_Closing;
    }

    private void FirstRunCustomizationDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        HideError();

        try
        {
            SaveSettings();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            args.Cancel = true;
            ShowError(ex.Message);
        }
    }

    private void SaveSettings()
    {
        var themeMode = ThemeModeComboBox.SelectedIndex switch
        {
            1 => AppThemeMode.Light,
            2 => AppThemeMode.Dark,
            _ => AppThemeMode.SystemDefault,
        };

        _themeSettingsService.SetThemeMode(themeMode);
        if (_themeRootElement is not null)
        {
            AppThemeManager.ApplyThemeMode(_themeRootElement, themeMode);
        }

        var commandBarOnLeft = CommandBarLocationToggleSwitch.IsOn;
        _commandBarSettingsService.SetCommandBarOnLeft(commandBarOnLeft);
        if (App.MainWindowInstance is MainWindow mainWindow)
        {
            mainWindow.SetCommandBarOnLeft(commandBarOnLeft);
        }

        _startupSettingsService.SetStartupEnabled(StartupToggleSwitch.IsOn);
        _settingsStore.SetBool("Settings.SetStartup", StartupToggleSwitch.IsOn);

        var initialArchivePath = InitialArchivePathTextBox.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(initialArchivePath))
        {
            _archivesSettingsService.SaveArchives([Path.GetFullPath(initialArchivePath)]);
        }

        var initialCustomIconsPath = InitialCustomIconsPathTextBox.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(initialCustomIconsPath))
        {
            var normalizedCustomIconsPath = Path.GetFullPath(initialCustomIconsPath);
            _customIconsSettingsService.SetCustomIconsFolderPath(normalizedCustomIconsPath);
            FolderTools.LoadDefaultIcons(normalizedCustomIconsPath);
        }
    }

    private static string GetDefaultArchivePath()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documentsPath))
        {
            documentsPath = "C:\\\\";
        }

        return Path.Combine(documentsPath, Properties.Resources.DefaultAppFolderName, Properties.Resources.DefaultArchiveName);
    }

    private static string GetDefaultCustomIconsPath()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documentsPath))
        {
            documentsPath = "C:\\\\";
        }

        return Path.Combine(documentsPath, Properties.Resources.DefaultAppFolderName, "CustomIcons");
    }

    private async void BrowseInitialArchivePathButton_OnClick(object sender, RoutedEventArgs e)
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

        InitialArchivePathTextBox.Text = folder.Path;
        HideError();
    }

    private async void BrowseInitialCustomIconsPathButton_OnClick(object sender, RoutedEventArgs e)
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

        InitialCustomIconsPathTextBox.Text = folder.Path;
        HideError();
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = string.IsNullOrWhiteSpace(message)
            ? "The app could not save your preferences. Check permissions and try again."
            : message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorTextBlock.Text = string.Empty;
        ErrorTextBlock.Visibility = Visibility.Collapsed;
    }
}
