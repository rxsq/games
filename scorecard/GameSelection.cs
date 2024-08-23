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
namespace scorecard
{
    public partial class GameSelection : Form
    {
        private ScorecardForm scorecardForm;

        public GameSelection()
        {
            InitializeComponent();
            InitializeScorecardForm();
            InitializeWebView();
            PlayScreensaver();
            SetBrowserFeatureControl();
           
            Lib.NFCReaderWriter readerWriter = new Lib.NFCReaderWriter("V", System.Configuration.ConfigurationSettings.AppSettings["server"]);
            webView2.Source = new Uri(System.Configuration.ConfigurationSettings.AppSettings["gameurl"]);
           // webView2.Visibility = Visibility.Visible;
            readerWriter.StatusChanged += (s, uid) =>
            {
                if (uid.Length > 0)
                {
                    //logger.Log($"Card detected: {uid}");
                    //this.uid = uid;
                    OnCardDetected(this, EventArgs.Empty);
                }
            };
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
            if (message == "show_video")
            {
                //Dispatcher.Invoke(() =>
                //{
                  //  webView2.Visibility = Visibility.Collapsed;
                    // Add your logic to handle the message, such as playing a video
                //});
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

        private void PlayScreensaver()
        {
            // Implement screensaver logic if necessary
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
