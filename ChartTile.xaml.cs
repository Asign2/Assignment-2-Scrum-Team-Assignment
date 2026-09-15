using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Assign_2
{
    /// <summary>
    /// A small self-contained chart panel. Drop more of these into the
    /// dashboard host as new visualisations are added.
    /// </summary>
    public partial class ChartTile : UserControl
    {
        private enum TileMode { Empty, Line, Band, Bars }

        private TileMode mode = TileMode.Empty;
        private List<string> labels = new List<string>();
        private List<double> primary = new List<double>();
        private List<double> lower = new List<double>();
        private List<double> upper = new List<double>();
        private double? warnLow;
        private double? warnHigh;

        public ChartTile()
        {
            InitializeComponent();
        }

        /// <summary>Single line, with optional admin temperature thresholds.</summary>
        public void ShowLine(string title, List<string> xLabels,
                             List<double> values, double? low, double? high)
        {
            TitleText.Text = title;
            mode = TileMode.Line;
            labels = xLabels;
            primary = values;
            warnLow = low;
            warnHigh = high;
            Redraw();
        }

        /// <summary>Min/max band with the average drawn through it.</summary>
        public void ShowBand(string title, List<string> xLabels,
                             List<double> mins, List<double> maxs, List<double> avgs,
                             double? low, double? high)
        {
            TitleText.Text = title;
            mode = TileMode.Band;
            labels = xLabels;
            lower = mins;
            upper = maxs;
            primary = avgs;
            warnLow = low;
            warnHigh = high;
            Redraw();
        }

        /// <summary>Simple vertical bars, e.g. sample counts per period.</summary>
        public void ShowBars(string title, List<string> xLabels, List<double> values)
        {
            TitleText.Text = title;
            mode = TileMode.Bars;
            labels = xLabels;
            primary = values;
            warnLow = null;
            warnHigh = null;
            Redraw();
        }

        /// <summary>Placeholder for a visualisation that is not built yet.</summary>
        public void ShowPlaceholder(string title)
        {
            TitleText.Text = title;
            mode = TileMode.Empty;
            primary = new List<double>();
            Redraw();
        }

        private void PlotCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Redraw();
        }

        /// <summary>Clears and repaints the canvas for the current mode.</summary>
        private void Redraw()
        {
            PlotCanvas.Children.Clear();

            double width = PlotCanvas.ActualWidth;
            double height = PlotCanvas.ActualHeight;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            if (mode == TileMode.Empty || primary == null || primary.Count == 0)
            {
                AddLabel("No data", width / 2 - 20, height / 2 - 8, Brushes.Gray);
                return;
            }

            double padLeft = 34;
            double padRight = 6;
            double padTop = 6;
            double padBottom = 16;

            double plotWidth = width - padLeft - padRight;
            double plotHeight = height - padTop - padBottom;

            double low = primary[0];
            double high = primary[0];

            foreach (double v in primary)
            {
                if (v < low) low = v;
                if (v > high) high = v;
            }

            if (mode == TileMode.Band)
            {
                foreach (double v in lower) if (v < low) low = v;
                foreach (double v in upper) if (v > high) high = v;
            }

            if (mode == TileMode.Bars)
            {
                low = 0;
            }

            if (warnLow.HasValue && warnLow.Value < low) low = warnLow.Value;
            if (warnHigh.HasValue && warnHigh.Value > high) high = warnHigh.Value;

            if (high - low < 0.001)
            {
                high = low + 1;
            }

            Func<int, double> xAt = i => primary.Count == 1
                ? padLeft + (plotWidth / 2)
                : padLeft + (plotWidth * i / (primary.Count - 1));

            Func<double, double> yAt = v =>
                padTop + plotHeight - ((v - low) / (high - low) * plotHeight);

            AddLine(padLeft, padTop, padLeft, padTop + plotHeight, Brushes.Gray, 1, false);
            AddLine(padLeft, padTop + plotHeight, padLeft + plotWidth,
                    padTop + plotHeight, Brushes.Gray, 1, false);

            AddLabel(Math.Round(high, 1).ToString(), 2, padTop - 6, Brushes.Gray);
            AddLabel(Math.Round(low, 1).ToString(), 2, padTop + plotHeight - 6, Brushes.Gray);

            if (labels.Count > 0)
            {
                AddLabel(labels[0], padLeft, padTop + plotHeight + 2, Brushes.Gray);

                string lastLabel = labels[labels.Count - 1];
                AddLabel(lastLabel,
                         padLeft + plotWidth - (lastLabel.Length * 5.0),
                         padTop + plotHeight + 2, Brushes.Gray);
            }

            // Admin temperature range shown as dashed guide lines
            if (warnLow.HasValue)
            {
                AddLine(padLeft, yAt(warnLow.Value), padLeft + plotWidth,
                        yAt(warnLow.Value), Brushes.CornflowerBlue, 1, true);
            }

            if (warnHigh.HasValue)
            {
                AddLine(padLeft, yAt(warnHigh.Value), padLeft + plotWidth,
                        yAt(warnHigh.Value), Brushes.IndianRed, 1, true);
            }

            if (mode == TileMode.Bars)
            {
                double barWidth = Math.Max(2, (plotWidth / primary.Count) * 0.6);

                for (int i = 0; i < primary.Count; i++)
                {
                    double top = yAt(primary[i]);

                    Rectangle bar = new Rectangle
                    {
                        Width = barWidth,
                        Height = Math.Max(1, padTop + plotHeight - top),
                        Fill = Brushes.SteelBlue
                    };

                    Canvas.SetLeft(bar, xAt(i) - (barWidth / 2));
                    Canvas.SetTop(bar, top);
                    PlotCanvas.Children.Add(bar);
                }

                return;
            }

            if (mode == TileMode.Band)
            {
                AddSeries(upper, xAt, yAt, Brushes.LightSalmon, 1);
                AddSeries(lower, xAt, yAt, Brushes.LightSkyBlue, 1);
            }

            AddSeries(primary, xAt, yAt, Brushes.SteelBlue, 2);

            for (int i = 0; i < primary.Count; i++)
            {
                bool outOfRange =
                    (warnLow.HasValue && primary[i] < warnLow.Value) ||
                    (warnHigh.HasValue && primary[i] > warnHigh.Value);

                Ellipse dot = new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = outOfRange ? Brushes.Red : Brushes.SteelBlue
                };

                Canvas.SetLeft(dot, xAt(i) - 2);
                Canvas.SetTop(dot, yAt(primary[i]) - 2);
                PlotCanvas.Children.Add(dot);
            }
        }

        /// <summary>Adds one polyline series to the canvas.</summary>
        private void AddSeries(List<double> values, Func<int, double> xAt,
                               Func<double, double> yAt, Brush stroke, double thickness)
        {
            if (values == null || values.Count == 0)
            {
                return;
            }

            PointCollection points = new PointCollection();

            for (int i = 0; i < values.Count; i++)
            {
                points.Add(new Point(xAt(i), yAt(values[i])));
            }

            PlotCanvas.Children.Add(new Polyline
            {
                Points = points,
                Stroke = stroke,
                StrokeThickness = thickness
            });
        }

        /// <summary>Adds a straight line, optionally dashed.</summary>
        private void AddLine(double x1, double y1, double x2, double y2,
                             Brush stroke, double thickness, bool dashed)
        {
            Line line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = stroke,
                StrokeThickness = thickness
            };

            if (dashed)
            {
                line.StrokeDashArray = new DoubleCollection { 3, 3 };
            }

            PlotCanvas.Children.Add(line);
        }

        /// <summary>Adds a small grey text label.</summary>
        private void AddLabel(string text, double left, double top, Brush brush)
        {
            TextBlock label = new TextBlock
            {
                Text = text,
                FontSize = 9,
                Foreground = brush
            };

            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, top);
            PlotCanvas.Children.Add(label);
        }
    }
}