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
        //public double OutsideTemp(int x)
        //{
        //    //variables to edit the graph.
        //    double average = 18;
        //    double dayNight = 7;
        //    double hour = 1.5;
        //    double seasons = 10;
        //    double pi = Math.PI;

        //    //24 value to match day
        //    double sin1 = Math.Sin(((2 * pi) / 24) * x);
        //    //6 value to match 6 hours
        //    double sin2 = Math.Sin(((2 * pi) / 6) * x);
        //    //8760 value to match the year/seasons
        //    double sin3 = Math.Sin(((2 * pi) / 8760) * x);

        //    double temp = average + (dayNight * sin1) + (hour * sin2) + (seasons * sin3);
        //    //Stop loop for 20 seconds
        //    //Thread.Sleep(20000);
        //    return temp;
        //}

        /// <summary>
        /// random temperature generator for testing the graphing functions.
        /// </summary>
        /// <param name="x">time frame for the temperatures.</param>
        /// <param name="n">additional parameter for variation.</param>
        /// <returns>double for temp.</returns>
        public double InsideTemp(int x,int n)
        {
            //variables to edit the graph.
            double average = 19;

            //Sin values to create a more complex temperature pattern.
            double sin1 = Math.Sin(((2 * Math.PI) / 24) +0.1 * n);
            double sin2 = Math.Sin((((4 * Math.PI) * (x + 10)) / 24) + 0.1 * n);
            double sin4 = Math.Sin(((2 * Math.PI) / 24) + 0.3 * n);
            double sin3 = Math.Sin(((2 * Math.PI) / 7) + 0.1 * n + 0.8 *sin4);

            //Calculate the temperature based on the average, the sine values and the variation.
            double temp = average + (3.2 * sin1) + (0.9 * sin2) + (0.5 * sin3) + 0.3 * n;
            //Stop loop for 20 seconds
            //Thread.Sleep(20000);
            return temp;
        }
    }
}
