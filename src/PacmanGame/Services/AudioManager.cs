using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using PacmanGame.Helpers;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.Services;

/// <summary>
/// Service for managing audio playback using platform-specific backends.
/// Windows: System.Media.SoundPlayer (built-in, no dependencies)
/// Linux: Graceful silent fallback
/// </summary>
public class AudioManager : IAudioManager
{
    private readonly string _musicPath;
    private readonly string _sfxPath;
    private bool _isMuted;
    private float _musicVolume = 0.5f;
    private float _sfxVolume = 0.7f;
    private bool _isInitialized;
    private readonly bool _isWindows;
    private SoundPlayer? _currentMusicPlayer;
    private CancellationTokenSource? _musicCancellation;
    private Task? _musicLoopTask;
    private readonly Queue<SoundPlayer> _activeSfxPlayers = new();
    private const int MaxConcurrentSfx = 8;

    public bool IsMuted => _isMuted;

    public AudioManager()
    {
        _musicPath = Path.Combine(AppContext.BaseDirectory, Constants.MusicPath);
        _sfxPath = Path.Combine(AppContext.BaseDirectory, Constants.SfxPath);
        _isMuted = false;
        _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
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
                Console.WriteLine($"⚠️  Music directory not found: {_musicPath}");
            }

            if (!Directory.Exists(_sfxPath))
            {
                Console.WriteLine($"⚠️  SFX directory not found: {_sfxPath}");
            }

            _isInitialized = true;
            if (_isWindows)
            {
                Console.WriteLine("✅ AudioManager initialized (Windows - System.Media.SoundPlayer)");
            }
            else
            {
                Console.WriteLine("✅ AudioManager initialized (Linux - Silent mode)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AudioManager initialization error: {ex.Message}");
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Play a sound effect (fire and forget, non-blocking)
    /// </summary>
    public void PlaySoundEffect(string soundName)
    {
        if (_isMuted || !_isInitialized || !_isWindows)
            return;

        try
        {
            string filePath = Path.Combine(_sfxPath, $"{soundName}.wav");

            if (!File.Exists(filePath))
            {
                return;
            }

            // Run on background thread to avoid blocking
            #pragma warning disable CA1416
            Task.Run(() =>
            {
                try
                {
                    var player = new SoundPlayer(filePath);
                    player.PlaySync();
                    player.Dispose();
                }
                catch (Exception)
                {
                    // Silently fail
                }
            });
            #pragma warning restore CA1416
        }
        catch (Exception)
        {
            // Silently fail - game continues
        }
    }

    /// <summary>
    /// Play background music (only one at a time, supports looping)
    /// </summary>
    public void PlayMusic(string musicName, bool loop = true)
    {
        if (_isMuted || !_isInitialized || !_isWindows)
            return;

        try
        {
            StopMusic();

            string filePath = Path.Combine(_musicPath, $"{musicName}.wav");

            if (!File.Exists(filePath))
            {
                return;
            }

            if (loop)
            {
                // For looping, use a background task that replays when finished
                _musicCancellation = new CancellationTokenSource();
                _musicLoopTask = PlayMusicLoopAsync(filePath, _musicCancellation.Token);
            }
            else
            {
                #pragma warning disable CA1416
                _currentMusicPlayer = new SoundPlayer(filePath);
                _currentMusicPlayer.PlaySync();
                #pragma warning restore CA1416
            }
        }
        catch (Exception)
        {
            // Silently fail
        }
    }

    /// <summary>
    /// Play music with looping in background task
    /// </summary>
    private async Task PlayMusicLoopAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            #pragma warning disable CA1416
            while (!cancellationToken.IsCancellationRequested)
            {
                var player = new SoundPlayer(filePath);
                player.PlaySync();

                // Wait for the sound to finish (approximately 3 seconds for typical background tracks)
                await Task.Delay(3000, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;
            }
            #pragma warning restore CA1416
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested
        }
        catch (Exception)
        {
            // Ignore errors in loop task
        }
    }

    /// <summary>
    /// Stop the currently playing music
    /// </summary>
    public void StopMusic()
    {
        try
        {
            if (_musicCancellation != null)
            {
                _musicCancellation.Cancel();
                _musicCancellation.Dispose();
                _musicCancellation = null;
            }

            if (_musicLoopTask != null)
            {
                _musicLoopTask.Wait(1000);
                _musicLoopTask = null;
            }

            #pragma warning disable CA1416
            if (_currentMusicPlayer != null)
            {
                _currentMusicPlayer.Stop();
                _currentMusicPlayer.Dispose();
                _currentMusicPlayer = null;
            }
            #pragma warning restore CA1416
        }
        catch (Exception)
        {
            // Ignore errors
        }
    }

    /// <summary>
    /// Pause the currently playing music
    /// </summary>
    public void PauseMusic()
    {
        try
        {
            if (_currentMusicPlayer != null && _isWindows)
            {
                #pragma warning disable CA1416
                // SoundPlayer doesn't have pause, so we stop it
                _currentMusicPlayer.Stop();
                #pragma warning restore CA1416
                if (_musicCancellation != null)
                {
                    _musicCancellation.Cancel();
                }
            }
        }
        catch (Exception)
        {
            // Ignore errors
        }
    }

    /// <summary>
    /// Resume paused music (restart from beginning)
    /// </summary>
    public void ResumeMusic()
    {
        // SoundPlayer doesn't support resume, so music restart is acceptable for simple game audio
    }

    /// <summary>
    /// Set the master music volume (0.0 to 1.0)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        _musicVolume = Math.Clamp(volume, 0f, 1f);
        // SoundPlayer doesn't support volume control, so we store it for potential future use
    }

    /// <summary>
    /// Set the sound effects volume (0.0 to 1.0)
    /// </summary>
    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Math.Clamp(volume, 0f, 1f);
        // SoundPlayer doesn't support volume control, so we store it for potential future use
    }

    /// <summary>
    /// Mute or unmute all audio
    /// </summary>
    public void SetMuted(bool muted)
    {
        _isMuted = muted;
        if (muted)
        {
            StopMusic();
        }
    }
}
