using KTcpServer.Toolkit.ViewModels;

namespace KTcpServer.Toolkit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            this.DataContext = viewModel;
        }
    }
}