using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PacmanGame.Helpers;
using PacmanGame.Services.Interfaces;
using PacmanGame.Services.Models;

namespace PacmanGame.Services;

/// <summary>
/// Service for managing sprite sheets and providing sprite access.
/// Loads PNG sprite sheets and JSON mapping files from Assets/Sprites/
/// </summary>
public class SpriteManager : ISpriteManager
{
    private readonly Dictionary<string, Bitmap> _spriteSheets = new();
    private readonly Dictionary<string, SpriteSheet> _spriteMaps = new();
    private bool _isInitialized = false;

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
    /// Load a sprite sheet and its JSON mapping
    /// </summary>
    private void LoadSpriteSheet(string name, string imageFileName, string mapFileName)
    {
        try
        {
            // Load the PNG sprite sheet using Avalonia's asset system
            var uri = new Uri($"avares://PacmanGame/{Constants.SpritesPath}/{imageFileName}");
            var asset = AssetLoader.Open(uri);
            _spriteSheets[name] = new Bitmap(asset);

            // Load the JSON mapping
            var mapUri = new Uri($"avares://PacmanGame/{Constants.SpritesPath}/{mapFileName}");
            var mapAsset = AssetLoader.Open(mapUri);
            using var reader = new StreamReader(mapAsset);
            string json = reader.ReadToEnd();

            // Parse JSON
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var spriteSheet = JsonSerializer.Deserialize<SpriteSheet>(json, options);
            if (spriteSheet != null)
            {
                _spriteMaps[name] = spriteSheet;
                Console.WriteLine($"   Loaded {name}: {spriteSheet.Sprites.Count} sprites");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error loading {name}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get a sprite from a sheet by cropping at specified coordinates
    /// </summary>
    private CroppedBitmap? GetSprite(string sheetName, string spriteName)
    {
        if (!_isInitialized)
        {
            Console.WriteLine("⚠️  SpriteManager not initialized!");
            return null;
        }

        if (!_spriteSheets.TryGetValue(sheetName, out var sheet))
        {
            Console.WriteLine($"⚠️  Sprite sheet '{sheetName}' not found");
            return null;
        }

        if (!_spriteMaps.TryGetValue(sheetName, out var map))
        {
            Console.WriteLine($"⚠️  Sprite map '{sheetName}' not found");
            return null;
        }

        if (!map.Sprites.TryGetValue(spriteName, out var info))
        {
            Console.WriteLine($"⚠️  Sprite '{spriteName}' not found in {sheetName}");
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
            Console.WriteLine($"❌ Error cropping sprite '{spriteName}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get a Pac-Man sprite
    /// </summary>
    public CroppedBitmap? GetPacmanSprite(string direction, int frame)
    {
        // Sprite naming convention: "pacman_{direction}_{frame}"
        // Examples: "pacman_right_0", "pacman_up_1", etc.
        string spriteName = $"pacman_{direction.ToLower()}_{frame}";
        return GetSprite("pacman", spriteName);
    }

    /// <summary>
    /// Get a ghost sprite
    /// </summary>
    public CroppedBitmap? GetGhostSprite(string ghostType, string direction, int frame)
    {
        // Sprite naming convention: "ghost_{type}_{direction}_{frame}"
        // Examples: "ghost_blinky_right_0", "ghost_pinky_up_1", etc.
        string spriteName = $"ghost_{ghostType.ToLower()}_{direction.ToLower()}_{frame}";
        return GetSprite("ghosts", spriteName);
    }

    /// <summary>
    /// Get a vulnerable ghost sprite
    /// </summary>
    public CroppedBitmap? GetVulnerableGhostSprite(int frame)
    {
        // Vulnerable ghosts are blue
        string spriteName = $"ghost_vulnerable_{frame}";
        return GetSprite("ghosts", spriteName);
    }

    /// <summary>
    /// Get a warning (flashing) ghost sprite
    /// </summary>
    public CroppedBitmap? GetWarningGhostSprite(int frame)
    {
        // Warning ghosts flash blue/white
        string spriteName = $"ghost_warning_{frame}";
        return GetSprite("ghosts", spriteName);
    }

    /// <summary>
    /// Get ghost eyes sprite (when eaten)
    /// </summary>
    public CroppedBitmap? GetGhostEyesSprite(string direction)
    {
        // Just eyes, no body
        string spriteName = $"ghost_eyes_{direction.ToLower()}";
        return GetSprite("ghosts", spriteName);
    }

    /// <summary>
    /// Get a collectible item sprite
    /// </summary>
    public CroppedBitmap? GetItemSprite(string itemType, int frame = 0)
    {
        // Item naming: "item_{type}" or "item_{type}_{frame}"
        string spriteName = frame > 0
            ? $"item_{itemType.ToLower()}_{frame}"
            : $"item_{itemType.ToLower()}";

        return GetSprite("items", spriteName);
    }

    /// <summary>
    /// Get a tile sprite
    /// </summary>
    public CroppedBitmap? GetTileSprite(string tileType)
    {
        // Tile naming: "tile_{type}"
        // Examples: "tile_wall_horizontal", "tile_corner_tl", etc.
        string spriteName = $"tile_{tileType.ToLower()}";
        return GetSprite("tiles", spriteName);
    }

    /// <summary>
    /// Get Pac-Man death animation sprite
    /// </summary>
    public CroppedBitmap? GetDeathSprite(int frame)
    {
        // Death animation frames
        string spriteName = $"pacman_death_{frame}";
        return GetSprite("pacman", spriteName);
    }
}
