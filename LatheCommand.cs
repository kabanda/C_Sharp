using System;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ns_main
{
    public partial class MainWindow : Window
    {

        private bool _DEBUGGING = true; //
        private bool _PAUSED = false;
        private bool _LOG_METHODS = false;
        private bool _LOG_GOTO_X = true;
        private bool _LOG_MESSAGES = true; //
        private bool _LOG_ERRORS = true;
        private bool _LOG_STATUS = false;
        private bool _LONG_COMMS_ERRORS = true;

        public struct sDistance
        {
            public ushort original;
            public ushort fulls;
            public ushort halves;
            public ushort quarters;
            public ushort eights;
            public ushort sixteens;
            public ushort thirtySeconds;

        };

        public struct st_uart_tx
        {
            public ushort header; // = 0xE697;
            public ushort command_id;
            public Lathe_command command;
            public ns_device device;
            public ushort command_param_1;
            public ushort command_param_2;            
            public string text;
            public ushort footer; //= 0x1968
        }

        public enum en_lathe_status : int
        {
            KILL_RECOVER,
            X_STOP,
            Y_STOP,
            FORWARD_OVERCURRENT,REVERSE_OVERCURRENT
        };
        public struct st_lathe_status
        {
            public bool killRecover;
            public bool xStop;
            public bool yStop;
            public bool forwardOverCurrent;
            public bool reverseOverCurrent;
            
        }
        
        public struct st_uart_rx
        {
            public ushort command_id;
            public st_lathe_status status;
            public byte device_busy_status;
            public UInt32 motor_rpm;
            public UInt32 x_pos;
            public UInt32 y_pos;
            public ushort ultrasonic;
        }        

        public enum latheState: byte
        {
            normal,
            sentCommand,
            receivedResponse,
            tooManyCommandResends,
            tooManyResponseResends,
            latheBusy,
            responseTooLate,
            latheRecoveredFromKill
        }
        

        public enum ns_device : byte
        {
            NONE,
            X_STEPPER,
            Y_STEPPER,
            LATHE_MOTOR,
            FAN,
            PC
        }

        public const uint ARM_UP_VALUE = 1000;
        public const uint ARM_DOWN_VALUE = 1900;





        public enum Lathe_command : byte
        {
            Lathe_command_No_Command, //0
            Lathe_command_Status, //1
            Stepper_command_Step, //2 
            Stepper_Command_Direction, //3
            Stepper_Command_Resolution, //4
            Stepper_Command_Motor_forward, //5
            Stepper_Command_Motor_reverse, //6
            Fan_Speed, //7
            Stepper_Command_Home_X, //8
            Stepper_Command_Home_Y, //9
            Lathe_Command_PauseMs, //10
            Lathe_command_Pause, //11
            Lathe_command_MessageBox, //12
            Motor_command_Target_RPM, //13
            Probe_command_Rotate, //14
            Comms_command_Request_Resend_Response, //15
            Comms_command_Request_Resend_Command //16

        };

        public enum ProbeAngle : uint
        {
            Probe_off = 670,
            Probe_on = 1999,
        }

        public enum Stepper_direction
        {
            TOWARDS_X_STOP,
            AWAY_FROM_X_STOP,
            TOWARDS_Y_STOP,
            AWAY_FROM_Y_STOP
        };

        public enum Stepper_resolution : byte
        {
            stepFull = 0,
            stepHalf = 4,
            stepQuater = 2,
            stepEight = 6,
            stepSixteenth = 1,
            stepThirtySecond = 5
        }



        bool X_HomeRequested;
        bool Y_HomeRequested;
        bool contacting;
        private bool sw_1;
        private bool sw_2;

        private bool inTest;

        //Stepper_resolution stepperResolution;
        uint currentX;
        uint currentY;
        private int latheCurrentX;
        private int latheCurrentY;
        private uint currentResendCommandCount;
        private uint currentResendResponseCount;

#pragma warning disable 414
        byte currentFan;
#pragma warning restore 414
        ushort MessageId;
        ushort lastSentMessageId;
        private uint stepMultiplier;
        uint contactDeltaDistance;
        uint contactedDistance;

        private bool contactDistanceEstablished;

        private uint currentMotorSpeed;
        bool commandPending;
        latheState currentState;
        private DateTime commandSentTime;
        private TimeSpan commandDuration;
        private DateTime QueueProcessStartTime;
        uint MAX_X;

        uint MAX_Y;

        //uint RIGHT_WOOD_X;
        //uint LEFT_WOOD_X;
        //private uint cuttingApproachDistanceY;
        //private uint cuttingApproachDistanceX;
        public Stepper_resolution _slow_speed;
        public Stepper_resolution _medium_speed;
        public Stepper_resolution _fast_speed;
        public bool HeartBeatPhase;
        public bool RPM_isRequested;
        public double requestedRpm;
        private double currentRpmValue;
        public int currentRpmCounterValue;

        public uint Last_X_Step;
        public uint Last_Y_Step;
        public uint tachoCount;
        public double ultrasonicDistance;
        private Queue CommandQueue;

        public async Task InitLathe()
        {

            if (_LOG_METHODS)
                await enqueuePCMessage("Method: InitLathe()");

            currentFan = 0;
            MessageId = 0;
            //stepperResolution = Stepper_resolution.stepThirtySecond;
            //CommandQueue = new Queue();
            commandPending = false;
            X_HomeRequested = false;
            Y_HomeRequested = false;
            currentX = 0;
            currentY = 0;
            MAX_X = 500000; //$$$
            MAX_Y = 2000; //$$$
            stepMultiplier = 1;
            currentMotorSpeed = 0;
            currentRpmValue = 0;
            requestedRpm = 0;
            //cuttingApproachDistanceX = 21800;
            //cuttingApproachDistanceY = 23500;  
            //RIGHT_WOOD_X = 9000;
            //LEFT_WOOD_X = 22192;
            inTest = false;
            contactDistanceEstablished = false;
            Last_X_Step = 0;
            Last_Y_Step = 0;
            tachoCount = 0;
            ultrasonicDistance = 0;
            HeartBeatPhase = false;
            RPM_isRequested = false;
            requestedRpm = 0;
            heartbeatCount = 0;
            latheBusyCounter = 0;
            currentResendCommandCount = 0;
            currentResendResponseCount = 0;
            currentRpmCounterValue = 0;
            
            await Setup_Plotting();
            await getSpeedCalibrationValues();
            
            

        }

        async void my_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = _serialPort.ReadLine();

            await LogPC("Received from PSoC: " + DecodeResponse(data));

            var splits = data.Split('$');

            //if (splits.Length != 6 || splits[4] != "MSG_LATHE_RESPONSE_END")
            //    commsError("Command mulformed");

            var msgId = splits[1];
            //check for synchronization error
            var queDepth = CommandQueue.Count;
            var adjustedId = MessageId - queDepth - 1;

        }

        /// <summary>
        /// Add a command to he queue
        /// Message ID for command is also generated
        /// </summary>
        /// <param name="command"></param>
        /// <param name="stepper"></param>
        /// <param name="value"></param>
        public async Task EnqueueCommand(Lathe_command command, ns_device device, ushort command_param_1  = 0, ushort command_param_2  = 0, string message = "")
        {
            if (CommandQueue == null)
                return;
            if (inTest && command != Lathe_command.Lathe_command_Status)
            {
                await LogPC("Function not allowed during testing.");
                //CommandQueue.Clear();
                return;
            }

            var id =  MessageId;
            var cmd = command;


            st_uart_tx txStructure = default;  //Initialize structure

            txStructure.header = 0xE697;
            txStructure.command_id = MessageId;
            txStructure.command = command;
            txStructure.device = device;
            txStructure.command_param_1 = command_param_1;
            txStructure.command_param_2 = command_param_2;            
            txStructure.footer = 0x1968;
        
            // string latheCommand;
            // if (message.Length != 0)
            // {
            //     latheCommand = $"{MSG_LATHE_COMMAND_START}${id}${cmd}${stp}${value}${message}$LCE$".ToString();
            // }
            // else
            // {
            //     latheCommand = $"{MSG_LATHE_COMMAND_START}${id}${cmd}${stp}${value}$LCE$".ToString();
            // }


            MessageId++;
            MessageId = MessageId;

            CommandQueue.Enqueue(txStructure);

        }

        /*/// <summary>
        /// Contruct a queue ready string
        /// </summary>
        /// <param name="command"></param>
        /// <param name="stepper">NONE, X or Y</param>
        /// <param name="value">if any</param>
        /// <returns></returns>
        public String CreateCommandString(Lathe_command command, ns_device stepper, uint value = 0)
        {

            var id = (byte) MessageId;
            var cmd = (byte) command;
            var stp = (byte) stepper;

            string latheCommand = $"MSG_LATHE_COMMAND_START${id}${cmd}${stp}${value}$LCE$".ToString();

            MessageId++;
            MessageId = MessageId % 256;

            return latheCommand;

        }*/


        public async Task getLatheStatus()
        {
            await EnqueueCommand(Lathe_command.Lathe_command_Status, ns_device.NONE);
            await ProcessQueue();
        }

        //public async Task homeX()
        public async Task homeX()
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: homeX()");

            await EnqueueCommand(Lathe_command.Stepper_Command_Home_X, ns_device.X_STEPPER);
            currentX = 0;
            X_HomeRequested = true;
            Current_X_TextBox.Text = "0";
        }

        //public async Task homeY()
        public async Task homeY()
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: homeY()");

            await EnqueueCommand(Lathe_command.Stepper_Command_Home_Y, ns_device.Y_STEPPER);
            currentY = 0;
            Y_HomeRequested = true;
            Current_Y_TextBox.Text = "0";
        }

        public async Task AwaitCommandQueEmpty()
        {
            await Task.Run(() =>
            {
                while (commandPending)
                {
                    Thread.Yield();
                    Thread.Sleep(20);
                }

            });
        }

        async Task StepX(ushort count)
        {
            if (!X_HomeRequested)
            {
                await logPCError("Cannot step X axis until have homed X axis at least once.");
                return;
            }

            await EnqueueCommand(Lathe_command.Stepper_command_Step, ns_device.X_STEPPER, count);
            currentX += count;
        }

        async Task StepY(ushort count)
        {
            if (!Y_HomeRequested)
            {
                await logPCError("Cannot step Y axis until have homed Y axis at least once.");
                return;
            }

            await EnqueueCommand(Lathe_command.Stepper_command_Step, ns_device.Y_STEPPER, count);
            currentY += count;
        }


        private async Task GotoLeftProbeLocation()
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: LowerArm(uint value)");

            await GotoY(15000); //just in case there are things to hit in X!!!
            await GotoX(15768);
            await GotoY(21104);
        }


        // private async Task  SetProbeAngle(ProbeAngle value)
        // {
        //     if (_LOG_METHODS)
        //         enqueuePCMessage("Method: SetProbeAngle(ProbeAngle value");
        //     
        //     await EnqueueCommand(Lathe_command.Stepper_command_Rotate_probe, ns_device.NONE, (uint)value);
        // }




        /// <summary>
        /// Enqueues a pause command in milliseconds. 
        /// This command is one of the few that is not sent to the lathe
        /// But since it is enqueued it happens at the right time in the 
        /// program sequence
        /// </summary>
        /// <param name="ms">milliseconds to pause</param>
        private async Task PauseMs(ushort ms)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: PauseMs(uint ms)");

            await EnqueueCommand(Lathe_command.Lathe_Command_PauseMs, ns_device.NONE, ms);
        }

        private async Task Pause()
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: Pause()");

            await EnqueueCommand(Lathe_command.Lathe_command_Pause, ns_device.NONE);
        }

        private async Task Set_RPM(ushort value)
        {
            //Tacho_command_Target
            if (_LOG_METHODS)
                await enqueuePCMessage($"Method: Set_RPM(uint value ({value}) )");

            await EnqueueCommand(Lathe_command.Motor_command_Target_RPM, ns_device.LATHE_MOTOR, value);
        }

        public static String GetTimestamp(DateTime value)
        {
            //return value.ToString("yyyy MM dd HH mm ss ffff");
            return value.ToString("~ dd-MM-yyyy HH:mm:ss:fff");
        }

        public String TimeStamp()
        {
            return GetTimestamp(DateTime.Now);
        }

        public async Task<string> DecodeValue(Lathe_command command, int value)
        {
            if (_LOG_METHODS)
               await enqueuePCMessage($"Method: DecodeValue(Lathe_command command, int value = {value})");

            String result = "";
            switch (command)
            {
                // case Lathe_command.Stepper_Command_Fan:
                //     result = $"{value}%";
                //     Fan_TextBox.Text = $"{value}";
                //     break;

                case Lathe_command.Stepper_Command_Direction:
                    //result = value == (int) Stepper_direction.AWAY_FROM_STOP ? "AWAY FROM STOP" : "TOWARDS STOP";
                    switch (value)
                    {
                        case (int) Stepper_direction.AWAY_FROM_X_STOP:
                            result = "AWAY FROM X STOP";
                            break;
                        case (int) Stepper_direction.TOWARDS_X_STOP:
                            result = "TOWARDS X STOP";
                            break;
                        case (int) Stepper_direction.AWAY_FROM_Y_STOP:
                            result = "AWAY FROM Y STOP";
                            break;
                        case (int) Stepper_direction.TOWARDS_Y_STOP:
                            result = "TOWARDS Y STOP";
                            break;
                    }

                    break;

                case Lathe_command.Stepper_command_Step:
                    result = $"{value.ToString()} steps";
                    break;

                case Lathe_command.Stepper_Command_Resolution:
                    switch (value)
                    {
                        case 0:
                            result = "Full";
                            break;
                        case 4:
                            result = "Half";
                            break;
                        case 2:
                            result = "Quarter";
                            break;
                        case 6:
                            result = "Eighth";
                            break;
                        case 1:
                            result = "Sixteenth";
                            break;
                        case 5:
                            result = "ThirtySecond";
                            break;

                    }

                    break;



                default:
                    result = value.ToString();
                    break;

            }

            // if (_LOG_METHODS)
            //     await enqueuePCMessage($"Decoded value = {result})");

            return result;
        }

        public async Task<bool> GotoX(UInt32 dest, Stepper_resolution stepType = Stepper_resolution.stepEight)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage(
                    "Method: GotoX(uint dest, Stepper_resolution stepType = Stepper_resolution.stepEight)");

            if (!X_HomeRequested)
            {
                await logPCError("Can only use use GotoX after at least once 'homing' X axis.");
                return false;
            }

            if (dest < ChuckX_Max && currentY >= ChuckY_Max)
            {
                await logPCError("If I go left any more I will hit the lathe Jaw Chuck.");
                return false;
            }

            // $$$ if (dest > RightLineStepperX)
            //{
            //    dest = RightLineStepperX+MMX(5);
            //}

            int destinationOffset = (int) dest - (int) currentX;
            var destination = currentX + destinationOffset;
            if (destination > MAX_X || destination < 1)
            {
                await logPCError($"X destination of {destination} is outside the range 1 - {MAX_X}.");
                return false;
            }

            // if away from stop i.e. positive
            if (destinationOffset >= 0)
            {
                await SetDirection(Stepper_direction.AWAY_FROM_X_STOP);
            }
            else //towards stop
            {
                await SetDirection(Stepper_direction.TOWARDS_X_STOP);
            }

            destinationOffset = Math.Abs(destinationOffset);

            // sDistance resolutions = divideDistance((uint)destinationOffset, stepType);
            //
            // await StepMultiResolutionX(resolutions);

            await StepX((ushort) destinationOffset);

            currentX = dest;
            Dispatcher.Invoke(() => { Current_X_TextBox.Text = currentX.ToString(); });
            return true;
        }

        public async Task<bool> GotoY(uint dest, Stepper_resolution stepType = Stepper_resolution.stepFull)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage(
                    "Method: GotoY(uint dest, Stepper_resolution stepType = Stepper_resolution.stepEight)");

            if (!Y_HomeRequested)
            {
                await logPCError("Can only use use GotoY after at least once 'homing' Y axis.");
                return false;
            }
            //$$$
            // if (dest > ChuckY_Max && currentX <= ChuckY_Max )
            // {
            //     await logPCError("If I go forward any more I will hit the lathe Jaw Chuck.");
            //     return false;
            // }

            int destinationOffset = (int) dest - (int) currentY;
            var destination = currentY + destinationOffset;
            if (destination > MAX_Y || destination < 1)
            {
                await logPCError($"Y destination of {destination} is outside the range 1 - {MAX_Y}.");
                return false;
            }

            // if away from stop i.e. positive
            if (destinationOffset >= 0)
            {
                await SetDirection(Stepper_direction.AWAY_FROM_Y_STOP);
            }
            else //towards stop
            {
                await SetDirection(Stepper_direction.TOWARDS_Y_STOP);
            }

            destinationOffset = Math.Abs(destinationOffset);

            // sDistance resolutions = divideDistance((uint)destinationOffset, stepType);
            //
            // await StepMultiResolutionY(resolutions);

            await StepY((ushort) destinationOffset);

            currentY = dest;

            //await StepY(dest);

            Dispatcher.Invoke(() => { Current_Y_TextBox.Text = currentY.ToString(); });
            return true;
        }

        public async Task GoToY_MSB(uint mm) // Mid stock based
        {
            var lathDisplacement = MMY(mm);
            await GotoY((uint) (Zero_Y - lathDisplacement));
        }

        public async Task GoToY_SMB(uint mm) //stop micro switch based
        {
            var lathDisplacement = MMY(mm);
            await GotoY((uint) lathDisplacement);
        }

        public uint Get_MSB_Lathe_Value(uint mm)
        {
            var lathDisplacement = MMY(mm);
            return Zero_Y - lathDisplacement;
        }

        public uint Get_SMB_Lathe_Value(uint mm)
        {
            var lathDisplacement = MMY(mm);
            return (uint) lathDisplacement;
        }

        /// <summary>
        /// Go to distance from which to measure distance to wood
        /// </summary>
        public async Task GotoCuttingApproachDistance(uint x)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: GotoCuttingApproachDistance(uint x)");

            if (_LOG_GOTO_X)
            {
                await enqueuePCMessage($"cutAlongProfile: 564 GotoX{x}");
                await ProcessQueue();
            }

            await GotoX(x);
            await GotoY(CuttingApproachDistanceY, Stepper_resolution.stepEight);
        }

        /*public async Task StepMultiResolutionX(sDistance resolutions)
        {
            //const uint _mult = 2; //to cater for double resolution stepper

            if (_LOG_METHODS)
                await enqueuePCMessage("Method: StepMultiResolutionX(sDistance resolutions)");

            if (resolutions.fulls > 0)
            {
                await SetResolution(Stepper_resolution.stepFull);
                await StepX(resolutions.fulls);
            }

            if (resolutions.halves > 0)
            {
                await SetResolution(Stepper_resolution.stepHalf);
                await StepX(resolutions.halves);
            }

            if (resolutions.quarters > 0)
            {
                await SetResolution(Stepper_resolution.stepQuater);
                await StepX(resolutions.quarters);
            }

            if (resolutions.eights > 0)
            {
                await SetResolution(Stepper_resolution.stepEight);
                await StepX(resolutions.eights);
            }

            if (resolutions.sixteens > 0)
            {
                await SetResolution(Stepper_resolution.stepSixteenth);
                await StepX(resolutions.sixteens);
            }

            if (resolutions.thirtySeconds > 0)
            {
                await SetResolution(Stepper_resolution.stepThirtySecond);
                await StepX(resolutions.thirtySeconds);
            }

        }

        public async Task StepMultiResolutionY(sDistance resolutions)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: StepMultiResolutionY(sDistance resolutions)");

            if (resolutions.fulls > 0)
            {
                await SetResolution(Stepper_resolution.stepFull);
                await StepY(resolutions.fulls);
            }

            if (resolutions.halves > 0)
            {
                await SetResolution(Stepper_resolution.stepHalf);
                await StepY(resolutions.halves);
            }

            if (resolutions.quarters > 0)
            {
                await SetResolution(Stepper_resolution.stepQuater);
                await StepY(resolutions.quarters);
            }

            if (resolutions.eights > 0)
            {
                await SetResolution(Stepper_resolution.stepEight);
                await StepY(resolutions.eights);
            }

            if (resolutions.sixteens > 0)
            {
                await SetResolution(Stepper_resolution.stepSixteenth);
                await StepY(resolutions.sixteens);
            }

            if (resolutions.thirtySeconds > 0)
            {
                await SetResolution(Stepper_resolution.stepThirtySecond);
                await StepY(resolutions.thirtySeconds);
            }

        }

        /// <summary>
        /// Given a distance and a resolution, maximize speed using that resolution
        /// as highest then second smallest for remeinder and so on.
        /// </summary>
        /// <param name="distance">required distance</param>
        /// <param name="stepType">highest resolution to use</param>
        /// <returns>Structure contains amounts of each of the used resolutions.</returns>
        async Task<sDistance> divideDistance(ushort distance, Stepper_resolution stepType)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: divideDistance(uint distance, Stepper_resolution stepType)");

            sDistance result;
            //clear all results
            result.fulls = result.halves = result.quarters =
                result.eights = result.sixteens = result.thirtySeconds = 0;

            result.original = distance;

            ushort mod = 0;
            ushort div = 0;


            /////////////////1/1//////////////////////////////

            if (stepType == Stepper_resolution.stepFull)
            {
                mod = distance % 16;
                result.fulls = distance / 16;
            }

            /////////////////1/2//////////////////////////////
            if (stepType == Stepper_resolution.stepHalf)
            {
                div = distance;
                mod = distance % 16; //make your own div of total
            }
            else //from 32nd div
            {
                div = mod;
                mod = mod % 16; //16th remeinder
            }

            result.halves = div / 16;
            //////////////////////1/4//////////////////////////
            if (stepType == Stepper_resolution.stepQuater)
            {
                div = distance;
                mod = distance % 8; //make your own div of total
            }
            else //from 16th div
            {
                div = mod;
                mod = mod % 8;
            }

            result.quarters = div / 8;
            //Utils::printInt("quarters", result.quarters);
            /////////////////////////1/8/////////////////////////////
            if (stepType == Stepper_resolution.stepEight)
            {
                div = distance;
                mod = distance % 4; //make your own div of total
            }
            else //from 1/4th div
            {
                div = mod;
                mod = mod % 4;
            }

            result.eights = div / 4;
            //Utils::printInt("eights", result.eights);
            //////////////////////////1/16///////////////////////////////////
            if (stepType == Stepper_resolution.stepSixteenth)
            {
                div = distance;
                mod = distance % 2; //make your own div of total
            }
            else //from 1/4th div
            {
                div = mod;
                mod = mod % 2;
            }

            result.sixteens = div / 2;
            //Utils::printInt("sixteens", result.sixteens);
            ////////////////////////////1/32///////////////////////////////////////

            if (stepType == Stepper_resolution.stepThirtySecond)
            {
                div = distance;
                mod = distance % 1; //make your own div of total
            }
            else //from 1/4th div
            {
                div = mod;
                mod = mod % 1;
            }

            result.thirtySeconds = div / 1;
            //Utils::printInt("thirtySeconds", result.thirtySeconds);
            ///////////////////////////////////////////////////////////////
            return result;
        }*/

        /// <summary>
        /// Return the current resolution from resolution Combo Box
        /// </summary>
        /// <returns></returns>
        public async Task<Stepper_resolution> GetComboResolution()
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: GetComboResolution()");

            Stepper_resolution result;

            switch (Resolution_ComboBox.SelectedIndex)
            {
                case 0:
                    result = Stepper_resolution.stepFull;
                    break;
                case 1:
                    result = Stepper_resolution.stepHalf;
                    break;
                case 2:
                    result = Stepper_resolution.stepQuater;
                    break;
                case 3:
                    result = Stepper_resolution.stepEight;
                    break;
                case 4:
                    result = Stepper_resolution.stepSixteenth;
                    break;
                case 5:
                    result = Stepper_resolution.stepThirtySecond;
                    break;
                default:
                    result = Stepper_resolution.stepEight;
                    break;

            }

            return result;
        }

        public async Task putMessageBox(string message)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: putMessageBox(string message)");

            await EnqueueCommand(Lathe_command.Lathe_command_MessageBox, ns_device.NONE, 0, 0,message);
        }

        public async Task awaitMessageBox(string message)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("Method: awaitMessageBox(string message))");


            await Task.Run(() =>
            {
                var result = MessageBox.Show(message, "L_System", MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:

                        break;
                    case MessageBoxResult.No:
                        if (cancellationTokenSource != null)
                        {
                            cancellationTokenSource.Cancel();
                            cancellationTokenSource = null;
                        }

                        Dispatcher.Invoke(() => { Test_Button.Content = "L_System"; });

                        inTest = false;
                        break;
                    case MessageBoxResult.Cancel:
                        //MessageBox.Show("Nevermind then...", "L_System");
                        break;
                }
            });

        }


        public async Task SetMotorForwardSpeed(ushort value)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage($"Method: SetMotorForwadSpeed(uint {value})");
            uint oldStepperValue = currentMotorSpeed;
            currentMotorSpeed = value;
            await EnqueueCommand(Lathe_command.Stepper_Command_Motor_forward, ns_device.LATHE_MOTOR, value);
            await ProcessQueue();
        }

        public async Task SetMotorReverseSpeed(ushort value)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage($"Method: SetMotorForwadSpeed(uint {value})");

            currentMotorSpeed = value;
            await EnqueueCommand(Lathe_command.Stepper_Command_Motor_reverse, ns_device.LATHE_MOTOR, value);
            await ProcessQueue();
        }

        public async Task SetFanSpeed(ushort value)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage($"Method: SetFanSpeed(uint {value})");

            currentMotorSpeed = value;
            await EnqueueCommand(Lathe_command.Fan_Speed, ns_device.FAN, value);
            await ProcessQueue();
        }
        
        // private async Task SetProbeAngle(ushort value)
        // {
        //     if (_LOG_METHODS)
        //         await enqueuePCMessage($"Method: SetProbeAngle(uint {value})");
        //
        //     await EnqueueCommand(Lathe_command.Probe_command_Rotate, ns_device.NONE, value);
        //     await ProcessQueue();
        // }
        
        private async Task SetFanValue(ushort value)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage($"Method: SetFanAngle(uint {value})");

            await EnqueueCommand(Lathe_command.Fan_Speed, ns_device.FAN, value);
            await ProcessQueue();
        }        

        private async Task RequestRPM(ushort anRpm)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage($"Method: RequestRPM(uint {anRpm})");
            await Task.Run(async () =>
            {
                //refuse if it's too high
                if (anRpm > highestRPM)
                {
                    await logErrors("requested RPM too high");
                    return;
                }

                await EnqueueCommand(Lathe_command.Motor_command_Target_RPM, ns_device.LATHE_MOTOR, anRpm);
                await ProcessQueue();

            });
        }

        public async Task resendCommand(st_uart_tx command)
        {


            currentResendCommandCount++;
            if (currentResendCommandCount >= maxResendCommandRequests)
            {
                currentState = latheState.tooManyCommandResends;
            }
            else
            {
                if (_LONG_COMMS_ERRORS)
                {
                    await logLatheMessage($"PC Log : Lathe Requesting resend of command = {command}.");
                }
                await SendCommand(lastSentCommand);  
            }
            
        }

        public async Task<string> decodeCommandAbriviation(string abriv)
        {
            string result = "";
            await Task.Run( () =>
            {
                string result = "";
                switch (abriv)
                {
                    case "MSG_LATHE_RESPONSE_START":
                        result = "[LATHE_RESPONSE_START]";
                        break;
                    case "MSG_LATHE_RESPONSE_END":
                        result = "[LATHE_RESPONSE_END]";
                        break;
                    case "MSG_LATHE_ERROR_START":
                        result = "[LATHE_ERROR_START]";
                        break;
                    case "MSG_LATHE_DEBUG_START":
                        result = "[LATHE_DEBUG_START]";
                        break;
                    case "MSG_LATHE_LOG_MESSAGE_START":
                        result = "[LATHE_LOG_MESSAGE_START]";
                        break;
                    case "MSG_PC_MESSAGE_START":
                        result = "[PC_MESSAGE_START]";
                        break;
                    case "MSG_LATHE_COMMAND_START":
                        result = "[LATH_COMMAND_START]";
                        break;
                    default:
                        result = $"{abriv} = Uknown command abriviation ";
                        break;
                }

                
            });
            return result;
        }
    }
}

