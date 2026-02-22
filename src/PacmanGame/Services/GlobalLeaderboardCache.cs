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
    private readonly object _sync = new();
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
        lock (_sync)
        {
            return _cache.Leaderboard;
        }
    }

    private async Task RefreshFromServerAsync()
    {
        TaskCompletionSource<List<LeaderboardEntry>> tcs;
        lock (_sync)
        {
            if (_fetchTcs != null)
            {
                tcs = _fetchTcs;
            }
            else
            {
                _fetchTcs = new TaskCompletionSource<List<LeaderboardEntry>>(TaskCreationOptions.RunContinuationsAsynchronously);
                tcs = _fetchTcs;
            }
        }

        if (tcs != _fetchTcs)
        {
            await tcs.Task;
            return;
        }

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
                long cacheTimestamp;
                lock (_sync)
                {
                    cacheTimestamp = _cache.CacheTimestamp;
                }
                _networkService.SendLeaderboardGetTop10Request(cacheTimestamp);

                // Wait for response or timeout
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning("Timeout waiting for leaderboard response");
                    List<LeaderboardEntry> fallback;
                    lock (_sync)
                    {
                        fallback = _cache.Leaderboard;
                    }
                    tcs.TrySetResult(fallback); // Fallback to cache
                }
            }
            else
            {
                List<LeaderboardEntry> fallback;
                lock (_sync)
                {
                    fallback = _cache.Leaderboard;
                }
                tcs.TrySetResult(fallback);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing leaderboard");
            List<LeaderboardEntry> fallback;
            lock (_sync)
            {
                fallback = _cache.Leaderboard;
            }
            tcs.TrySetResult(fallback);
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_fetchTcs, tcs))
                {
                    _fetchTcs = null;
                }
            }
        }
    }

    private void HandleTop10Response(LeaderboardGetTop10Response response)
    {
        TaskCompletionSource<List<LeaderboardEntry>>? tcs;
        lock (_sync)
        {
            _cache.Leaderboard = response.Top10;
            _cache.CacheTimestamp = response.ServerTimestamp;
            tcs = _fetchTcs;
        }
        SaveCache();
        tcs?.TrySetResult(response.Top10);
    }

    public async Task SubmitScoreAsync(string profileId, string profileName, int highScore)
    {
        lock (_sync)
        {
            _cache.PendingUpdate = new PendingUpdate
            {
                ProfileId = profileId,
                ProfileName = profileName,
                HighScore = highScore
            };
        }
        SaveCache();
        await FlushPendingUpdatesAsync();
    }

    public async Task FlushPendingUpdatesAsync()
    {
        PendingUpdate? pending;
        TaskCompletionSource<LeaderboardSubmitScoreResponse>? existingSubmit;
        lock (_sync)
        {
            pending = _cache.PendingUpdate;
            existingSubmit = _submitTcs;
        }

        if (pending == null)
        {
            return;
        }

        if (existingSubmit != null)
        {
            // A submit is already in-flight.
            await existingSubmit.Task;
            return;
        }

        TaskCompletionSource<LeaderboardSubmitScoreResponse> submitTcs;
        lock (_sync)
        {
            if (_submitTcs != null)
            {
                submitTcs = _submitTcs;
            }
            else
            {
                _submitTcs = new TaskCompletionSource<LeaderboardSubmitScoreResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                submitTcs = _submitTcs;
            }
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
                // Capture pending update snapshot to avoid races with HandleSubmitResponse clearing it.
                var snapshot = pending;

                _networkService.SendLeaderboardSubmitScoreRequest(
                    snapshot.ProfileId,
                    snapshot.ProfileName,
                    snapshot.HighScore,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                var timeoutTask = Task.Delay(5000);
                var completed = await Task.WhenAny(submitTcs.Task, timeoutTask);
                if (completed == timeoutTask)
                {
                    _logger.LogWarning("Timeout waiting for leaderboard submit response");
                    submitTcs.TrySetResult(new LeaderboardSubmitScoreResponse { Success = false, Message = "Timeout" });
                }

                await submitTcs.Task;
            }
            finally
            {
                lock (_sync)
                {
                    if (ReferenceEquals(_submitTcs, submitTcs))
                    {
                        _submitTcs = null;
                    }
                }
            }
        }
    }

    private void HandleSubmitResponse(LeaderboardSubmitScoreResponse response)
    {
        TaskCompletionSource<LeaderboardSubmitScoreResponse>? tcs;
        bool shouldRefresh;
        if (response.Success)
        {
            lock (_sync)
            {
                _cache.PendingUpdate = null;
                tcs = _submitTcs;
                _submitTcs = null;
            }
            shouldRefresh = true;
            SaveCache();
        }
        else
        {
            lock (_sync)
            {
                tcs = _submitTcs;
                _submitTcs = null;
            }
            shouldRefresh = false;
            _logger.LogWarning("Score submission failed: {Message}", response.Message);
        }

        tcs?.TrySetResult(response);
        if (shouldRefresh)
        {
            // Trigger refresh to see updated leaderboard
            _ = RefreshFromServerAsync();
        }
    }
}
