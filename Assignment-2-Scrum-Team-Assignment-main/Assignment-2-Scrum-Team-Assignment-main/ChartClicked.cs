using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Assign_2
{
    public class ChartTileClickedEventArgs : EventArgs
    {
        public ImageSource Image { get; }
        public string Title { get; }

        public ChartTileClickedEventArgs(ImageSource image, string title)
        {
            Image = image;
            Title = title;
        }
    }
}
