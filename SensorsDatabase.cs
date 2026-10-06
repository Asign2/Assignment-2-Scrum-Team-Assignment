using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Assign_2
{
    public static class SensorsDatabase
    {
        public static void Initialize()
        {
            using var connection = UserDatabase.OpenConnection();

            // 1. Create Locations table
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
                        Floor INT NOT NULL,
                        Room INT NOT NULL
                    );");
            }

            // 2. Create Sensors table (linked to Locations via Foreign Key)
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
                        Date_Installed DATE NOT NULL,
                        Make VARCHAR(255) NOT NULL,
                        Model VARCHAR(255) NOT NULL,
                        Variance INT NOT NULL,
                        Location_Id INT NOT NULL,
                        Active BIT NOT NULL DEFAULT 1,
                        CONSTRAINT FK_Sensors_Locations
                        FOREIGN KEY (Location_Id)
                        REFERENCES dbo.Locations(Id)
                        ON DELETE CASCADE
                    );");
            }

            // 3. Create Data table (linked to Sensors via Foreign Key)
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
                        [Timestamp] DATETIME2 NOT NULL DEFAULT GETDATE(),
                        Temperature DECIMAL(5,2) NOT NULL,
                        Sensor_Id INT NOT NULL,

                        CONSTRAINT FK_Data_Sensors
                        FOREIGN KEY (Sensor_Id)
                        REFERENCES dbo.Sensors(Id)
                        ON DELETE CASCADE
                    );");
            }

            // 4. Create DashboardSettings table
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
                        DefaultGranularity NVARCHAR(255) NOT NULL,
                        UpdatedBy NVARCHAR(255) NULL,
                        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
                    );");

                RunNonQuery(connection, @"
                    INSERT INTO dbo.DashboardSettings
                        (Id, MinTemp, MaxTemp, GraphCount, DefaultGranularity)
                    VALUES (1, 5, 30, 3, 'Monthly');"); // Updated GraphCount default to 3 for extra stuff, can be changed.
            }

            // 5. Create DashboardLog table
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
                        ViewedBy NVARCHAR(255) NOT NULL,
                        ViewedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        Location NVARCHAR(255) NOT NULL,
                        Granularity NVARCHAR(255) NOT NULL,
                        GraphCount INT NOT NULL,
                        AvgTemp FLOAT NOT NULL
                    );"
                );
            }

            SeedSampleData(connection);
        }

        private static void RunNonQuery(SqlConnection connection, string sql)
        {
            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private static bool RunScalarBool(SqlConnection connection, string sql, bool checkNotNull = false)
        {
            using var command = new SqlCommand(sql, connection);
            object result = command.ExecuteScalar();

            if (checkNotNull)
            {
                return result != null && result != DBNull.Value;
            }

            return Convert.ToInt32(result) > 0;
        }

        private static void SeedSampleData(SqlConnection connection)
        {
            bool dataHasRows = RunScalarBool(connection, "SELECT COUNT(*) FROM dbo.Data;");
            if (dataHasRows) return;

            bool locationsHaveRows = RunScalarBool(connection, "SELECT COUNT(*) FROM dbo.Locations;");
            if (!locationsHaveRows)
            {
                RunNonQuery(connection, @"
                    INSERT INTO dbo.Locations (Floor, Room)
                    VALUES 
                            (1, 1),
                            (1, 2),
                            (1, 3),
                            (2, 1),
                            (2, 2),
                            (2, 3),
                            (3, 1),
                            (3, 2),
                            (3, 3);");
                // need more locations feel free to add more locations shouldn't break anything
            }

            bool sensorsHaveRows = RunScalarBool(connection, "SELECT COUNT(*) FROM dbo.Sensors;");
            if (!sensorsHaveRows)
            {
                RunNonQuery(connection, @"
                    INSERT INTO dbo.Sensors (Date_Installed, Make, Model, Location_Id, Variance)
                    VALUES 
                        (GETDATE(), 'Siemens', 'QAA2061', 1, 3),
                        (GETDATE(), 'Bosch', 'BME280', 2, 5),
                        (GETDATE(), 'Texas Instruments', 'TMP36', 3, 4),
                        (GETDATE(), 'Siemens', 'QAA2061', 4, -5),
                        (GETDATE(), 'Bosch', 'BME280', 5, -7),
                        (GETDATE(), 'Siemens', 'QAA2061', 6, -8),
                        (GETDATE(), 'Texas Instruments', 'TMP36', 7, 9),
                        (GETDATE(), 'Sensirion', 'SHT31', 8, -10),
                        (GETDATE(), 'Honeywell', 'HTP-1000', 9, 8);
                ");
                // need more sensors feel free to add more sensors shoudn't break anything
            }

            RandomTemp random = new RandomTemp();
            foreach (SensorRecord sensor in SensorsDatabase.GetAllSensors())
            {
                Debug.WriteLine($"{sensor.Id}, {sensor.Room}, {sensor.Variance}");
                int n = sensor.Variance;
                int t = 24; // number of hours to pre-seed database.
                             // Default 24 (1 day)
                             // 168 == 1 week
                             // 720 == 1 month

                using var command = new SqlCommand(
                    @"INSERT INTO dbo.Data (Timestamp, Temperature, Sensor_Id) 
                    VALUES (DATEADD(hour, @hours, GETDATE()), @temperature, @sensorId);", connection);

                command.Parameters.Add("@hours", System.Data.SqlDbType.Int);
                command.Parameters.Add("@temperature", System.Data.SqlDbType.Decimal);
                command.Parameters.Add("@sensorId", System.Data.SqlDbType.Int);

                command.Parameters["@temperature"].Precision = 5;
                command.Parameters["@temperature"].Scale = 2;

                for (int i = 1; i <= t; i++)
                {
                    double temp = random.randomTemp(i, sensor.Variance);
                    Debug.WriteLine($"{i}, {sensor.Variance}, {temp}");

                    command.Parameters["@hours"].Value = i; 
                    command.Parameters["@temperature"].Value = temp;
                    command.Parameters["@sensorId"].Value = sensor.Id;
                    command.ExecuteNonQuery();
                }
            }
        }//If you want to test new location just add more data after the final year stuff

        public static List<LocationRecord> GetAllLocations()
        {
            List<LocationRecord> locations = new List<LocationRecord>();
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand("SELECT Id, Floor, Room FROM dbo.Locations ORDER BY Floor, Room;", connection);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                LocationRecord location = new LocationRecord
                {
                    Id = reader.GetInt32(0),
                    Floor = reader.GetInt32(1),
                    Room = reader.GetInt32(2)
                };
                location.Display = location.Room + ", " + location.Floor;
                locations.Add(location);
            }
            return locations;
        }

        public static List<SensorRecord> GetAllSensors()
        {
            List<SensorRecord> sensors = new List<SensorRecord>();
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                SELECT s.Id, s.Make, s.Model, l.Floor, l.Room, s.Variance, s.Active
                    FROM dbo.Sensors s
                    INNER JOIN dbo.Locations l ON l.Id = s.Location_Id
                    ORDER BY l.Floor, l.Room, s.Id;"
                , connection);

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                sensors.Add(new SensorRecord
                {
                    Id = reader.GetInt32(0),
                    Make = reader.GetString(1),
                    Model = reader.GetString(2),
                    Floor = reader.GetInt32(3),
                    Room = reader.GetInt32(4),
                    Variance = reader.GetInt32(5),
                    Active = reader.GetBoolean(6)   
                });
            }
            return sensors;
        }

        public static List<ReadingAggregate> GetAggregates(int locationId, Granularity granularity)
        {
            string bucket = granularity switch
            {
                Granularity.Daily => "DATEADD(hour, DATEDIFF(hour, 0, d.[Timestamp]), 0)",
                Granularity.Weekly => "DATEADD(day, DATEDIFF(day, 0, d.[Timestamp]), 0)",
                Granularity.Monthly => "DATEADD(month, DATEDIFF(month, 0, d.[Timestamp]), 0)",
                _                   => "DATEADD(year, DATEDIFF(year, 0, d.[Timestamp]), 0)"
            };

            string sql = $@"
                SELECT {bucket} AS PeriodStart,
                       AVG(CAST(d.Temperature AS FLOAT)), 
                       MIN(CAST(d.Temperature AS FLOAT)), 
                       MAX(CAST(d.Temperature AS FLOAT)), 
                       COUNT(*)
                    FROM dbo.Data d
                    INNER JOIN dbo.Sensors s ON s.Id = d.Sensor_Id
                    WHERE s.Location_Id = @locationId
                    GROUP BY {bucket}
                    ORDER BY 1;";

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

        private static string FormatPeriod(DateTime start, Granularity granularity)
        {
            return granularity switch
            {
                Granularity.Daily => start.ToString("dd MMM HH:00"),
                Granularity.Weekly => start.ToString("dd MMM yy"),
                Granularity.Monthly => start.ToString("MMM yyyy"),
                _ => start.ToString("yyyy")
            };
        }

        public static DashboardSettings GetSettings()
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                SELECT MinTemp, MaxTemp, GraphCount, DefaultGranularity, UpdatedBy, UpdatedAt
                FROM dbo.DashboardSettings WHERE Id = 1;", connection);

            using SqlDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return new DashboardSettings
                {
                    MinTemp = 5,
                    MaxTemp = 30,
                    GraphCount = 3,
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

        public static void SaveSettings(DashboardSettings settings, string updatedBy)
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                UPDATE dbo.DashboardSettings
                SET MinTemp = @min, MaxTemp = @max, GraphCount = @graphs,
                    DefaultGranularity = @gran, UpdatedBy = @by, UpdatedAt = GETDATE()
                WHERE Id = 1;", connection);

            command.Parameters.AddWithValue("@min", settings.MinTemp);
            command.Parameters.AddWithValue("@max", settings.MaxTemp);
            command.Parameters.AddWithValue("@graphs", settings.GraphCount);
            command.Parameters.AddWithValue("@gran", settings.DefaultGranularity);
            command.Parameters.AddWithValue("@by", (object)updatedBy ?? DBNull.Value);

            command.ExecuteNonQuery();
        }
        public static double? GetLatestTemp(int locationId)
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
        SELECT TOP (1) CAST(d.Temperature AS FLOAT)
        FROM dbo.Data d
        INNER JOIN dbo.Sensors s ON s.Id = d.Sensor_Id
        WHERE s.Location_Id = @locationId
        ORDER BY d.[Timestamp] DESC;", connection);

            command.Parameters.AddWithValue("@locationId", locationId);
            object result = command.ExecuteScalar();
            return result == null ? null : Convert.ToDouble(result);
        }
        public static void RecordDashboard(DashboardSnapshot snapshot)
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                INSERT INTO dbo.DashboardLog (ViewedBy, Location, Granularity, GraphCount, AvgTemp)
                VALUES (@by, @loc, @gran, @graphs, @avg);", connection);

            command.Parameters.AddWithValue("@by", snapshot.ViewedBy);
            command.Parameters.AddWithValue("@loc", snapshot.Location);
            command.Parameters.AddWithValue("@gran", snapshot.Granularity);
            command.Parameters.AddWithValue("@graphs", snapshot.GraphCount);
            command.Parameters.AddWithValue("@avg", snapshot.AvgTemp);

            command.ExecuteNonQuery();
        }

        public static List<DashboardSnapshot> GetDashboardLog(int take = 200)
        {
            List<DashboardSnapshot> log = new List<DashboardSnapshot>();
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                SELECT TOP (@take) ViewedBy, ViewedAt, Location, Granularity, GraphCount, AvgTemp
                FROM dbo.DashboardLog ORDER BY ViewedAt DESC;", connection);

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
                    AvgTemp = Math.Round(reader.GetDouble(5), 2)
                });
            }
            return log;
        }

        // Gets chart readings to populate ChartTile graph
        // uses a switch for the granularity
        public static List<(DateTime Period, double Temperature)> GetChartReadings(
            int locationId,
            Granularity granularity)
        {
            int hours = granularity switch
            {
                Granularity.Daily => 24,
                Granularity.Weekly => 168,
                Granularity.Monthly => 720,
                Granularity.Yearly => 8760,
                _ => throw new ArgumentOutOfRangeException(nameof(granularity))
            };

            string query = @"
                SELECT
                    DATEADD(HOUR, DATEDIFF(HOUR, 0, d.Timestamp), 0) AS Period,
                    CAST(AVG(d.Temperature) AS DECIMAL(5, 2)) AS Temperature
                FROM dbo.Data d
                INNER JOIN dbo.Sensors s ON d.Sensor_id = s.Id
                INNER JOIN dbo.Locations l ON s.Location_id = l.Id
                WHERE l.Id = @LocationId
                    AND d.Timestamp >= DATEADD(HOUR, -@Hours, (SELECT MAX(Timestamp) FROM dbo.Data))
                GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, d.Timestamp), 0)
                ORDER BY Period;";

            using SqlConnection connection = UserDatabase.OpenConnection();
            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@LocationId", locationId);
            command.Parameters.AddWithValue("@Hours", hours);
            // @LocationId and @Hours are parameters for parameterised query
            // reduces SQL SELECT code to just one statement, with Granularity & location as input

            List<(DateTime Period, double Temperature)> readings = new List<(DateTime, double)>();

            using SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                readings.Add((reader.GetDateTime(0), Convert.ToDouble(reader.GetValue(1))));
            }

            return readings;
        }
    }
}