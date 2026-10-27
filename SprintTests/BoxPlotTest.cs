
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using Assign_2;
using Xunit;

namespace SprintUnitTests;

public class BoxPlotTests
{
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

    // TEST 1-6: Execute box plot drawing
    // with six different temperature distributions.
    [Theory]
    [InlineData(new double[] { 10, 15, 20, 25, 30 })]
    [InlineData(new double[] { 20, 20, 20, 20, 20 })]
    [InlineData(new double[] { 25 })]
    [InlineData(new double[] { -10, -5, 0, 5, 10 })]
    [InlineData(new double[] { 5, 10, 50, 90, 100 })]
    [InlineData(new double[] { 30, 10, 40, 20, 50, 60 })]
    public void ShowBoxPlot_ValidDatasets_CompletesSuccessfully(
        double[] temperatures)
    {
        RunSta(() =>
        {
            var plot = new BoxPlot();

            // Fails the test if drawing throws an exception.
            plot.ShowBoxPlot(new List<double>(temperatures));
        });
    }

    // TEST 7: Repeated updates on one chart.
    [Fact]
    public void ShowBoxPlot_UpdatedData_RedrawsWithoutException()
    {
        RunSta(() =>
        {
            var plot = new BoxPlot();

            plot.ShowBoxPlot(new List<double>
            {
                10, 15, 20, 25, 30
            });

            plot.ShowBoxPlot(new List<double>
            {
                -5, 0, 5, 10, 15
            });

            plot.ShowBoxPlot(new List<double>
            {
                20, 22, 24, 26, 28
            });
        });
    }

    // TEST 8: Larger realistic dataset.
    [Fact]
    public void ShowBoxPlot_LargeDataset_CompletesSuccessfully()
    {
        RunSta(() =>
        {
            var plot = new BoxPlot();

            var temperatures = new List<double>();

            for (int i = 0; i < 200; i++)
            {
                temperatures.Add(15 + (i % 30) * 0.5);
            }

            plot.ShowBoxPlot(temperatures);
        });
    }

    // TEST 9: Decimal temperatures.
    [Fact]
    public void ShowBoxPlot_DecimalTemperatures_CompletesSuccessfully()
    {
        RunSta(() =>
        {
            var plot = new BoxPlot();

            plot.ShowBoxPlot(new List<double>
            {
                18.25, 19.75, 20.5, 21.35, 22.9
            });
        });
    }
}
