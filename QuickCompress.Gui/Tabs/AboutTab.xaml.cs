using System.Reflection;

namespace QuickCompress.Gui.Tabs;

public partial class AboutTab
{
    public AboutTab()
    {
        InitializeComponent();

        VersionLabel.Content += Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    }
}
