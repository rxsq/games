using scorecard;
using scorecard.lib;
using scorecard.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

class Program
{
    static void Main(string[] p)
    {
        logger.Log($"Starting game:{string.Join(" ",p)} ");
        if (p != null)
        {
            StartGame(p[0], p[1], p[2]);
        }
        else
        {

        }

    }
    
    public static void StartGame(string gameType, string numberofplayers, string IsTestMode)
    {
        try
        {
            var gameConfig = FetchGameConfig(gameType);
            gameConfig.isTestMode = (IsTestMode == "1");
            if (IsTestMode == "1")
            {
                gameConfig.IpAddress = "127.0.0.1";
                gameConfig.isTestMode = true;
                gameConfig.SmartPlugip = "127.0.0.1";
                gameConfig.NoofLedPerdevice = 1;

            }
           gameConfig.GameName = gameType;
            if (gameConfig == null)
            {
                MessageBox.Show("Failed to start the game due to configuration issues.");
                return;
            }

            BaseGame currentGame;
            gameConfig.MaxPlayers = int.Parse(numberofplayers);

            switch (gameType.Replace(" ", ""))
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
                    currentGame = new WipeoutGame(gameConfig, 9);
                    break;
                case "TileSiege":
                    currentGame = new TileSiege(gameConfig, 10);
                    break;

                case "HexaPatternMatch":
                    currentGame = new HexaPatternMatch(gameConfig);
                    break;
                case "PushGame":
                    currentGame = new PushGame(gameConfig);
                    break;
                case "Invader":
                    currentGame = new Invader(gameConfig);
                    break;
                case "WackAMole":
                    currentGame = new WackAMole(gameConfig);
                    break;
                default:
                    MessageBox.Show("Unknown game type.");
                    return;
            }
            logger.Log($"Starting game {gameType} {numberofplayers} {IsTestMode}");
            currentGame?.StartGame();
            currentGame.StatusChanged += (sender, args) =>
            {
                if (args == GameStatus.Completed)
                {
                    logger.Log("Game ended");
                    //currentGame?.Dispose();
                    Environment.Exit(0); // Force the application to exit immediately
                    //Application.Exit();
                }
            };
            Thread.Sleep(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to start the game: {ex.Message}  \n{ex.StackTrace}" );
        }
    }
    private static GameConfig FetchGameConfig(string gameType)
    {
        using (var httpClient = new HttpClient())
        {
            try
            {
                // Replace with your Node.js API URL
                string apiUrl = $"{System.Configuration.ConfigurationSettings.AppSettings["server"]}/gamesVariant/findall?name={gameType}";
                var response = httpClient.GetAsync(apiUrl).Result;

                response.EnsureSuccessStatusCode();

                // Get the raw JSON response as a string
                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                var gameVariant = JsonSerializer.Deserialize<GameVariant>(jsonResponse);

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
                  
                    logger.Log("congig fetched");
                    return gameConfig;
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.Log($"Failed to fetch game configuration: {ex.StackTrace}");
                return null;
            }
        }
    }
}
