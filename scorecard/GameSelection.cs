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
namespace scorecard
{
    public partial class GameSelection : Form
    {
        private ScorecardForm scorecardForm;

        public GameSelection()
        {
            InitializeComponent();
            
            InitializeWebView();
           
            SetBrowserFeatureControl();
            InitializeScorecardForm();
            Lib.NFCReaderWriter readerWriter = new Lib.NFCReaderWriter("V", ConfigurationSettings.AppSettings["server"]);
            webView2.Source = new Uri(ConfigurationSettings.AppSettings["gameurl"]);
           // webView2.Visibility = Visibility.Visible;
            readerWriter.StatusChanged += (s, uid) =>
            {
                if (uid.Length > 0)
                {
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
            if (ConfigurationSettings.AppSettings["TestGame"]!="")
            {
                scorecardForm = new ScorecardForm();
                scorecardForm.Show();
                
                
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
                int noofplayers = int.Parse(message.Split(':')[2]);
                scorecardForm.StartGame(game, noofplayers);
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
