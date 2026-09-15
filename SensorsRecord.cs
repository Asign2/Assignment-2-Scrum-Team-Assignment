using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assign_2
{
    public enum Granularity
    {
        Hourly,
        Daily,
        Monthly,
        Yearly
    }

    /// <summary>A row from dbo.Locations, used by the user's location filter.</summary>
    public class LocationRecord
    {
        public int Id { get; set; }
        public string City { get; set; }
        public string Suburb { get; set; }
        public string Display { get; set; }
    }

    /// <summary>A row from dbo.Sensors joined to its location.</summary>
    public class SensorRecord
    {
        public int Id { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string City { get; set; }
        public string Suburb { get; set; }
        public string Display { get; set; }
    }

    /// <summary>One aggregated time bucket of temperature readings.</summary>
    public class ReadingAggregate
    {
        public DateTime PeriodStart { get; set; }
        public string Period { get; set; }
        public double AvgTemp { get; set; }
        public double MinTemp { get; set; }
        public double MaxTemp { get; set; }
        public int Samples { get; set; }
        public double Range => MaxTemp - MinTemp;
    }

    /// <summary>Admin-controlled dashboard configuration (single row, Id = 1).</summary>
    public class DashboardSettings
    {
        public double MinTemp { get; set; }
        public double MaxTemp { get; set; }
        public int GraphCount { get; set; }
        public string DefaultGranularity { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>A record of a dashboard that was actually displayed to a user.</summary>
    public class DashboardSnapshot
    {
        public int Id { get; set; }
        public string ViewedBy { get; set; }
        public DateTime ViewedAt { get; set; }
        public string Location { get; set; }
        public string Granularity { get; set; }
        public int GraphCount { get; set; }
        public int Buckets { get; set; }
        public double AvgTemp { get; set; }
    }
}