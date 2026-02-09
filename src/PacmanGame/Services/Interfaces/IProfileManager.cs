using System.Collections.Generic;
using PacmanGame.Models.Game;

namespace PacmanGame.Services.Interfaces;

/// <summary>
/// Interface for managing user profiles and scores.
/// </summary>
public interface IProfileManager
{
    /// <summary>
    /// Gets all existing profiles.
    /// </summary>
    List<Profile> GetAllProfiles();

    /// <summary>
    /// Creates a new profile.
    /// </summary>
    Profile CreateProfile(string name, string avatarColor);

    /// <summary>
    /// Gets a profile by its ID.
    /// </summary>
    Profile? GetProfileById(int id);

    /// <summary>
    /// Sets the currently active profile.
    /// </summary>
    void SetActiveProfile(int profileId);

    /// <summary>
    /// Gets the currently active profile.
    /// </summary>
    Profile? GetActiveProfile();

    /// <summary>
    /// Deletes a profile and its associated scores.
    /// </summary>
    void DeleteProfile(int profileId);

    /// <summary>
    /// Saves a score for the specified profile.
    /// </summary>
    void SaveScore(int profileId, int score, int level);

    /// <summary>
    /// Gets the top scores across all profiles.
    /// </summary>
    List<ScoreEntry> GetTopScores(int limit = 10);

    /// <summary>
    /// Saves settings for a specific profile.
    /// </summary>
    void SaveSettings(int profileId, Settings settings);

    /// <summary>
    /// Loads settings for a specific profile.
    /// </summary>
    Settings LoadSettings(int profileId);
}
