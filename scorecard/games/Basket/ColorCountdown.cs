using NAudio.Wave;
using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace scorecard.games.Basket
{
    public class ColorCountdown : BaseSingleDevice
    {
        readonly int[] switchCounts = { 3, 4, 5, 6, 7 }; // This array follows the indicies of the level-1 so level 1 the baskets switch 3 times, level 2 switches 3, etc
        readonly int[] basketCounts = { 3, 3, 2, 2, 2 }; // Same with this array but it tracks the basket counts

        const int ROUND_DURATION = 15000; // Round duration is configurable (currently 15 sec)

        string backgroundColor;
        CoolDown coolDown;

        private bool gameEnded = false;

        Dictionary<int, bool> currentHitStatus;
        private List<int> lastActiveIndices = new List<int>();

        // Audio file paths
        private string hitSoundPath = "content/Basket/ColorCountdown/hit-sound.mp3";
        private string missSoundPath = "content/Basket/ColorCountdown/miss-sound.mp3";
        private string roundEndSoundPath = "content/Basket/ColorCountdown/round-end.mp3";
        private string backgroundMusicPath = "content/Basket/ColorCountdown/background-music.mp3";
        private string gameEndSoundPath = "content/Basket/ColorCountdown/game-over.mp3";

        public ColorCountdown(GameConfig co) : base(co)
        {

            Task.Run(() =>
            {
                Thread.Sleep(100); 
                musicPlayer.StopBackgroundMusic();
            });

            targetColor = config.NoofLedPerdevice == 3 ? ColorPalette.Green : ColorPaletteone.Green;
            backgroundColor = config.NoofLedPerdevice == 3 ? ColorPalette.Red : ColorPaletteone.Red;
            coolDown = new CoolDown();
            currentHitStatus = new Dictionary<int, bool>();

            OverrideBackgroundMusic();
        }

        protected override void Initialize()
        {
            base.BlinkAllAsync(2);
        }

        protected override async void StartAnimition()
        {
            LoopAll();
            base.StartAnimition();
        }

        /// <summary>
        /// In each round, we switch basket configurations a fixed number of times.
        /// </summary>
        protected override async void OnIteration()
        {
            coolDown.SetFlagTrue(500);

            int round = Level;
            if (round > 5)
            {
                EndGame();
                return;
            }

            int switches = switchCounts[round - 1];
            int basketsActive = basketCounts[round - 1];

            int interval = ROUND_DURATION / (switches + 1);

            currentHitStatus = new Dictionary<int, bool>();
            List<int> activeBaskets = ChooseRandomBaskets(basketsActive);

            // Store active indices for later use (for blinking at round end).
            lastActiveIndices = new List<int>(activeBaskets);

            foreach (var idx in activeBaskets)
            {
                currentHitStatus[idx] = false;
            }

            UpdateDeviceColors(activeBaskets);

            // For each switch, wait the interval then change the config
            for (int i = 0; i < switches; i++)
            {
                await Task.Delay(interval);
                activeBaskets = ChooseRandomBaskets(basketsActive);
                lastActiveIndices = new List<int>(activeBaskets);
                currentHitStatus = new Dictionary<int, bool>();
                foreach (var idx in activeBaskets)
                {
                    currentHitStatus[idx] = false;
                }

                UpdateDeviceColors(activeBaskets);
            }

            int elapsed = interval * (switches + 1);
            int remaining = ROUND_DURATION - elapsed;
            if (remaining > 0)
            {
                await Task.Delay(remaining);
            }

            IterationWon();
        }

        protected override void IterationWon()
        {
            isGameRunning = false;
            udpHandlers.ForEach(x => x.StopReceive());

            if (config.timerPointLoss)
                iterationTimer.Dispose();

            iterations++;

            ClearAllHoops();

            Task.Run(() => PlaySoundAsync(roundEndSoundPath));

            BlinkAllHoops(2);

            if (iterations >= config.Maxiterations) 
            {
                Status = $"{GameStatus.Running}: Moved to Next Level {Level}";
                Level++;
                iterations = 1;
                if (Level >= config.MaxLevel)
                {
                    Status = $"Reached the last Level {config.MaxLevel}, ending game";
                    musicPlayer.StopBackgroundMusic();
                    //LogData("Background music stopped; now playing game end sound.");
                    Task.Run(() => PlaySoundAsync(gameEndSoundPath)).Wait();
                    //LogData("Attempting to play game over sound.");
                    Task.Delay(2000).Wait();

                    AnnounceFinalScore();   

                    EndGame();
                    return;
                }
                else
                {
                    OverrideBackgroundMusic();
                }
            }
            else
            {
                BlinkAllHoops(2); // Blinks hoops twice for round end
            }

            Status = $"{GameStatus.Running}: Moved to Next Iteration {iterations}";

            // This will reapply custom background music on new iteration
            if (!gameEnded)
            {
                OverrideBackgroundMusic();
                RunGameInSequence();
            }
        }

        protected override void IterationLost(object state)
        {
            if (gameEnded)
            {
                return;
            }
            base.IterationLost(state);
        }

        protected override void OnStart()
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }

        public new void EndGame()
        {
            isGameRunning = false;
            iterationTimer?.Dispose();
            iterationTimer = null;
            udpHandlers.ForEach(x => x.StopReceive());
            musicPlayer.Dispose();
            gameEnded = true;
            LogData("Game has ended. No further iterations will be run.");
        }

        /// <summary>
        /// Chooses a random set of device indices for active baskets.
        /// </summary>
        private List<int> ChooseRandomBaskets(int count)
        {
            List<int> available = Enumerable.Range(0, handler.DeviceList.Count).ToList();
            List<int> chosen = new List<int>();
            Random rnd = new Random();
            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int idx = rnd.Next(available.Count);
                chosen.Add(available[idx]);
                available.RemoveAt(idx);
            }
            return chosen;
        }

        private void UpdateDeviceColors(List<int> activeIndices)
        {
            for (int i = 0; i < handler.DeviceList.Count; i++)
            {
                if (activeIndices.Contains(i))
                    handler.DeviceList[i] = targetColor;
                else
                    handler.DeviceList[i] = backgroundColor;
            }
            handler.SendColorsToUdp(handler.DeviceList);
        }

        /// <summary>
        /// Processes incoming hit signals.
        /// For an active basket, awards points and plays custom hit sound.
        /// For a wrong hit, deducts points and plays custom miss sound.
        /// </summary>
        private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
        {
            if (!isGameRunning)
                return;
            string receivedData = Encoding.UTF8.GetString(receivedBytes);
            var touchedDevices = receivedData
                .Select((value, index) => new { value, index })
                .Where(x => x.value == 0x0A)
                .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                .ToList();

            if (touchedDevices.Count > 0 && !coolDown.Flag)
            {
                foreach (var device in touchedDevices)
                {
                    if (currentHitStatus.ContainsKey(device))
                    {
                        // Update score directly rather than calling update score
                        Score += 2;
                        Task.Run(() => PlaySoundAsync(hitSoundPath));
                        //LogData($"Custom hit on basket {device}. Score updated to {Score}.");
                        currentHitStatus[device] = true;
                    }
                    else
                    {
                        Score = Math.Max(Score - 2, 0);
                        Task.Run(() => PlaySoundAsync(missSoundPath));
                        //LogData($"Custom miss on basket {device}. Score updated to {Score}.");
                    }
                }
            }
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }

        /// <summary>
        /// Stops the default background music and starts playing your custom background music.
        /// </summary>
        private void OverrideBackgroundMusic()
        {
            musicPlayer.StopBackgroundMusic();
            musicPlayer.PlayBackgroundMusic(backgroundMusicPath, repeat: true);
        }

        /// <summary>
        /// Asynchronously plays a sound from the given relative file path.
        /// </summary>
        private async Task PlaySoundAsync(string filePath)
        {
            string absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);

            if (File.Exists(absolutePath))
            {
                //LogData($"Playing sound: {absolutePath}");
                using (var audioFile = new AudioFileReader(absolutePath))
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();

                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        await Task.Delay(100);
                    }
                }
            }
            else
            {
                LogData($"ERROR: Sound file not found at {absolutePath}");
            }
        }

        /// <summary>
        /// Clears all hoops by setting their color to "no color".
        /// </summary>
        private void ClearAllHoops()
        {
            string noColor = config.NoofLedPerdevice == 3 ? ColorPalette.noColor3 : ColorPaletteone.NoColor;
            for (int i = 0; i < handler.DeviceList.Count; i++)
            {
                handler.DeviceList[i] = noColor;
            }
            handler.SendColorsToUdp(handler.DeviceList);
        }


        /// <summary>
        /// Blinks all hoops a specified number of times.
        /// </summary>
        private void BlinkAllHoops(int times)
        {
            string blinkColor = config.NoofLedPerdevice == 3 ? ColorPalette.Blue : ColorPaletteone.Blue;
            string noColor = config.NoofLedPerdevice == 3 ? ColorPalette.noColor3 : ColorPaletteone.NoColor;
            for (int t = 0; t < times; t++)
            {
                for (int i = 0; i < handler.DeviceList.Count; i++)
                {
                    handler.DeviceList[i] = blinkColor;
                }
                handler.SendColorsToUdp(handler.DeviceList);
                Thread.Sleep(200);

                for (int i = 0; i < handler.DeviceList.Count; i++)
                {
                    handler.DeviceList[i] = noColor;
                }
                handler.SendColorsToUdp(handler.DeviceList);
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Uses a default windows text to speech to announce score
        /// TODO: Find a good package to install to change this voice and make it sound not so robotic lol or find a solution for this
        /// </summary>
        private void AnnounceFinalScore()
        {
            try
            {
                Type t = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (t == null)
                {
                    //LogData("SAPI.SpVoice type not found.");
                    return;
                }
                dynamic voice = Activator.CreateInstance(t);
                string announcement = $"Your final score is {Score}";
                LogData($"Announcing: {announcement}");
                voice.Speak(announcement, 0);
            }
            catch (Exception ex)
            {
                LogData($"Error in AnnounceFinalScore: {ex.Message}");
            }
        }

    }
}
