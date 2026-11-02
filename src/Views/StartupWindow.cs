using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace CrosshairMaster.Views
{
    public class StartupWindow : Window
    {
        public event Action OnAnimationCompleted;

        private Border _card;
        private Canvas _iconCanvas;
        private Ellipse _outerRing;
        private Ellipse _centerDot;
        private Line _lineLeft;
        private Line _lineRight;
        private Line _lineTop;
        private Line _lineBottom;
        private TextBlock _txtTitle;
        private TextBlock _txtSub;
        private Border _scanBar;
        private bool _isClosing = false;

        public StartupWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Width = 460;
            Height = 320;

            BuildUI();

            MouseDown += (s, e) => SkipAnimation();
            KeyDown += (s, e) => SkipAnimation();
            Loaded += (s, e) => StartAnimationSequence();
        }

        private void BuildUI()
        {
            Grid host = new Grid();

            _card = new Border
            {
                Width = 420,
                Height = 280,
                Background = new SolidColorBrush(Color.FromArgb(248, 28, 28, 30)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(18),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 36,
                    ShadowDepth = 4,
                    Opacity = 0.55
                }
            };

            Grid inner = new Grid();
            inner.RowDefinitions.Add(new RowDefinition { Height = new GridLength(140) });
            inner.RowDefinitions.Add(new RowDefinition { Height = new GridLength(45) });
            inner.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            inner.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 1. Icon Animation Canvas (⭐ 简约极简准星动画)
            _iconCanvas = new Canvas
            {
                Width = 110,
                Height = 110,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ClipToBounds = false
            };
            Grid.SetRow(_iconCanvas, 0);

            double cx = 55;
            double cy = 55;

            // Outer precision ring
            _outerRing = new Ellipse
            {
                Width = 72,
                Height = 72,
                Stroke = new SolidColorBrush(Color.FromRgb(10, 132, 255)),
                StrokeThickness = 2.0,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(0.3, 0.3),
                Opacity = 0.0
            };
            Canvas.SetLeft(_outerRing, cx - 36);
            Canvas.SetTop(_outerRing, cy - 36);

            // Center red dot
            _centerDot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Color.FromRgb(255, 69, 58)),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(0.0, 0.0),
                Opacity = 0.0
            };
            Canvas.SetLeft(_centerDot, cx - 5);
            Canvas.SetTop(_centerDot, cy - 5);

            SolidColorBrush bladeBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            _lineLeft = new Line { X1 = cx, Y1 = cy, X2 = cx, Y2 = cy, Stroke = bladeBrush, StrokeThickness = 2.5, StrokeStartLineCap = PenLineCap.Flat, StrokeEndLineCap = PenLineCap.Flat, Opacity = 0.0 };
            _lineRight = new Line { X1 = cx, Y1 = cy, X2 = cx, Y2 = cy, Stroke = bladeBrush, StrokeThickness = 2.5, StrokeStartLineCap = PenLineCap.Flat, StrokeEndLineCap = PenLineCap.Flat, Opacity = 0.0 };
            _lineTop = new Line { X1 = cx, Y1 = cy, X2 = cx, Y2 = cy, Stroke = bladeBrush, StrokeThickness = 2.5, StrokeStartLineCap = PenLineCap.Flat, StrokeEndLineCap = PenLineCap.Flat, Opacity = 0.0 };
            _lineBottom = new Line { X1 = cx, Y1 = cy, X2 = cx, Y2 = cy, Stroke = bladeBrush, StrokeThickness = 2.5, StrokeStartLineCap = PenLineCap.Flat, StrokeEndLineCap = PenLineCap.Flat, Opacity = 0.0 };

            _iconCanvas.Children.Add(_outerRing);
            _iconCanvas.Children.Add(_lineLeft);
            _iconCanvas.Children.Add(_lineRight);
            _iconCanvas.Children.Add(_lineTop);
            _iconCanvas.Children.Add(_lineBottom);
            _iconCanvas.Children.Add(_centerDot);

            inner.Children.Add(_iconCanvas);

            // 2. Title Text
            _txtTitle = new TextBlock
            {
                Text = "SCREEN CROSSHAIR  v6.9",
                FontWeight = FontWeights.Bold,
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(10, 132, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.0,
                RenderTransform = new TranslateTransform(0, 12)
            };
            Grid.SetRow(_txtTitle, 1);
            inner.Children.Add(_txtTitle);

            // 3. Subtitle Text "by zhezhi"
            _txtSub = new TextBlock
            {
                Text = "by zhezhi",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 175)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Opacity = 0.0
            };
            Grid.SetRow(_txtSub, 2);
            inner.Children.Add(_txtSub);

            // 4. Subtle Scan line
            _scanBar = new Border
            {
                Width = 0,
                Height = 2.5,
                CornerRadius = new CornerRadius(1.25),
                Background = new SolidColorBrush(Color.FromRgb(10, 132, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 14),
                Opacity = 0.9
            };
            Grid.SetRow(_scanBar, 3);
            inner.Children.Add(_scanBar);

            _card.Child = inner;
            host.Children.Add(_card);
            Content = host;
        }

        private void StartAnimationSequence()
        {
            double cx = 55;
            double cy = 55;
            double gap = 10;
            double len = 34;

            // 1. Center dot pulse
            ScaleTransform dotSt = (ScaleTransform)_centerDot.RenderTransform;
            DoubleAnimation dotScale = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new BackEase { Amplitude = 0.6, EasingMode = EasingMode.EaseOut }
            };
            DoubleAnimation dotFade = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200));
            dotSt.BeginAnimation(ScaleTransform.ScaleXProperty, dotScale);
            dotSt.BeginAnimation(ScaleTransform.ScaleYProperty, dotScale);
            _centerDot.BeginAnimation(UIElement.OpacityProperty, dotFade);

            // 2. Outer ring expand and fade in (start at 0.15s)
            ScaleTransform ringSt = (ScaleTransform)_outerRing.RenderTransform;
            DoubleAnimation ringScale = new DoubleAnimation(0.3, 1.0, TimeSpan.FromMilliseconds(400))
            {
                BeginTime = TimeSpan.FromMilliseconds(150),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };
            DoubleAnimation ringFade = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(300))
            {
                BeginTime = TimeSpan.FromMilliseconds(150)
            };
            ringSt.BeginAnimation(ScaleTransform.ScaleXProperty, ringScale);
            ringSt.BeginAnimation(ScaleTransform.ScaleYProperty, ringScale);
            _outerRing.BeginAnimation(UIElement.OpacityProperty, ringFade);

            // 3. 4 Minimalist Crosshair Ticks slide outwards (start at 0.3s)
            TimeSpan tickDelay = TimeSpan.FromMilliseconds(300);
            TimeSpan tickDur = TimeSpan.FromMilliseconds(350);
            QuarticEase tickEase = new QuarticEase { EasingMode = EasingMode.EaseOut };

            // Left
            _lineLeft.BeginAnimation(Line.X1Property, new DoubleAnimation(cx - gap, cx - len, tickDur) { BeginTime = tickDelay, EasingFunction = tickEase });
            _lineLeft.BeginAnimation(Line.X2Property, new DoubleAnimation(cx, cx - gap, tickDur) { BeginTime = tickDelay, EasingFunction = tickEase });
            _lineLeft.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200)) { BeginTime = tickDelay });

            // Right
            _lineRight.BeginAnimation(Line.X1Property, new DoubleAnimation(cx + gap, cx + len, tickDur) { BeginTime = tickDelay, EasingFunction = tickEase });
            _lineRight.BeginAnimation(Line.X2Property, new DoubleAnimation(cx, cx + gap, tickDur) { BeginTime = tickDelay, EasingFunction = tickEase });
            _lineRight.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200)) { BeginTime = tickDelay });

            // Top
            _lineTop.BeginAnimation(Line.Y1Property, new DoubleAnimation(cy - gap, cy - len, tickDur) { BeginTime = tickDelay, EasingFunction = tickEase });
            _lineTop.BeginAnimation(Line.Y2Property, new DoubleAnimation(cy, cy - gap, tickDur) { BeginTime = tickDelay, EasingFunction = tickEase });
            _lineTop.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200)) { BeginTime = tickDelay });

            // Bottom
            _lineBottom.BeginAnimation(Line.Y1Property, new DoubleAnimation(cy + gap, cy + len, tickDur) { BeginTime = tickDelay, EasingFunction = tickEase });
            _lineBottom.BeginAnimation(Line.Y2Property, new DoubleAnimation(cy, cy + gap, tickDur) { BeginTime = tickDelay, EasingFunction = tickEase });
            _lineBottom.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200)) { BeginTime = tickDelay });

            // 4. Text fade in & slide up (start at 0.45s)
            TranslateTransform tt = (TranslateTransform)_txtTitle.RenderTransform;
            tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(350)) { BeginTime = TimeSpan.FromMilliseconds(450), EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });
            _txtTitle.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(300)) { BeginTime = TimeSpan.FromMilliseconds(450) });

            _txtSub.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(300)) { BeginTime = TimeSpan.FromMilliseconds(600) });

            // 5. Scan bar expansion
            _scanBar.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation(0, 240, TimeSpan.FromMilliseconds(500)) { BeginTime = TimeSpan.FromMilliseconds(400), EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });

            // 6. Complete and dissolve window after 1.1s
            DoubleAnimation exitFade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(280))
            {
                BeginTime = TimeSpan.FromMilliseconds(1150)
            };
            exitFade.Completed += (s, e) => FinishAndClose();
            this.BeginAnimation(UIElement.OpacityProperty, exitFade);
        }

        private void SkipAnimation()
        {
            if (_isClosing) return;
            FinishAndClose();
        }

        private void FinishAndClose()
        {
            if (_isClosing) return;
            _isClosing = true;

            Close();

            if (OnAnimationCompleted != null)
            {
                OnAnimationCompleted();
            }
        }
    }
}
