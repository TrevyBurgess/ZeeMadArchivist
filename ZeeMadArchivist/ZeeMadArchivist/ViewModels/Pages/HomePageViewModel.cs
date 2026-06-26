using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CyberFeedForward.TheMadArchivist.ViewModels.Pages;

public sealed partial class HomePageViewModel : INotifyPropertyChanged
{
    private string _folderPath = string.Empty;

    public HomePageViewModel()
    {
        FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static string Title => "Home";

    public static string Description => "Welcome to Zee Mad Archivist.";

    public string FolderPath
    {
        get => _folderPath;
        set
        {
            if (string.Equals(_folderPath, value, StringComparison.Ordinal))
            {
                return;
            }

            _folderPath = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
