using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PacmanGame.Models.Creative;
using PacmanGame.Models.Enums;
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
    public ObservableCollection<LevelCell> Cells { get; }

    private int _cursorX;
    private int _cursorY;
    private string _statusMessage = string.Empty;

    private ToolType _selectedTool = ToolType.WallBlock;
    public ToolType SelectedTool
    {
        get => _selectedTool;
        set => this.RaiseAndSetIfChanged(ref _selectedTool, value);
    }

    public LevelCanvasViewModel()
    {
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
    }

    public int CursorX => _cursorX;
    public int CursorY => _cursorY;
    public int CurrentCellRotation => _grid[_cursorX, _cursorY].Rotation;
    public static int TotalCellSize => CellSize + 2;

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

    public void PlaceTool()
    {
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
        var cell = _grid[_cursorX, _cursorY];
        if (cell.IsPartOfGhostHouse)
        {
            ClearGhostHouse();
            return;
        }
        cell.TileType = CreativeTileType.Empty;
        cell.WallVariant = WallVariant.Block;
        cell.IsPartOfGhostHouse = false;
    }

    public void RotateCurrentCell()
    {
        var cell = _grid[_cursorX, _cursorY];
        cell.Rotation = (cell.Rotation + 90) % 360;
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
            }
        }
        MarkGhostHouseFromStructure(lines);
        _cursorX = 0;
        _cursorY = 0;
        RefreshCursor();
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
                return;
            }
        }
    }

    private void ClearGhostHouse()
    {
        foreach (var ghostCell in Cells.Where(c => c.IsPartOfGhostHouse))
        {
            ghostCell.IsPartOfGhostHouse = false;
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

        // Only allow one ghost house: clear any existing one first.
        if (Cells.Any(c => c.IsPartOfGhostHouse))
        {
            ClearGhostHouse();
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
