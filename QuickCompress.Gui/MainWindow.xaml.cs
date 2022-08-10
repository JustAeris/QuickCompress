using System.Reflection;

namespace QuickCompress.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            // Set the version number in the title bar
            Title = $"QuickCompress {Assembly.GetExecutingAssembly().GetName().Version}";
        }
    }
}
