using System.Collections.Generic;
using Avalonia.Media;
using PacmanGame.Models.Creative;
using PacmanGame.Services.Interfaces;
using ReactiveUI;

namespace PacmanGame.ViewModels.Creative;

public class ToolboxViewModel : ViewModelBase
{
    private readonly LevelCanvasViewModel _canvasViewModel;
    private readonly ISpriteManager _spriteManager;
    private readonly List<ToolEntry> _tools;

    private ToolEntry _selectedToolEntry = null!;
    private ToolType _selectedTool;

    public IReadOnlyList<ToolEntry> Tools => _tools;

    public ToolEntry SelectedToolEntry
    {
        get => _selectedToolEntry;
        set
        {
            if (_selectedToolEntry == value) return;
            this.RaiseAndSetIfChanged(ref _selectedToolEntry, value);
            SelectedTool = value.Tool;
        }
    }

    public bool HasSelection => _selectedToolEntry != null;

    public ToolType SelectedTool
    {
        get => _selectedTool;
        private set
        {
            if (_selectedTool == value) return;
            this.RaiseAndSetIfChanged(ref _selectedTool, value);
            _canvasViewModel.SelectedTool = value;
        }
    }

    public ToolboxViewModel(LevelCanvasViewModel canvasViewModel, ISpriteManager spriteManager)
    {
        _canvasViewModel = canvasViewModel;
        _spriteManager = spriteManager;
        _spriteManager.Initialize();

        _tools = new List<ToolEntry>
        {
            new(ToolType.WallBlock, "Block", "Solid block wall tile", "1", IsRotatable: false, Icon: _spriteManager.GetTileSprite("walls_cross")),
            new(ToolType.WallLine, "Line", "Thin horizontal/vertical wall (rotate with R)", "2", IsRotatable: true, Icon: _spriteManager.GetTileSprite("walls_horizontal")),
            new(ToolType.WallCorner, "Corner", "Corner wall section usable for turns", "3", IsRotatable: true, Icon: _spriteManager.GetTileSprite("walls_corner_tr")),
            new(ToolType.GhostHouse, "Ghost House", "Pre-built 7x5 ghost den (only one allowed)", "4", IsRotatable: false, Icon: _spriteManager.GetTileSprite("special_ghost_door")),
            new(ToolType.PowerPellet, "Power Pellet", "Makes ghosts vulnerable (place at least 4)", "5", IsRotatable: false, Icon: _spriteManager.GetItemSprite("power_pellet", 0)),
            new(ToolType.Fruit, "Fruit", "Bonus fruit spawn (F). Not allowed inside the ghost house.", "6", IsRotatable: false, Icon: _spriteManager.GetItemSprite("cherry")),
            new(ToolType.Dot, "Dot", "Standard pellet (auto-generated, manual override)", "7", IsRotatable: false, Icon: _spriteManager.GetItemSprite("dot")),
        };
        SelectedToolEntry = _tools[0];
    }

    public void CycleToNextTool()
    {
        var currentIndex = _tools.IndexOf(_selectedToolEntry);
        if (currentIndex < 0)
        {
            SelectedToolEntry = _tools[0];
            return;
        }

        var nextIndex = (currentIndex + 1) % _tools.Count;
        SelectedToolEntry = _tools[nextIndex];
    }

    public sealed record ToolEntry(ToolType Tool, string Label, string Description, string Shortcut, bool IsRotatable, IImage? Icon);
}
