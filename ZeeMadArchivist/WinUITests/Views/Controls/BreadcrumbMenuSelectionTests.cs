using CyberFeedForward.TheMadArchivist.Views.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnitTests.Views.Controls;

[TestClass]
public sealed class BreadcrumbMenuSelectionTests
{
    [TestMethod]
    public void ClickingMenuItem_RaisesFolderPathSelected_WithCombinedPath()
    {
        WinUiTestHelper.Run(() =>
        {
            var control = new Breadcrumb
            {
                FolderPath = "C:\\Root",
                Items = ["Child"],
            };

            string? selected = null;
            control.FolderPathSelected += (_, path) => selected = path;

            var breadcrumbButton = (Button)control.FindName("BreadcrumbArrowButton");

            var clickMethod = typeof(Breadcrumb).GetMethod(
                "BreadcrumbButton_OnClick",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(clickMethod);

            clickMethod.Invoke(control, [breadcrumbButton, new RoutedEventArgs()]);

            var flyout = breadcrumbButton.Flyout as MenuFlyout;
            Assert.IsNotNull(flyout);

            var menuItem = flyout.Items.OfType<MenuFlyoutItem>().Single();

            var menuClickMethod = typeof(Breadcrumb).GetMethod(
                "MenuItem_OnClick",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(menuClickMethod);

            menuClickMethod.Invoke(control, [menuItem, new RoutedEventArgs()]);

            Assert.AreEqual("C:\\Root\\Child", selected);
        });
    }
}
