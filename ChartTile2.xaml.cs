using System;
using System.Windows.Controls;

namespace Assign_2
{
    public partial class ChartTile2 : UserControl
    {
        // Maximum available height in pixels inside the canvas container
        private const double MaxBarHeight = 104.0;

        public ChartTile2()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Updates the thermometer liquid level and the displayed text.
        /// </summary>
        /// <param name="currentTemp">The current temperature to display.</param>
        /// <param name="minTemp">Minimum expected range bound (e.g., 0°C).</param>
        /// <param name="maxTemp">Maximum expected range bound (e.g., 50°C).</param>
        public void UpdateValue(double currentTemp, double minTemp = 0, double maxTemp = 50)
        {
            // 1. Update text value display
            CurrentValueText.Text = Math.Round(currentTemp, 1).ToString("F1");

            // 2. Calculate percentage fill based on range
            double range = maxTemp - minTemp;
            if (range <= 0) range = 1; // Prevent division by zero

            double fillPercentage = (currentTemp - minTemp) / range;

            // Clamp percentage between 0% and 100%
            fillPercentage = Math.Clamp(fillPercentage, 0.0, 1.0);

            // 3. Set bar height and calculate Y position from top
            double calculatedHeight = MaxBarHeight * fillPercentage;

            TempBar.Height = calculatedHeight;

            // Push the rectangle top down so it grows upwards from the bulb
            Canvas.SetTop(TempBar, MaxBarHeight - calculatedHeight);
        }
    }
}