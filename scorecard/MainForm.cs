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

public partial class MainForm : Form
{
    private WebView2 webView;
    private UdpClient udpClientReceiver;
    private System.Threading.Timer relayTimer;
    private IPEndPoint remoteEndPoint;
    private string currentState = GameStatus.NotStarted;
    private BaseGame currentGame = null;

    public MainForm()
    {
        InitializeComponent();
        InitializeWebView();
        InitializeUdpReceiver();
    }

    private async void InitializeWebView()
    {
        webView = new WebView2
        {
            Size = new Size(800, 600),
            Location = new Point(100, 100), // Adjust as needed
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        webView.BringToFront();
        await webView.EnsureCoreWebView2Async(null);
        this.Controls.Add(webView);

        // Navigate to the desired URL
        webView.Source = new Uri("http://localhost:3002/scorecard");
    }

    private void InitializeUdpReceiver()
    {
        remoteEndPoint = new IPEndPoint(IPAddress.Any, 27);
        udpClientReceiver = new UdpClient(remoteEndPoint);
        relayTimer = new System.Threading.Timer(TargetTimeElapsed, null, 1000, 200);
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
                StartGame(receivedData.Split(':')[1].Trim());
                Console.WriteLine("game started");
            }

            byte[] replyBytes = Encoding.UTF8.GetBytes(currentState);
            udpClientReceiver.Send(replyBytes, replyBytes.Length, remoteEndPoint);
            Console.WriteLine(currentState);
            Console.WriteLine(receivedData);
        }, null);
    }

    private void StartGame(string gameType)
    {
        ShowGameDescription(gameType, GetGameDescription(gameType));

        if (currentGame != null)
        {
            currentGame.EndGame();
        }

        switch (gameType)
        {
            case "Target":
                currentGame = new Target(new GameConfig { Maxiterations = 2, MaxLevel = 5, MaxPlayers = 5, MaxIterationTime = 30, ReductionTimeEachLevel = 5, NoofLedPerdevice = 1 }, 18);
                break;
            case "Smash":
                currentGame = new Smash(new GameConfig { Maxiterations = 3, MaxLevel = 5, MaxPlayers = 2, MaxIterationTime = 60, ReductionTimeEachLevel = 10, NoofLedPerdevice = 3 }, .2);
                break;
            case "Chaser":
                currentGame = new Chaser(new GameConfig { Maxiterations = 3, MaxLevel = 5, MaxPlayers = 2, MaxIterationTime = 60, ReductionTimeEachLevel = 10, NoofLedPerdevice = 3 });
                break;
            case "FloorGame":
                currentGame = new FloorGame1(new GameConfig { Maxiterations = 3, MaxLevel = 5, MaxPlayers = 5, MaxIterationTime = 20, ReductionTimeEachLevel = 2, NoOfControllers = 3, columns = 14, introAudio = "content\\floorgameintro.wav" }, 200);
                break;
            case "PatternBuilder":
                currentGame = new PatternBuilderGame(new GameConfig { Maxiterations = 3, MaxLevel = 3, MaxPlayers = 2, MaxIterationTime = 60, ReductionTimeEachLevel = 10, NoOfControllers = 3, columns = 14 }, 2);
                break;
            case "Snakes":
                currentGame = new Snakes(new GameConfig { Maxiterations = 3, MaxLevel = 3, MaxPlayers = 2, MaxIterationTime = 60, ReductionTimeEachLevel = 10, NoOfControllers = 2, columns = 14 }, 5000, 5000, "AIzaSyDfOsv-WRB882U3W1ij-p3Io2xe5tSCRbI");
                break;
            case "Wipeout":
                currentGame = new WipeoutGame(new GameConfig { Maxiterations = 3, MaxLevel = 5, MaxPlayers = 5, MaxIterationTime = 60, ReductionTimeEachLevel = 5, NoOfControllers = 3, columns = 14, introAudio = "content\\wipeoutintro.wav" });
                break;
        }

        currentGame.LifeLineChanged += CurrentGame_LifeLineChanged;
        currentGame.ScoreChanged += CurrentGame_ScoreChanged;
        currentGame.LevelChanged += CurrentGame_LevelChanged;
        currentGame.StatusChanged += CurrentGame_StatusChanged;

        currentGame?.StartGame();
    }

    private void CurrentGame_StatusChanged(object sender, string status)
    {
        currentState = status;
    }

    private void CurrentGame_LevelChanged(object sender, int level)
    {
        // Update level in the React component
        webView.ExecuteScriptAsync($"window.updateLevel({level});");
    }

    private void CurrentGame_LifeLineChanged(object sender, int newLife)
    {
        // Update lives in the React component
        webView.ExecuteScriptAsync($"window.updateLives({newLife});");
    }

    private void CurrentGame_ScoreChanged(object sender, int newScore)
    {
        // Update score in the React component
        webView.ExecuteScriptAsync($"window.updateScore({newScore});");
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
