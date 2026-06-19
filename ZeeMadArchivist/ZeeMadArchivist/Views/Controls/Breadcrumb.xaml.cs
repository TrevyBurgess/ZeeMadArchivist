using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CyberFeedForward.TheMadArchivist.Views.Controls;

public sealed partial class Breadcrumb : UserControl
{
    public Breadcrumb()
    {
        InitializeComponent();
    }

    public event EventHandler<string?>? FolderPathSelected;

    public string? FolderPath
    {
        get => (string?)GetValue(FolderPathProperty);
        set => SetValue(FolderPathProperty, value);
    }

    public static readonly DependencyProperty FolderPathProperty =
        DependencyProperty.Register(
            nameof(FolderPath),
            typeof(string),
            typeof(Breadcrumb),
            new PropertyMetadata(null, OnFolderPathChanged));

    public IEnumerable<string>? Items
    {
        get => (IEnumerable<string>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(IEnumerable<string>),
            typeof(Breadcrumb),
            new PropertyMetadata(null));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(Breadcrumb),
            new PropertyMetadata(string.Empty));

    private static void OnFolderPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (Breadcrumb)d;
        control.Text = GetFolderNameFromPath((string?)e.NewValue);
    }

    private static string GetFolderNameFromPath(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return string.Empty;
        }

        var normalized = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var name = Path.GetFileName(normalized);
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var root = Path.GetPathRoot(normalized);
        return root?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? normalized;
    }

    private void BreadcrumbButton_OnClick(object sender, RoutedEventArgs e)
    {
        var items = Items?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (items is null || items.Count == 0)
        {
            return;
        }

        var flyout = new MenuFlyout();
        foreach (var item in items)
        {
            var menuItem = new MenuFlyoutItem
            {
                Text = item,
                Tag = BuildSelectedFolderPath(FolderPath, item)
            };
            menuItem.Click += MenuItem_OnClick;
            flyout.Items.Add(menuItem);
        }

        BreadcrumbArrowButton.Flyout = flyout;
        flyout.ShowAt(BreadcrumbArrowButton);
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem item)
        {
            return;
        }

        FolderPathSelected?.Invoke(this, item.Tag as string);
    }

    private static string? BuildSelectedFolderPath(string? parentFolderPath, string childFolderName)
    {
        if (string.IsNullOrWhiteSpace(parentFolderPath) || string.IsNullOrWhiteSpace(childFolderName))
        {
            return null;
        }

        return Path.Combine(parentFolderPath, childFolderName);
    }

    private void BreadcrumbTextButton_OnClick(object sender, RoutedEventArgs e)
    {
        FolderPathSelected?.Invoke(this, FolderPath);
    }
}
