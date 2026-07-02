using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.Utilities;
using CyberFeedForward.Tools.ZeeFileSystem.Services;
using Microsoft.UI.Xaml;
using System;

namespace CyberFeedForward.TheMadArchivist.ViewModels.Pages;

public sealed partial class SettingsPageViewModel : ViewModelBase
{
    private const string SetStartupPreferenceKey = "Settings.SetStartup";

    private readonly ThemeSettingsService _themeSettingsService;
    private readonly CommandBarSettingsService _commandBarSettingsService;
    private readonly StartupSettingsService _startupSettingsService;
    private readonly IAppSettingsStore _settingsStore;
    private readonly FrameworkElement? _themeRootElement;
    private AppThemeMode _themeMode;
    private bool _isCommandBarOnLeft;
    private bool _setStartup;

    public SettingsPageViewModel()
        : this(CreateDefaultSettingsStore())
    {
    }

    private SettingsPageViewModel(IAppSettingsStore settingsStore)
        : this(
            new ThemeSettingsService(settingsStore),
            new CommandBarSettingsService(settingsStore),
            new StartupSettingsService(),
            App.MainWindowInstance?.Content as FrameworkElement,
            settingsStore)
    {
    }

    private static LocalAppSettingsStore CreateDefaultSettingsStore()
    {
        return LocalAppSettingsStore.Instance;
    }

    public SettingsPageViewModel(
        ThemeSettingsService themeSettingsService,
        CommandBarSettingsService commandBarSettingsService,
        FrameworkElement? themeRootElement,
        IAppSettingsStore? settingsStore = null)
        : this(themeSettingsService, commandBarSettingsService, new StartupSettingsService(), themeRootElement, settingsStore)
    {
    }

    public SettingsPageViewModel(
        ThemeSettingsService themeSettingsService,
        CommandBarSettingsService commandBarSettingsService,
        StartupSettingsService startupSettingsService,
        FrameworkElement? themeRootElement,
        IAppSettingsStore? settingsStore = null)
    {
        _themeSettingsService = themeSettingsService;
        _commandBarSettingsService = commandBarSettingsService;
        _startupSettingsService = startupSettingsService;
        _themeRootElement = themeRootElement;
        _settingsStore = settingsStore ?? LocalAppSettingsStore.Instance;

        _themeMode = _themeSettingsService.GetThemeMode();
        _isCommandBarOnLeft = _commandBarSettingsService.IsCommandBarOnLeft();

        var hasPreference = _settingsStore.TryGetBool(SetStartupPreferenceKey, out var preferredStartupEnabled);
        if (!hasPreference)
        {
            preferredStartupEnabled = true;
            _settingsStore.SetBool(SetStartupPreferenceKey, preferredStartupEnabled);
        }

        try
        {
            _startupSettingsService.SetStartupEnabled(preferredStartupEnabled);
            _setStartup = _startupSettingsService.MinimizeToTray();
            _settingsStore.SetBool(SetStartupPreferenceKey, _setStartup);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            try
            {
                _setStartup = _startupSettingsService.MinimizeToTray();
                _settingsStore.SetBool(SetStartupPreferenceKey, _setStartup);
            }
            catch (Exception ex2)
            {
                System.Diagnostics.Trace.TraceError(ex2.ToString());
            }
        }
    }

    public AppThemeMode ThemeMode
    {
        get => _themeMode;
        set
        {
            if (!SetField(ref _themeMode, value)) return;
            OnPropertyChanged(nameof(IsDarkModeEnabled));
            OnPropertyChanged(nameof(ThemeModeIndex));
            _themeSettingsService.SetThemeMode(value);
            var rootElement = _themeRootElement ?? (App.MainWindowInstance?.Content as FrameworkElement);
            if (rootElement is not null)
            {
                AppThemeManager.ApplyThemeMode(rootElement, value);
            }
        }
    }

    public bool SetStartup
    {
        get => _setStartup;
        set => SetField(ref _setStartup, value);
    }

    public bool TrySetStartupEnabled(bool enabled, out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            _startupSettingsService.SetStartupEnabled(enabled);
            SetStartup = _startupSettingsService.MinimizeToTray();
            _settingsStore.SetBool(SetStartupPreferenceKey, SetStartup);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public bool IsDarkModeEnabled
    {
        get => ThemeMode == AppThemeMode.Dark;
        set => ThemeMode = value ? AppThemeMode.Dark : AppThemeMode.Light;
    }

    public int ThemeModeIndex
    {
        get => ThemeMode switch
        {
            AppThemeMode.SystemDefault => 0,
            AppThemeMode.Light => 1,
            _ => 2,
        };
        set => ThemeMode = value switch
        {
            1 => AppThemeMode.Light,
            2 => AppThemeMode.Dark,
            _ => AppThemeMode.SystemDefault,
        };
    }

    public bool IsCommandBarOnLeft
    {
        get => _isCommandBarOnLeft;
        set
        {
            if (!SetField(ref _isCommandBarOnLeft, value)) return;
            _commandBarSettingsService.SetCommandBarOnLeft(value);
            if (App.MainWindowInstance is MainWindow mainWindow)
            {
                mainWindow.SetCommandBarOnLeft(value);
            }
        }
    }

    /// <summary>
    /// Checks whether the Tags shell property page is registered in the registry.
    /// </summary>
    /// <returns><c>true</c> if the required registry keys are present; otherwise <c>false</c>.</returns>
    public bool IsTagsPropertyPageRegistered()
    {
        return ShellServices.IsTagsPropertyPageRegistered();
    }
}
