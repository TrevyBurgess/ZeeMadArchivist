using CyberFeedForward.TheMadArchivist.Services;
using CyberFeedForward.TheMadArchivist.ViewModels.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;

namespace CyberFeedForward.TheMadArchivist.Views.Controls;

public sealed partial class GeneralSettingsControl : UserControl
{
    public GeneralSettingsControl()
    {
        InitializeComponent();
    }

    private async void SetStartupToggleSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is not SettingsPageViewModel vm)
            {
                return;
            }

            if (sender is not ToggleSwitch toggle)
            {
                return;
            }

            var requested = toggle.IsOn;

            if (vm.TrySetStartupEnabled(requested, out var errorMessage))
            {
                return;
            }

            toggle.Toggled -= SetStartupToggleSwitch_OnToggled;
            toggle.IsOn = vm.SetStartup;
            toggle.Toggled += SetStartupToggleSwitch_OnToggled;

            var dialog = new ContentDialog
            {
                Title = "Startup Setting Failed",
                Content = string.IsNullOrWhiteSpace(errorMessage) ? "Unable to update startup setting." : errorMessage,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot,
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            throw;
        }
    }

    private async void ShowFirstRunCustomizationButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var service = FirstRunService.Instance;
            service.ResetFirstRunExperience();

            var dialog = new ContentDialog
            {
                Title = "Customization Dialog Enabled",
                Content = "The customization dialog will open the next time the app starts.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot,
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            throw;
        }
    }
}
