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
    AsyncLogger logger;

    public MusicPlayer(string backgroundFile, AsyncLogger logger)
    {
        this.logger = logger;
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
        if (repeatBackgroundMusic && backgroundAudioFile != null)
        {
            backgroundAudioFile.Position = 0;
            backgroundMusicPlayer.Play();
        }
    }

    public void PlayEffect(string filePath)
    {
       logger.Log($"using playefect {filePath} {isPlayingEffect}");
        if (!File.Exists(filePath))
        {
            logger.Log($"Music File not found: {filePath}");
            return;
        }

        effectQueue.Enqueue(filePath);
        if (!isPlayingEffect)
        {
          
            PlayNextEffect();
        }
    }

    private void PlayNextEffect()
    {
        if (effectQueue.TryDequeue(out string filePath))
        {
            isPlayingEffect = true;
            effectPlayingTask = Task.Run(() =>
            {
                try
                {
                    if (effectsPlayer != null && effectsPlayer.PlaybackState == PlaybackState.Playing)
                    {
                        effectsPlayer.Stop();
                    }

                    effectsPlayer = new WaveOutEvent();
                    var audioFile = new AudioFileReader(filePath);
                    if (effectsPlayer == null)
                    {
                        logger.Log("sound could not play as effectplayer is null");
                        return;

                    }
                    effectsPlayer.Init(audioFile);
                    logger.Log(filePath);
                    // effectsPlayer.Volume = 1.0f;
                    if (effectsPlayer == null)
                    {
                        logger.Log("sound could not play as effectplayer is null");
                        return;

                    }
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
                                if (backgroundMusicPlayer != null)
                                {
                                    backgroundMusicPlayer.Volume = 0.4f;
                                }
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
            effectsPlayer?.Dispose();
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

    public void Announcement(string filePath)
    {
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
            PlayBackgroundMusic(backgroundFilePath, true);
       // }
    }
}
