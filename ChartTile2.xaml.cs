using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
namespace Assign_2
{
    public partial class ChartTile2 : UserControl
    {
        public ChartTile2()
        {
            InitializeComponent();
        }

        public void Refresh(int locationId)
        {
            double? temp = SensorsDatabase.GetLatestTemp(locationId);
            CurrentValueText.Text = temp?.ToString("0.0") ?? "--";
        }
    }
}
