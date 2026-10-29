
using System;

namespace Assign_2
{
    /// <summary>
    /// Defines the available time intervals for temperature readings.
    /// </summary>
    public enum Granularity
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    /// <summary>
    /// Represents a location stored in the database.
    /// </summary>
    public class LocationRecord
    {
        /// <summary>
        /// The unique ID of the location.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The floor number of the location.
        /// </summary>
        public int Floor { get; set; }

        /// <summary>
        /// The room number of the location.
        /// </summary>
        public int Room { get; set; }

        /// <summary>
        /// The location text displayed to users.
        /// </summary>
        public string Display { get; set; }
    }

    /// <summary>
    /// Represents a sensor and its assigned location.
    /// </summary>
    public class SensorRecord
    {
        /// <summary>
        /// The unique ID of the sensor.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The manufacturer of the sensor.
        /// </summary>
        public string Make { get; set; }

        /// <summary>
        /// The model of the sensor.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// The temperature variance of the sensor.
        /// </summary>
        public int Variance { get; set; }

        /// <summary>
        /// Whether the sensor is active.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// The floor where the sensor is located.
        /// </summary>
        public int Floor { get; set; }

        /// <summary>
        /// The room where the sensor is located.
        /// </summary>
        public int Room { get; set; }
    }

    /// <summary>
    /// Stores aggregated temperature readings for a time period.
    /// </summary>
    public class ReadingAggregate
    {
        /// <summary>
        /// The location ID for the readings.
        /// </summary>
        public int Location { get; set; }

        /// <summary>
        /// The time period of the readings.
        /// </summary>
        public string Period { get; set; }

        /// <summary>
        /// The average temperature.
        /// </summary>
        public double AvgTemp { get; set; }

        /// <summary>
        /// The minimum temperature.
        /// </summary>
        public double MinTemp { get; set; }

        /// <summary>
        /// The maximum temperature.
        /// </summary>
        public double MaxTemp { get; set; }

        /// <summary>
        /// The number of temperature readings.
        /// </summary>
        public int Samples { get; set; }

        /// <summary>
        /// The difference between the maximum and minimum temperatures.
        /// </summary>
        public double Range => MaxTemp - MinTemp;
    }

    /// <summary>
    /// Stores the dashboard settings configured by an admin.
    /// </summary>
    public class DashboardSettings
    {
        /// <summary>
        /// The number of graphs to display.
        /// </summary>
        public int GraphCount { get; set; }

        /// <summary>
        /// The default time interval for readings.
        /// </summary>
        public string DefaultGranularity { get; set; }

        /// <summary>
        /// Whether to display the current temperature graph.
        /// </summary>
        public bool ShowCurrentTemp { get; set; }

        /// <summary>
        /// Whether to display the temperature comparison graph.
        /// </summary>
        public bool ShowTempComparison { get; set; }

        /// <summary>
        /// Whether to display the temperature reading graph.
        /// </summary>
        public bool ShowTempReading { get; set; }

        /// <summary>
        /// The username of the last admin to update the settings.
        /// </summary>
        public string UpdatedBy { get; set; }

        /// <summary>
        /// The date and time the settings were last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// The interval used for generating temperature readings.
        /// </summary>
        public int Tick { get; set; }
    }

    /// <summary>
    /// Stores a record of a dashboard viewed by a user.
    /// </summary>
    public class DashboardSnapshot
    {
        /// <summary>
        /// The username of the person viewing the dashboard.
        /// </summary>
        public string ViewedBy { get; set; }

        /// <summary>
        /// The date and time the dashboard was viewed.
        /// </summary>
        public DateTime ViewedAt { get; set; }

        /// <summary>
        /// The location viewed on the dashboard.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// The selected time interval.
        /// </summary>
        public string Granularity { get; set; }

        /// <summary>
        /// The number of graphs displayed.
        /// </summary>
        public int GraphCount { get; set; }

        /// <summary>
        /// The average temperature recorded.
        /// </summary>
        public double AvgTemp { get; set; }
    }
}
