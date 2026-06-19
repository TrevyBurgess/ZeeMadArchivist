using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace CyberFeedForward.TheMadArchivist.Services;

public sealed class ArchivesSettingsService(IAppSettingsStore store)
{
    private const string ArchivesKey = "Archives.Paths";
    private readonly IAppSettingsStore _store = store;

    public IReadOnlyList<string> GetArchives()
    {
        if (!_store.TryGetString(ArchivesKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(json);
            if (parsed is null)
            {
                return [];
            }

            return [.. parsed
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)];
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return [];
        }
    }

    public void SaveArchives(IEnumerable<string> archives)
    {
        ArgumentNullException.ThrowIfNull(archives);

        var cleaned = archives
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var json = JsonSerializer.Serialize(cleaned);
        _store.SetString(ArchivesKey, json);
    }
}
