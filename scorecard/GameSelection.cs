using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Threading;
using scorecard.lib;
using System.Net.Http;
using System.Security.Cryptography;
using RestSharp;
using System.Text.Json;
namespace scorecard
{
    public partial class GameSelection : Form
    {
        private ScorecardForm scorecardForm;
        List<Player> players = new List<Player>();
        List<Player> Waitingplayers = new List<Player>();
        private string gameVarient;
        public GameSelection()
        {
           
        InitializeComponent();
            StartCheckInTimer();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;

            InitializeWebView();

            SetBrowserFeatureControl();
            InitializeScorecardForm();
            Lib.NFCReaderWriter readerWriter = new Lib.NFCReaderWriter("V", ConfigurationSettings.AppSettings["server"]);
            webView2.Source = new Uri(ConfigurationSettings.AppSettings["gameurl"])  ;
            // webView2.Visibility = Visibility.Visible;
            readerWriter.StatusChanged += (s, uid) =>
            {
                if (uid.Length > 0)
                {
                    //wristbandCode, playerStartTime, playerEndTime, gameType, points, LevelPlayed
                    Waitingplayers.Add(new Player { wristbandCode = uid, CheckInTime = DateTime.Now });
                    if (webView2.InvokeRequired)
                    {
                        webView2.Invoke(new Action(() =>
                            webView2.CoreWebView2.ExecuteScriptAsync($"window.receiveMessageFromWPF('{uid}')")
                        ));
                    }
                    else
                    {
                        webView2.CoreWebView2.ExecuteScriptAsync($"window.receiveMessageFromWPF('{uid}')");
                    }


                }
            };

        }
        private System.Windows.Forms.Timer checkInTimer;

        private void StartCheckInTimer()
        {
            checkInTimer = new System.Windows.Forms.Timer();
            checkInTimer.Interval = 300000; // 5 minutes
            checkInTimer.Tick += CheckInTimer_Tick;
            checkInTimer.Start();
        }

        private void CheckInTimer_Tick(object sender, EventArgs e)
        {
          //  if (!scorecardForm.currentGame.IsRunning)
            {
                if (Waitingplayers.FindAll(x => x.CheckInTime > DateTime.Now.AddMinutes(-5)).Count > 0)
                {
                    Waitingplayers.Clear();
                    RefreshWebView();
                }
            }
        }

        private void RefreshWebView()
        {
            if (webView2.InvokeRequired)
            {
                webView2.Invoke(new Action(() =>
                {
                    webView2.CoreWebView2.Reload();
                }));
            }
            else
            {
                webView2.CoreWebView2.Reload();
            }
        }
        private void InitializeScorecardForm()
        {
            // Find the secondary screen
            Screen[] screens = Screen.AllScreens;
            Screen secondaryScreen = screens.Length > 1 ? screens[1] : null;

            if (secondaryScreen != null)
            {
                scorecardForm = new ScorecardForm();
                
                // Set the position of the scorecard form to the secondary screen
                scorecardForm.StartPosition = FormStartPosition.Manual;
                scorecardForm.Location = new Point(secondaryScreen.Bounds.Left, secondaryScreen.Bounds.Top);
                scorecardForm.Size = new Size(secondaryScreen.Bounds.Width, secondaryScreen.Bounds.Height);

                // Show the scorecard form on the secondary screen
                scorecardForm.Show();
            }
            else
            {
                scorecardForm = new ScorecardForm();
                scorecardForm.Show();
                MessageBox.Show("Secondary monitor not detected.");
            }
        }
        private void InitializeWebView()
        {
            webView2.CoreWebView2InitializationCompleted += WebView2_CoreWebView2InitializationCompleted;
        }

        private void WebView2_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            webView2.CoreWebView2.WebMessageReceived += WebView2_WebMessageReceived;
        }

        private void WebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var message = e.TryGetWebMessageAsString();
            if (message.StartsWith("start"))
            {
                
                string game = message.Split(':')[1];
                gameVarient = game;
                foreach (var item in players)
                {
                    item.GamesVariantCode = gameVarient;
                    item.playerStartTime = DateTime.Now;                    
                }
                int noofplayers = int.Parse(message.Split(':')[2]);
                scorecardForm.StartGame(game, Waitingplayers);
                scorecardForm.currentGame.StatusChanged += CurrentGame_StatusChanged;
                  
            }
        }

        private void CurrentGame_StatusChanged(object sender, string status)
        {
            if (scorecardForm != null)
            {
                util.uiupdate($"window.updateStaus('{status}')", webView2);

                if (status == GameStatus.Completed)
                {

                    UpdateWristBandStatus(players);
                    players.Clear();
                 //   Waitingplayers.Clear(); // Clear the waiting list
                    RefreshWebView(); // Refresh WebView2
                }
                if (status == GameStatus.Running)
                {
                    players.AddRange(Waitingplayers);
                    Waitingplayers.Clear();
                } 
            }
        }
        private HttpClient httpClient = null;
        private void UpdateWristBandStatus(List<Player> players)
        {
            foreach (var item in players)
            {
                item.playerEndTime = DateTime.Now;
            }
            var request = new
            {
               players = players
            };

            // Serialize the object to JSON
            string jsonRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(jsonRequest);

            httpClient = new HttpClient { BaseAddress = new Uri(ConfigurationSettings.AppSettings["server"]) };
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                var response = httpClient.PostAsync("playerScore/addPlayerScores", content);
                Console.WriteLine( response.Result.IsSuccessStatusCode ? "" : "Error inserting data into Database!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                // "Error communicating with API";
            }

        }

        private void SetBrowserFeatureControl()
        {
            string appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
            {
                key.SetValue(appName, 11001, RegistryValueKind.DWord);
            }
        }




        private void OnCardDetected(object sender, EventArgs e)
        {
            // Dispatcher.Invoke(() =>
            //{
            //  webView2.CoreWebView2.ExecuteScriptAsync($"window.receiveMessageFromWPF('{this.uid}')");

            //});
        }
    }
}