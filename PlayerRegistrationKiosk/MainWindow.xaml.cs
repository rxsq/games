using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using System.Configuration;
namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        Logger logger = new AsyncLogger("playerregistration");
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
           // PlayScreensaver();
            SetBrowserFeatureControl();
            
            webView2.Source = new Uri(ConfigurationManager.AppSettings["registrationurl"]);
            Lib.NFCReaderWriter readerWriter = new Lib.NFCReaderWriter("R", ConfigurationManager.AppSettings["server"],  logger);

            readerWriter.StatusChanged += (s, uid1) =>
            {
                if (ifWebaskedtoShow.StartsWith("ScanCard"))
                {
                    int playerid= int.Parse(ifWebaskedtoShow.Split(':')[1]);
                    Dispatcher.Invoke(() =>
                    {
                        string uid = uid1.Split(':')[0];
                        if (uid.Length > 0)
                        {
                            logger.Log($"Card detected: {uid}");
                            if (webView2.CoreWebView2 != null)
                            {
                                string script = $"window.receiveMessageFromWPF('{uid}');";
                                webView2.CoreWebView2.ExecuteScriptAsync(script);
                                readerWriter.updateStatus(uid,"R", playerid);
                            }
                        }
                        else
                        {
                            string script = $"window.receiveMessageFromWPF('');";
                        }
                    });
                }
            };
        }
        string ifWebaskedtoShow = "N";
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
           
                ifWebaskedtoShow = message;
                // Restart the video

          
        }
        private void SetBrowserFeatureControl()
        {
            string appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
            {
                key.SetValue(appName, 11001, RegistryValueKind.DWord);
            }
        }
        
    }
}
