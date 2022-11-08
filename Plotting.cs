using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ImageClassLibrary;
using Point = System.Drawing.Point;

namespace ns_main
{
    public partial class MainWindow : Window
    {

            //public Bitmap plottingBitmap;
            public Bitmap plottingBitmap;
            public BitmapSource plottingBitmapSource;
            public string plottingFileName;
            public List<List<Point>> plottedLines;


            public async Task Setup_Plotting()
            {
                plottingFileName = @"G:\Backup\Hardware\ns\vs\ImageClassLibrary\1000X500_blank.png ";
                plottingBitmapSource = ImageHandler.GetBitmapSourceFromFile(plottingFileName);
                plottingBitmap = ImageHandler.BitmapFromSource(plottingBitmapSource);
                plottedLines = new List<List<Point>>();
                await ClearPlottingArea();
            }

            public async Task PlotLine(List<Point> points, Color color)
            {
                ChangeTabTo(Plotting_TabItem);
                plottedLines.Add(points);

                for (int i = 0; i < points.Count; i++)
                {
                   
                    await DrawPixel(points[i].X, points[i].Y, color);
                    //setupPlottingCanvas();
                    //From here on just thicken
                    if(points[i].Y < plottingBitmap.Height-1)
                      await DrawPixel(points[i].X, points[i].Y+1, color);
                    //setupPlottingCanvas();
                    await DrawPixel(points[i].X, points[i].Y-1, color);
                    //setupPlottingCanvas();
                    
                    if(points[i].X>0)
                    await DrawPixel(points[i].X-1, points[i].Y, color);
                    //setupPlottingCanvas();
                    await DrawPixel(points[i].X+1, points[i].Y, color);
                    setupPlottingCanvas();
                }

   

            }

            public void setupPlottingCanvas()
            {
                plottingBitmapSource = ImageHandler.BitmapToSource(plottingBitmap);
                PlottingCanvas.Source = plottingBitmapSource;  
            }

            public async Task DrawPixel(int x, int y, Color color)
            {
                await Task.Run( () =>
                {
                    int xx = x;
                    int yy = y;
                
                    if (x >= plottingBitmap.Width)
                        xx=0;
                    if(y >= plottingBitmap.Height)
                        yy = plottingBitmap.Height-1;
                    yy = (int) mapRange(yy, 0, 499, 499, 0);
                    plottingBitmap.SetPixel(xx, yy, color);
                });
            }

            public async Task ErazePlottedLine(int indexOfPlot)
            {
                List<Point> aList = plottedLines[indexOfPlot];
                for (int i = 0; i < aList.Count; i++)
                {
                    await DrawPixel(aList[i].X, aList[i].Y, Color.White);
                } 
                plottingBitmapSource = ImageHandler.BitmapToSource(plottingBitmap);
                PlottingCanvas.Source = plottingBitmapSource;
                plottedLines.RemoveAt(indexOfPlot);
            }

            private async void ClearPlot_Button_Click(object sender, RoutedEventArgs e)
            {
                
                await Task.Run(async () =>
                { 
                    await ErazeAllPlottedLines();
                });
            }

            private async Task<List<Point>> scalePlotY(List<Point> points, double factor)
            {
                List<Point> result = new List<Point>();
                   
                    await Task.Run( () =>
                    {
                       
                        for (int i = 0; i < points.Count; i++)
                        {
                            Point newPoint = new Point();
                            newPoint.X = points[i].X; //copy X as is
                            double doubleY = (double) (points[i].Y);
                            doubleY *= factor;
                            newPoint.Y = (int) Math.Floor(doubleY+.5);
                            result.Add(newPoint);
                        }

                       
                    });
                    return result;
            }
            
            private async Task<List<Point>> scalePlotX(List<Point> points, double factor)
            {
                List<Point> result = new List<Point>();

                await Task.Run(() =>
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        Point newPoint = new Point();
                        newPoint.Y = points[i].Y; //copy Y as is
                        double doubleX = (double) (points[i].X);
                        doubleX *= factor;
                        newPoint.X = (int) Math.Floor(doubleX + .5);
                        result.Add(newPoint);
                    }
                });

                return result;
            }

            public async Task ErazeAllPlottedLines()
            {
                for (int i = 0; i < plottedLines.Count; i++)
                {
                    await ErazePlottedLine(i);
                }
                plottingBitmapSource = ImageHandler.BitmapToSource(plottingBitmap);
                PlottingCanvas.Source = plottingBitmapSource;              
            }

            public async Task ClearPlottingArea()
            {
                await Task.Run( () =>
                {
                    for (int y = 0; y < plottingBitmap.Height; y++)
                    {
                        for (int x = 0; x < plottingBitmap.Width; x++)
                        {
                            plottingBitmap.SetPixel(x, y, Color.Black);
                        }

                    }

                    Dispatcher.Invoke(() =>
                    {
                        plottingBitmapSource = ImageHandler.BitmapToSource(plottingBitmap);
                        PlottingCanvas.Source = plottingBitmapSource;
                    });
                });

            }
       
    }
}