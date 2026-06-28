using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.Utilities;
using System;

namespace CyberFeedForward.TheMadArchivist.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly CommandBarSettingsService _commandBarSettingsService;
    private string _statusText;
    private bool _isCommandBarOnLeft;

    public MainWindowViewModel()
        : this(
            new CommandBarSettingsService(LocalAppSettingsStore.Instance),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
    {
    }

    public MainWindowViewModel(CommandBarSettingsService commandBarSettingsService, string defaultFolderPath)
    {
        _commandBarSettingsService = commandBarSettingsService;
        _statusText = "Ready";
        _isCommandBarOnLeft = _commandBarSettingsService.IsCommandBarOnLeft();

         DefaultFolderPath = defaultFolderPath;
   }

    private string DefaultFolderPath { get; }

    public string StatusText
    {
        get => _statusText;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? "Ready" : value;
            SetField(ref _statusText, next);
        }
    }

    public bool IsCommandBarOnLeft
    {
        get => _isCommandBarOnLeft;
        set
        {
            if (!SetField(ref _isCommandBarOnLeft, value)) return;
            _commandBarSettingsService.SetCommandBarOnLeft(value);
        }
    }
}
