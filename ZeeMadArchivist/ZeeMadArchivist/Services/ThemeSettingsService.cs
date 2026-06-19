using System;

namespace CyberFeedForward.TheMadArchivist.Services;

public sealed class ThemeSettingsService(IAppSettingsStore store)
{
    private const string DarkModeEnabledKey = "Theme.DarkModeEnabled";
    private const string ThemeModeKey = "Theme.Mode";
    private readonly IAppSettingsStore _store = store;

    public bool IsDarkModeEnabled()
    {
        return GetThemeMode() == AppThemeMode.Dark;
    }

    public void SetDarkModeEnabled(bool enabled)
    {
        SetThemeMode(enabled ? AppThemeMode.Dark : AppThemeMode.Light);
    }

    public AppThemeMode GetThemeMode()
    {
        if (_store.TryGetInt(ThemeModeKey, out var stored))
        {
            if (Enum.IsDefined(typeof(AppThemeMode), stored))
            {
                return (AppThemeMode)stored;
            }
        }

        if (_store.TryGetBool(DarkModeEnabledKey, out var legacyDark))
        {
            return legacyDark ? AppThemeMode.Dark : AppThemeMode.Light;
        }

        return AppThemeMode.SystemDefault;
    }

    public void SetThemeMode(AppThemeMode mode)
    {
        _store.SetInt(ThemeModeKey, (int)mode);
    }
}
