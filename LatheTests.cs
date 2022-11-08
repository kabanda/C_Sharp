using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Point = System.Drawing.Point;

namespace ns_main
{
    public partial class MainWindow : Window
    {
        public async Task testPlottingALine()
        {
#pragma warning disable 1998
            await Task.Run(async () =>
#pragma warning restore 1998
            {
                ChangeTabTo(Plotting_TabItem);
                List<Point> pointList = new List<Point>();

                for (int i = 0; i < 500; i++)
                {
                    Point point = new Point();
                    point.X = i;
                    point.Y = i;
                    pointList.Add(point);
                }
            });

            //PlotLine(pointList, Color.Brown);
        }
    }
}