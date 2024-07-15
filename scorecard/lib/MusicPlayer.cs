using NAudio.Wave;
using System;
using System.IO;

public class MusicPlayer
{
    private WaveOutEvent backgroundMusicPlayer;
    private WaveOutEvent effectsPlayer;
    private AudioFileReader backgroundAudioFile;
    private bool repeatBackgroundMusic;

    public void PlayBackgroundMusic(string filePath, bool repeat = false)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Music File not found: {filePath}");
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
            Console.WriteLine($"Error playing background music: {ex.Message}");
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
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Music File not found: {filePath}");
            return;
        }

        try
        {
            if (effectsPlayer != null && effectsPlayer.PlaybackState == PlaybackState.Playing)
            {
                effectsPlayer.Stop();
                //effectsPlayer.Dispose();
            }

            effectsPlayer = new WaveOutEvent();
            var audioFile = new AudioFileReader(filePath);
            effectsPlayer.Init(audioFile);
            effectsPlayer.Volume = 1.0f;
            effectsPlayer.Play();
            effectsPlayer.PlaybackStopped += (s, e) =>
            {
                try
                {
                    audioFile.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing effect audio file: {ex.Message}");
                }
                finally
                {
                    if (backgroundMusicPlayer != null)
                    {
                        backgroundMusicPlayer.Volume = 0.4f;
                    }
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing effect: {ex.Message}");
            CleanUpEffectsResources();
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
            Console.WriteLine($"Error stopping background music: {ex.Message}");
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
            Console.WriteLine($"Error stopping effects: {ex.Message}");
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
            Console.WriteLine($"Error cleaning up background music resources: {ex.Message}");
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
            Console.WriteLine($"Error cleaning up effects resources: {ex.Message}");
        }
        finally
        {
            effectsPlayer = null;
        }
    }
}
