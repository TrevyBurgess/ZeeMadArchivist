using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.Utilities;
using CyberFeedForward.TheMadArchivist.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CyberFeedForward.TheMadArchivist
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private TrayIconService? _trayIcon;
        private FirstRunService? _firstRunService;

        public static Window? MainWindowInstance { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        public static bool DialogShowing { get; set; }

        public static void UpdateMessage(string message)
        {
            if (MainWindowInstance is MainWindow mainWindow)
            {
                mainWindow.SetStatusText(message);
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            MainWindowInstance = _window;

            _firstRunService = FirstRunService.Instance;

            if (_window.Content is FrameworkElement rootElement)
            {
                var themeSettings = new ThemeSettingsService(LocalAppSettingsStore.Instance);
                AppThemeManager.ApplyThemeMode(rootElement, themeSettings.GetThemeMode());
            }

            _window.Activate();

            _ = RunFirstRunExperienceAsync();

            _trayIcon = new TrayIconService();
            _trayIcon.Initialize();
        }

        private async Task RunFirstRunExperienceAsync()
        {
            if (_firstRunService is null || _window?.Content is not FrameworkElement rootElement)
            {
                return;
            }

            try
            {
                if (!_firstRunService.ShouldRunFirstRunExperience())
                {
                    return;
                }

                var xamlRoot = await GetXamlRootAsync(rootElement);
                if (xamlRoot is null)
                {
                    return;
                }

                var dialog = new FirstRunCustomizationDialog
                {
                    XamlRoot = xamlRoot,
                };

                DialogShowing = true;

                await dialog.ShowAsync();

                if (dialog.ViewModel.RegisterTagsPropertyPage && !string.IsNullOrWhiteSpace(dialog.ViewModel.TagsRegistrationErrorMessage))
                {
                    var resultDialog = new ContentDialog
                    {
                        Title = "Tags Property Page",
                        Content = dialog.ViewModel.TagsRegistrationErrorMessage,
                        CloseButtonText = "OK",
                        XamlRoot = xamlRoot,
                    };

                    await resultDialog.ShowAsync();
                }

                _firstRunService.MarkFirstRunExperienceCompleted();

                (MainWindowInstance as MainWindow)?.ReloadArchives();

                DialogShowing = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
            }
        }

        private static async Task<XamlRoot?> GetXamlRootAsync(FrameworkElement rootElement)
        {
            if (rootElement.XamlRoot is not null)
            {
                return rootElement.XamlRoot;
            }

            var completionSource = new System.Threading.Tasks.TaskCompletionSource<XamlRoot?>();

            void RootElement_Loaded(object sender, RoutedEventArgs e)
            {
                rootElement.Loaded -= RootElement_Loaded;
                completionSource.TrySetResult(rootElement.XamlRoot);
            }

            rootElement.Loaded += RootElement_Loaded;
            await completionSource.Task;
            return rootElement.XamlRoot;
        }
    }
}
