using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading;

public class WipeoutGame : BaseMultiDevice
{
    private List<string> grid;
    private List<int> obstaclePositions;
    private Random random = new Random();
    private System.Threading.Timer gameTimer;
    private UdpHandler udpHandler;
    private int centerX;
    private int centerY;
    private int radius;
    private int rows;
    private int angleStep;
    private double currentAngle;
    private int revolutions;
    private int totalHalfTiles;
    public WipeoutGame(GameConfig config) : base(config)
    {
        udpHandler = base.udpHandlers[0];
        foreach (var handler in udpHandlers)
        {
            rows += handler.Rows;
        }
    }

    protected override void Initialize()
    {
        grid = new List<string>(new string[rows * config.columns]);
        for (int i = 0; i < rows * config.columns; i++)
        {
            grid[i] = ColorPaletteone.NoColor;
        }
        obstaclePositions = new List<int>();
        centerX = config.columns / 2;
        centerY = rows / 2;
        radius = (rows / 2) + 1;
        angleStep = 10; // Adjust the angle step for smoother movement
        currentAngle = 270;
        revolutions = 0;
        totalHalfTiles=  config.columns * centerY; 
    }

    protected override void OnStart()
    {
        gameTimer = new System.Threading.Timer(GameLoop, null, 0, 1000000); // Game loop runs every 200ms
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

        foreach (int position in positions)
        {
            LogData($"Received data from {handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");
            LogData($"Touch detected: {string.Join(",", positions)}");

            if (handler.activeDevices.Contains(position))
            {
                isGameRunning = false;
                BlinkAllAsync(1);
                gameTimer.Dispose();
                TargetTimeElapsed(null);
                               
                return;
            }
        }
      
     
        if (!isGameRunning)
            return;

        handler.BeginReceive(data => ReceiveCallback(data, handler));

    }
    private void GameLoop(object state)
    {
        if (!isGameRunning)
        {
            return;
        }
        if (revolutions == 1)
        {
            gameTimer.Dispose();
            updateScore(Score + 1);
            revolutions = 0;
            isGameRunning = false;
            MoveToNextIteration();
            return;
        }
        MoveObstacles();
        UpdateGrid();
        SendColorToUdpAsync();
        Thread.Sleep(10);
        if (!isGameRunning)
        {
            return;
        }
        GameLoop(null);
    }

    private void MoveObstacles()
    {
        currentAngle += angleStep;
        if (currentAngle >= 360)
        {
            revolutions += 1;
            currentAngle -= 360;
        }
      
        obstaclePositions.Clear();
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
        Console.WriteLine($"x:{x1} x2:{x2}  currentAngle {currentAngle} {string.Join(",", obstaclePositions)}"); 

    }

    private void Swap(ref int a, ref int b)
    {
        int temp = a;
        a = b;
        b = temp;
    }

    private void UpdateGrid()
    {
        foreach (var handler in udpHandlers)
        {
            for (int i = 0; i < handler.DeviceList.Count; i++)
            {
                handler.DeviceList[i] = ColorPaletteone.NoColor;
            }
        }

        foreach (int pos in obstaclePositions)
        {
            int actualHandlerPos = base.deviceMapping[pos].deviceNo;
            base.deviceMapping[pos].udpHandler.DeviceList[actualHandlerPos] = ColorPaletteone.Red;
            base.deviceMapping[pos].udpHandler.activeDevices.Add(actualHandlerPos);
        }
    }

    public void StopGame()
    {
        gameTimer.Dispose();
        udpHandler.Close();
    }
}
