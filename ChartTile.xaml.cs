using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VectSharp.Plots;

namespace Assign_2
{
    /// <summary>
    /// Displays temperature readings as a chart tile.
    /// </summary>
    public partial class ChartTile : UserControl
    {
        private enum ChartKind { None, Line, Band, Bars }
        /// <summary>
        /// Occurs when the chart tile is clicked.
        /// </summary>
        public event EventHandler Clicked;
        private ChartKind kind = ChartKind.None; 
        private string chartTitle = "Bar Graph";
        private List<string> labels = new List<string>();
        private List<double> values = new List<double>();
        private List<double> minValues = new List<double>();
        private List<double> maxValues = new List<double>();
        // Track the active enlarged copy linked to this tile
        private ChartTile activeEnlargedCopy;
        /// <summary>
        /// Gets the current chart title.
        /// </summary>
        public string Title { get; private set; }
        /// <summary>
        /// Gets the labels used by the current chart.
        /// </summary>
        public List<string> Labels { get; private set; }
        /// <summary>
        /// Gets the values used by the current chart.
        /// </summary>
        public List<double> Values { get; private set; }
        /// <summary>
        /// Gets whether the chart location is active.
        /// </summary>
        public bool isActive { get; private set; }
        /// <summary>
        /// Creates and sets up a new chart tile.
        /// </summary>
        public ChartTile()
        {
            InitializeComponent();
            SparklineHost.SizeChanged += (s, e) => DrawSparkline();

        }
        /// <summary>
        /// Displays temperature data as a line chart.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="xLabels"></param>
        /// <param name="data"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        public void ShowLine(string title, List<string> xLabels,
                             List<double> data, double? low, double? high)
        {
            chartTitle = title;
            kind = ChartKind.Line;
            labels = xLabels;
            values = data;

            // Correctly set public properties after updating fields
            Title = title;
            Labels = labels;
            Values = values;

            minValues = new List<double>();
            maxValues = new List<double>();
            Redraw();
            UpdateEnlargedCopy();
        }
        /// <summary>
        /// Displays the minimum, maximum and average values as a band chart.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="xLabels"></param>
        /// <param name="mins"></param>
        /// <param name="maxs"></param>
        /// <param name="avgs"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
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

            Title = title;
            Labels = labels;
            Values = values;

            Redraw();
            UpdateEnlargedCopy();
        }
        /// <summary>
        /// Displays temperature values as a bar chart.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="xLabels"></param>
        /// <param name="data"></param>
        public void ShowBars(string title, List<string> xLabels, List<double> data)
        {
            chartTitle = title;
            kind = ChartKind.Bars;
            labels = xLabels;
            values = data;

            Title = title;
            Labels = labels;
            Values = values;

            minValues = new List<double>();
            maxValues = new List<double>();
            Redraw();
            UpdateEnlargedCopy();
        }
        /// <summary>
        /// Displays a placeholder when chart data is unavailable.
        /// </summary>
        /// <param name="title"></param>
        public void ShowPlaceholder(string title)
        {
            chartTitle = title;
            kind = ChartKind.None;
            values = new List<double>();
            Title = title;
            Labels = new List<string>();
            Values = new List<double>();
            Redraw();
            UpdateEnlargedCopy();
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

            double minTemp = hasBand ? minValues.Min() : values.Min();
            double maxTemp = hasBand ? maxValues.Max() : values.Max();
            double avgTemp = values.Average();

            int minIndex = hasBand ? minValues.IndexOf(minTemp) : values.IndexOf(minTemp);
            int maxIndex = hasBand ? maxValues.IndexOf(maxTemp) : values.IndexOf(maxTemp);

            MinValueText.Text = minTemp.ToString("0.0") + " °C";
            AvgValueText.Text = avgTemp.ToString("0.00") + " °C";
            MaxValueText.Text = maxTemp.ToString("0.0") + " °C";
            MinTimeText.Text = LabelAt(minIndex);
            MaxTimeText.Text = LabelAt(maxIndex);

            DrawSparkline();
            UpdateEnlargedCopy();
        }
        private void UpdateEnlargedCopy()
        {
            if (activeEnlargedCopy == null) return;

            // Mirror the exact data type to the enlarged copy
            switch (kind)
            {
                case ChartKind.Line:
                    activeEnlargedCopy.ShowLine(chartTitle, labels, values, null, null);
                    break;
                case ChartKind.Band:
                    activeEnlargedCopy.ShowBand(chartTitle, labels, minValues, maxValues, values, null, null);
                    break;
                case ChartKind.Bars:
                    activeEnlargedCopy.ShowBars(chartTitle, labels, values);
                    break;
                case ChartKind.None:
                    activeEnlargedCopy.ShowPlaceholder(chartTitle);
                    break;
            }
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
        /// <summary>
        /// Creates a larger copy of the current chart.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public ChartTile CreateEnlargedCopy(double width = 1100, double height = 550)
        {
            activeEnlargedCopy = new ChartTile { Width = width, Height = height };
            activeEnlargedCopy.UpdateStatus(isActive);
            UpdateEnlargedCopy();
            
            return activeEnlargedCopy;
        }

        //Shows the temp of the hovered Point on the line
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
        /// <summary>
        /// Updates the chart status to show whether its sensor is active.
        /// </summary>
        public void UpdateStatus(bool isActive)
        {
            this.isActive = isActive;
            Activated.Visibility = System.Windows.Visibility.Visible;
            Deactivated.Visibility = System.Windows.Visibility.Collapsed;

            if (!isActive)
            {
                Activated.Visibility = System.Windows.Visibility.Collapsed;
                Deactivated.Visibility = System.Windows.Visibility.Visible;
            }
        }
    }
}