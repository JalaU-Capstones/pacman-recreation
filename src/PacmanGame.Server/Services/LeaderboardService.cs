using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PacmanGame.Shared;
using Dapper;

namespace PacmanGame.Server.Services;

public class LeaderboardSubmitResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? NewRank { get; set; }
    public LeaderboardEntry? ReplacedEntry { get; set; }
}

public class LeaderboardService
{
    private readonly string _dbPath = "/var/lib/pacman-server/global_leaderboard.db";
    private readonly SemaphoreSlim _dbLock = new(1, 1);
    private readonly ILogger<LeaderboardService> _logger;

    public LeaderboardService(ILogger<LeaderboardService> logger)
    {
        _logger = logger;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            // Ensure directory exists
            var dir = System.IO.Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS GlobalLeaderboard (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProfileId TEXT NOT NULL UNIQUE,
                    ProfileName TEXT NOT NULL UNIQUE,
                    HighScore INTEGER NOT NULL,
                    LastUpdated INTEGER NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_highscore ON GlobalLeaderboard(HighScore DESC);
            ");

            _logger.LogInformation("Leaderboard database initialized at {Path}", _dbPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize leaderboard database");
        }
    }

    public async Task<List<LeaderboardEntry>> GetTop10Async()
    {
        await _dbLock.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var entries = await connection.QueryAsync<LeaderboardEntry>(@"
                SELECT ProfileId, ProfileName, HighScore, LastUpdated
                FROM GlobalLeaderboard
                ORDER BY HighScore DESC
                LIMIT 10");

            return entries.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top 10");
            return new List<LeaderboardEntry>();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private async Task<int?> GetRankAsync(SqliteConnection connection, string profileId)
    {
        var rank = await connection.ExecuteScalarAsync<int?>(@"
            SELECT COUNT(*) + 1
            FROM GlobalLeaderboard
            WHERE HighScore > (SELECT HighScore FROM GlobalLeaderboard WHERE ProfileId = @ProfileId)",
            new { ProfileId = profileId });

        return rank;
    }

    public async Task<LeaderboardSubmitResult> SubmitScoreAsync(
        string profileId,
        string profileName,
        int highScore,
        long clientTimestamp)
    {
        await _dbLock.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            // Check if profile already exists
            var existing = await connection.QueryFirstOrDefaultAsync<LeaderboardEntry>(
                "SELECT * FROM GlobalLeaderboard WHERE ProfileId = @ProfileId",
                new { ProfileId = profileId });

            if (existing != null)
            {
                // Update only if new score is higher
                if (highScore > existing.HighScore)
                {
                    await connection.ExecuteAsync(@"
                        UPDATE GlobalLeaderboard
                        SET HighScore = @HighScore, LastUpdated = @LastUpdated, ProfileName = @ProfileName
                        WHERE ProfileId = @ProfileId",
                        new { ProfileId = profileId, ProfileName = profileName, HighScore = highScore, LastUpdated = clientTimestamp });

                    transaction.Commit();
                    return new LeaderboardSubmitResult
                    {
                        Success = true,
                        Message = "Score updated",
                        NewRank = await GetRankAsync(connection, profileId)
                    };
                }
                else
                {
                    transaction.Rollback();
                    return new LeaderboardSubmitResult
                    {
                        Success = false,
                        Message = "Score not higher than existing"
                    };
                }
            }

            // Check if leaderboard is full (10 entries)
            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM GlobalLeaderboard");

            if (count >= 10)
            {
                // Get lowest score entry
                var lowest = await connection.QueryFirstOrDefaultAsync<LeaderboardEntry>(@"
                    SELECT * FROM GlobalLeaderboard
                    ORDER BY HighScore ASC
                    LIMIT 1");

                if (lowest != null && highScore > lowest.HighScore)
                {
                    // Replace lowest entry
                    await connection.ExecuteAsync(
                        "DELETE FROM GlobalLeaderboard WHERE ProfileId = @ProfileId",
                        new { ProfileId = lowest.ProfileId });

                    await connection.ExecuteAsync(@"
                        INSERT INTO GlobalLeaderboard (ProfileId, ProfileName, HighScore, LastUpdated)
                        VALUES (@ProfileId, @ProfileName, @HighScore, @LastUpdated)",
                        new { ProfileId = profileId, ProfileName = profileName, HighScore = highScore, LastUpdated = clientTimestamp });

                    transaction.Commit();
                    return new LeaderboardSubmitResult
                    {
                        Success = true,
                        Message = "Entered top 10",
                        NewRank = await GetRankAsync(connection, profileId),
                        ReplacedEntry = lowest
                    };
                }
                else
                {
                    transaction.Rollback();
                    return new LeaderboardSubmitResult
                    {
                        Success = false,
                        Message = "Score too low for top 10"
                    };
                }
            }
            else
            {
                // Leaderboard not full, just insert
                await connection.ExecuteAsync(@"
                    INSERT INTO GlobalLeaderboard (ProfileId, ProfileName, HighScore, LastUpdated)
                    VALUES (@ProfileId, @ProfileName, @HighScore, @LastUpdated)",
                    new { ProfileId = profileId, ProfileName = profileName, HighScore = highScore, LastUpdated = clientTimestamp });

                transaction.Commit();
                return new LeaderboardSubmitResult
                {
                    Success = true,
                    Message = "Added to leaderboard",
                    NewRank = await GetRankAsync(connection, profileId)
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting score");
            return new LeaderboardSubmitResult { Success = false, Message = "Server error" };
        }
        finally
        {
            _dbLock.Release();
        }
    }
}
