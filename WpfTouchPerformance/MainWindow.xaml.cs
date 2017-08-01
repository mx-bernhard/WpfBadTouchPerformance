using System.Windows;
using System.Windows.Input;

namespace WpfTouchPerformance {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            this.PreviewMouseDown += MainWindow_MouseDown;
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e) {
            // uncomment to log out the tree
            //ReverseInheritStats.Log(e.OriginalSource);
        }
    }
}
