using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace scorecard
{
    public class BaseMultiplayerMultiDeviceGames : BaseGame
    {
        protected Dictionary<int, Mapping> deviceMapping;
        protected int rows = 0;
        protected Dictionary<int, List<int>> surroundingMap;
        protected int[] scores; // Array to hold scores for each player
        protected Dictionary<int, HashSet<int>> playerTargets; // Track targets for each player across all devices

        public BaseMultiplayerMultiDeviceGames(GameConfig config) : base(config)
        {
            scores = new int[config.MaxPlayers];
            playerTargets = new Dictionary<int, HashSet<int>>();

            deviceMapping = new Dictionary<int, Mapping>();
            int deviceIndex = 0;

            foreach (var handler in udpHandlers)
            {
                for (int i = 0; i < handler.DeviceList.Count; i++)
                {
                    deviceMapping.Add(deviceIndex, new Mapping(handler, false, Resequencer(i, handler)));
                    deviceIndex += 1;
                }
            }

            foreach (var handler in udpHandlers)
            {
                rows += handler.Rows;
            }

            surroundingMap = SurroundingMap.CreateSurroundingTilesDictionary(config.columns, rows, 3);
            logger.Log($"Surrounding map created with {surroundingMap.Count} entries");
            logger.Log($"Device mapping created with {deviceMapping.Count} entries");

            for (int i = 0; i < config.MaxPlayers; i++)
            {
                playerTargets[i] = new HashSet<int>(); // Initialize target list for each player
            }

            statusPublisher.PublishStatus(scores, config.MaxLifeLines, Level, GameStatus.NotStarted, IterationTime, config.GameName, iterations);
        }

        public int[] Scores
        {
            get { return scores; }
            set
            {
                value.CopyTo(scores, 0);
                statusPublisher.PublishStatus(scores, lifeLine, Level, status, IterationTime, config.GameName, iterations);
                OnScoresChanged(scores);
                LogData($"Scores: {string.Join(", ", scores)}");
            }
        }

        public override string Status
        {
            get { return status; }
            set
            {
                status = value;
                statusPublisher.PublishStatus(scores, lifeLine, Level, status, IterationTime, config.GameName, iterations);
                OnStatusChanged(status);
            }
        }

        public override int Level
        {
            get { return level; }
            set
            {
                level = value;
                statusPublisher.PublishStatus(scores, lifeLine, Level, status, IterationTime, config.GameName, iterations);
                OnLevelChanged(level);
            }
        }

        public override int LifeLine
        {
            get { return lifeLine; }
            set
            {
                lifeLine = value;
                statusPublisher.PublishStatus(scores, lifeLine, Level, status, IterationTime, config.GameName, iterations);
                OnLifelineChanged(value);
                LogData($"LifeLine: {lifeLine}");
            }
        }

        public event EventHandler<int[]> ScoresChanged;

        protected virtual void OnScoresChanged(int[] newScores)
        {
            LogData($"Scores updated to: {string.Join(", ", newScores)}");
            ScoresChanged?.Invoke(this, newScores);
        }

        protected void UpdateScore(int playerIndex, int newScore)
        {
            scores[playerIndex] = newScore;
            statusPublisher.PublishStatus(scores, lifeLine, Level, status, IterationTime, config.GameName, iterations);
            OnScoresChanged(scores);
            LogData($"Player {playerIndex} score updated: {newScore}");
        }

        protected int getHighestScoreIndex()
        {
            int highestScore = scores[0];
            int highestScoreIndex = 0;

            for (int i = 1; i < scores.Length; i++)
            {
                if (scores[i] > highestScore)
                {
                    highestScore = scores[i];
                    highestScoreIndex = i;
                }
            }
            return highestScoreIndex;
        }

        override protected void IterationWon()
        {
            isGameRunning = false;
            udpHandlers.ForEach(x => x.StopReceive());
            LogData($"All targets hit iterations:{iterations} passed");
            if (config.timerPointLoss)
                iterationTimer.Dispose();
            iterations = iterations + 1;



            if (iterations >= config.Maxiterations)
            {

                Status = $"{GameStatus.Running}: Moved to Next Level {Level}";
                LogData($"Game Win level: {Level}");
                Level = Level + 1;
                iterations = 1;
                int highest = getHighestScoreIndex();
                if (Level >= config.MaxLevel)
                {
                    Status = $"Reached to last Level {config.MaxLevel} ending game. Player {highest + 1} wins";
                    LogData(Status);
                    musicPlayer.Announcement($"content/voicelines/winPlayer{highest + 1}.mp3");
                    Status = GameStatus.Completed;
                    EndGame();
                    return;
                }
                else
                {
                    //Text to speech: Great job, Team! 🎉You’ve won this level! Now, get ready for the next one.Expect more energy and excitement.  Let’s go! 🚀 one two three go 
                    LogData(Status);
                    musicPlayer.Announcement($"content/voicelines/level_{Level}.mp3");
                }

            }
            else { BlinkAllAsync(1); }
            Status = $"{GameStatus.Running}: Moved to Next iterations {iterations}";
            RunGameInSequence();
            LogData($"moving to next iterations: {iterations} Iteration time: {IterationTime} ");
        }

        protected void AssignPlayerTargets()
        {
            // Logic for assigning targets per player across multiple devices
        }

        protected void ResetGameForPlayers()
        {
            // Logic for resetting the game state for each player
        }

        protected int Resequencer(int index, UdpHandler handler)
        {
            if ((index / config.columns) % 2 == 0)
            {
                return index;
            }

            int columns = config.columns;
            int row = index / columns;
            int column = index % columns;
            int dest = (row + 1) * columns - 1 - column;
            return dest;
        }

        protected void AnimateColor(bool reverse)
        {

            foreach (var handler in udpHandlers)
            {
                for (int iterations = 0; iterations < handler.Rows; iterations++)
                {
                    for (int i = 0; i < handler.DeviceList.Count; i++)
                    {
                        handler.DeviceList[i] = ColorPaletteone.Red;
                    }

                    int row = (iterations / handler.Rows) % 2 == 0 ? (iterations % handler.Rows) : handler.Rows - 1 - (iterations % handler.Rows);

                    if (reverse)
                    {
                        row = handler.Rows - row - 1;
                    }

                    for (int i = 0; i < config.columns; i++)
                    {
                        handler.DeviceList[row * config.columns + i] = ColorPaletteone.Green;
                    }

                    handler.SendColorsToUdp(handler.DeviceList);
                    Thread.Sleep(75);
                }
            }
        }

        protected override async void StartAnimition()
        {
            AnimateGrowth(ColorPaletteone.Pink);
        }
        protected void AnimateGrowth(string color)
        {
            {
                var handler = udpHandlers[0];
                int totalLights = handler.DeviceList.Count * udpHandlers.Count;
                List<int> unchanged = new List<int>(Enumerable.Range(0, handler.DeviceList.Count * udpHandlers.Count));
                for (double i = 0.00; i < totalLights && isGameRunning; i++)
                {
                    int random = new Random().Next(0, unchanged.Count);
                    int handlerIndex = unchanged[random] / handler.DeviceList.Count;
                    int position = unchanged[random] % handler.DeviceList.Count;
                    Console.WriteLine($"index {handlerIndex} position {position} random {random}");
                    udpHandlers[handlerIndex].DeviceList[unchanged[random] - handler.DeviceList.Count * handlerIndex] = color;
                    unchanged.RemoveAt(random);

                    udpHandlers[handlerIndex].SendColorsToUdp(udpHandlers[handlerIndex].DeviceList);
                    Thread.Sleep(Convert.ToInt32(38.00 - i / 6));
                }
            }
        }

        protected void SendColorToUdpAsync()
        {

            var tasks = new List<Task>();
            foreach (var handler in udpHandlers)
            {
                tasks.Add(handler.SendColorsToUdpAsync(handler.DeviceList));
            }
            Task.WhenAll(tasks);


        }
    }
}
