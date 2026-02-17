using System.Windows;


namespace OrderApprovalSystem.Views
{

    public partial class Main : BaseCustomWindow
    {
        public Main()
        {
            InitializeComponent();
            Loaded += Main_Loaded;
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
        }
    }

}