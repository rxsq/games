using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

public class MusicPlayer
{
    private WaveOutEvent backgroundMusicPlayer;
    private WaveOutEvent effectsPlayer;
    private AudioFileReader backgroundAudioFile;
    private bool repeatBackgroundMusic;
    private ConcurrentQueue<string> effectQueue;
    private Task effectPlayingTask;
    private bool isPlayingEffect;
    string backgroundFilePath;
    private readonly object effectsLock = new object();
    //Logger logger;

    public MusicPlayer(string backgroundFile)
    {
        //this.logger = logger;
        effectQueue = new ConcurrentQueue<string>();
        isPlayingEffect = false;
         backgroundFilePath=backgroundFile;
    }

    public void Dispose()
    {
        StopAllMusic();
    }

    public void PlayBackgroundMusic(string filePath, bool repeat = false)
    {
        if (!File.Exists(filePath))
        {
            logger.Log($"Music File not found: {filePath}");
            return;
        }

        try
        {
            repeatBackgroundMusic = repeat;

            if (backgroundMusicPlayer == null)
            {
                backgroundMusicPlayer = new WaveOutEvent();
                backgroundMusicPlayer.PlaybackStopped += BackgroundMusicPlayer_PlaybackStopped;
            }
            else if (backgroundMusicPlayer.PlaybackState == PlaybackState.Playing)
            {
                backgroundMusicPlayer.Stop();
                backgroundMusicPlayer.Dispose();
                backgroundMusicPlayer = new WaveOutEvent();
                backgroundMusicPlayer.PlaybackStopped += BackgroundMusicPlayer_PlaybackStopped;
            }

            backgroundAudioFile = new AudioFileReader(filePath);
            backgroundMusicPlayer.Init(backgroundAudioFile);
            backgroundMusicPlayer.Volume = 0.4f;
            backgroundMusicPlayer.Play();
        }
        catch (Exception ex)
        {
            logger.Log($"Error playing background music: {ex.Message}");
            CleanUpBackgroundMusicResources();
        }
    }

    private void BackgroundMusicPlayer_PlaybackStopped(object sender, StoppedEventArgs e)
    {
        try
        {
            if (repeatBackgroundMusic && backgroundAudioFile != null)
            {
                // Rewind the audio file
                backgroundAudioFile.Position = 0;

                // Ensure the player is initialized again before playing
                if (backgroundMusicPlayer != null)
                {
                    backgroundMusicPlayer.Stop(); // Stop in case it’s already running
                    backgroundMusicPlayer.Init(backgroundAudioFile);
                    backgroundMusicPlayer.Play();
                }
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Error in PlaybackStopped handler: {ex.Message}");
            CleanUpBackgroundMusicResources();
        }
    }


    public void PlayEffect(string filePath)
    {
       logger.Log($"using playeffect {filePath} {isPlayingEffect}");
        if (!File.Exists(filePath))
        {
            logger.Log($"Music File not found: {filePath}");
            return;
        }

        effectQueue.Enqueue(filePath);
        PlayNextEffect();
        //if (!isPlayingEffect)
        //{
          
        //    PlayNextEffect();
        //}
    }

    private void PlayNextEffect()
    {
        if (effectQueue.TryDequeue(out string filePath))
        {
            isPlayingEffect = true;
            effectPlayingTask = Task.Run(() =>
            {
                lock (effectsLock)
                {
                    try
                    {
                        if (effectsPlayer != null && effectsPlayer.PlaybackState == PlaybackState.Playing)
                        {
                            effectsPlayer.Stop();
                            effectsPlayer.Dispose();
                        }

                        effectsPlayer = new WaveOutEvent();
                        var audioFile = new AudioFileReader(filePath);
                        effectsPlayer.Init(audioFile);
                        logger.Log($"Playing effect: {filePath}");
                        effectsPlayer.Play();

                        effectsPlayer.PlaybackStopped += (s, e) =>
                        {
                            try
                            {
                                audioFile.Dispose();
                            }
                            catch (Exception ex)
                            {
                                logger.Log($"Error disposing effect audio file: {ex.Message}");
                            }
                            finally
                            {
                                isPlayingEffect = false;
                                PlayNextEffect();
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"Error playing effect: {ex.Message}");
                        CleanUpEffectsResources();
                        isPlayingEffect = false;
                        PlayNextEffect();
                    }
                }
            });
        }
    }


    public void StopBackgroundMusic()
    {
        try
        {
            if (backgroundMusicPlayer != null && backgroundMusicPlayer.PlaybackState == PlaybackState.Playing)
            {
                repeatBackgroundMusic = false;
                backgroundMusicPlayer.Stop();
                CleanUpBackgroundMusicResources();
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Error stopping background music: {ex.Message}");
        }
    }

    public void StopAllMusic()
    {
        StopBackgroundMusic();
        StopEffects();
    }

    private void StopEffects()
    {
        try
        {
            if (effectsPlayer != null && effectsPlayer.PlaybackState == PlaybackState.Playing)
            {
                effectsPlayer.Stop();
                CleanUpEffectsResources();
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Error stopping effects: {ex.Message}");
        }
    }

    private void CleanUpBackgroundMusicResources()
    {
        try
        {
            backgroundMusicPlayer?.Dispose();
            backgroundAudioFile?.Dispose();
        }
        catch (Exception ex)
        {
            logger.Log($"Error cleaning up background music resources: {ex.Message}");
        }
        finally
        {
            backgroundMusicPlayer = null;
            backgroundAudioFile = null;
        }
    }

    private void CleanUpEffectsResources()
    {
        try
        {
            if (effectsPlayer != null)
            {
                if (effectsPlayer.PlaybackState != PlaybackState.Stopped)
                {
                    effectsPlayer.Stop();
                }
                effectsPlayer.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Error cleaning up effects resources: {ex.Message}");
        }
        finally
        {
            effectsPlayer = null;
        }
    }

    // bool ifAnnouncementPlaying = false;
    public void Announcement(string filePath, bool playbckMusic)
    {
        logger.Log($"Announcement: {filePath}");
        if (!File.Exists(filePath))
        {
            logger.Log($"Announcement file not found: {filePath}");
            return;
        }

        StopAllMusic();

        using (var announcementPlayer = new WaveOutEvent())
        using (var announcementFile = new AudioFileReader(filePath))
        {
            announcementPlayer.Init(announcementFile);
            announcementPlayer.Volume = 1.0f;
            announcementPlayer.Play();
            
            // Wait for the announcement to finish
            while (announcementPlayer.PlaybackState == PlaybackState.Playing)
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        // Resume background music if it was playing
        // if (backgroundAudioFile != null)
        // {
        if(playbckMusic )
        {
            PlayBackgroundMusic(backgroundFilePath, true);
        }
        logger.Log("Announcement finished");
        // }
    }
    public void Announcement(string filePath)
    {
        Announcement(filePath, true );
    }
}
