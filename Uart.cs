using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ns_main
{
    public partial class MainWindow : Window
    {
        public async Task SendCommand(st_uart_tx command)
        {
            // await Task.Run(() =>
            // {
            //     while (currentState == latheState.sentCommand)
            //     {
            //         Thread.Yield();
            //         Thread.Sleep(20);
            //     }
            // });

            Dispatcher.Invoke(() => { response_ListBox.Items.Add(">>"); });

            if (_fpga_serial != null)
            {
                _fpga_serial.DiscardOutBuffer();
                _fpga_serial.DiscardInBuffer();
                byte[] serialOutputFrame = new byte[12];


                serialOutputFrame[0] = SERIAL_FRAME_HEADER_LOW;
                serialOutputFrame[1] = SERIAL_FRAME_HEADER_HIGH;
                serialOutputFrame[2] = (byte) (command.command_id & 0xff);
                serialOutputFrame[3] = (byte) ((command.command_id >> 8) & 0xff);
                serialOutputFrame[4] = (byte) (command.command);
                serialOutputFrame[5] = (byte) (command.device);
                serialOutputFrame[6] = (byte) (command.command_param_1 & 0xff);
                serialOutputFrame[7] = (byte) ((command.command_param_1 >> 8) & 0xff);
                serialOutputFrame[8] = (byte) (command.command_param_2 & 0xff);
                serialOutputFrame[9] = (byte) ((command.command_param_2 >> 8) & 0xff);
                serialOutputFrame[10] = SERIAL_FRAME_FOOTER_LOW;
                serialOutputFrame[11] = SERIAL_FRAME_FOOTER_HIGH;

                string outputString = string.Format(
                    "header={0:X}, command_id={1:X}, command={2,2:X} device={3,2:X}, param_1={4:X}, param_2={5:X}, footer ={6,4:X}",
                    serialOutputFrame[0]+(serialOutputFrame[1]*256), //header
                    serialOutputFrame[2]+(serialOutputFrame[3]*256), //command_id
                    serialOutputFrame[4], //command
                    serialOutputFrame[5], //device
                    serialOutputFrame[6]+(serialOutputFrame[7]*256), //param1
                    serialOutputFrame[8]+(serialOutputFrame[9]*256), //param2
                    serialOutputFrame[10]+(serialOutputFrame[11]*256)  //footer
   
                    );
                
                response_ListBox.Dispatcher.Invoke(() =>
                {
                    response_ListBox.Items.Add(outputString);
                });
                
                _fpga_serial.Write(serialOutputFrame, 0, 12);
                commandPending = true;
                currentState = latheState.sentCommand;

                if (!(command.command == Lathe_command.Comms_command_Request_Resend_Response))
                {
                    lastSentMessageId = command.command_id;
                }
            }

            latheBusyCounter = 0;

            commandSentTime = DateTime.Now;
        }

        public static void Read()
        {
            while (_continue)
            {
                try
                {
                    string message = _serialPort.ReadLine();
                }
                catch (TimeoutException)
                {
                }
            }
        }

        // async Task sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        // {
        //     Dispatcher.Invoke(() => { response_ListBox.Items.Add("<<"); });
        //
        //     string data;
        //     try
        //     {
        //         data = _serialPort.ReadLine();
        //     }
        //     catch (Exception)
        //     {
        //         //await logPCError("Received a null message from Serial In");
        //         await RequestReSendResponse($"Received a null message.");
        //         return;
        //     }
        //
        //     _serialPort.DiscardInBuffer();
        //     _serialPort.DiscardOutBuffer();
        //
        //
        //     var splits = data.Split('$');
        //     string crcString;
        //
        //     //check CRC
        //     try
        //     {
        //         crcString = splits[splits.Length - 2];
        //     }
        //     catch (Exception)
        //     {
        //         await RequestReSendResponse($"CRC check failed {data}");
        //         return;
        //     }
        //
        //     uint intCrc;
        //
        //     if (uint.TryParse(crcString, out intCrc) && data.Length > 21)
        //     {
        //         string dataPotion = data.Remove(data.Length - 7); //strip off the check sum 
        //         //dataPotion += '\n';
        //         byte[] byteResponse = Encoding.ASCII.GetBytes(dataPotion);
        //         uint crc = Crc16Ccitt(byteResponse);
        //         if (crc != intCrc)
        //         {
        //             await RequestReSendResponse($"CRC check failed {data}");
        //             return;
        //         }
        //     }
        //     else
        //     {
        //         await RequestReSendResponse($"CRC parsing failed {data}");
        //         return;
        //     }
        //
        //
        //     uint newID = await ExtractCommandID(data);
        //     var lastID = lastSentMessageId;
        //
        //     UInt16 sensors;
        //     string fan;
        //     BrushConverter bc;
        //
        //     bool synchError = ((newID != lastID));
        //     switch (splits[0])
        //     {
        //         case LATHE_BUSY_START: //Status new
        //         {
        //             currentResendResponseCount = 0;
        //             currentState = latheState.latheBusy;
        //             latheBusyCounter += latheHeartbeatInterval;
        //             if (latheBusyCounter >= allowedLatheBusyTime)
        //             {
        //                 currentState = latheState.responseTooLate;
        //             }
        //
        //             break;
        //         }
        //
        //         case LATHE_KILL_RECOVER_START: //Lathe has just recovered from a hardware kill!
        //         {
        //             currentResendResponseCount = 0;
        //             currentState = latheState.latheRecoveredFromKill;
        //             await processLatheRecoverKill();
        //             break;
        //         }
        //
        //         case LATHE_REQUEST_RESENT_CMD_START:
        //         {
        //             //await LogLathe($"Lathe request resend of last command = {lastSentCommand}");
        //             await resendCommand(lastSentCommand);
        //             return;
        //         }
        //
        //         case MSG_LATHE_RESPONSE_START:
        //             if (splits[8] != MSG_LATHE_RESPONSE_END)
        //             {
        //                 await RequestReSendResponse($"Response mulformed {data}");
        //                 return;
        //             }
        //
        //             commandDuration = (DateTime.Now - commandSentTime); //"HH:mm:ss:fff"
        //             await LogLathe($"Received response from PSoC: {await DecodeResponse(data)}");
        //
        //             newID = await ExtractCommandID(data);
        //             lastID = lastSentMessageId;
        //             synchError = ((newID != lastID));
        //
        //             if (synchError)
        //             {
        //                 //To_Do
        //                 //await commsError($"Id Synch: Sent: {lastSentMessageId} ,  Received: {newID}  " );
        //                 //return;
        //             }
        //
        //
        //             sensors = Convert.ToUInt16(splits[2]);
        //             X_isHomed = ((sensors & 1) != 0) ? true : false;
        //             Y_isHomed = ((sensors & 2) != 0) ? true : false;
        //             contacting = ((sensors & 4) != 0) ? true : false;
        //             sw_1 = ((sensors & 8) != 0) ? true : false;
        //             sw_2 = ((sensors & 16) != 0) ? true : false;
        //
        //             fan = splits[3];
        //             Int32.TryParse(splits[4], out latheCurrentX);
        //             Int32.TryParse(splits[5], out latheCurrentY);
        //             double.TryParse(splits[6], out currentRpmValue);
        //             double.TryParse(splits[7], out ultrasonicDistance);
        //
        //             bc = new BrushConverter();
        //
        //             if (CommandQueue.Count == 0)
        //                 commandPending = false;
        //
        //
        //             Utrasonic_Distance_TextBox.Dispatcher.Invoke(() =>
        //             {
        //                 var realDistance = ultrasonic.addNew(ultrasonicDistance / 58.0) * 10;
        //                 ultrasonicDistance = realDistance;
        //                 var rd = String.Format("{0:0.00}", realDistance);
        //                 Utrasonic_Distance_TextBox.Text = rd;
        //             });
        //
        //             MotorSpeed_TextBox.Dispatcher.Invoke(() =>
        //             {
        //                 var rpmDisplay = String.Format("{0:0.0}", rpmRingBuffer.addNew(currentRpmValue));
        //                 Motor_RPM_TextBox.Text = rpmDisplay;
        //             });
        //
        //             X_TextBox.Dispatcher.Invoke(() =>
        //             {
        //                 if (!X_isHomed())
        //                     xStop.Background = (Brush) bc.ConvertFrom("#FF1C2E16");
        //                 else
        //                     xStop.Background = (Brush) bc.ConvertFrom("#FFFF0000");
        //             });
        //
        //             Y_TextBox.Dispatcher.Invoke(() =>
        //             {
        //                 if (!Y_isHomed())
        //                     yStop.Background = (Brush) bc.ConvertFrom("#FF1C2E16");
        //                 else
        //                     yStop.Background = (Brush) bc.ConvertFrom("#FFFF0000");
        //             });
        //
        //             Dispatcher.Invoke(() =>
        //             {
        //                 if (!contacting)
        //                     contact.Background = (Brush) bc.ConvertFrom("#FF1C2E16");
        //                 else
        //                     contact.Background = (Brush) bc.ConvertFrom("#FFFF0000");
        //             });
        //
        //             Dispatcher.Invoke(() =>
        //             {
        //                 if (!sw_1)
        //                     switch_1.Background = (Brush) bc.ConvertFrom("#FF1C2E16");
        //                 else
        //                     switch_1.Background = (Brush) bc.ConvertFrom("#FFFF0000");
        //             });
        //
        //             Dispatcher.Invoke(() =>
        //             {
        //                 Lathe_Current_X_TextBox.Text = splits[4];
        //                 Lathe_Current_Y_TextBox.Text = splits[5];
        //             });
        //
        //             // Dispatcher.Invoke(() =>
        //             // {
        //             //     MotorSpeed_TextBox.Text = splits[6];
        //             // });
        //             //Fan_Slider.Dispatcher.Invoke(() =>
        //             //{
        //             //    double Fv = Convert.ToDouble(fan);
        //             //    Fan_Slider.Value = Fv;
        //             //});
        //             _lf();
        //             //Thread.Sleep(200);
        //             currentState = latheState.receivedResponse;
        //
        //             break; //end case "MSG_LATHE_RESPONSE_START":
        //
        //         case MSG_LATHE_ERROR_START: //Error
        //             await logLatheError($"{splits[1]}");
        //             break;
        //
        //         case MSG_LATHE_DEBUG_START:
        //             await logLatheDebugMessage($"Lathe DEBUG :  {splits[1]} ");
        //             break;
        //         case MSG_LATHE_LOG_MESSAGE_START:
        //             await logLatheMessage($"Lathe Log : {splits[1]} ");
        //             break;
        //
        //         case MSG_PC_MESSAGE_START: //PC message
        //             await logLatheMessage($"PC Log : {splits[1]} "); //not used at the moment
        //             break;
        //
        //         default:
        //             if (splits[0].Length > 0)
        //             {
        //                 if (!_LOG_ERRORS)
        //                     return;
        //                 var trace = GetTrace();
        //                 await logPCMessage($"Received an unknown command {splits[0]}. {getAllTraceString()} ");
        //             }
        //
        //             break;
        //     }
        // }

        async Task sp_fpga_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //public int Read (byte[] buffer, int offset, int count);
            byte[] serialInputFrame = new byte[SERIAL_INPUT_FRAME_LENGTH];
            Dispatcher.Invoke(() => { response_ListBox.Items.Add("<<"); });


            try
            {
                _fpga_serial.Read(serialInputFrame, 0, (int) SERIAL_INPUT_FRAME_LENGTH);
            }
            catch (Exception)
            {
                return;
            }

            //check the integrity of the block
            if ((serialInputFrame[0] != SERIAL_FRAME_HEADER_LOW) ||
                (serialInputFrame[1] != SERIAL_FRAME_HEADER_HIGH) ||
                (serialInputFrame[16] != SERIAL_FRAME_FOOTER_LOW) ||
                (serialInputFrame[17] != SERIAL_FRAME_FOOTER_HIGH)
            )
            {
                await logErrors("Invalid serial frame.");
            }
            else
            {
                foreach (var aByte in serialInputFrame)
                {
                    response_ListBox.Dispatcher.Invoke(() =>
                    {
                        string stringByte = String.Format("Got this: {0:X2}", aByte); //crc.ToString()
                        response_ListBox.Items.Add(stringByte);
                    });
                }
            }

            _fpga_serial.DiscardInBuffer();
            _fpga_serial.DiscardOutBuffer();
            

            uartRx.command_id = (ushort) (serialInputFrame[2] + (serialInputFrame[3] * 256));
            latheStatus.killRecover = (serialInputFrame[4] & (1 << (int) (en_lathe_status.KILL_RECOVER))) != 0;
            latheStatus.xStop = (serialInputFrame[4] & (1 << (int) (en_lathe_status.X_STOP))) != 0;
            latheStatus.yStop = (serialInputFrame[4] & (1 << (int) (en_lathe_status.Y_STOP))) != 0;
            latheStatus.forwardOverCurrent =
                (serialInputFrame[4] & (1 << (int) (en_lathe_status.FORWARD_OVERCURRENT))) != 0;
            latheStatus.reverseOverCurrent =
                (serialInputFrame[4] & (1 << (int) (en_lathe_status.REVERSE_OVERCURRENT))) != 0;
            uartRx.status = latheStatus;
            uartRx.device_busy_status = serialInputFrame[5];
            uartRx.motor_rpm = (UInt32) (serialInputFrame[6] + (serialInputFrame[7] * 256) + (serialInputFrame[8] * 65536)+ (serialInputFrame[9] * 16777216));
            uartRx.x_pos = (UInt32) (serialInputFrame[10] + (serialInputFrame[11] * 256) + (serialInputFrame[12] * 65536)+ (serialInputFrame[13] * 16777216));
            uartRx.y_pos = (UInt32) (serialInputFrame[14] + (serialInputFrame[15] * 256) + (serialInputFrame[16] * 65536)+ (serialInputFrame[17] * 16777216));
            uartRx.ultrasonic = (ushort) (serialInputFrame[18] + (serialInputFrame[19] * 256));
            
            ushort newID = uartRx.command_id;

            var lastID = lastSentMessageId;

            ushort sensors;
            string fan;
            BrushConverter bc;

            bool synchError = ((newID != lastID)); ;

            bc = new BrushConverter();

            if (CommandQueue.Count == 0)
                commandPending = false;


            Utrasonic_Distance_TextBox.Dispatcher.Invoke(() =>
            {
                var realDistance = ultrasonic.addNew(uartRx.ultrasonic / 58.0) * 10;
                ultrasonicDistance = realDistance;
                var rd = String.Format("{0:0.00}", realDistance);
                Utrasonic_Distance_TextBox.Text = rd;
            });

            MotorSpeed_TextBox.Dispatcher.Invoke(() =>
            {
                var rpmDisplay = String.Format("{0:0.0}", rpmRingBuffer.addNew(uartRx.motor_rpm));
                Motor_RPM_TextBox.Text = rpmDisplay;
            });

            X_TextBox.Dispatcher.Invoke(() =>
            {
                if (!X_isHomed())
                    xStop.Background = (Brush) bc.ConvertFrom("#FF1C2E16");
                else
                    xStop.Background = (Brush) bc.ConvertFrom("#FFFF0000");
            });

            Y_TextBox.Dispatcher.Invoke(() =>
            {
                if (!Y_isHomed())
                    yStop.Background = (Brush) bc.ConvertFrom("#FF1C2E16");
                else
                    yStop.Background = (Brush) bc.ConvertFrom("#FFFF0000");
            });

            Dispatcher.Invoke(() =>
            {
                if (!contacting)
                    contact.Background = (Brush) bc.ConvertFrom("#FF1C2E16");
                else
                    contact.Background = (Brush) bc.ConvertFrom("#FFFF0000");
            });

            Dispatcher.Invoke(() =>
            {
                if (!sw_1)
                    switch_1.Background = (Brush) bc.ConvertFrom("#FF1C2E16");
                else
                    switch_1.Background = (Brush) bc.ConvertFrom("#FFFF0000");
            });

            Dispatcher.Invoke(() =>
            {
                Lathe_Current_X_TextBox.Text = uartRx.x_pos.ToString();
                Lathe_Current_Y_TextBox.Text = uartRx.y_pos.ToString();
            });

            Dispatcher.Invoke(() =>
            {
                MotorSpeed_TextBox.Text = uartRx.motor_rpm.ToString();
            });
            
            // Fan_Slider.Dispatcher.Invoke(() =>
            // {
            //     double Fv = Convert.ToDouble(fan);
            //     Fan_Slider.Value = Fv;
            // });
            _lf();
            //Thread.Sleep(200);
            currentState = latheState.receivedResponse;

           
        }


        private async Task logPCMessage(string msg)
        {
            await Task.Run(() =>
            {
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


        private void si_DataReceived(string data)
        {
            response_ListBox.Dispatcher.Invoke(() =>
            {
                response_ListBox.Items.Add(data);
                response_ListBox.SelectedIndex = response_ListBox.Items.Count - 1;
                response_ListBox.ScrollIntoView(response_ListBox.SelectedItem);
            });
            // _serialPort.DiscardInBuffer();
        }

        private async Task RequestReSendResponse(string reason)
        {
            if (currentState == latheState.tooManyCommandResends)
            {
            }
            else
            {
                currentResendResponseCount++;
                if (currentResendResponseCount >= maxResendResponseRequests)
                {
                    currentState = latheState.tooManyResponseResends;
                }
                else
                {
                    if (_LONG_COMMS_ERRORS)
                    {
                        await logLatheMessage($"PC Log : Requesting Lathe to resend response. reason: {reason}");
                    }

                    st_uart_tx command = default;

                    command.command = Lathe_command.Comms_command_Request_Resend_Response;
                    command.command_id = lastSentMessageId;
                    command.device = (byte) ns_device.NONE;
                    //string request = $"RES${lastSentMessageId}${command.command}${stp}$REE$".ToString();

                    Thread.Sleep(200); // give lath a brake to sort itself out
                    await SendCommand(command);
                }
            }
        }

        public async Task processLatheRecoverKill()
        {
        }
// private async Task logSerialOut(string data)
// {
//     //public String DecodeValue(Lathe_command command, int value)
//     //public async Task<string> DecodeResponse(string resp)
//     //public async Task<string> DecodeCommand(string command)
//     //public async Task<string> DecodeResponse2(string resp)
// }
//
// private async Task logSerialIn(string data)
// {
// }
    }
}