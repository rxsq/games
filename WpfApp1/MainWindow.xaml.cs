using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        AsyncLogger logger = new AsyncLogger("wpf.log");
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
            PlayScreensaver();
            SetBrowserFeatureControl();
            Lib.NFCReaderWriter readerWriter = new Lib.NFCReaderWriter("R");
            webView2.Source = new Uri("http://localhost:3001/");
            readerWriter.StatusChanged += (s, uid) =>
            {
                if (uid.Length > 0)
                {
                    logger.Log($"Card detected: {uid}");
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
                // Restart the video
                Dispatcher.Invoke(() =>
                {
                    webView2.Visibility = Visibility.Collapsed;
                    videoPlayer.Visibility = Visibility.Visible;
                   // videoPlayer.Position = TimeSpan.Zero;
                   // videoPlayer.Play();
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
            videoPlayer.MediaEnded += VideoPlayer_MediaEnded;
            videoPlayer.Play();
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Restart the video
            videoPlayer.Position = TimeSpan.Zero;
            videoPlayer.Play();
        }

        private void OnCardDetected(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                videoPlayer.Visibility = Visibility.Collapsed;
                webView2.Visibility = Visibility.Visible;
                webView2.Source = new Uri("http://localhost:8081/");
                logger.Log("web visible");
                
                // Delay visibility change to ensure the content is loaded

            });
        }

    }
}
