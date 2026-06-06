using Windows.Storage;

namespace CyberFeedForward.TheMadArchivist.Services;

public sealed class LocalAppSettingsStore : IAppSettingsStore
{
    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    private LocalAppSettingsStore() { }

    public static LocalAppSettingsStore Instance { get; } = new LocalAppSettingsStore();

    public bool TryGetBool(string key, out bool value)
    {
        if (_localSettings.Values.TryGetValue(key, out var stored) && stored is bool b)
        {
            value = b;
            return true;
        }

        value = false;
        return false;
    }

    public void SetBool(string key, bool value)
    {
        _localSettings.Values[key] = value;
    }

    public bool TryGetInt(string key, out int value)
    {
        if (_localSettings.Values.TryGetValue(key, out var stored) && stored is int i)
        {
            value = i;
            return true;
        }

        value = 0;
        return false;
    }

    public void SetInt(string key, int value)
    {
        _localSettings.Values[key] = value;
    }

    public bool TryGetString(string key, out string value)
    {
        if (_localSettings.Values.TryGetValue(key, out var stored) && stored is string s)
        {
            value = s;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public void SetString(string key, string value)
    {
        _localSettings.Values[key] = value;
    }
}
