using System;

namespace CyberFeedForward.TheMadArchivist.Services;

public sealed class CustomIconsSettingsService(IAppSettingsStore store)
{
    private const string CustomIconsFolderPathKey = "CustomIcons.FolderPath";
    private readonly IAppSettingsStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public string GetCustomIconsFolderPath()
    {
        return _store.TryGetString(CustomIconsFolderPathKey, out var stored)
            ? stored ?? string.Empty
            : string.Empty;
    }

    public void SetCustomIconsFolderPath(string folderPath)
    {
        _store.SetString(CustomIconsFolderPathKey, folderPath ?? string.Empty);
    }
}
