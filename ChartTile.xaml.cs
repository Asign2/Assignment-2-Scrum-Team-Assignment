using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Assign_2
{
    /// <summary>
    /// A stat-card tile styled after a live temperature dashboard: current
    /// value, a recent-history sparkline, and a Min/Avg/Max footer — drawn
    /// entirely with native WPF vector shapes, so it scales crisply at any
    /// size with no rasterisation step.
    ///
    /// Keeps the same public methods as the old VectSharp version
    /// (ShowLine/ShowBand/ShowBars/ShowPlaceholder) so callers in
    /// MainWindow.xaml.cs don't need to change what they pass in.
    /// </summary>
    public partial class ChartTile : UserControl
    {
        private enum ChartKind { None, Line, Band, Bars}

        public event EventHandler Clicked;

        private ChartKind kind = ChartKind.None;
        private string chartTitle = "";
        private List<string> labels = new List<string>();
        private List<double> values = new List<double>();
        private List<double> minValues = new List<double>();
        private List<double> maxValues = new List<double>();

        public ChartTile()
        {
            InitializeComponent();
            SparklineHost.SizeChanged += (s, e) => DrawSparkline();
        }

        public void ShowLine(string title, List<string> xLabels,
                             List<double> data, double? low, double? high)
        {
            chartTitle = title;
            kind = ChartKind.Line;
            labels = xLabels;
            values = data;
            minValues = new List<double>();
            maxValues = new List<double>();
            Redraw();
        }

        public void ShowBand(string title, List<string> xLabels,
                             List<double> mins, List<double> maxs, List<double> avgs,
                             double? low, double? high)
        {
            chartTitle = title;
            kind = ChartKind.Band;
            labels = xLabels;
            minValues = mins;
            maxValues = maxs;
            values = avgs;
            Redraw();
        }

        public void ShowBars(string title, List<string> xLabels, List<double> data)
        {
            chartTitle = title;
            kind = ChartKind.Bars;
            labels = xLabels;
            values = data;
            minValues = new List<double>();
            maxValues = new List<double>();
            Redraw();
        }

        public void ShowPlaceholder(string title)
        {
            chartTitle = title;
            kind = ChartKind.None;
            values = new List<double>();
            Redraw();
        }

        private void Redraw()
        {
            TitleText.Text = chartTitle;

            if (kind == ChartKind.None || values.Count == 0)
            {
                NoDataText.Visibility = Visibility.Visible;
                CurrentValueText.Text = "--";
                MinValueText.Text = "--";
                AvgValueText.Text = "--";
                MaxValueText.Text = "--";
                MinTimeText.Text = "";
                MaxTimeText.Text = "";
                Sparkline.Points = null;
                return;
            }

            NoDataText.Visibility = Visibility.Collapsed;

            // Band mode has real min/max series; every other mode derives
            // min/max/avg straight from the main values list.
            bool hasBand = kind == ChartKind.Band && minValues.Count > 0 && maxValues.Count > 0;

            double minTemp = hasBand ? minValues.Min() : values.Min();
            double maxTemp = hasBand ? maxValues.Max() : values.Max();
            double avgTemp = values.Average();

            int minIndex = hasBand ? minValues.IndexOf(minTemp) : values.IndexOf(minTemp);
            int maxIndex = hasBand ? maxValues.IndexOf(maxTemp) : values.IndexOf(maxTemp);

            CurrentValueText.Text = values.Last().ToString("0.0");
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

            double min = values.Min();
            double max = values.Max();
            double range = (max - min) < 0.01 ? 1 : (max - min);

            var points = new PointCollection();
            for (int i = 0; i < values.Count; i++)
            {
                double x = width * i / (values.Count - 1);
                double normalized = (values[i] - min) / range;
                double y = height - (normalized * height);
                points.Add(new Point(x, y));
            }

            Sparkline.Points = points;
        }

        private void EnlargeChart(object sender, MouseButtonEventArgs e)
        {
            if (kind == ChartKind.None || values.Count == 0) return;
            Clicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Builds a second tile with the same data at a larger size, for the
        /// enlarge-on-click overlay. Vector rendering stays crisp at any
        /// size, unlike the old PNG upscale.
        /// </summary>
        public ChartTile CreateEnlargedCopy(double width = 420, double height = 320)
        {
            ChartTile copy = new ChartTile { Width = width, Height = height };

            switch (kind)
            {
                case ChartKind.Band:
                    copy.ShowBand(chartTitle, labels, minValues, maxValues, values, null, null);
                    break;
                case ChartKind.Bars:
                    copy.ShowBars(chartTitle, labels, values);
                    break;
                case ChartKind.Line:
                    copy.ShowLine(chartTitle, labels, values, null, null);
                    break;
                default:
                    copy.ShowPlaceholder(chartTitle);
                    break;
            }

            return copy;
        }
    }
}