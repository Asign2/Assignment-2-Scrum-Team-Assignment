using Assign_2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace SensorDatabaseTests;

// Add as a NEW file in SprintTests. It does not replace existing test classes.
// These are database-backed integration tests. They READ from the existing database,
// except the UI export test, which writes a JSON file in the test working directory.
// No sensor/user/settings records are changed by these tests.
public class SensorDatabaseTests
{
    private static void RunWindow(Action<MainWindow> body)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            MainWindow? window = null;
            try
            {
                window = new MainWindow(false);
                body(window);
            }
            catch (Exception ex) { failure = ex; }
            finally { window?.Close(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void Call(MainWindow window, string name, params object?[] args)
    {
        var method = typeof(MainWindow).GetMethod(name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        try { method.Invoke(window, args); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    [Theory]
    [InlineData(Granularity.Daily)]
    [InlineData(Granularity.Weekly)]
    [InlineData(Granularity.Monthly)]
    [InlineData(Granularity.Yearly)]
    public void GetAggregates_ValidGranularity_ReturnsConsistentValues(Granularity granularity)
    {
        // Exercises every SQL grouping and period-formatting branch.
        var location = Assert.Single(SensorsDatabase.GetAllLocations().Take(1));
        var readings = SensorsDatabase.GetAggregates(new List<int> { location.Id }, granularity);
        foreach (var r in readings)
        {
            Assert.Equal(location.Id, r.Location);
            Assert.False(string.IsNullOrWhiteSpace(r.Period));
            Assert.True(r.Samples > 0);
            Assert.True(r.MinTemp <= r.AvgTemp && r.AvgTemp <= r.MaxTemp);
        }
    }

    [Fact]
    public void GetAggregates_NoLocations_ReturnsEmpty()
    {
        Assert.Empty(SensorsDatabase.GetAggregates(new List<int>(), Granularity.Monthly));
    }

    [Theory]
    [InlineData(Granularity.Daily)]
    [InlineData(Granularity.Weekly)]
    [InlineData(Granularity.Monthly)]
    [InlineData(Granularity.Yearly)]
    public void GetChartReadings_KnownLocation_ReturnsChronologicalReadings(Granularity granularity)
    {
        var location = Assert.Single(SensorsDatabase.GetAllLocations().Take(1));
        var readings = SensorsDatabase.GetChartReadings(location.Id, granularity);
        Assert.True(readings.SequenceEqual(readings.OrderBy(r => r.Period)));
        Assert.All(readings, r => Assert.False(double.IsNaN(r.Temperature)));
    }

    [Fact]
    public void GetChartReadings_InvalidGranularity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SensorsDatabase.GetChartReadings(-1, (Granularity)999));
    }

    [Fact]
    public void GetLatestTemp_NonexistentLocation_ReturnsNull()
    {
        Assert.Null(SensorsDatabase.GetLatestTemp(-1));
    }

    [Fact]
    public void IsLocationActive_MatchesSensorRecords()
    {
        var locations = SensorsDatabase.GetAllLocations();
        var sensors = SensorsDatabase.GetAllSensors();
        foreach (var location in locations)
        {
            bool expected = sensors.Any(s => s.Floor == location.Floor &&
                                             s.Room == location.Room && s.Active);
            Assert.Equal(expected, SensorsDatabase.IsLocationActive(location.Id));
        }
    }

    [Fact]
    public void GetDashboardLog_ZeroLimit_ReturnsEmpty()
    {
        Assert.Empty(SensorsDatabase.GetDashboardLog(0));
    }

    [Fact]
    public void RedrawChartTiles_WithAllLocationsAndReadings_RefreshesExistingTiles()
    {
        // Select ALL locations, not just one: current production RedrawChartTiles
        // loops over the entire location table and accesses chartTiles[location.Id].
        // Choosing one location when other locations have readings can crash it.
        RunWindow(window =>
        {
            var locations = SensorsDatabase.GetAllLocations();
            Assert.NotEmpty(locations);

            var tree = Assert.IsType<TreeView>(window.FindName("LocationTreeView"));
            tree.Items.Clear();
            foreach (var group in locations.GroupBy(l => l.Floor))
            {
                var floor = new TreeViewItem
                {
                    Header = new CheckBox { Content = $"Floor {group.Key}", IsChecked = true }
                };
                foreach (var loc in group)
                    floor.Items.Add(new TreeViewItem
                    {
                        Header = new CheckBox { Content = $"Room {loc.Room}", IsChecked = true },
                        Tag = loc
                    });
                tree.Items.Add(floor);
            }

            var combo = Assert.IsType<ComboBox>(window.FindName("UserGranularityBox"));
            combo.SelectedItem = combo.Items.OfType<ComboBoxItem>()
                .First(i => i.Content?.ToString() == "Monthly");

            Call(window, "BuildChartTiles", locations.Select(l => l.Id).ToList(),
                Granularity.Monthly);
            var host = Assert.IsType<Grid>(window.FindName("ChartHost"));
            Assert.NotEmpty(host.Children); // requires seeded readings
            int before = host.Children.Count;

            window.RedrawChartTiles();
            Assert.Equal(before, host.Children.Count);
        });
    }
}
