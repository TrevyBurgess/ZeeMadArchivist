using System;

namespace CyberFeedForward.TheMadArchivist.Services;

public sealed class FirstRunService
{
    private const string FirstRunCompletedKey = "App.FirstRun.Completed";

    private IAppSettingsStore? _store;

    private FirstRunService() { }

    public IAppSettingsStore Store
    {
        get => _store ??= LocalAppSettingsStore.Instance;
        set => _store = value;
    }

    public static FirstRunService Instance { get; } = new FirstRunService();

    public bool ShouldRunFirstRunExperience()
    {
        return !Store.TryGetBool(FirstRunCompletedKey, out var completed) || !completed;
    }

    public void MarkFirstRunExperienceCompleted()
    {
        Store.SetBool(FirstRunCompletedKey, true);
    }

    public void ResetFirstRunExperience()
    {
        Store.SetBool(FirstRunCompletedKey, false);
    }

    public void DeleteAllSettings()
    {
        Store.Clear();
    }
}
