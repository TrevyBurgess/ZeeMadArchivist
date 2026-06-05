using System;

namespace CyberFeedForward.TheMadArchivist.Services;

public sealed class FirstRunService(IAppSettingsStore store)
{
    private const string FirstRunCompletedKey = "App.FirstRun.Completed";

    private readonly IAppSettingsStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public bool ShouldRunFirstRunExperience()
    {
        return !_store.TryGetBool(FirstRunCompletedKey, out var completed) || !completed;
    }

    public void MarkFirstRunExperienceCompleted()
    {
        _store.SetBool(FirstRunCompletedKey, true);
    }

    public void ResetFirstRunExperience()
    {
        _store.SetBool(FirstRunCompletedKey, false);
    }
}
