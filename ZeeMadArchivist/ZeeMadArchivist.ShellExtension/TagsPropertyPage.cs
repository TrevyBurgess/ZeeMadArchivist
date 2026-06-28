using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CyberFeedForward.TheMadArchivist.ShellExtension;

/// <summary>
/// WinForms control shown in the "Tags" property sheet tab.
/// </summary>
public partial class TagsPropertyPage : UserControl
{
    public TagsPropertyPage(IReadOnlyList<string> filePaths)
    {
        InitializeComponent();
        FilePaths = filePaths;

        if (filePaths.Count > 0)
        {
            FilesListBox.Items.AddRange([.. filePaths]);
        }
        else
        {
            FilesListBox.Items.Add("No selection information available.");
        }
    }

    public IReadOnlyList<string> FilePaths { get; }

    /// <summary>
    /// Unmanaged pointer to the dialog template. Filled by <see cref="TagsPropertySheet"/>.
    /// </summary>
    internal IntPtr DialogTemplatePointer { get; set; }

    /// <summary>
    /// Unmanaged pointer to the tab title. Filled by <see cref="TagsPropertySheet"/>.
    /// </summary>
    internal IntPtr TitlePointer { get; set; }

    private void InitializeComponent()
    {
        SuspendLayout();

        var headerLabel = new Label
        {
            Dock = DockStyle.Top,
            Text = "Tags for selected files and folders:",
            Padding = new Padding(4),
            Height = 24,
        };

        FilesListBox = new ListBox
        {
            Dock = DockStyle.Top,
            Height = 120,
            IntegralHeight = false,
        };

        var tagsLabel = new Label
        {
            Dock = DockStyle.Top,
            Text = "Tags (comma-separated):",
            Padding = new Padding(4),
            Height = 24,
        };

        TagsTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
        };

        Controls.Add(TagsTextBox);
        Controls.Add(tagsLabel);
        Controls.Add(FilesListBox);
        Controls.Add(headerLabel);
        Name = "TagsPropertyPage";
        Size = new Size(400, 300);
        ResumeLayout(false);
        PerformLayout();
    }

    public ListBox FilesListBox { get; private set; } = null!;

    public TextBox TagsTextBox { get; private set; } = null!;
}
