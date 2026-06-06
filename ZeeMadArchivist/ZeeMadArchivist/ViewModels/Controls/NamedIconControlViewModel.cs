using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.AppTools.Graphics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CyberFeedForward.TheMadArchivist.ViewModels.Controls;

public sealed partial class NamedIconControlViewModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private Symbol _iconSymbol = Symbol.Placeholder;
    private string _customIconsFolderPath = string.Empty;
    private string _savedCustomIconsFolderPath = string.Empty;
    private readonly Func<string> _getProgramDataFolder;
    private readonly Func<string, bool> _fileExists;
    private readonly Func<string, string> _readAllText;
    private readonly Action<string> _createDirectory;
    private readonly Action<string, string> _writeAllText;
    private readonly Func<string, bool> _directoryExists;
    private readonly Func<string, System.Collections.Generic.IEnumerable<string>> _enumerateFiles;
    private readonly Func<string> _getDocumentsFolder;
    private readonly CustomIconsSettingsService _customIconsSettingsService;
    private readonly Action<Action> _dispatchToUiThread;
    private readonly Func<string, ICustomIconsFolderWatcher> _createCustomIconsFolderWatcher;
    private readonly TimeSpan _customIconsWatcherDebounceDelay;
    private ICustomIconsFolderWatcher? _customIconsFolderWatcher;
    private CancellationTokenSource? _customIconsRefreshDebounceCts;
    private bool _isSaveEnabled;
    private bool _suppressDirtyTracking;

    private const string DefaultFolderName = "TheMadArchivist";
    private const string DefaultFileName = "NamedIconControl.json";

    private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions SerializeOptions = new() { WriteIndented = true };

    public NamedIconControlViewModel(
        Func<string>? getProgramDataFolder = null,
        Func<string, bool>? fileExists = null,
        Func<string, string>? readAllText = null,
        Action<string>? createDirectory = null,
        Action<string, string>? writeAllText = null,
        Func<string, bool>? directoryExists = null,
        Func<string, System.Collections.Generic.IEnumerable<string>>? enumerateFiles = null,
        Func<string>? getDocumentsFolder = null,
        CustomIconsSettingsService? customIconsSettingsService = null,
        Action<Action>? dispatchToUiThread = null,
        Func<string, ICustomIconsFolderWatcher>? createCustomIconsFolderWatcher = null,
        TimeSpan? customIconsWatcherDebounceDelay = null)
    {
        _getProgramDataFolder = getProgramDataFolder ?? (() => Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        _fileExists = fileExists ?? File.Exists;
        _readAllText = readAllText ?? File.ReadAllText;
        _createDirectory = createDirectory ?? (p => Directory.CreateDirectory(p));
        _writeAllText = writeAllText ?? File.WriteAllText;
        _directoryExists = directoryExists ?? Directory.Exists;
        _enumerateFiles = enumerateFiles ?? Directory.EnumerateFiles;
        _getDocumentsFolder = getDocumentsFolder ?? (() => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        _customIconsSettingsService = customIconsSettingsService ?? CreateDefaultCustomIconsSettingsService();

        var syncContext = SynchronizationContext.Current;
        _dispatchToUiThread = dispatchToUiThread ?? (action =>
        {
            if (action is null)
            {
                return;
            }

            if (syncContext is null)
            {
                action();
                return;
            }

            syncContext.Post(_ => action(), null);
        });

        _createCustomIconsFolderWatcher = createCustomIconsFolderWatcher ?? (folder => new FileSystemCustomIconsFolderWatcher(folder));
        _customIconsWatcherDebounceDelay = customIconsWatcherDebounceDelay ?? TimeSpan.FromMilliseconds(250);

        Items = [];
        IconList = [];
        Items.CollectionChanged += (_, __) =>
        {
            if (_suppressDirtyTracking)
            {
                return;
            }
            IsSaveEnabled = true;
        };
    }

    private static CustomIconsSettingsService CreateDefaultCustomIconsSettingsService()
    {
        try
        {
            return new CustomIconsSettingsService(LocalAppSettingsStore.Instance);
        }
        catch
        {
            return new CustomIconsSettingsService(new InMemoryAppSettingsStore());
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_name, next, StringComparison.Ordinal))
            {
                return;
            }

            _name = next;
            OnPropertyChanged();
        }
    }

    public Symbol IconSymbol
    {
        get => _iconSymbol;
        set
        {
            if (_iconSymbol == value)
            {
                return;
            }

            _iconSymbol = value;
            OnPropertyChanged();
        }
    }

    public string CustomIconsFolderPath
    {
        get => _customIconsFolderPath;
        set
        {
            var normalized = NormalizeCustomIconsFolderPath(value);
            if (string.Equals(_customIconsFolderPath, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _customIconsFolderPath = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCustomIconsPathSaveEnabled));
            RefreshIconList();
            ResetCustomIconsFolderWatcher();
        }
    }

    public bool IsCustomIconsPathSaveEnabled
    {
        get
        {
            return !string.Equals(
                NormalizeCustomIconsFolderPath(_savedCustomIconsFolderPath),
                NormalizeCustomIconsFolderPath(CustomIconsFolderPath),
                StringComparison.Ordinal);
        }
    }

    public ObservableCollection<NamedIconRowViewModel> Items { get; }

    public ObservableCollection<IconListItemViewModel> IconList { get; }

    public enum IconOverwriteDecision
    {
        Overwrite = 0,
        Skip = 1,
        Cancel = 2,
    }

    public enum CustomIconsFolderSaveResult
    {
        Saved = 0,
        DestinationExists = 1,
        Failed = 2,
    }

    public sealed class ImportIconsResult
    {
        public int ImportedCount { get; internal set; }

        public int SkippedCount { get; internal set; }

        public bool WasCancelled { get; internal set; }

        public System.Collections.Generic.List<string> Errors { get; } = [];
    }

    public async Task<ImportIconsResult> ImportImagesAsIconsAsync(
        System.Collections.Generic.IEnumerable<string> imagePaths,
        Func<string, Task<IconOverwriteDecision>> decideOverwriteAsync,
        Func<string, bool>? fileExists = null,
        Func<string, Icon>? toIcon = null,
        Action<Icon, string>? saveIcon = null)
    {
        ArgumentNullException.ThrowIfNull(imagePaths);
        ArgumentNullException.ThrowIfNull(decideOverwriteAsync);

        fileExists ??= File.Exists;
        toIcon ??= ImageTools.ToIcon;
        saveIcon ??= ImageTools.SaveIcon;

        var result = new ImportIconsResult();

        EnsureCustomIconsFolderExists(CustomIconsFolderPath);

        foreach (var imagePath in imagePaths)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                continue;
            }

            var baseName = Path.GetFileNameWithoutExtension(imagePath);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                continue;
            }

            var destIconPath = Path.Combine(CustomIconsFolderPath, baseName + ".ico");

            if (fileExists(destIconPath))
            {
                var decision = await decideOverwriteAsync(destIconPath);
                if (decision == IconOverwriteDecision.Skip)
                {
                    result.SkippedCount++;
                    continue;
                }

                if (decision == IconOverwriteDecision.Cancel)
                {
                    result.WasCancelled = true;
                    break;
                }
            }

            try
            {
                using var icon = toIcon(imagePath);
                saveIcon(icon, destIconPath);
                result.ImportedCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{Path.GetFileName(imagePath)}: {ex.Message}");
            }
        }

        RefreshIconList();
        return result;
    }

    public bool IsSaveEnabled
    {
        get => _isSaveEnabled;
        private set
        {
            if (_isSaveEnabled == value)
            {
                return;
            }

            _isSaveEnabled = value;
            OnPropertyChanged();
        }
    }

    public void RefreshIcons()
    {
        RefreshIconList();
    }

    public void LoadFromProgramData(string? folderName = null, string? fileName = null)
    {
        LoadCustomIconsFolderFromSettings();
        var folder = BuildDataFolderPath(folderName);
        var path = Path.Combine(folder, fileName ?? DefaultFileName);
        LoadFromJsonFile(path);
    }

    public void SaveToProgramData(string? folderName = null, string? fileName = null)
    {
        EnsureCustomIconsFolderExists(CustomIconsFolderPath);
        var folder = BuildDataFolderPath(folderName);
        var path = Path.Combine(folder, fileName ?? DefaultFileName);
        SaveToJsonFile(path);
    }

    private void LoadCustomIconsFolderFromSettings()
    {
        var stored = _customIconsSettingsService.GetCustomIconsFolderPath();
        var normalized = NormalizeCustomIconsFolderPath(stored);

        _savedCustomIconsFolderPath = normalized;
        _customIconsFolderPath = normalized;
        EnsureCustomIconsFolderExists(_savedCustomIconsFolderPath);
        RefreshIconList();
        ResetCustomIconsFolderWatcher();
        OnPropertyChanged(nameof(CustomIconsFolderPath));
        OnPropertyChanged(nameof(IsCustomIconsPathSaveEnabled));
    }

    public CustomIconsFolderSaveResult SaveCustomIconsFolderPath(bool mergeIfDestinationExists = false)
    {
        var normalized = NormalizeCustomIconsFolderPath(CustomIconsFolderPath);
        var previous = NormalizeCustomIconsFolderPath(_savedCustomIconsFolderPath);

        try
        {
            if (!string.Equals(previous, normalized, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(previous) &&
                _directoryExists(previous))
            {
                if (_directoryExists(normalized))
                {
                    if (!mergeIfDestinationExists)
                    {
                        return CustomIconsFolderSaveResult.DestinationExists;
                    }

                    MergeDirectoryContents(previous, normalized);
                    Directory.Delete(previous, recursive: true);
                }
                else
                {
                    var parent = Path.GetDirectoryName(normalized);
                    if (!string.IsNullOrWhiteSpace(parent))
                    {
                        _createDirectory(parent);
                    }

                    Directory.Move(previous, normalized);
                }
            }
            else
            {
                EnsureCustomIconsFolderExists(normalized);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return CustomIconsFolderSaveResult.Failed;
        }

        _savedCustomIconsFolderPath = normalized;
        _customIconsFolderPath = normalized;

        _customIconsSettingsService.SetCustomIconsFolderPath(_savedCustomIconsFolderPath);
        RefreshIconList();
        ResetCustomIconsFolderWatcher();

        OnPropertyChanged(nameof(CustomIconsFolderPath));
        OnPropertyChanged(nameof(IsCustomIconsPathSaveEnabled));
        return CustomIconsFolderSaveResult.Saved;
    }

    private static void MergeDirectoryContents(string sourceFolderPath, string destinationFolderPath)
    {
        Directory.CreateDirectory(destinationFolderPath);

        foreach (var sourceDirectory in Directory.EnumerateDirectories(sourceFolderPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceFolderPath, sourceDirectory);
            Directory.CreateDirectory(Path.Combine(destinationFolderPath, relativePath));
        }

        foreach (var sourceFile in Directory.EnumerateFiles(sourceFolderPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceFolderPath, sourceFile);
            var destinationFile = Path.Combine(destinationFolderPath, relativePath);
            var destinationDirectory = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Move(sourceFile, destinationFile, overwrite: true);
        }
    }

    private void ResetCustomIconsFolderWatcher()
    {
        try
        {
            _customIconsFolderWatcher?.Dispose();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
        }
        finally
        {
            _customIconsFolderWatcher = null;
        }

        var folder = NormalizeCustomIconsFolderPath(CustomIconsFolderPath);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        try
        {
            if (!_directoryExists(folder))
            {
                return;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return;
        }

        try
        {
            _customIconsFolderWatcher = _createCustomIconsFolderWatcher(folder);
            _customIconsFolderWatcher.IconsChanged += (_, __) => ScheduleIconListRefresh();
            _customIconsFolderWatcher.Start();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());

            try
            {
                _customIconsFolderWatcher?.Dispose();
            }
            catch (Exception disposeEx)
            {
                Trace.TraceError(disposeEx.ToString());
            }
            finally
            {
                _customIconsFolderWatcher = null;
            }
        }
    }

    private void ScheduleIconListRefresh()
    {
        try
        {
            _customIconsRefreshDebounceCts?.Cancel();
            _customIconsRefreshDebounceCts?.Dispose();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
        }

        var cts = new CancellationTokenSource();
        _customIconsRefreshDebounceCts = cts;

        if (_customIconsWatcherDebounceDelay <= TimeSpan.Zero)
        {
            DispatchRefreshIconList(cts.Token);
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_customIconsWatcherDebounceDelay, cts.Token);
                DispatchRefreshIconList(cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        });
    }

    private void DispatchRefreshIconList(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        _dispatchToUiThread(() =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            RefreshIconList();
        });
    }

    public interface ICustomIconsFolderWatcher : IDisposable
    {
        event EventHandler? IconsChanged;

        void Start();
    }

    private sealed class FileSystemCustomIconsFolderWatcher : ICustomIconsFolderWatcher
    {
        private readonly FileSystemWatcher _watcher;

        public FileSystemCustomIconsFolderWatcher(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException("Folder path cannot be empty.", nameof(folderPath));
            }

            _watcher = new FileSystemWatcher(folderPath, "*.ico")
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = false,
            };

            _watcher.Created += Watcher_OnChanged;
            _watcher.Changed += Watcher_OnChanged;
            _watcher.Deleted += Watcher_OnChanged;
            _watcher.Renamed += Watcher_OnRenamed;
            _watcher.Error += Watcher_OnError;
        }

        public event EventHandler? IconsChanged;

        public void Start()
        {
            _watcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= Watcher_OnChanged;
            _watcher.Changed -= Watcher_OnChanged;
            _watcher.Deleted -= Watcher_OnChanged;
            _watcher.Renamed -= Watcher_OnRenamed;
            _watcher.Error -= Watcher_OnError;
            _watcher.Dispose();
        }

        private void Watcher_OnChanged(object sender, FileSystemEventArgs e)
        {
            IconsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Watcher_OnRenamed(object sender, RenamedEventArgs e)
        {
            IconsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Watcher_OnError(object sender, ErrorEventArgs e)
        {
            if (e.GetException() is { } ex)
            {
                Trace.TraceError(ex.ToString());
            }

            IconsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void RefreshIconList()
    {
        IconList.Clear();

        var folder = NormalizeCustomIconsFolderPath(CustomIconsFolderPath);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        try
        {
            if (!_directoryExists(folder))
            {
                return;
            }
        }
        catch
        {
            return;
        }

        System.Collections.Generic.IEnumerable<string> files;
        try
        {
            files = _enumerateFiles(folder);
        }
        catch
        {
            return;
        }

        foreach (var f in files
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Trim())
            .Where(f => string.Equals(Path.GetExtension(f), ".ico", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
        {
            IconList.Add(new IconListItemViewModel(f));
        }
    }

    private string NormalizeCustomIconsFolderPath(string? candidate)
    {
        var next = candidate?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(next))
        {
            return next;
        }

        var docs = _getDocumentsFolder();
        if (string.IsNullOrWhiteSpace(docs))
        {
            return "CustomIcons";
        }

        return Path.Combine(docs, "ZeeMadArchivist", "CustomIcons");
    }

    private void EnsureCustomIconsFolderExists(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || FirstRunService.Instance.ShouldRunFirstRunExperience())
        {
            return;
        }

        try
        {
            if (_directoryExists(folderPath))
            {
                return;
            }

            _createDirectory(folderPath);
        }
        catch
        {
        }
    }

    private string BuildDataFolderPath(string? folderName)
    {
        var baseFolder = _getProgramDataFolder();
        if (string.IsNullOrWhiteSpace(baseFolder))
        {
            baseFolder = AppContext.BaseDirectory;
        }

        return Path.Combine(baseFolder, folderName ?? DefaultFolderName);
    }

    public void LoadFromJsonFile(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            throw new ArgumentException("Path cannot be empty.", nameof(jsonFilePath));
        }

        _suppressDirtyTracking = true;
        Items.Clear();
        IsSaveEnabled = false;

        if (!_fileExists(jsonFilePath))
        {
            _suppressDirtyTracking = false;
            return;
        }

        string json;
        try
        {
            json = _readAllText(jsonFilePath);
        }
        catch
        {
            _suppressDirtyTracking = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            _suppressDirtyTracking = false;
            return;
        }

        NamedIconControlFileModel? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<NamedIconControlFileModel>(json, DeserializeOptions);
        }
        catch
        {
            _suppressDirtyTracking = false;
            return;
        }

        if (parsed?.Items is null)
        {
            _suppressDirtyTracking = false;
            return;
        }

        foreach (var i in parsed.Items.Where(i => i is not null))
        {
            var iconPath = i!.IconPath ?? string.Empty;
            var text = i.Text ?? string.Empty;

            var row = new NamedIconRowViewModel(iconPath, text);
            row.PropertyChanged += (_, __) =>
            {
                if (_suppressDirtyTracking)
                {
                    return;
                }

                IsSaveEnabled = true;
            };
            Items.Add(row);
        }

        _suppressDirtyTracking = false;
    }

    public void SaveToJsonFile(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            throw new ArgumentException("Path cannot be empty.", nameof(jsonFilePath));
        }

        var folder = Path.GetDirectoryName(jsonFilePath);
        if (string.IsNullOrWhiteSpace(folder))
        {
            throw new ArgumentException("Path must include a directory.", nameof(jsonFilePath));
        }

        _createDirectory(folder);

        var model = new NamedIconControlFileModel
        {
            Items = [.. Items
                .Select(i => new NamedIconRowFileModel
                {
                    IconPath = i.IconPath,
                    Text = i.Text,
                })],
        };

        var json = JsonSerializer.Serialize(model, SerializeOptions);
        _writeAllText(jsonFilePath, json);
        IsSaveEnabled = false;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class NamedIconControlFileModel
    {
        public System.Collections.Generic.List<NamedIconRowFileModel>? Items { get; set; }
    }

    private sealed class NamedIconRowFileModel
    {
        public string? IconPath { get; set; }
        public string? Text { get; set; }
    }

    private sealed class InMemoryAppSettingsStore : IAppSettingsStore
    {
        private readonly System.Collections.Generic.Dictionary<string, object?> _values = new(StringComparer.Ordinal);

        public bool TryGetBool(string key, out bool value)
        {
            if (_values.TryGetValue(key, out var stored) && stored is bool b)
            {
                value = b;
                return true;
            }

            value = false;
            return false;
        }

        public void SetBool(string key, bool value)
        {
            _values[key] = value;
        }

        public bool TryGetInt(string key, out int value)
        {
            if (_values.TryGetValue(key, out var stored) && stored is int i)
            {
                value = i;
                return true;
            }

            value = 0;
            return false;
        }

        public void SetInt(string key, int value)
        {
            _values[key] = value;
        }

        public bool TryGetString(string key, out string value)
        {
            if (_values.TryGetValue(key, out var stored) && stored is string s)
            {
                value = s;
                return true;
            }

            value = string.Empty;
            return false;
        }

        public void SetString(string key, string value)
        {
            _values[key] = value;
        }
    }
}

public sealed class IconListItemViewModel : INotifyPropertyChanged
{
    private string _name = string.Empty;

    public IconListItemViewModel(string filePath)
    {
        FilePath = filePath ?? string.Empty;
        var initialName = string.IsNullOrWhiteSpace(FilePath)
            ? string.Empty
            : Path.GetFileNameWithoutExtension(FilePath);

        OriginalName = initialName;
        _name = initialName;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string FilePath { get; }

    public string OriginalName { get; }

    public bool IsDirty
    {
        get
        {
            return !string.Equals(Name, OriginalName, StringComparison.Ordinal);
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_name, next, StringComparison.Ordinal))
            {
                return;
            }

            _name = next;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDirty)));
        }
    }

    public BitmapImage? IconImage
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                return null;
            }

            try
            {
                return new BitmapImage(new Uri(FilePath, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                return null;
            }
        }
    }
}

public sealed partial class NamedIconRowViewModel(string? iconPath, string? text) : INotifyPropertyChanged
{
    private string _iconPath = iconPath ?? string.Empty;
    private string _text = text ?? string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string IconPath
    {
        get => _iconPath;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_iconPath, next, StringComparison.Ordinal))
            {
                return;
            }

            _iconPath = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IconUri));
            OnPropertyChanged(nameof(IconImage));
        }
    }

    public Uri? IconUri
    {
        get
        {
            if (string.IsNullOrWhiteSpace(IconPath))
            {
                return null;
            }

            try
            {
                return new Uri(IconPath, UriKind.RelativeOrAbsolute);
            }
            catch
            {
                return null;
            }
        }
    }

    public BitmapImage? IconImage
    {
        get
        {
            if (string.IsNullOrWhiteSpace(IconPath))
            {
                return null;
            }

            try
            {
                return new BitmapImage(new Uri(IconPath, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                return null;
            }
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_text, next, StringComparison.Ordinal))
            {
                return;
            }

            _text = next;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
