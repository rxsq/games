using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class WipeoutGame : BaseMultiDevice
{
    private List<string> grid;
    private List<int> obstaclePositions;
    private System.Threading.Timer gameTimer;

    private int centerX;
    private int centerY;
    private int radius;

    private int angleStep;
    private double currentAngle;
    private int revolutions;
    private int totalHalfTiles;
    private bool isReversed; // Track the direction of movement
    private double secondsPerRound;
    private int maxRoundsPerLevel;


    public WipeoutGame(GameConfig config, double secondsPerRound) : base(config)
    {
        config.timerPointLoss = false;
        this.secondsPerRound = secondsPerRound;
        Initialize();
    }

    protected override void Initialize()
    {

        //musicPlayer.PlayEffect("content/WipeoutIntro.wav");
        grid = new List<string>(new string[rows * config.columns]);
        for (int i = 0; i < rows * config.columns; i++)
        {
            grid[i] = ColorPaletteone.NoColor;
        }
        obstaclePositions = new List<int>();
        centerX =  config.columns / 2;
        centerY = rows / 2;
        radius = (rows / 2) + 1;
        angleStep = 10; // Adjust the angle step for smoother movement
        currentAngle = 1;
        totalHalfTiles = config.columns * centerY;
        isReversed = false;
}
    private CancellationTokenSource _cancellationTokenSource;

    protected override void OnIteration()
    {
        revolutions = 0;
     
        if (iterationTimer != null)
        {
            iterationTimer.Dispose();
        }

        maxRoundsPerLevel = (int)(IterationTime /(secondsPerRound*1000));
    }
    protected override void OnStart()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => GameLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }


    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning)
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => x.index - 2)
                                          .Where(position => position >= 0)
                                          .ToList();

      //  LogData($"Touch detected: {string.Join(",", positions)} handler: {handler.name} active devices: {string.Join(",", handler.activeDevices)}");
        if (!isGameRunning)
            return;
        if (handler.activeDevices.Exists(x => positions.Contains(x)))
        {
            LogData($"Touch detected: {string.Join(",", positions)} handler: {handler.name} active devices: {string.Join(",", handler.activeDevices)}");
            isGameRunning = false;
            LogData("starting clearing active devices");
            udpHandlers.ForEach(x => x.activeDevices.Clear());
           // LogData("End clearing active devices");
            CancelTargetThread();        
           
            LogData($"after clearing: {string.Join(",", positions)} handler: {handler.name} active devices: {string.Join(",", handler.activeDevices)}");

            // handler.activeDevices.Clear();            
            BlinkAllAsync(1);           
            IterationLost("Lost Iteration");
            
            return;
        }


        if (!isGameRunning)
            return;

        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }

    private async Task GameLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && isGameRunning)
        {
            LogData($"currentAngle:{currentAngle} angleStep {angleStep}");

            if ((currentAngle >= 360) || (currentAngle <= 0))
            {
                revolutions += 1;
                
                angleStep = -angleStep;
                if (currentAngle - angleStep > 360)
                    currentAngle = currentAngle - 5;
                updateScore(Score + 1);
            }


            obstaclePositions.Clear();
            currentAngle += angleStep;
            MoveObstacles();
            LogData($"Revolutions: {revolutions} maxRoundsPerLevel: {maxRoundsPerLevel}");
            if (revolutions == maxRoundsPerLevel)
            {
                isGameRunning = false;
                IterationWon();
                return;
            }


            foreach (var handler in udpHandlers)
            {
                for (int i = 0; i < handler.DeviceList.Count; i++)
                {
                    handler.DeviceList[i] = ColorPaletteone.Green;
                }
            }
           
            if (!isGameRunning)
            {
                return;
            }
            foreach (var handler in udpHandlers)
            {
                handler.activeDevices.Clear();
            }
            int waitTime = Convert.ToInt32((secondsPerRound *1000) / 36);
            logger.Log($"cleared Active devices:{string.Join(",", udpHandlers.Select(x => x.name))} active devices: {string.Join(",", udpHandlers.Select(x => string.Join(",", x.activeDevices)))}");
            StringBuilder sb = new StringBuilder();
            foreach (int pos in obstaclePositions)
            {
                int actualHandlerPos = base.deviceMapping[pos].deviceNo;
                base.deviceMapping[pos].udpHandler.DeviceList[actualHandlerPos] = ColorPaletteone.Red;
                base.deviceMapping[pos].udpHandler.activeDevices.Add(actualHandlerPos);

            }
            foreach(var handler in udpHandlers)
            {
                sb.Append(handler.name).Append(":").Append(string.Join(",", handler.activeDevices)).Append(";");
            }

           // logger.Log($"Active devices filling handler:{string.Join(",", udpHandlers.Select(x => x.name))} active devices: {string.Join(",", udpHandlers.Select(x => string.Join(",", x.activeDevices)))}");
            logger.Log($"Active devices filling handler {sb.ToString()} wait time inms:{waitTime}");
            SendColorToUdpAsync();
           
            try
            {
                await Task.Delay(waitTime, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Task was canceled
                return;
            }
        }

        // Cleanup after game loop ends
        OnEnd();
    }


    private void MoveObstacles()
    {
        double radianAngle = currentAngle * Math.PI / 180;

        int x1 = centerX;
        int y1 = centerY;
        int x2 = (int)(centerX + radius * Math.Cos(radianAngle));
        int y2 = (int)(centerY + radius * Math.Sin(radianAngle));

        bool steep = Math.Abs(y2 - y1) > Math.Abs(x2 - x1);
        if (steep)
        {
            Swap(ref x1, ref y1);
            Swap(ref x2, ref y2);
        }

        if (x1 > x2)
        {
            Swap(ref x1, ref x2);
            Swap(ref y1, ref y2);
        }

        int dx = x2 - x1;
        int dy = Math.Abs(y2 - y1);
        int error = dx / 2;
        int ystep = (y1 < y2) ? 1 : -1;
        int y = y1;

        for (int x = x1; x <= x2; x++)
        {
            int posToAdd = -500;
            if (steep && x < rows && x >= 0 && y < config.columns && y >= 0)
            {
                posToAdd = x * config.columns + y;
            }
            else if (x < config.columns && x >= 0 && y < rows && y >= 0)
            {
                posToAdd = y * config.columns + x;
            }

            if (currentAngle > 300 && currentAngle < 330 && posToAdd > totalHalfTiles)
            {
                Console.WriteLine($"x:{x} y:{y} posToAdd {posToAdd} currentAngle {currentAngle}");
            }
            else if (posToAdd != -500)
            {
                obstaclePositions.Add(posToAdd);
            }
            error -= dy;
            if (error < 0)
            {
                y += ystep;
                error += dx;
            }
        }
        // Console.WriteLine($"x:{x1} x2:{x2}  currentAngle {currentAngle} {string.Join(",", obstaclePositions)}");
    }

    private void Swap(ref int a, ref int b)
    {
        int temp = a;
        a = b;
        b = temp;
    }

    private void UpdateGrid()
    {
       
    }
    protected  void CancelTargetThread()
    {
        _cancellationTokenSource?.Cancel(); // Cancel the running task
        _cancellationTokenSource?.Dispose(); // Dispose of the token source
        _cancellationTokenSource = null;

    }
    protected override void OnEnd()
    {
             base.OnEnd();
    }
}