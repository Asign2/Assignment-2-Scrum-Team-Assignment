using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assign_2
{
    internal class RandomTemp
    {
        /// <summary>
        /// random temperature generator for testing the graphing functions.
        /// </summary>
        /// <param name="x">time frame for the temperatures.</param>
        /// <returns>double for temp.</returns>
        public double randomTemp(int x)
        {
            //variables to edit the graph.
            double average = 18;
            double dayNight = 7;
            double hour = 1.5;
            double seasons = 10;
            double pi = Math.PI;

            //24 value to match day
            double sin1 = Math.Sin(((2 * pi) / 24) * x);
            //6 value to match 6 hours
            double sin2 = Math.Sin(((2 * pi) / 6) * x);
            //8760 value to match the year/seasons
            double sin3 = Math.Sin(((2 * pi) / 8760) * x);

            double temp = average + (dayNight * sin1) + (hour * sin2) + (seasons * sin3);
            //Stop loop for 20 seconds
            //Thread.Sleep(20000);
            return temp;
        }
    }
}
