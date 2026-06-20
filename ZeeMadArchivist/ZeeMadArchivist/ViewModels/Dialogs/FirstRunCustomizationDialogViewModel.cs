using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace CyberFeedForward.TheMadArchivist.ViewModels.Dialogs;

public sealed partial class FirstRunCustomizationDialogViewModel : INotifyPropertyChanged
{
    private const string SetStartupPreferenceKey = "Settings.SetStartup";
    private const string DefaultAppFolderName = "ZeeMadArchivist";
    private const string DefaultArchiveName = "Archive";
    private const string DefaultCustomIconsFolderName = "CustomIcons";

    private readonly ThemeSettingsService _themeSettingsService;
    private readonly CommandBarSettingsService _commandBarSettingsService;
    private readonly StartupSettingsService _startupSettingsService;
    private readonly ArchivesSettingsService _archivesSettingsService;
    private readonly CustomIconsSettingsService _customIconsSettingsService;
    private readonly IAppSettingsStore _settingsStore;

    private string _title = string.Empty;
    private string _initialArchivePath = string.Empty;
    private string _initialCustomIconsPath = string.Empty;
    private int _themeModeIndex;
    private bool _isCommandBarOnLeft;
    private bool _setStartup;
    private string? _errorMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
            {
                return;
            }

            _title = value;
            OnPropertyChanged();
        }
    }

    public string InitialArchivePath
    {
        get => _initialArchivePath;
        set
        {
            if (_initialArchivePath == value)
            {
                return;
            }

            _initialArchivePath = value;
            OnPropertyChanged();
        }
    }

    public string InitialCustomIconsPath
    {
        get => _initialCustomIconsPath;
        set
        {
            if (_initialCustomIconsPath == value)
            {
                return;
            }

            _initialCustomIconsPath = value;
            OnPropertyChanged();
        }
    }

    public int ThemeModeIndex
    {
        get => _themeModeIndex;
        set
        {
            if (_themeModeIndex == value)
            {
                return;
            }

            _themeModeIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ThemeMode));
        }
    }

    public AppThemeMode ThemeMode
    {
        get => ThemeModeIndex switch
        {
            1 => AppThemeMode.Light,
            2 => AppThemeMode.Dark,
            _ => AppThemeMode.SystemDefault,
        };
        set => ThemeModeIndex = value switch
        {
            AppThemeMode.Light => 1,
            AppThemeMode.Dark => 2,
            _ => 0,
        };
    }

    public bool IsCommandBarOnLeft
    {
        get => _isCommandBarOnLeft;
        set
        {
            if (_isCommandBarOnLeft == value)
            {
                return;
            }

            _isCommandBarOnLeft = value;
            OnPropertyChanged();
        }
    }

    public bool SetStartup
    {
        get => _setStartup;
        set
        {
            if (_setStartup == value)
            {
                return;
            }

            _setStartup = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage == value)
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public FirstRunCustomizationDialogViewModel(IAppSettingsStore settingsStore)
        : this(
            settingsStore,
            new ThemeSettingsService(settingsStore),
            new CommandBarSettingsService(settingsStore),
            new StartupSettingsService(),
            new ArchivesSettingsService(settingsStore),
            new CustomIconsSettingsService(settingsStore))
    {
    }

    public FirstRunCustomizationDialogViewModel(
        IAppSettingsStore settingsStore,
        ThemeSettingsService themeSettingsService,
        CommandBarSettingsService commandBarSettingsService,
        StartupSettingsService startupSettingsService,
        ArchivesSettingsService archivesSettingsService,
        CustomIconsSettingsService customIconsSettingsService)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _themeSettingsService = themeSettingsService ?? throw new ArgumentNullException(nameof(themeSettingsService));
        _commandBarSettingsService = commandBarSettingsService ?? throw new ArgumentNullException(nameof(commandBarSettingsService));
        _startupSettingsService = startupSettingsService ?? throw new ArgumentNullException(nameof(startupSettingsService));
        _archivesSettingsService = archivesSettingsService ?? throw new ArgumentNullException(nameof(archivesSettingsService));
        _customIconsSettingsService = customIconsSettingsService ?? throw new ArgumentNullException(nameof(customIconsSettingsService));
    }

    public void LoadDefaults()
    {
        Title = "Welcome to The Mad Archivist!";
        ThemeModeIndex = 0;
        IsCommandBarOnLeft = true;
        InitialArchivePath = GetDefaultArchivePath();
        InitialCustomIconsPath = GetDefaultCustomIconsPath();
        SetStartup = true;
        ErrorMessage = null;
    }

    public bool TrySave()
    {
        ErrorMessage = null;

        try
        {
            SaveSettings();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            ErrorMessage = string.IsNullOrWhiteSpace(ex.Message)
                ? "The app could not save your preferences. Check permissions and try again."
                : ex.Message;
            return false;
        }
    }

    private void SaveSettings()
    {
        var themeMode = ThemeMode;
        _themeSettingsService.SetThemeMode(themeMode);

        _commandBarSettingsService.SetCommandBarOnLeft(IsCommandBarOnLeft);

        _startupSettingsService.SetStartupEnabled(SetStartup);
        _settingsStore.SetBool(SetStartupPreferenceKey, SetStartup);

        var initialArchivePath = InitialArchivePath?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(initialArchivePath))
        {
            _archivesSettingsService.SaveArchives([Path.GetFullPath(initialArchivePath)]);
        }

        var initialCustomIconsPath = InitialCustomIconsPath?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(initialCustomIconsPath))
        {
            var normalizedCustomIconsPath = Path.GetFullPath(initialCustomIconsPath);
            _customIconsSettingsService.SetCustomIconsFolderPath(normalizedCustomIconsPath);
            FolderTools.LoadDefaultIcons(normalizedCustomIconsPath);
        }
    }

    public static string GetDefaultArchivePath()
    {
        return Path.Combine(GetDefaultAppFolderPath(), DefaultArchiveName);
    }

    public static string GetDefaultCustomIconsPath()
    {
        return Path.Combine(GetDefaultAppFolderPath(), DefaultCustomIconsFolderName);
    }

    private static string GetDefaultAppFolderPath()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documentsPath))
        {
            documentsPath = "C:\\";
        }

        return Path.Combine(documentsPath, DefaultAppFolderName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
