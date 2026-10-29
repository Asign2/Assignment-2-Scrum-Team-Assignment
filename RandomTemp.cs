using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assign_2
{
    /// <summary>
    /// Generates sample temperatures for testing and graph displays.
    /// </summary>
    public class RandomTemp
    {

        /// <summary>
        /// Generates a temperature using the time value and variance.
        /// </summary>
        /// <param name="x">time frame for the temperatures.</param>
        /// <param name="n">additional parameter to modify the temperature.</param>
        /// <returns>temperature.</returns>
        public double randomTemp(int x, int n)
        {
            //variables to edit the graph.
            int average = 19;
            double pi = Math.PI;
            // sin functions to create a more complex temperature pattern
            double sin1 = Math.Sin((2 * pi * (x - 8))/ 24 + 0.1 * n);
            double sin2 = Math.Sin((4 * pi * (x + 10))/ 24 + 0.1 * n);
            double sin3 = Math.Sin((2 * pi * x)/24);
            double sin4 = Math.Sin((2 * pi * x)/7 + (0.8 * sin3));

            // Calculate the temperature using the modified sine functions and the additional parameter n
            double temp = average + (3.2 * sin1) + (0.9 * sin2) + (0.5 * sin4) + 0.3 * n;
            return temp;
        }
    }
}
