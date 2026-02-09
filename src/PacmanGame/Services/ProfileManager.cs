using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.Services;

/// <summary>
/// Implementation of IProfileManager using SQLite.
/// </summary>
public class ProfileManager : IProfileManager
{
    private readonly string _connectionString;
    private Profile? _activeProfile;

    public ProfileManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "PacmanGame");
        Directory.CreateDirectory(folder);
        var dbPath = Path.Combine(folder, "profiles.db");
        _connectionString = $"Data Source={dbPath}";

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Profiles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                AvatarColor TEXT,
                CreatedAt TEXT NOT NULL,
                LastPlayedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS Scores (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProfileId INTEGER NOT NULL,
                Score INTEGER NOT NULL,
                Level INTEGER NOT NULL,
                Date TEXT NOT NULL,
                FOREIGN KEY (ProfileId) REFERENCES Profiles(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS UserSettings (
                ProfileId INTEGER PRIMARY KEY,
                MenuMusicVolume REAL NOT NULL DEFAULT 0.7,
                GameMusicVolume REAL NOT NULL DEFAULT 0.7,
                SfxVolume REAL NOT NULL DEFAULT 0.8,
                IsMuted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (ProfileId) REFERENCES Profiles(Id) ON DELETE CASCADE
            );
        ";
        command.ExecuteNonQuery();
    }

    public List<Profile> GetAllProfiles()
    {
        var profiles = new List<Profile>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT p.Id, p.Name, p.AvatarColor, p.CreatedAt, p.LastPlayedAt, MAX(s.Score) as HighScore
            FROM Profiles p
            LEFT JOIN Scores s ON p.Id = s.ProfileId
            GROUP BY p.Id
            ORDER BY p.LastPlayedAt DESC
        ";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            profiles.Add(new Profile
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                AvatarColor = reader.IsDBNull(2) ? null : reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3)),
                LastPlayedAt = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                HighScore = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
            });
        }

        return profiles;
    }

    public Profile CreateProfile(string name, string avatarColor)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Profiles (Name, AvatarColor, CreatedAt, LastPlayedAt)
            VALUES ($name, $avatarColor, $createdAt, $lastPlayedAt);
            SELECT last_insert_rowid();
        ";

        var now = DateTime.Now;
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$avatarColor", avatarColor);
        command.Parameters.AddWithValue("$createdAt", now.ToString("o"));
        command.Parameters.AddWithValue("$lastPlayedAt", now.ToString("o"));

        var id = (long)command.ExecuteScalar()!;

        var profile = new Profile
        {
            Id = (int)id,
            Name = name,
            AvatarColor = avatarColor,
            CreatedAt = now,
            LastPlayedAt = now
        };

        // Create default settings for the new profile
        SaveSettings((int)id, new Settings { ProfileId = (int)id });

        _activeProfile = profile;
        return profile;
    }

    public Profile? GetProfileById(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, AvatarColor, CreatedAt, LastPlayedAt FROM Profiles WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Profile
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                AvatarColor = reader.IsDBNull(2) ? null : reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3)),
                LastPlayedAt = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4))
            };
        }

        return null;
    }

    public void SetActiveProfile(int profileId)
    {
        _activeProfile = GetProfileById(profileId);
        if (_activeProfile != null)
        {
            // Update LastPlayedAt
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Profiles SET LastPlayedAt = $lastPlayedAt WHERE Id = $id";
            command.Parameters.AddWithValue("$lastPlayedAt", DateTime.Now.ToString("o"));
            command.Parameters.AddWithValue("$id", profileId);
            command.ExecuteNonQuery();

            _activeProfile.LastPlayedAt = DateTime.Now;
        }
    }

    public Profile? GetActiveProfile()
    {
        return _activeProfile;
    }

    public void DeleteProfile(int profileId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Profiles WHERE Id = $id";
        command.Parameters.AddWithValue("$id", profileId);
        command.ExecuteNonQuery();

        if (_activeProfile?.Id == profileId)
        {
            _activeProfile = null;
        }
    }

    public void SaveScore(int profileId, int score, int level)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Scores (ProfileId, Score, Level, Date)
            VALUES ($profileId, $score, $level, $date)
        ";

        command.Parameters.AddWithValue("$profileId", profileId);
        command.Parameters.AddWithValue("$score", score);
        command.Parameters.AddWithValue("$level", level);
        command.Parameters.AddWithValue("$date", DateTime.Now.ToString("o"));

        command.ExecuteNonQuery();

        // Also update LastPlayedAt for the profile
        var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = "UPDATE Profiles SET LastPlayedAt = $lastPlayedAt WHERE Id = $id";
        updateCmd.Parameters.AddWithValue("$lastPlayedAt", DateTime.Now.ToString("o"));
        updateCmd.Parameters.AddWithValue("$id", profileId);
        updateCmd.ExecuteNonQuery();
    }

    public List<ScoreEntry> GetTopScores(int limit = 10)
    {
        var scores = new List<ScoreEntry>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT p.Name, s.Score, s.Level, s.Date
            FROM Scores s
            JOIN Profiles p ON s.ProfileId = p.Id
            ORDER BY s.Score DESC
            LIMIT $limit
        ";
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            scores.Add(new ScoreEntry
            {
                PlayerName = reader.GetString(0),
                Score = reader.GetInt32(1),
                Level = reader.GetInt32(2),
                Date = DateTime.Parse(reader.GetString(3))
            });
        }

        return scores;
    }

    public void SaveSettings(int profileId, Settings settings)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO UserSettings (ProfileId, MenuMusicVolume, GameMusicVolume, SfxVolume, IsMuted)
            VALUES ($profileId, $menuVol, $gameVol, $sfxVol, $isMuted)
            ON CONFLICT(ProfileId) DO UPDATE SET
                MenuMusicVolume = excluded.MenuMusicVolume,
                GameMusicVolume = excluded.GameMusicVolume,
                SfxVolume = excluded.SfxVolume,
                IsMuted = excluded.IsMuted;
        ";

        command.Parameters.AddWithValue("$profileId", profileId);
        command.Parameters.AddWithValue("$menuVol", settings.MenuMusicVolume);
        command.Parameters.AddWithValue("$gameVol", settings.GameMusicVolume);
        command.Parameters.AddWithValue("$sfxVol", settings.SfxVolume);
        command.Parameters.AddWithValue("$isMuted", settings.IsMuted ? 1 : 0);

        command.ExecuteNonQuery();
    }

    public Settings LoadSettings(int profileId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT MenuMusicVolume, GameMusicVolume, SfxVolume, IsMuted FROM UserSettings WHERE ProfileId = $profileId";
        command.Parameters.AddWithValue("$profileId", profileId);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Settings
            {
                ProfileId = profileId,
                MenuMusicVolume = reader.GetDouble(0),
                GameMusicVolume = reader.GetDouble(1),
                SfxVolume = reader.GetDouble(2),
                IsMuted = reader.GetInt32(3) == 1
            };
        }

        // Return defaults if no settings found, but also SAVE them so next time they exist
        var defaultSettings = new Settings { ProfileId = profileId };

        // We need to close the reader before we can execute another command on the same connection
        reader.Close();

        // Save the defaults to the DB so we don't return 0s next time if something fails
        SaveSettings(profileId, defaultSettings);

        return defaultSettings;
    }
}
