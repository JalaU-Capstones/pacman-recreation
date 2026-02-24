using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;
using Dapper;
using PacmanGame.Services.KeyBindings;

namespace PacmanGame.Services;

public class ProfileManager : IProfileManager
{
    private readonly ILogger<ProfileManager> _logger;
    private readonly string _connectionString;
    private readonly string _dbPath;
    private readonly string _installSecretPath;
    private readonly string _dbKey;
    private bool _useEncryption;
    private Profile? _activeProfile;
    private bool _isInitialized;

    public ProfileManager(ILogger<ProfileManager> logger, string? dbPath = null)
    {
        _logger = logger;
        string path = dbPath ?? GetDatabasePath();
        _dbPath = path;
        _installSecretPath = Path.Combine(Path.GetDirectoryName(_dbPath) ?? AppContext.BaseDirectory,
            Path.GetFileName(_dbPath) + ".secret");
        _dbKey = EnsureInstallSecretAndGetKey();
        _connectionString = $"Data Source={path}";
        _logger.LogInformation("ProfileManager initialized with database at {DbPath}", path);
    }

    private string EnsureInstallSecretAndGetKey()
    {
        try
        {
            var dir = Path.GetDirectoryName(_installSecretPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(_installSecretPath))
            {
                var secret = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                File.WriteAllText(_installSecretPath, secret);

                if (OperatingSystem.IsLinux())
                {
                    try
                    {
                        File.SetUnixFileMode(_installSecretPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                    }
                    catch
                    {
                        // Best-effort hardening; ignore if unsupported.
                    }
                }
            }

            var key = File.ReadAllText(_installSecretPath).Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                key = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                File.WriteAllText(_installSecretPath, key);
            }

            return key;
        }
        catch (Exception ex)
        {
            // If the key file can't be created/read, fall back to a process-only key.
            // This weakens integrity, but keeps the app functional.
            _logger.LogWarning(ex, "Failed to load install secret; using a temporary key for this run only.");
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }
    }

    private static string SqlQuote(string value)
    {
        return "'" + value.Replace("'", "''") + "'";
    }

    private static bool IsNotADatabase(SqliteException ex)
    {
        var msg = ex.Message ?? string.Empty;
        return msg.Contains("not a database", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("file is not a database", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyEncryptionKey(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA key = {SqlQuote(_dbKey)};";
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection(bool applyEncryptionKey)
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        if (applyEncryptionKey)
        {
            ApplyEncryptionKey(connection);
        }

        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }

        return connection;
    }

    private async Task<SqliteConnection> OpenConnectionAsync(bool applyEncryptionKey)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        if (applyEncryptionKey)
        {
            ApplyEncryptionKey(connection);
        }

        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            await pragma.ExecuteNonQueryAsync();
        }

        return connection;
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
            _useEncryption = DetermineAndMigrateEncryptionIfNeeded();

            await using var connection = await OpenConnectionAsync(_useEncryption);
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
                CREATE TABLE IF NOT EXISTS KeyBindings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProfileId INTEGER NOT NULL,
                    Action TEXT NOT NULL,
                    KeyCode TEXT NOT NULL,
                    ModifierKeys TEXT,
                    CreatedAt INTEGER NOT NULL,
                    UpdatedAt INTEGER NOT NULL,
                    FOREIGN KEY (ProfileId) REFERENCES Profiles(Id) ON DELETE CASCADE,
                    UNIQUE(ProfileId, Action)
                );
                CREATE INDEX IF NOT EXISTS idx_keybindings_profile ON KeyBindings(ProfileId);
                CREATE INDEX IF NOT EXISTS idx_keybindings_action ON KeyBindings(Action);
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

            await SeedDefaultKeyBindingsAsync(connection);

            _isInitialized = true;
            _logger.LogInformation("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed");
            throw;
        }
    }

    private bool DetermineAndMigrateEncryptionIfNeeded()
    {
        // New installs: create encrypted DB from the start.
        if (!File.Exists(_dbPath))
        {
            _logger.LogInformation("No existing database found; creating encrypted database.");
            return true;
        }

        // Existing installs: try to open as encrypted; if that fails, attempt migration from plaintext.
        try
        {
            using var testEncrypted = OpenConnection(applyEncryptionKey: true);
            using var cmd = testEncrypted.CreateCommand();
            cmd.CommandText = "SELECT count(*) FROM sqlite_master;";
            _ = cmd.ExecuteScalar();
            _logger.LogInformation("Encrypted database detected.");
            return true;
        }
        catch (SqliteException ex) when (IsNotADatabase(ex))
        {
            try
            {
                // Verify we can open it as plaintext before attempting SQLCipher export.
                using var testPlain = OpenConnection(applyEncryptionKey: false);
                using var plainCmd = testPlain.CreateCommand();
                plainCmd.CommandText = "SELECT count(*) FROM sqlite_master;";
                _ = plainCmd.ExecuteScalar();

                _logger.LogInformation("Plaintext database detected; starting migration to encrypted database.");
                MigratePlaintextToEncrypted();
                _logger.LogInformation("Database migration to encrypted format completed.");
                return true;
            }
            catch (Exception migEx) when (migEx is SqliteException or IOException or UnauthorizedAccessException)
            {
                // If the DB can't be opened as plaintext or migration fails, treat it as corrupted or key-mismatched.
                // Keep the app booting by moving it aside and starting fresh encrypted DB.
                var quarantinePath = _dbPath + ".corrupt." + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                try
                {
                    File.Move(_dbPath, quarantinePath, overwrite: true);
                    _logger.LogError(migEx, "Database could not be migrated; moved to {QuarantinePath}. A new encrypted database will be created.", quarantinePath);
                }
                catch (Exception moveEx)
                {
                    _logger.LogError(moveEx, "Database could not be migrated and could not be moved aside; continuing without encryption.");
                    return false;
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            // Unexpected failure: don't brick the app; fall back to plaintext mode.
            _logger.LogError(ex, "Failed to determine database encryption state; continuing without encryption.");
            return false;
        }
    }

    private void MigratePlaintextToEncrypted()
    {
        var tempPath = _dbPath + ".encrypted.tmp";
        var backupPath = _dbPath + ".bak";

        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        // Open the existing database without a key (plaintext), then export into an attached encrypted DB.
        using (var plaintext = OpenConnection(applyEncryptionKey: false))
        using (var cmd = plaintext.CreateCommand())
        {
            cmd.CommandText = $"ATTACH DATABASE {SqlQuote(tempPath)} AS encrypted KEY {SqlQuote(_dbKey)};";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "SELECT sqlcipher_export('encrypted');";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "DETACH DATABASE encrypted;";
            cmd.ExecuteNonQuery();
        }

        // Swap in the encrypted DB.
        File.Move(_dbPath, backupPath, overwrite: true);
        File.Move(tempPath, _dbPath, overwrite: true);
        try
        {
            File.Delete(backupPath);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private static async Task SeedDefaultKeyBindingsAsync(SqliteConnection connection)
    {
        // Ensure each profile has the full set of default bindings.
        var profileIds = (await connection.QueryAsync<int>("SELECT Id FROM Profiles;")).ToList();
        if (profileIds.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var profileId in profileIds)
        {
            foreach (var kvp in KeyBindingDefaults.Defaults)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO KeyBindings (ProfileId, Action, KeyCode, ModifierKeys, CreatedAt, UpdatedAt)
                    VALUES (@ProfileId, @Action, @KeyCode, @ModifierKeys, @CreatedAt, @UpdatedAt)
                    ON CONFLICT(ProfileId, Action) DO NOTHING;
                ", new
                {
                    ProfileId = profileId,
                    Action = kvp.Key,
                    KeyCode = kvp.Value.Key.ToString(),
                    ModifierKeys = kvp.Value.Modifiers == Avalonia.Input.KeyModifiers.None
                        ? null
                        : SerializeModifiersForDb(kvp.Value.Modifiers),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }
    }

    private static string SerializeModifiersForDb(Avalonia.Input.KeyModifiers modifiers)
    {
        modifiers &= (Avalonia.Input.KeyModifiers.Control | Avalonia.Input.KeyModifiers.Shift | Avalonia.Input.KeyModifiers.Alt);
        if (modifiers == Avalonia.Input.KeyModifiers.None) return string.Empty;
        var parts = new List<string>();
        if (modifiers.HasFlag(Avalonia.Input.KeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt)) parts.Add("Alt");
        return string.Join("+", parts);
    }

    public List<Profile> GetAllProfiles()
    {
        var profiles = new List<Profile>();
        if (!_isInitialized) return profiles;

        try
        {
            _logger.LogDebug("GetAllProfiles started");
            using var connection = OpenConnection(_useEncryption);

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
            using var connection = OpenConnection(_useEncryption);

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
            try
            {
                // Populate default keybindings for this new profile.
                SeedDefaultKeyBindingsForProfile(connection, (int)id);
            }
            catch
            {
                // Best-effort; keybindings are also seeded on app startup migration.
            }

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

    private static void SeedDefaultKeyBindingsForProfile(SqliteConnection connection, int profileId)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var kvp in KeyBindingDefaults.Defaults)
        {
            connection.Execute(@"
                INSERT INTO KeyBindings (ProfileId, Action, KeyCode, ModifierKeys, CreatedAt, UpdatedAt)
                VALUES (@ProfileId, @Action, @KeyCode, @ModifierKeys, @CreatedAt, @UpdatedAt)
                ON CONFLICT(ProfileId, Action) DO NOTHING;
            ", new
            {
                ProfileId = profileId,
                Action = kvp.Key,
                KeyCode = kvp.Value.Key.ToString(),
                ModifierKeys = kvp.Value.Modifiers == Avalonia.Input.KeyModifiers.None
                    ? null
                    : SerializeModifiersForDb(kvp.Value.Modifiers),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
    }

    public Profile? GetProfileById(int id)
    {
        if (!_isInitialized) return null;

        try
        {
            using var connection = OpenConnection(_useEncryption);

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
            await using var connection = await OpenConnectionAsync(_useEncryption);

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
            using var connection = OpenConnection(_useEncryption);

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
            using var connection = OpenConnection(_useEncryption);

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
            await using var connection = await OpenConnectionAsync(_useEncryption);

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
            await using var connection = await OpenConnectionAsync(_useEncryption);

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
            using var connection = OpenConnection(_useEncryption);

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
            using var connection = OpenConnection(_useEncryption);

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
            using var connection = OpenConnection(_useEncryption);

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

    internal SqliteConnection OpenConnectionForServices()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("ProfileManager not initialized.");
        }

        return OpenConnection(_useEncryption);
    }

    internal Task<SqliteConnection> OpenConnectionForServicesAsync()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("ProfileManager not initialized.");
        }

        return OpenConnectionAsync(_useEncryption);
    }
}
