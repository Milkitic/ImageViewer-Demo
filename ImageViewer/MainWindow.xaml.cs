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
            ImageViewer.LoadImage(@"C:\Windows\Web\Screen\img103.png");
            //ImageViewer.LoadImage(@"C:\Users\Milky\Desktop\datav-template.png");
        }
    }
}
