using ReactiveUI;
using Avalonia.Media.Imaging;

namespace PacmanGame.Models.Creative;

public enum CreativeTileType
{
    Empty,
    Wall,
    Dot,
    PowerPellet,
    GhostSpawn,
    Fruit,
    PacmanSpawn
}

public enum WallVariant
{
    Block,
    LineHorizontal,
    LineVertical,
    Corner,
    GhostHouse
}

public sealed class LevelCell : PacmanGame.ViewModels.ViewModelBase
{
    private CreativeTileType _tileType = CreativeTileType.Empty;
    private WallVariant _wallVariant = WallVariant.Block;
    private int _rotation;
    private bool _isCursor;
    private bool _isSelected;
    private bool _isPartOfGhostHouse;
    private CroppedBitmap? _sprite;

    public int X { get; init; }
    public int Y { get; init; }
    public CreativeTileType TileType
    {
        get => _tileType;
        set => this.RaiseAndSetIfChanged(ref _tileType, value);
    }

    public WallVariant WallVariant
    {
        get => _wallVariant;
        set => this.RaiseAndSetIfChanged(ref _wallVariant, value);
    }

    public int Rotation
    {
        get => _rotation;
        set => this.RaiseAndSetIfChanged(ref _rotation, value);
    }

    public bool IsCursor
    {
        get => _isCursor;
        set => this.RaiseAndSetIfChanged(ref _isCursor, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public bool IsPartOfGhostHouse
    {
        get => _isPartOfGhostHouse;
        set => this.RaiseAndSetIfChanged(ref _isPartOfGhostHouse, value);
    }

    public CroppedBitmap? Sprite
    {
        get => _sprite;
        set => this.RaiseAndSetIfChanged(ref _sprite, value);
    }

    public LevelCell(int x, int y)
    {
        X = x;
        Y = y;
    }
}
