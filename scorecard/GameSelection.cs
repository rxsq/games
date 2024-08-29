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
using System.Diagnostics;
namespace scorecard
{
    public partial class GameSelection : Form
    {
        private ScorecardForm scorecardForm;
        List<Player> players = new List<Player>();
        List<Player> Waitingplayers = new List<Player>();
        AsyncLogger logger = new AsyncLogger("scorecard");

        public GameSelection()
        {
           
        InitializeComponent();
            StartCheckInTimer();
            if (!Debugger.IsAttached)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                this.TopMost = true;
            }
            logger.Log("application started");
            InitializeWebView();

            SetBrowserFeatureControl();
            InitializeScorecardForm();
            Lib.NFCReaderWriter readerWriter = new Lib.NFCReaderWriter("V", ConfigurationSettings.AppSettings["server"], logger);
            webView2.Source = new Uri(ConfigurationSettings.AppSettings["gameurl"])  ;
            // webView2.Visibility = Visibility.Visible;
            readerWriter.StatusChanged += (s, uid) =>
            {

                if (uid.Length > 0)
                {
                    logger.Log($"card uid detected {uid}");
                    if (Waitingplayers.FindAll(x => x.wristbandCode == uid).Count > 0)
                    {

                        logger.Log($"card already added {uid}");
                        return;
                    }
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
                else
                {
                    logger.Log($"card uid not detected {uid}");
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
                    logger.Log($"player did play game minute so clearing them");
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
               
                scorecardForm = new ScorecardForm(logger);
                
                // Set the position of the scorecard form to the secondary screen
                scorecardForm.StartPosition = FormStartPosition.Manual;
                scorecardForm.Location = new Point(secondaryScreen.Bounds.Left, secondaryScreen.Bounds.Top);
                scorecardForm.Size = new Size(secondaryScreen.Bounds.Width, secondaryScreen.Bounds.Height);

                // Show the scorecard form on the secondary screen
                scorecardForm.Show();
            }
            else
            {
                scorecardForm = new ScorecardForm(logger);
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
            logger.Log($"receive message from front end message{message}");
            if (message.StartsWith("start"))
            {
                
                string game = message.Split(':')[1];
               
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
                logger.Log($"receive status change message in select form status:{status}");
                if (status == GameStatus.Completed)
                {
                    

                    players.Clear();
                 //   Waitingplayers.Clear(); // Clear the waiting list
                    RefreshWebView(); // Refresh WebView2
                }
                if (status.StartsWith(GameStatus.Running))
                {
                    players.AddRange(Waitingplayers);
                    Waitingplayers.Clear();
                } 
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