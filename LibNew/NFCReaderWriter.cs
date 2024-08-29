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
        AsyncLogger logger;// = new AsyncLogger("NFCReaderWriter.log");
        
        public NFCReaderWriter(string mode, string serverurl, AsyncLogger logger)
        {
            this.logger = logger;
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
                logger.Log($"received card number...{uid}");
                try
                {
                    if (!string.IsNullOrEmpty(uid))
                    {
                        string result = "";
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
                        Console.WriteLine(result);
                        OnStatusChanged(result.Length == 0 ? uid : "");
                        // SendUidToWebSocket(uid).Wait();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }

            };
            monitor.Start(readerName);
            //while (true)
            //{
            //    Thread.Sleep(1000); // Sleep to reduce CPU usage, adjust as needed.
            //}
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
            try
            {
                using (var r = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any))
                {
                    var apdu = new CommandApdu(IsoCase.Case2Short, r.Protocol)
                    {
                        CLA = 0xFF,
                        Instruction = InstructionCode.GetData,
                        P1 = 0x00,
                        P2 = 0x00,
                        Le = 0
                    };

                    using (r.Transaction(SCardReaderDisposition.Leave))
                    {
                        Console.WriteLine("Retrieving the UID .... ");
                        var sendPci = SCardPCI.GetPci(r.Protocol);
                        var receivePci = new SCardPCI();

                        var receiveBuffer = new byte[256];
                        var command = apdu.ToArray();

                        var bytesReceived = r.Transmit(
                            sendPci,
                            command,
                            command.Length,
                            receivePci,
                            receiveBuffer,
                            receiveBuffer.Length);

                        var responseApdu = new ResponseApdu(receiveBuffer, bytesReceived, IsoCase.Case2Short, r.Protocol);
                        logger.Log($"SW1: {responseApdu.SW1:X2}, SW2: {responseApdu.SW2:X2}\nUid: {BitConverter.ToString(responseApdu.GetData())}");
                        return BitConverter.ToString(responseApdu.GetData()).Replace("-", "");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            return "";
        }

       
        public void Dispose()
        {
            monitor.Cancel();
            context.Dispose();
           
        }
    }

}