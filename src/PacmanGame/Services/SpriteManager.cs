using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PacmanGame.Helpers;
using PacmanGame.Services.Interfaces;
using PacmanGame.Services.Models;

namespace PacmanGame.Services;

/// <summary>
/// Service for managing sprite sheets and providing sprite access.
/// Loads PNG sprite sheets and JSON mapping files from Assets/Sprites/ and flattens nested JSON structure.
/// </summary>
public class SpriteManager : ISpriteManager
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, Bitmap> _spriteSheets = new();
    private readonly Dictionary<string, (string SheetName, SpriteInfo Info)> _flattenedSprites = new();
    private bool _isInitialized;

    public SpriteManager(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize and load all sprite sheets and their mappings
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        try
        {
            LoadSpriteSheet("pacman", Constants.PacmanSpriteSheet, Constants.PacmanSpriteMap);
            LoadSpriteSheet("ghosts", Constants.GhostsSpriteSheet, Constants.GhostsSpriteMap);
            LoadSpriteSheet("items", Constants.ItemsSpriteSheet, Constants.ItemsSpriteMap);
            LoadSpriteSheet("tiles", Constants.TilesSpriteSheet, Constants.TilesSpriteMap);

            _isInitialized = true;
            _logger.Info("SpriteManager initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Error initializing SpriteManager", ex);
            throw;
        }
    }

    private void LoadSpriteSheet(string name, string imageFileName, string mapFileName)
    {
        try
        {
            var uri = new Uri($"avares://PacmanGame/{Constants.SpritesPath}/{imageFileName}");
            var asset = AssetLoader.Open(uri);
            _spriteSheets[name] = new Bitmap(asset);

            var mapUri = new Uri($"avares://PacmanGame/{Constants.SpritesPath}/{mapFileName}");
            var mapAsset = AssetLoader.Open(mapUri);
            using var reader = new StreamReader(mapAsset);
            string json = reader.ReadToEnd();

            var spriteSize = 32;
            var doc = JsonNode.Parse(json);
            if (doc?["sprite_size"] is not null)
            {
                spriteSize = doc["sprite_size"]!.GetValue<int>();
            }

            if (doc?["sprites"] is JsonObject spritesObj)
            {
                FlattenSpriteObject(name, spritesObj, spriteSize, "");
            }

            _logger.Info($"Loaded sprite sheet '{name}' with {_flattenedSprites.Count} sprites");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading sprite sheet {name}", ex);
            throw;
        }
    }

    private void FlattenSpriteObject(string sheetName, JsonObject obj, int spriteSize, string prefix)
    {
        foreach (var kvp in obj)
        {
            string currentKey = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}_{kvp.Key}";

            if (kvp.Value is JsonArray frameArray)
            {
                for (int i = 0; i < frameArray.Count; i++)
                {
                    if (frameArray[i] is JsonObject frameObj)
                    {
                        var sprite = ExtractSpriteInfo(frameObj, spriteSize);
                        string flatKey = $"{sheetName}_{currentKey}_{i}";
                        _flattenedSprites[flatKey] = (sheetName, sprite);
                    }
                }
            }
            else if (kvp.Value is JsonObject nestedObj)
            {
                if (nestedObj.ContainsKey("x") && nestedObj.ContainsKey("y"))
                {
                    var sprite = ExtractSpriteInfo(nestedObj, spriteSize);
                    string flatKey = $"{sheetName}_{currentKey}";
                    _flattenedSprites[flatKey] = (sheetName, sprite);
                }
                else if (nestedObj.ContainsKey("frames") && nestedObj["frames"] is JsonArray framesArray)
                {
                    for (int i = 0; i < framesArray.Count; i++)
                    {
                        if (framesArray[i] is JsonObject frameObj)
                        {
                            var sprite = ExtractSpriteInfo(frameObj, spriteSize);
                            string flatKey = $"{sheetName}_{currentKey}_{i}";
                            _flattenedSprites[flatKey] = (sheetName, sprite);
                        }
                    }
                }
                else
                {
                    FlattenSpriteObject(sheetName, nestedObj, spriteSize, currentKey);
                }
            }
        }
    }

    private SpriteInfo ExtractSpriteInfo(JsonObject obj, int defaultSize)
    {
        var x = obj["x"]?.GetValue<int>() ?? 0;
        var y = obj["y"]?.GetValue<int>() ?? 0;
        var width = obj["width"]?.GetValue<int>() ?? defaultSize;
        var height = obj["height"]?.GetValue<int>() ?? defaultSize;

        return new SpriteInfo { X = x, Y = y, Width = width, Height = height, Name = "" };
    }

    private CroppedBitmap? GetSprite(string flatKey)
    {
        if (!_isInitialized) return null;

        if (!_flattenedSprites.TryGetValue(flatKey, out var entry))
        {
            _logger.Warning($"Sprite not found: {flatKey}");
            return null;
        }

        var (sheetName, info) = entry;

        if (!_spriteSheets.TryGetValue(sheetName, out var sheet))
        {
            _logger.Warning($"Sheet not loaded: {sheetName} for key={flatKey}");
            return null;
        }

        try
        {
            var sourceRect = new PixelRect(info.X, info.Y, info.Width, info.Height);
            return new CroppedBitmap(sheet, sourceRect);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error cropping sprite key={flatKey}", ex);
            return null;
        }
    }

    public CroppedBitmap? GetPacmanSprite(string direction, int frame)
    {
        string flatKey = $"pacman_pacman_{direction.ToLower()}_{frame}";
        return GetSprite(flatKey);
    }

    public CroppedBitmap? GetGhostSprite(string ghostType, string direction, int frame)
    {
        string flatKey = $"ghosts_{ghostType.ToLower()}_{direction.ToLower()}_{frame}";
        return GetSprite(flatKey);
    }

    public CroppedBitmap? GetVulnerableGhostSprite(int frame)
    {
        string flatKey = $"ghosts_vulnerable_normal_{frame}";
        return GetSprite(flatKey);
    }

    public CroppedBitmap? GetWarningGhostSprite(int frame)
    {
        string flatKey = $"ghosts_vulnerable_warning_{frame}";
        return GetSprite(flatKey);
    }

    public CroppedBitmap? GetGhostEyesSprite(string direction)
    {
        string flatKey = $"ghosts_eyes_only_{direction.ToLower()}";
        return GetSprite(flatKey);
    }

    public CroppedBitmap? GetItemSprite(string itemType, int frame = 0)
    {
        string baseKey;
        if (itemType == "dot" || itemType == "power_pellet")
        {
            baseKey = $"items_{itemType.ToLower()}";
        }
        else // Assume it's a fruit, which are nested under "fruits" in the JSON
        {
            baseKey = $"items_fruits_{itemType.ToLower()}";
        }

        // For animated items (like power_pellet)
        if (itemType == "power_pellet")
        {
            string flatKeyWithFrame = $"{baseKey}_{frame}";
            var sprite = GetSprite(flatKeyWithFrame);
            if (sprite != null) return sprite;
        }

        // For static items (dot, fruits) or if animated frame not found
        return GetSprite(baseKey);
    }

    public CroppedBitmap? GetTileSprite(string tileType)
    {
        string flatKey = $"tiles_{tileType.ToLower()}";
        return GetSprite(flatKey);
    }

    public CroppedBitmap? GetDeathSprite(int frame)
    {
        string flatKey = $"pacman_pacman_death_{frame}";
        return GetSprite(flatKey);
    }
}
