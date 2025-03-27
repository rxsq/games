using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static log4net.Appender.ColoredConsoleAppender;

public class Climb : BaseGameClimb
{
    private double targetPercentage;
    private int targetCount;
    private List<int> obstacles = new List<int>();
    private List<int> targets = new List<int>();
    private string obstacleColor;
    Task targetTask;
    UdpHandlerWeTop handler;
    public Climb(GameConfig config) : base(config)
    {
        if (handler == null)
            handler = new UdpHandlerWeTop(config.IpAddress, config.LocalPort, config.RemotePort, config.SocketBReceiverPort, config.NoofLedPerdevice, config.columns, "handler-1");

        this.config.MaxPlayers = 1;
        targetColor = config.NoofLedPerdevice != 3 ? ColorPaletteone.White : ColorPalette.White;
        obstacleColor = config.NoofLedPerdevice != 3 ? ColorPaletteone.Red : ColorPalette.Red;
        BlinkAllAsync(5);
    }
    protected override void OnIteration()
    {
        handler.activeDevices.Clear();
        obstacles.Clear();
        for (int i = 0; i < handler.DeviceList.Count(); i++)
        {
            handler.DeviceList[i] = config.NoofLedPerdevice == 3 ? ColorPalette.Green : ColorPaletteone.Green;
        }
        ActivateLevel();
        ActivateRandomLights();
    }
    protected override void OnStart()
    {
        handler.BeginReceive(data => ReceiveCallback(data));
        if (targetTask == null || targetTask.IsCompleted)
        {
            if (targetTask != null && !targetTask.IsCompleted)
            {
                logger.Log("targetTask task still running");
            }
            logger.Log("Starting targetTask task");
            targetTask = Task.Run(() => BlinkTargetLight());
        }
    }
    private void BlinkTargetLight()
    {
        if (!isGameRunning)
            return;

        BlinkLights(targets, 1, config.NoofLedPerdevice == 1 ? ColorPaletteone.Blue : ColorPalette.Blue);
        if (isGameRunning)
        {
            Thread.Sleep(2000);
            BlinkTargetLight();
        }
    }
    private void ActivateLevel()
    {
        int totalColumns = config.columns;
        int totalRows = handler.DeviceList.Count / totalColumns;

        switch (Level)
        {
            case 1:
                // Level 1: Simple horizontal line obstacle at mid-height
                for (int col = 0; col < totalColumns; col++)
                {
                    int index = GetIndexFromRowCol(totalRows / 2, col, totalRows);
                    obstacles.Add(index);
                    handler.activeDevices.Add(index);
                }
                break;

            case 2:
                // Level 2: Two horizontal barriers at different heights
                for (int col = 0; col < totalColumns; col++)
                {
                    int index = GetIndexFromRowCol(totalRows / 3, col, totalRows);
                    obstacles.Add(index);
                    handler.activeDevices.Add(index);
                    index = GetIndexFromRowCol(2 * totalRows / 3, col, totalRows);
                    obstacles.Add(index);
                    handler.activeDevices.Add(index);
                }
                break;

            case 3:
                // Level 3: Zigzag pattern
                for (int row = 0; row < totalRows; row += 2)
                {
                    int col = row % 2 == 0 ? 0 : totalColumns - 1; // Alternate column positions
                    int index = GetIndexFromRowCol(row, col, totalRows);
                    obstacles.Add(index);
                    handler.activeDevices.Add(index);
                }
                break;

            case 4:
                // Level 4: X pattern
                for (int i = 0; i < Math.Min(totalRows, totalColumns); i++)
                {
                    int index = GetIndexFromRowCol(i, i, totalRows);
                    obstacles.Add(index); // Top-left to bottom-right diagonal
                    handler.activeDevices.Add(index);
                    index = GetIndexFromRowCol(i, totalColumns - 1 - i, totalRows);
                    obstacles.Add(GetIndexFromRowCol(i, totalColumns - 1 - i, totalRows)); // Top-right to bottom-left diagonal
                    handler.activeDevices.Add(index);
                }
                break;

            case 5:
                // Level 5: Random gaps in vertical barriers
                for (int col = 0; col < totalColumns; col += 2)
                {
                    for (int row = 0; row < totalRows; row++)
                    {
                        if (random.NextDouble() > 0.2) // Leave some gaps
                            obstacles.Add(GetIndexFromRowCol(row, col, totalRows));
                    }
                }
                break;

            default:
                // Higher levels: Dense obstacles with small gaps
                for (int row = totalRows / 4; row < 3 * totalRows / 4; row++)
                {
                    for (int col = 0; col < totalColumns; col++)
                    {
                        if (random.NextDouble() > 0.3) // Make it more difficult
                            obstacles.Add(GetIndexFromRowCol(row, col, totalRows));
                    }
                }
                break;
        }

        // Apply obstacle colors
        foreach (var index in obstacles)
        {
            handler.DeviceList[index] = obstacleColor;
        }

        handler.SendColorsToUdp(handler.DeviceList);
    }

    // Utility function to convert row and column to index considering snake-wise numbering
    private int GetIndexFromRowCol(int row, int col, int totalRows)
    {
        return col % 2 == 0 ? (row * config.columns + col) : ((totalRows - 1 - row) * config.columns + col);
    }

    private void ActivateRandomLights()
    {
        targetCount = this.config.MaxPlayers * 2 * level;
        

        while (targets.Count < targetCount)
        {
            int index = random.Next(handler.DeviceList.Count());
            if (!handler.activeDevices.Contains(index))
            {
                handler.DeviceList[index] = targetColor; // Green indicates the target light
                handler.activeDevices.Add(index);
                targets.Add(index);
            }
        }

        handler.SendColorsToUdp(handler.DeviceList);

    }

    private void ReceiveCallback(byte[] receivedBytes)

    {
        if (!isGameRunning)
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        //LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");
        byte[] filteredBytes = ProcessByteArray(receivedBytes);
        List<int> positions = filteredBytes
                                .Select((b, index) => new { Byte = b, Index = index }) // Select byte and index
                                .Where(x => x.Byte == 0xCC) // Filter where byte equals 0xCC
                                .Select(x => x.Index-1) // Select only the indices
                                .ToList();
        if (positions.Count > 0)
            LogData("a");
        LogData($"Received data from {String.Join(",", positions)}: active positions:{string.Join(",", handler.activeDevices)}");
        var touchedActiveDevices = handler.activeDevices.FindAll(x => positions.Contains(x));
        
        if (touchedActiveDevices.Count > 0)
        {
            if (!isGameRunning)
                return;
            foreach (var device in touchedActiveDevices) { handler.DeviceList[device] = ColorPaletteone.NoColor; }
            handler.SendColorsToUdp(handler.DeviceList);
            handler.activeDevices.RemoveAll(x => touchedActiveDevices.Contains(x));
            foreach (var device in touchedActiveDevices)
            {
                if (targets.Contains(device))
                {
                    targets.Remove(device);
                    updateScore(Score + Level + LifeLine);
                    LogData($"Score updated: {Score}  position {String.Join(",", positions)} active positions:{string.Join(",", handler.activeDevices)}");
                }
                else if(obstacles.Contains(device))
                {
                    obstacles.Remove(device);
                    updateScore(Score - level);
                    LogData($"Score updated: {Score}  position {String.Join(",", positions)} active positions:{string.Join(",", handler.activeDevices)}");
                    IterationLost(null);
                }
            }
        }



        if (targets.Count == 0)
        {
            int random = new Random().Next(0, 9);

            IterationWon();
        }
        else
        {

            handler.BeginReceive(data => ReceiveCallback(data));
        }

    }


}