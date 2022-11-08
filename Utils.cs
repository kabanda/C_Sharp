using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
//using ImageClassLibrary;
using System.Windows;
using System.Windows.Controls;


namespace ns_main
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Return Stepper count for given mm length
        /// </summary>
        /// <param name="mm">mm to convert to stepper units</param>
        /// <returns>stepper counts</returns>
        public static uint MMX(double mm)
        {
            return (uint) (Math.Round((double)STEPS_PER_MMX * (double)mm));
        }
        
        /// <summary>
        /// Return Stepper count for given mm length
        /// </summary>
        /// <param name="mm">mm to convert to stepper units</param>
        /// <returns>stepper counts</returns>
        public static uint MMY(double mm)
        {
            return (uint) (Math.Round((double)STEPS_PER_MMY * (double)mm));
        }

        private static readonly Regex _regex = new Regex("[^0-9.-]+");

        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        public async Task getContactDistance(uint x)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("getContactDistance(uint x)");

            //const uint averages = 5;
            List<double> contactsList = new List<double>();

            if (!_DEBUGGING)
            {

                /*await GotoY(CuttingApproachDistanceY);

                for (int count = 0; count < averages; count++)
                {

                    await GotoX((uint) (x - (MMX(1) * count)));
                    await EnqueueCommand(Lathe_command.Stepper_Command_Aproach_Wood, ns_device.NONE);
                    await SetFan(50);
                   
                    await ProcessQueue();
                    await GotoY(CuttingApproachDistanceY, Stepper_resolution.stepHalf);
                    contactsList.Add(contactDeltaDistance);
                    await PauseMs(1000);
                }

                await GotoY(CuttingApproachDistanceY, Stepper_resolution.stepHalf);
                await ProcessQueue();
                //contactsList.OrderBy(c => c);
                var smallest = contactsList.Min();
                contactedDistance = (uint) (currentY + smallest); //use the smallest distance
                contactDistanceEstablished = true;*/
                contactedDistance = 5012; //5292;
                contactDeltaDistance = (uint) (contactedDistance-500);
                contactDistanceEstablished = true;
            }
            else
            {
                //Y_Offset = 10000;

                //const uint fakeContactDistance = 500;
                if (_LOG_GOTO_X)
                {
                    await enqueuePCMessage($"getContactDistance: 75 GotoX{x}");
                    await ProcessQueue();
                }

                await GotoX(x);
                await GotoY(CuttingApproachDistanceY - _Debug_Y_Offset);
                await ProcessQueue();
                contactDeltaDistance = _Debug_ContactDistance;
                contactedDistance = currentY + _Debug_ContactDistance;
                contactDistanceEstablished = true;

                await ProcessQueue();

                //SetFan(30);
            }

            Approach_TextBox.Dispatcher.Invoke(() =>
            {
                Approach_TextBox.Text = $"Cont.:{contactDeltaDistance.ToString()}";
            });


        }

        /// <summary>
        /// Cut a line from given x position to another given x position at given y
        /// two runs will be done at the same y but at different directions
        /// </summary>
        /// <param name="from_X"></param>
        /// <param name="to_X"></param>
        /// <param name="from_Y"></param>
        /// <param name="resolution">optional</param>
        public async Task CutLatheLine(uint from_X, uint to_X, uint from_Y,
            Stepper_resolution resolution = Stepper_resolution.stepThirtySecond)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage(
                    "CutLatheLine(uint from_X,uint to_X, uint from_Y,Stepper_resolution resolution = Stepper_resolution.stepThirtySecond))");

            if (_LOG_GOTO_X)
            {
                await enqueuePCMessage($"CutLatheLine: 75 GotoX{from_X}");
                await ProcessQueue();
            }
            await GotoX(from_X, resolution);
            await GotoY(from_Y, resolution);
            if(_LOG_GOTO_X)
            {
                await enqueuePCMessage($"CutLatheLine: 115 GotoX{to_X}");
                await ProcessQueue();
            }
            await GotoX(to_X, resolution);
            await GotoX(from_X, resolution);

            await ProcessQueue();
        }

        /// <summary>
        /// Cut out a series of lines
        /// </summary>
        /// <param name="from_X"></param>
        /// <param name="to_X"></param>
        /// <param name="from_Y"></param>
        /// <param name="to_Y"></param>
        /// <param name="Y_increment"></param>
        /// <param name="resolution">optional</param>
        /// 
        private uint debugUint;
        public async Task CutLatheLines(uint from_X, uint to_X, uint from_Y, uint to_Y, uint Y_increment,Stepper_resolution resolution = Stepper_resolution.stepThirtySecond)
        {
            if (_LOG_METHODS)
                await enqueuePCMessage("CutLatheLines(uint from_X, uint ...");

            uint currentCutY;
            Lathing_ProgressBar.Maximum = to_Y - from_Y;
            for (currentCutY = from_Y; currentCutY <= to_Y; currentCutY += Y_increment)
            {
                debugUint = currentCutY;
                await CutLatheLine(currentX, to_X, (uint) (currentCutY), resolution);
                Lathing_ProgressBar.Value = currentCutY - from_Y;
                await enqueuePCMessage($"currentCutY = {currentCutY.ToString()}");
            }

            //await ProcessQueue();
            await enqueuePCMessage($"Finished lines ...currentCutY = {currentCutY.ToString()}");
        }

        /// <summary>
        /// Process all the commands that are currently on the queue
        /// Does not return until the entire queue is processed
        /// On return the que is  empty.
        /// </summary>
        /// <returns>with queue empty</returns>
        public async Task ProcessQueue()
        {
            if (CommandQueue == null || CommandQueue.Count == 0)
                return;

            if (_LOG_METHODS)
                await enqueuePCMessage("ProcessQueue()");

            if (inTest)
            {
                var peek = CommandQueue.Peek() as string;
                Lathe_command cmd = await ExtractCommand(peek);
                if (cmd != Lathe_command.Lathe_command_Status)
                {
                    CommandQueue.Clear();
                    await LogPC($"Attemted to process queue while in test mode with command {cmd}");
                    return;
                }
            }

            // await EnqueueCommand(Lathe_command.Lathe_command_Status, ns_device.NONE);

            QueueProcessStartTime = DateTime.Now;

            while (CommandQueue.Count != 0)
            {
               
 //               var nextCommand = (string) CommandQueue.Dequeue();

//                var tag = ExtractTag(nextCommand);
                

                var nextCommand = (st_uart_tx) CommandQueue.Dequeue();

                if (nextCommand.device ==  ns_device.PC) //pc message for marking things etc.
                {
                    logPC_lf();
                    await logPCMessage(nextCommand.text);
                    logPC_lf();
                    continue;
                }

                var command = nextCommand.command; //await ExtractCommand(nextCommand);

                switch (command)
                {
                    case Lathe_command.Lathe_Command_PauseMs:
                        var param_1 = nextCommand.command_param_1; //await ExtractValue(nextCommand);
                        var id = nextCommand.command_id;//await ExtractCommandID(nextCommand);
                        await logPCMessage($"id: {id.ToString()}, Pausing for  {param_1.ToString()}  MilliSeconds ");
                        _lf();

                        if (param_1 >= 0)
                            Thread.Sleep(param_1);
                        break;

                    case Lathe_command.Lathe_command_Pause:
                        await Pausing();
                        break;

                    case Lathe_command.Lathe_command_MessageBox:
                        await awaitMessageBox(nextCommand.text);
                        break;

                    default:
                        //var deb = await DecodeCommand(nextCommand);
                        //$$$see if not to show status commands


                        currentResendCommandCount = 0;
                        
                        await SendCommand(nextCommand);
                        
                        if ( !(command == Lathe_command.Lathe_command_Status && _LOG_STATUS == false))
                            await LogPC($"Sent:   {await DecodeCommand(nextCommand)} ");                        
                        
                        await Task.Run(() =>
                        {
                            //tight loop for waiting for lathe response
                            //exited only if current state changes from:
                            //latheState.sentCommand but NOT into latheState.latheBusy which
                            //will persist in this loop until too many busy messages or too many
                            //requests form PC for a command resend
                            //while (currentState is latheState.sentCommand or latheState.latheBusy)
                            while(currentState==latheState.sentCommand /*|| currentState==latheState.normal*/)
                            {
                                Thread.Yield();
                                Thread.Sleep(200);
                            }
                        });
                        _lf();
                        break;
                }
            }

            await logDuration("Queue processing", QueueProcessStartTime);
        }

        private async Task Pausing()
        {
            {
                if (_LOG_METHODS)
                    await enqueuePCMessage("Pausing()");


                _PAUSED = true;
                await Task.Run(() =>
                {
                    while (_PAUSED)
                    {

                    }
                });
            }
        }

        /// <summary>
        /// Set resolution of the stepper steps
        /// </summary>
        /// <param name="resolution"></param>
        public async Task SetResolution(Stepper_resolution resolution)
        {
                
            await EnqueueCommand(Lathe_command.Stepper_Command_Resolution, ns_device.NONE, (ushort) resolution);
        }

        public async Task SetDirection(Stepper_direction direction)
        {
            await EnqueueCommand(Lathe_command.Stepper_Command_Direction, ns_device.NONE, (ushort) direction);
        }

        /// <summary>
        /// Provide a printable string of a command that is on/from the queue
        /// </summary>
        /// <param name="command">queue command</param>
        /// <returns>printable string of command</returns>
        public async Task<string> DecodeCommand(st_uart_tx command)
        {
            var id =command.command_id;
            var cmd = (Lathe_command) command.command;
            var stepper = (ns_device) command.device;
            var param_1 = command.command_param_1;
            var param_2 = command.command_param_2;

            //var decodedAbriviation = await decodeCommandAbriviation(splits[0]);
            var result =
                //$"tag: {splits[0]}={decodedAbriviation} id: {id},  command:   >>{cmd.ToString()}<<,   stepper: {stepper},  value: {DecodeValue(cmd, value)}";
                $"id: {id.ToString()},  command:   >>{cmd.ToString()}<<,   stepper: {stepper},  param_1: {param_1.ToString()}, param_2: {param_2.ToString()}";

            return result;
        }

        public async Task<Lathe_command> ExtractCommand(string latheCommand)
        {
            // if (_LOG_METHODS)
            //     enqueuePCMessage("ExtractCommand(string latheCommand)");
            //
            var splits = latheCommand.Split('$');
            if ((splits[0] != MSG_LATHE_COMMAND_START) && (splits[0] != MSG_LATHE_RESPONSE_START))
            {
                await logPCError($"ExtractCommand: {splits[0]} is not a valid command string. ");
                return Lathe_command.Lathe_command_No_Command;
            }

            var command = Int32.Parse(splits[2]);
            return (Lathe_command) command;
        }

        /// <summary>
        /// Extract the value field from a command or reponse string
        /// </summary>
        /// <param name="command"></param>
        /// <returns>command or resonse string</returns>
        public async Task<Int32> ExtractValue(string latheCommand)
        {
            // if (_LOG_METHODS)
            //     enqueuePCMessage("ExtractValue(string latheCommand)");

            var splits = latheCommand.Split('$');
            if ((splits[0] != MSG_LATHE_COMMAND_START) && (splits[0] != MSG_LATHE_RESPONSE_START))
            {
                await logPCError($"ExtractCommand: {splits[0]} is not a valid command string. ");
                return -1;
            }

            var value = Int32.Parse(splits[4]);
            return value;
        }

        public async Task<string> ExtractMessage(string latheCommand)
        {
            // if (_LOG_METHODS)
            //     enqueuePCMessage("ExtractMessage(string latheCommand)");

            var splits = latheCommand.Split('$');
            if ((splits[0] != "MSG_LATHE_COMMAND_START") && (splits[0] != "MSG_LATHE_RESPONSE_START"))
            {
                await logPCError($"ExtractCommand: {splits[0]} is not a valid command string. ");
                return null;
            }

            var value = splits[5];
            return value;
        }

        /// <summary>
        /// Extract the ID field from a command or reponse string
        /// </summary>
        /// <param name="command"></param>
        /// <returns>command or resonse string</returns>
        public async Task<uint> ExtractCommandID(string command)
        {
            // var cmd = $"ExtractCommandID(string command = {command})";
            // cmd = cmd.Replace('$', '|');
            // var tag = command.Substring(0, 3);
            // var tagString = await decodeCommandAbriviation(tag);
            // cmd = cmd.Insert(37,"="+tagString+"!!!!!");
            // if (_LOG_METHODS)
            //     await enqueuePCMessage(cmd);

            var splits = command.Split('$');
            uint result = 0;
            switch (splits[0])
            {
                case MSG_LATHE_RESPONSE_START:
                case MSG_LATHE_RESPONSE_END:
                case MSG_LATHE_ERROR_START:
                case MSG_LATHE_DEBUG_START:
                case MSG_LATHE_LOG_MESSAGE_START:
                case MSG_PC_MESSAGE_START:
                case MSG_LATHE_COMMAND_START:
                case LATHE_REQUEST_RESENT_CMD_START:
                case LATHE_BUSY_START:
                case LATHE_KILL_RECOVER_START:                        
                        
                    try
                    {
                        uint.TryParse(splits[1], out result);
                    }
                    catch (Exception e)
                    {
                        await logPCError($"ExtractCommandID: {e} (command = {command}), is not a valid command ID.  ");
                        result = 0;
                    }
                    break;

                case "":
                    result = 0;
                    break;
                    
                default:
                    await logPCError($"ExtractCommandID: command = {command}, token= {splits[0]} is not a valid command token. ");
                    result = int.MaxValue;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Extract the Tag field from a command or reponse string
        /// </summary>
        /// <param name="command"></param>
        /// <returns>command or resonse string</returns>
        public String ExtractTag(string command)
        {
            // if (_LOG_METHODS)
            //     enqueuePCMessage("ExtractTag(string command)");

            var splits = command.Split('$');
            return splits[0];
        }

        /// <summary>
        /// currently used as a decoder to return a printable string 
        /// for response string from lathe
        /// </summary>
        /// <param name="resp"></param>
        /// <returns></returns>
        public async Task<string> DecodeResponse(string resp)
        {
            // if (_LOG_METHODS)
            //     enqueuePCMessage("ExtractTag(string command)");

            var splits = resp.Split('$');
            //if (splits.Length != 10)
            //{
            //    await commsError($"Wrong response message length {splits.Length}: '{resp}' ");
            //    return null;
            //}

            if (splits[8] != MSG_LATHE_RESPONSE_END)
            {
                await commsError($"DecodeResponse: {splits[4]} is not a valid response token. ");
                return null;
            }

            //var t = splits[1].GetType();

            var msgId = splits[1];
            //check for synchronization error

            UInt16 sensors;

            if (splits[2] == "")
                splits[2] = "8";
            sensors = Convert.ToUInt16(splits[2]);

            var xS = ((sensors & 1) != 0) ? "ON" : "OFF";
            var yS = ((sensors & 2) != 0) ? "ON" : "OFF";
            var contact = ((sensors & 4) != 0) ? "MADE" : "NONE";
            var v = sensors & 4;
            var f = splits[3];

            var result = $"id: {msgId},  X stop: {xS},   Y stop: {yS},  contact {contact}, value: {v},  Fan: {f} X: {splits[4]}, Y:{splits[5]}, Tacho:{splits[6]}";
            return result;
        }

        /// <summary>
        /// currently only used as a decoder to return a printable string 
        /// for approach response string from lathe
        /// </summary>
        /// <param name="resp"></param>
        /// <returns>response string to decode</returns>
        public async Task<string> DecodeResponse2(string resp)
        {
            var splits = resp.Split('$');
            if (splits.Length != 7)
            {
                await commsError($"Wrong response message length {splits.Length}: '{resp}' ");
                return null;
            }

            if (splits[5] != "MSG_LATHE_RESPONSE_START2E")
            {
                await commsError($"DecodeResponse: {splits[5]} is not a valid response token. ");
                return null;
            }

            //var t = splits[1].GetType();

            var msgId = splits[1];
            //check for synchronization error

            UInt16 sensors;
            UInt16 contactDist;

            if (splits[2] == "")
                splits[2] = "8";
            sensors = Convert.ToUInt16(splits[2]);
            contactDist = Convert.ToUInt16(splits[4]);

            var xS = ((sensors & 1) != 0) ? "ON" : "OFF";
            var yS = ((sensors & 2) != 0) ? "ON" : "OFF";
            var contact = ((sensors & 4) != 0) ? "MADE" : "NONE";
            var v = sensors & 4;
            var f = splits[3];


            var result = $"id: {msgId}, Fan: {f} dist: {contactDeltaDistance} ";
            return result;
        }

        private void logPC_lf()
        {
            if (_LOG_MESSAGES)
            {
                response_ListBox.Dispatcher.Invoke(() =>
                {
                    response_ListBox.Items.Add($" ");
                    response_ListBox.SelectedIndex = response_ListBox.Items.Count - 1;
                });
            }
        }


        private async Task enqueuePCMessage(string msg)
        {
            await Task.Run( async () =>
            {
                if (CommandQueue != null)
                {
                    string queueCommand =
                        $"MSG_PC_MESSAGE_START$                        ///////////// {msg} ////////////$PME$".ToString();
                    CommandQueue.Enqueue(queueCommand);
                }
            });
        }

        private async Task logLatheMessage(string msg)
        {
            await Task.Run(() =>
            {
                if (_LOG_MESSAGES)
                {
                    response_ListBox.Dispatcher.Invoke(() =>
                    {
                        response_ListBox.Items.Add($"{msg} {TimeStamp()}");
                        response_ListBox.SelectedIndex = response_ListBox.Items.Count - 1;
                    });
                }
            });
        }

        private async Task logLatheDebugMessage(string msg)
        {
            await Task.Run(() =>
            {
                response_ListBox.Dispatcher.Invoke(() =>
                {
                    response_ListBox.Items.Add($"{msg} {TimeStamp()}");
                    response_ListBox.SelectedIndex = response_ListBox.Items.Count - 1;
                });
            });
        }

        private void _lf()
        {
            if (_LOG_MESSAGES)
            {
                response_ListBox.Dispatcher.Invoke(() =>
                {
                    response_ListBox.Items.Add(" ");
                    response_ListBox.SelectedIndex = response_ListBox.Items.Count - 1;
                });

                //using (myLogFile = File.AppendText(logFile))
                //{
                //    myLogFile.WriteLineAsync("\n");
                //}
            }
        }


        private async Task logInt(string prompt, int anInt)
        {
            await logPCMessage($"Debug int:  {prompt} = {anInt.ToString()} ");
        }

        private async Task LogPC(string log)
        {
            if (!_LOG_MESSAGES)
                return;
                
            await Task.Run(() =>
            {
                var msg = $"PC log: {log}";
                response_ListBox.Dispatcher.Invoke(() =>
                {
                    response_ListBox.Items.Add($"{msg} {TimeStamp()}");
                    response_ListBox.SelectedIndex = response_ListBox.Items.Count - 1;

                });

                //using (myLogFile = File.AppendText(logFile))
                //{
                //    await myLogFile.WriteLineAsync($"{msg} {TimeStamp()}");
                //}	
            });
        }

        private async Task LogLathe(string log)
        {
            if (!_LOG_MESSAGES)
                return;
            await logPCMessage(
                $"Lathe log: {log}   duration: >>{commandDuration.Seconds}.{commandDuration.Milliseconds}sec<< ");
        }

        private async Task logDuration(String anEvent, DateTime startTime)
        {
            if (!_LOG_MESSAGES)
                return;
                
            await Task.Run(async () =>
            {
                TimeSpan duration = DateTime.Now - startTime;

                //await logPCMessage("\n");
                await LogPC(
                    $"[[>>{anEvent} took >>{duration.Hours}hr:{duration.Seconds}sec:{duration.Milliseconds}ms<<]]");
                _lf();
            });
        }

        private async Task logErrors(string err)
        {
            if(_LOG_ERRORS)
                await Task.Run(() =>
                {
                    response_ListBox.Dispatcher.Invoke(() =>
                    {
                        response_ListBox.Items.Add($"{err} {TimeStamp()}");
                        response_ListBox.SelectedIndex = response_ListBox.Items.Count - 1;

                    });

                    //using (myLogFile = File.AppendText(logFile))
                    //{
                    //    await myLogFile.WriteLineAsync($"{err} {TimeStamp()}");
                    //}
                });
        }

        private async Task logPCError(string err)
        {
            // await logPCMessage($"ERROR: PC says, {err} ");

            await logErrors(err);
        }

        private async Task logLatheError(string err)
        {
            await Task.Run(async () =>
            {
                if (_LOG_METHODS)
                    await enqueuePCMessage("logLatheError(string err)");

                await logErrors($"ERROR: Lathe says, {err} ");
            });
        }

        private async Task commsError(string msg)
        {

            await logErrors($"Comms Error: {msg} ");
        }
            
        public static (int, string, string) GetTrace()
        {
            StackTrace stackTrace = new StackTrace(true);
            StackFrame sf = stackTrace.GetFrame(1);

            var methodName = sf.GetMethod().Name;
            var fileName = sf.GetFileName();
            var lineNumber = sf.GetFileLineNumber();

            return (lineNumber, methodName, fileName);
        }

        public string getAllTraceString()
        {
            var result = GetTrace();
            return $" line:{result.Item1}, method:{result.Item2}, fileName:{result.Item3}";
        }

        public uint getHighestModel_Y()
        {
            var highest = DepthList.OrderByDescending(d => d.Y).FirstOrDefault();
            return (uint) highest.Y;
        }

        public uint getModelHeight()
        {
            return (uint) ImageCanvas.Height;
        }

        private void ChangeTabTo(TabItem wantedTab)
        {
            Tab_Control.SelectedItem = wantedTab;

        }

        public bool X_isHomed()
        {
            return latheStatus.xStop;
        }

        public bool Y_isHomed()
        {
            return latheStatus.yStop;
        }

    }
}