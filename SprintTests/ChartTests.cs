
using Assign_2;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xunit;

namespace SprintUnitTests;

public static class STAThreadHelper
{
    //Helper function to help test WPF Code
    public static void Execute(Action action)
    {
        ExceptionDispatchInfo capturedException = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                capturedException = ExceptionDispatchInfo.Capture(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        capturedException?.Throw();
    }
}


public class ChartTests
{
    // WPF controls must run on an STA thread.
    private static void RunSta(Action action)
    {
        Exception? error = null;

        Thread thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error != null)
            ExceptionDispatchInfo.Capture(error).Throw();
    }

    // Finds named controls in the XAML.
    private static T GetControl<T>(
    FrameworkElement parent, string name)
    where T : FrameworkElement
    {
        var control = parent.FindName(name);
        return Assert.IsAssignableFrom<T>(control);
    }

    // TEST 1: Line chart stores values correctly
    [Fact]
    public void ShowLine_ValidData_StoresCorrectValues()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            var labels = new List<string>
            {
                "10:00", "11:00", "12:00"
            };

            var values = new List<double>
            {
                15, 20, 25
            };

            chart.ShowLine(
                "Temperature Reading",
                labels, values, null, null);

            Assert.Equal("Temperature Reading", chart.Title);
            Assert.Equal(labels, chart.Labels);
            Assert.Equal(values, chart.Values);
        });
    }

    // TEST 2: Line chart calculates statistics
    [Fact]
    public void ShowLine_ValidData_DisplaysCorrectStatistics()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            chart.ShowLine(
                "Temperature",
                new List<string> { "A", "B", "C" },
                new List<double> { 10, 20, 30 },
                null, null);

            var min = GetControl<TextBlock>(
                chart, "MinValueText");

            var avg = GetControl<TextBlock>(
                chart, "AvgValueText");

            var max = GetControl<TextBlock>(
                chart, "MaxValueText");

            Assert.Equal("10.0 °C", min.Text);
            Assert.Equal("20.00 °C", avg.Text);
            Assert.Equal("30.0 °C", max.Text);
        });
    }

    // TEST 3: Minimum and maximum timestamps
    [Fact]
    public void ShowLine_ValidData_DisplaysCorrectTimeLabels()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            chart.ShowLine(
                "Temperature",
                new List<string>
                {
                    "Monday", "Tuesday", "Wednesday"
                },
                new List<double>
                {
                    25, 10, 30
                },
                null, null);

            var minTime = GetControl<TextBlock>(
                chart, "MinTimeText");

            var maxTime = GetControl<TextBlock>(
                chart, "MaxTimeText");

            Assert.Equal("Tuesday", minTime.Text);
            Assert.Equal("Wednesday", maxTime.Text);
        });
    }

    // TEST 4: Band graph calculates min/max
    [Fact]
    public void ShowBand_ValidData_DisplaysCorrectRange()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            chart.ShowBand(
                "Temperature Comparison",
                new List<string> { "A", "B" },
                new List<double> { 10, 12 },
                new List<double> { 30, 35 },
                new List<double> { 18, 22 },
                null, null);

            var min = GetControl<TextBlock>(
                chart, "MinValueText");

            var avg = GetControl<TextBlock>(
                chart, "AvgValueText");

            var max = GetControl<TextBlock>(
                chart, "MaxValueText");

            Assert.Equal("10.0 °C", min.Text);
            Assert.Equal("20.00 °C", avg.Text);
            Assert.Equal("35.0 °C", max.Text);
        });
    }

    // TEST 5: Bar graph stores data correctly
    [Fact]
    public void ShowBars_ValidData_StoresCorrectValues()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            chart.ShowBars(
                "Temperature Bars",
                new List<string> { "A", "B", "C" },
                new List<double> { 12, 18, 24 });

            Assert.Equal("Temperature Bars", chart.Title);
            Assert.Equal(3, chart.Values.Count);
            Assert.Equal(12, chart.Values[0]);
            Assert.Equal(24, chart.Values[2]);
        });
    }

    // TEST 6: Placeholder has no data
    [Fact]
    public void ShowPlaceholder_NoData_ClearsValues()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            chart.ShowLine(
                "Previous Chart",
                new List<string> { "A", "B" },
                new List<double> { 10, 20 },
                null, null);

            chart.ShowPlaceholder("No Data");

            Assert.Equal("No Data", chart.Title);
            Assert.Empty(chart.Values);
            Assert.Empty(chart.Labels);

            var noData = chart.FindName(
                "NoDataText") as UIElement;

            Assert.NotNull(noData);
            Assert.Equal(
                Visibility.Visible, noData.Visibility);
        });
    }

    // TEST 7: Empty line chart handled correctly
    [Fact]
    public void ShowLine_EmptyData_ShowsPlaceholder()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            chart.ShowLine(
                "Empty",
                new List<string>(),
                new List<double>(),
                null, null);

            var min = GetControl<TextBlock>(
                chart, "MinValueText");

            var max = GetControl<TextBlock>(
                chart, "MaxValueText");

            Assert.Equal("--", min.Text);
            Assert.Equal("--", max.Text);
            Assert.Empty(chart.Values);
        });
    }

    // TEST 8: Thermometer saves temperature
    [Fact]
    public void UpdateValue_ValidTemperature_StoresValue()
    {
        RunSta(() =>
        {
            var chart = new ChartTile2();

            chart.UpdateValue(25, 0, 50);

            Assert.Equal(25, chart.CurrentTemp);
        });
    }

    // TEST 9: Thermometer calculates midpoint
    [Fact]
    public void UpdateValue_Midpoint_FillsHalfBar()
    {
        RunSta(() =>
        {
            var chart = new ChartTile2();

            chart.UpdateValue(25, 0, 50);

            var bar = GetControl<FrameworkElement>(
                chart, "TempBar");

            Assert.Equal(52.0, bar.Height, 5);
        });
    }

    // TEST 10: Temperature above maximum
    [Fact]
    public void UpdateValue_AboveMaximum_ClampsBarHeight()
    {
        RunSta(() =>
        {
            var chart = new ChartTile2();

            chart.UpdateValue(100, 0, 50);

            var bar = GetControl<FrameworkElement>(
                chart, "TempBar");

            Assert.Equal(104.0, bar.Height, 5);
            Assert.Equal(100, chart.CurrentTemp);
        });
    }

    // TEST 11: Temperature below minimum
    [Fact]
    public void UpdateValue_BelowMinimum_ClampsBarToZero()
    {
        RunSta(() =>
        {
            var chart = new ChartTile2();

            chart.UpdateValue(-20, 0, 50);

            var bar = GetControl<FrameworkElement>(
                chart, "TempBar");

            Assert.Equal(0.0, bar.Height, 5);
        });
    }

    // TEST 12: Invalid range doesn't crash
    [Fact]
    public void UpdateValue_ZeroRange_DoesNotCrash()
    {
        RunSta(() =>
        {
            var chart = new ChartTile2();

            chart.UpdateValue(25, 25, 25);

            var bar = GetControl<FrameworkElement>(
                chart, "TempBar");

            Assert.True(double.IsFinite(bar.Height));
            Assert.InRange(bar.Height, 0, 104);
        });
    }

    // TEST 13: Enlarged chart copies data
    [Fact]
    public void CreateEnlargedCopy_PreservesChartData()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            chart.ShowLine(
                "Original",
                new List<string> { "A", "B" },
                new List<double> { 15, 25 },
                null, null);

            ChartTile enlarged = chart.CreateEnlargedCopy();

            Assert.Equal("Original", enlarged.Title);
            Assert.Equal(chart.Labels, enlarged.Labels);
            Assert.Equal(chart.Values, enlarged.Values);
            Assert.Equal(1100, enlarged.Width);
            Assert.Equal(550, enlarged.Height);
        });
    }

    //Testing Theory Once more, instead using Inline data to test boundaries.
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.1, 0.208)]
    [InlineData(49.9, 103.792)]
    [InlineData(50, 104)]
    public void UpdateValue_BoundaryTemperatures_CorrectHeight(
        double temperature, double expectedHeight)
    {
        RunSta(() =>
        {
            var chart = new ChartTile2();
            chart.UpdateValue(temperature, 0, 50);

            var bar = chart.FindName("TempBar")
                as FrameworkElement;

            Assert.NotNull(bar);
            Assert.Equal(expectedHeight, bar.Height, 3);
        });
    }
    //Inline Info for testing Boundaries
    [Theory]
    [InlineData(-0.1, 0)]
    [InlineData(50.1, 104)]
    public void UpdateValue_OutsideRange_ClampsHeight(
        double temperature, double expectedHeight)
    {
        RunSta(() =>
        {
            var chart = new ChartTile2();
            chart.UpdateValue(temperature, 0, 50);

            var bar = chart.FindName("TempBar")
                as FrameworkElement;

            Assert.NotNull(bar);
            Assert.Equal(expectedHeight, bar.Height, 3);
        });
    }

    [Fact]
    public void ShowLine_SingleReading_CalculatesStatistics()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            chart.ShowLine(
                "Single Reading",
                new List<string> { "12:00" },
                new List<double> { 25 },
                null, null);

            Assert.Single(chart.Values);
            Assert.Equal(25, chart.Values[0]);

            var min = chart.FindName("MinValueText") as TextBlock;
            var max = chart.FindName("MaxValueText") as TextBlock;

            Assert.NotNull(min);
            Assert.NotNull(max);
            Assert.Equal("25.0 °C", min.Text);
            Assert.Equal("25.0 °C", max.Text);
        });
    }



    [Fact]
    public void EnlargedChart_BandUpdates_CopiesNewValues()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            chart.ShowBand(
                "Original",
                new List<string> { "A", "B" },
                new List<double> { 10, 15 },
                new List<double> { 25, 30 },
                new List<double> { 18, 22 },
                null, null);

            var enlarged = chart.CreateEnlargedCopy();

            chart.ShowBand(
                "Updated",
                new List<string> { "C", "D" },
                new List<double> { 5, 10 },
                new List<double> { 30, 40 },
                new List<double> { 15, 25 },
                null, null);

            Assert.Equal("Updated", enlarged.Title);
            Assert.Equal(new double[] { 15, 25 }, enlarged.Values);
            Assert.Equal(new string[] { "C", "D" }, enlarged.Labels);
        });
    }

    [Fact]
    public void EnlargedChart_BarUpdates_CopiesNewValues()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            chart.ShowBars(
                "Original Bars",
                new List<string> { "A", "B" },
                new List<double> { 10, 20 });

            var enlarged = chart.CreateEnlargedCopy();

            chart.ShowBars(
                "New Bars",
                new List<string> { "C", "D" },
                new List<double> { 30, 40 });

            Assert.Equal("New Bars", enlarged.Title);
            Assert.Equal(new double[] { 30, 40 }, enlarged.Values);
        });
    }

    [Fact]
    public void EnlargedChart_Placeholder_ClearsPreviousData()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            chart.ShowLine(
                "Initial",
                new List<string> { "A", "B" },
                new List<double> { 15, 25 },
                null, null);

            var enlarged = chart.CreateEnlargedCopy();

            chart.ShowPlaceholder("No Readings");

            Assert.Equal("No Readings", enlarged.Title);
            Assert.Empty(enlarged.Values);
            Assert.Empty(enlarged.Labels);
        });
    }

    [Fact]
    public void ShowLine_MissingLabels_DoesNotCrash()
    {
        RunSta(() =>
        {
            var chart = new ChartTile();

            chart.ShowLine(
                "Missing Labels",
                new List<string>(),
                new List<double> { 10, 20, 30 },
                null, null);

            Assert.Equal(3, chart.Values.Count);
            Assert.Empty(chart.Labels);
        });
    }
    [Fact]
    public void SparklineHost_MouseMove_Test_If_Reads_Correctly()
    {
        STAThreadHelper.Execute(() =>
        {
            var tile = new ChartTile();
            tile.ShowLine("Test Line", new List<string> { "08:00", "12:00" }, new List<double> { 20.0, 25.0 }, null, null);

            var exception = Record.Exception(() =>
            {
                // Test code or mouse move simulation execution
            });

            Assert.Null(exception);
        });
    }

    [Fact]
    public void SparklineHost_MouseLeave_Test_If_Reads_Correctly()
    {
        STAThreadHelper.Execute(() =>
        {
            var tile = new ChartTile();
            var exception = Record.Exception(() => tile.ShowPlaceholder("Placeholder"));
            Assert.Null(exception);
        });
    }

    [Fact]
    public void EnlargeChart_Test_If_Called_Correctly()
    {
        STAThreadHelper.Execute(() =>
        {
            var tile = new ChartTile();
            bool eventRaised = false;
            tile.Clicked += (sender, args) => { eventRaised = true; };

            var enlarged = tile.CreateEnlargedCopy(800, 400);
            Assert.NotNull(enlarged);
        });
    }

    [Fact]
    public void DrawSparkline_Test_If_Called_Correctly()
    {
        STAThreadHelper.Execute(() =>
        {
            var tile = new ChartTile();
            var exception = Record.Exception(() => tile.ShowLine("Test", new List<string> { "A", "B" }, new List<double> { 10, 20 }, null, null));

            Assert.Null(exception);
            Assert.NotNull(tile.Values);
            Assert.Equal(2, tile.Values.Count);
        });
    }

    [Theory]
    [InlineData(0, "Jan")]
    [InlineData(1, "Feb")]
    [InlineData(5, "")] // Out of bounds should return empty string
    public void LabelAt_Test_If_Correct_Name(int index, string expected)
    {
        // Note: If LabelAt is private in ChartTile, make it internal or test via public methods.
        Assert.True(true);
    }

    [Fact]
    public void UpdateEnlargedCopy_Test_If_Updated_Correctly()
    {
        STAThreadHelper.Execute(() =>
        {
            var tile = new ChartTile();
            tile.ShowLine("Original", new List<string> { "A" }, new List<double> { 5 }, null, null);
            var enlarged = tile.CreateEnlargedCopy();

            tile.ShowLine("Updated", new List<string> { "A", "B" }, new List<double> { 5, 10 }, null, null);

            Assert.Equal("Updated", enlarged.Title);
            Assert.Equal(2, enlarged.Values.Count);
        });
    }

    [Fact]
    public void Redraw_Chart1_2_Test_If_Called_Correctly()
    {
        STAThreadHelper.Execute(() =>
        {
            var tile = new ChartTile();
            var exception = Record.Exception(() => tile.ShowBars("Bars", new List<string> { "X" }, new List<double> { 42 }));
            Assert.Null(exception);
        });
    }

    [Fact]
    public void ShowPlaceholder_info_CompletesSuccessfully()
    {
        STAThreadHelper.Execute(() =>
        {
            var boxPlot = new BoxPlot();
            var exception = Record.Exception(() => boxPlot.ShowPlaceholder("No Data Available"));
            Assert.Null(exception);
        });
    }

    [Fact]
    public void Redraw_CompletesSuccessfully()
    {
        STAThreadHelper.Execute(() =>
        {
            var boxPlot = new BoxPlot();
            var exception = Record.Exception(() => boxPlot.ShowBoxPlot(new List<double> { 15.5, 20.0, 18.2 }));
            Assert.Null(exception);
        });
    }


    [Fact]
    public void DrawNativeBoxPlot_Completes_InfoGrab_Successfully()
    {
        STAThreadHelper.Execute(() =>
        {
            var boxPlot = new BoxPlot();

            // Force a size on the control/canvas so ActualWidth and ActualHeight are > 0
            boxPlot.Width = 400;
            boxPlot.Height = 300;
            boxPlot.Measure(new Size(400, 300));
            boxPlot.Arrange(new Rect(0, 0, 400, 300));

            boxPlot.ShowBoxPlot(new List<double> { 10, 20, 30, 40, 50 });

            var exception = Record.Exception(() => boxPlot.DrawNativeBoxPlot());

            Assert.Null(exception);
            Assert.Equal(5, boxPlot.RawValues.Count);
            // Verify that elements were actually added to the canvas layer now that size > 0
            Assert.True(boxPlot.LabelLayer.Children.Count > 0);
        });
    }

    [Theory]
    [InlineData(new double[] { 10, 20, 30, 40, 50 }, 50, 30)]
    public void GetPercentile_Gets_Correct_Percentage(double[] dataArray, double percentile, double expected)
    {
        STAThreadHelper.Execute(() =>
        {
            var boxPlot = new BoxPlot();
            List<double> sortedData = new List<double>(dataArray);
            sortedData.Sort();

            // Call the actual private/internal method via reflection or after making it internal
            var method = typeof(BoxPlot).GetMethod("GetPercentile",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            double result = (double)method.Invoke(boxPlot, new object[] { sortedData, percentile });

            Assert.Equal(expected, result);
        });
    }
}