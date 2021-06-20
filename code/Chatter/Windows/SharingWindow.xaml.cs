using Chatter.Internal;
using Chatter.Interop;
using Mikodev.Links.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Chatter.Windows
{
    public abstract partial class SharingWindow : Window
    {
        private readonly ISharingObject sharingObject;

        private readonly SharingViewer viewer;

        private IList<(double progress, double speed)> values;

        private double maximumSpeed = 0.0;

        public SharingWindow(ISharingObject sharingObject)
        {
            this.InitializeComponent();

            var owner = Application.Current.MainWindow;
            Debug.Assert(owner is Entrance);
            this.Owner = owner;

            var receiver = sharingObject is ISharingReceiver;
            this.sharingObject = sharingObject ?? throw new ArgumentNullException(nameof(sharingObject));
            this.viewer = sharingObject.Viewer;
            if (receiver == false)
                this.acceptButton.Visibility = Visibility.Collapsed;
            this.sourceTextBlock.Text = receiver ? "Sender" : "Receiver";
            this.Title = receiver ? "Receiver" : "Sender";
            this.DataContext = this.viewer;

            var handler = new PropertyChangedEventHandler((s, e) => this.OnUpdate(e.PropertyName));
            if (receiver)
                Loaded += (s, _) => NativeMethods.FlashWindow(new WindowInteropHelper((Window)s).Handle, true);
            Closed += (s, _) => (sharingObject as IDisposable)?.Dispose();
            Loaded += (s, _) => this.OnUpdate(string.Empty);
            Loaded += (s, _) => ((INotifyPropertyChanged)this.viewer).PropertyChanged += handler;
            Unloaded += (s, _) => ((INotifyPropertyChanged)this.viewer).PropertyChanged -= handler;
            SizeChanged += (s, _) => this.UpdateGraphics(this.values);
        }

        private void OnButtonClick(object _, RoutedEventArgs e)
        {
            var button = e.OriginalSource as Button; ;
            var receiver = this.sharingObject as ISharingReceiver;
            if (button == this.acceptButton)
            {
                Debug.Assert(receiver != null);
                receiver.Accept(true);
                button.IsEnabled = false;
            }
            else if (button == this.cancelButton)
            {
                if (receiver != null && this.viewer.Status == SharingStatus.Pending)
                    receiver.Accept(false);
                else
                    (this.sharingObject as IDisposable)?.Dispose();
            }
            else if (button == this.backupButton)
            {
                this.Close();
            }
        }

        protected virtual void OnUpdate(string propertyName)
        {
            if (propertyName != nameof(SharingViewer.Status) || (this.viewer.Status & SharingStatus.Completed) == 0)
                return;
            // Change visibility when completed
            this.buttonPanel.IsEnabled = false;
            this.buttonPanel.Visibility = Visibility.Collapsed;
            this.backupPanel.Visibility = Visibility.Visible;
        }

        protected void UpdateNotice(string text)
        {
            this.noticeTextBlock.Text = $"Status: {text}";
        }

        protected void UpdateGraphics(IList<(double progress, double speed)> values)
        {
            this.canvas.ClearVisuals();
            this.values = values;
            if (values == null || values.Count == 0)
                return;
            var window = this.canvas.FindAncestor<Window>();
            var point = this.canvas.TranslatePoint(new Point(0, 0), window);
            // Align to pixels
            var offset = new Vector(point.X - Math.Truncate(point.X), point.Y - Math.Truncate(point.Y));
            var width = this.canvas.ActualWidth;
            var height = this.canvas.ActualHeight;
            var visual = new DrawingVisual();
            var context = visual.RenderOpen();

            var lastValue = values.Last();
            if (lastValue.speed > this.maximumSpeed)
                this.maximumSpeed = lastValue.speed;
            var maximum = 1.25 * this.maximumSpeed;

            var points = new List<Point>();
            foreach (var (progress, speed) in values)
            {
                var y = (1.0 - speed / maximum) * height;
                var x = progress * width;
                if (progress != 0 && points.Count == 0)
                    points.Add(new Point(0, y));
                points.Add(new Point(x, y));
            }
            var lastPoint = points.Last();
            var streamGeometry = new StreamGeometry();
            using (var geometryContext = streamGeometry.Open())
            {
                geometryContext.BeginFigure(new Point(0, height), true, true);
                points.Add(new Point(lastPoint.X, height));
                geometryContext.PolyLineTo(points, true, true);
            }
            var status = this.viewer.Status;
            var color = status == SharingStatus.Success
                ? Color.FromArgb(192, 32, 192, 0)
                : status == SharingStatus.Aborted ? Color.FromArgb(192, 220, 20, 60) : Color.FromArgb(192, 58, 110, 165);
            context.DrawGeometry(new SolidColorBrush(color), null, streamGeometry);

            var vertical = (int)(lastPoint.Y + 0.5) + 0.5 - offset.Y;
            context.DrawLine(new Pen(Brushes.Black, 1), new Point(0, vertical), new Point(width, vertical));

            var text = $"{Extensions.ToUnit((long)values.Last().speed)}/s";
            var typeface = new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch);
            var fontSize = this.noticeTextBlock.FontSize;
            var formatted = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, fontSize, this.Foreground, 96.0);
            var textPoint = new Point(width - formatted.Width - this.canvasBorder.BorderThickness.Right, vertical > formatted.Height ? vertical - formatted.Height : vertical);
            context.DrawText(formatted, textPoint);

            context.Close();
            this.canvas.AddVisual(visual);
        }
    }
}
