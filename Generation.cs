using Microsoft.Data.SqlClient;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assign_2
{
    /// <summary>
    /// Generates periodic temperature readings for active sensors.
    /// </summary>
    public class Generation 
    {

        private readonly int delay;
        /// <summary>
        /// Occurs after new temperature data has been generated.
        /// </summary>
        public event Action? DataGenerated;
        /// <summary>
        /// Creates a temperature generator with the specified delay.
        /// </summary>
        /// <param name="delay"></param>
        public Generation(int delay = 60000)
        {
            this.delay = delay;
        }

        /// <summary>
        /// Periodically generates temperature readings for all sensors in the database and inserts them into the Database.
        /// Ticks every 60 seconds.
        /// Each tick is a minute.
        /// </summary>
        public async Task generatePeriodicTemperature(CancellationToken cancellationToken = default)
        {
            using var connection = UserDatabase.OpenConnection();
            DashboardSettings settings = SensorsDatabase.GetSettings();

            RandomTemp random = new RandomTemp();
            while (true)
            {

                await Task.Delay(delay, cancellationToken); // Wait for 60 seconds before generating the next set of 
                using var command = new SqlCommand(
                   @"INSERT INTO dbo.Data (Timestamp, Temperature, Sensor_Id) 
                        VALUES (
                            @timestamp,
                            @temperature,
                            @sensorId
                        );", connection);
                using var timeStampCommand = new SqlCommand(
                    @"SELECT DATEADD(minute, 1, MAX(Timestamp)) FROM dbo.Data;", connection);
                DateTime timeStamp = (DateTime)timeStampCommand.ExecuteScalar();

                foreach (SensorRecord sensor in SensorsDatabase.GetAllSensors())
                {
                    if (!sensor.Active)
                    {
                        continue;
                    }
                    Debug.WriteLine($"{sensor.Id}, {sensor.Room}, {sensor.Variance}");

                    double temp = random.randomTemp(settings.Tick , sensor.Variance);
                    command.Parameters.Clear();

                    command.Parameters.Add("@timestamp", System.Data.SqlDbType.DateTime).Value = timeStamp;

                    command.Parameters.Add("@temperature", System.Data.SqlDbType.Decimal).Value = temp;
                    command.Parameters["@temperature"].Precision = 5;
                    command.Parameters["@temperature"].Scale = 2;

                    command.Parameters.Add("@sensorId", System.Data.SqlDbType.Int).Value = sensor.Id;

                    command.ExecuteNonQuery();
                }


                settings.Tick++;
                Debug.WriteLine($"Settings Tick: {settings.Tick}");
                SensorsDatabase.UpdateSettings(settings);

                DataGenerated?.Invoke();
            }
        }
    }
}
