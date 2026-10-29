using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Windows.Controls;
using System.Xaml.Schema;

namespace Assign_2
{
    /// <summary>
    /// Handles sensor records, temperature readings and dashboard data.
    /// </summary>
    public static class SensorsDatabase
    {
        /// <summary>
        /// Sets the number of minutes of sample data to generate.
        /// </summary>
        public static int SeedMinutes = 1440; // number of minutes to pre-seed database.
                                              // 60 = 1 hour
                                              // 1440 = 1 day

        /// <summary>
        /// Creates missing sensor tables and adds initial sample data.
        /// </summary>
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
                        DefaultGranularity NVARCHAR(255) NOT NULL,
                        ShowCurrentTemp BIT NOT NULL DEFAULT 1,
                        ShowTempComparison BIT NOT NULL DEFAULT 1,
                        ShowTempReading BIT NOT NULL DEFAULT 1,
                        UpdatedBy NVARCHAR(255) NULL,
                        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        Tick INT
                    );");
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
                        AvgTemp FLOAT NOT NULL
                    );"
                );
            }

            SeedSampleData(connection);
        }

        /// <summary>
        /// Adds a sensor and creates its location if needed.
        /// </summary>
        /// <param name="make">New sensor make</param>
        /// <param name="model">New sensor model</param>
        /// <param name="floor">Assigned floor</param>
        /// <param name="room">Assigned room</param>
        /// <param name="active">New sensor activity</param>
        /// <param name="variance">New sensor variance</param>
        /// <returns>Sensor creation confirmation</returns>
        /// <exception cref="Exception">Sensor creation failure</exception>
        public static bool Register(
            string make,
            string model,
            int floor,
            int room,
            bool active,
            int variance = 0)
        {
            using var connection = UserDatabase.OpenConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Find or create the location
                int locationId;

                using var locCheckCommand = new SqlCommand(
                    @"SELECT Id FROM dbo.Locations
              WHERE Floor = @floor AND Room = @room;",
                    connection,
                    transaction);

                locCheckCommand.Parameters.AddWithValue("@floor", floor);
                locCheckCommand.Parameters.AddWithValue("@room", room);

                object? locResult = locCheckCommand.ExecuteScalar();

                if (locResult != null && locResult != DBNull.Value)
                {
                    // Use the existing location
                    locationId = Convert.ToInt32(locResult);
                }
                else
                {
                    // Create a new location
                    using var insertLocation = new SqlCommand(
                        @"INSERT INTO dbo.Locations (Floor, Room)
                  OUTPUT INSERTED.Id
                  VALUES (@floor, @room);",
                        connection,
                        transaction);

                    insertLocation.Parameters.AddWithValue("@floor", floor);
                    insertLocation.Parameters.AddWithValue("@room", room);

                    locationId = Convert.ToInt32(insertLocation.ExecuteScalar());
                }

                // Check whether the room already has a sensor
                using var sensorCheck = new SqlCommand(
                    @"SELECT COUNT(*)
                    FROM dbo.Sensors
                    WHERE Location_Id = @locationId;",
                    connection,
                    transaction);

                sensorCheck.Parameters.AddWithValue("@locationId", locationId);

                int sensorCount = Convert.ToInt32(sensorCheck.ExecuteScalar());

                if (sensorCount > 0)
                {
                    throw new Exception(
                        $"Floor {floor}, Room {room} already has a sensor.");
                }

                // 2. Register the sensor
                using var command = new SqlCommand(
                    @"INSERT INTO dbo.Sensors
              (
                  Date_Installed,
                  Make,
                  Model,
                  Variance,
                  Location_Id,
                  Active
              )
              VALUES
              (
                  @date_installed,
                  @make,
                  @model,
                  @variance,
                  @locationId,
                  @active
              );",
                    connection,
                    transaction);

                command.Parameters.AddWithValue(
                    "@date_installed", DateTime.Now.Date);

                command.Parameters.AddWithValue("@make", make);
                command.Parameters.AddWithValue("@model", model);
                command.Parameters.AddWithValue("@variance", variance);
                command.Parameters.AddWithValue("@locationId", locationId);
                command.Parameters.AddWithValue("@active", active);

                command.ExecuteNonQuery();

                // 3. Save both changes
                transaction.Commit();
                return true;
            }
            catch (SqlException ex)
            {
                // Undo any changes made during registration
                transaction.Rollback();

                throw new Exception("Error registering sensor.", ex);
            }
            catch
            {
                // Also roll back any other errors
                transaction.Rollback();
                throw;
            }
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
                //Sensor 9 is deactivated to show how activation works.
                RunNonQuery(connection, @"
                    INSERT INTO dbo.Sensors (Date_Installed, Make, Model, Location_Id, Variance, Active)
                    VALUES 
                        (GETDATE(), 'Siemens', 'QAA2061', 1, 3, 1),
                        (GETDATE(), 'Bosch', 'BME280', 2, 5, 1),
                        (GETDATE(), 'Texas Instruments', 'TMP36', 3, 4, 1),
                        (GETDATE(), 'Siemens', 'QAA2061', 4, -5, 0),
                        (GETDATE(), 'Bosch', 'BME280', 5, -7, 0),
                        (GETDATE(), 'Siemens', 'QAA2061', 6, -8, 0),
                        (GETDATE(), 'Texas Instruments', 'TMP36', 7, 9, 0),
                        (GETDATE(), 'Sensirion', 'SHT31', 8, -10, 0),
                        (GETDATE(), 'Honeywell', 'HTP-1000', 9, 8, 0);
                ");
            }
            bool settingsHaveRows = RunScalarBool(connection, "SELECT COUNT(*) FROM dbo.DashboardSettings;"); 
            if (!settingsHaveRows)
            {
                using var command = new SqlCommand(@"
                    INSERT INTO dbo.DashboardSettings (Id, DefaultGranularity, ShowCurrentTemp, ShowTempComparison, ShowTempReading,Tick)
                    VALUES (1, 'Monthly', 1, 1, 1, @SeededMinutes);", connection);
                command.Parameters.Add("@SeededMinutes", System.Data.SqlDbType.Int);
                command.Parameters["@SeededMinutes"].Value = SeedMinutes;

                command.ExecuteNonQuery();
            }

            RandomTemp random = new RandomTemp();
            
            foreach (SensorRecord sensor in SensorsDatabase.GetAllSensors())
            {
                if (!sensor.Active)
                {
                    continue;
                }
                Debug.WriteLine($"{ sensor.Id}, {sensor.Room}, {sensor.Variance}");
                int n = sensor.Variance;
                

                using var command = new SqlCommand(
                    @"INSERT INTO dbo.Data (Timestamp, Temperature, Sensor_Id) 
                    VALUES (DATEADD(minute, @minute, DATEADD(minute, -@SeededMinutes, GETDATE())) , @temperature, @sensorId);", connection);

                command.Parameters.Add("@minute", System.Data.SqlDbType.Int);
                command.Parameters.Add("@temperature", System.Data.SqlDbType.Decimal);
                command.Parameters.Add("@sensorId", System.Data.SqlDbType.Int);
                command.Parameters.AddWithValue("@SeededMinutes", SeedMinutes);

                command.Parameters["@temperature"].Precision = 5;
                command.Parameters["@temperature"].Scale = 2;

                for (int i = 0; i < SeedMinutes; i++)
                {
                    double temp = random.randomTemp(i, sensor.Variance);
                    Debug.WriteLine($"{i}, {sensor.Variance}, {temp}");

                    command.Parameters["@minute"].Value = i; 
                    command.Parameters["@temperature"].Value = temp;
                    command.Parameters["@sensorId"].Value = sensor.Id;
                    command.ExecuteNonQuery();
                }
            }
        }//If you want to test new location just add more data after the final year stuff
        
        /// <summary>
        /// Gets all available floor and room locations.
        /// </summary>
        /// <returns>list of locations</returns>
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
        
        /// <summary>
        /// Gets all sensors along with their location details.
        /// </summary>
        /// <returns>list of all sensors and information </returns>
        public static List<SensorRecord> GetAllSensors()
        {
            List<SensorRecord> sensors = new List<SensorRecord>();
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                SELECT s.Id, s.Make, s.Model, l.Floor, l.Room, s.Variance, s.Active
                    FROM dbo.Sensors s
                    INNER JOIN dbo.Locations l ON l.Id = s.Location_Id
                    ORDER BY s.Id;"
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
        
        /// <summary>
        /// Updates the make and model of an existing sensor.
        /// </summary>
        /// <param name="sensorId">Id of sensor to update</param>
        /// <param name="make">New make string</param>
        /// <param name="model">New model string</param>
        public static void UpdateSensor(int sensorId, string make, string model)
        {
            using var connection = UserDatabase.OpenConnection();

            using var command = new SqlCommand(@"
                UPDATE dbo.Sensors
                SET Make = @Make,
                    Model = @Model
                WHERE Id = @Id;", connection);

            command.Parameters.AddWithValue("@Make", make);
            command.Parameters.AddWithValue("@Model", model);
            command.Parameters.AddWithValue("@Id", sensorId);

            command.ExecuteNonQuery();
        }
       
        /// <summary>
        /// Gets grouped temperature statistics for the selected locations.
        /// </summary>
        /// <param name="locations">list of locations.</param>
        /// <param name="granularity">Time interval used.</param>
        /// <returns> A list of readings aggregates.</returns>
        public static List<ReadingAggregate> GetAggregates(List<int> locations, Granularity granularity)
        {
            List<ReadingAggregate> rows = new List<ReadingAggregate>();
            foreach (int location in locations)
            {
                string bucket = granularity switch
                {
                    Granularity.Daily => "DATEADD(hour, DATEDIFF(hour, 0, d.[Timestamp]), 0)",
                    Granularity.Weekly => "DATEADD(day, DATEDIFF(day, 0, d.[Timestamp]), 0)",
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

                
                using var connection = UserDatabase.OpenConnection();
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@locationId", location);

                using SqlDataReader reader = command.ExecuteReader();


                while (reader.Read())
                {
                    DateTime start = reader.GetDateTime(0);
                    rows.Add(new ReadingAggregate
                    {
                        Location = location,
                        Period = FormatPeriod(start, granularity),
                        AvgTemp = Math.Round(reader.GetDouble(1), 2),
                        MinTemp = Math.Round(reader.GetDouble(2), 2),
                        MaxTemp = Math.Round(reader.GetDouble(3), 2),
                        Samples = reader.GetInt32(4)
                    });
                }
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
       
        /// <summary>
        /// Gets the saved dashboard settings or the default values.
        /// </summary>
        /// <returns>All Dashboard settings</returns>
        public static DashboardSettings GetSettings()
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
        SELECT DefaultGranularity,
               ShowCurrentTemp, ShowTempComparison, ShowTempReading,
               UpdatedBy, UpdatedAt, Tick
        FROM dbo.DashboardSettings
        WHERE Id = 1;", connection);

            using SqlDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return new DashboardSettings
                {
                    DefaultGranularity = "Monthly",
                    ShowCurrentTemp = true,
                    ShowTempComparison = true,
                    ShowTempReading = true,
                    Tick = SeedMinutes

                };
            }

            return new DashboardSettings
            {
                DefaultGranularity = reader.GetString(0),
                ShowCurrentTemp = reader.GetBoolean(1),
                ShowTempComparison = reader.GetBoolean(2),
                ShowTempReading = reader.GetBoolean(3),
                UpdatedBy = reader.IsDBNull(4) ? "" : reader.GetString(4),
                UpdatedAt = reader.GetDateTime(5),
                Tick = reader.GetInt32(6)


            };
        }
       
        /// <summary>
        /// Saves dashboard display settings and who updated them.
        /// </summary>
        /// <param name="settings">Dashboard settings</param>
        /// <param name="updatedBy">user that trigger update</param>
        public static void SaveSettings(DashboardSettings settings, string updatedBy)
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
        UPDATE dbo.DashboardSettings
        SET
            DefaultGranularity = @gran,
            ShowCurrentTemp = @currentTemp,
            ShowTempComparison = @comparison,
            ShowTempReading = @reading,
            UpdatedBy = @by,
            UpdatedAt = GETDATE()
        WHERE Id = 1;", connection);

            command.Parameters.AddWithValue("@gran", settings.DefaultGranularity);
            command.Parameters.AddWithValue("@currentTemp", settings.ShowCurrentTemp);
            command.Parameters.AddWithValue("@comparison", settings.ShowTempComparison);
            command.Parameters.AddWithValue("@reading", settings.ShowTempReading);
            command.Parameters.AddWithValue("@by", (object)updatedBy ?? DBNull.Value);

            command.ExecuteNonQuery();
        }
        
        /// <summary>
        /// Gets the latest recorded temperature for a location.
        /// </summary>
        /// <param name="locationId">location's Id</param>
        /// <returns>Latest temperature record, or null if no readings exist.</returns>
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
        
        /// <summary>
        /// Adds a dashboard viewing record to the log.
        /// </summary>
        /// <param name="snapshot">list of particular information from opening the user dashboard</param>
        public static void RecordDashboard(DashboardSnapshot snapshot)
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                INSERT INTO dbo.DashboardLog (ViewedBy, Location, Granularity, AvgTemp)
                VALUES (@by, @loc, @gran, @avg);", connection);

            command.Parameters.AddWithValue("@by", snapshot.ViewedBy);
            command.Parameters.AddWithValue("@loc", snapshot.Location);
            command.Parameters.AddWithValue("@gran", snapshot.Granularity);
            command.Parameters.AddWithValue("@avg", snapshot.AvgTemp);

            command.ExecuteNonQuery();
        }
        
        /// <summary>
        /// Gets the most recent dashboard viewing records.
        /// </summary>
        /// <param name="take">max amount of entries returned.</param>
        /// <returns>List of dashboard log</returns>
        public static List<DashboardSnapshot> GetDashboardLog(int take = 200)
        {
            List<DashboardSnapshot> log = new List<DashboardSnapshot>();
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                SELECT TOP (@take) ViewedBy, ViewedAt, Location, Granularity, AvgTemp
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
                    AvgTemp = Math.Round(reader.GetDouble(4), 2)
                });
            }
            return log;
        }

        /// <summary>
        /// Gets temperature readings for a location and time interval.
        /// </summary>
        /// <param name="location">location id</param>
        /// <param name="granularity">Time frame for data.</param>
        /// <returns>List of timestamps and their average temp.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when granularity value is not supported</exception>
        public static List<(DateTime Period, double Temperature)> GetChartReadings(
            int location,
            Granularity granularity)
        {
            int minutes = granularity switch
            {
                Granularity.Daily => 1440,
                Granularity.Weekly => 13440,
                Granularity.Monthly => 43200,
                Granularity.Yearly => 525600,
                _ => throw new ArgumentOutOfRangeException(nameof(granularity))
            };

            string query = @"
                SELECT TOP(@Minutes)
                    DATEADD(MINUTE, DATEDIFF(MINUTE, 0, d.Timestamp), 0) AS Period,
                    CAST(AVG(d.Temperature) AS DECIMAL(5, 2)) AS Temperature
                FROM dbo.Data d
                INNER JOIN dbo.Sensors s ON d.Sensor_id = s.Id
                INNER JOIN dbo.Locations l ON s.Location_id = l.Id
                WHERE l.Id = @LocationId
                    AND d.Timestamp >= DATEADD(MINUTE, -@Minutes, (SELECT MAX(Timestamp) FROM dbo.Data))
                GROUP BY DATEADD(MINUTE, DATEDIFF(MINUTE, 0, d.Timestamp), 0)
                ORDER BY Period;";

            using SqlConnection connection = UserDatabase.OpenConnection();
            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@LocationId", location);
            command.Parameters.AddWithValue("@Minutes", minutes);

            List<(DateTime Period, double Temperature)> readings = new List<(DateTime, double)>();

            using SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                readings.Add((reader.GetDateTime(0), Convert.ToDouble(reader.GetValue(1))));
            }

            return readings;
        }
        
        /// <summary>
        /// Updates the temperature generation interval setting.
        /// </summary>
        /// <param name="settings">Updated tick value from temperature generation</param>
        public static void UpdateSettings(DashboardSettings settings)
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                UPDATE dbo.DashboardSettings
                SET 
                    Tick = @tick
                WHERE Id = 1;", connection);
            command.Parameters.AddWithValue("@tick", settings.Tick);
            command.ExecuteNonQuery();
        }
        
        /// <summary>
        /// Changes whether a sensor is active.
        /// </summary>
        /// <param name="newStatus">Activity status</param>
        /// <param name="sensorId">Sensor Id</param>
        public static void UpdateActivity(bool newStatus, int sensorId)
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                UPDATE dbo.Sensors
                SET Active = @Activity
                WHERE Id = @Id", connection);

            command.Parameters.AddWithValue("@Activity", newStatus);
            command.Parameters.AddWithValue("@Id", sensorId);
            command.ExecuteNonQuery();

        }
        
        /// <summary>
        /// Checks whether a location has an active sensor.
        /// </summary>
        /// <param name="locationId">location id</param>
        /// <returns>true or false if location has sensors that are active</returns>
        public static bool IsLocationActive(int locationId)
        {
            using var connection = UserDatabase.OpenConnection();
            using var command = new SqlCommand(@"
                SELECT  COUNT(*)
                FROM dbo.Sensors
                WHERE Location_Id = @Location_Id
                AND Active = 1", connection);

            command.Parameters.AddWithValue("@Location_Id", locationId);

            int count = (int)command.ExecuteScalar();
            
            return count > 0;
        }
    }
}