using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;

namespace CyberFeedForward.TheMadArchivist.Views.Dialogs;

public sealed partial class FirstRunCustomizationDialog : ContentDialog
{
    private readonly ThemeSettingsService _themeSettingsService;
    private readonly CommandBarSettingsService _commandBarSettingsService;
    private readonly StartupSettingsService _startupSettingsService;
    private readonly IAppSettingsStore _settingsStore;
    private readonly FrameworkElement? _themeRootElement;

    public FirstRunCustomizationDialog()
        : this(new LocalAppSettingsStore(), App.MainWindowInstance?.Content as FrameworkElement)
    {
    }

    public FirstRunCustomizationDialog(IAppSettingsStore settingsStore, FrameworkElement? themeRootElement)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _themeSettingsService = new ThemeSettingsService(_settingsStore);
        _commandBarSettingsService = new CommandBarSettingsService(_settingsStore);
        _startupSettingsService = new StartupSettingsService();
        _themeRootElement = themeRootElement;

        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        ThemeModeComboBox.SelectedIndex = _themeSettingsService.GetThemeMode() switch
        {
            AppThemeMode.Light => 1,
            AppThemeMode.Dark => 2,
            _ => 0,
        };

        CommandBarLocationToggleSwitch.IsOn = _commandBarSettingsService.IsCommandBarOnLeft();

        try
        {
            StartupToggleSwitch.IsOn = _startupSettingsService.IsStartupEnabled();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            StartupToggleSwitch.IsOn = false;
        }

        PrimaryButtonClick += FirstRunCustomizationDialog_PrimaryButtonClick;
    }

    private void FirstRunCustomizationDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        HideError();

        try
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
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            args.Cancel = true;
            ShowError(ex.Message);
        }
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
