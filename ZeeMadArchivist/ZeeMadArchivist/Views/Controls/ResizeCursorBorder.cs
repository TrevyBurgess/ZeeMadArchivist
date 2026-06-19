using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace CyberFeedForward.TheMadArchivist.Views.Controls;

public sealed partial class ResizeCursorBorder : UserControl
{
    private readonly Border _root;
    private Brush? _normalBackground;
    private readonly Brush? _highlightBackground;

    public InputCursor? ResizeCursor => ProtectedCursor;

    public ResizeCursorBorder()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);

        _root = new Border();
        Content = _root;

        _highlightBackground = new SolidColorBrush(Windows.UI.Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF));

        UpdateBackground();
        RegisterPropertyChangedCallback(BackgroundProperty, (_, _) => UpdateBackground());

        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
    }

    private void UpdateBackground()
    {
        _normalBackground = Background;
        _root.Background = Background;
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _root.Background = _highlightBackground ?? _normalBackground;
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _root.Background = _normalBackground;
    }
}
