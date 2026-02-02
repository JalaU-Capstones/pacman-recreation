using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using PacmanGame.Helpers;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.Services;

/// <summary>
/// Service for managing audio playback using NAudio for cross-platform WAV support.
/// Handles background music, sound effects, and volume control.
/// </summary>
public class AudioManager : IAudioManager
{
    private readonly string _musicPath;
    private readonly string _sfxPath;
    private IWavePlayer? _musicPlayer;
    private ISampleProvider? _musicReader;
    private AudioFileReader? _baseAudioReader;
    private bool _isMuted;
    private float _musicVolume = 0.5f;
    private float _sfxVolume = 0.7f;
    private bool _isInitialized;
    private readonly Queue<IWavePlayer> _activeSfxPlayers = new();
    private const int MaxConcurrentSfx = 8;

    public bool IsMuted => _isMuted;

    public AudioManager()
    {
        _musicPath = Path.Combine(AppContext.BaseDirectory, Constants.MusicPath);
        _sfxPath = Path.Combine(AppContext.BaseDirectory, Constants.SfxPath);
        _isMuted = false;
    }

    /// <summary>
    /// Initialize the audio system
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        try
        {
            // Verify audio directories exist
            if (!Directory.Exists(_musicPath))
            {
                Console.WriteLine($"⚠️  Music directory not found: {_musicPath}");
            }

            if (!Directory.Exists(_sfxPath))
            {
                Console.WriteLine($"⚠️  SFX directory not found: {_sfxPath}");
            }

            _isInitialized = true;
            Console.WriteLine("✅ AudioManager initialized (NAudio - Real WAV Playback)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error initializing AudioManager: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Play a sound effect (fire-and-forget, non-blocking, multiple can overlap)
    /// </summary>
    public void PlaySoundEffect(string soundName)
    {
        if (_isMuted || !_isInitialized)
            return;

        try
        {
            string filePath = Path.Combine(_sfxPath, $"{soundName}.wav");

            if (!File.Exists(filePath))
            {
                return;
            }

            // Create a new player for this SFX
            var sfxPlayer = new WaveOutEvent();
            var sfxReader = new AudioFileReader(filePath);
            sfxReader.Volume = _sfxVolume;

            sfxPlayer.Init(sfxReader);
            sfxPlayer.Play();

            // Track the player and clean up when done
            _activeSfxPlayers.Enqueue(sfxPlayer);
            if (_activeSfxPlayers.Count > MaxConcurrentSfx)
            {
                var oldPlayer = _activeSfxPlayers.Dequeue();
                try
                {
                    oldPlayer.Dispose();
                }
                catch (Exception)
                {
                    // Ignore disposal errors
                }
            }

            // Clean up disposed players periodically
            var playersToRemove = new List<IWavePlayer>();
            foreach (var player in _activeSfxPlayers)
            {
                if (player.PlaybackState == PlaybackState.Stopped)
                {
                    playersToRemove.Add(player);
                }
            }

            foreach (var player in playersToRemove)
            {
                _activeSfxPlayers.Dequeue();
                try
                {
                    player.Dispose();
                }
                catch (Exception)
                {
                    // Ignore disposal errors
                }
            }
        }
        catch (Exception)
        {
            // Silently fail - game continues even if audio fails
        }
    }

    /// <summary>
    /// Play background music (only one at a time, supports looping)
    /// </summary>
    public void PlayMusic(string musicName, bool loop = true)
    {
        if (_isMuted || !_isInitialized)
            return;

        try
        {
            // Stop current music first
            StopMusic();

            string filePath = Path.Combine(_musicPath, $"{musicName}.wav");

            if (!File.Exists(filePath))
            {
                return;
            }

            _musicPlayer = new WaveOutEvent();
            _baseAudioReader = new AudioFileReader(filePath);
            _baseAudioReader.Volume = _musicVolume;

            ISampleProvider reader = _baseAudioReader;
            if (loop)
            {
                // Create a looping reader
                reader = new LoopingAudioFileReader(_baseAudioReader);
            }

            _musicReader = reader;
            _musicPlayer.Init(_musicReader);
            _musicPlayer.Play();
        }
        catch (Exception)
        {
            // Silently fail on Linux where winmm.dll is not available
        }
    }

    /// <summary>
    /// Stop the currently playing music
    /// </summary>
    public void StopMusic()
    {
        try
        {
            if (_musicPlayer != null)
            {
                _musicPlayer.Stop();
                _musicPlayer.Dispose();
                _musicPlayer = null;
            }

            if (_musicReader != null)
            {
                if (_musicReader is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _musicReader = null;
            }

            if (_baseAudioReader != null)
            {
                _baseAudioReader.Dispose();
                _baseAudioReader = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error stopping music: {ex.Message}");
        }
    }

    /// <summary>
    /// Pause the currently playing music
    /// </summary>
    public void PauseMusic()
    {
        try
        {
            if (_musicPlayer?.PlaybackState == PlaybackState.Playing)
            {
                _musicPlayer.Pause();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error pausing music: {ex.Message}");
        }
    }

    /// <summary>
    /// Resume paused music
    /// </summary>
    public void ResumeMusic()
    {
        try
        {
            if (_musicPlayer?.PlaybackState == PlaybackState.Paused)
            {
                _musicPlayer.Play();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error resuming music: {ex.Message}");
        }
    }

    /// <summary>
    /// Set the volume for music (0.0 to 1.0)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        _musicVolume = Math.Clamp(volume, 0f, 1f);
        try
        {
            if (_baseAudioReader != null)
            {
                _baseAudioReader.Volume = _musicVolume;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error setting music volume: {ex.Message}");
        }
    }

    /// <summary>
    /// Set the volume for sound effects (0.0 to 1.0)
    /// </summary>
    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Math.Clamp(volume, 0f, 1f);
    }

    /// <summary>
    /// Mute/unmute all audio
    /// </summary>
    public void SetMuted(bool muted)
    {
        _isMuted = muted;

        try
        {
            if (muted)
            {
                PauseMusic();
            }
            else
            {
                ResumeMusic();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error setting mute state: {ex.Message}");
        }
    }
}

/// <summary>
/// Helper class to loop audio playback
/// </summary>
internal class LoopingAudioFileReader : ISampleProvider
{
    private readonly AudioFileReader _baseReader;

    public WaveFormat WaveFormat => _baseReader.WaveFormat;

    public LoopingAudioFileReader(AudioFileReader reader)
    {
        _baseReader = reader;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int totalRead = 0;

        while (totalRead < count)
        {
            int read = _baseReader.Read(buffer, offset + totalRead, count - totalRead);
            if (read == 0)
            {
                // EOF reached, seek back to start
                _baseReader.Position = 0;
                read = _baseReader.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                    break;
            }

            totalRead += read;
        }

        return totalRead;
    }
}

