using CyberFeedForward.TheMadArchivist.Models;
using CyberFeedForward.TheMadArchivist.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CyberFeedForward.TheMadArchivist.Views.Controls;

public sealed partial class FolderTreeViewControl : UserControl
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IFileSystemService _fileSystemService;
    private readonly FileSystemTreeProvider _treeProvider;
    private CancellationTokenSource? _loadCts;

    public FolderTreeViewControl()
    {
        InitializeComponent();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _fileSystemService = new FileSystemService();
        _treeProvider = new FileSystemTreeProvider(_fileSystemService);

        Unloaded += OnUnloaded;
    }

    public ObservableCollection<FileSystemTreeNode> RootNodes { get; } = [];

    public string? FolderPath
    {
        get => (string?)GetValue(FolderPathProperty);
        set => SetValue(FolderPathProperty, value);
    }

    public static readonly DependencyProperty FolderPathProperty =
        DependencyProperty.Register(
            nameof(FolderPath),
            typeof(string),
            typeof(FolderTreeViewControl),
            new PropertyMetadata(null, OnFolderPathChanged));

    private static void OnFolderPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (FolderTreeViewControl)d;
        control.LoadRootAsync((string?)e.NewValue);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
    }

    private void LoadRootAsync(string? folderPath)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        _ = Task.Run(() =>
        {
            var nodes = _treeProvider.CreateRoot(folderPath ?? string.Empty);

            if (ct.IsCancellationRequested)
            {
                return;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                RootNodes.Clear();
                foreach (var node in nodes)
                {
                    RootNodes.Add(node);
                }
            });
        }, ct);
    }

    private void FolderTreeView_OnExpanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (args.Item is not FileSystemTreeNode node)
        {
            return;
        }

        _ = Task.Run(() =>
        {
            _treeProvider.LoadChildren(node);
        });
    }
}
