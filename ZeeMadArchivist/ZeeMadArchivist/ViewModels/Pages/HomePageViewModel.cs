using System;

namespace CyberFeedForward.TheMadArchivist.ViewModels.Pages;

public sealed partial class HomePageViewModel : ViewModelBase
{
    private string _folderPath = string.Empty;

    public HomePageViewModel()
    {
        FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public static string Title => "Home";

    public static string Description => "Welcome to Zee Mad Archivist.";

    public string FolderPath
    {
        get => _folderPath;
        set => SetField(ref _folderPath, value);
    }
}
