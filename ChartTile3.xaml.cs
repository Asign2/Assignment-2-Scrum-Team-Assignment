using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using VectSharp.Plots;

namespace Assign_2
{
    /// <summary>
    /// Displays temperature values as a box plot.
    /// </summary>
    public partial class BoxPlot : UserControl
    {
        /// <summary>
        /// Gets the  collection of temperature values currently populated in the box plot.
        /// </summary>
        private List<double> values = new List<double>();

        /// <summary>
        /// Gets the temperature values currently used in the box plot.
        /// </summary>
        public List<double> RawValues { get; private set; }

        /// <summary>
        /// Creates and sets up a box plot.
        /// </summary>
        public BoxPlot()
        {
            InitializeComponent();
            SizeChanged += (s, e) => DrawNativeBoxPlot();
        }

        /// <summary>
        /// Loads temperature values and displays the box plot.
        /// </summary>
        /// <param name="temperatures">The collection of temperature data points to display.</param>
        public void ShowBoxPlot(List<double> temperatures)
        {
            values = temperatures ?? new List<double>();
            Redraw();
        }

        /// <summary>
        /// Displays a placeholder when no temperature data is available.
        /// </summary>
        /// <param name="title">The placeholder title text.</param>
        public void ShowPlaceholder(string title)
        {
            values = new List<double>();
            Redraw();
        }

        /// <summary>
        /// Refreshes the box plot display.
        /// </summary>
        public void Redraw()
        {   
            RawValues = values;

            if (values.Count == 0)
            {
                NoDataText.Visibility = Visibility.Visible;
                PointsText.Text = "--";
                MinText.Text = "--";
                AvgText.Text = "--";
                MaxText.Text = "--";
                LabelLayer.Children.Clear();
                return;
            }

            NoDataText.Visibility = Visibility.Collapsed;

            var sorted = values.OrderBy(v => v).ToList();
            double min = sorted.First();
            double max = sorted.Last();
            double avg = values.Average();

            PointsText.Text = sorted.Count.ToString();
            MinText.Text = min.ToString("0.0") + " °C";
            AvgText.Text = avg.ToString("0.00") + " °C";
            MaxText.Text = max.ToString("0.0") + " °C";

            DrawNativeBoxPlot();
        }

        /// <summary>
        /// Draws the box plot using the current temperature values.
        /// </summary>
        public void DrawNativeBoxPlot()
        {
            RawValues = values;
            LabelLayer.Children.Clear();
            if (values.Count == 0)
                return;

            // Fallback dimensions if running in a unit test where ActualHeight/Width are 0
            double canvasHeight = LabelLayer.ActualHeight > 0 ? LabelLayer.ActualHeight : 300;
            double canvasWidth = LabelLayer.ActualWidth > 0 ? LabelLayer.ActualWidth : 400;

            var sorted = values.OrderBy(v => v).ToList();
            double min = sorted.First();
            double max = sorted.Last();
            double q1 = GetPercentile(sorted, 25);
            double median = GetPercentile(sorted, 50);
            double q3 = GetPercentile(sorted, 75);

            double dataMin = min - 1;
            double dataMax = max + 1;
            double dataRange = dataMax - dataMin;
            if (dataRange <= 0) dataRange = 1;

            double centerX = canvasWidth / 2.0;
            double boxWidth = Math.Min(40, canvasWidth * 0.4);

            // Helper to map data value to canvas Y coordinate (inverted because WPF Y grows downwards)
            Func<double, double> scaleY = val =>
            {
                double normalized = (val - dataMin) / dataRange;
                return canvasHeight - (normalized * (canvasHeight - 20) + 10);
            };

            double yMin = scaleY(min);
            double yMax = scaleY(max);
            double yQ1 = scaleY(q1);
            double yQ3 = scaleY(q3);
            double yMed = scaleY(median);

            // Draw Whiskers (Vertical line from min to max, horizontal caps)
            var verticalWhisker = new Line
            {
                X1 = centerX,
                Y1 = yMin,
                X2 = centerX,
                Y2 = yMax,
                Stroke = Brushes.Gray,
                StrokeThickness = 1.5
            };

            var capMin = new Line
            {
                X1 = centerX - boxWidth / 4,
                Y1 = yMin,
                X2 = centerX + boxWidth / 4,
                Y2 = yMin,
                Stroke = Brushes.Gray,
                StrokeThickness = 1.5
            };

            var capMax = new Line
            {
                X1 = centerX - boxWidth / 4,
                Y1 = yMax,
                X2 = centerX + boxWidth / 4,
                Y2 = yMax,
                Stroke = Brushes.Gray,
                StrokeThickness = 1.5
            };

            // Draw Interquartile Box (Q1 to Q3)
            double boxHeight = Math.Abs(yQ1 - yQ3);
            var boxRect = new System.Windows.Shapes.Rectangle
            {
                Width = boxWidth,
                Height = Math.Max(2, boxHeight),
                Fill = new SolidColorBrush(Color.FromArgb(100, 40, 40, 45)),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(boxRect, centerX - boxWidth / 2.0);
            Canvas.SetTop(boxRect, Math.Min(yQ1, yQ3));

            //  Draw Median Line
            var medianLine = new Line
            {
                X1 = centerX - boxWidth / 2.0,
                Y1 = yMed,
                X2 = centerX + boxWidth / 2.0,
                Y2 = yMed,
                Stroke = Brushes.Cyan,
                StrokeThickness = 2
            };

            // Add elements to the native WPF canvas layer
            LabelLayer.Children.Add(verticalWhisker);
            LabelLayer.Children.Add(capMin);
            LabelLayer.Children.Add(capMax);
            LabelLayer.Children.Add(boxRect);
            LabelLayer.Children.Add(medianLine);
        }

        /// <summary>
        /// Calculates the specific percentile value from a sorted dataset.
        /// </summary>
        /// <param name="sortedData">The sorted collection of numeric data values.</param>
        /// <param name="percentile">The target percentile to calculate (e.g., 25, 50, 75).</param>
        /// <returns>The calculated percentile value.</returns>
        internal double GetPercentile(List<double> sortedData, double percentile)
        {
            int idx = (int)Math.Ceiling((percentile / 100.0) * sortedData.Count) - 1;
            return sortedData[Math.Max(0, Math.Min(idx, sortedData.Count - 1))];
        }

        /// <summary>
        /// Updates the box plot status based on sensor activity.
        /// </summary>
        /// <param name="isActive">Status of location sensor's</param>
        public void UpdateStatus(bool isActive)
        {
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