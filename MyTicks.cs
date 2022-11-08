using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

public partial class MainWindow : Window
{
    public class MotorSpeedSlider2 : TickBar
    {
        private Slider slider;
        public MotorSpeedSlider2(Slider aSlider)
        {
            slider = aSlider;
        }
        protected override void OnRender(DrawingContext dc)
        {
            Size size = new Size(base.ActualWidth, base.ActualHeight);
            double num = this.Maximum - this.Minimum;          
            Point point = new Point(0, 0);
            Point point2 = new Point(0, 0);
            double y = this.ReservedSpace * 0.5;
            FormattedText formattedText = null;
            // Slider slider = this.Ch
            // double xStart = Window.MotorSpeedSlider;
            double xStart = 1060;//slider.Margin.Left;
            double yStart = 594; //slider.Margin.Bottom;
  
            for (int i = 0; i < 31; i++)
            {
                formattedText = new FormattedText(
                    (i*200).ToString(),
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.RightToLeft,
                    new Typeface("Verdana"),
                    8,
                    Brushes.Black);

                formattedText.TextAlignment = TextAlignment.Right;
                if(i>1)
                    dc.DrawText(formattedText, new Point(xStart,yStart-(i*16.26)));
            }
        }
    }

    public class ringBufferPointer
    {
        private uint depth;
        private static uint currentValue;
        public ringBufferPointer(uint aDepth)
        {
            depth = aDepth;
            currentValue = 0;
        }
        
        // public static ringBufferPointer operator ++()
        // {
        //     currentValue++;
        // }
        
    }
    
    
}