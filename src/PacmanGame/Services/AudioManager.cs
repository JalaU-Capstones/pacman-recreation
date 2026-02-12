using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using PacmanGame.Helpers;
using PacmanGame.Services.Interfaces;
using SFML.Audio;

namespace PacmanGame.Services;

/// <summary>
/// Service for managing audio playback using SFML.Audio.
/// Works on both Windows and Linux.
/// </summary>
public class AudioManager : IAudioManager, IDisposable
{
    private readonly ILogger<AudioManager> _logger;
    private readonly string _musicPath;
    private readonly string _sfxPath;
    private bool _isMuted;
    private float _menuMusicVolume = 50f; // SFML uses 0-100
    private float _gameMusicVolume = 50f; // SFML uses 0-100
    private float _sfxVolume = 70f;   // SFML uses 0-100
    private bool _isInitialized;

    private Music? _currentMusic;
    private string? _currentMusicName;
    private readonly List<Sound> _activeSounds = new();
    private readonly Dictionary<string, SoundBuffer> _soundBuffers = new();

    public bool IsMuted => _isMuted;
    public float MenuMusicVolume => _menuMusicVolume / 100f;
    public float GameMusicVolume => _gameMusicVolume / 100f;
    public float SfxVolume => _sfxVolume / 100f;

    public AudioManager(ILogger<AudioManager> logger)
    {
        _logger = logger;
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
            if (!Directory.Exists(_musicPath))
            {
                _logger.LogWarning($"Music directory not found: {_musicPath}");
            }

            if (!Directory.Exists(_sfxPath))
            {
                _logger.LogWarning($"SFX directory not found: {_sfxPath}");
            }

            // Preload common sound effects
            PreloadSound("chomp");
            PreloadSound("death");
            PreloadSound("eat-ghost");
            PreloadSound("eat-fruit");
            PreloadSound("game-start");
            PreloadSound("game-over");
            PreloadSound("menu-select");
            PreloadSound("menu-navigate");

            _isInitialized = true;
            _logger.LogInformation("AudioManager initialized (SFML.Audio)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioManager initialization error");
            // Don't set initialized to true if SFML fails to load
        }
    }

    private void PreloadSound(string soundName)
    {
        try
        {
            string filePath = Path.Combine(_sfxPath, $"{soundName}.wav");
            if (File.Exists(filePath))
            {
                _soundBuffers[soundName] = new SoundBuffer(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to preload sound {soundName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Play a sound effect (fire and forget, non-blocking)
    /// </summary>
    public void PlaySoundEffect(string soundName)
    {
        if (_isMuted || !_isInitialized)
            return;

        try
        {
            SoundBuffer? buffer;

            // Try to get from cache, or load if not cached
            if (!_soundBuffers.TryGetValue(soundName, out buffer))
            {
                string filePath = Path.Combine(_sfxPath, $"{soundName}.wav");
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Sound effect file not found: {soundName}.wav");
                    return;
                }

                buffer = new SoundBuffer(filePath);
                _soundBuffers[soundName] = buffer;
            }

            // Clean up stopped sounds
            _activeSounds.RemoveAll(s => s.Status == SoundStatus.Stopped);

            // Create and play new sound
            var sound = new Sound(buffer);
            sound.Volume = _sfxVolume;
            sound.Play();
            _activeSounds.Add(sound);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to play sound {soundName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Play background music (only one at a time, supports looping)
    /// </summary>
    public void PlayMusic(string musicName, bool loop = true)
    {
        if (!_isInitialized)
            return;

        try
        {
            StopMusic();

            if (_isMuted)
                return;

            string filePath = Path.Combine(_musicPath, $"{musicName}");

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Music file not found: {filePath}");
                return;
            }

            _currentMusic = new Music(filePath);
            _currentMusicName = musicName;
            _currentMusic.Loop = loop;

            // Set volume based on music type
            UpdateCurrentMusicVolume();

            _currentMusic.Play();
            _logger.LogInformation($"Playing music: {musicName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to play music {musicName}", ex);
        }
    }

    private void UpdateCurrentMusicVolume()
    {
        if (_currentMusic == null) return;

        if (_isMuted)
        {
            _currentMusic.Volume = 0;
            return;
        }

        if (_currentMusicName != null && _currentMusicName.Contains("menu"))
        {
            _currentMusic.Volume = _menuMusicVolume;
        }
        else
        {
            _currentMusic.Volume = _gameMusicVolume;
        }
    }

    /// <summary>
    /// Stop the currently playing music
    /// </summary>
    public void StopMusic()
    {
        try
        {
            if (_currentMusic != null)
            {
                _currentMusic.Stop();
                _currentMusic.Dispose();
                _currentMusic = null;
                _currentMusicName = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error stopping music: {ex.Message}");
        }
    }

    /// <summary>
    /// Pause the currently playing music
    /// </summary>
    public void PauseMusic()
    {
        try
        {
            if (_currentMusic != null && _currentMusic.Status == SoundStatus.Playing)
            {
                _currentMusic.Pause();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error pausing music: {ex.Message}");
        }
    }

    /// <summary>
    /// Resume paused music
    /// </summary>
    public void ResumeMusic()
    {
        try
        {
            if (_currentMusic != null && _currentMusic.Status == SoundStatus.Paused && !_isMuted)
            {
                _currentMusic.Play();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error resuming music: {ex.Message}");
        }
    }

    /// <summary>
    /// Set the master music volume (0.0 to 1.0)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        // This sets both for backward compatibility, but prefer specific methods
        float vol = Math.Clamp(volume, 0f, 1f) * 100f;
        _menuMusicVolume = vol;
        _gameMusicVolume = vol;
        UpdateCurrentMusicVolume();
    }

    public void SetMenuMusicVolume(float volume)
    {
        _menuMusicVolume = Math.Clamp(volume, 0f, 1f) * 100f;
        if (_currentMusicName != null && _currentMusicName.Contains("menu"))
        {
            UpdateCurrentMusicVolume();
        }
    }

    public void SetGameMusicVolume(float volume)
    {
        _gameMusicVolume = Math.Clamp(volume, 0f, 1f) * 100f;
        if (_currentMusicName != null && !_currentMusicName.Contains("menu"))
        {
            UpdateCurrentMusicVolume();
        }
    }

    /// <summary>
    /// Set the sound effects volume (0.0 to 1.0)
    /// </summary>
    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Math.Clamp(volume, 0f, 1f) * 100f; // Convert to 0-100
        // Note: This only affects future sounds, not currently playing ones
    }

    /// <summary>
    /// Mute or unmute all audio
    /// </summary>
    public void SetMuted(bool muted)
    {
        _isMuted = muted;
        if (muted)
        {
            if (_currentMusic != null)
            {
                _currentMusic.Volume = 0;
            }

            foreach (var sound in _activeSounds)
            {
                sound.Volume = 0;
            }
            _logger.LogInformation("Audio muted");
        }
        else
        {
            UpdateCurrentMusicVolume();
            _logger.LogInformation("Audio unmuted");
            // We don't restore volume for active sounds as they are short-lived
        }
    }

    public void Dispose()
    {
        StopMusic();

        foreach (var sound in _activeSounds)
        {
            sound.Dispose();
        }
        _activeSounds.Clear();

        foreach (var buffer in _soundBuffers.Values)
        {
            buffer.Dispose();
        }
        _soundBuffers.Clear();
        _logger.LogInformation("AudioManager disposed");
    }
}
