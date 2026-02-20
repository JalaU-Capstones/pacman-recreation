using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PacmanGame.Shared;
using Microsoft.Extensions.Logging;

namespace PacmanGame.Services;

public class LeaderboardCacheData
{
    public List<LeaderboardEntry> Leaderboard { get; set; } = new();
    public long CacheTimestamp { get; set; }
    public PendingUpdate? PendingUpdate { get; set; }
}

public class PendingUpdate
{
    public string ProfileId { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public int HighScore { get; set; }
}

public class GlobalLeaderboardCache
{
    private readonly string _cacheFilePath;
    private LeaderboardCacheData _cache;
    private readonly NetworkService _networkService;
    private readonly ILogger<GlobalLeaderboardCache> _logger;
    private TaskCompletionSource<List<LeaderboardEntry>>? _fetchTcs;
    private TaskCompletionSource<LeaderboardSubmitScoreResponse>? _submitTcs;
    private static readonly JsonSerializerOptions JsonWriteOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    private static readonly JsonSerializerOptions JsonReadOptions = new() { PropertyNameCaseInsensitive = true };

    public GlobalLeaderboardCache(NetworkService networkService, ILogger<GlobalLeaderboardCache> logger)
    {
        _networkService = networkService;
        _logger = logger;

        var cacheDir = GetCacheDirectory();
        Directory.CreateDirectory(cacheDir);
        _cacheFilePath = Path.Combine(cacheDir, "global_leaderboard.cache");

        _cache = new LeaderboardCacheData();
        LoadCache();

        _networkService.OnLeaderboardGetTop10Response += HandleTop10Response;
        _networkService.OnLeaderboardSubmitScoreResponse += HandleSubmitResponse;
    }

    private static string GetCacheDirectory()
    {
        // Requirement: Linux uses ~/.cache/pacman-recreation; Windows uses %LOCALAPPDATA%\\pacman-recreation.
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pacman-recreation");
        }

        var xdgCacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (!string.IsNullOrWhiteSpace(xdgCacheHome))
        {
            return Path.Combine(xdgCacheHome, "pacman-recreation");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".cache", "pacman-recreation");
    }

    private void LoadCache()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                _cache = JsonSerializer.Deserialize<LeaderboardCacheData>(json, JsonReadOptions) ?? new LeaderboardCacheData();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load leaderboard cache");
            _cache = new LeaderboardCacheData();
        }
    }

    private void SaveCache()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cache, JsonWriteOptions);
            File.WriteAllText(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save leaderboard cache");
        }
    }

    private bool IsCacheExpired()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return (now - _cache.CacheTimestamp) > 300; // 5 minutes
    }

    public async Task<List<LeaderboardEntry>> GetTop10Async()
    {
        if (IsCacheExpired())
        {
            await RefreshFromServerAsync();
        }
        return _cache.Leaderboard;
    }

    private async Task RefreshFromServerAsync()
    {
        if (_fetchTcs != null)
        {
            await _fetchTcs.Task;
            return;
        }

        _fetchTcs = new TaskCompletionSource<List<LeaderboardEntry>>();

        try
        {
            if (!_networkService.IsConnected)
            {
                _networkService.Start();
                // Wait a bit for connection
                await Task.Delay(1000);
            }

            if (_networkService.IsConnected)
            {
                _networkService.SendLeaderboardGetTop10Request(_cache.CacheTimestamp);

                // Wait for response or timeout
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(_fetchTcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning("Timeout waiting for leaderboard response");
                    _fetchTcs.TrySetResult(_cache.Leaderboard); // Fallback to cache
                }
            }
            else
            {
                _fetchTcs.TrySetResult(_cache.Leaderboard);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing leaderboard");
            _fetchTcs?.TrySetResult(_cache.Leaderboard);
        }
        finally
        {
            _fetchTcs = null;
        }
    }

    private void HandleTop10Response(LeaderboardGetTop10Response response)
    {
        _cache.Leaderboard = response.Top10;
        _cache.CacheTimestamp = response.ServerTimestamp;
        SaveCache();
        _fetchTcs?.TrySetResult(response.Top10);
    }

    public async Task SubmitScoreAsync(string profileId, string profileName, int highScore)
    {
        _cache.PendingUpdate = new PendingUpdate
        {
            ProfileId = profileId,
            ProfileName = profileName,
            HighScore = highScore
        };
        SaveCache();
        await FlushPendingUpdatesAsync();
    }

    public async Task FlushPendingUpdatesAsync()
    {
        if (_cache.PendingUpdate != null)
        {
            if (_submitTcs != null)
            {
                // A submit is already in-flight.
                await _submitTcs.Task;
                return;
            }

            if (!_networkService.IsConnected)
            {
                _networkService.Start();
                await Task.Delay(1000);
            }

            if (_networkService.IsConnected)
            {
                try
                {
                    _submitTcs = new TaskCompletionSource<LeaderboardSubmitScoreResponse>();
                    _networkService.SendLeaderboardSubmitScoreRequest(
                        _cache.PendingUpdate.ProfileId,
                        _cache.PendingUpdate.ProfileName,
                        _cache.PendingUpdate.HighScore,
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    var timeoutTask = Task.Delay(5000);
                    var completed = await Task.WhenAny(_submitTcs.Task, timeoutTask);
                    if (completed == timeoutTask)
                    {
                        _logger.LogWarning("Timeout waiting for leaderboard submit response");
                        _submitTcs.TrySetResult(new LeaderboardSubmitScoreResponse { Success = false, Message = "Timeout" });
                    }

                    await _submitTcs.Task;
                }
                finally
                {
                    // If a late response arrives, HandleSubmitResponse will safely no-op the cleared TCS.
                    _submitTcs = null;
                }
            }
        }
    }

    private void HandleSubmitResponse(LeaderboardSubmitScoreResponse response)
    {
        if (response.Success)
        {
            _cache.PendingUpdate = null;
            SaveCache();
            // Trigger refresh to see updated leaderboard
            _ = RefreshFromServerAsync();
        }
        else
        {
            _logger.LogWarning("Score submission failed: {Message}", response.Message);
        }

        _submitTcs?.TrySetResult(response);
        _submitTcs = null;
    }
}
