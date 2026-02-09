namespace PacmanGame.Services.Interfaces;

/// <summary>
/// Interface for managing audio playback (music and sound effects)
/// </summary>
public interface IAudioManager
{
    /// <summary>
    /// Initialize the audio system
    /// </summary>
    void Initialize();

    /// <summary>
    /// Play a sound effect
    /// </summary>
    /// <param name="soundName">Name of the sound effect (without .wav extension)</param>
    void PlaySoundEffect(string soundName);

    /// <summary>
    /// Play background music
    /// </summary>
    /// <param name="musicName">Name of the music file (without .wav extension)</param>
    /// <param name="loop">Whether to loop the music</param>
    void PlayMusic(string musicName, bool loop = true);

    /// <summary>
    /// Stop the currently playing music
    /// </summary>
    void StopMusic();

    /// <summary>
    /// Pause the currently playing music
    /// </summary>
    void PauseMusic();

    /// <summary>
    /// Resume paused music
    /// </summary>
    void ResumeMusic();

    /// <summary>
    /// Set the volume for music (0.0 to 1.0)
    /// </summary>
    /// <param name="volume">Volume level</param>
    void SetMusicVolume(float volume);

    /// <summary>
    /// Set the volume for menu music (0.0 to 1.0)
    /// </summary>
    /// <param name="volume">Volume level</param>
    void SetMenuMusicVolume(float volume);

    /// <summary>
    /// Set the volume for game music (0.0 to 1.0)
    /// </summary>
    /// <param name="volume">Volume level</param>
    void SetGameMusicVolume(float volume);

    /// <summary>
    /// Set the volume for sound effects (0.0 to 1.0)
    /// </summary>
    /// <param name="volume">Volume level</param>
    void SetSfxVolume(float volume);

    /// <summary>
    /// Mute or unmute all audio
    /// </summary>
    /// <param name="muted">True to mute, false to unmute</param>
    void SetMuted(bool muted);

    /// <summary>
    /// Get whether audio is currently muted
    /// </summary>
    bool IsMuted { get; }

    /// <summary>
    /// Get current menu music volume (0.0 to 1.0)
    /// </summary>
    float MenuMusicVolume { get; }

    /// <summary>
    /// Get current game music volume (0.0 to 1.0)
    /// </summary>
    float GameMusicVolume { get; }

    /// <summary>
    /// Get current SFX volume (0.0 to 1.0)
    /// </summary>
    float SfxVolume { get; }
}
