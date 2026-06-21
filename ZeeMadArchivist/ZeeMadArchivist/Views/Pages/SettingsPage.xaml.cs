using Microsoft.UI.Xaml.Controls;

namespace CyberFeedForward.TheMadArchivist.Views.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    public void ReloadArchives()
    {
        ArchiveListControl.ViewModel.ReloadArchives();
    }
}
