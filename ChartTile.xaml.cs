using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using VectSharp.Raster.ImageSharp;
using System.Windows.Input;
using System.Windows.Media;

namespace Assign_2
{
    /// <summary>
    /// A small chart panel. Give it some numbers via ShowLine / ShowBand /
    /// ShowBars, and it draws them using VectSharp, then displays the result
    /// as a picture in PlotImage.
    ///
    /// Requires the NuGet packages: VectSharp, VectSharp.Plots,
    /// VectSharp.Raster.ImageSharp.
    ///
    /// How it works, in order:
    ///   1. Store whatever numbers we were given.
    ///   2. Turn those numbers into a VectSharp "Plot".
    ///   3. Render the Plot to a temporary PNG file on disk.
    ///   4. Load that PNG into the PlotImage control so it shows on screen.
    /// </summary>
    public partial class ChartTile : UserControl
    {
        private enum ChartKind { None, Line, Band, Bars, Pie }

        private ChartKind kind = ChartKind.None;
        private string chartTitle = "";
        private List<string> labels = new List<string>();

        // "values" is the main series: the line in Line mode, the average in
        // Band mode, or the bar heights in Bars mode.
        private List<double> values = new List<double>();
        private List<double> minValues = new List<double>(); // Band mode only
        private List<double> maxValues = new List<double>(); // Band mode only

        public ChartTile()
        {
            InitializeComponent();
        }

        // ---------------------------------------------------------------
        // Public methods - these are what MainWindow.cs calls.
        // low/high are accepted for compatibility but not drawn any more;
        // dropping the dashed threshold lines kept this class much simpler.
        // ---------------------------------------------------------------

        public void ShowLine(string title, List<string> xLabels,
                             List<double> data, double? low, double? high)
        {
            chartTitle = title;
            kind = ChartKind.Line;
            labels = xLabels;
            values = data;
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
            Redraw();
        }

        /// <summary>One slice per value, sized by how big each value is.</summary>
        public void ShowPie(string title, List<string> xLabels, List<double> data)
        {
            chartTitle = title;
            kind = ChartKind.Pie;
            labels = xLabels;
            values = data;
            Redraw();
        }

        public void ShowPlaceholder(string title)
        {
            chartTitle = title;
            kind = ChartKind.None;
            values = new List<double>();
            Redraw();
        }

        private void PlotImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Redraw();
        }

        // ---------------------------------------------------------------
        // Everything below just turns the stored numbers into a picture.
        // ---------------------------------------------------------------

        private void Redraw()
        {
            if (kind == ChartKind.None || values.Count == 0)
            {
                PlotImage.Source = null;
                NoDataText.Visibility = Visibility.Visible;
                return;
            }

            NoDataText.Visibility = Visibility.Collapsed;

            VectSharp.Plots.Plot plot = kind switch
            {
                ChartKind.Bars => BuildBarChart(),
                _ => BuildLineChart()
            };

            DisplayPlot(plot);
        }

        /// <summary>Line mode draws one line. Band mode draws max, min and average.</summary>
        private VectSharp.Plots.Plot BuildLineChart()
        {
            List<(double, double)[]> lines = new List<(double, double)[]>();

            if (kind == ChartKind.Band)
            {
                lines.Add(ToPoints(maxValues));
                lines.Add(ToPoints(minValues));
            }

            lines.Add(ToPoints(values));

            return VectSharp.Plots.Plot.Create.LineCharts(
                lines.ToArray(),
                title: chartTitle,
                xAxisTitle: "Sample",
                yAxisTitle: "Value");
        }
        //Script to enlarge chart on click
        public event EventHandler<ChartTileClickedEventArgs> Clicked;

        private void EnlargeChart(object sender, MouseButtonEventArgs e)
        {
            if (PlotImage.Source == null)
            {
                return; // No chart to enlarge
            }
            Clicked?.Invoke(this, new ChartTileClickedEventArgs(PlotImage.Source, chartTitle));
        }


        /// <summary>One bar per value, labelled with the matching period.</summary>
        private VectSharp.Plots.Plot BuildBarChart()
        {
            (string, double)[] bars = new (string, double)[values.Count];

            for (int i = 0; i < values.Count; i++)
            {
                string label = i < labels.Count ? labels[i] : i.ToString();
                bars[i] = (label, values[i]);
            }

            return VectSharp.Plots.Plot.Create.BarChart(
                bars,
                title: chartTitle,
                yAxisTitle: "Count");
        }

       /// <summary>Turns a list of numbers into (x, y) points, using position as x.</summary>
        private static (double, double)[] ToPoints(List<double> data)
        {
            (double, double)[] points = new (double, double)[data.Count];

            for (int i = 0; i < data.Count; i++)
            {
                points[i] = (i, data[i]);
            }

            return points;
        }

        /// <summary>Saves the plot as a temp PNG, then shows that PNG in PlotImage.</summary>
        private void DisplayPlot(VectSharp.Plots.Plot plot)
        {
            // "VectSharp.Page" is written in full because WPF also has a
            // class called Page (System.Windows.Controls.Page) - without
            // the full name the compiler can't tell which one we mean.
            VectSharp.Page page = plot.Render();

            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
            page.SaveAsImage(tempFile);

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad; // load fully so we can delete the file
            image.UriSource = new Uri(tempFile);
            image.EndInit();
            image.Freeze();

            PlotImage.Source = image;

            File.Delete(tempFile);
        }

    }
}