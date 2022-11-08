using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using log4net;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;

namespace ns_main
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static bool _continue;
        private static SerialPort _serialPort;
        private static SerialPort _fpga_serial;
        private static readonly string logFile = "i-system.log";
        public static readonly string speedCalFile = "speedCalibrator.csv";
        public StreamWriter speedLogFile;
        public static readonly string serialExchangeFile = "commsExchange.txt";
        public StreamWriter serialFile;




        private static readonly ILog log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool BlinkOn;
        private StreamWriter myLogFile;

        private Thread readThread = new(Read);
        


        private StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
        

        public MainWindow()
        {
            InitializeComponent();
            // var mySliderNumbers = new global::MainWindow.MotorSpeedSlider2(MotorSpeedSlider);
            // TestGrid.Children.Add(mySliderNumbers);
        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CommandQueue = new Queue();
            File.Delete(logFile);
            currentRpmValue = 0;
            var heartbeatTimer = new DispatcherTimer();
            heartbeatTimer.Interval = TimeSpan.FromMilliseconds(500);
            heartbeatTimer.Tick += heartbeat_Tick;
            heartbeatTimer.Start();

            var statusTimer = new DispatcherTimer();
            statusTimer.Interval = TimeSpan.FromMilliseconds(50);
            statusTimer.Tick += status_Tick;
            //statusTimer.Start();

            //  var rpmRefreshTimer = new DispatcherTimer();
            //  rpmRefreshTimer.Interval = TimeSpan.FromMilliseconds(rpmRefreshRate);
            //  rpmRefreshTimer.Tick +=rmpRefreshTick;
            // rpmRefreshTimer.Start();

            //InitSerial();
            Init_fpga_Serial();

            await LoadImage();

            await InitLathe();

            //await SetResolution(Stepper_resolution.stepFull);
            //await SetResolution(Stepper_resolution.stepHalf);

            //await SetFan(0);

            Resolution_ComboBox.Items.Add("Full");
            Resolution_ComboBox.Items.Add("Half");
            Resolution_ComboBox.Items.Add("Quater");
            Resolution_ComboBox.Items.Add("Eighths");
            Resolution_ComboBox.Items.Add("Sixteenth");
            Resolution_ComboBox.Items.Add("Thirtysecondths");

            Resolution_ComboBox.SelectedIndex = 0;

            await ProcessQueue();


            // await homeY();
            //
            // await homeX();



           // X_isHomed = true;
           // Y_isHomed = true;

            //await ChangeTabTo(Plotting_TabItem);
            
            ChangeTabTo(Development_TabItem);
            //testPlottingALine();
        }




        private void InitSerial()
        {
            if (_serialPort != null)
                _serialPort.Close();
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                if (_serialPort != null && !_serialPort.IsOpen)
                    _serialPort.Close();
                _serialPort = new SerialPort();
                _serialPort.PortName = "COM18"; //"COM11";
                _serialPort.BaudRate = 115200; //19200;//38400; //$$$9600; 115200;230400;
                _serialPort.Parity = Parity.None;
                _serialPort.DataBits = 8;
                _serialPort.StopBits = StopBits.One;
                _serialPort.Handshake = Handshake.None;

                // Set the read/write timeouts  
                _serialPort.ReadTimeout = int.MaxValue;
                _serialPort.WriteTimeout = 500;
                // _serialPort.DataReceived += async (o, args) => { await sp_DataReceived(o, args); };


                //_serialPort.Open(); //$$$$$$$$$$$$$$$$$$!!!!!!!!!!!!!$$$$$$$$$$$$$$$$$$$$$$$$
                _continue = true;
            }
        }

        private void Init_fpga_Serial()
        {
            if (_fpga_serial != null)
                _fpga_serial.Close();
            if (_fpga_serial == null || !_fpga_serial.IsOpen)
            {
                if (_fpga_serial != null && !_fpga_serial.IsOpen) _fpga_serial.Close();
                _fpga_serial = new SerialPort();
                _fpga_serial.PortName = "COM5"; //"COM11";
                _fpga_serial.BaudRate = 115200; //19200;//38400; //$$$9600; 115200;230400;
                _fpga_serial.Parity = Parity.None;
                _fpga_serial.DataBits = 8;
                _fpga_serial.StopBits = StopBits.One;
                _fpga_serial.Handshake = Handshake.None;
 
                // Set the read/write timeouts  
                _fpga_serial.ReadTimeout = int.MaxValue;
                _fpga_serial.WriteTimeout = 500;
                _fpga_serial.DataReceived += async (o, args) => { await sp_fpga_DataReceived(o, args); };
                
                _fpga_serial.Open(); //$$$$$$$$$$$$$$$$$$!!!!!!!!!!!!!$$$$$$$$$$$$$$$$$$$$$$$$
                _continue = true;
            }
        }        
        
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
        }


        private void ClearResponse()
        {
            response_ListBox.Items.Clear();
        }

        public async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            byte[] serialOuputFrame = new byte[]{0x97,0xE6,0xAA,0x55,0x03,0x06,0x88,0x13,0xF4,0x01,0x68,0x19};
            Dispatcher.Invoke(() =>
            {

            });
            

            try
            {
                //$$$ For WPM
                // var percent = 40;
                // ushort value = (ushort)((percent / 100.0) * 20000.0);
                // byte low = (byte)(value & 0xff);
                // byte high = (byte)((value>>8) & 0xff);
                //
                // serialOuputFrame[2] = low;
                // serialOuputFrame[3] = high;
                
                _fpga_serial.Write(serialOuputFrame,0,12);
                Dispatcher.Invoke(() =>
                {

                    
                    
                    response_ListBox.Items.Add(
                      //  $"<< Sent 0x97,0xE6,{low},{high},0x03,0x04,0x05,0x06,0x07,0x08,0x68,0x19");
                      $"<< Sent 0x97,0xE6,0xAA,0x55,0x00,0x01,0x05,0x06,0x07,0x08,0x68,0x19");      
                });
            }
            catch (Exception)
            {
                var x = typeof(Exception);
                Dispatcher.Invoke(() =>
                {
                    response_ListBox.Items.Add($"<< Problems sending Block {x}");
                });
                return;
            }
          


        }

        private async void Button_2_Click(object sender, RoutedEventArgs e)
        {
            await ProcessQueue();
            /*try
            {
                SKImageInfo imageInfo = new SKImageInfo(300, 250);
                using (SKSurface surface = SKSurface.Create(imageInfo))
                {
                    SKCanvas canvas = surface.Canvas;
                    canvas.Clear(SKColors.Blue);

                    using (SKPaint paint = new SKPaint())
                    {
                        paint.Color = SKColors.Red;
                        paint.StrokeWidth = 15;
                        paint.Style = SKPaintStyle.Stroke;
                        canvas.DrawCircle(50, 50, 30, paint);
                    }

                    using (SKImage image = surface.Snapshot())
                    using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                    using (MemoryStream mStream = new MemoryStream(data.ToArray()))
                    {
                        Bitmap bm = new Bitmap(mStream, false);

                        System.Windows.Forms.PictureBox picturebox1 = new System.Windows.Forms.PictureBox();
                        windowsFormsHost1.Child = picturebox1;
                        picturebox1.Paint += new System.Windows.Forms.PaintEventHandler(picturebox1_Paint)
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }*/
        }

        private async void Button_3_Click(object sender, RoutedEventArgs e)
        {
            Init_fpga_Serial();
            await InitLathe();
            ClearResponse();
            uint testDestination1 = 20000;
            uint testDestination2 = 10000;
            uint testDestination3 = 15000;
            uint testDestination4 = 300;

            await homeX();
            await homeY();
            await enqueuePCMessage($"Going to {testDestination1}");
            await GotoX(testDestination1);
            await enqueuePCMessage($"Going to {testDestination2}");
            await GotoX(testDestination2);
            await enqueuePCMessage($"Going to {testDestination3}");
            await GotoX(testDestination3);
            await enqueuePCMessage($"Going to {testDestination4}");
            await GotoX(testDestination4);
            //await GotoX(testDestination2, Stepper_resolution.stepEight);
            await ProcessQueue();
            await InitLathe();
        }

        private async void Button_4_Click(object sender, RoutedEventArgs e)
        {
            Init_fpga_Serial();
            await InitLathe();
            ClearResponse();

            uint testDestination1 = 20000;

            Init_fpga_Serial();
            await InitLathe();
            await homeX();
            await enqueuePCMessage("stepFull");
            await GotoX(testDestination1, Stepper_resolution.stepFull);
            await homeX();

            await enqueuePCMessage("stepHalf");
            await GotoX(testDestination1, Stepper_resolution.stepHalf);
            await homeX();

            await enqueuePCMessage("stepQuater");
            await GotoX(testDestination1, Stepper_resolution.stepQuater);
            await homeX();

            await enqueuePCMessage("stepEight");
            await GotoX(testDestination1);
            await homeX();

            await enqueuePCMessage("stepSixteenth");
            await GotoX(testDestination1, Stepper_resolution.stepSixteenth);
            await homeX();

            await enqueuePCMessage("stepThirtySecond");
            await GotoX(testDestination1, Stepper_resolution.stepThirtySecond);
            await homeX();
            //InitSerial();
            await ProcessQueue();
            await InitLathe();
        }

        private void Button_5_Click(object sender, RoutedEventArgs e)
        {
            //TestCompletedPixels();
            DrawDoneLine(lineToDraw--);
        }

        private async void Button_6_Click(object sender, RoutedEventArgs e)
        {
            Init_fpga_Serial();
            await InitLathe();
            ClearResponse();

            uint testDestination1 = 25000; //25700

            Init_fpga_Serial();
            await InitLathe();
            await homeY();
            await enqueuePCMessage("stepFull");
            await GotoY(testDestination1);
            await homeY();

            await enqueuePCMessage("stepHalf");
            await GotoY(testDestination1, Stepper_resolution.stepHalf);
            await homeY();

            await enqueuePCMessage("stepQuater");
            await GotoY(testDestination1, Stepper_resolution.stepQuater);
            await homeY();

            await enqueuePCMessage("stepEight");
            await GotoY(testDestination1, Stepper_resolution.stepEight);
            await homeY();

            await enqueuePCMessage("stepSixteenth");
            await GotoY(testDestination1, Stepper_resolution.stepSixteenth);
            await homeY();

            await enqueuePCMessage("stepThirtySecond");
            await GotoY(testDestination1, Stepper_resolution.stepThirtySecond);
            await homeY();
            //InitSerial();
            await ProcessQueue();
            await InitLathe();
        }

        private void X_Radio_Button_Checked(object sender, RoutedEventArgs e)
        {
            X_Label.IsEnabled = true;
            CurrentX_TextBox.IsEnabled = true;

            Y_Label.IsEnabled = false;
            CurrentY_TextBox.IsEnabled = false;
        }

        private void Y_Radio_Button_Checked(object sender, RoutedEventArgs e)
        {
            Y_Label.IsEnabled = true;
            CurrentY_TextBox.IsEnabled = true;

            X_Label.IsEnabled = false;
            CurrentX_TextBox.IsEnabled = false;
        }


        private void CurrentY_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }


        private async void HomeX_Button(object sender, RoutedEventArgs e)
        {
            await homeX();
            await ProcessQueue();
        }

        private async void HomeY_Button(object sender, RoutedEventArgs e)
        {
            await homeY();
            await ProcessQueue();
        }

        private async void HomeAll_Button(object sender, RoutedEventArgs e)
        {
            await homeY();
            await homeX();

            await ProcessQueue();
        }

        private async void X_Go_Button_Click(object sender, RoutedEventArgs e)
        {
            var resoution = GetComboResolution();
            var distance = uint.Parse(CurrentX_TextBox.Text);
            await GotoX(distance, await resoution);
            await ProcessQueue();
        }

        private async void Y_Go_Button_Click(object sender, RoutedEventArgs e)
        {
            var resoution = GetComboResolution();
            var distance = ushort.Parse(CurrentY_TextBox.Text);
            await GotoY(distance, await resoution);
            await ProcessQueue();
        }

        private async void Step_0_Button_Click(object sender, RoutedEventArgs e)
        {
            //if 0 clicked hen remove all  ffstes and go back to original i.e. offset = 0

            var ButtonValue = 0 * stepMultiplier;
            await StepValue(ButtonValue);
        }

        private async void Step_1_Button_Click(object sender, RoutedEventArgs e)
        {
            var ButtonValue = 1 * stepMultiplier;
            await StepValue(ButtonValue);
        }

        private async void Step_2_Button_Click(object sender, RoutedEventArgs e)
        {
            var ButtonValue = 2 * stepMultiplier;
            await StepValue(ButtonValue);
        }

        private async void Step_4_Button_Click(object sender, RoutedEventArgs e)
        {
            var ButtonValue = 4 * stepMultiplier;
            await StepValue(ButtonValue);
        }

        private async void Step_8_Button_Click(object sender, RoutedEventArgs e)
        {
            var ButtonValue = 8 * stepMultiplier;
            await StepValue(ButtonValue);
        }

        private async void Step_16_Button_Click(object sender, RoutedEventArgs e)
        {
            var ButtonValue = 16 * stepMultiplier;
            await StepValue(ButtonValue);
        }

        private async void Step_32_Button_Click(object sender, RoutedEventArgs e)
        {
            var ButtonValue = 32 * stepMultiplier;
            await StepValue(ButtonValue);
        }

        private async void Step_64_Button_Click(object sender, RoutedEventArgs e)
        {
            var ButtonValue = 64 * stepMultiplier;
            await StepValue(ButtonValue);
        }

        private async void Step_128_Button_Click(object sender, RoutedEventArgs e)
        {
            var ButtonValue = 128 * stepMultiplier;
            await StepValue(ButtonValue);
        }


        private async void Step_256_Button_Click(object sender, RoutedEventArgs e)
        {
            var ButtonValue = 256 * stepMultiplier;
            await StepValue(ButtonValue);
        }


        private async Task StepValue(uint ButtonValue)
        {
            var X_TextBox = uint.Parse(CurrentX_TextBox.Text);
            var Y_TextBox = ushort.Parse(CurrentY_TextBox.Text);
            var X_TextBox_Actual = uint.Parse(CurrentX_TextBox_Actual.Text);
            var Y_TextBox_Actual = ushort.Parse(CurrentY_TextBox_Actual.Text);

            uint X_dest;
            uint Y_dest;

            if ((bool) Pos_RadioButton.IsChecked)
            {
                X_dest = X_TextBox_Actual + ButtonValue;
                Y_dest = Y_TextBox_Actual + ButtonValue;
            }
            else
            {
                X_dest = X_TextBox_Actual - ButtonValue;
                Y_dest = Y_TextBox_Actual - ButtonValue;
            }


            if ((bool) X_Radio_Button.IsChecked)
            {
                CurrentX_TextBox_Actual.Text = X_dest.ToString();
                Last_X_Step = currentX;
                await GotoX(X_dest, await GetComboResolution());
            }
            else
            {
                CurrentY_TextBox_Actual.Text = Y_dest.ToString();
                Last_Y_Step = currentY;
                await GotoY(Y_dest, await GetComboResolution());
            }

            await ProcessQueue();
        }

        private async void Approach_Button_Click(object sender, RoutedEventArgs e)
        {
            //uint startX = LEFT_WOOD_X - MMX(16);
            uint startX = 15500;
            await getContactDistance(startX);


            var startY = currentY + contactDeltaDistance;

            var depth = MMX(16);
            Lathing_ProgressBar.Maximum = depth;

            const double incriment = .1;


            await CutLatheLines(startX, startX - MMX(20), startY, 25600, MMX(incriment));

            await homeY();
            await homeX();
            //await SetFan(0);
            await ProcessQueue();
        }


        private void CurrentX_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                //textBlock1.Text = "You Entered: " + textBox1.Text;
                X_Go_Button_Click(sender, e);
        }

        private void CurrentY_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) Y_Go_Button_Click(sender, e);
        }

        private void Debug_CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if ((bool) cb.IsChecked)
                _DEBUGGING = true;
            else
                _DEBUGGING = false;
        }

        private void Pause_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_PAUSED) _PAUSED = false;
        }

        private async void heartbeat_Tick(object sender, EventArgs e)
        {
            await Task.Run( async () =>
            {
                if (_PAUSED)
                {
                    Pause_Button.IsEnabled = true;
                    if (BlinkOn)
                    {
                        Pause_Button.Content = "         Paused\nClick to continue.";
                        Pause_Button.Foreground = Brushes.Red;
                        Pause_Button.Background = Brushes.White;
                    }
                    else
                    {
                        Pause_Button.Foreground = Brushes.Black;
                        Pause_Button.Background = Brushes.Black;
                    }

                    BlinkOn = !BlinkOn;
                }
                else
                {
                    HeartBeatPhase = (HeartBeatPhase) ? false : true;
                    HeartbeatTextBox.Dispatcher.Invoke(() =>
                    {
                        if (HeartBeatPhase == true)
                        {
                            HeartbeatTextBox.Background = new SolidColorBrush(Colors.Red);
                        }
                        else
                        {
                            HeartbeatTextBox.Background = new SolidColorBrush(Colors.Black);
                        }
                    });
                    //await getLatheStatus();
                }
            });
 

        }
        
        private async void  status_Tick(object sender, EventArgs e)
        {
            //do state indication
            await Status_Label.Dispatcher.Invoke(async () =>
            {
                SolidColorBrush statusColor;
                switch (currentState)
                {
                    case latheState.normal:
                        statusColor = new SolidColorBrush(Colors.LightSkyBlue);
                        break;

                    case latheState.latheBusy:
                        statusColor = new SolidColorBrush(Colors.Yellow);
                        break;

                    case latheState.responseTooLate:
                        statusColor = new SolidColorBrush(Colors.Orange);
                        break;

                    case latheState.receivedResponse:
                        statusColor = new SolidColorBrush(Colors.LightGreen);
                        break;

                    case latheState.sentCommand:
                        statusColor = new SolidColorBrush(Colors.DarkGreen);
                        break;

                    case latheState.tooManyCommandResends:
                        statusColor = new SolidColorBrush(Colors.DeepPink);
                        break;

                    case latheState.latheRecoveredFromKill:
                        statusColor = new SolidColorBrush(Colors.DarkRed);
                        break;

                    default:
                        statusColor = new SolidColorBrush(Colors.DarkGray);
                        break;
                }
                Status_Label.Background = statusColor;
                if(currentState!= latheState.sentCommand)
                {
                    await getLatheStatus();
                    await ProcessQueue();
                }
            });
        }
        
        private async Task heartBeat()
        {
            await Task.Run(async () =>
            {

                if (_PAUSED)
                {
                    Pause_Button.IsEnabled = true;
                    if (BlinkOn)
                    {
                        Pause_Button.Content = "         Paused\nClick to continue.";
                        Pause_Button.Foreground = Brushes.Red;
                        Pause_Button.Background = Brushes.White;
                    }
                    else
                    {
                        Pause_Button.Foreground = Brushes.Black;
                        Pause_Button.Background = Brushes.Black;
                    }

                    BlinkOn = !BlinkOn;
                }
                else
                {
                    HeartbeatTextBox.Dispatcher.Invoke(() =>
                    {
                        if (HeartBeatPhase == true)
                        {
                            HeartbeatTextBox.Background = new SolidColorBrush(Colors.Red);
                        }
                        else
                        {
                            HeartbeatTextBox.Background = new SolidColorBrush(Colors.Black);
                        }
                    });

                }
            });
            
//////////////////////////////////heartbeat/////
            await Task.Run(async () =>
            {
                heartbeatCount++;
                if (heartbeatCount > 5)
                {
                    heartbeatCount = 0;




                    HeartbeatTextBox.Dispatcher.Invoke(() =>
                    {
                        if (HeartBeatPhase == true)
                        {
                            HeartbeatTextBox.Background = new SolidColorBrush(Colors.Red);
                        }
                        else
                        {
                            HeartbeatTextBox.Background = new SolidColorBrush(Colors.Black);
                        }
                    });
                    await getLatheStatus();
                }
            });

            /// ////////////////////////////// 
        }       

        // private async void UI_update_Tick(object sender, EventArgs e)
        // {
        //     await getLatheStatus();
        //     await ProcessQueue();
        // }


        private async void GoToLeft_Button_CLick(object sender, RoutedEventArgs e)
        {
            await GotoX(LeftLineStepperX);
            CurrentX_TextBox.Text = currentX.ToString();
            await ProcessQueue();
        }

        private async void GoToRight_Button_Click(object sender, RoutedEventArgs e)
        {
            await GotoX(RightLineStepperX);
            CurrentX_TextBox.Text = currentX.ToString();
            await ProcessQueue();
        }

        private async void Goto_Wood_Approach_Button_Click(object sender, RoutedEventArgs e)
        {
            await GotoY(CuttingApproachDistanceY);
            CurrentY_TextBox.Text = currentY.ToString();
            await ProcessQueue();
        }

        private void BlackCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(BlackImageCanvas);
            X_TextBox.Text = pos.X.ToString();
            Y_TextBox.Text = pos.Y.ToString();
        }

        private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private async void Pop_Button_Click(object sender, RoutedEventArgs e)
        {
            if ((bool) X_Radio_Button.IsChecked)
                await GotoX(Last_X_Step);
            else
                await GotoY(Last_Y_Step);

            await ProcessQueue();
        }


        private void CurrentX_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Nudge_Slider.Value = 0;
            var text = CurrentX_TextBox.Text;
            if (text != null && CurrentX_TextBox_Actual != null) CurrentX_TextBox_Actual.Text = text;
        }

        private void CurrentX_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void CurrentY_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Nudge_Slider.Value = 0;
            var text = CurrentY_TextBox.Text;
            if (text != null && CurrentY_TextBox_Actual != null) CurrentY_TextBox_Actual.Text = text;
        }


        private async void MotorSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

                //$$$
                // var slider = sender as Slider;
                //
                // var value = (int) slider.Value;
                //
                //
                // Slider_Input_TextBox.Text = value.ToString();
                //
                // requestedRpm = value;
                // RPM_isRequested = true;
 
        }

        // private async void ProbeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        // {
        //     var slider = sender as Slider;
        //
        //     if (slider != null)
        //     {
        //         var value = (int) slider.Value;
        //
        //
        //         if (Probe_TextBox != null)
        //             Probe_TextBox.Text = value.ToString();
        //
        //
        //         await SetProbeAngle((ushort) value);
        //     }
        //
        //
        //     await ProcessQueue();
        // }

        private async void Probe_Off_Radio_Button_Checked(object sender, RoutedEventArgs e)
        {
            // await SetProbeAngle(PROBE_FOLDED_VALUE);
        }

        private async void Probe_On_Radio_Button_Checked(object sender, RoutedEventArgs e)
        {
            // await SetProbeAngle(PROBE_DEPLOYED_VALUE);
        }

        public async void DeployProbe()
        {
            // await SetProbeAngle(PROBE_DEPLOYED_VALUE);
        }

        public async void FoldProbe()
        {
            // await SetProbeAngle(PROBE_FOLDED_VALUE);
        }

        private async void Motor_Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            // MotorSpeed_TextBox.Text = 0.ToString();
            // await SetMotorForwardSpeed(0);
            // await ProcessQueue();
            await RequestRPM(0);
        }

        private async void Slider_Input_KeyDown_TextBox(object sender, KeyEventArgs e)
        {
            ushort value;
            if (e.Key == Key.Return)
                if (ushort.TryParse(Slider_Input_TextBox.Text, out value))
                {
                    await SetMotorForwardSpeed(value);
                    await ProcessQueue();
                }
        }

        // delegate is used to write to a UI control from a non-UI thread  
        private delegate void SetTextDeleg(string text);

        private async void CalibrateRPM_Button_Click(object sender, RoutedEventArgs e)
        {
            // private StreamWriter speedLogFile;
            ChangeTabTo(Plotting_TabItem);
            plottedLines.Clear();
            
            List<List<Point>> resultScaledLines = new List<List<Point>>();
            List<List<Point>> resultUnScaledLines = new List<List<Point>>();
            List<Point> currentResultLine;
 
            using (speedLogFile)
            {
                File.Delete(speedCalFile); //delete the file first
            }

            await ClearPlottingArea();

            List<Color> colors = new List<Color>();
            colors.Add(Color.Blue);
            colors.Add(Color.Red);
            colors.Add(Color.LightGreen);
            colors.Add(Color.Black);
            colors.Add(Color.Yellow);
            colors.Add(Color.Orange);
        

            ushort testCount = 6;
            ushort startingTachoCount = 30;
            ushort currentTest = 0;

            List<Point> resultList = new List<Point>();

            response_ListBox.Items.Clear();
            var scaledValues = new List<Point>();


            uint currentTachoCount;
   
            await Task.Run(async () =>
            {
                currentTachoCount = startingTachoCount;
                currentRpmValue = 0;
                //give RPM time to settle
                await SetMotorForwardSpeed((ushort) currentTachoCount);
                await ProcessQueue();
                Thread.Sleep(2000);
                do
                {
                    //resultLines[currentTest] = new List<Point>();
                    currentResultLine = new List<Point>();
                    while (currentRpmValue < 6500.0)
                    {
                        await SetMotorForwardSpeed((ushort) currentTachoCount);
                        await ProcessQueue();

                        //using (speedLogFile = File.AppendText(speedCal))
                        //{
                         //   await speedLogFile.WriteLineAsync($"{currentTachoCount}, {currentRPM}");
                            var aResult = new Point();
                            if (currentRpmValue > 10000)
                                currentRpmValue = 0; //patch I dont know why I get infinity for first
                            aResult.X = (ushort) currentTachoCount;

                            aResult.Y = (ushort) Math.Floor(currentRpmValue + .5);
                            currentResultLine.Add(aResult);
                        //}

                        await LogPC($"{currentTachoCount}, {currentRpmValue}");

                        Thread.Yield();
                        Thread.Sleep(100);
                        currentTachoCount += 10;
                    }



                    //ramp down speed
                    while (currentTachoCount > 50)
                    {
                        await SetMotorForwardSpeed((ushort) currentTachoCount);
                        await ProcessQueue();
                        Thread.Yield();
                        Thread.Sleep(200);
                        currentTachoCount -= 20;
                    }

                    await SetMotorForwardSpeed(startingTachoCount);
                    await ProcessQueue();
                    Thread.Sleep(2000);
                    //Plot current results
                    var xFactor = 1.786;
                    var yFactor = 0.071;
                    resultUnScaledLines.Add(currentResultLine);
                    scaledValues = await scalePlotX(currentResultLine, xFactor);
                    scaledValues = await scalePlotY(scaledValues, yFactor); //Can reuse!
                    resultScaledLines.Add(scaledValues);
 
                    // if (currentTest < testCount - 1)
                        currentTest++;

                } while (currentTest < testCount);
                

            });

            //stop the motor
            await SetMotorForwardSpeed(0);
            await ProcessQueue();
            
            //plot the values
            List<Point> scaledAverageList = new List<Point>();
            List<Point> UnScaledAverageList = new List<Point>();
            double scaledAverage = 0.0;
            double unScaledAverage = 0.0;
            int nextList;

            //find the shortest list count in all results  taken
            var shortestListLength = 999999;
            for (int shortest = 0; shortest < resultScaledLines.Count; shortest++)
            {
                if (resultScaledLines[shortest].Count < shortestListLength)
                    shortestListLength = resultScaledLines[shortest].Count;
            }
            
            for (int point = 0; point < shortestListLength; point++)
            {
                scaledAverage = 0;
                unScaledAverage = 0;
                //get all the Y members of the current point
                for (nextList = 0; nextList < resultScaledLines.Count; nextList++)
                {
                    scaledAverage += resultScaledLines[nextList][point].Y;
                    unScaledAverage += resultUnScaledLines[nextList][point].Y;
                }

                scaledAverage /= resultScaledLines.Count;
                unScaledAverage /= resultUnScaledLines.Count;
                
                //scaled avarage list
                Point scaledNextPoint = new Point();
                scaledNextPoint.Y = (int)(Math.Floor(scaledAverage + .5));
                scaledNextPoint.X = resultScaledLines[0][point].X;
                scaledAverageList.Add(scaledNextPoint);
                
                //unscaled average
                Point unscaledNextPoint = new Point();
                unscaledNextPoint.Y = (int)(Math.Floor(unScaledAverage + .5));
                unscaledNextPoint.X = resultUnScaledLines[0][point].X;
                UnScaledAverageList.Add(unscaledNextPoint);                
            }
            
            await PlottingCanvas.Dispatcher.Invoke(async () =>
            {
                await PlotLine(scaledAverageList, Color.Aqua);
            });

            for (int point = 0; point < shortestListLength; point++)
            {
                using(speedLogFile = File.AppendText(speedCalFile))
                {
                    await speedLogFile.WriteLineAsync($"{UnScaledAverageList[point].X}, {UnScaledAverageList[point].Y}");
                }
            }
        }



        private async void RPM_Select_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;

            string Value = "";
            if (combo.SelectedIndex >= 0)
                Value = ((ComboBoxItem)combo.SelectedItem).Content.ToString();

            int selectedRpm = 0;

            if(int.TryParse(Value, out selectedRpm))
            {
                await RequestRPM((ushort) selectedRpm);
            }

        }

        private async void FanSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;

            if (slider != null)
            {
                var value = (int) slider.Value;


                if (Fan_TextBox != null)
                    Fan_TextBox.Text = value.ToString();


                await SetFanSpeed((ushort) value);
            }


            await ProcessQueue();
        }

        private void Fan_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void Fan_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // var text = Fan_TextBox.Text;
            // if (text != null && Fan_TextBox_Actual != null) CurrentY_TextBox_Actual.Text = text;
        }

        private void Motor_RPM_TextBox_KeyDown(object sender, KeyEventArgs e)
        {

        }


        private void Motor_CurrentX_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void Motor_CurrentX_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Nudge_Slider.Value = 0;
            var text = CurrentX_TextBox.Text;
            if (text != null && CurrentX_TextBox_Actual != null) CurrentX_TextBox_Actual.Text = text;
        }

        private async void MotorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;

            if (slider != null)
            {
                var value = (int)slider.Value;


                if (Fan_TextBox != null)
                    Fan_TextBox.Text = value.ToString();


                await SetMotorForwardSpeed((ushort)value);
            }


            await ProcessQueue();
        }
    }
}