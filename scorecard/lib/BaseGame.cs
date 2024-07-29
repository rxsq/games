using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using Timer = System.Threading.Timer;
using scorecard.lib;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Drawing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics;
public abstract class BaseGame
{
    protected List<UdpHandler> udpHandlers;
    protected Dictionary<UdpHandler, List<string>> handlerDevices;
    protected Dictionary<UdpHandler, HashSet<int>> activeIndices;
    protected Random random = new Random();
    public TimeSpan Duration { get; protected set; }
    public double Progress { get; protected set; }

    protected MusicPlayer musicPlayer;
    protected int lifeLine = 5;
    protected Timer iterationTimer;
    private string logFile = "log1.log";
     protected int iterations = 0;
    private int score;
    protected GameConfig config;
    private string status { get; set; }
    protected int level = 1;
    protected List<string> gameColors = new List<string>();
    protected bool isGameRunning = false;
    public string Status
    {
        get { return status; }
        set
        {
            status = value;

            OnStatusChanged(status);

        }
    }
    public int Level
    {
        get { return level; }
        set
        {
            level = value;

            OnLevelChanged(level);

        }
    }
    public int Score
    {
        get { return score; }
        set
        {
            score = value;
            OnScoreChanged(score);
            //labelScore.Text = $"Score: {score}";
            LogData($"Score: {score}");
        }
    }
    public int LifeLine
    {
        get { return lifeLine; }
        set
        {
            lifeLine = value;
            OnLifelineChanged(value);
            //  LogData($"GameLost lifeLine: {lifeLine}");
        }
    }
    protected int IterationTime { get { return (config.MaxIterationTime - (Level - 1) * config.ReductionTimeEachLevel) * 1000; } }
    public event EventHandler<int> ScoreChanged;
    public event EventHandler<int> LifeLineChanged;
    public event EventHandler<int> LevelChanged;
    public event EventHandler<string> StatusChanged;
    protected virtual void OnStatusChanged(string newStatus)
    {
        StatusChanged?.Invoke(this, newStatus);
    }
    protected virtual void OnLevelChanged(int newLevel)
    {
        LevelChanged?.Invoke(this, newLevel);
    }
    protected virtual void OnScoreChanged(int newScore)
    {
        ScoreChanged?.Invoke(this, newScore);
    }
    protected virtual void OnLifelineChanged(int newLifeline)
    {
        LifeLineChanged?.Invoke(this, newLifeline);
    }

    protected int noOfPlayers = 5;
    public string targetColor;
    //  public int numberOfDevices = 12; // 4x3 grid
    protected AsyncLogger logger;
    
    public BaseGame(GameConfig config)
    {
        this.config = config;
        logger = new AsyncLogger($"{DateTime.Now:ddMMyy}{logFile}");
        musicPlayer = new MusicPlayer();
      if(!Debugger.IsAttached)
        musicPlayer.Announcement(config.introAudio);
     

        musicPlayer.PlayBackgroundMusic("content/background_music.wav", true);
        udpHandlers = new List<UdpHandler>();
        udpHandlers.Add( new UdpHandler(config.IpAddress, config.LocalPort, config.RemotePort, "udplog.log", config.SocketBReceiverPort, config.NoofLedPerdevice, config.columns, "handler1"));
        for(int i = 1; i < config.NoOfControllers; i++)
        {
            udpHandlers.Add(new UdpHandler(config.IpAddress, config.LocalPort + i, config.RemotePort + i, $"udplog1.log", config.SocketBReceiverPort+i, config.NoofLedPerdevice, config.columns, "handler2"));
        }

       // initializeDevices();
        
        //devices = udpHandler.DeviceList;
        
        gameColors = getColorList();
    }
    //private void initializeDevices()
    //{
    //     handlerDevices = new Dictionary<UdpHandler, List<string>>();
    //    activeIndices = new Dictionary<UdpHandler, HashSet<int>>();

    //    foreach (var handler in udpHandlers)
    //    {
    //        handlerDevices[handler] = handler.DeviceList;
    //        activeIndices[handler] = new HashSet<int>();

    //    }
    //}
    public void StartGame()
    {
        Console.WriteLine("Game starting in 3... 2... 1...");
        Status = "Starting";
        Thread.Sleep(3000); // Countdown
        Initialize();
        RunGameInSequence();

    }
    protected void RunGameInSequence()
    {
        OnIteration();
        isGameRunning = true;
        // Start target timer
        iterationTimer = new Timer(TargetTimeElapsed, null, IterationTime, IterationTime); // Change target tiles every 10 seconds
        OnStart();

       
    }
    protected virtual void TargetTimeElapsed(object state)
    {
        isGameRunning = false;
        LogData($"iteration failed within {IterationTime} second");
        musicPlayer.PlayEffect("content/fail.wav");
        iterationTimer.Dispose();
        LifeLine = LifeLine - 1;
        Status = $"Lost Lifeline {LifeLine}";
        if (lifeLine <= 0)
        {
            //TexttoSpeech: Oh no! You’ve lost all your lives. Game over! 🎮
            musicPlayer.PlayEffect("content/gameoverlost.wav");
            EndGame();
            musicPlayer.StopAllMusic();
        }
        else
        {
            //iterations = iterations + 1;
            RunGameInSequence();


        }

    }
  
    protected virtual void OnIteration()
    {
        
    }
    public void EndGame()
    {


        iterationTimer.Dispose();

        SendSameColorToAllDevice(ColorPaletteone.NoColor);
        Thread.Sleep(100);
        BlinkAllAsync(2);
        foreach (var handler in udpHandlers)
        {
            if (handler != null)
                handler.Close();

        }
        musicPlayer.Dispose();

        //OnEnd();
    }
    protected virtual void Initialize() { }
    protected virtual void OnStart() { }
    protected virtual void OnEnd() { }

    public void LogData(string message)
    {
        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fef} {message}";
        logger.Log(message);
        Console.WriteLine(logMessage);
    }
    protected void SendDataToDevice(string color, int deviceNo)
    {
        
            foreach (var handler in udpHandlers)
            {
                handlerDevices[handler][deviceNo] = color;
                handler.SendColorsToUdp(handlerDevices[handler]);
            }
    }
    protected void SendSameColorToAllDevice(string color)
    {
        foreach (var handler in udpHandlers)
        {
            handler.SendColorsToUdp(handlerDevices[handler].Select(x => color).ToList());
        }

    }
    protected void SendSameColorToAllDevice(string color, bool reset)
    {
        if (reset)
        {
            foreach (var handler in udpHandlers)
            {
                for (int x = 0; x < handler.DeviceList.Count; x++)
                {
                    handlerDevices[handler][x] = color;
                }
                handler.SendColorsToUdp(handlerDevices[handler]);
            }

        }
        else
        {
            foreach (var handler in udpHandlers)
            {
                handler.SendColorsToUdp(handlerDevices[handler].Select(x => color).ToList());
            }
        }

    }
    protected void MoveToNextIteration()
    {
        isGameRunning = false;
        LogData("All targets hit");
        
        iterationTimer.Dispose();
        iterations = iterations + 1;
       


        if (iterations >= config.Maxiterations)
        {
            
            Status = $"Moved to Next Level {Level}";
            LogData($"Game Win level: {Level}");
            Level = Level + 1;
            iterations = 1;
            if (Level > config.MaxLevel + 1)
            {
                Status = $"Reached to last Level {config.MaxLevel} ending game";
                LogData(Status);
                //Text to speech : Congratulations! 🎉You’ve won the game! You’ve completed all the levels. You’re a champion! 🏆
                musicPlayer.PlayEffect("content/GameWin.wav");
                EndGame();
                return;
            }
            else
            {
             //   musicPlayer.StopBackgroundMusic();
                //Text to speech: Great job, Team! 🎉You’ve won this level! Now, get ready for the next one.Expect more energy and excitement.  Let’s go! 🚀 one two three go 

                musicPlayer.Announcement("content/levelwin.wav");
                musicPlayer.PlayBackgroundMusic("content/background_music.wav", true);
            }
            //labelScore.Text = $"Score: {score}";

        }
        else { BlinkAllAsync(1); }
        Status = $"Moved to Next iterations {iterations}";
      //  if (IterationTime > 0)
       // {
            RunGameInSequence();
            LogData($"moving to next iterations: {iterations} Iteration time: {IterationTime} ");
       // }
       // else { Status = $"All iterations completed IterationTime:{IterationTime}";
       //     LogData($"{Status}: {iterations} Iteration time: {IterationTime} ");
       // }

    }
    protected void updateScore(int score)
    {
        Score = score;
        int random = new Random().Next(0, 9);
        //if (random == 0) { musicPlayer.PlayEffect("content/voicelines/praise1.mp3"); }
        //if (random == 1) { musicPlayer.PlayEffect("content/voicelines/praise2.mp3"); }
        //if (random == 2) { musicPlayer.PlayEffect("content/voicelines/praise3.mp3"); }
        if (0 <= random && random < 3) { musicPlayer.PlayEffect("content//hit2.wav"); }
        if (3 <= random && random < 6) { musicPlayer.PlayEffect("content/hit2.wav"); }
        if (6 <= random) { musicPlayer.PlayEffect("content/hit2.wav"); }
        //        musicPlayer.backgroundMusicPlayer.Volume = 0.8f;
    }
    protected void ChnageColorToDevice(string color, int deviceNo, UdpHandler handler)
    {
        ChnageColorToDevice(color, new List<int> { deviceNo }, handler);
    }
    protected void ChnageColorToDevice(string color, List<int> deviceNos, UdpHandler handler)
    {
        try
        {
            Console.WriteLine($"before change: {string.Join(",", handlerDevices[handler])}");
            foreach (int x in deviceNos)
            {
                handlerDevices[handler][x] = color;
            }
            handler.SendColorsToUdp(handlerDevices[handler]);
            Console.WriteLine($"after change: {string.Join(",", handlerDevices[handler])}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
           // ChnageColorToDevice(color, deviceNo, handler);
        }
    }

    protected void SendColorToDevices(string color, bool isDeviceToBeUpdate)
    {
       
            var tasks = new List<Task>();
        foreach (var handler in udpHandlers)
        {
            if (isDeviceToBeUpdate)
            {
                for (int x = 0; x < handlerDevices[handler].Count; x++)
                {
                    handlerDevices[handler][x] = color;
                }

            }
            var colors = handlerDevices[handler].Select(x => color).ToList();
            tasks.Add(handler.SendColorsToUdpAsync(colors));

        }
        Task.WhenAll(tasks);
    }

    protected void  BlinkAllAsync(int nooftimes)
    {
        for (int i = 0; i < nooftimes; i++)
        {
            var tasks = new List<Task>();
            foreach (var handler in udpHandlers)
            {
                
                var colors = handlerDevices[handler].Select(x => config.NoofLedPerdevice == 1 ? ColorPaletteone.Yellow : ColorPalette.yellow).ToList();
                tasks.Add(handler.SendColorsToUdpAsync(colors));
            }
            Task.WhenAll(tasks);
            Thread.Sleep(200);

            foreach (var handler in udpHandlers)
             {
                tasks.Add(handler.SendColorsToUdpAsync(handlerDevices[handler]));
               // var colors = handler.SendColorsToUdpAsync(handlerDevices[handler]);
            }
            Task.WhenAll(tasks);
            Thread.Sleep(200);
        }
    }
    public void BlinkLights(List<int> lightIndex,int repeation, UdpHandler handler, string Color)
    {
            for (int j = 0; j < repeation; j++)
            {
                handler.SendColorsToUdp(handlerDevices[handler].Select((x, i) => lightIndex.Contains(i) ? Color : x).ToList());
                Thread.Sleep(1000);
                handler.SendColorsToUdp(handlerDevices[handler]);
            }
    }
    protected void LoopAll()
    {
        LoopAll((config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3),1);
    }
    protected void LoopAll(string basecolor, int frequency)
    {
        for (int i = 0; i < frequency; i++)
        {
            foreach (var handler in udpHandlers)
            {
                var deepCopiedList = handlerDevices[handler].Select(x => basecolor).ToList();
                // handler.SendColorsToUdp(deepCopiedList);

                var loopColor = gameColors[random.Next(gameColors.Count - 1)];
                for (int j = 0; j < handlerDevices[handler].Count; j++)
                {
                    deepCopiedList[j] = loopColor;
                    handler.SendColorsToUdp(deepCopiedList);
                    Thread.Sleep(100);
                    deepCopiedList[j] = basecolor;
                    handler.SendColorsToUdp(deepCopiedList);
                    Thread.Sleep(100);
                }

                LogData($"LoopAll: {string.Join(",", deepCopiedList)}");
            }
        }
    }
    protected int PositionToIndex(int row, int column)
    {
        int index = 0;
        int handler;
        if (row <= (udpHandlers[0].Rows - 1))
        {
            index = row * udpHandlers[0].columns + column;
        }
        else if (row > (udpHandlers[0].Rows - 1))
        {
            handler = 0;
            index = row - udpHandlers[0].DeviceList.Count;

        }

        return index;
    }
    protected int Resequencer(int index, UdpHandler handler)
    {
        if ((index / handler.columns) % 2 == 0)
        {
            return index;
        }

        int columns = handler.columns;
        int row = index / columns;
        int column = index % columns;
        int dest = (row + 1) * columns - 1 - column;
        return dest;
    }
    protected List<string> getColorList()
    {
        List<string> colorList = new List<string>();


        // Get all public static string fields from ColorPalette
        _FieldInfo[] fields;
        if(config.NoofLedPerdevice==3)
           fields = typeof(ColorPalette).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        else
            fields = typeof(ColorPaletteone).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);



        foreach (var field in fields)
        {
            if (field.FieldType == typeof(string))
            {
                string value = (string)field.GetValue(null); // null because it's a static field
                colorList.Add(value);
            }
        }
        return colorList;
    }
}
