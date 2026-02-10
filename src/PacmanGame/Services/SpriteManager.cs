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
    private readonly Dictionary<string, Bitmap> _spriteSheets = new();
    private readonly Dictionary<string, (string SheetName, SpriteInfo Info)> _flattenedSprites = new();
    private bool _isInitialized;

    /// <summary>
    /// Initialize and load all sprite sheets and their mappings
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        try
        {
            // Load Pac-Man sprites
            LoadSpriteSheet("pacman", Constants.PacmanSpriteSheet, Constants.PacmanSpriteMap);

            // Load Ghost sprites
            LoadSpriteSheet("ghosts", Constants.GhostsSpriteSheet, Constants.GhostsSpriteMap);

            // Load Item sprites
            LoadSpriteSheet("items", Constants.ItemsSpriteSheet, Constants.ItemsSpriteMap);

            // Load Tile sprites
            LoadSpriteSheet("tiles", Constants.TilesSpriteSheet, Constants.TilesSpriteMap);

            _isInitialized = true;
            Console.WriteLine("✅ SpriteManager initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error initializing SpriteManager: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Load a sprite sheet and its JSON mapping, flattening the nested structure
    /// </summary>
    private void LoadSpriteSheet(string name, string imageFileName, string mapFileName)
    {
        try
        {
            // Load the PNG sprite sheet using Avalonia's asset system
            var uri = new Uri($"avares://PacmanGame/{Constants.SpritesPath}/{imageFileName}");
            var asset = AssetLoader.Open(uri);
            _spriteSheets[name] = new Bitmap(asset);

            // Load and parse the JSON mapping
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

            // Flatten the nested sprite structure
            if (doc?["sprites"] is JsonObject spritesObj)
            {
                FlattenSpriteObject(name, spritesObj, spriteSize, "");
            }

            Console.WriteLine($"   Loaded {name}: {_flattenedSprites.Count} sprites");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error loading {name}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Recursively flatten nested sprite JSON into a flat dictionary
    /// </summary>
    private void FlattenSpriteObject(string sheetName, JsonObject obj, int spriteSize, string prefix)
    {
        foreach (var kvp in obj)
        {
            string key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}_{kvp.Key}";

            if (kvp.Value is JsonArray frameArray)
            {
                // This is an array of frames (e.g., "right": [{...}, {...}, {...}])
                for (int i = 0; i < frameArray.Count; i++)
                {
                    if (frameArray[i] is JsonObject frameObj)
                    {
                        var sprite = ExtractSpriteInfo(frameObj, spriteSize);
                        string flatKey = $"{sheetName}_{key}_{i}";
                        _flattenedSprites[flatKey] = (sheetName, sprite);
                    }
                }
            }
            else if (kvp.Value is JsonObject nestedObj)
            {
                // Check if this is a sprite definition (has x, y) or a nested category
                if (nestedObj.ContainsKey("x") && nestedObj.ContainsKey("y"))
                {
                    // This is a direct sprite definition (no frames)
                    var sprite = ExtractSpriteInfo(nestedObj, spriteSize);
                    string flatKey = $"{sheetName}_{key}";
                    _flattenedSprites[flatKey] = (sheetName, sprite);
                }
                else if (nestedObj.ContainsKey("frames") && nestedObj["frames"] is JsonArray framesArray)
                {
                    // This is a sprite with multiple frames in a "frames" property
                    for (int i = 0; i < framesArray.Count; i++)
                    {
                        if (framesArray[i] is JsonObject frameObj)
                        {
                            var sprite = ExtractSpriteInfo(frameObj, spriteSize);
                            string flatKey = $"{sheetName}_{key}_{i}";
                            _flattenedSprites[flatKey] = (sheetName, sprite);
                        }
                    }
                }
                else
                {
                    // This is a category, recurse into it
                    FlattenSpriteObject(sheetName, nestedObj, spriteSize, key);
                }
            }
        }
    }

    /// <summary>
    /// Extract x, y, width, height from a sprite JSON object
    /// </summary>
    private SpriteInfo ExtractSpriteInfo(JsonObject obj, int defaultSize)
    {
        var x = obj["x"]?.GetValue<int>() ?? 0;
        var y = obj["y"]?.GetValue<int>() ?? 0;
        var width = obj["width"]?.GetValue<int>() ?? defaultSize;
        var height = obj["height"]?.GetValue<int>() ?? defaultSize;

        return new SpriteInfo
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Name = ""
        };
    }

    /// <summary>
    /// Get a sprite from a sheet by looking up in the flattened dictionary
    /// </summary>

    private CroppedBitmap? GetSprite(string flatKey)
    {
        if (!_isInitialized)
        {
            return null;
        }

        // Console.WriteLine($"[SPRITE] Requesting sprite key={flatKey}");

        if (!_flattenedSprites.TryGetValue(flatKey, out var entry))
        {
            // Console.WriteLine($"[SPRITE] Key not found: {flatKey}");
            return null;
        }

        var (sheetName, info) = entry;

        if (!_spriteSheets.TryGetValue(sheetName, out var sheet))
        {
            Console.WriteLine($"[SPRITE] Sheet not loaded: {sheetName} for key={flatKey}");
            return null;
        }

        try
        {
            var sourceRect = new PixelRect(info.X, info.Y, info.Width, info.Height);
            var cropped = new CroppedBitmap(sheet, sourceRect);
            return cropped;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SPRITE] Error cropping sprite key={flatKey}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get a Pac-Man sprite
    /// </summary>
    public CroppedBitmap? GetPacmanSprite(string direction, int frame)
    {
        string flatKey = $"pacman_pacman_{direction.ToLower()}_{frame}";
        return GetSprite(flatKey);
    }

    /// <summary>
    /// Get a ghost sprite
    /// </summary>
    public CroppedBitmap? GetGhostSprite(string ghostType, string direction, int frame)
    {
        string flatKey = $"ghosts_{ghostType.ToLower()}_{direction.ToLower()}_{frame}";
        // Console.WriteLine($"[SPRITE] GetGhostSprite -> {flatKey}");
        return GetSprite(flatKey);
    }

    /// <summary>
    /// Get a vulnerable ghost sprite
    /// </summary>
    public CroppedBitmap? GetVulnerableGhostSprite(int frame)
    {
        string flatKey = $"ghosts_vulnerable_normal_{frame}";
        // Console.WriteLine($"[SPRITE] GetVulnerableGhostSprite -> {flatKey}");
        return GetSprite(flatKey);
    }

    /// <summary>
    /// Get a warning (flashing) ghost sprite
    /// </summary>
    public CroppedBitmap? GetWarningGhostSprite(int frame)
    {
        string flatKey = $"ghosts_vulnerable_warning_{frame}";
        // Console.WriteLine($"[SPRITE] GetWarningGhostSprite -> {flatKey}");
        return GetSprite(flatKey);
    }

    /// <summary>
    /// Get ghost eyes sprite (when eaten)
    /// </summary>
    public CroppedBitmap? GetGhostEyesSprite(string direction)
    {
        string flatKey = $"ghosts_eyes_only_{direction.ToLower()}";
        return GetSprite(flatKey);
    }

    /// <summary>
    /// Get a collectible item sprite
    /// </summary>
    public CroppedBitmap? GetItemSprite(string itemType, int frame = 0)
    {
        // Try with frame suffix first
        string flatKeyWithFrame = $"items_{itemType.ToLower()}_{frame}";
        var sprite = GetSprite(flatKeyWithFrame);

        if (sprite != null)
        {
            return sprite;
        }

        // If not found, try without frame suffix (for static items like dots)
        string flatKeyNoFrame = $"items_{itemType.ToLower()}";
        return GetSprite(flatKeyNoFrame);
    }

    /// <summary>
    /// Get a tile sprite
    /// </summary>
    public CroppedBitmap? GetTileSprite(string tileType)
    {
        string flatKey = $"tiles_{tileType.ToLower()}";
        return GetSprite(flatKey);
    }

    /// <summary>
    /// Get Pac-Man death animation sprite
    /// </summary>
    public CroppedBitmap? GetDeathSprite(int frame)
    {
        string flatKey = $"pacman_pacman_death_{frame}";
        return GetSprite(flatKey);
    }
}
