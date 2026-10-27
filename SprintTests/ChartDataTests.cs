
using System;
using System.Collections.Generic;
using Assign_2;
using Xunit;

namespace SprintUnitTests;

public class ChartDataTests
{

    //Testing Theory As its needed for Graph testing.
    [Theory]
    [InlineData(Granularity.Daily)]
    [InlineData(Granularity.Weekly)]
    [InlineData(Granularity.Monthly)]
    [InlineData(Granularity.Yearly)]
    public void LineGraph_ReadingsHaveValidTemperatures(
        Granularity granularity)
    {
        SensorsDatabase.Initialize();

        var locations = SensorsDatabase.GetAllLocations();
        Assert.NotEmpty(locations);

        int locationId = locations[0].Id;

        var readings = SensorsDatabase.GetChartReadings(
            locationId, granularity);

        Assert.NotNull(readings);

        foreach (var reading in readings)
        {
            Assert.True(double.IsFinite(reading.Temperature));
        }
    }

    [Fact]
    public void BoxPlot_MinimumIsNeverGreaterThanMaximum()
    {
        SensorsDatabase.Initialize();

        var locations = SensorsDatabase.GetAllLocations();
        Assert.NotEmpty(locations);

        var aggregates = SensorsDatabase.GetAggregates(
            new List<int> { locations[0].Id },
            Granularity.Monthly);

        Assert.NotEmpty(aggregates);

        foreach (var aggregate in aggregates)
        {
            Assert.True(aggregate.MinTemp <= aggregate.MaxTemp);
        }
    }

    [Fact]
    public void BoxPlot_AverageIsWithinTemperatureRange()
    {
        SensorsDatabase.Initialize();

        var locations = SensorsDatabase.GetAllLocations();
        Assert.NotEmpty(locations);

        var aggregates = SensorsDatabase.GetAggregates(
            new List<int> { locations[0].Id },
            Granularity.Monthly);

        Assert.NotEmpty(aggregates);

        foreach (var aggregate in aggregates)
        {
            Assert.InRange(
                aggregate.AvgTemp,
                aggregate.MinTemp,
                aggregate.MaxTemp);
        }
    }

    [Fact]
    public void CurrentTemperature_ExistingLocation_ReturnsFiniteValue()
    {
        SensorsDatabase.Initialize();

        var locations = SensorsDatabase.GetAllLocations();
        Assert.NotEmpty(locations);

        double? temp = SensorsDatabase.GetLatestTemp(
            locations[0].Id);

        Assert.NotNull(temp);
        Assert.True(double.IsFinite(temp.Value));
    }

    [Fact]
    public void LineGraph_InvalidGranularity_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SensorsDatabase.GetChartReadings(
                1, (Granularity)999));
    }

    [Fact]
    public void BoxPlot_NoLocations_ReturnsEmpty()
    {
        var aggregates = SensorsDatabase.GetAggregates(
            new List<int>(),
            Granularity.Monthly);

        Assert.Empty(aggregates);
    }
}
