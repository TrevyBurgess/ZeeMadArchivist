using System;

namespace CyberFeedForward.TheMadArchivist.Services;

public sealed class FirstRunService
{
    private const string FirstRunCompletedKey = "App.FirstRun.Completed";

    private FirstRunService() { }

    public IAppSettingsStore Store = LocalAppSettingsStore.Instance;

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
}
