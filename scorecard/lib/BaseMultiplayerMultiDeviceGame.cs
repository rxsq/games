using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
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

        protected override void IterationWon()
        {
            isGameRunning = false;
            udpHandlers.ForEach(x => x.StopReceive());
            LogData($"All targets hit; iterations: {iterations} passed");

            if (config.timerPointLoss)
                iterationTimer.Dispose();

            iterations++;

            if (iterations >= config.Maxiterations)
            {
                Status = $"{GameStatus.Running}: Moving to next level {Level}";
                LogData($"Level {Level} completed");

                Level++;
                iterations = 1;

                if (Level >= config.MaxLevel)
                {
                    Status = $"Reached last level {config.MaxLevel}, ending game";
                    LogData(Status);
                    musicPlayer.Announcement("content/GameWinAlllevelPassed.mp3");
                    EndGame();
                    return;
                }
                else
                {
                    LogData(Status);
                    musicPlayer.Announcement($"content/voicelines/level_{Level}.mp3");
                }
            }
            else
            {
                BlinkAllAsync(1);
            }

            Status = $"{GameStatus.Running}: Moving to next iteration {iterations}";
            RunGameInSequence();
            LogData($"Moving to next iteration: {iterations} with iteration time: {IterationTime}");
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

        protected override async void StartAnimition()
        {
            // Animation logic for the multiplayer multi-device setup
        }
    }
}
