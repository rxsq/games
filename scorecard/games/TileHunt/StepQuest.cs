using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class StepQuest: BaseMultiplayerMultiDeviceGames
{
    private int targetTilesPerPlayer;
    private Dictionary<int, Dictionary<UdpHandler, List<int>>> obstaclePositionsMap;
    private string[] starColorSet;
    private int numberOfPlayers;
    public StepQuest(GameConfig config) : base(config)
    {
        targetTilesPerPlayer = 6;
        if (config.NoofLedPerdevice == 1) starColorSet = new string[] { ColorPaletteone.Red, ColorPaletteone.Green, ColorPaletteone.Blue, ColorPaletteone.White, ColorPaletteone.Yellow };
        else starColorSet = new string[] { ColorPalette.Red, ColorPalette.Green, ColorPalette.Blue, ColorPalette.White, ColorPalette.yellow };

        this.numberOfPlayers = config.MaxPlayers;
        obstaclePositionsMap = new Dictionary<int, Dictionary<UdpHandler, List<int>>>();
    }

    protected override void Initialize()
    {                     
        AnimateColor(false);
        AnimateColor(true);
        BlinkAllAsync(4);
    }

    protected override void OnStart()
    {
        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }
    protected override void OnIteration()
    {
        SendSameColorToAllDevice(ColorPaletteone.NoColor, true);
        logger.Log("Iteration started");
        obstaclePositionsMap.Clear();
        foreach (var handler in udpHandlers)
        {
            handler.activeDevicesGroup.Clear();
        }
        CreateTargetTiles();

        SendColorToUdpAsync();
    }

    private void CreateTargetTiles()
    {

        int targetTilesPerPlayerlocal = this.targetTilesPerPlayer / udpHandlers.Count;
        logger.Log($"STARTING CreateTargetTiles targetTilesPerPlayerlocal: {targetTilesPerPlayerlocal}");
        Dictionary<UdpHandler, List<int>> obstaclesByHandlers = new Dictionary<UdpHandler, List<int>>();
        foreach(var handler in udpHandlers)
        {
            obstaclesByHandlers.Add(handler, new List<int>());
        }
        for (int i = 0; i < numberOfPlayers; i++)
        {
            Dictionary<UdpHandler, List<int>> handlerObstacles = new Dictionary<UdpHandler, List<int>>();
            foreach (var handler in udpHandlers)
            {
                handler.activeDevices.Clear();
                int totalTargets = 0;
                List<int> obstaclesList = new List<int>();

                while (totalTargets < targetTilesPerPlayerlocal)
                {
                    int randomTile = random.Next(0, handler.DeviceList.Count - 1);

                    while (handler.activeDevices.Contains(randomTile) || obstaclesByHandlers[handler].Contains(randomTile))
                    {
                        randomTile = random.Next(0, handler.DeviceList.Count - 1);
                    }

                    handler.activeDevices.Add(randomTile);
                    obstaclesByHandlers[handler].Add(randomTile);
                    handler.DeviceList[randomTile] = starColorSet[i];
                    obstaclesList.Add(randomTile);
                    totalTargets++;
                }
                handlerObstacles.Add(handler, obstaclesList);
                logger.Log($"Active devices filling handler:{handler.name} for player: {i} active devices: {string.Join(",", handler.activeDevices)}");
            }
            obstaclePositionsMap.Add(i, handlerObstacles);
        }
    }

    #region datareceiving and results
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

        // Track target hits for logging and processing
        List<int> targetHits = new List<int>();

        // Loop through each player in obstaclePositionsMap
        foreach (int playerId in obstaclePositionsMap.Keys.ToList())
        {
            foreach (var position in positions)
            {
                // Check if the current position is a target for the player in this handler
                if (obstaclePositionsMap[playerId].ContainsKey(handler) && obstaclePositionsMap[playerId][handler].Contains(position))
                {
                    targetHits.Add(position);
                    // Remove the touched target from the player's list for this handler
                    obstaclePositionsMap[playerId][handler].Remove(position);

                    // Update the score of the player
                    int newScore = Scores[playerId]+Level;
                    UpdateScore(playerId, newScore);

                    // If the player's list for this handler is empty, remove the handler from the map
                    if (obstaclePositionsMap[playerId][handler].Count == 0)
                    {
                        obstaclePositionsMap[playerId].Remove(handler);
                    }
                }
            }

            // If this player has no targets left across all handlers, end the iteration
            if (obstaclePositionsMap[playerId].Count == 0)
            {
                LogData($"Player {playerId} has cleared all targets. Ending iteration.");
                ChnageColorToDevice(ColorPaletteone.NoColor, targetHits, handler);
                IterationWon();
                return;
            }
        }

        if (targetHits.Count > 0)
        {
            LogData($"Received data from {handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");
            LogData($"Tiles hit: {string.Join(",", targetHits)}");

            // Change the color of the hit targets to indicate they are cleared
            ChnageColorToDevice(ColorPaletteone.NoColor, targetHits, handler);
            updateScore(Score + targetHits.Count);

            foreach (var target in targetHits)
            {
                handler.activeDevices.Remove(target);
            }
        }

        // Continue receiving data if iteration has not been won yet
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }
    #endregion
}
