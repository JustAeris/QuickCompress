using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace QuickCompress.Gui.Tabs.Common;

public partial class FileList
{
    public FileList()
    {
        InitializeComponent();

        FileListView.ItemsSource = new List<FileInfo>();
    }

    public IList<FileInfo> GetFiles() => FileListView.Items.Cast<FileInfo>().ToList();

    public void SetFiles(IEnumerable<FileInfo> files) => FileListView.ItemsSource = files;


    #region File selection and management

    /// <summary>
    /// Adds user-selected files to the list
    /// </summary>
    private void ChooseFilesButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Choose files to compress",
            Filter = "Video files (*.avi;*.mov;*.mp4;*.webm)|*.avi;*.mov;*.mp4;*.webm|All files (*.*)|*.*",
            CheckFileExists = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
        };

        dialog.ShowDialog();

        if (dialog.FileNames.Length > 0)
            FileListView.ItemsSource = dialog.FileNames.Select(s => new FileInfo(s)).Concat(FileListView.ItemsSource.Cast<FileInfo>()).DistinctBy(f => f.FullName);
    }

    private void RemoveFiles_OnClick(object sender, RoutedEventArgs e)
    {
        var list = FileListView.Items.Cast<object>().Where(item => !FileListView.SelectedItems.Contains(item)).ToList();

        FileListView.ItemsSource = list;
    }

    private void DeselectAllButton_OnClick(object sender, RoutedEventArgs e) => FileListView.UnselectAll();

    private void SelectAllButton_OnClick(object sender, RoutedEventArgs e) => FileListView.SelectAll();

    #endregion

}
