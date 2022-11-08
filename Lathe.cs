using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//using ImageClassLibrary;
using System.Windows;
using System.Windows.Media;
using ImageClassLibrary;
//using SkiaSharp.Views.WPF;
using Color = System.Drawing.Color;


namespace ns_main
{
    public partial class MainWindow : Window
    {
        public const string MSG_LATHE_RESPONSE_START = "LRS";
        public const string MSG_LATHE_RESPONSE_END = "LRE";
        public const string MSG_LATHE_ERROR_START = "LES";
        public const string MSG_LATHE_DEBUG_START = "LDS";
        public const string MSG_LATHE_LOG_MESSAGE_START = "LMS";
        public const string MSG_PC_MESSAGE_START = "PMS";
        public const string MSG_LATHE_COMMAND_START = "LCS";
        public const string LATHE_REQUEST_RESENT_CMD_START = "LRR";
        public const string LATHE_BUSY_START = "LBS";
        public const string LATHE_BUSY_END = "LBE";
        public const string LATHE_KILL_RECOVER_START = "LKS";
        public const string LATHE_KILL_RECOVER_END = "LKE";
        public const byte SERIAL_FRAME_HEADER_LOW = 0x97;
        public const byte SERIAL_FRAME_HEADER_HIGH = 0xE6;
        public const byte SERIAL_FRAME_FOOTER_LOW = 0x68;
        public const byte SERIAL_FRAME_FOOTER_HIGH = 0x19;
        public const ushort highestRPM = 6000;
        public const ushort biggetsWoodRadius = 45; 
        public const ushort biggetsWoodDiameter = biggetsWoodRadius * 2; 
        public const ushort BlankLatheStart = 0;
        public const ushort BlankMMLength = 1600;
        public const ushort cuttingApproachDistanceX = 3304;
        public const ushort LeftLineStepperX = 5032; //left most latheable
        public const ushort RightLineStepperX = 21488; //need to add MMX(12) to 19928 = (1560+19928)=
        public const ushort ModelMMWidth = 1200;
        public const ushort DepthListCount = ModelMMWidth;
        public const ushort TotalLatheWoodCuttableWidth = 15600; //MMX(120)
        public const ushort LeftWoodPadEnd = 140;
        public const ushort ChuckY_Max = 20000; //$$$ 1200;
        public const ushort ChuckX_Max = LeftLineStepperX; // for now!
        public const ushort CuttingApproachDistanceY = 4736;
        public const ushort PROBE_DEPLOYED_VALUE = 1700;
        public const ushort PROBE_FOLDED_VALUE = 900;
        public const ushort Max_Stepper_Y = 6200;
        public const ushort TotalLines = 90;
         //952;
        public const double STEPS_PER_MMX = 130.7;
        public const double STEPS_PER_MMY = 161.3;
        const uint _Debug_Y_Offset = 1200;
        const uint _Debug_ContactDistance = 500;
        public const uint Zero_Y = 7662;
        public const uint Contact_18 = 5012;
        public const double mainCounterClockFrequency = 10000000;
        public const double pointsPerRev = 10;
        private const int Threshold = 250 * 250 * 250;
        public const int rpmRingBufferDepth = 64;
        public const int ultrasonicBufferDepth = 8;
        public const uint latheHeartbeatInterval = 500; //ms
        public const uint allowedLatheBusyTime = 800000; //ms = 8sec
        public const uint maxResendCommandRequests = 4; //amount of times a resend is issued to lathe for whatever reason
        public const uint maxResendResponseRequests = 4; //amount of times a resend is issued to lathe for whatever reason
        public const uint SERIAL_INPUT_FRAME_LENGTH = 20;
        
        
        private int Max_X, Max_Y, Half_Y, Max_Cut_X, Max_Cut_Y;
        System.Drawing.Bitmap ImageBitmap, woodImageBitmap, blackImageBitmap;
        private List<System.Drawing.Point> DepthList; 

        System.Windows.Media.Imaging.BitmapSource bms;
        System.Windows.Media.Imaging.BitmapSource bmw;
        System.Windows.Media.Imaging.BitmapSource blm;

        private const uint _LineDivisions = 6;

        public List<int> speedCalibrationIndexes = new List<int>();
        public List<int> speedCalibrationValues = new List<int>();
        private int minRPM;
        private int maxRPM;
        private int minCntIndex;
        private int maxCntIndex;
        private double rpmRefreshRate = 50.0; //ms 50?
        private int maxIncrementInterval = 5;
        MyRingBuffer rpmRingBuffer = new(rpmRingBufferDepth);
        private MyRingBuffer ultrasonic = new(ultrasonicBufferDepth);
        public int heartbeatCount;
        public st_uart_rx uartRx = default;
        public st_uart_tx lastSentCommand = default;
        public st_lathe_status latheStatus = default;
        private uint latheBusyCounter;

        
        
        /// <summary>
        /// Image brightness above or below certain fixed threshold
        /// </summary>
        public enum ThresholdResult
        {
            Below,
            Above
        }


        private async Task LoadImage()
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: LoadImage()");

            DepthList = new List<System.Drawing.Point>();
            
            int width, height;
            string fileName;
            string fileName2 = null;
            string woodImage;
            string blackImage;

            if (Environment.MachineName == "DESKTOP-H9M4HI7")
            {
                fileName   = @"G:\Backup\Hardware\ns\vs\ImageClassLibrary\test_image.png";
                fileName2   = @"G:\Backup\Hardware\ns\vs\ImageClassLibrary\test_image2.png";
                blackImage = @"G:\Backup\Hardware\ns\vs\ImageClassLibrary\black_image.png";
                woodImage  = @"G:\Backup\Hardware\ns\Images\wood_1_d.jpg";
            }
            else
            {
                fileName = (@"C:\ns\vs\ImageClassLibrary\test_image.png");
                woodImage = @"C:\ns\Images\wood_1_d.jpg";
                blackImage = @"C:\ns\Images\black_image.png";
            }

            var mn = Environment.MachineName;
            ImageHandler.GetImageSize(fileName, out width, out height);
            width += 120;
            ImageHandler.ResizeImage(fileName, fileName2, width, height);
            ImageHandler.ResizeImage(woodImage, woodImage, width, height);
            ImageHandler.ResizeImage(blackImage, blackImage, width, height);
            ImageCanvas.Width = width;
            ImageCanvas.Height = height;
            BlackImageCanvas.Width = width;
            BlackImageCanvas.Height = height;
            Tab_Control.Width = width;
            LatheGrid.Width = width;
            Main_Window.Width = width;
            Main_Grid.Width = width;
            
            bms = ImageHandler.GetBitmapSourceFromFile(fileName2);
            bmw = ImageHandler.GetBitmapSourceFromFile(woodImage);
            blm = ImageHandler.GetBitmapSourceFromFile(blackImage);
            
            ImageBitmap = ImageHandler.BitmapFromSource(bms);
            woodImageBitmap = ImageHandler.BitmapFromSource(bmw);
            blackImageBitmap = ImageHandler.BitmapFromSource(blm);
            //create an empty correct length
            for(int i = 0;i<width;i++)
            {
                DepthList.Add(new System.Drawing.Point(0, 0));
            }

            if (ImageCanvas != null)
            {
                int y;
                for (y = 0; y < height; y++)
                {
                    {
                        for (var x = 0; x < width; x++)
                        {
                            blackImageBitmap.SetPixel(x, y, woodImageBitmap.GetPixel(x, y));
                            if (compareThreshold(ImageBitmap, x, y) == ThresholdResult.Below)
                            {
                                ImageBitmap.SetPixel(x, y, woodImageBitmap.GetPixel(x, y));
                            }
                            else
                            {
                                if (y == ImageBitmap.Height - 1 ||
                                    (compareThreshold(ImageBitmap, x, y + 1) == ThresholdResult.Below)) // if on boarder
                                {
                                    DepthList[x] = (new System.Drawing.Point(x, y));
                                }

                                ImageBitmap.SetPixel(x, y, woodImageBitmap.GetPixel(x, y));
                            }
                        }
                    }
                }

                bms = ImageHandler.BitmapToSource(ImageBitmap);
                PlottingCanvas.Source = bms;
            }
            

            BlackImageCanvas.Height = height;
            Max_Y = height - 1;
            Max_X = (int) (BlackImageCanvas.Width-1);

            // for (int i = 100; i < 600 ;i++)
            // {
            //     SetCompletedPixel((uint)i, 100);
            // }
            
            drawDepths(0, blackImageBitmap);
            //TestCompletedPixels();
        }

        public void SetCompletedPixel(uint x, uint y)
        {
            blackImageBitmap.SetPixel((int)x, (int)y, System.Drawing.Color.FromArgb(255, 255, 255, 255));
            var bls = ImageHandler.BitmapToSource(blackImageBitmap);
            BlackImageCanvas.Source = bls;
        }

        public void SortPoints()
        {
            var result = new List<System.Drawing.Point>();

            for (int i1 = 0; i1 < DepthList.Count; i1++)
            {
                System.Drawing.Point point = DepthList[i1];
                var count = DepthList.Count;
                if (count == 0)
                {
                    DepthList.Add(point);
                }
                else
                {
                    int i;
                    for (i = 0; i < count; i++)
                    {
                        if (DepthList[i].X >= point.X)
                        {
                            DepthList.Insert(i, point);
                            break;
                        }
                    }
                    DepthList.Insert(i, point); //top one
                }
            }

        }

        public async Task PrintPoints()
        {
            
            for (int i = 0; i < DepthList.Count; i++)
            {
                await LogPC($"x={DepthList[i].X}, y={DepthList[i].Y}");
            }
        }

        public void DrawMyOwn()
        {
            try
            {
                var bs = ImageHandler.CreateBitmapSource(800, 800, Colors.White);
                var myBitmap = ImageHandler.BitmapFromSource(bs);

                for (var y = 0; y < 800; y++)
                {

                    {

                        for (var x = 400 - 100; x < 400 + 100; x++)
                        {
                            myBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(255, 0, 0, 0));
                        }

                    }
                }

                bs = ImageHandler.BitmapToSource(myBitmap);
                ImageCanvas.Source = bs;
                // encode the image using the original format
                var encodedBytes = ImageHandler.GetEncodedImageData(bs, ".png");

                // save the modified image
                // string filename = @"G:\Programming\Image\new test_image.png";
                // ImageHandler.SaveImageData(encodedBytes, filename);
            }
            catch (Exception)
            {

                throw;
            }


        }



        void draw_X_dotted_line(int y, System.Drawing.Bitmap bm, System.Drawing.Color color)
        {
            var width = ImageCanvas.Width;
            draw_X_dotted_line(y, 0, (int)width, bm, color);
        }

        void draw_X_dotted_line(int y, int xStart, int xEnd, System.Drawing.Bitmap bm, System.Drawing.Color color)
        {
            var width = ImageCanvas.Width;
            for (var x = xStart; x < xEnd; x++)
            {
                if (x % 4 == 0)
                    bm.SetPixel(x, y, color); //Max_y
            }
        }

        /// <summary>
        /// Superimpose lathe shape on blank wood image
        /// </summary>
        /// <param name="y"></param>
        /// <param name="bitmap"></param>
        void drawDepths(int y, System.Drawing.Bitmap bitmap)
        {

            for (int i = 0; i < DepthList.Count; i++)
            {

                bitmap.SetPixel(DepthList[i].X, DepthList[i].Y, System.Drawing.Color.FromArgb(255, 255, 255, 255));
            }
            var bls = ImageHandler.BitmapToSource(blackImageBitmap);
            BlackImageCanvas.Source = bls;
        }

        void draw_Y_dotted_line(int x, System.Drawing.Bitmap bm, System.Drawing.Color color)
        {
            var height = bm.Height;
            int y;
            try
            {
                for (y = 0; y < height; y++)
                {
                    if (y % 4 == 0)
                        bm.SetPixel(x, y, color); //Max_x
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        int findMax_X(System.Drawing.Bitmap bm)
        {
            var width = bm.Width; //X
            var height = bm.Height; //Y

            var longest = 0;
            var currentLength = 0;
            var inModel = false;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (compareThreshold(bm, x, y) == ThresholdResult.Below) //if in model
                    {
                        inModel = true;
                        currentLength++;
                    }
                    else if (inModel == true)
                    {
                        if (currentLength > longest == true)
                        {
                            longest = currentLength;
                            Max_X = y;
                        }

                        currentLength = 0;
                        inModel = false;
                        continue; //no need to look further in this line because only non-model left
                    }
                    else
                    {
                        inModel = false;
                    }
                }
            }

            return longest;
        }

        int findMax_Y(System.Drawing.Bitmap bm)
        {
            var width = bm.Width; //X
            var height = bm.Height; //Y
            var modelStart_Y = 0; //start of model coming from above

            var longest = 0;
            var currentLength = 0;
            var inModel = false;

            for (var x = 0; x < width; x++)
            {
                modelStart_Y = 0;
                for (var y = 0; y < height; y++)
                {
                    if (compareThreshold(bm, x, y) == ThresholdResult.Below) //if in model
                    {
                        if (modelStart_Y == 0 && inModel == false) //if first time you hit model
                            modelStart_Y = y;
                        inModel = true;
                        currentLength++;
                    }
                    else if (inModel) //if previous was in model ie. end of line
                    {
                        if (currentLength > longest)
                        {
                            longest = currentLength;
                            Max_Y = x;
                        }

                        currentLength = 0;
                        inModel = false;
                        continue; //no need to look further in this line because only non-model left
                    }
                    else
                    {
                        inModel = false;
                    }
                }
            }

            Half_Y = modelStart_Y + (Max_Y / 2);
            return longest;
        }

        ThresholdResult compareThreshold(System.Drawing.Bitmap bitmap, int x, int y, int threshold = Threshold)
        {
            
            try
            {
                var px = bitmap.GetPixel(x, y);
                if (px.R * px.G * px.B < threshold)
                    return ThresholdResult.Below;
                return ThresholdResult.Above;
            }
            catch (Exception)
            {

                throw;
            }

        }

        void findMax_Cut(System.Drawing.Bitmap bm)
        {
            Max_Cut_X = Max_Cut_Y = 0;
            var width = bm.Width; //X
            var height = bm.Height; //Y

            int x;

            for (var y = Half_Y; y < height; y++)
            {
                x = 0;
                while ((compareThreshold(bm, x, y) == ThresholdResult.Above))
                    x++; //move into start of model
                for (; x < width; x++)
                {
                    if (compareThreshold(bm, x, y) == ThresholdResult.Above && (x < (width - 300))) //if reached max cut
                    {
                        Max_Cut_X = x;
                        Max_Cut_Y = y;
                    }
                }
            }

        }

        public static double mapRange(double fromValue, double fromLow, double fromHigh, double toLow, double toHigh)
        {
            
            var value = (fromLow * toHigh - fromHigh * toLow + fromValue * toLow - fromValue * toHigh) / (fromLow - fromHigh);
            //clamp
            // if (value < toLow)
            //     value = toLow;
            // if (value > toHigh)
            //     value = toHigh;
            return value;
        }

        public static double mapRangeNoClamp(double fromValue, double fromLow, double fromHigh, double toLow, double toHigh)
        {
            return (fromLow * toHigh - fromHigh * toLow + fromValue * toLow - fromValue * toHigh) / (fromLow - fromHigh);
        }

        public static bool inRange(double value, double rangeLow, double rangeHigh)
        {
            return (!(value < rangeLow) && !(value > rangeHigh));

        }

        /// <summary>
        /// Get the depths that describe shape to be lathed from photo
        /// </summary>
        /*public void getDepths()
        {
            int deb =0;
            for (int x = 0; x < ImageCanvas.Width; x++)
            {
                var otherColour = System.Drawing.Color.FromArgb(255, 73, 64, 56);
                int currentDepth = 0;
            for (var y = 0; y < ImageBitmap.Height; y++)
            {
                    var pix = ImageBitmap.GetPixel(x, y);
                    if (pix != otherColour)
                    {
                        currentDepth++;
                    }
                    else
                    {
                        deb++;
                    }
            }
        
            DepthList.Add(new System.Drawing.Point(x, currentDepth));
            //DepthList.Add(new System.Drawing.Point(x, 60));

            }
            var redPen = new System.Drawing.Pen(System.Drawing.Color.Red, 3);
            var greenPen = new System.Drawing.Pen(System.Drawing.Color.Green, 3);

            for (var x = 0; x < ImageBitmap.Width; x++)
            {
                ImageBitmap.SetPixel(x, (DepthList[x].Y)-1, System.Drawing.Color.FromArgb(255, 255, 255, 255));
            }

            var bm = ImageHandler.BitmapToSource(ImageBitmap);
            ImageCanvas.Source = bm;

            // Create pens.

        }*/

        /// <summary>
        /// See if point is to be cut away i.e not part of model
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public bool toCut(uint x, uint y)
        {

            if (!inRange(x, 0, DepthList.Count - 1))
                return false;
            
            var point = DepthList[(int) x];
            return (y <= Max_Y && y > point.Y);
        }

        public List<int> toCutInLine(uint line)
        {
            return Enumerable.Range(0, Max_X).Where(cut => toCut((uint) cut,  line)).ToList();

        }

        public bool toCutWholeLine(uint line)
        {
            return toCutInLine(line).Count == DepthList.Count;
        }

        /// <summary>
        /// Assume from is at start of valley
        /// </summary>
        /// <param name="line"></param>
        /// <param name="from">x value</param>
        /// <returns></returns>
        public LatheLineSegment getValleyWidth(uint line, uint from)
        {
            //uint aLine, uint from,uint cuttingApproachDistanceX,uint contactedDistance,uint aMax_Y,List<Point> aDepthList)
            LatheLineSegment lls = new LatheLineSegment(line, from, cuttingApproachDistanceX, contactedDistance, MAX_Y,
                DepthList);

            lls.WoodWidth = getValleyEnd(line, from);
            
            return lls;
        }

        public List<LatheLineSegment> getLineValleysMouths(uint line)
        {
            List<LatheLineSegment> segments = new List<LatheLineSegment>();
            List<LatheLineSegment> valleys = new List<LatheLineSegment>();
            //    LatheLineSegment lls = new LatheLineSegment(line,from,cuttingApproachDistanceX,contactedDistance,MAX_Y,DepthList);

            LatheLineSegment nextValley = new LatheLineSegment(line,0,cuttingApproachDistanceX,contactedDistance,MAX_Y,DepthList);
            long xIncrement;
            for(long x = 0; x<DepthList.Count;x +=xIncrement)
            {
                if (isValleyMouthStart(line,(uint) x))
                {
                    nextValley = getValleyWidth(line, (uint) x);
                    valleys.Add(nextValley);
                    xIncrement =  (nextValley.WoodWidth == 0)?1:nextValley.WoodWidth;
                }
                else
                {
                    xIncrement = 1;
                }
            }
            return valleys;
        }

        public bool isValleyMouthStart(uint line, uint from)
        {
            if (!inRange(from,0,DepthList.Count-1))
                return false;
            if (from == 0 && toCut(from, line))
                return true;

            var start = (from == 0) ? 1 : from;
            
            if(!toCut((uint) (start - 1), line) && toCut((uint) start, line))
            {
                return true;
            }
            return false;
        }

        public bool isValleyMouthEnd(uint line, uint from)
        {
            if (!inRange(from,1,DepthList.Count-1))
                return false;
            
            return (toCut(from, line) && !toCut(from+1, line) ) || (from == DepthList.Count-1);           
        }

        /// <summary>
        /// Get next valley start from  wherever you are
        /// </summary>
        /// <param name="line"></param>
        /// <param name="from"></param>
        /// <returns>index of valley start was found starting from 'from' = from + distanceToNextValleyStart
        /// Use getValleyEnd for next from</returns>
        public uint getValleyStart(uint line, uint from)
        {
            if(toCutWholeLine(line))
                return UInt32.MaxValue;
            
            uint current = from;
            while (!isValleyMouthStart(current, line) && current < DepthList.Count)
            {
                current++;
            }

            if (current >= DepthList.Count)
                return UInt32.MaxValue;
            return current;
        }
        
        public uint getValleyEnd(uint line, uint from)
        {
            if(toCutWholeLine(line))
                return UInt32.MaxValue;
            

            uint current = 0;
            while (!isValleyMouthEnd(line, current+from) && (current+from) < DepthList.Count)
            {
                current++;
            }
            
            return current;          
        }

        private void DrawDoneLine(uint y)
        {
            var Tocut = toCutInLine(y);

            foreach (var point in Tocut)
            {
                blackImageBitmap.SetPixel((int) point, (int) y, System.Drawing.Color.FromArgb(255, 0, 0, 0));
            }

            bms = ImageHandler.BitmapToSource(blackImageBitmap);
            BlackImageCanvas.Source = bms;
        }

        private void DrawDoneLine(uint y, uint from, uint to)
        {
            var points = DepthList.Where(dl => (dl.X >= from) && (dl.X <= to) && (dl.Y == y)).ToList();
            foreach (var point in points)
            {
                blackImageBitmap.SetPixel( point.X,  point.Y, System.Drawing.Color.FromArgb(255, 0, 0, 0)); 
            }
            bms = ImageHandler.BitmapToSource(blackImageBitmap);
            BlackImageCanvas.Source = bms;
        }

        
        private async Task DrawDoneSegment(LatheLineSegment segment)
        {
            int x;
            var xx = segment.StepperStart.X;
            var width = segment.WoodWidth;
            try
            {
                foreach (var point in Enumerable.Range(segment.SegmentStepperStartX.X,(int) segment.WoodWidth))
                {
                    x = point;
                    Dispatcher.Invoke(() =>
                    {
                        blackImageBitmap.SetPixel(point, (int) segment.line, Color.FromArgb(255, 0, 0, 0));
                    });
                }
                bms = ImageHandler.BitmapToSource(blackImageBitmap);
                BlackImageCanvas.Source = bms;
            }
            catch (Exception e)
            {
                await logPCError(e.ToString());
                throw;
            }
        }

        private uint getLineToCutLatheYReference(uint woodLine)
        {
            return (uint) mapRange(woodLine-1, 0, TotalLines-1, 0, Contact_18);
        }
        
        private uint getLineToCutLatheXReference(uint woodX)
        {
            return (uint) mapRange(woodX, 0, DepthList.Count-1, LeftLineStepperX, RightLineStepperX-1 );
        }

        private uint getAbsoluteLineToCutLatheXReference(uint woodX)
        {
            //double debug = 95.1;
            return (uint) mapRange(woodX, 0, 999, cuttingApproachDistanceX, cuttingApproachDistanceX + (MMX(99.9) ));
        }
      
        private async Task cutSegment(LatheLineSegment segment)
        {
            if(!contactDistanceEstablished)
                await logPCError("No cutting is allowed until contact distance is established in order to get reference.");
         

            
            var start = segment.StepperStart.X;
            var end = segment.getStepperXEnd();
            //segment.line = 9;
            var yReff = segment.getStepperY();
            //await homeY();
            //await GotoY((uint) yReff,Stepper_resolution.stepFull);
            await GotoY((uint) (yReff - MMY(6)),
                Stepper_resolution.stepFull); //go slightly away from cutting spot
            await ProcessQueue();
            var subSegments = getSubSegments(segment,_LineDivisions);

            foreach (var seg in subSegments)
            {

                ////////////////////////////////////////////////////////////
                // await GotoY(200, Stepper_resolution.stepHalf);
                // await homeY(); //synchronise Y
                // await GotoX(200, Stepper_resolution.stepFull);
                // await homeX(); //synchronise X
                //////////////////////////////////////////////////////////                
                

                
                // await GotoY((uint) (yReff - MMY(6)),
                //     Stepper_resolution.stepFull); //go slightly away from cutting spot
                // await ProcessQueue();
                
                await GotoX((uint) start, Stepper_resolution.stepHalf); //go to the correct x
                await ProcessQueue();
                
        
                
                await GotoY((uint) yReff); //now digg in!!
                await ProcessQueue();

                if (!_DEBUGGING)
                {
                    await GotoX(end, Stepper_resolution.stepHalf);
                    await ProcessQueue();
                    await GotoX((uint) start, Stepper_resolution.stepHalf);
                    await ProcessQueue();
                }


                else
                {
                    if (_LOG_GOTO_X)
                    {
                        await enqueuePCMessage($"cutSegment: 688 GotoX{end}");
                        await ProcessQueue();
                    }

                    await GotoX(end, Stepper_resolution.stepHalf);
                    if (_LOG_GOTO_X)
                    {
                        await enqueuePCMessage($"cutSegment: 691 GotoX{start}");
                        await ProcessQueue();
                    }
                    await GotoX((uint) start, Stepper_resolution.stepHalf);
                }
            }

            await GotoY(yReff);
            await ProcessQueue(); //$$$ New!!!
            await DrawDoneSegment(segment);
        }

        private List<LatheLineSegment> getSubSegments(LatheLineSegment segment,uint count)
        {
            var result = new List<LatheLineSegment>();
            var yStartSegment = segment.getStepperY();
            var yLastSegment = yStartSegment + (MMY(1));
            var eachSegmentHeight = (uint) Math.Round(((double)yLastSegment - (double)yStartSegment) / count);

            for( int seg = 0; seg<count;seg++) 
            {
                var nextSeg = segment.Copy();
                nextSeg.StepperStart.Y = (int) (nextSeg.StepperStart.Y + (eachSegmentHeight * (seg+1)));
                result.Add(nextSeg);
            }
            
            return result;
        }
        
        private async Task cutLine(uint line)
        {
            if(!contactDistanceEstablished)
                await logPCError("No cutting is allowed until contact distance is established in order to get reference.");

            // await GotoY(200, Stepper_resolution.stepHalf);
             await homeY(); //synchronise Y
            // await GotoX(200, Stepper_resolution.stepFull);
            // await homeX(); //synchronise X

            // await ProcessQueue();
            // await GotoX(LeftLineStepperX,Stepper_resolution.stepFull);
            // await ProcessQueue();
            
            var valleys = getLineValleysMouths(line);

            foreach (var valley in valleys)
            {
                await LogPC($"line:{valley.line}," +
                      $"LatheWidth:{valley.LatheWidth}," +
                      $"StepperStart:{valley.StepperStart}" +
                      $"WoodStart:{valley.SegmentStepperStartX}," +
                      $"getStepperXStart():{valley.getStepperXStart()}");
                await cutSegment(valley);
                await ProcessQueue();
            }
        }

 
        private async Task cutAlongProfile(int offset = 0)
        {
            int x = 0; //$$$ watch that x!
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: cutAlongProfile(int offset)");
            
            if(!contactDistanceEstablished)
                await logPCError("No cutting is allowed until contact distance is established in order to get reference.");

            MyPointStore myPointStore = null;
            
            var startX = getLineToCutLatheXReference((uint) DepthList[0].X);
            var startY = mapRange(DepthList[x].Y-1, 0, TotalLines-1,  Contact_18,Zero_Y-1);

            await GotoX(startX, Stepper_resolution.stepEight);
            await GotoY((uint) startY, Stepper_resolution.stepEight);
            await ProcessQueue();

            for (int xx = 0;x<DepthListCount-1;x +=13)
            {
                await GotoDepthListX(xx);
                if (myPointStore != null) // if used before for point x-1
                    myPointStore.restoreSurroundingColor();
                myPointStore = new MyPointStore(DepthList[xx], blackImageBitmap);
                myPointStore.storeSurrounding(3);
                myPointStore.setSurroundingColor(Color.Black);

                var bls = ImageHandler.BitmapToSource(blackImageBitmap);
                BlackImageCanvas.Source = bls;
                await ProcessQueue();
            }
            await ProcessQueue();
            for (var xx  = (int) (DepthListCount-1);xx>=0;xx -=13)
            {
                await GotoDepthListX(xx);
                if (myPointStore != null) // if used before for point x-1
                    myPointStore.restoreSurroundingColor();
                myPointStore = new MyPointStore(DepthList[xx], blackImageBitmap);
                myPointStore.storeSurrounding(3);
                myPointStore.setSurroundingColor(Color.Black);

                var bls = ImageHandler.BitmapToSource(blackImageBitmap);
                BlackImageCanvas.Source = bls;
                await ProcessQueue();
            }
            
            await ProcessQueue();
            
            await LoadImage();
        }

        private async Task GotoDepthListX(int x)
        {
            
            //var lathY = getLineToCutLatheYReference((uint) DepthList[x].Y);
            var lathY = mapRange(DepthList[x].Y - 1, 0, TotalLines - 1, Contact_18, Zero_Y - 1);
            var lathX = getLineToCutLatheXReference((uint) DepthList[x].X);

            await GotoX((uint) (lathX), Stepper_resolution.stepThirtySecond);
            await GotoY((uint) lathY, Stepper_resolution.stepThirtySecond);
            blackImageBitmap.SetPixel((int) x, (int) DepthList[x].Y, System.Drawing.Color.FromArgb(255, 255, 0, 0));
            await ProcessQueue();

        }

        /*private async Task<List<Point>> Get_Surrounding_Pixels(Point aPoint)
        {
            List<Point> points = new List<Point>();
            var x = aPoint.X;
            var y = aPoint.Y; 
            points.Add(new Point(aPoint.X, aPoint.Y));
            if(aPoint.X>0)
                points.Add(new Point(aPoint.X-1, aPoint.Y));
            if(aPoint.X<TotalWidth-1)
                points.Add(new Point(aPoint.X+1, aPoint.Y));
            if(aPoint.Y<TotalLines-1)
                points.Add(new Point(aPoint.X, aPoint.Y+1));
            if(aPoint.Y>0)
                points.Add(new Point(aPoint.X, aPoint.Y-1));

            return points;

        }*/

        /// <summary>
        /// this is it!! cut whole model.
        /// </summary>
        private async void Cut_Model_Button_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                // Already have an instance of the cancellation token source?
                // This means the button has already been pressed!

                cancellationTokenSource.Cancel();
                cancellationTokenSource = null;

                Cut_Model_Button.Content = "Cut Model";
                //inTest = false;
                return;
            }

            try
            {

                cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.Token.Register(() => {
                
                });
                Cut_Model_Button.Content = "Cancel"; // Button text
                //inTest = true;
                await CutModel(cancellationTokenSource.Token);
                if (_LOG_METHODS)
                   await enqueuePCMessage("Model has been cut");

                cancellationTokenSource.Cancel();
                cancellationTokenSource = null;

                Cut_Model_Button.Content = "Cut Model";
            }
            catch (Exception ex)
            {
                //inTest = false;
                await logPCError(ex.Message);
            }
        }

        public async Task CutModel(CancellationToken cancellationToken)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: CutModel(CancellationToken cancellationToken)");
            await getContactDistance(cuttingApproachDistanceX);

            //await cutAlongProfile();
            //for (var line =getModelHeight(); line >=0 ; line--)
            for (var line =getModelHeight(); line >=0 ; line--)
            {
                if (cancellationToken.IsCancellationRequested)
                {

                    return;
                }

                // for (var l = 0; l <= 90; l++)
                // {
                //     LatheLineSegment s = new LatheLineSegment( (uint) l,0,cuttingApproachDistanceX,contactedDistance,MAX_Y,DepthList);
                //     await logInt($"line {l} = ", (int) s.getStepperY());
                // }
                
                
                
                await cutLine((uint) line);
                await ProcessQueue();
                
                Dispatcher.Invoke(() =>
                {
                    Cut_Model_Text_Box.Text = line.ToString();
                });

            } 
            
            if (_LOG_METHODS)
                await enqueuePCMessage("Finished cutting model)");
            
            await cutAlongProfile(0);
            await ProcessQueue();
            
            //await SetFan(0);
            await putMessageBox("Done cutting!");
            Dispatcher.Invoke(() =>
            {
                Cut_Model_Text_Box.IsEnabled = false;
            });
            await homeY();
            await homeX();
            await ProcessQueue();
            Dispatcher.Invoke(() =>
            {
                Cut_Model_Text_Box.Text = "Cut Model";
                Cut_Model_Text_Box.IsEnabled = true;
            });
        }

        public async Task getSpeedCalibrationValues()
        {
            await Task.Run( () =>
            {
                try
                {
                    // Create an instance of StreamReader to read from a file.
                    // The using statement also closes the StreamReader.
                    //private List<int> speedCalibrationIndexes = new List<int>();
                    //private List<int> speedCalibrationValues = new List<int>();
                    using (StreamReader sr = new StreamReader(speedCalFile))
                    {
                        string line;
                        // Read and display lines from the file until the end of
                        // the file is reached.
                        while ((line = sr.ReadLine()) != null)
                        {
                            int index;
                            int value;

                            string[] split = line.Split(',');
                            int.TryParse(split[0], out index);
                            int.TryParse(split[1], out value);

                            speedCalibrationIndexes.Add(index);
                            speedCalibrationValues.Add(value);
                            maxRPM =speedCalibrationValues.Last();
                            minRPM =speedCalibrationValues.First();
                            maxCntIndex = speedCalibrationIndexes.Last();
                            minCntIndex = speedCalibrationIndexes.First();

                        }
                    }
                }
                catch (Exception e)
                {
                    // Let the user know what went wrong.
                    Console.WriteLine("The file could not be read:");
                    Console.WriteLine(e.Message);
                }
            });
        }


        //Ammpiunt to increment per increment slot depending on magnitude of error
    }
}
