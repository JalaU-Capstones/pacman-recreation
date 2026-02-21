using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using PacmanGame.Models.Creative;
using PacmanGame.Models.Enums;
using PacmanGame.Services.Interfaces;
using ReactiveUI;

namespace PacmanGame.ViewModels.Creative;

public class LevelCanvasViewModel : ViewModelBase
{
    public const int GridWidth = 28;
    public const int GridHeight = 31;
    public const int CellSize = 20;
    private const int GhostHouseWidth = 7;
    private const int GhostHouseHeight = 5;

    private readonly LevelCell[,] _grid;
    private readonly ISpriteManager _spriteManager;
    private readonly ILogger<LevelCanvasViewModel> _logger;
    public ObservableCollection<LevelCell> Cells { get; }

    private int _cursorX;
    private int _cursorY;
    private string _statusMessage = string.Empty;
    private PickedObject? _pickedObject;

    private ToolType _selectedTool = ToolType.WallBlock;
    public ToolType SelectedTool
    {
        get => _selectedTool;
        set => this.RaiseAndSetIfChanged(ref _selectedTool, value);
    }

    public LevelCanvasViewModel(ISpriteManager spriteManager, ILogger<LevelCanvasViewModel> logger)
    {
        _spriteManager = spriteManager;
        _logger = logger;
        _spriteManager.Initialize();

        _grid = new LevelCell[GridWidth, GridHeight];
        Cells = new ObservableCollection<LevelCell>();

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                var cell = new LevelCell(x, y);
                _grid[x, y] = cell;
                Cells.Add(cell);
            }
        }

        RefreshCursor();
        SeedDemoLayout();
        RefreshAllSprites();
    }

    public int CursorX => _cursorX;
    public int CursorY => _cursorY;
    public int CurrentCellRotation => _grid[_cursorX, _cursorY].Rotation;
    public static int TotalCellSize => CellSize + 2;
    public bool HasPickedObject => _pickedObject != null;

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public void MoveCursor(int dx, int dy)
    {
        var newX = Math.Clamp(_cursorX + dx, 0, GridWidth - 1);
        var newY = Math.Clamp(_cursorY + dy, 0, GridHeight - 1);
        if (newX == _cursorX && newY == _cursorY)
        {
            return;
        }

        _cursorX = newX;
        _cursorY = newY;
        RefreshCursor();
    }

    public void MoveCursorTo(int x, int y)
    {
        x = Math.Clamp(x, 0, GridWidth - 1);
        y = Math.Clamp(y, 0, GridHeight - 1);
        if (x == _cursorX && y == _cursorY) return;
        _cursorX = x;
        _cursorY = y;
        RefreshCursor();
    }

    public void PlaceTool()
    {
        // If an object is picked up, Enter places it at the cursor (move).
        if (_pickedObject != null)
        {
            PlacePickedObjectAtCursor();
            return;
        }

        var cell = _grid[_cursorX, _cursorY];
        if (cell.IsPartOfGhostHouse && SelectedTool != ToolType.GhostHouse)
        {
            StatusMessage = "The Ghost House structure is locked. Clear it and place it again to move it.";
            return;
        }

        switch (SelectedTool)
        {
            case ToolType.WallBlock:
                cell.TileType = CreativeTileType.Wall;
                cell.WallVariant = WallVariant.Block;
                cell.IsPartOfGhostHouse = false;
                break;
            case ToolType.WallLine:
                cell.TileType = CreativeTileType.Wall;
                cell.WallVariant = WallVariant.LineHorizontal;
                cell.IsPartOfGhostHouse = false;
                break;
            case ToolType.WallCorner:
                cell.TileType = CreativeTileType.Wall;
                cell.WallVariant = WallVariant.Corner;
                cell.IsPartOfGhostHouse = false;
                break;
            case ToolType.GhostHouse:
                if (!TryPlaceGhostHouseAtCursor(out var error))
                {
                    StatusMessage = error ?? "Unable to place Ghost House here.";
                }
                else
                {
                    StatusMessage = "Ghost House placed.";
                    RefreshAllSprites();
                }
                break;
            case ToolType.PowerPellet:
                if (cell.IsPartOfGhostHouse)
                {
                    StatusMessage = "Power pellets cannot be placed inside the Ghost House.";
                    return;
                }
                cell.TileType = CreativeTileType.PowerPellet;
                cell.IsPartOfGhostHouse = false;
                break;
            case ToolType.Fruit:
                if (cell.IsPartOfGhostHouse)
                {
                    StatusMessage = "Fruits cannot be placed inside the Ghost House.";
                    return;
                }
                cell.TileType = CreativeTileType.Fruit;
                cell.IsPartOfGhostHouse = false;
                break;
            case ToolType.Dot:
                if (cell.IsPartOfGhostHouse)
                {
                    StatusMessage = "Dots cannot be placed inside the Ghost House.";
                    return;
                }
                cell.TileType = CreativeTileType.Dot;
                cell.IsPartOfGhostHouse = false;
                break;
            default:
                cell.TileType = CreativeTileType.Empty;
                cell.IsPartOfGhostHouse = false;
                break;
        }

        RefreshSpritesAround(_cursorX, _cursorY);
    }

    private void SeedDemoLayout()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            _grid[x, 0].TileType = CreativeTileType.Wall;
            _grid[x, GridHeight - 1].TileType = CreativeTileType.Wall;
        }

        for (int y = 0; y < GridHeight; y++)
        {
            _grid[0, y].TileType = CreativeTileType.Wall;
            _grid[GridWidth - 1, y].TileType = CreativeTileType.Wall;
        }

        // Seed a valid-ish starter layout so Play Test works immediately:
        // - 1 Pac-Man spawn
        // - 1 Ghost House (7x5)
        // - 4 power pellets
        _grid[GridWidth / 2, GridHeight - 6].TileType = CreativeTileType.PacmanSpawn;

        // Place ghost house near the upper-middle.
        _cursorX = 10;
        _cursorY = 11;
        _ = TryPlaceGhostHouseAtCursor(out _);
        _cursorX = 0;
        _cursorY = 0;

        // 4 pellets in corners inside the border.
        _grid[1, 1].TileType = CreativeTileType.PowerPellet;
        _grid[GridWidth - 2, 1].TileType = CreativeTileType.PowerPellet;
        _grid[1, GridHeight - 2].TileType = CreativeTileType.PowerPellet;
        _grid[GridWidth - 2, GridHeight - 2].TileType = CreativeTileType.PowerPellet;
    }

    public void ClearCell()
    {
        if (_pickedObject != null)
        {
            CancelPickedObject();
            return;
        }

        var cell = _grid[_cursorX, _cursorY];
        if (cell.IsPartOfGhostHouse)
        {
            ClearGhostHouse();
            RefreshAllSprites();
            return;
        }
        cell.TileType = CreativeTileType.Empty;
        cell.WallVariant = WallVariant.Block;
        cell.IsPartOfGhostHouse = false;
        cell.Rotation = 0;

        RefreshSpritesAround(_cursorX, _cursorY);
    }

    public void RotateCurrentCell()
    {
        if (_pickedObject != null)
        {
            _pickedObject = _pickedObject.Value.RotateClockwise();
            StatusMessage = "Rotated selected object.";
            return;
        }

        var cell = _grid[_cursorX, _cursorY];
        cell.Rotation = (cell.Rotation + 90) % 360;
        RefreshSpritesAround(_cursorX, _cursorY);
    }

    public void HandleCellActionAtCursor()
    {
        // Enter semantics:
        // - If carrying an object: place it.
        // - Else if on non-empty: pick it up (move mode).
        // - Else: place current tool.
        var cell = _grid[_cursorX, _cursorY];
        if (_pickedObject != null)
        {
            PlacePickedObjectAtCursor();
            return;
        }

        if (cell.TileType != CreativeTileType.Empty)
        {
            PickUpAtCursor();
            return;
        }

        PlaceTool();
    }

    public void HandleCellClick(int x, int y)
    {
        MoveCursorTo(x, y);

        // Click semantics:
        // - If carrying an object: place it at click position.
        // - Else if clicked a non-empty cell: pick it up for relocation.
        // - Else: place current tool.
        var cell = _grid[_cursorX, _cursorY];
        if (_pickedObject != null)
        {
            PlacePickedObjectAtCursor();
            return;
        }

        if (cell.TileType != CreativeTileType.Empty)
        {
            PickUpAtCursor();
            return;
        }

        PlaceTool();
    }

    public string[] BuildLevelLines()
    {
        var lines = new string[GridHeight];
        var builder = new char[GridWidth];
        for (var y = 0; y < GridHeight; y++)
        {
            for (var x = 0; x < GridWidth; x++)
            {
                builder[x] = MapTileToChar(_grid[x, y]);
            }
            lines[y] = new string(builder);
        }
        return lines;
    }

    public void LoadFromLines(IEnumerable<string> sourceLines)
    {
        var lines = sourceLines.Take(GridHeight).ToArray();
        for (var y = 0; y < GridHeight; y++)
        {
            var line = y < lines.Length ? lines[y] : string.Empty;
            for (var x = 0; x < GridWidth; x++)
            {
                var character = x < line.Length ? line[x] : ' ';
                var cell = _grid[x, y];
                cell.TileType = MapCharToTile(character);
                cell.WallVariant = character switch
                {
                    '-' => WallVariant.GhostHouse,
                    _ => WallVariant.Block
                };
                if (cell.TileType != CreativeTileType.Wall)
                {
                    cell.WallVariant = WallVariant.Block;
                }
                cell.IsPartOfGhostHouse = false;
                cell.IsSelected = false;
                cell.Rotation = 0;
            }
        }
        MarkGhostHouseFromStructure(lines);
        _cursorX = 0;
        _cursorY = 0;
        RefreshCursor();
        _pickedObject = null;
        RefreshAllSprites();
    }

    private char MapTileToChar(LevelCell cell)
    {
        return cell.TileType switch
        {
            CreativeTileType.Wall => cell.WallVariant == WallVariant.GhostHouse ? '-' : '#',
            CreativeTileType.Dot => '.',
            CreativeTileType.PowerPellet => 'o',
            CreativeTileType.GhostSpawn => 'G',
            CreativeTileType.Fruit => 'F',
            CreativeTileType.PacmanSpawn => 'P',
            _ => ' '
        };
    }

    private CreativeTileType MapCharToTile(char character)
    {
        return character switch
        {
            '#' => CreativeTileType.Wall,
            '-' => CreativeTileType.Wall,
            '.' => CreativeTileType.Dot,
            'o' => CreativeTileType.PowerPellet,
            'O' => CreativeTileType.PowerPellet,
            'G' => CreativeTileType.GhostSpawn,
            'g' => CreativeTileType.GhostSpawn,
            'F' => CreativeTileType.Fruit,
            'f' => CreativeTileType.Fruit,
            'P' => CreativeTileType.PacmanSpawn,
            'p' => CreativeTileType.PacmanSpawn,
            _ => CreativeTileType.Empty,
        };
    }

    private void MarkGhostHouseFromStructure(IReadOnlyList<string> lines)
    {
        const int ghostHouseWidth = 7;
        const int ghostHouseHeight = 5;

        if (lines.Count == 0)
        {
            return;
        }

        char At(int x, int y)
        {
            if (y < 0 || y >= lines.Count) return ' ';
            var line = lines[y] ?? string.Empty;
            return x < 0 || x >= line.Length ? ' ' : line[x];
        }

        bool IsGateRow(int startX, int y)
        {
            for (var dx = 0; dx < ghostHouseWidth; dx++)
            {
                var c = At(startX + dx, y);
                if (dx == 2 || dx == 3 || dx == 4)
                {
                    if (c != '-') return false;
                }
                else
                {
                    if (c != '#') return false;
                }
            }
            return true;
        }

        bool IsWallRow(int startX, int y)
        {
            for (var dx = 0; dx < ghostHouseWidth; dx++)
            {
                if (At(startX + dx, y) != '#') return false;
            }
            return true;
        }

        bool MiddleRowsOk(int startX, int startY)
        {
            for (var dy = 1; dy < ghostHouseHeight - 1; dy++)
            {
                if (At(startX, startY + dy) != '#') return false;
                if (At(startX + ghostHouseWidth - 1, startY + dy) != '#') return false;
            }
            return true;
        }

        for (var y = 0; y <= GridHeight - ghostHouseHeight; y++)
        {
            for (var x = 0; x <= GridWidth - ghostHouseWidth; x++)
            {
                var topGate = IsGateRow(x, y) && IsWallRow(x, y + ghostHouseHeight - 1);
                var bottomGate = IsGateRow(x, y + ghostHouseHeight - 1) && IsWallRow(x, y);
                if (!topGate && !bottomGate) continue;
                if (!MiddleRowsOk(x, y)) continue;

                for (var dy = 0; dy < ghostHouseHeight; dy++)
                {
                    for (var dx = 0; dx < ghostHouseWidth; dx++)
                    {
                        _grid[x + dx, y + dy].IsPartOfGhostHouse = true;
                    }
                }

                // Some imported templates include the ghost house structure but omit 'G' spawn markers.
                // Seed the canonical 4 spawn points inside the house so validation/export is reliable.
                var existingSpawns = 0;
                for (var dy = 0; dy < ghostHouseHeight; dy++)
                {
                    for (var dx = 0; dx < ghostHouseWidth; dx++)
                    {
                        if (_grid[x + dx, y + dy].TileType == CreativeTileType.GhostSpawn)
                        {
                            existingSpawns++;
                        }
                    }
                }
                if (existingSpawns < 4)
                {
                    var spawnOffsets = new (int Dx, int Dy)[]
                    {
                        (2, 2), (4, 2),
                        (2, 3), (4, 3),
                    };
                    foreach (var (dx, dy) in spawnOffsets)
                    {
                        var spawnCell = _grid[x + dx, y + dy];
                        spawnCell.TileType = CreativeTileType.GhostSpawn;
                        spawnCell.IsPartOfGhostHouse = true;
                    }
                    _logger.LogInformation("Seeded missing ghost spawns for detected ghost house at ({X},{Y}).", x, y);
                }

                return;
            }
        }
    }

    private void ClearGhostHouse()
    {
        foreach (var ghostCell in Cells.Where(c => c.IsPartOfGhostHouse))
        {
            ghostCell.IsPartOfGhostHouse = false;
            ghostCell.IsSelected = false;
            ghostCell.TileType = CreativeTileType.Empty;
            ghostCell.WallVariant = WallVariant.Block;
            ghostCell.Rotation = 0;
        }
    }

    private bool TryPlaceGhostHouseAtCursor(out string? error)
    {
        error = null;
        var topLeftX = _cursorX;
        var topLeftY = _cursorY;

        if (topLeftX + GhostHouseWidth > GridWidth || topLeftY + GhostHouseHeight > GridHeight)
        {
            error = "Not enough space for Ghost House here (needs 7x5).";
            return false;
        }

        // Only allow one ghost house.
        if (Cells.Any(c => c.IsPartOfGhostHouse))
        {
            error = "Only one Ghost House is allowed. Select it and move it, or delete it first.";
            return false;
        }

        for (int dy = 0; dy < GhostHouseHeight; dy++)
        {
            for (int dx = 0; dx < GhostHouseWidth; dx++)
            {
                var target = _grid[topLeftX + dx, topLeftY + dy];
                if (target.TileType != CreativeTileType.Empty)
                {
                    // Area is occupied; do not place.
                    error = "Cannot place Ghost House here; the 7x5 area must be empty.";
                    return false;
                }
            }
        }

        for (int dy = 0; dy < GhostHouseHeight; dy++)
        {
            for (int dx = 0; dx < GhostHouseWidth; dx++)
            {
                var x = topLeftX + dx;
                var y = topLeftY + dy;
                var target = _grid[x, y];

                target.IsPartOfGhostHouse = true;
                target.Rotation = 0;

                // Top row: walls with a 3-cell ghost door gate in the center.
                if (dy == 0)
                {
                    target.TileType = CreativeTileType.Wall;
                    target.WallVariant = (dx == 2 || dx == 3 || dx == 4) ? WallVariant.GhostHouse : WallVariant.Block;
                    continue;
                }

                // Bottom row: walls.
                if (dy == GhostHouseHeight - 1)
                {
                    target.TileType = CreativeTileType.Wall;
                    target.WallVariant = WallVariant.Block;
                    continue;
                }

                // Middle rows: walls on sides; interior is empty (spawns are placed separately).
                if (dx == 0 || dx == GhostHouseWidth - 1)
                {
                    target.TileType = CreativeTileType.Wall;
                    target.WallVariant = WallVariant.Block;
                }
                else
                {
                    target.TileType = CreativeTileType.Empty;
                }
            }
        }

        // Seed ghost spawns inside the house so ghosts behave like normal mode.
        // The engine expects up to 4 'G' markers; these are within the 7x5 footprint.
        var spawnOffsets = new (int Dx, int Dy)[]
        {
            (2, 2), (4, 2),
            (2, 3), (4, 3),
        };
        foreach (var (dx, dy) in spawnOffsets)
        {
            var spawnCell = _grid[topLeftX + dx, topLeftY + dy];
            spawnCell.TileType = CreativeTileType.GhostSpawn;
            spawnCell.IsPartOfGhostHouse = true;
        }

        return true;
    }

    private void PickUpAtCursor()
    {
        var cell = _grid[_cursorX, _cursorY];
        if (cell.IsPartOfGhostHouse)
        {
            // Pick up the full 7x5 ghost house as one object.
            if (!TryGetGhostHouseBounds(out var bounds))
            {
                StatusMessage = "Unable to select Ghost House (structure not found).";
                return;
            }

            var snapshot = new List<PickedCellSnapshot>(GhostHouseWidth * GhostHouseHeight);
            for (var dy = 0; dy < GhostHouseHeight; dy++)
            {
                for (var dx = 0; dx < GhostHouseWidth; dx++)
                {
                    var c = _grid[bounds.TopLeftX + dx, bounds.TopLeftY + dy];
                    snapshot.Add(new PickedCellSnapshot(dx, dy, c.TileType, c.WallVariant, c.Rotation));
                }
            }

            SetSelectedBounds(bounds.TopLeftX, bounds.TopLeftY, GhostHouseWidth, GhostHouseHeight, selected: true);
            _pickedObject = PickedObject.ForGhostHouse(bounds.TopLeftX, bounds.TopLeftY, snapshot);
            StatusMessage = "Ghost House selected. Click a new location to move it.";
            return;
        }

        // Single-cell pick up.
        cell.IsSelected = true;
        _pickedObject = PickedObject.ForSingleCell(_cursorX, _cursorY, cell.TileType, cell.WallVariant, cell.Rotation);
        StatusMessage = "Tile selected. Click a new cell to move it.";
    }

    private void CancelPickedObject()
    {
        if (_pickedObject == null) return;
        ClearSelection();
        _pickedObject = null;
        StatusMessage = "Selection cancelled.";
    }

    private void PlacePickedObjectAtCursor()
    {
        if (_pickedObject == null) return;
        var picked = _pickedObject.Value;

        if (picked.Kind == PickedKind.GhostHouse)
        {
            if (!CanPlaceGhostHouseAt(_cursorX, _cursorY, picked.OriginX, picked.OriginY))
            {
                StatusMessage = "Cannot move Ghost House here; the 7x5 area must be empty.";
                return;
            }

            // Clear origin (only after we know the destination works).
            ClearGhostHouseAt(picked.OriginX, picked.OriginY);

            foreach (var snap in picked.Snapshot)
            {
                var target = _grid[_cursorX + snap.Dx, _cursorY + snap.Dy];
                target.IsPartOfGhostHouse = true;
                target.TileType = snap.TileType;
                target.WallVariant = snap.WallVariant;
                target.Rotation = snap.Rotation;
            }

            ClearSelection();
            _pickedObject = null;
            StatusMessage = "Ghost House moved.";
            RefreshAllSprites();
            return;
        }

        var originCell = _grid[picked.OriginX, picked.OriginY];
        var destination = _grid[_cursorX, _cursorY];
        if (destination.IsPartOfGhostHouse)
        {
            StatusMessage = "Cannot place tiles inside the Ghost House.";
            return;
        }

        // Place at destination and clear origin.
        destination.TileType = picked.TileType;
        destination.WallVariant = picked.WallVariant;
        destination.Rotation = picked.Rotation;
        destination.IsPartOfGhostHouse = false;

        originCell.TileType = CreativeTileType.Empty;
        originCell.WallVariant = WallVariant.Block;
        originCell.Rotation = 0;
        originCell.IsPartOfGhostHouse = false;

        ClearSelection();
        _pickedObject = null;
        StatusMessage = "Tile moved.";
        RefreshSpritesAround(_cursorX, _cursorY);
        RefreshSpritesAround(picked.OriginX, picked.OriginY);
    }

    private void ClearSelection()
    {
        foreach (var c in Cells.Where(c => c.IsSelected))
        {
            c.IsSelected = false;
        }
    }

    private void SetSelectedBounds(int x, int y, int width, int height, bool selected)
    {
        for (var dy = 0; dy < height; dy++)
        {
            for (var dx = 0; dx < width; dx++)
            {
                _grid[x + dx, y + dy].IsSelected = selected;
            }
        }
    }

    private bool TryGetGhostHouseBounds(out (int TopLeftX, int TopLeftY) bounds)
    {
        bounds = default;
        var cells = Cells.Where(c => c.IsPartOfGhostHouse).ToList();
        if (cells.Count == 0) return false;
        var minX = cells.Min(c => c.X);
        var minY = cells.Min(c => c.Y);
        // The placement is always 7x5.
        bounds = (minX, minY);
        return true;
    }

    private bool CanPlaceGhostHouseAt(int destX, int destY, int originX, int originY)
    {
        if (destX + GhostHouseWidth > GridWidth || destY + GhostHouseHeight > GridHeight)
        {
            return false;
        }

        // Check emptiness allowing overlap with the origin footprint.
        for (var dy = 0; dy < GhostHouseHeight; dy++)
        {
            for (var dx = 0; dx < GhostHouseWidth; dx++)
            {
                var x = destX + dx;
                var y = destY + dy;
                var isOriginFootprint = x >= originX && x < originX + GhostHouseWidth && y >= originY && y < originY + GhostHouseHeight;
                if (isOriginFootprint) continue;

                if (_grid[x, y].TileType != CreativeTileType.Empty)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void ClearGhostHouseAt(int originX, int originY)
    {
        for (var dy = 0; dy < GhostHouseHeight; dy++)
        {
            for (var dx = 0; dx < GhostHouseWidth; dx++)
            {
                var c = _grid[originX + dx, originY + dy];
                c.TileType = CreativeTileType.Empty;
                c.WallVariant = WallVariant.Block;
                c.Rotation = 0;
                c.IsPartOfGhostHouse = false;
                c.IsSelected = false;
            }
        }
    }

    private void RefreshAllSprites()
    {
        for (var y = 0; y < GridHeight; y++)
        {
            for (var x = 0; x < GridWidth; x++)
            {
                UpdateSpriteForCell(x, y);
            }
        }
    }

    private void RefreshSpritesAround(int x, int y)
    {
        foreach (var (nx, ny) in new[] { (x, y), (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1) })
        {
            if (nx < 0 || ny < 0 || nx >= GridWidth || ny >= GridHeight) continue;
            UpdateSpriteForCell(nx, ny);
        }
    }

    private void UpdateSpriteForCell(int x, int y)
    {
        var cell = _grid[x, y];

        CroppedBitmap? sprite = cell.TileType switch
        {
            CreativeTileType.Wall when cell.WallVariant == WallVariant.GhostHouse => _spriteManager.GetTileSprite("special_ghost_door"),
            CreativeTileType.Wall => _spriteManager.GetTileSprite(GetWallSpriteName(y, x)),
            CreativeTileType.Dot => _spriteManager.GetItemSprite("dot"),
            CreativeTileType.PowerPellet => _spriteManager.GetItemSprite("power_pellet", 0),
            CreativeTileType.Fruit => _spriteManager.GetItemSprite("cherry"),
            CreativeTileType.PacmanSpawn => _spriteManager.GetPacmanSprite("right", 0),
            CreativeTileType.GhostSpawn => _spriteManager.GetGhostSprite("blinky", "left", 0),
            _ => _spriteManager.GetTileSprite("special_empty")
        };

        cell.Sprite = sprite;
    }

    private string GetWallSpriteName(int row, int col)
    {
        bool hasUp = row > 0 && IsWallForAdjacency(col, row - 1);
        bool hasDown = row < GridHeight - 1 && IsWallForAdjacency(col, row + 1);
        bool hasLeft = col > 0 && IsWallForAdjacency(col - 1, row);
        bool hasRight = col < GridWidth - 1 && IsWallForAdjacency(col + 1, row);

        int neighbors = (hasUp ? 1 : 0) + (hasDown ? 1 : 0) + (hasLeft ? 1 : 0) + (hasRight ? 1 : 0);

        if (neighbors == 4) return "walls_cross";

        if (neighbors == 3)
        {
            if (!hasRight) return "walls_t_left";
            if (!hasLeft) return "walls_t_right";
            if (!hasDown) return "walls_t_up";
            if (!hasUp) return "walls_t_down";
        }

        if (neighbors == 2)
        {
            if (hasUp && hasDown) return "walls_vertical";
            if (hasLeft && hasRight) return "walls_horizontal";
            if (hasDown && hasRight) return "walls_corner_br";
            if (hasDown && hasLeft) return "walls_corner_bl";
            if (hasUp && hasRight) return "walls_corner_tr";
            if (hasUp && hasLeft) return "walls_corner_tl";
        }

        if (neighbors == 1)
        {
            if (hasUp) return "walls_end_down";
            if (hasDown) return "walls_end_up";
            if (hasLeft) return "walls_end_left";
            if (hasRight) return "walls_end_right";
        }

        // Isolated walls are rendered as empty in the runtime wall renderer.
        return "special_empty";
    }

    private bool IsWallForAdjacency(int x, int y)
    {
        var cell = _grid[x, y];
        return cell.TileType == CreativeTileType.Wall && cell.WallVariant != WallVariant.GhostHouse;
    }

    private readonly record struct PickedCellSnapshot(int Dx, int Dy, CreativeTileType TileType, WallVariant WallVariant, int Rotation);

    private enum PickedKind
    {
        SingleCell,
        GhostHouse
    }

    private readonly record struct PickedObject(
        PickedKind Kind,
        int OriginX,
        int OriginY,
        CreativeTileType TileType,
        WallVariant WallVariant,
        int Rotation,
        IReadOnlyList<PickedCellSnapshot> Snapshot)
    {
        public static PickedObject ForSingleCell(int originX, int originY, CreativeTileType tileType, WallVariant wallVariant, int rotation)
            => new(PickedKind.SingleCell, originX, originY, tileType, wallVariant, rotation, Array.Empty<PickedCellSnapshot>());

        public static PickedObject ForGhostHouse(int originX, int originY, IReadOnlyList<PickedCellSnapshot> snapshot)
            => new(PickedKind.GhostHouse, originX, originY, CreativeTileType.Empty, WallVariant.Block, 0, snapshot);

        public PickedObject RotateClockwise()
        {
            if (Kind != PickedKind.SingleCell)
            {
                return this;
            }
            return this with { Rotation = (Rotation + 90) % 360 };
        }
    }

    private void RefreshCursor()
    {
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                _grid[x, y].IsCursor = x == _cursorX && y == _cursorY;
            }
        }

        this.RaisePropertyChanged(nameof(CursorX));
        this.RaisePropertyChanged(nameof(CursorY));
        this.RaisePropertyChanged(nameof(CurrentCellRotation));
    }
}
