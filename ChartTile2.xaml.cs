using System;
using System.Windows.Controls;

namespace Assign_2
{
    /// <summary>
    /// Displays the current temperature as a thermometer chart.
    /// </summary>
    public partial class ChartTile2 : UserControl
    {
        // Maximum available height in pixels inside the canvas container
        private const double MaxBarHeight = 104.0;
        /// <summary>
        /// Creates and sets up a thermometer chart.
        /// </summary>
        public ChartTile2()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the temperature currently displayed by the chart.
        /// </summary>
        /// <param name="currentTemp">The current temperature to display.</param>
        /// <param name="minTemp">Minimum expected range bound (e.g., 0°C).</param>
        /// <param name="maxTemp">Maximum expected range bound (e.g., 50°C).</param>
        public double CurrentTemp { get; private set; }

        /// <summary>
        /// Updates the displayed temperature and bar height.
        /// </summary>
        /// <param name="currentTemp">Current temp for a location</param>
        /// <param name="minTemp">Min temp for bar</param>
        /// <param name="maxTemp">Max temp for bar</param>
        public void UpdateValue(double currentTemp, double minTemp = 0, double maxTemp = 50)
        {
            CurrentTemp = currentTemp;

            // Update text value display
            CurrentValueText.Text = Math.Round(currentTemp, 1).ToString("F1");

            // Calculate percentage fill based on range.
            double range = maxTemp - minTemp;
            if (range <= 0) range = 1;
            //How much the bar should be filled up.
            double fillPercentage = (currentTemp - minTemp) / range;
            fillPercentage = Math.Clamp(fillPercentage, 0.0, 1.0);
            //How much the graph can be filled up.
            double calculatedHeight = MaxBarHeight * fillPercentage;
            TempBar.Height = calculatedHeight;
            Canvas.SetTop(TempBar, MaxBarHeight - calculatedHeight);
        }
        /// <summary>
        /// Updates the thermometer status based on sensor activity.
        /// </summary>
        /// <param name="isActive">Status of location sensor's</param>
        public void UpdateStatus(bool isActive)
        {
            Activated.Visibility = System.Windows.Visibility.Visible;
            Deactivated.Visibility = System.Windows.Visibility.Collapsed;

            if (!isActive)
            {
                Activated.Visibility = System.Windows.Visibility.Collapsed;
                Deactivated.Visibility = System.Windows.Visibility.Visible;
            }
        }
    }
}