using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace Assign_2
{
    public static class SensorsDatabase
    {
        public static void Initialize()
        {
            using var connection = UserDatabase.OpenConnection();

            // Create Locations table
            bool locationsExists = RunScalarBool(
                connection,
                "SELECT OBJECT_ID('dbo.Locations', 'U');",
                true);

            if (!locationsExists)
            {
                RunNonQuery(connection, @"
                    CREATE TABLE dbo.Locations
                    (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        City NVARCHAR(100) NOT NULL,
                        Suburb NVARCHAR(100) NOT NULL
                    );");
            }

            // Create Sensors table
            bool sensorsExists = RunScalarBool(
                connection,
                "SELECT OBJECT_ID('dbo.Sensors', 'U');",
                true);

            if (!sensorsExists)
            {
                RunNonQuery(connection, @"
                    CREATE TABLE dbo.Sensors
                    (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Date_Installed DATETIME2 NOT NULL,
                        Make NVARCHAR(100) NOT NULL,
                        Model NVARCHAR(100) NOT NULL,
                        Location_Id INT NOT NULL,

                        CONSTRAINT FK_Sensors_Locations
                        FOREIGN KEY (Location_Id)
                        REFERENCES dbo.Locations(Id)
                    );");
            }

            // Create Data table
            bool dataExists = RunScalarBool(
                connection,
                "SELECT OBJECT_ID('dbo.Data', 'U');",
                true);

            if (!dataExists)
            {
                RunNonQuery(connection, @"
                    CREATE TABLE dbo.Data
                    (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        [Timestamp] DATETIME2 NOT NULL
                            DEFAULT GETDATE(),
                        Temperature FLOAT NOT NULL,
                        Sensor_Id INT NOT NULL,

                        CONSTRAINT FK_Data_Sensors
                        FOREIGN KEY (Sensor_Id)
                        REFERENCES dbo.Sensors(Id)
                    );");
            }

            // Create DashboardSettings table (one row of admin config)
            bool settingsExists = RunScalarBool(
                connection,
                "SELECT OBJECT_ID('dbo.DashboardSettings', 'U');",
                true);

            if (!settingsExists)
            {
                RunNonQuery(connection, @"
                    CREATE TABLE dbo.DashboardSettings
                    (
                        Id INT PRIMARY KEY,
                        MinTemp FLOAT NOT NULL,
                        MaxTemp FLOAT NOT NULL,
                        GraphCount INT NOT NULL,
                        DefaultGranularity NVARCHAR(20) NOT NULL,
                        UpdatedBy NVARCHAR(100) NULL,
                        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
                    );");

                RunNonQuery(connection, @"
                    INSERT INTO dbo.DashboardSettings
                        (Id, MinTemp, MaxTemp, GraphCount, DefaultGranularity)
                    VALUES (1, 5, 30, 2, 'Monthly');");
            }

            // Create DashboardLog table (recording of displayed dashboards)
            bool logExists = RunScalarBool(
                connection,
                "SELECT OBJECT_ID('dbo.DashboardLog', 'U');",
                true);

            if (!logExists)
            {
                RunNonQuery(connection, @"
                    CREATE TABLE dbo.DashboardLog
                    (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        ViewedBy NVARCHAR(100) NOT NULL,
                        ViewedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        Location NVARCHAR(200) NOT NULL,
                        Granularity NVARCHAR(20) NOT NULL,
                        GraphCount INT NOT NULL,
                        Buckets INT NOT NULL,
                        AvgTemp FLOAT NOT NULL
                    );");
            }

            SeedSampleData(connection);
        }

        private static void RunNonQuery(
            SqlConnection connection,
            string sql)
        {
            using var command =
                new SqlCommand(sql, connection);

            command.ExecuteNonQuery();
        }

        private static bool RunScalarBool(
            SqlConnection connection,
            string sql,
            bool checkNotNull = false)
        {
            using var command =
                new SqlCommand(sql, connection);

            object result = command.ExecuteScalar();

            if (checkNotNull)
            {
                return result != null &&
                       result != DBNull.Value;
            }

            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Inserts a starter location, sensor and a spread of readings if
        /// dbo.Data is empty, so the dashboard has something to show on a
        /// freshly created database.
        /// </summary>
        private static void SeedSampleData(SqlConnection connection)
        {
            bool dataHasRows = RunScalarBool(
                connection,
                "SELECT COUNT(*) FROM dbo.Data;");

            if (dataHasRows)
            {
                return;
            }

            bool locationsHaveRows = RunScalarBool(
                connection,
                "SELECT COUNT(*) FROM dbo.Locations;");

            if (!locationsHaveRows)
            {
                RunNonQuery(connection, @"
                    INSERT INTO dbo.Locations (City, Suburb)
                    VALUES ('Palmerston North', 'Hokowhitu');");
            }

            bool sensorsHaveRows = RunScalarBool(
                connection,
                "SELECT COUNT(*) FROM dbo.Sensors;");

            if (!sensorsHaveRows)
            {
                RunNonQuery(connection, @"
                    INSERT INTO dbo.Sensors (Date_Installed, Make, Model, Location_Id)
                    VALUES (GETDATE(), 'Acme', 'TempSense 1', 1);");
            }

            RunNonQuery(connection, @"
                INSERT INTO dbo.Data (Timestamp, Temperature, Sensor_Id)
                VALUES
                (DATEADD(hour, -1, GETDATE()), 18.2, 1),
                (DATEADD(hour, -5, GETDATE()), 19.1, 1),
                (DATEADD(hour, -12, GETDATE()), 16.7, 1),
                (DATEADD(day, -1, GETDATE()), 17.5, 1),
                (DATEADD(day, -2, GETDATE()), 20.3, 1),
                (DATEADD(day, -5, GETDATE()), 14.9, 1),
                (DATEADD(day, -10, GETDATE()), 15.8, 1),
                (DATEADD(day, -20, GETDATE()), 21.6, 1),
                (DATEADD(month, -1, GETDATE()), 22.4, 1),
                (DATEADD(month, -2, GETDATE()), 13.2, 1),
                (DATEADD(month, -6, GETDATE()), 25.1, 1),
                (DATEADD(year, -1, GETDATE()), 11.4, 1);");
        }

        /// <summary>Every location, for the user's location filter.</summary>
        public static List<LocationRecord> GetAllLocations()
        {
            List<LocationRecord> locations = new List<LocationRecord>();

            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                SELECT Id, City, Suburb
                FROM dbo.Locations
                ORDER BY City, Suburb;", connection);

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                LocationRecord location = new LocationRecord
                {
                    Id = reader.GetInt32(0),
                    City = reader.GetString(1),
                    Suburb = reader.GetString(2)
                };

                location.Display = location.Suburb + ", " + location.City;
                locations.Add(location);
            }

            return locations;
        }

        /// <summary>Every sensor, for the admin's sensor settings tab.</summary>
        public static List<SensorRecord> GetAllSensors()
        {
            List<SensorRecord> sensors = new List<SensorRecord>();

            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                SELECT s.Id, s.Make, s.Model, l.City, l.Suburb
                FROM dbo.Sensors s
                INNER JOIN dbo.Locations l ON l.Id = s.Location_Id
                ORDER BY l.City, l.Suburb, s.Id;", connection);

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                sensors.Add(new SensorRecord
                {
                    Id = reader.GetInt32(0),
                    Make = reader.GetString(1),
                    Model = reader.GetString(2),
                    City = reader.GetString(3),
                    Suburb = reader.GetString(4)
                });
            }

            return sensors;
        }

        /// <summary>
        /// Groups dbo.Data into time buckets for one location (0 = all locations).
        /// </summary>
        /// <param name="locationId">Location to filter on, or 0 for every location.</param>
        /// <param name="granularity">Bucket size: hour, day, month or year.</param>
        public static List<ReadingAggregate> GetAggregates(
            int locationId,
            Granularity granularity)
        {
            string bucket;

            switch (granularity)
            {
                case Granularity.Hourly:
                    bucket = "DATEADD(hour, DATEDIFF(hour, 0, d.[Timestamp]), 0)";
                    break;
                case Granularity.Daily:
                    bucket = "DATEADD(day, DATEDIFF(day, 0, d.[Timestamp]), 0)";
                    break;
                case Granularity.Monthly:
                    bucket = "DATEADD(month, DATEDIFF(month, 0, d.[Timestamp]), 0)";
                    break;
                default:
                    bucket = "DATEADD(year, DATEDIFF(year, 0, d.[Timestamp]), 0)";
                    break;
            }

            string sql =
                "SELECT " + bucket + " AS PeriodStart, " +
                "       AVG(d.Temperature), MIN(d.Temperature), " +
                "       MAX(d.Temperature), COUNT(*) " +
                "FROM dbo.Data d " +
                "INNER JOIN dbo.Sensors s ON s.Id = d.Sensor_Id " +
                "WHERE (@locationId = 0 OR s.Location_Id = @locationId) " +
                "GROUP BY " + bucket + " " +
                "ORDER BY 1;";

            List<ReadingAggregate> rows = new List<ReadingAggregate>();

            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@locationId", locationId);

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                DateTime start = reader.GetDateTime(0);

                rows.Add(new ReadingAggregate
                {
                    Period = FormatPeriod(start, granularity),
                    AvgTemp = Math.Round(reader.GetDouble(1), 2),
                    MinTemp = Math.Round(reader.GetDouble(2), 2),
                    MaxTemp = Math.Round(reader.GetDouble(3), 2),
                    Samples = reader.GetInt32(4)
                });
            }

            return rows;
        }

        /// <summary>Formats a bucket start to suit its granularity.</summary>
        private static string FormatPeriod(DateTime start, Granularity granularity)
        {
            switch (granularity)
            {
                case Granularity.Hourly:
                    return start.ToString("dd MMM HH:00");
                case Granularity.Daily:
                    return start.ToString("dd MMM yy");
                case Granularity.Monthly:
                    return start.ToString("MMM yyyy");
                default:
                    return start.ToString("yyyy");
            }
        }

        /// <summary>Reads the single admin settings row.</summary>
        public static DashboardSettings GetSettings()
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                SELECT MinTemp, MaxTemp, GraphCount,
                       DefaultGranularity, UpdatedBy, UpdatedAt
                FROM dbo.DashboardSettings
                WHERE Id = 1;", connection);

            using SqlDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return new DashboardSettings
                {
                    MinTemp = 5,
                    MaxTemp = 30,
                    GraphCount = 2,
                    DefaultGranularity = "Monthly"
                };
            }

            return new DashboardSettings
            {
                MinTemp = reader.GetDouble(0),
                MaxTemp = reader.GetDouble(1),
                GraphCount = reader.GetInt32(2),
                DefaultGranularity = reader.GetString(3),
                UpdatedBy = reader.IsDBNull(4) ? "" : reader.GetString(4),
                UpdatedAt = reader.GetDateTime(5)
            };
        }

        /// <summary>Saves the admin settings row.</summary>
        public static void SaveSettings(DashboardSettings settings, string updatedBy)
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                UPDATE dbo.DashboardSettings
                SET MinTemp = @min,
                    MaxTemp = @max,
                    GraphCount = @graphs,
                    DefaultGranularity = @gran,
                    UpdatedBy = @by,
                    UpdatedAt = GETDATE()
                WHERE Id = 1;", connection);

            command.Parameters.AddWithValue("@min", settings.MinTemp);
            command.Parameters.AddWithValue("@max", settings.MaxTemp);
            command.Parameters.AddWithValue("@graphs", settings.GraphCount);
            command.Parameters.AddWithValue("@gran", settings.DefaultGranularity);
            command.Parameters.AddWithValue("@by", (object)updatedBy ?? DBNull.Value);

            command.ExecuteNonQuery();
        }

        /// <summary>Records a dashboard that was displayed to a user.</summary>
        public static void RecordDashboard(DashboardSnapshot snapshot)
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                INSERT INTO dbo.DashboardLog
                    (ViewedBy, Location, Granularity, GraphCount, Buckets, AvgTemp)
                VALUES (@by, @loc, @gran, @graphs, @buckets, @avg);", connection);

            command.Parameters.AddWithValue("@by", snapshot.ViewedBy);
            command.Parameters.AddWithValue("@loc", snapshot.Location);
            command.Parameters.AddWithValue("@gran", snapshot.Granularity);
            command.Parameters.AddWithValue("@graphs", snapshot.GraphCount);
            command.Parameters.AddWithValue("@buckets", snapshot.Buckets);
            command.Parameters.AddWithValue("@avg", snapshot.AvgTemp);

            command.ExecuteNonQuery();
        }

        /// <summary>Most recent dashboard records, for the admin log tab.</summary>
        public static List<DashboardSnapshot> GetDashboardLog(int take = 200)
        {
            List<DashboardSnapshot> log = new List<DashboardSnapshot>();

            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                SELECT TOP (@take) ViewedBy, ViewedAt, Location,
                       Granularity, GraphCount, Buckets, AvgTemp
                FROM dbo.DashboardLog
                ORDER BY ViewedAt DESC;", connection);

            command.Parameters.AddWithValue("@take", take);

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                log.Add(new DashboardSnapshot
                {
                    ViewedBy = reader.GetString(0),
                    ViewedAt = reader.GetDateTime(1),
                    Location = reader.GetString(2),
                    Granularity = reader.GetString(3),
                    GraphCount = reader.GetInt32(4),
                    Buckets = reader.GetInt32(5),
                    AvgTemp = Math.Round(reader.GetDouble(6), 2)
                });
            }

            return log;
        }
    }
}