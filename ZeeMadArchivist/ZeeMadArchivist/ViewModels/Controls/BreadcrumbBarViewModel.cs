using CyberFeedForward.TheMadArchivist.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CyberFeedForward.TheMadArchivist.ViewModels.Controls;

public sealed partial class BreadcrumbBarViewModel(IFileSystemService fileSystemService) : ViewModelBase
{
    private readonly IFileSystemService _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
    private string? _folderPath = "C:\\\\";

    public ObservableCollection<BreadcrumbSegmentViewModel> Segments { get; } = [];

    public string? FolderPath
    {
        get => _folderPath;
        set
        {
            if (SetField(ref _folderPath, value))
            {
                RebuildSegments();
            }
        }
    }

    private void RebuildSegments()
    {
        Segments.Clear();

        var paths = BuildCumulativePaths(FolderPath);
        foreach (var p in paths)
        {
            var items = GetSubFolderNames(p);
            Segments.Add(new BreadcrumbSegmentViewModel(p, items));
        }
    }

    public static IReadOnlyList<string> BuildCumulativePaths(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return [];
        }

        var normalized = folderPath.Trim();
        normalized = normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var root = Path.GetPathRoot(normalized);
        if (string.IsNullOrWhiteSpace(root))
        {
            return [normalized];
        }

        root = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var remainder = normalized[root.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parts = remainder.Length == 0
            ? []
            : remainder.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

        var results = new List<string>();
        var current = root + Path.DirectorySeparatorChar;
        results.Add(current);

        foreach (var part in parts)
        {
            current = Path.Combine(current, part);
            results.Add(current);
        }

        return [.. results];
    }

    private IReadOnlyList<string> GetSubFolderNames(string folderPath)
    {
        var entries = _fileSystemService.GetEntries(folderPath);
        return [.. entries
            .Where(e => e.IsFolder)
            .Select(e => e.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))];
    }

}

public sealed class BreadcrumbSegmentViewModel(string folderPath, IReadOnlyList<string> items)
{
    public string FolderPath { get; } = folderPath;

    public IReadOnlyList<string> Items { get; } = items;
}
