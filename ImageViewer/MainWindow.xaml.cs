using System.Windows;

namespace ImageViewerDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ImageViewer.LoadImage(@"C:\Users\milkitic\Desktop\gocqlog.png");
            //ImageViewer.LoadImage(@"C:\Users\milki\Desktop\59c0dadd33e62_610.jpg");
            //ImageViewer.LoadImage(@"C:\Users\Milky\Desktop\datav-template.png");
        }
    }
}
