using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace CyberFeedForward.TheMadArchivist.Converters;

public sealed class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isNullOrEmpty = value is null || (value is string s && string.IsNullOrWhiteSpace(s));
        return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
