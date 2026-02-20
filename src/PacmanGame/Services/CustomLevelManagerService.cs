using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PacmanGame.Models.CustomLevel;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.Services;

public class CustomLevelManagerService : ICustomLevelManagerService
{
    private readonly ILogger<CustomLevelManagerService> _logger;
    private readonly string _libraryPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CustomLevelManagerService(ILogger<CustomLevelManagerService> logger)
    {
        _logger = logger;
        _libraryPath = Path.Combine(GetDataRoot(), "custom-levels");
        Directory.CreateDirectory(_libraryPath);
    }

    public async Task<IReadOnlyList<CustomLevelSummary>> GetCustomLevelsAsync()
    {
        var list = new List<CustomLevelSummary>();
        foreach (var directory in Directory.EnumerateDirectories(_libraryPath))
        {
            var summary = await ReadSummaryFromDirectoryAsync(directory);
            if (summary != null)
            {
                list.Add(summary);
            }
        }
        return list;
    }

    public async Task<CustomLevelSummary> ImportProjectAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Custom level project not found.", filePath);
        }

        using var archive = ZipFile.OpenRead(filePath);
        var configEntry = archive.Entries.FirstOrDefault(e => string.Equals(e.Name, "project.json", StringComparison.OrdinalIgnoreCase));
        if (configEntry == null)
        {
            throw new InvalidDataException("project.json is missing from the project archive.");
        }

        using var configStream = configEntry.Open();
        using var mem = new MemoryStream();
        await configStream.CopyToAsync(mem);
        mem.Position = 0;

        var config = await JsonSerializer.DeserializeAsync<ProjectConfig>(mem, _jsonOptions)
                     ?? throw new InvalidDataException("Invalid project.json");

        mem.Position = 0;
        var hash = ComputeSha256(mem.ToArray());
        var sanitized = SanitizeDirectoryName(config.ProjectName);
        var folderName = $"{sanitized}-{hash[..8]}";
        var targetDir = Path.Combine(_libraryPath, folderName);
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, true);
        }
        Directory.CreateDirectory(targetDir);

        ZipFile.ExtractToDirectory(filePath, targetDir);
        _logger.LogInformation("Imported custom level {Name} into {Path}", config.ProjectName, targetDir);

        return await ReadSummaryFromDirectoryAsync(targetDir)
               ?? throw new InvalidOperationException("Failed to load imported project.");
    }

    public Task DeleteCustomLevelAsync(string id)
    {
        var directory = Directory.GetDirectories(_libraryPath)
            .FirstOrDefault(dir => string.Equals(Path.GetFileName(dir), id, StringComparison.Ordinal));
        if (directory != null)
        {
            Directory.Delete(directory, true);
            _logger.LogInformation("Deleted custom level {Id}", id);
        }
        return Task.CompletedTask;
    }

    private async Task<CustomLevelSummary?> ReadSummaryFromDirectoryAsync(string directory)
    {
        var projectPath = Path.Combine(directory, "project.json");
        if (!File.Exists(projectPath)) return null;

        try
        {
            await using var stream = File.OpenRead(projectPath);
            var config = await JsonSerializer.DeserializeAsync<ProjectConfig>(stream, _jsonOptions);
            if (config == null) return null;

            var levelCount = config.LevelConfigs?.Count ?? config.GlobalConfig.LevelCount;
            return new CustomLevelSummary
            {
                Id = Path.GetFileName(directory),
                ProjectName = config.ProjectName,
                Author = config.Author,
                CreatedDate = config.CreatedDate == default ? File.GetCreationTimeUtc(projectPath) : config.CreatedDate,
                LevelCount = levelCount,
                IsEditable = config.IsEditable,
                DirectoryPath = directory
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read custom level summary from {Path}", directory);
            return null;
        }
    }

    private static string ComputeSha256(byte[] data)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static string SanitizeDirectoryName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (invalid.Contains(c)) continue;
            builder.Append(c);
        }
        var value = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(value) ? "custom-level" : value;
    }

    private static string GetDataRoot()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PacmanRecreation");
        }

        if (OperatingSystem.IsLinux())
        {
            var flatpakId = Environment.GetEnvironmentVariable("FLATPAK_ID");
            if (!string.IsNullOrEmpty(flatpakId))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pacman-recreation");
            }

            var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (string.IsNullOrEmpty(xdg))
            {
                xdg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
            }
            return Path.Combine(xdg, "pacman-recreation");
        }

        return AppContext.BaseDirectory;
    }
}
