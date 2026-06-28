using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.Tools.ZeeFileSystem.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CyberFeedForward.TheMadArchivist.ViewModels.Dialogs;

public sealed partial class FirstRunCustomizationDialogViewModel(
    IAppSettingsStore settingsStore,
    ThemeSettingsService themeSettingsService,
    CommandBarSettingsService commandBarSettingsService,
    StartupSettingsService startupSettingsService,
    ArchivesSettingsService archivesSettingsService,
    CustomIconsSettingsService customIconsSettingsService,
FirstRunCustomizationDialogViewModel.GetUnusedDriveLettersDelegate? getUnusedDriveLetters = null,
FirstRunCustomizationDialogViewModel.MapDriveDelegate? mapDrive = null,
FirstRunCustomizationDialogViewModel.TrySetDriveIconDelegate? trySetDriveIcon = null) : ViewModelBase
{
    public delegate IEnumerable<char> GetUnusedDriveLettersDelegate();
    public delegate int MapDriveDelegate(string folderPath, char driveLetter, string name);
    public delegate bool TrySetDriveIconDelegate(char driveLetter, string iconPath, out string errorMessage);

    private const string SetStartupPreferenceKey = "Settings.SetStartup";
    private const string DefaultAppFolderName = "ZeeMadArchivist";
    private const string DefaultArchiveName = "Archive";
    private const string DefaultCustomIconsFolderName = "CustomIcons";

    private readonly ThemeSettingsService _themeSettingsService = themeSettingsService ?? throw new ArgumentNullException(nameof(themeSettingsService));
    private readonly CommandBarSettingsService _commandBarSettingsService = commandBarSettingsService ?? throw new ArgumentNullException(nameof(commandBarSettingsService));
    private readonly StartupSettingsService _startupSettingsService = startupSettingsService ?? throw new ArgumentNullException(nameof(startupSettingsService));
    private readonly ArchivesSettingsService _archivesSettingsService = archivesSettingsService ?? throw new ArgumentNullException(nameof(archivesSettingsService));
    private readonly CustomIconsSettingsService _customIconsSettingsService = customIconsSettingsService ?? throw new ArgumentNullException(nameof(customIconsSettingsService));
    private readonly IAppSettingsStore _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
    private readonly GetUnusedDriveLettersDelegate _getUnusedDriveLetters = getUnusedDriveLetters ?? GetUnusedDriveLetters;
    private readonly MapDriveDelegate _mapDrive = mapDrive ?? FolderTools.MapDrive;
    private readonly TrySetDriveIconDelegate _trySetDriveIcon = trySetDriveIcon ?? FolderTools.TrySetDriveIcon;

    private string _title = string.Empty;
    private string _initialArchivePath = string.Empty;
    private string _initialCustomIconsPath = string.Empty;
    private string _archiveName = string.Empty;
    private string _selectedIconPath = string.Empty;
    private int _themeModeIndex;
    private bool _isCommandBarOnLeft;
    private bool _setStartup;
    private string? _errorMessage;
    private string? _selectedDriveLetter;

    public ObservableCollection<string> AvailableDriveLetters { get; } = [];

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public string InitialArchivePath
    {
        get => _initialArchivePath;
        set => SetField(ref _initialArchivePath, value);
    }

    public string ArchiveName
    {
        get => _archiveName;
        set => SetField(ref _archiveName, value);
    }

    public string SelectedIconPath
    {
        get => _selectedIconPath;
        set => SetField(ref _selectedIconPath, value);
    }

    public string InitialCustomIconsPath
    {
        get => _initialCustomIconsPath;
        set => SetField(ref _initialCustomIconsPath, value);
    }

    public int ThemeModeIndex
    {
        get => _themeModeIndex;
        set
        {
            if (!SetField(ref _themeModeIndex, value)) return;
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
        set => SetField(ref _isCommandBarOnLeft, value);
    }

    public bool SetStartup
    {
        get => _setStartup;
        set => SetField(ref _setStartup, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public string? SelectedDriveLetter
    {
        get => _selectedDriveLetter;
        set => SetField(ref _selectedDriveLetter, value);
    }

    public FirstRunCustomizationDialogViewModel(IAppSettingsStore settingsStore)
        : this(
            settingsStore,
            new ThemeSettingsService(settingsStore),
            new CommandBarSettingsService(settingsStore),
            new StartupSettingsService(),
            new ArchivesSettingsService(settingsStore),
            new CustomIconsSettingsService(settingsStore),
            GetUnusedDriveLetters,
            FolderTools.MapDrive,
            FolderTools.TrySetDriveIcon)
    {
    }

    public void LoadDefaults()
    {
        Title = "Welcome to Zee Mad Archivist!";
        ThemeModeIndex = 0;
        IsCommandBarOnLeft = true;
        ArchiveName = DefaultArchiveName;
        InitialArchivePath = GetDefaultArchivePath();
        InitialCustomIconsPath = GetDefaultCustomIconsPath();
        SelectedIconPath = FolderTools.GetDefaultAppIconPath() ?? string.Empty;
        SetStartup = true;
        ErrorMessage = null;
        SelectedDriveLetter = null;
        LoadDriveLetters();
    }

    public void LoadDriveLetters()
    {
        AvailableDriveLetters.Clear();
        foreach (var letter in _getUnusedDriveLetters())
        {
            AvailableDriveLetters.Add(letter + ":");
        }

        if (AvailableDriveLetters.Count > 0)
        {
            SelectedDriveLetter = AvailableDriveLetters[0];
        }
        else
        {
            SelectedDriveLetter = null;
        }
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
        var archiveName = ArchiveName?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(initialArchivePath) && !string.IsNullOrWhiteSpace(archiveName))
        {
            var fullArchivePath = Path.GetFullPath(Path.Combine(initialArchivePath, archiveName));
            var existingArchives = _archivesSettingsService.GetArchives();
            _archivesSettingsService.SaveArchives(existingArchives.Concat([fullArchivePath]));

            Directory.CreateDirectory(fullArchivePath);

            if (TryParseSelectedDriveLetter(out var driveLetter))
            {
                var mapResult = _mapDrive(fullArchivePath, driveLetter, archiveName);
                if (mapResult != 0)
                {
                    throw FolderTools.CreateMapDriveException(mapResult, driveLetter, fullArchivePath);
                }

                var iconPath = SelectedIconPath?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
                {
                    throw new InvalidOperationException($"Icon file does not exist. {iconPath}");
                }

                if (!_trySetDriveIcon(driveLetter, iconPath, out var driveIconError))
                {
                    throw new InvalidOperationException($"Failed to set mapped drive icon. {driveIconError}");
                }

            }
        }

        var initialCustomIconsPath = InitialCustomIconsPath?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(initialCustomIconsPath))
        {
            var normalizedCustomIconsPath = Path.GetFullPath(initialCustomIconsPath);
            _customIconsSettingsService.SetCustomIconsFolderPath(normalizedCustomIconsPath);
            FolderTools.LoadDefaultIcons(normalizedCustomIconsPath);
        }
    }

    private bool TryParseSelectedDriveLetter(out char driveLetter)
    {
        var parsed = DriveLetterHelper.ParseDriveLetter(SelectedDriveLetter);
        driveLetter = parsed ?? default;
        return parsed.HasValue;
    }

    public static string GetDefaultArchivePath()
    {
        return GetDefaultAppFolderPath();
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

    public static IEnumerable<char> GetUnusedDriveLetters()
        => DriveLetterHelper.GetUnusedDriveLetters();

    public static IEnumerable<char> GetUnusedDriveLetters(IEnumerable<char> usedDriveLetters, char startLetter = 'D')
        => DriveLetterHelper.GetUnusedDriveLetters(usedDriveLetters, startLetter);
}
