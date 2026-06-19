namespace CyberFeedForward.TheMadArchivist.Services;

public sealed class CommandBarSettingsService(IAppSettingsStore store)
{
    private const string CommandBarOnLeftKey = "Layout.CommandBarOnLeft";
    private readonly IAppSettingsStore _store = store;

    public bool IsCommandBarOnLeft()
    {
        return !_store.TryGetBool(CommandBarOnLeftKey, out var value) || value;
    }

    public void SetCommandBarOnLeft(bool onLeft)
    {
        _store.SetBool(CommandBarOnLeftKey, onLeft);
    }
}
