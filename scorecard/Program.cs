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
                /*
                Hexa Pattern Match - Game Instructions:

                Objective:
                - The goal of the game is to correctly hit all the target tiles (highlighted in yellow) before making too many wrong attempts.
                - Players should avoid hitting non-target tiles to avoid penalties.

                Game Flow:
                1. The game starts by blinking all the tiles to signal the beginning of a new level.
                2. In each iteration, a set of target tiles will be highlighted in yellow.
                3. These target tiles will remain visible for a short period of time before turning blue again.
                4. Players must hit the target tiles (even after they are hidden) by touching them in the correct positions.
                5. The number of target tiles will increase as the player progresses through levels.

                Scoring:
                - Players score points by hitting the correct target tiles.
                - Each correct hit will turn the tile green and remove it from the active targets.
                - Each wrong hit will turn the tile red and count as a "wrong attempt."
                - Players have a maximum of 3 wrong attempts per iteration.

                Gameplay Rules:
                1. Players must hit all the correct target tiles to complete the iteration.
                2. Players can hit target tiles even when they are hidden (after the display phase).
                3. If all target tiles are hit, the iteration is won and the game moves to the next iteration or level.
                4. If the player hits a wrong tile 3 times during an iteration, the iteration is lost, and the player loses a life.
                5. If the player loses all lives, the game is over.

                Level Progression:
                - The game consists of multiple levels, each with several iterations.
                - As the player progresses to higher levels, the number of target tiles increases, and the display time for target tiles decreases.
                - The game gets progressively harder as levels increase.

                Winning and Losing:
                - A player wins an iteration by correctly hitting all target tiles.
                - A player wins the game by completing all levels without losing all lives.
                - The game is lost if the player loses all lives by hitting wrong tiles in too many iterations.

                Good luck, and may your pattern recognition and reaction speed guide you to victory!
                */

                case "HexaPatternMatch":
                    currentGame = new HexaPatternMatch(gameConfig);
                    break;
                case "PushGame":
                    currentGame = new PushGame(gameConfig);
                    break;
                case "Invader":
                    currentGame = new Invader(gameConfig);
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
