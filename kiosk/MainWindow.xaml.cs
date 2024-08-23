using Microsoft.Win32;
using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        AsyncLogger logger = new AsyncLogger("wpf.log");
        string uid = "";
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
            PlayScreensaver();
            SetBrowserFeatureControl();
            Lib.NFCReaderWriter readerWriter = new Lib.NFCReaderWriter("V", System.Configuration.ConfigurationManager.AppSettings["server"]);
            webView2.Source = new Uri(System.Configuration.ConfigurationManager.AppSettings["gameurl"]);
            webView2.Visibility = Visibility.Visible;
            readerWriter.StatusChanged += (s, uid) =>
            {
                if (uid.Length > 0)
                {
                    logger.Log($"Card detected: {uid}");
                    this.uid    = uid;
                    OnCardDetected(this, EventArgs.Empty);
                }
            };
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
                Dispatcher.Invoke(() =>
                {
                    webView2.Visibility = Visibility.Collapsed;
                    // Add your logic to handle the message, such as playing a video
                });
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

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Implement logic to restart the video
        }

        private void OnCardDetected(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                webView2.CoreWebView2.ExecuteScriptAsync($"window.receiveMessageFromWPF('{this.uid}')");
                
            });
        }

       
    }
}
