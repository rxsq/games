using System.Net.WebSockets;
using System.Text;
using PCSC;
using PCSC.Iso7816;
using PCSC.Monitoring;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Runtime.Remoting.Contexts;
namespace Lib
{
    public class NFCReaderWriter : IDisposable
    {
        private string readerName;
        private ISCardContext context;
        private ISCardMonitor monitor;
        
        private HttpClient httpClient = null;
        public int NoogGames;
        public int Duration;
        
        public event EventHandler<string> StatusChanged;
        protected virtual void OnStatusChanged(string newStatus)
        {
            StatusChanged?.Invoke(this, newStatus);
        }
       // Logger logger;// = new Logger("NFCReaderWriter.log");
        
        public NFCReaderWriter(string mode, string serverurl)
        {
            //this.logger = logger;
            process(mode, serverurl);
        }
       

        private void process(string mode, string serverurl)
        {
            httpClient = new HttpClient { BaseAddress = new Uri(serverurl) };
            var availableReaders = ContextFactory.Instance.Establish(SCardScope.System).GetReaders();
            if (availableReaders.Length == 0)
            {
                Console.WriteLine("No readers found.");
                return;
            }


            this.readerName = availableReaders[0];
            context = ContextFactory.Instance.Establish(SCardScope.System);

            monitor = new SCardMonitor(ContextFactory.Instance, SCardScope.System);
            monitor.CardInserted += (sender, args) =>
            {

                logger.Log($"Card inserted, processing...{args.ReaderName}");

                string uid = WriteData(args.ReaderName);
                if(uid.Length==0)
                {
                    OnStatusChanged($"{uid}:card could not be read please try again");
                    return;
                }
                logger.Log($"received card number...{uid}");
                string result = "";
                try
                {
                    if (!string.IsNullOrEmpty(uid))
                    {
                       
                        if (mode == "I")
                        {
                            result = "";
                        }
                        else if (mode == "R")
                        {
                            result = ifCardRegisted(uid);
                        }
                        else if (mode == "V")
                        {
                            result = ifPlayerHaveTime(uid);
                        }
                        logger.Log($"uuid:{uid} result:{result}");
                        
                    }
                }
                catch (Exception ex)
                {
                    result = ex.Message;
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
                OnStatusChanged($"{uid}:{result}");
            };
            monitor.Start(readerName);
           
        }
  

        public string updateStatus(string uid, string status, int playerid)
        {
            string b = $"{{\"uid\":\"{uid}\", \"status\":\"{status}\",\"currentstatus\":\"I\",\"src\":\"{System.Environment.MachineName}\",\"playerID\":{playerid} }}";
            var content = new StringContent(b, Encoding.UTF8, "application/json");

            try
            {
                var response = httpClient.PutAsync($"wristbandtran", content);
                return response.Result.IsSuccessStatusCode ? "" : "Error updating data into Database!";
            }
            catch (Exception ex)
            {
                logger.Log("An error occurred: " + ex.Message);
                return "Error communicating with API";
            }
           
        }
        private string ifPlayerHaveTime(string uid)
        {
            //string query = $"SELECT count(*) FROM [dbo].[WristbandTrans] WHERE wristbandCode = '{uid}' AND playerEndDate > GETDATE() and wristbandStatusFlag='R' ";
            logger.Log("calling service");
            var response = httpClient.GetAsync($"wristbandtran/validate?wristbandCode={uid}");

            //  logger.Log(response.Result);
            string result = response.Result.IsSuccessStatusCode ? "" : "Error:Wristband Not in db!";
            return result;

        }


        public string ifCardRegisted(string uid)
        {
            //  string query = $"SELECT count(*) FROM [dbo].[WristbandTrans] WHERE wristbandCode = '{uid}' AND WristbandTranDate > DATEADD(HOUR, -1, GETDATE()) and wristbandStatusFlag='I' ";
            var response = httpClient.GetAsync($"wristbandtran?wristbandcode={uid}&flag=I&timelimit=60");
            return response.Result.IsSuccessStatusCode ? "" : "Error:Wristband Not in db!";
           
        }

        public string InsertRecord(string uid, int count, double time)
        {
            DateTime totime = DateTime.Now.AddMinutes(time);
            string toTime = totime.ToString("yyyy-MM-dd HH:mm:ss");
            string playerStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Construct JSON content
            var jsonContent = $"{{\"uid\":\"{uid}\",\"status\":\"I\",\"gameType\":\"PixelPulse\",\"count\":{count},\"playerStartTime\":\"{playerStartTime}\", \"playerEndTime\":\"{toTime}\"}}";
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            logger.Log(jsonContent);
            try
            {
                // Perform the POST request synchronously
                var responseTask = httpClient.PostAsync("wristbandtran/create", content);
                responseTask.Wait(); // Blocks until the task completes

                var response = responseTask.Result; // Access the result
                var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult(); // Read the content synchronously

                Console.WriteLine(responseContent);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    return "";
                }
                else
                {
                    return $"Error: {response.StatusCode} - {responseContent}";
                }
            }
            catch (Exception ex)
            {
                logger.Log("An error occurred: " + ex.Message);
                return "Error communicating with API";
            }
        }

        private string WriteData(string readerName)
        {
            const int MaxRetries = 3;
            const int RetryDelayMilliseconds = 1000;

            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    using (var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any))
                    {
                        var apdu = new CommandApdu(IsoCase.Case2Short, reader.Protocol)
                        {
                            CLA = 0xFF,
                            Instruction = InstructionCode.GetData,
                            P1 = 0x00,
                            P2 = 0x00,
                            Le = 0x00 // Expecting full response
                        };

                        using (reader.Transaction(SCardReaderDisposition.Leave))
                        {
                            logger.Log("Sending APDU command to retrieve UID...");

                            var sendPci = SCardPCI.GetPci(reader.Protocol);
                            var receivePci = new SCardPCI();
                            var receiveBuffer = new byte[256];
                            var command = apdu.ToArray();

                            int bytesReceived = reader.Transmit(
                                sendPci,
                                command,
                                command.Length,
                                receivePci,
                                receiveBuffer,
                                receiveBuffer.Length);

                            if (bytesReceived > 0)
                            {
                                var responseApdu = new ResponseApdu(receiveBuffer, bytesReceived, IsoCase.Case2Short, reader.Protocol);

                                if (responseApdu.SW1 == 0x90 && responseApdu.SW2 == 0x00)
                                {
                                    string uid = BitConverter.ToString(responseApdu.GetData()).Replace("-", "");
                                    logger.Log($"UID retrieved successfully: {uid}");
                                    return uid;
                                }
                                else
                                {
                                    logger.Log($"APDU response indicates an error: SW1: {responseApdu.SW1:X2}, SW2: {responseApdu.SW2:X2}");
                                }
                            }
                            else
                            {
                                logger.Log("No bytes received from the card.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Log($"Exception occurred while reading UID: {ex.Message}");
                }

                // Wait before retrying
                Task.Delay(RetryDelayMilliseconds).Wait();
            }

            // After retries, if UID is not retrieved, return an empty string
            logger.Log("Failed to retrieve UID after multiple attempts.");
            return string.Empty;
        }


        public void Dispose()
        {
            monitor.Cancel();
            context.Dispose();
           
        }
    }

}