using System;
using System.Threading;
using System.Threading.Tasks;
//using ImageClassLibrary;
using System.Windows;
using ImageClassLibrary;
using System.Windows.Media.Imaging;

namespace ns_main
{
    public partial class MainWindow : Window
    {
        private uint lineToDraw = TotalLines-1;
        CancellationTokenSource cancellationTokenSource;
        private async void Test_Button_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                // Already have an instance of the cancellation token source?
                // This means the button has already been pressed!

                cancellationTokenSource.Cancel();
                cancellationTokenSource = null;

                Test_Button.Content = "Test";
                inTest = false;
                return;
            }

            try
            {
                
                cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.Token.Register(() => {

                });
                Test_Button.Content = "Cancel"; // Button text
                inTest = true;
                await DisplayInfo(cancellationTokenSource.Token);

            }
            catch (Exception ex)
            {
                inTest = false;
                await logPCError(ex.Message);
            }
            finally
            {
                cancellationTokenSource = null;
                Test_Button.Content = "Test";
                inTest = false;
            }
        }

        private async Task DisplayInfo(CancellationToken cancellationToken)
        {
            while(true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    inTest = false;
                    break;
                }

                await getLatheStatus();
                await ProcessQueue();
            }
        }


        
        
        private async Task TestCompletedPixels()
        {
            uint y;
            BitmapSource bls;
            bls = ImageHandler.BitmapToSource(blackImageBitmap);
            BlackImageCanvas.Source = bls;
            try
            {
                for (y = (uint) Max_Y; y >= 120; y--)
                {
                    DrawDoneLine(y);
                }
                bls = ImageHandler.BitmapToSource(blackImageBitmap);
                BlackImageCanvas.Source = bls;
            }
            catch (Exception e)
            {
                await LogPC(e.ToString());
                //Console.WriteLine(e);
                //throw;
            }
            

        }

        
    }
}