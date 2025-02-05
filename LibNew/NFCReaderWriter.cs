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
using Lib;
namespace Lib
{
    public class NFCReaderWriter : BaseScanner
    {
        private string readerName;
        private ISCardContext context;
        private ISCardMonitor monitor;
        
        
        public int NoogGames;
        public int Duration;
        
       
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
                logger.LogError("No readers found.");
                isScannerActive = false;
                return;
            }

            isScannerActive = true;

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