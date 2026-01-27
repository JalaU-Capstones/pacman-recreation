# 🏗️ Architecture Documentation

## Table of Contents
- [Overview](#overview)
- [MVVM Pattern](#mvvm-pattern)
- [Project Structure](#project-structure)
- [Layer Responsibilities](#layer-responsibilities)
- [Data Flow](#data-flow)
- [Key Components](#key-components)
- [Design Patterns](#design-patterns)
- [Technologies](#technologies)

---

## Overview

This Pac-Man recreation follows the **MVVM (Model-View-ViewModel)** architectural pattern, which provides:
- Clear separation of concerns
- Testability
- Maintainability
- Reactive UI updates

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                         Views                           │
│              (AXAML + Code-behind)                      │
│    MainMenuView, GameView, ScoreBoardView              │
└────────────────┬────────────────────────────────────────┘
                 │ Data Binding
                 ↓
┌─────────────────────────────────────────────────────────┐
│                     ViewModels                          │
│              (Reactive Properties)                      │
│  MainMenuViewModel, GameViewModel, ScoreBoardViewModel  │
└────────────────┬────────────────────────────────────────┘
                 │ Business Logic
                 ↓
┌─────────────────────────────────────────────────────────┐
│                      Services                           │
│   MapLoader, SpriteManager, AudioManager, AI, etc.      │
└────────────────┬────────────────────────────────────────┘
                 │ Data Access
                 ↓
┌─────────────────────────────────────────────────────────┐
│                       Models                            │
│          Pacman, Ghost, Collectible, etc.               │
└─────────────────────────────────────────────────────────┘
```

---

## MVVM Pattern

### Model
**Responsibility:** Represents the data and business rules
**Location:** `src/PacmanGame/Models/`

**Examples:**
```csharp
// Models/Entities/Pacman.cs
public class Pacman
{
    public int X { get; set; }
    public int Y { get; set; }
    public Direction CurrentDirection { get; set; }
    public int Lives { get; set; }
    public int Score { get; set; }
    public bool IsInvulnerable { get; set; }
}

// Models/Entities/Ghost.cs
public class Ghost
{
    public int X { get; set; }
    public int Y { get; set; }
    public GhostType Type { get; set; }
    public GhostState State { get; set; } // Normal, Vulnerable, Eaten
    public Direction CurrentDirection { get; set; }
}
```

### View
**Responsibility:** Displays data and captures user input
**Location:** `src/PacmanGame/Views/`

**Characteristics:**
- Declarative XAML (AXAML) definitions
- No business logic
- Data binding to ViewModel properties
- Event handlers delegate to ViewModel commands

**Example:**
```xml
<!-- Views/GameView.axaml -->
<UserControl>
    <Grid>
        <Canvas x:Name="GameCanvas" 
                Background="Black"
                Width="{Binding CanvasWidth}"
                Height="{Binding CanvasHeight}"/>
        
        <StackPanel VerticalAlignment="Top" 
                    HorizontalAlignment="Left">
            <TextBlock Text="{Binding Score, StringFormat='SCORE: {0}'}"/>
            <TextBlock Text="{Binding Lives, StringFormat='LIVES: {0}'}"/>
        </StackPanel>
    </Grid>
</UserControl>
```

### ViewModel
**Responsibility:** Mediates between View and Model, handles presentation logic
**Location:** `src/PacmanGame/ViewModels/`

**Characteristics:**
- Implements `INotifyPropertyChanged` (via ReactiveUI)
- Exposes properties for data binding
- Contains Commands for user actions
- No direct reference to View

**Example:**
```csharp
// ViewModels/GameViewModel.cs
public class GameViewModel : ViewModelBase
{
    private readonly MapLoader _mapLoader;
    private readonly AudioManager _audioManager;
    private readonly CollisionDetector _collisionDetector;
    
    private int _score;
    public int Score
    {
        get => _score;
        set => this.RaiseAndSetIfChanged(ref _score, value);
    }
    
    private int _lives;
    public int Lives
    {
        get => _lives;
        set => this.RaiseAndSetIfChanged(ref _lives, value);
    }
    
    public ReactiveCommand<Unit, Unit> StartGameCommand { get; }
    public ReactiveCommand<Direction, Unit> MovePacmanCommand { get; }
    
    public GameViewModel(/* dependencies */)
    {
        StartGameCommand = ReactiveCommand.Create(StartGame);
        MovePacmanCommand = ReactiveCommand.Create<Direction>(MovePacman);
    }
    
    private void StartGame() { /* ... */ }
    private void MovePacman(Direction direction) { /* ... */ }
}
```

---

## Project Structure

```
src/PacmanGame/
├── Models/
│   ├── Entities/           # Game entities
│   │   ├── Pacman.cs
│   │   ├── Ghost.cs
│   │   ├── Collectible.cs
│   │   └── Tile.cs
│   ├── Enums/              # Enumerations
│   │   ├── Direction.cs
│   │   ├── GhostType.cs
│   │   ├── GhostState.cs
│   │   └── CollectibleType.cs
│   └── Game/               # Game state
│       ├── GameState.cs
│       ├── Level.cs
│       └── ScoreEntry.cs
│
├── ViewModels/             # MVVM ViewModels
│   ├── ViewModelBase.cs    # Base class for all VMs
│   ├── MainWindowViewModel.cs
│   ├── MainMenuViewModel.cs
│   ├── GameViewModel.cs
│   ├── ScoreBoardViewModel.cs
│   └── SettingsViewModel.cs
│
├── Views/                  # MVVM Views
│   ├── MainWindow.axaml
│   ├── MainMenuView.axaml
│   ├── GameView.axaml
│   ├── ScoreBoardView.axaml
│   └── SettingsView.axaml
│
├── Services/               # Business logic services
│   ├── MapLoader.cs        # Loads maps from .txt files
│   ├── SpriteManager.cs    # Manages sprite loading
│   ├── AudioManager.cs     # Handles audio playback
│   ├── CollisionDetector.cs # Collision detection
│   ├── ScoreManager.cs     # Score persistence
│   ├── GameEngine.cs       # Main game loop
│   └── AI/
│       ├── IGhostAI.cs     # AI interface
│       ├── BlinkyAI.cs     # Direct chase
│       ├── PinkyAI.cs      # Ambush
│       ├── InkyAI.cs       # Flanking
│       └── ClydeAI.cs      # Random/scatter
│
├── Helpers/                # Utility classes
│   ├── Constants.cs        # Game constants
│   └── Extensions.cs       # Extension methods
│
├── Assets/                 # Game resources
│   ├── Sprites/            # PNG sprite sheets
│   ├── Audio/              # WAV audio files
│   └── Maps/               # TXT map files
│
└── Styles/                 # UI styles
    └── ButtonStyles.axaml
```

---

## Layer Responsibilities

### 1. Models Layer
**What it does:**
- Defines data structures
- Contains business rules
- No dependencies on UI

**What it doesn't do:**
- UI logic
- File I/O (that's in Services)
- User input handling

### 2. ViewModels Layer
**What it does:**
- Prepares data for display
- Handles user commands
- Orchestrates services
- Manages application state

**What it doesn't do:**
- Direct UI manipulation
- File/network I/O
- Complex algorithms (delegate to Services)

### 3. Views Layer
**What it does:**
- Renders UI
- Captures user input
- Data binding

**What it doesn't do:**
- Business logic
- Data transformation
- Service calls

### 4. Services Layer
**What it does:**
- Business logic implementation
- File I/O
- Audio management
- AI algorithms
- Collision detection

**What it doesn't do:**
- UI concerns
- Direct user input handling

---

## Data Flow

### Game Loop Flow

```
┌─────────────────────────────────────────────┐
│  1. User Input (Arrow Keys)                │
│     View captures KeyDown event             │
└──────────────────┬──────────────────────────┘
                   ↓
┌─────────────────────────────────────────────┐
│  2. Command Execution                       │
│     ViewModel.MovePacmanCommand             │
└──────────────────┬──────────────────────────┘
                   ↓
┌─────────────────────────────────────────────┐
│  3. Service Layer                           │
│     GameEngine.Update()                     │
│     - Move Pac-Man                          │
│     - Move Ghosts (AI)                      │
│     - Check Collisions                      │
│     - Update Score                          │
└──────────────────┬──────────────────────────┘
                   ↓
┌─────────────────────────────────────────────┐
│  4. Model Update                            │
│     Pacman.X, Pacman.Y updated              │
│     Ghost positions updated                 │
└──────────────────┬──────────────────────────┘
                   ↓
┌─────────────────────────────────────────────┐
│  5. Property Changed Notification           │
│     INotifyPropertyChanged                  │
└──────────────────┬──────────────────────────┘
                   ↓
┌─────────────────────────────────────────────┐
│  6. UI Update                               │
│     View re-renders via data binding        │
└─────────────────────────────────────────────┘
```

### Score Persistence Flow

```
Game End → ViewModel.SaveScore() → ScoreManager.SaveScore()
   ↓
   └─> Write to scores.txt
   
App Start → ViewModel.LoadScores() → ScoreManager.LoadScores()
   ↓
   └─> Read from scores.txt → Display in ScoreBoardView
```

---

## Key Components

### GameEngine
**Responsibility:** Main game loop and state management

```csharp
public class GameEngine
{
    private readonly ICollisionDetector _collisionDetector;
    private readonly IAudioManager _audioManager;
    private readonly List<IGhostAI> _ghostAIs;
    
    private GameState _gameState;
    private Timer _gameTimer;
    
    public void StartGame(Level level)
    {
        _gameState = new GameState(level);
        _gameTimer = new Timer(UpdateGame, null, 0, 16); // ~60 FPS
    }
    
    private void UpdateGame(object state)
    {
        MovePacman();
        MoveGhosts();
        CheckCollisions();
        UpdatePowerPelletTimer();
        
        if (_gameState.IsGameOver)
            StopGame();
    }
}
```

### SpriteManager
**Responsibility:** Load and manage sprite sheets

```csharp
public class SpriteManager
{
    private Dictionary<string, Bitmap> _spriteSheets;
    private Dictionary<string, SpriteInfo> _spriteMap;
    
    public void LoadSprites()
    {
        _spriteSheets["pacman"] = LoadImage("Assets/Sprites/pacman_spritesheet.png");
        _spriteSheets["ghosts"] = LoadImage("Assets/Sprites/ghosts_spritesheet.png");
        // ...
        
        _spriteMap = LoadSpriteMap("Assets/Sprites/pacman_sprite_map.json");
    }
    
    public Bitmap GetSprite(string name, int frame)
    {
        var info = _spriteMap[name];
        return CropSprite(_spriteSheets[info.SheetName], info.X, info.Y, info.Width, info.Height);
    }
}
```

### AudioManager
**Responsibility:** Manage all audio playback

```csharp
public class AudioManager
{
    private Dictionary<string, SoundEffect> _soundEffects;
    private MediaPlayer _musicPlayer;
    
    public void PlaySoundEffect(string name)
    {
        if (_soundEffects.TryGetValue(name, out var effect))
            effect.Play();
    }
    
    public void PlayMusic(string name, bool loop = true)
    {
        _musicPlayer.Open($"Assets/Audio/Music/{name}.wav");
        _musicPlayer.IsRepeating = loop;
        _musicPlayer.Play();
    }
}
```

---

## Design Patterns

### 1. MVVM (Architectural Pattern)
- Separation of concerns
- Testability
- Data binding

### 2. Dependency Injection
```csharp
// Program.cs
services.AddSingleton<IMapLoader, MapLoader>();
services.AddSingleton<ISpriteManager, SpriteManager>();
services.AddSingleton<IAudioManager, AudioManager>();
services.AddTransient<GameViewModel>();
```

### 3. Command Pattern
```csharp
public ReactiveCommand<Unit, Unit> StartGameCommand { get; }
```

### 4. Strategy Pattern (Ghost AI)
```csharp
public interface IGhostAI
{
    Direction GetNextMove(Ghost ghost, Pacman pacman, Map map);
}

public class BlinkyAI : IGhostAI { /* Direct chase */ }
public class PinkyAI : IGhostAI { /* Ambush */ }
public class InkyAI : IGhostAI { /* Flanking */ }
public class ClydeAI : IGhostAI { /* Random/scatter */ }
```

### 5. Observer Pattern
- Via `INotifyPropertyChanged`
- ReactiveUI observables

### 6. Singleton Pattern
- Services (AudioManager, SpriteManager)

---

## Technologies

### Core Framework
- **.NET 9.0:** Modern, cross-platform framework
- **C# 13:** Latest language features

### UI Framework
- **Avalonia UI 11.x:** Cross-platform XAML-based UI
- **ReactiveUI:** MVVM framework with reactive extensions

### Audio
- **Avalonia.Audio** or **NAudio:** Audio playback

### Testing
- **xUnit:** Unit testing framework
- **Moq:** Mocking library

### Build & CI/CD
- **GitHub Actions:** Automated builds and tests

---

## Best Practices

### Code Organization
✅ One class per file
✅ Meaningful names
✅ SOLID principles
✅ DRY (Don't Repeat Yourself)

### MVVM Guidelines
✅ ViewModels never reference Views
✅ Views never contain business logic
✅ Models are POCOs (Plain Old CLR Objects)
✅ Use Commands for user actions
✅ Use data binding over code-behind

### Performance
✅ Use sprite atlases (sprite sheets)
✅ Object pooling for frequently created objects
✅ Avoid allocations in game loop
✅ Cache frequently accessed data

### Testing
✅ Unit test ViewModels
✅ Unit test Services
✅ Integration tests for game logic
✅ Mock external dependencies

---

## Future Enhancements

### Potential Improvements
- State Machine for game states
- Event Bus for loose coupling
- Resource pooling for better performance
- Async/await for file operations
- Configuration file for game settings

---

**Last Updated:** January 2026  
**Author:** Diego Alejandro Botina
**Project:** Pac-Man Educational Recreation
