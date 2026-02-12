using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.Services;

public class ProfileManager : IProfileManager
{
    private readonly ILogger<ProfileManager> _logger;
    private readonly string _connectionString;
    private Profile? _activeProfile;
    private bool _isInitialized;

    public ProfileManager(ILogger<ProfileManager> logger, string? dbPath = null)
    {
        _logger = logger;
        if (string.IsNullOrEmpty(dbPath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "PacmanGame");
            Directory.CreateDirectory(folder);
            dbPath = Path.Combine(folder, "profiles.db");
        }

        _connectionString = $"Data Source={dbPath}";
        _logger.LogInformation("ProfileManager created with database at {DbPath}", dbPath);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            _logger.LogDebug("ProfileManager.InitializeAsync started");
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            _logger.LogDebug("Database connection opened");

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
            await command.ExecuteNonQueryAsync();
            _isInitialized = true;
            _logger.LogInformation("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed");
            throw;
        }
    }

    public List<Profile> GetAllProfiles()
    {
        var profiles = new List<Profile>();
        if (!_isInitialized) return profiles;

        try
        {
            _logger.LogDebug("GetAllProfiles started");
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
            _logger.LogDebug("GetAllProfiles completed, found {Count} profiles", profiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all profiles.");
        }
        return profiles;
    }

    public Profile CreateProfile(string name, string avatarColor)
    {
        if (!_isInitialized) throw new InvalidOperationException("ProfileManager not initialized.");

        try
        {
            _logger.LogDebug("CreateProfile called with name={Name}", name);
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

            SaveSettings((int)id, new Settings { ProfileId = (int)id });

            _activeProfile = profile;
            _logger.LogInformation("New profile created: {Name}", name);
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create profile '{Name}'.", name);
            throw;
        }
    }

    public Profile? GetProfileById(int id)
    {
        if (!_isInitialized) return null;
        // ... implementation ...
        return null;
    }

    public void SetActiveProfile(int profileId)
    {
        if (!_isInitialized) return;
        _logger.LogDebug("SetActiveProfile called with profileId={ProfileId}", profileId);
        _activeProfile = GetProfileById(profileId);
        // ... implementation ...
    }

    public Profile? GetActiveProfile() => _activeProfile;

    public void DeleteProfile(int profileId)
    {
        if (!_isInitialized) return;
        // ... implementation ...
    }

    public void SaveScore(int profileId, int score, int level)
    {
        if (!_isInitialized) return;
        // ... implementation ...
    }

    public List<ScoreEntry> GetTopScores(int limit = 10)
    {
        if (!_isInitialized) return new List<ScoreEntry>();
        // ... implementation ...
        return new List<ScoreEntry>();
    }

    public void SaveSettings(int profileId, Settings settings)
    {
        if (!_isInitialized) return;
        // ... implementation ...
    }

    public Settings LoadSettings(int profileId)
    {
        if (!_isInitialized) return new Settings { ProfileId = profileId };
        // ... implementation ...
        return new Settings { ProfileId = profileId };
    }
}
