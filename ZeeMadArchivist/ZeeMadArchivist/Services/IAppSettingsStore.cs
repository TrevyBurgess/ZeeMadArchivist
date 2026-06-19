namespace CyberFeedForward.TheMadArchivist.Services;

public interface IAppSettingsStore
{
    bool TryGetBool(string key, out bool value);
    void SetBool(string key, bool value);

    bool TryGetInt(string key, out int value);
    void SetInt(string key, int value);

    bool TryGetString(string key, out string value);
    void SetString(string key, string value);

    void Clear();
}
