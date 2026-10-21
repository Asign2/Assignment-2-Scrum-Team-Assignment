using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assign_2
{
    internal class Generation 
    {
        private MainWindow mainWindow;

        public Generation(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        /// <summary>
        /// Periodically generates temperature readings for all sensors in the database and inserts them into the Database.
        /// Ticks every 20 seconds.
        /// Each tick is an hour.
        /// <param name="time">The time the database is up to from seeded data.</param>
        /// </summary>
        public async Task generatePeriodicTemperature(int time)
        {
            using var connection = UserDatabase.OpenConnection();

            RandomTemp random = new RandomTemp();
            while (true)
            {

                await Task.Delay(20000); // Wait for 20 seconds before generating the next set of readings
                foreach (SensorRecord sensor in SensorsDatabase.GetAllSensors())
                {
                    Debug.WriteLine($"{sensor.Id}, {sensor.Room}, {sensor.Variance}");

                    double temp = random.randomTemp(time, sensor.Room);

                    using var command = new SqlCommand(
                        @"INSERT INTO dbo.Data (Timestamp, Temperature, Sensor_Id) 
                        VALUES (
                            DATEADD(hour, 1, (SELECT MAX(Timestamp) FROM dbo.Data)),
                            @temperature,
                            @sensorId
                        );", connection);

                    command.Parameters.Add("@temperature", System.Data.SqlDbType.Decimal).Value = temp;
                    command.Parameters.Add("@sensorId", System.Data.SqlDbType.Int).Value = sensor.Id;

                    command.Parameters["@temperature"].Precision = 5;
                    command.Parameters["@temperature"].Scale = 2;

                    command.ExecuteNonQuery();
                }

                time++;

                mainWindow.Dispatcher.Invoke(() =>
                {
                    mainWindow.RefreshDashboard();
                });

            }
        }
    }
}
