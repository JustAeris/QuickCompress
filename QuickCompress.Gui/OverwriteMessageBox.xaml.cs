using System.Windows;
using SharpVectors.Converters;

namespace QuickCompress.Gui;

public partial class OverwriteMessageBox
{
    public OverwriteMessageBoxResult Result { get; private set; } = OverwriteMessageBoxResult.Cancel;

    public OverwriteMessageBox(string title, string message)
    {
        InitializeComponent();
        Title = title;
        MessageTextBlock.Text = message;
        IconSvgViewbox.SetResourceReference(SvgViewbox.SourceProperty, "AlertTriangleIcon");
    }

    private void ButtonCancel_OnClick(object sender, RoutedEventArgs e) => Close();


    private void ButtonNo_OnClick(object sender, RoutedEventArgs e)
    {
        Result = OverwriteMessageBoxResult.No;
        Close();
    }

    private void ButtonYes_OnClick(object sender, RoutedEventArgs e)
    {
        Result = OverwriteMessageBoxResult.Yes;
        Close();
    }

    private void ButtonYesToAll_OnClick(object sender, RoutedEventArgs e)
    {
        Result = OverwriteMessageBoxResult.YesToAll;
        Close();
    }

    private void ButtonNoToAll_OnClick(object sender, RoutedEventArgs e)
    {
        Result = OverwriteMessageBoxResult.NoToAll;
        Close();
    }

    private void ButtonRename_OnClick(object sender, RoutedEventArgs e)
    {
        Result = OverwriteMessageBoxResult.Rename;
        Close();
    }

    private void ButtonRenameAll_OnClick(object sender, RoutedEventArgs e)
    {
        Result = OverwriteMessageBoxResult.RenameAll;
        Close();
    }
}

public enum OverwriteMessageBoxResult
{
    Yes,
    No,
    YesToAll,
    NoToAll,
    Rename,
    RenameAll,
    Cancel
}
