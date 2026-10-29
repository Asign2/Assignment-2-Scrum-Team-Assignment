using System;
using System.Windows.Controls;

namespace Assign_2
{
    /// <summary>
    /// Represents a custom user control tile that displays a thermometer chart and updates its liquid level dynamically
    /// </summary>
    public partial class ChartTile2 : UserControl
    {
        // Maximum available height in pixels inside the canvas container
        private const double MaxBarHeight = 104.0;
        //Calls the builds of graphs
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
        public double CurrentTemp { get; private set; }

        //Gets the current temp from the database and updates it into the Graph.
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