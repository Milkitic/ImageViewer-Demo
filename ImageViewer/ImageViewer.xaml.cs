using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ImageViewerDemo
{
    /// <summary>
    /// ImageViewer.xaml 的交互逻辑
    /// </summary>
    public partial class ImageViewer : UserControl
    {
        private static readonly CircleEase CircleEaseOut = new CircleEase { EasingMode = EasingMode.EaseOut };

        public double CurrentScale
        {
            get => _currentScale;
            set => SetScale(value, true);
        }

        private double ImgRatio => SourceSize.Width / SourceSize.Height;
        private double CanvasRatio => ActualWidth / ActualHeight;

        public ImageViewer()
        {
            InitializeComponent();
        }

        public Size SourceSize { get; private set; }
        public bool AutoFitLargeSize { get; set; } = true;
        private double FullScaleRatio { get; set; }

        public void LoadImage(string path)
        {
            var bitmap = new BitmapImage();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                //bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();
            }

            SourceSize = new Size(bitmap.Width, bitmap.Height);
            FixFullRatio();
            ResetPosition();
            Image.Source = bitmap;
        }

        private double prevStep = 0;
        private double GetNextScaleRatio(bool add)
        {
            var quadIn = new Func<double, double>(x => x * x);
            var min = 1;
            var max = 3;
            var total = 10;
            var step = 1d / total;
            var currentStep = add ? prevStep - step : prevStep + step;
            if (currentStep > 1) currentStep = 1;
            else if (currentStep < 0) currentStep = 0;
            var sb = min + (max - min) * currentStep;
            return sb;
            //var current = CurrentScale;
        }

        private void MainCanvas_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (GetNextScaleRatio(true) > 3)
                {
                    SetScale(3, true);
                    e.Handled = true;
                    return;
                }

                //Scale.ScaleY += delta;
            }
            else
            {
                var finalScale = GetNextScaleRatio(false);
                if (FullScaleRatio <= 1 && finalScale < 1)
                {
                    //SetScale(1);
                    ResetPosition(true);
                    e.Handled = true;
                }
                else if (FullScaleRatio > 1 && finalScale < 1 / FullScaleRatio)
                {
                    //SetScale(1 / FullScaleRatio);
                    ResetPosition(true);
                    e.Handled = true;
                }
            }
        }

        private void MainCanvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            Point imageRelBefore = Mouse.GetPosition(Image);
            //Console.WriteLine($"Before: {nameof(imageRelBefore)}: {imageRelBefore}");

            double result;
            if (e.Delta > 0)
            {
                result = GetNextScaleRatio(true);
                SetScale(result, true);
            }
            else
            {
                result = GetNextScaleRatio(false);
                SetScale(result, true);
            }

            Point imageRelAfter = Mouse.GetPosition(HideBorder);
            Console.WriteLine($"After: {nameof(imageRelAfter)}: {imageRelAfter}");
            SetTranslate(Translate.X + imageRelAfter.X - imageRelBefore.X,
                Translate.Y + imageRelAfter.Y - imageRelBefore.Y, true);
        }

        private Point _prevImageRel = new Point(double.MinValue, double.MinValue);
        private double _currentScale;

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Canvas canvas))
            {
                return;
            }

            _prevImageRel = new Point(double.MinValue, double.MinValue);
            canvas.CaptureMouse();
            canvas.Cursor = Cursors.SizeAll;
        }


        private void MainCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            //_prevImageRel = Mouse.GetPosition(Image);
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !(sender is Canvas canvas))
            {
                return;
            }

            var nowImageRel = Mouse.GetPosition(Image);
            if (_prevImageRel == new Point(double.MinValue, double.MinValue))
            {
                _prevImageRel = nowImageRel;
            }

            SetTranslate(Translate.X + nowImageRel.X - _prevImageRel.X,
                Translate.Y + nowImageRel.Y - _prevImageRel.Y);
            _prevImageRel = Mouse.GetPosition(Image);
        }

        private void MainCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Canvas canvas))
            {
                return;
            }

            canvas.ReleaseMouseCapture();
            canvas.Cursor = Cursors.Arrow;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FixFullRatio();
            ResetPosition();
        }

        private void ResetPosition(bool animation = false)
        {
            if (AutoFitLargeSize)
            {
                if (FullScaleRatio < 1)
                {
                    SetScale(1, animation);
                }
                else
                {
                    SetScale(1 / FullScaleRatio, animation);
                }
            }
            else
            {
                SetScale(1, animation);
            }

            SetTranslate(0, 0, animation);
        }

        private void FixFullRatio()
        {
            if (ImgRatio >= CanvasRatio)
            {
                FullScaleRatio = SourceSize.Width / ActualWidth;
            }
            else if (ImgRatio < CanvasRatio)
            {
                FullScaleRatio = SourceSize.Height / ActualHeight;
            }
        }

        private void SetScale(double value, bool animation = false)
        {
            var sb = new Storyboard();
            _currentScale = value;
            if (animation)
            {
                var sx = new DoubleAnimation
                {
                    To = value,
                    EasingFunction = CircleEaseOut,
                    BeginTime = TimeSpan.Zero,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300))
                };
                var sy = sx.Clone();

                Storyboard.SetTargetProperty(sx, new PropertyPath("RenderTransform.Children[1].ScaleX"));
                Storyboard.SetTargetProperty(sy, new PropertyPath("RenderTransform.Children[1].ScaleY"));
                Storyboard.SetTarget(sx, Image);
                Storyboard.SetTarget(sy, Image);
                sb.Children.Add(sx);
                sb.Children.Add(sy);
                sb.Completed += (obj, arg) =>
                {
                    sb.Stop();
                    Scale.ScaleX = value;
                    Scale.ScaleY = value;
                };
                sb.Begin();
            }
            else
            {
                Scale.ScaleX = value;
                Scale.ScaleY = value;
            }

            ScaleBorder.ScaleX = value;
            ScaleBorder.ScaleY = value;
        }

        private void SetTranslate(double x, double y, bool animation = false)
        {
            if (CurrentScale * SourceSize.Width >= ActualWidth)
            {
                if (x > 0) x = 0;
                else if ((x + SourceSize.Width) * CurrentScale < ActualWidth)
                {
                    x = ActualWidth / CurrentScale - SourceSize.Width;
                }
            }
            else
            {
                var display = SourceSize.Width * CurrentScale;
                x = (ActualWidth - display) / (2 * CurrentScale);
            }

            if (CurrentScale * SourceSize.Height >= ActualHeight)
            {
                if (y > 0) y = 0;
                else if ((y + SourceSize.Height) * CurrentScale < ActualHeight)
                {
                    y = ActualHeight / CurrentScale - SourceSize.Height;
                }
            }
            else
            {
                var display = SourceSize.Height * CurrentScale;
                y = (ActualHeight - display) / (2 * CurrentScale);
            }

            if (animation)
            {
                var sb = new Storyboard();
                var ts = TimeSpan.FromMilliseconds(300);
                var mx = new DoubleAnimation
                {
                    To = x,
                    EasingFunction = CircleEaseOut,
                    BeginTime = TimeSpan.Zero,
                    Duration = new Duration(ts)
                };
                var my = new DoubleAnimation
                {
                    To = y,
                    EasingFunction = CircleEaseOut,
                    BeginTime = TimeSpan.Zero,
                    Duration = new Duration(ts)
                };
                Storyboard.SetTargetProperty(mx, new PropertyPath("RenderTransform.Children[0].X"));
                Storyboard.SetTargetProperty(my, new PropertyPath("RenderTransform.Children[0].Y"));
                Storyboard.SetTarget(mx, Image);
                Storyboard.SetTarget(my, Image);
                sb.Children.Add(mx);
                sb.Children.Add(my);
                sb.Completed += (obj, arg) =>
                {
                    sb.Stop();
                    Translate.X = x;
                    Translate.Y = y;
                };
                sb.Begin();
            }
            else
            {
                Translate.X = x;
                Translate.Y = y;
            }

            TranslateBorder.X = x;
            TranslateBorder.Y = y;
        }
    }
}
