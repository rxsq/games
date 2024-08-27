using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Configuration;

namespace scorecard
{
    public partial class GameSelection : Form
    {
        private ScorecardForm scorecardForm;
        private Button closeButton;
        private int touchSequenceIndex = 0;
        private readonly int[] correctSequence = { 1, 2, 3, 4 };  // Define the correct sequence in "Z" shape

        // Import necessary WinAPI functions
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public GameSelection()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            InitializeWebView();

            SetBrowserFeatureControl();
            InitializeScorecardForm();
            CreateTouchZones();
            CreateExitButton();  // Initially hidden

            // Ensure the WebView is visible and loads the game URL
            webView2.Visible = true;
            webView2.Source = new Uri(ConfigurationSettings.AppSettings["gameurl"]);

            // Set up the keyboard hook to prevent task switching
            _hookID = SetHook(_proc);
        }

        private void InitializeScorecardForm()
        {
            Screen[] screens = Screen.AllScreens;
            Screen secondaryScreen = screens.Length > 1 ? screens[1] : null;

            if (secondaryScreen != null)
            {
                scorecardForm = new ScorecardForm();
                scorecardForm.StartPosition = FormStartPosition.Manual;
                scorecardForm.Location = new Point(secondaryScreen.Bounds.Left, secondaryScreen.Bounds.Top);
                scorecardForm.Size = new Size(secondaryScreen.Bounds.Width, secondaryScreen.Bounds.Height);
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
            webView2.Dock = DockStyle.Fill;  // Dock WebView2 to fill the form
            webView2.CoreWebView2InitializationCompleted += WebView2_CoreWebView2InitializationCompleted;
            this.Controls.Add(webView2);  // Ensure WebView2 is added before other controls
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

        private void HideTaskbar()
        {
            var taskBar = FindWindow("Shell_TrayWnd", "");
            ShowWindow(taskBar, SW_HIDE);
        }

        [DllImport("user32.dll")]
        private static extern int FindWindow(string className, string windowText);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(int hwnd, int command);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 1;

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(13, proc, GetModuleHandle(null), 0); // Use null for the current module
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)0x0100 || wParam == (IntPtr)0x0104)) // WM_KEYDOWN or WM_SYSKEYDOWN
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // Block specific keys like Alt, Ctrl, Tab, Win key
                if (vkCode == 0x09 || vkCode == 0x5B || vkCode == 0x5C || vkCode == 0x1B) // Tab, LWin, RWin, Esc
                {
                    return (IntPtr)1; // Handled
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void CreateTouchZones()
        {
            int zoneSize = 100;

            // Top Left Corner
            Label touchZone1 = new Label
            {
                Size = new Size(zoneSize, zoneSize),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(50, Color.Red),  // Semi-transparent red for visibility
                Name = "TouchZone1",
                Tag = 1
            };

            // Top Right Corner
            Label touchZone2 = new Label
            {
                Size = new Size(zoneSize, zoneSize),
                Location = new Point(this.Width - zoneSize, 0),
                BackColor = Color.FromArgb(50, Color.Green),  // Semi-transparent green for visibility
                Name = "TouchZone2",
                Tag = 2
            };

            // Bottom Left Corner
            Label touchZone3 = new Label
            {
                Size = new Size(zoneSize, zoneSize),
                Location = new Point(0, this.Height - zoneSize),
                BackColor = Color.FromArgb(50, Color.Blue),  // Semi-transparent blue for visibility
                Name = "TouchZone3",
                Tag = 3
            };

            // Bottom Right Corner
            Label touchZone4 = new Label
            {
                Size = new Size(zoneSize, zoneSize),
                Location = new Point(this.Width - zoneSize, this.Height - zoneSize),
                BackColor = Color.FromArgb(50, Color.Yellow),  // Semi-transparent yellow for visibility
                Name = "TouchZone4",
                Tag = 4
            };

            // Add touch event handlers
            touchZone1.Click += TouchZone_Click;
            touchZone2.Click += TouchZone_Click;
            touchZone3.Click += TouchZone_Click;
            touchZone4.Click += TouchZone_Click;

            // Add touch zones to form
            this.Controls.Add(touchZone1);
            this.Controls.Add(touchZone2);
            this.Controls.Add(touchZone3);
            this.Controls.Add(touchZone4);

            // Bring touch zones to front
            touchZone1.BringToFront();
            touchZone2.BringToFront();
            touchZone3.BringToFront();
            touchZone4.BringToFront();
        }

        private void TouchZone_Click(object sender, EventArgs e)
        {
            Label touchedZone = sender as Label;
            int zoneNumber = (int)touchedZone.Tag;

            if (zoneNumber == correctSequence[touchSequenceIndex])
            {
                touchSequenceIndex++;
                if (touchSequenceIndex == correctSequence.Length)
                {
                    ShowExitButton();
                }
            }
            else
            {
                touchSequenceIndex = 0;  // Reset if wrong sequence
            }
        }

        // Initially hidden exit button
        private void CreateExitButton()
        {
            closeButton = new Button
            {
                Text = "Exit",
                Font = new Font("Arial", 20, FontStyle.Bold),
                Size = new Size(200, 100),
                Location = new Point(this.Width - 220, 20),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Visible = false  // Hidden initially
            };

            closeButton.Click += (s, e) =>
            {
                var taskBar = FindWindow("Shell_TrayWnd", "");
                ShowWindow(taskBar, SW_SHOW);
                this.Close();
            };

            this.Controls.Add(closeButton);
            closeButton.BringToFront();  // Ensure the close button is brought to the front
        }

        private void ShowExitButton()
        {
            closeButton.Visible = true;
        }

        // Unhook the keyboard hook when the form closes
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnhookWindowsHookEx(_hookID);
            base.OnFormClosing(e);
        }
    }
}
