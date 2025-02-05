using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Lib
{
    public class BaseScanner : IDisposable
    {
        protected HttpClient httpClient = null;
        public bool isScannerActive = false;
        public event EventHandler<string> StatusChanged;
        protected int numberOfPlayers;
        protected string gameStatus;
        public BaseScanner() { }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        protected virtual void OnStatusChanged(string newStatus)
        {
            StatusChanged?.Invoke(this, newStatus);
        }
        public virtual void OnGameStatusChanged(string newStatus)
        {
        }
        public virtual void OnNumberOfPlayersChanged(int num)
        {
        }
        public string InvalidateStatus(string uid)
        {
            string b = $"{{\"uid\":\"{uid}\", \"status\":\"V\",\"currentstatus\":\"R\",\"src\":\"{System.Environment.MachineName}\" }}";
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
        protected string ifPlayerHaveTime(string uid)
        {
            //string query = $"SELECT count(*) FROM [dbo].[WristbandTrans] WHERE wristbandCode = '{uid}' AND playerEndDate > GETDATE() and wristbandStatusFlag='R' ";
            logger.Log("calling service");
            var response = httpClient.GetAsync($"wristbandtran/validate?wristbandCode={uid}");

            //  logger.Log(response.Result);
            string result = response.Result.StatusCode == System.Net.HttpStatusCode.OK ? "" : "Error:Wristband Not in db!";
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
    }
}
