using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;
using Dapper;

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
        string path = dbPath ?? GetDatabasePath();
        _connectionString = $"Data Source={path}";
        _logger.LogInformation("ProfileManager initialized with database at {DbPath}", path);
    }

    private string GetDatabasePath()
    {
        string dataDirectory;

        if (OperatingSystem.IsWindows())
        {
            // Windows: %APPDATA%\PacmanRecreation
            dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PacmanRecreation"
            );
        }
        else if (OperatingSystem.IsLinux())
        {
            // Check if running in Flatpak
            var flatpakId = Environment.GetEnvironmentVariable("FLATPAK_ID");

            if (!string.IsNullOrEmpty(flatpakId))
            {
                // Flatpak: XDG_DATA_HOME is already set to the sandboxed location
                dataDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "pacman-recreation"
                );
            }
            else
            {
                // Normal Linux: ~/.local/share/pacman-recreation
                var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (string.IsNullOrEmpty(xdgDataHome))
                {
                    xdgDataHome = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".local", "share"
                    );
                }
                dataDirectory = Path.Combine(xdgDataHome, "pacman-recreation");
            }
        }
        else
        {
            // macOS or other: fallback to current directory
            dataDirectory = AppContext.BaseDirectory;
        }

        Directory.CreateDirectory(dataDirectory);

        return Path.Combine(dataDirectory, "profiles.db");
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
                    LastPlayedAt TEXT,
                    HasCompletedAllLevels INTEGER DEFAULT 0,
                    GlobalProfileId TEXT,
                    LastGlobalScoreSubmission INTEGER DEFAULT 0
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

            // Migration: Add new columns if they don't exist
            try
            {
                await connection.ExecuteAsync("ALTER TABLE Profiles ADD COLUMN HasCompletedAllLevels INTEGER DEFAULT 0;");
            }
            catch { /* Column likely exists */ }

            try
            {
                await connection.ExecuteAsync("ALTER TABLE Profiles ADD COLUMN GlobalProfileId TEXT;");
            }
            catch { /* Column likely exists */ }

            try
            {
                await connection.ExecuteAsync("ALTER TABLE Profiles ADD COLUMN LastGlobalScoreSubmission INTEGER DEFAULT 0;");
            }
            catch { /* Column likely exists */ }

            // Migration: ensure only one score row per profile (high score) and enforce via unique index.
            try
            {
                await connection.ExecuteAsync(@"
                    DELETE FROM Scores
                    WHERE Id NOT IN (
                        SELECT s.Id
                        FROM Scores s
                        WHERE s.Score = (
                            SELECT MAX(s2.Score) FROM Scores s2 WHERE s2.ProfileId = s.ProfileId
                        )
                        AND s.Id = (
                            SELECT MAX(s3.Id) FROM Scores s3 WHERE s3.ProfileId = s.ProfileId AND s3.Score = s.Score
                        )
                    );
                ");
            }
            catch { /* Best-effort cleanup */ }

            try
            {
                await connection.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS idx_scores_profile_unique ON Scores(ProfileId);");
            }
            catch { /* Index might already exist */ }

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
                SELECT p.Id, p.Name, p.AvatarColor, p.CreatedAt, p.LastPlayedAt, MAX(s.Score) as HighScore,
                       p.HasCompletedAllLevels, p.GlobalProfileId, p.LastGlobalScoreSubmission
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
                    HighScore = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    HasCompletedAllLevels = !reader.IsDBNull(6) && reader.GetInt32(6) != 0,
                    GlobalProfileId = reader.IsDBNull(7) ? null : reader.GetString(7),
                    LastGlobalScoreSubmission = reader.IsDBNull(8) ? 0 : reader.GetInt64(8)
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
                INSERT INTO Profiles (Name, AvatarColor, CreatedAt, LastPlayedAt, HasCompletedAllLevels, LastGlobalScoreSubmission)
                VALUES ($name, $avatarColor, $createdAt, $lastPlayedAt, 0, 0);
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
                LastPlayedAt = now,
                HasCompletedAllLevels = false,
                LastGlobalScoreSubmission = 0
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

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT p.Id,
                       p.Name,
                       p.AvatarColor,
                       p.CreatedAt,
                       p.LastPlayedAt,
                       p.HasCompletedAllLevels,
                       p.GlobalProfileId,
                       p.LastGlobalScoreSubmission,
                       (SELECT MAX(s.Score) FROM Scores s WHERE s.ProfileId = p.Id) AS HighScore
                FROM Profiles p
                WHERE p.Id = $id
            ";
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
                    LastPlayedAt = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                    HasCompletedAllLevels = !reader.IsDBNull(5) && reader.GetInt32(5) != 0,
                    GlobalProfileId = reader.IsDBNull(6) ? null : reader.GetString(6),
                    LastGlobalScoreSubmission = reader.IsDBNull(7) ? 0 : reader.GetInt64(7),
                    HighScore = reader.IsDBNull(8) ? 0 : reader.GetInt32(8)
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profile by ID {ProfileId}.", id);
        }

        return null;
    }

    public void SetActiveProfile(int profileId)
    {
        if (!_isInitialized) return;
        _logger.LogDebug("SetActiveProfile called with profileId={ProfileId}", profileId);
        _activeProfile = GetProfileById(profileId);
    }

    public Profile? GetActiveProfile() => _activeProfile;

    public Task<Profile?> GetCurrentProfileAsync()
    {
        if (_activeProfile == null) return Task.FromResult<Profile?>(null);
        return Task.FromResult(GetProfileById(_activeProfile.Id));
    }

    public async Task UpdateProfileAsync(Profile profile)
    {
        if (!_isInitialized) return;

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await connection.ExecuteAsync(@"
                UPDATE Profiles
                SET Name = @Name,
                    AvatarColor = @AvatarColor,
                    LastPlayedAt = @LastPlayedAt,
                    HasCompletedAllLevels = @HasCompletedAllLevels,
                    GlobalProfileId = @GlobalProfileId,
                    LastGlobalScoreSubmission = @LastGlobalScoreSubmission
                WHERE Id = @Id",
                new {
                    profile.Name,
                    profile.AvatarColor,
                    LastPlayedAt = profile.LastPlayedAt?.ToString("o"),
                    HasCompletedAllLevels = profile.HasCompletedAllLevels ? 1 : 0,
                    profile.GlobalProfileId,
                    profile.LastGlobalScoreSubmission,
                    profile.Id
                });

            if (_activeProfile?.Id == profile.Id)
            {
                _activeProfile = profile;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update profile {ProfileId}.", profile.Id);
        }
    }

    public void DeleteProfile(int profileId)
    {
        if (!_isInitialized) return;

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Profiles WHERE Id = $id";
            command.Parameters.AddWithValue("$id", profileId);

            command.ExecuteNonQuery();
            _logger.LogInformation("Profile {ProfileId} deleted.", profileId);

            if (_activeProfile?.Id == profileId)
            {
                _activeProfile = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile {ProfileId}.", profileId);
        }
    }

    public void SaveScore(int profileId, int score, int level)
    {
        if (!_isInitialized) return;

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Check if this is a new high score for this profile
            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT MAX(Score) FROM Scores WHERE ProfileId = $profileId";
            checkCmd.Parameters.AddWithValue("$profileId", profileId);
            var result = checkCmd.ExecuteScalar();
            int currentHighScore = result != DBNull.Value ? Convert.ToInt32(result) : 0;

            // Only save if it's a new high score or if no score exists
            if (score > currentHighScore)
            {
                // Remove old scores for this profile to keep only the highest
                var deleteCmd = connection.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM Scores WHERE ProfileId = $profileId";
                deleteCmd.Parameters.AddWithValue("$profileId", profileId);
                deleteCmd.ExecuteNonQuery();

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
                _logger.LogInformation("New High Score saved for profile {ProfileId}: {Score} points at level {Level}", profileId, score, level);
            }
            else
            {
                _logger.LogInformation("Score {Score} is not higher than current high score {HighScore} for profile {ProfileId}. Not saving.", score, currentHighScore, profileId);
            }

            // Always update LastPlayedAt for the profile
            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = "UPDATE Profiles SET LastPlayedAt = $lastPlayedAt WHERE Id = $id";
            updateCmd.Parameters.AddWithValue("$lastPlayedAt", DateTime.Now.ToString("o"));
            updateCmd.Parameters.AddWithValue("$id", profileId);
            updateCmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save score for profile ID {ProfileId}.", profileId);
        }
    }

    public async Task SaveOrUpdateHighScoreAsync(string profileName, int score)
    {
        // Get existing high score
        var existingScore = await GetHighScoreAsync(profileName);

        if (existingScore == null || score > existingScore.Score)
        {
            // Update or insert
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // We need to find the ProfileId first
            var profileId = await connection.ExecuteScalarAsync<int?>("SELECT Id FROM Profiles WHERE Name = @Name", new { Name = profileName });

            if (profileId.HasValue)
            {
                // Remove old scores for this profile to keep only the highest
                await connection.ExecuteAsync("DELETE FROM Scores WHERE ProfileId = @ProfileId", new { ProfileId = profileId.Value });

                await connection.ExecuteAsync(@"
                    INSERT INTO Scores (ProfileId, Score, Level, Date)
                    VALUES (@ProfileId, @Score, 1, @DateAchieved)",
                    new { ProfileId = profileId.Value, Score = score, DateAchieved = DateTime.UtcNow.ToString("o") });
            }
        }
    }

    private async Task<ScoreEntry?> GetHighScoreAsync(string profileName)
    {
        if (!_isInitialized) return null;

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT p.Name, MAX(s.Score) as Score, s.Level, s.Date
                FROM Scores s
                JOIN Profiles p ON s.ProfileId = p.Id
                WHERE p.Name = $profileName
                GROUP BY s.ProfileId
            ";
            command.Parameters.AddWithValue("$profileName", profileName);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ScoreEntry
                {
                    PlayerName = reader.GetString(0),
                    Score = reader.GetInt32(1),
                    Level = reader.GetInt32(2),
                    Date = DateTime.Parse(reader.GetString(3))
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get high score for profile {ProfileName}.", profileName);
        }

        return null;
    }

    public List<ScoreEntry> GetTopScores(int limit = 10)
    {
        var scores = new List<ScoreEntry>();
        if (!_isInitialized) return scores;

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT p.Name, MAX(s.Score) as Score, s.Level, s.Date
                FROM Scores s
                JOIN Profiles p ON s.ProfileId = p.Id
                GROUP BY s.ProfileId
                ORDER BY Score DESC
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

            _logger.LogDebug("GetTopScores completed, found {Count} scores", scores.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top scores.");
        }

        return scores;
    }

    public void SaveSettings(int profileId, Settings settings)
    {
        if (!_isInitialized) return;

        try
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
            _logger.LogInformation("Settings saved for profile ID {ProfileId}.", profileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings for profile ID {ProfileId}.", profileId);
        }
    }

    public Settings LoadSettings(int profileId)
    {
        if (!_isInitialized) return new Settings { ProfileId = profileId };

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT MenuMusicVolume, GameMusicVolume, SfxVolume, IsMuted
                FROM UserSettings
                WHERE ProfileId = $profileId
            ";
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
                    IsMuted = reader.GetInt32(3) != 0
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings for profile ID {ProfileId}.", profileId);
        }

        return new Settings { ProfileId = profileId };
    }

    public void CleanupDuplicateScores()
    {
        // Placeholder for cleanup logic if needed
    }
}
