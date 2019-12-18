using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageViewerDemo
{
    /// <summary>
    /// ImageViewer.xaml 的交互逻辑
    /// </summary>
    public partial class ImageViewer : UserControl
    {
        public delegate void ScaleChangedHandler(double ratio);

        public event ScaleChangedHandler ScaleChanged;
        private static readonly CircleEase CircleEaseOut = new CircleEase { EasingMode = EasingMode.EaseOut };

        private double _previousScaleIndex = 0;
        private double _previewScaleRatio = 0;
        private HashSet<Rectangle> _boundSbList = new HashSet<Rectangle>();

        private double ImgRatio => SourceSizePrivate.Width / SourceSizePrivate.Height;
        private double CanvasRatio => ActualWidth / ActualHeight;
        private double FullScaleRatio { get; set; }
        private bool AutoFitLargeSize { get; } = true;


        public ImageViewer()
        {
            InitializeComponent();
        }

        public Size SourceSizePrivate { get; private set; }
        public double MinScale { get; set; } = 1;
        public double MaxScale { get; set; } = 5;
        public double ScaleCount { get; set; } = 12;
        public TimeSpan AnimationTime { get; set; } = TimeSpan.Zero;

        public double CurrentScale
        {
            get => _currentScale;
            set
            {
                if (Math.Abs(_currentScale - value) <= 0.01)
                    return;
                ScaleChanged?.Invoke(value);
                SetScale(value, true);
            }
        }

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

            SourceSizePrivate = new Size(bitmap.Width, bitmap.Height);
            FixFullRatio();
            ResetPosition();
            Image.Source = bitmap;
        }

        private double GetNextScaleRatio(bool add)
        {
            var quadIn = new Func<double, double>(x => x * x);
            var scaleIndex = add ? _previousScaleIndex + 1 : _previousScaleIndex - 1;
            if (scaleIndex > ScaleCount) scaleIndex = ScaleCount;
            else if (scaleIndex < 0) scaleIndex = 0;
            _previousScaleIndex = scaleIndex;
            var trueMin = Math.Min(1 / FullScaleRatio, MinScale);
            var scaleRatio = trueMin + (MaxScale - trueMin) * quadIn(scaleIndex / ScaleCount);
            Console.WriteLine(scaleRatio);
            return scaleRatio;
        }

        private void MainCanvas_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                _previewScaleRatio = GetNextScaleRatio(true);
                if (_previewScaleRatio > MaxScale)
                {
                    SetScale(MaxScale, true);
                    e.Handled = true;
                    return;
                }

                //Scale.ScaleY += delta;
            }
            else
            {
                _previewScaleRatio = GetNextScaleRatio(false);
                var finalScale = _previewScaleRatio;
                if (FullScaleRatio <= 1 && finalScale < MinScale)
                {
                    ResetPosition(true);
                    e.Handled = true;
                }
                else if (FullScaleRatio > 1 && finalScale < MinScale / FullScaleRatio)
                {
                    ResetPosition(true);
                    e.Handled = true;
                }
            }
        }

        private void MainCanvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            Point imageRelBefore = Mouse.GetPosition(HideBorder);
            //Console.WriteLine($"Before: {nameof(imageRelBefore)}: {imageRelBefore}");

            SetScale(_previewScaleRatio, true);

            Point imageRelAfter = Mouse.GetPosition(HideBorder);
            //Console.WriteLine($"After: {nameof(imageRelAfter)}: {imageRelAfter}");
            SetTranslate(TranslateBorder.X + imageRelAfter.X - imageRelBefore.X,
                TranslateBorder.Y + imageRelAfter.Y - imageRelBefore.Y, true);
        }

        private Point _prevImageRel = new Point(double.MinValue, double.MinValue);
        private double _currentScale;
        private bool _mouseDown;

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Canvas canvas))
            {
                return;
            }

            _prevImageRel = Mouse.GetPosition(Image);
            canvas.CaptureMouse();
            canvas.Cursor = Cursors.SizeAll;
            _mouseDown = true;
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
            if (nowImageRel == _prevImageRel) return;

            Console.WriteLine(_prevImageRel + ";" + nowImageRel);
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

            _prevImageRel = new Point(double.MinValue, double.MinValue);
            canvas.ReleaseMouseCapture();
            canvas.Cursor = Cursors.Arrow;
            _mouseDown = false;
            foreach (var boundRec in _boundSbList)
            {
                ClearBound(boundRec);
            }
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
                FullScaleRatio = SourceSizePrivate.Width / ActualWidth;
            }
            else if (ImgRatio < CanvasRatio)
            {
                FullScaleRatio = SourceSizePrivate.Height / ActualHeight;
            }
        }

        private void SetScale(double value, bool animation = false)
        {
            if (AnimationTime == TimeSpan.Zero) animation = false;
            ScaleBorder.ScaleX = value;
            ScaleBorder.ScaleY = value;

            var sb = new Storyboard();
            _currentScale = value;
            if (animation)
            {
                var sx = new DoubleAnimation
                {
                    To = value,
                    EasingFunction = CircleEaseOut,
                    //EasingFunction = new CubicEase(){EasingMode = EasingMode.EaseOut},
                    BeginTime = TimeSpan.Zero,
                    Duration = new Duration(AnimationTime)
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
        }

        private void SetTranslate(double x, double y, bool animation = false)
        {
            if (AnimationTime == TimeSpan.Zero) animation = false;
            bool recLeft = false, recRight = false, recUp = false, recBottom = false;
            if (CurrentScale * SourceSizePrivate.Width >= ActualWidth)
            {
                if (x >= 0)
                {
                    x = 0;
                    if (_mouseDown)
                    {
                        recLeft = true;
                        ShowBound(RecLeft);
                    }
                }
                else if ((x + SourceSizePrivate.Width) * CurrentScale <= ActualWidth)
                {
                    x = ActualWidth / CurrentScale - SourceSizePrivate.Width;
                    if (_mouseDown)
                    {
                        recRight = true;
                        ShowBound(RecRight);
                    }
                }
            }
            else
            {
                var display = SourceSizePrivate.Width * CurrentScale;
                x = (ActualWidth - display) / (2 * CurrentScale);
            }

            if (CurrentScale * SourceSizePrivate.Height >= ActualHeight)
            {
                if (y >= 0)
                {
                    y = 0;
                    if (_mouseDown)
                    {
                        recUp = true;
                        ShowBound(RecUp);
                    }
                }
                else if ((y + SourceSizePrivate.Height) * CurrentScale <= ActualHeight)
                {
                    y = ActualHeight / CurrentScale - SourceSizePrivate.Height;
                    if (_mouseDown)
                    {
                        recBottom = true;
                        ShowBound(RecBottom);
                    }
                }
            }
            else
            {
                var display = SourceSizePrivate.Height * CurrentScale;
                y = (ActualHeight - display) / (2 * CurrentScale);
            }

            TranslateBorder.X = x;
            TranslateBorder.Y = y;

            if (Math.Abs(Translate.X - x) >= 0.01 || Math.Abs(Translate.Y - y) >= 0.01)
            {
                if (!recLeft) ClearBound(RecLeft);
                if (!recUp) ClearBound(RecUp);
                if (!recRight) ClearBound(RecRight);
                if (!recBottom) ClearBound(RecBottom);
            }

            if (animation)
            {
                var sb = new Storyboard();
                var mx = new DoubleAnimation
                {
                    To = x,
                    EasingFunction = CircleEaseOut,
                    BeginTime = TimeSpan.Zero,
                    Duration = new Duration(AnimationTime)
                };
                var my = new DoubleAnimation
                {
                    To = y,
                    EasingFunction = CircleEaseOut,
                    BeginTime = TimeSpan.Zero,
                    Duration = new Duration(AnimationTime)
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
        }

        private void ShowBound(Rectangle rec)
        {
            if (rec == null || _boundSbList.Contains(rec))
                return;

            var recTrans = (ScaleTransform)rec.RenderTransform;
            var sb = new Storyboard();
            var mx = new DoubleAnimation
            {
                To = 1,
                EasingFunction = CircleEaseOut,
                BeginTime = TimeSpan.Zero,
                Duration = new Duration(TimeSpan.FromMilliseconds(400))
            };

            string path;
            switch (rec.Name)
            {
                case nameof(RecLeft):
                case nameof(RecRight):
                    path = "X";
                    break;
                case nameof(RecUp):
                case nameof(RecBottom):
                    path = "Y";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Storyboard.SetTargetProperty(mx, new PropertyPath("RenderTransform.Scale" + path));
            Storyboard.SetTarget(mx, rec);
            sb.Children.Add(mx);
            sb.Completed += (obj, arg) =>
            {
                if (path == "X")
                    recTrans.ScaleX = 1;
                else
                    recTrans.ScaleY = 1;

                sb.Stop();
            };
            _boundSbList.Add(rec);
            sb.Begin();
        }

        private void ClearBound(Rectangle rec)
        {
            if (rec == null || !_boundSbList.Contains(rec))
                return;
            var recTrans = (ScaleTransform)rec.RenderTransform;
            var sb = new Storyboard();
            var mx = new DoubleAnimation
            {
                To = 0,
                EasingFunction = CircleEaseOut,
                BeginTime = TimeSpan.Zero,
                Duration = new Duration(TimeSpan.FromMilliseconds(400))
            };

            string path;
            switch (rec.Name)
            {
                case nameof(RecLeft):
                case nameof(RecRight):
                    path = "X";
                    break;
                case nameof(RecUp):
                case nameof(RecBottom):
                    path = "Y";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Storyboard.SetTargetProperty(mx, new PropertyPath("RenderTransform.Scale" + path));
            Storyboard.SetTarget(mx, rec);
            sb.Children.Add(mx);
            sb.Completed += (obj, arg) =>
            {
                if (path == "X")
                    recTrans.ScaleX = 0;
                else
                    recTrans.ScaleY = 0;

                sb.Stop();
                _boundSbList.Remove(rec);
            };
            _boundSbList.Add(rec);
            sb.Begin();
        }
    }
}
