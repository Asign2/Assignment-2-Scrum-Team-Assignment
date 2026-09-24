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
                        City VARCHAR(255) NOT NULL,
                        Suburb VARCHAR(255) NOT NULL
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
                        Location_Id INT NOT NULL,

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
                        Buckets INT NOT NULL,
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
                    INSERT INTO dbo.Locations (City, Suburb)
                    VALUES 
                            ('Palmerston North', 'Hokowhitu'),
                            ('Auckland', 'Ponsonby');");
                // need more locations feel free to add more locations shouldn't break anything
            }

            bool sensorsHaveRows = RunScalarBool(connection, "SELECT COUNT(*) FROM dbo.Sensors;");
            if (!sensorsHaveRows)
            {
                RunNonQuery(connection, @"
                    INSERT INTO dbo.Sensors (Date_Installed, Make, Model, Location_Id)
                    VALUES 
                    (GETDATE(), 'Acme', 'TempSense 1', 1),
                    (GETDATE(), 'Acme', 'TempSense 2', 2)");
                // need more sensors feel free to add more sensors shoudn't break anything
            }

            RandomTemp random = new RandomTemp();

            for (int i = 1; i <= 24; i++) { 
                double temp = random.randomTemp(i, 0);
                
                RunNonQuery(connection,
                    @$"INSERT INTO dbo.Data (Timestamp, Temperature, Sensor_Id) " +
                    @$"VALUES " +
                    @$"(DATEADD(hour, {i}, GETDATE()), {temp}, 1);"
                );
                Debug.WriteLine($"{DateTime.Now}, hour {i}, {temp}"); // Logs sensor readings to Output window
                // refactor Debug.WriteLine when implementing live updates
            }
            // Sensor generation WIP - fixed values to be replaced by a generator
    //        RunNonQuery(connection, @"
    //INSERT INTO dbo.Data (Timestamp, Temperature, Sensor_Id)
    //VALUES
    //(DATEADD(hour, -1, GETDATE(03/06/2026)), 18.20, 1),
    //(DATEADD(hour, -5, GETDATE()), 19.10, 1),
    //(DATEADD(hour, -12, GETDATE()), 16.70, 1),
    //(DATEADD(day, -1, GETDATE()), 17.50, 1),
    //(DATEADD(day, -2, GETDATE()), 20.30, 1),
    //(DATEADD(day, -5, GETDATE()), 14.90, 1),
    //(DATEADD(day, -10, GETDATE()), 15.80, 1),
    //(DATEADD(day, -20, GETDATE()), 21.60, 1),
    //(DATEADD(month, -1, GETDATE()), 22.40, 1),
    //(DATEADD(month, -2, GETDATE()), 13.20, 1),
    //(DATEADD(month, -6, GETDATE()), 25.10, 1),
    //(DATEADD(year, -1, GETDATE()), 11.40, 1),
    //(DATEADD(hour, -2, GETDATE()), 22.50, 2),
    //(DATEADD(hour, -6, GETDATE()), 23.80, 2),
    //(DATEADD(hour, -14, GETDATE()), 21.10, 2),
    //(DATEADD(day, -1, GETDATE()), 24.30, 2),
    //(DATEADD(day, -3, GETDATE()), 26.70, 2),
    //(DATEADD(day, -7, GETDATE()), 19.90, 2),
    //(DATEADD(day, -15, GETDATE()), 20.40, 2),
    //(DATEADD(day, -25, GETDATE()), 27.60, 2),
    //(DATEADD(month, -1, GETDATE()), 25.20, 2),
    //(DATEADD(month, -3, GETDATE()), 18.90, 2),
    //(DATEADD(month, -7, GETDATE()), 28.30, 2),
    //(DATEADD(year, -1, GETDATE()), 16.50, 2);");
        }//If you want to test new location just add more data after the final year stuff

        public static List<LocationRecord> GetAllLocations()
        {
            List<LocationRecord> locations = new List<LocationRecord>();
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand("SELECT Id, City, Suburb FROM dbo.Locations ORDER BY City, Suburb;", connection);
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

        public static List<ReadingAggregate> GetAggregates(int locationId, Granularity granularity)
        {
            string bucket = granularity switch
            {
                Granularity.Hourly => "DATEADD(hour, DATEDIFF(hour, 0, d.[Timestamp]), 0)",
                Granularity.Daily => "DATEADD(day, DATEDIFF(day, 0, d.[Timestamp]), 0)",
                Granularity.Monthly => "DATEADD(month, DATEDIFF(month, 0, d.[Timestamp]), 0)",
                _ => "DATEADD(year, DATEDIFF(year, 0, d.[Timestamp]), 0)"
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
                Granularity.Hourly => start.ToString("dd MMM HH:00"),
                Granularity.Daily => start.ToString("dd MMM yy"),
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

        public static void RecordDashboard(DashboardSnapshot snapshot)
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                INSERT INTO dbo.DashboardLog (ViewedBy, Location, Granularity, GraphCount, Buckets, AvgTemp)
                VALUES (@by, @loc, @gran, @graphs, @buckets, @avg);", connection);

            command.Parameters.AddWithValue("@by", snapshot.ViewedBy);
            command.Parameters.AddWithValue("@loc", snapshot.Location);
            command.Parameters.AddWithValue("@gran", snapshot.Granularity);
            command.Parameters.AddWithValue("@graphs", snapshot.GraphCount);
            command.Parameters.AddWithValue("@buckets", snapshot.Buckets);
            command.Parameters.AddWithValue("@avg", snapshot.AvgTemp);

            command.ExecuteNonQuery();
        }

        public static List<DashboardSnapshot> GetDashboardLog(int take = 200)
        {
            List<DashboardSnapshot> log = new List<DashboardSnapshot>();
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                SELECT TOP (@take) ViewedBy, ViewedAt, Location, Granularity, GraphCount, Buckets, AvgTemp
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
                    Buckets = reader.GetInt32(5),
                    AvgTemp = Math.Round(reader.GetDouble(6), 2)
                });
            }
            return log;
        }
    }
}