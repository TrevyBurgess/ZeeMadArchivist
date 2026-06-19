using Microsoft.UI.Xaml.Controls;
using System;

namespace CyberFeedForward.TheMadArchivist.Utilities;

public static class FrameNavigationExtensions
{
    public static bool NavigateIfNotCurrent(this Frame frame, Type sourcePageType)
    {
        ArgumentNullException.ThrowIfNull(frame);

        ArgumentNullException.ThrowIfNull(sourcePageType);

        if (frame.CurrentSourcePageType == sourcePageType)
        {
            return false;
        }

        return frame.Navigate(sourcePageType);
    }
}
