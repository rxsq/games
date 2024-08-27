using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using Microsoft.Web.WebView2.WinForms;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using scorecard.model;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.Configuration;
using System.Reflection.Emit;
using System.ComponentModel;
public partial class ScorecardForm : Form
{
    
    private UdpClient udpClientReceiver;
    private System.Threading.Timer relayTimer;
    private IPEndPoint remoteEndPoint;
    private string currentState = GameStatus.NotStarted;
    private BaseGame currentGame = null;
    private string gameType = "";
    public ScorecardForm()
    {
        InitializeComponent();
        InitializeWebView();
        SetBrowserFeatureControl();
       // InitializeUdpReceiver();
    }
    private async void InitializeWebView()
    {
        webView2.Source = new Uri(System.Configuration.ConfigurationSettings.AppSettings["scorecardurl"]);
        // Initialize the WebView2 control and set its source
        await webView2.EnsureCoreWebView2Async(null);

        // Set up the NavigationCompleted event handler
        webView2.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

        // Navigate to the desired URL
       
    }
    private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            Console.WriteLine("Navigation completed successfully.");
            // Perform any additional actions after navigation is complete
        }
        else
        {
            Console.WriteLine($"Navigation failed with error: {e.WebErrorStatus}");
            // Handle navigation errors or perform fallback actions
        }
        //StartGame(ConfigurationSettings.AppSettings["TestGame"]);
    }
    //private async void InitializeWebView()
    //{

    //    // Navigate to the desired URL
    //    webView2.Source = new Uri(System.Configuration.ConfigurationSettings.AppSettings["scorecardurl"]);
    //    webView2.CoreWebView2InitializationCompleted += WebView2_CoreWebView2InitializationCompleted;
    //}
    private void WebView2_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        webView2.CoreWebView2.WebMessageReceived += WebView2_WebMessageReceived;
    }
    private void WebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
       
    }
    private void SetBrowserFeatureControl()
    {
        string appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
        using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
        {
            key.SetValue(appName, 11001, RegistryValueKind.DWord);
        }
    }

    private void InitializeUdpReceiver()
    {
        remoteEndPoint = new IPEndPoint(IPAddress.Any, 27);
        udpClientReceiver = new UdpClient(remoteEndPoint);
        relayTimer = new System.Threading.Timer(TargetTimeElapsed, null, 1000, 200);
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (currentGame != null)
            currentGame.lightonoff(false);
        base.OnClosing(e);
    }
    private async Task<GameConfig> FetchGameConfigAsync(string gameType)
    {
        using (var httpClient = new HttpClient())
        {
            try
            {
                // Replace with your Node.js API URL
                string apiUrl = $"{System.Configuration.ConfigurationSettings.AppSettings["server"]}/gamesVariant/findall?name={gameType}";
                var response = await httpClient.GetAsync(apiUrl);

                response.EnsureSuccessStatusCode();

                // Get the raw JSON response as a string
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var gameVariant = JsonSerializer.Deserialize<GameVariant>(jsonResponse);
                //var gameVariants = JsonSerializer.Deserialize<List<GameVariant>>(jsonResponse);
                // Deserialize the JSON response to a GameConfig object
                //var gameVariant = gameVariants.FirstOrDefault();
                if (gameVariant != null)
                {
                    GameConfig gameConfig = new GameConfig
                    {
                        Maxiterations = gameVariant.MaxIterations,
                        MaxIterationTime = gameVariant.MaxIterationTime,
                        MaxLevel = gameVariant.MaxLevel,
                        ReductionTimeEachLevel = gameVariant.ReductionTimeEachLevel,
                        IpAddress = gameVariant.game.IpAddress,
                        LocalPort = gameVariant.game.LocalPort,
                        RemotePort = gameVariant.game.RemotePort,
                        SocketBReceiverPort = gameVariant.game.SocketBReceiverPort,
                        NoOfControllers = gameVariant.game.NoOfControllers,
                        NoofLedPerdevice = gameVariant.game.NoofLedPerdevice,
                        columns = gameVariant.game.columns,
                        introAudio = gameVariant.introAudio ?? string.Empty,
                         SmartPlugip = gameVariant.game.SmartPlugip
                    };
                    return gameConfig;

                    // Use your gameConfig object as needed
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to fetch game configuration: {ex.Message}");
                return null;
            }
        }
    }

    private void TargetTimeElapsed(object state)
    {
        udpClientReceiver.BeginReceive(ar =>
        {
            byte[] receivedBytes = udpClientReceiver.EndReceive(ar, ref remoteEndPoint);
            string receivedData = Encoding.UTF8.GetString(receivedBytes);
            if (receivedData.StartsWith("start") && currentState == GameStatus.NotStarted)
            {
                currentState = GameStatus.Running;
                var replyBytes1 = Encoding.UTF8.GetBytes(currentState);
                udpClientReceiver.Send(replyBytes1, replyBytes1.Length, remoteEndPoint);

                Thread.Sleep(10000);
                //StartGame(receivedData.Split(':')[1].Trim());
                Console.WriteLine("game started");
            }

            byte[] replyBytes = Encoding.UTF8.GetBytes(currentState);
            udpClientReceiver.Send(replyBytes, replyBytes.Length, remoteEndPoint);
            Console.WriteLine(currentState);
            Console.WriteLine(receivedData);
        }, null);
    }
   
    public async void StartGame(string gameType, int noofplayers)
    {
        
        var gameConfig = await FetchGameConfigAsync(gameType);
        
        if (gameConfig == null)
        {
            MessageBox.Show("Failed to start the game due to configuration issues.");
            return;
        }
       
        gameConfig.MaxPlayers = noofplayers;
        ShowGameDescription(gameType, GetGameDescription(gameType));

        if (currentGame != null)
        {
            currentGame.EndGame();
            currentGame = null;           

        }

        initializedWebView2();
        switch (gameType.Replace(" ",""))
        {
            case "Target":
                currentGame = new Target(gameConfig, 18);
                break;
            case "Smash":
                currentGame = new Smash(gameConfig);
                break;
            case "Chaser":
                currentGame = new Chaser(gameConfig);
                break;
            case "TileHunt":
                currentGame = new TileHunt(gameConfig, 200);
                break;
            case "PatternBuilder":
                currentGame = new PatternBuilderGame(gameConfig, 2);
                break;
            case "Snakes":
                currentGame = new Snakes(gameConfig, 5000, 5000, "AIzaSyDfOsv-WRB882U3W1ij-p3Io2xe5tSCRbI");
                break;
            case "Wipeout":
                currentGame = new WipeoutGame(gameConfig);
                break;
            default:
                MessageBox.Show("Unknown game type.");
                return;
        }

        currentGame.LifeLineChanged += CurrentGame_LifeLineChanged;
        currentGame.ScoreChanged += CurrentGame_ScoreChanged;
        currentGame.LevelChanged += CurrentGame_LevelChanged;
        currentGame.StatusChanged += CurrentGame_StatusChanged;

        currentGame?.StartGame();

        //setTimer
        uiupdate("updateTimer", currentGame.IterationTime);
    }

    private void initializedWebView2()
    {
        uiupdate("updateTimer", 0);
        uiupdate("updateLevel", 0);
        uiupdate("updateLives", 5);
        uiupdate("updateScore", 0);
    }
    private void CurrentGame_StatusChanged(object sender, string status)
    {
        currentState = status;
      
       
        uiupdate("updateTimer", currentGame.IterationTime);
    }

    private void CurrentGame_LevelChanged(object sender, int level)
    {
        // Update level in the React component
       // webView2.ExecuteScriptAsync($"window.updateLevel({level});");
        uiupdate("updateLevel", level);
        uiupdate("updateTimer", currentGame.IterationTime);
    }

    private void CurrentGame_LifeLineChanged(object sender, int newLife)
    {
        // Update lives in the React component
        uiupdate("updateLives", newLife);
        uiupdate("updateTimer", currentGame.IterationTime);

        //  webView2.ExecuteScriptAsync($"window.updateLives({newLife});");
    }

    private void CurrentGame_ScoreChanged(object sender, int newScore)
    {
        uiupdate("updateScore", newScore);
    }
    private async void uiupdate(string func, int newScore)
    {
        string script = @"
        try {
            window."+ func + "(" + newScore + @");
        } catch (error) {
            console.error('Error executing script:', error);
        }";

        if (webView2.InvokeRequired)
        {
            webView2.Invoke(new Action(async () =>
            {
                try
                {
                    await webView2.ExecuteScriptAsync(script);
                    Console.WriteLine("Script executed successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Script execution failed: " + ex.Message);
                }
            }));
        }
        else
        {
            try
            {
                await webView2.ExecuteScriptAsync(script);
                Console.WriteLine("Script executed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Script execution failed: " + ex.Message);
            }
        }
    }


    private string GetGameDescription(string gameType)
    {
        switch (gameType)
        {
            case "Target":
                return "In the Target Game, hit the highlighted targets as quickly as possible. Each successful hit scores points.";
            case "Smash":
                return "In the Smash Game, smash the targets that light up. The faster you smash, the higher your score.";
            case "Chaser":
                return "In the Chaser Game, chase and hit the moving targets. Stay quick and keep up to score points.";
            case "FloorGame":
                return "Welcome to the LED Floor Game! Here's how to play: Avoid the blue line as it moves across the grid. Step on the green tiles to score points. Each level gets faster, so stay sharp! Touch the blue line and it's game over. Survive all iterations to win! Good luck, and have fun!";
            case "PatternBuilder":
                return "Players must recreate a pattern based off memory as quickly as possible. Each correct pattern earns a point.";
            case "wipeout":
                return "Welcome to the LED Wipeout Game! Here's how to play: Your goal is to avoid the rotating obstacles and survive as long as possible. Obstacles will move around the center of the grid. Each full rotation without a collision increases your score. Be careful, the speed and direction of rotation can change, so stay alert! If you touch an obstacle, the game ends. Survive through all iterations to win the game. Good luck, and get ready for the challenge!";
            default:
                return "";
        }
    }

    private void ShowGameDescription(string gameTitle, string description)
    {
        // Show game description if necessary
    }
}
