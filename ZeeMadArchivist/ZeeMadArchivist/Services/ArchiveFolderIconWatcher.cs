using CyberFeedForward.TheMadArchivist.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CyberFeedForward.TheMadArchivist.Services;

public sealed class ArchiveFolderIconWatcher : IDisposable
{
    private readonly ObservableCollection<ArchiveItem> _archives;
    private readonly CustomIconsSettingsService _customIconsSettingsService;
    private readonly Func<string, bool> _directoryExists;
    private readonly Func<string, IEnumerable<string>> _enumerateDirectories;
    private readonly Func<string, IEnumerable<string>> _enumerateFiles;
    private readonly Action<Action> _dispatchToUiThread;
    private readonly TimeSpan _debounceDelay;
    private readonly Dictionary<string, FileSystemWatcher> _watchers;

    private CancellationTokenSource? _scanCts;
    private bool _started;
    private bool _disposed;

    public ArchiveFolderIconWatcher(
        ObservableCollection<ArchiveItem> archives,
        CustomIconsSettingsService customIconsSettingsService,
        Func<string, bool>? directoryExists = null,
        Func<string, IEnumerable<string>>? enumerateDirectories = null,
        Func<string, IEnumerable<string>>? enumerateFiles = null,
        Action<Action>? dispatchToUiThread = null,
        TimeSpan? debounceDelay = null)
    {
        _archives = archives ?? throw new ArgumentNullException(nameof(archives));
        _customIconsSettingsService = customIconsSettingsService ?? throw new ArgumentNullException(nameof(customIconsSettingsService));
        _directoryExists = directoryExists ?? Directory.Exists;
        _enumerateDirectories = enumerateDirectories ?? Directory.EnumerateDirectories;
        _enumerateFiles = enumerateFiles ?? Directory.EnumerateFiles;
        _dispatchToUiThread = dispatchToUiThread ?? (action => action());
        _debounceDelay = debounceDelay ?? TimeSpan.FromMilliseconds(250);
        _watchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);

        _archives.CollectionChanged += Archives_OnCollectionChanged;
    }

    public void Start()
    {
        if (_disposed || _started)
        {
            return;
        }

        _started = true;
        RebuildWatchers();
        ScheduleScanAll();
    }

    public void Stop()
    {
        _started = false;
        CancelPendingScan();

        foreach (var watcher in _watchers.Values)
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        _watchers.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        _archives.CollectionChanged -= Archives_OnCollectionChanged;
    }

    private void RebuildWatchers()
    {
        foreach (var watcher in _watchers.Values)
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        _watchers.Clear();

        foreach (var archive in _archives)
        {
            TryAddWatcher(archive.Path);
        }
    }

    private void Archives_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (ArchiveItem item in e.NewItems!)
                {
                    TryAddWatcher(item.Path);
                }

                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (ArchiveItem item in e.OldItems!)
                {
                    RemoveWatcher(item.Path);
                }

                break;

            case NotifyCollectionChangedAction.Replace:
                foreach (ArchiveItem item in e.OldItems!)
                {
                    RemoveWatcher(item.Path);
                }

                foreach (ArchiveItem item in e.NewItems!)
                {
                    TryAddWatcher(item.Path);
                }

                break;

            case NotifyCollectionChangedAction.Reset:
                RebuildWatchers();
                break;
        }

        ScheduleScanAll();
    }

    private void TryAddWatcher(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || _watchers.ContainsKey(path) || !_directoryExists(path))
        {
            return;
        }

        try
        {
            var watcher = new FileSystemWatcher(path, "*")
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.DirectoryName,
                EnableRaisingEvents = false,
            };

            watcher.Created += (_, _) => OnArchiveFolderChanged(path);
            watcher.Renamed += (_, _) => OnArchiveFolderChanged(path);
            watcher.Deleted += (_, _) => OnArchiveFolderChanged(path);
            watcher.Error += (_, e) => OnWatcherError(e);

            _watchers[path] = watcher;

            if (_started)
            {
                watcher.EnableRaisingEvents = true;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
        }
    }

    private void RemoveWatcher(string path)
    {
        if (_watchers.TryGetValue(path, out var watcher))
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }

            _watchers.Remove(path);
        }
    }

    private void OnArchiveFolderChanged(string archivePath)
    {
        ScheduleScan(archivePath);
    }

    private void OnWatcherError(ErrorEventArgs e)
    {
        if (e.GetException() is { } ex)
        {
            Trace.TraceError(ex.ToString());
        }
    }

    private void ScheduleScan(string archivePath)
    {
        ScheduleScanCore(() => ScanArchive(archivePath));
    }

    private void ScheduleScanAll()
    {
        ScheduleScanCore(ScanAllArchives);
    }

    private void ScheduleScanCore(Action scan)
    {
        if (_disposed)
        {
            return;
        }

        CancelPendingScan();

        var cts = new CancellationTokenSource();
        _scanCts = cts;

        if (_debounceDelay <= TimeSpan.Zero)
        {
            _dispatchToUiThread(scan);
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_debounceDelay, cts.Token);
                _dispatchToUiThread(scan);
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

    private void CancelPendingScan()
    {
        try
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
        }
        finally
        {
            _scanCts = null;
        }
    }

    private void ScanAllArchives()
    {
        var iconMap = BuildIconMap();

        foreach (var archive in _archives)
        {
            UpdateArchiveIcon(archive, iconMap);
        }
    }

    private void ScanArchive(string archivePath)
    {
        var archive = _archives.FirstOrDefault(a => string.Equals(a.Path, archivePath, StringComparison.OrdinalIgnoreCase));
        if (archive is null)
        {
            return;
        }

        UpdateArchiveIcon(archive, BuildIconMap());
    }

    private void UpdateArchiveIcon(ArchiveItem archive, IReadOnlyDictionary<string, string> iconMap)
    {
        var match = FindMatchingIconPath(archive.Path, iconMap);
        _dispatchToUiThread(() => archive.CustomIconPath = match);
    }

    private IReadOnlyDictionary<string, string> BuildIconMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var folder = _customIconsSettingsService.GetCustomIconsFolderPath();
        if (string.IsNullOrWhiteSpace(folder) || !_directoryExists(folder))
        {
            return map;
        }

        try
        {
            foreach (var file in _enumerateFiles(folder))
            {
                if (string.IsNullOrWhiteSpace(file))
                {
                    continue;
                }

                if (!string.Equals(Path.GetExtension(file), ".ico", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(file);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    map[name] = file;
                }
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
        }

        return map;
    }

    private string? FindMatchingIconPath(string archivePath, IReadOnlyDictionary<string, string> iconMap)
    {
        if (string.IsNullOrWhiteSpace(archivePath) || !_directoryExists(archivePath))
        {
            return null;
        }

        try
        {
            foreach (var dir in _enumerateDirectories(archivePath))
            {
                if (string.IsNullOrWhiteSpace(dir))
                {
                    continue;
                }

                var name = Path.GetFileName(dir.Trim());
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (iconMap.TryGetValue(name, out var iconPath))
                {
                    return iconPath;
                }
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
        }

        return null;
    }
}
