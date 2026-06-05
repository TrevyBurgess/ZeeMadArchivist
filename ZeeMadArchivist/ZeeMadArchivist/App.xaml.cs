using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CyberFeedForward.TheMadArchivist.Utilities;
using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.Views.Dialogs;

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

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            MainWindowInstance = _window;

            _trayIcon = new TrayIconService();
            _trayIcon.Initialize();
            _firstRunService = new FirstRunService(new LocalAppSettingsStore());

            if (_window.Content is FrameworkElement rootElement)
            {
                var themeSettings = new ThemeSettingsService(new LocalAppSettingsStore());
                AppThemeManager.ApplyThemeMode(rootElement, themeSettings.GetThemeMode());
            }

            _window.Activate();

            _ = RunFirstRunExperienceAsync();
        }

        private async System.Threading.Tasks.Task RunFirstRunExperienceAsync()
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

                await dialog.ShowAsync();
                _firstRunService.MarkFirstRunExperienceCompleted();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
            }
        }

        private static async System.Threading.Tasks.Task<XamlRoot?> GetXamlRootAsync(FrameworkElement rootElement)
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
