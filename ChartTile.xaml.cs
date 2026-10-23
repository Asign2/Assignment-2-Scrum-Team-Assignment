using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Assign_2
{
    public partial class ChartTile : UserControl
    {
        private enum ChartKind { None, Line, Band, Bars }

        public event EventHandler Clicked;

        private ChartKind kind = ChartKind.None;
        private string chartTitle = "Bar Graph";
        private List<string> labels = new List<string>();
        private List<double> values = new List<double>();
        private List<double> minValues = new List<double>();
        private List<double> maxValues = new List<double>();

        public string Title { get; private set; }
        public List<string> Labels { get; private set; }
        public List<double> Values { get; private set; }

        private double _lastWidth = -1;
        private double _lastHeight = -1;

        public ChartTile()
        {
            InitializeComponent();
            SparklineHost.SizeChanged += (s, e) =>
            {
                // Only redraw if the size actually changed significantly to avoid layout thrashing
                if (Math.Abs(SparklineHost.ActualWidth - _lastWidth) > 1 ||
                    Math.Abs(SparklineHost.ActualHeight - _lastHeight) > 1)
                {
                    _lastWidth = SparklineHost.ActualWidth;
                    _lastHeight = SparklineHost.ActualHeight;
                    DrawSparkline();
                }
            };
        }

        public void ShowLine(string title, List<string> xLabels, List<double> data, double? low, double? high)
        {
            chartTitle = title;
            kind = ChartKind.Line;
            labels = xLabels;
            values = data;

            Title = title;
            Labels = labels;
            Values = values;

            minValues.Clear();
            maxValues.Clear();
            Redraw();
        }

        public void ShowBand(string title, List<string> xLabels, List<double> mins, List<double> maxs, List<double> avgs, double? low, double? high)
        {
            chartTitle = title;
            kind = ChartKind.Band;
            labels = xLabels;
            minValues = mins;
            maxValues = maxs;
            values = avgs;

            Title = title;
            Labels = labels;
            Values = values;

            Redraw();
        }

        public void ShowBars(string title, List<string> xLabels, List<double> data)
        {
            chartTitle = title;
            kind = ChartKind.Bars;
            labels = xLabels;
            values = data;

            Title = title;
            Labels = labels;
            Values = values;

            minValues.Clear();
            maxValues.Clear();
            Redraw();
        }

        public void ShowPlaceholder(string title)
        {
            chartTitle = title;
            kind = ChartKind.None;
            values = new List<double>();
            Title = title;
            Labels = new List<string>();
            Values = new List<double>();
            Redraw();
        }

        private void Redraw()
        {
            TitleText.Text = chartTitle;

            if (kind == ChartKind.None || values.Count == 0)
            {
                NoDataText.Visibility = Visibility.Visible;
                MinValueText.Text = "--";
                AvgValueText.Text = "--";
                MaxValueText.Text = "--";
                MinTimeText.Text = "";
                MaxTimeText.Text = "";
                Sparkline.Points = null;
                return;
            }

            NoDataText.Visibility = Visibility.Collapsed;

            bool hasBand = kind == ChartKind.Band && minValues.Count > 0 && maxValues.Count > 0;

            // OPTIMIZATION: Compute Min, Max, and Average in a single efficient pass instead of multiple LINQ scans
            double minTemp = double.MaxValue, maxTemp = double.MinValue, sum = 0;
            int targetCount = values.Count;

            if (hasBand)
            {
                for (int i = 0; i < targetCount; i++)
                {
                    if (minValues[i] < minTemp) minTemp = minValues[i];
                    if (maxValues[i] > maxTemp) maxTemp = maxValues[i];
                    sum += values[i];
                }
            }
            else
            {
                for (int i = 0; i < targetCount; i++)
                {
                    double val = values[i];
                    if (val < minTemp) minTemp = val;
                    if (val > maxTemp) maxTemp = val;
                    sum += val;
                }
            }

            double avgTemp = sum / targetCount;

            int minIndex = hasBand ? minValues.IndexOf(minTemp) : values.IndexOf(minTemp);
            int maxIndex = hasBand ? maxValues.IndexOf(maxTemp) : values.IndexOf(maxTemp);

            MinValueText.Text = minTemp.ToString("0.0") + " °C";
            AvgValueText.Text = avgTemp.ToString("0.00") + " °C";
            MaxValueText.Text = maxTemp.ToString("0.0") + " °C";
            MinTimeText.Text = LabelAt(minIndex);
            MaxTimeText.Text = LabelAt(maxIndex);

            DrawSparkline();
        }

        private string LabelAt(int index)
        {
            return (index >= 0 && index < labels.Count) ? labels[index] : "";
        }

        private void DrawSparkline()
        {
            if (kind == ChartKind.None || values.Count < 2 ||
                SparklineHost.ActualWidth <= 0 || SparklineHost.ActualHeight <= 0)
            {
                Sparkline.Points = null;
                return;
            }

            double width = SparklineHost.ActualWidth;
            double height = SparklineHost.ActualHeight;

            // Single pass min/max for sparkline scaling
            double min = values[0];
            double max = values[0];
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i] < min) min = values[i];
                if (values[i] > max) max = values[i];
            }

            double range = (max - min) < 0.01 ? 1 : (max - min);

            var points = new PointCollection();
            int count = values.Count;

            // OPTIMIZATION: If there are more data points than horizontal pixels, 
            // downsample to avoid processing unnecessary overhead on the UI thread.
            int step = 1;
            if (count > width)
            {
                step = (int)Math.Ceiling((double)count / width);
            }

            for (int i = 0; i < count; i += step)
            {
                double x = width * i / (count - 1);
                double normalized = (values[i] - min) / range;
                double y = height - (normalized * height);
                points.Add(new Point(x, y));
            }

            Sparkline.Points = points;
        }

        public ChartTile CreateEnlargedCopy(double width = 850, double height = 350)
        {
            ChartTile copy = new ChartTile { Width = width, Height = height };
            copy.ShowLine(this.Title, this.Labels, this.Values, null, null);
            return copy;
        }

        private void SparklineHost_MouseMove(object sender, MouseEventArgs e)
        {
            if (values.Count < 2) return;

            Point pos = e.GetPosition(SparklineHost);
            double fraction = pos.X / SparklineHost.ActualWidth;
            int lastIndex = values.Count - 1;
            int i = (int)Math.Round(fraction * lastIndex);

            if (i < 0) i = 0;
            if (i > lastIndex) i = lastIndex;

            TooltipTemperature.Text = $"{values[i]:0.0} °C";
            TooltipTime.Text = LabelAt(i);

            double tipWidth = 70;
            double tipHeight = 40;

            double left = pos.X - tipWidth / 2;
            double maxLeft = SparklineHost.ActualWidth - tipWidth;
            if (left > maxLeft) left = maxLeft;
            if (left < 0) left = 0;

            Canvas.SetLeft(TemperatureTooltip, left);
            Canvas.SetTop(TemperatureTooltip, pos.Y - tipHeight - 10);
            TemperatureTooltip.Visibility = Visibility.Visible;
        }

        private void SparklineHost_MouseLeave(object sender, MouseEventArgs e)
        {
            TemperatureTooltip.Visibility = Visibility.Collapsed;
        }

        private void EnlargeChart(object sender, MouseButtonEventArgs e)
        {
            Clicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
  