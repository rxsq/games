﻿using NAudio.Gui;
using NAudio.Utils;
using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class TileHunt : BaseMultiDevice
{
    private int killerSpeedReduction = 200;
    private bool isReversed = false; // Track the direction of the killer line
    Task killerLineTask;
    private Dictionary<UdpHandler, List<int>> killerRowsDict = new Dictionary<UdpHandler, List<int>>();
    private List<int> obstaclePositions = new List<int>();
    private CoolDown coolDown = new CoolDown();

    public TileHunt(GameConfig config, int killerSpeedReduction) : base(config)
    {
        this.killerSpeedReduction = killerSpeedReduction;
    }

    protected void MakeSurroundingMap()
    {
        // Implementation for creating a surrounding map
    }

    protected override void Initialize()
    {
        AnimateColor(false);
        AnimateColor(true);
        BlinkAllAsync(4);
    }

    protected override void OnStart()
    {
        if (killerLineTask == null || killerLineTask.IsCompleted)
        {
            if (killerLineTask != null && !killerLineTask.IsCompleted)
            {
                logger.Log("killer line task still running");
            }
            logger.Log("Starting killer line task");
            killerLineTask = Task.Run(() => drawkillingline(null));
        }


        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }

    protected override void OnIteration()
    {
        coolDown.SetFlagTrue(500);
        SendSameColorToAllDevice(ColorPaletteone.Red, true);
        targetColor = ColorPaletteone.Green;
        int totalTargets = 0;
        int tries = 0;

        obstaclePositions.Clear();
        foreach (var handler in udpHandlers)
        {
            handler.activeDevicesGroup.Clear();
        }

        while (totalTargets < config.MaxPlayers && tries < 200)
        {
            int origMain = random.Next(0, deviceMapping.Count - 1);

            while (!isValidpos(origMain))
            {
                origMain = random.Next(0, deviceMapping.Count - 1);
            }

            int nextPosition = 1;
            int nextRowAdd = config.columns;

            if ((origMain % config.columns == 0 && origMain != 0) || origMain == rows * config.columns)
            {
                nextPosition = -1;
            }

            if (deviceMapping.Count - origMain < config.columns)
            {
                nextRowAdd = -1 * nextRowAdd;
            }

            int mainRight = origMain + nextPosition;
            int mainBelow = origMain + nextRowAdd;
            int mainBelowRight = mainBelow + nextPosition;

            List<int> group = new List<int> { origMain, mainRight, mainBelow, mainBelowRight };
            tries++;

            if (tries == 200)
            {
                LogData("Max try for finding safe positions reached!");
            }

            // Skip this group if any of the items already exist in activeDevicesGroup
            if (group.Any(item => base.deviceMapping[item].udpHandler.activeDevicesGroup.ContainsKey(base.deviceMapping[item].deviceNo)))
            {
                LogData($"Item {group} contains items that overlap with existing items. Skipping this loop!");
                continue;
            }

            obstaclePositions.AddRange(group);

            List<int> ActualGroup = new List<int>();

            foreach (var item in group)
            {
                if (base.deviceMapping.ContainsKey(item))
                    ActualGroup.Add(base.deviceMapping[item].deviceNo);
            }

            foreach (var item in group)
            {
                int actualHandlerPos = base.deviceMapping[item].deviceNo;
                base.deviceMapping[item].udpHandler.DeviceList[actualHandlerPos] = ColorPaletteone.Green;
                base.deviceMapping[item].udpHandler.activeDevicesGroup.Add(actualHandlerPos, ActualGroup);
                base.deviceMapping[item].isActive = true;
            }

            LogData($"Active devices filling active devices: {string.Join(",", new List<int>(obstaclePositions))}"); //
            totalTargets++;
        }
        SendColorToUdpAsync();
    }

    private bool isValidpos(int pos)
    {
        var device = base.deviceMapping[pos];
        int lastrowmax = device.udpHandler.DeviceList.Count;
        int lastrowmin = lastrowmax - config.columns;

        if ((device.deviceNo <= lastrowmax && device.deviceNo > lastrowmin) || device.deviceNo % 14 == 13)
        {
            return false;
        }
        foreach (int x in obstaclePositions)
        {
            if(!surroundingMap.ContainsKey(x)) return false;
            List<int> b = surroundingMap[x];
            if (b.Contains(pos))
            {
                return false;
            }
        }
        return true;
    }
    
    protected void drawkillingline(object state)
    {
        if (!isGameRunning)
        {
            //  gameTimer = null;
            return;
        }
        UdpHandler prevhandler = null;
        if (!isReversed)
        {
            for (int handlerCount = 0; handlerCount < udpHandlers.Count; handlerCount++)
            {

                UdpHandler handler = udpHandlers[handlerCount];
                LogData($"calling line loop {handler.name}");
                if (prevhandler != null)
                {
                    LogData($"handler changed from {prevhandler.name} {handler.name}");
                    prevhandler.SendColorsToUdp(prevhandler.DeviceList);

                }

                // Move the killer line from top to bottom
                for (int row = 0; row < handler.Rows; row++)
                {

                    LogData($"moving line for {handler.name}  row:{row}");
                    MoveKillerLine(handler, row);
                    if (!isGameRunning)
                    {
                        return;
                    }
                }

                prevhandler = handler;
            }
        }
        else
        {
            for (int handlerCount = udpHandlers.Count - 1; handlerCount >= 0; handlerCount--)
            {
                UdpHandler handler = udpHandlers[handlerCount];
                if (prevhandler != null)
                {
                    LogData($"handler changed from {prevhandler.name} {handler.name}");
                    prevhandler.SendColorsToUdp(prevhandler.DeviceList);
                }
                for (int row = handler.Rows - 1; row >= 0; row--)
                {
                    if (!isGameRunning)
                    {
                        return;
                    }
                    LogData($"moving line for {handler.name}  row:{row}");
                    MoveKillerLine(handler, row);
                }

                prevhandler = handler;
            }
        }
        // Reverse the direction when the killer line reaches the end
        isReversed = !isReversed;

        if (isGameRunning)
        {
            drawkillingline(null);
        }
    }

    private void MoveKillerLine(UdpHandler handler, int row)
    {
        var colorList = new List<string>();
        var cl = handler.DeviceList.Select(x => x).ToList();
        int rowNum = (row / handler.Rows) % 2 == 0 ? (row % handler.Rows) : handler.Rows - 1 - (row % handler.Rows);
        var blueLineDevices = new List<int>();

        for (int i = 0; i < config.columns; i++)
        {
            if (handler.activeDevices.Contains(rowNum * config.columns + i))
                continue;

            cl[rowNum * config.columns + i] = ColorPaletteone.Blue;
            blueLineDevices.Add(rowNum * config.columns + i);
        }

        if (!isGameRunning)
        {
            //  gameTimer = null;
            return;
        }

        killerRowsDict.Clear();
        killerRowsDict.Add(handler, blueLineDevices);
        handler.SendColorsToUdp(cl);

        LogData($"filling data handler row:{row} handler:{handler.name} active:{string.Join(",", handler.activeDevices)} blueline: {string.Join(",", blueLineDevices)}");

        int killerlineClipTime = 1200 - (base.level - 1) * killerSpeedReduction;
        if (killerlineClipTime < 200)
        {
            killerlineClipTime = 200;
        }
        Thread.Sleep(killerlineClipTime);
    }

    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning)
            return;

        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        var positions = receivedData
            .Select((value, index) => new { value, index })
            .Where(x => x.value == 0x0A)
            .Select(x => x.index - 2)
            .Where(position => position >= 0)
            .ToList();

        try
        {
            if (positions.Count > 0)
            {

                List<int> l2 = new List<int>();

                foreach (var position in positions)
                {
                    if (handler.activeDevicesGroup.ContainsKey(position))
                    {
                        l2.AddRange(handler.activeDevicesGroup[position]);
                    }
                }
                if (l2.Count > 0)
                {
                    LogData($"Received data from {handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");
                    LogData($"Touch detected: {string.Join(",", l2)}");
                    ChnageColorToDevice(ColorPaletteone.NoColor, l2, handler);
                    updateScore(Score + l2.Count / 4);
                    foreach (var item in l2)
                    {
                        handler.activeDevicesGroup.Remove(item);
                    }
                    LogData($"Score updated: {Score} active:{string.Join(",", handler.activeDevicesGroup.Values)}");
                }
                else if (killerRowsDict.ContainsKey(handler))
                {
                    if (positions.Any(x => killerRowsDict[handler].Contains(x)) && !coolDown.Flag)
                    {
                        isGameRunning = false;
                        LogData($"Game Failed : {Score} position:{string.Join(",", positions)} killerRow : {string.Join(",", killerRowsDict[handler])}");
                        killerRowsDict[handler].Clear();
                        base.Score--;
                        IterationLost(null);
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogData($"Exception in ReceiveCallback: {ex.Message}");
        }

        

        LogData($"{handler.name} processing received data");
        if (udpHandlers.Where(x => x.activeDevicesGroup.Count > 0).Count() == 0)
        {
            LogData("Iteration won");
            IterationWon();
        }
        else
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }
}