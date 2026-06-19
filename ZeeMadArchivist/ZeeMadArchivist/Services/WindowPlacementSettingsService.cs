using System;

namespace CyberFeedForward.TheMadArchivist.Services;

public sealed class WindowPlacementSettingsService(IAppSettingsStore store)
{
    private const string WindowXKey = "Window.X";
    private const string WindowYKey = "Window.Y";
    private const string WindowWidthKey = "Window.Width";
    private const string WindowHeightKey = "Window.Height";

    private readonly IAppSettingsStore _store = store;

    public bool TryGetPlacement(out WindowPlacement placement)
    {
        if (!_store.TryGetInt(WindowXKey, out var x))
        {
            placement = default;
            return false;
        }

        if (!_store.TryGetInt(WindowYKey, out var y))
        {
            placement = default;
            return false;
        }

        if (!_store.TryGetInt(WindowWidthKey, out var width) || width <= 0)
        {
            placement = default;
            return false;
        }

        if (!_store.TryGetInt(WindowHeightKey, out var height) || height <= 0)
        {
            placement = default;
            return false;
        }

        placement = new WindowPlacement(x, y, width, height);
        return true;
    }

    public void SavePlacement(WindowPlacement placement)
    {
        if (placement.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placement), "Width must be greater than 0.");
        }

        if (placement.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placement), "Height must be greater than 0.");
        }

        _store.SetInt(WindowXKey, placement.X);
        _store.SetInt(WindowYKey, placement.Y);
        _store.SetInt(WindowWidthKey, placement.Width);
        _store.SetInt(WindowHeightKey, placement.Height);
    }
}

public readonly record struct WindowPlacement(int X, int Y, int Width, int Height);
