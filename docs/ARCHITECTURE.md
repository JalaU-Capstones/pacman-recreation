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
│   GameEngine, MapLoader, SpriteManager, AudioManager    │
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
    private readonly IGameEngine _gameEngine;
    
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
    public ReactiveCommand<Unit, Unit> PauseGameCommand { get; }
    
    public GameViewModel(IGameEngine gameEngine)
    {
        _gameEngine = gameEngine;
        StartGameCommand = ReactiveCommand.Create(StartGame);
        PauseGameCommand = ReactiveCommand.Create(PauseGame);
    }
    
    private void StartGame() { _gameEngine.Start(); }
    private void PauseGame() { _gameEngine.Pause(); }
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
│       ├── Profile.cs      # User profile
│       ├── ScoreEntry.cs   # High score
│       └── Settings.cs     # Audio settings
│
├── ViewModels/             # MVVM ViewModels
│   ├── ViewModelBase.cs    # Base class for all VMs
│   ├── MainWindowViewModel.cs
│   ├── MainMenuViewModel.cs
│   ├── GameViewModel.cs
│   ├── ScoreBoardViewModel.cs
│   ├── SettingsViewModel.cs
│   ├── ProfileCreationViewModel.cs
│   └── ProfileSelectionViewModel.cs
│
├── Views/                  # MVVM Views
│   ├── MainWindow.axaml
│   ├── MainMenuView.axaml
│   ├── GameView.axaml
│   ├── ScoreBoardView.axaml
│   ├── SettingsView.axaml
│   ├── ProfileCreationView.axaml
│   └── ProfileSelectionView.axaml
│
├── Services/               # Business logic services
│   ├── Interfaces/         # Service contracts
│   │   ├── IGameEngine.cs
│   │   ├── IMapLoader.cs
│   │   ├── ISpriteManager.cs
│   │   ├── IAudioManager.cs
│   │   ├── ICollisionDetector.cs
│   │   └── IProfileManager.cs
│   ├── MapLoader.cs        # Loads maps from .txt files
│   ├── SpriteManager.cs    # Manages sprite loading
│   ├── AudioManager.cs     # Handles audio playback (SFML.Audio)
│   ├── CollisionDetector.cs # Collision detection
│   ├── ProfileManager.cs   # SQLite database management
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
- Database operations

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

### Settings Persistence Flow

```
Profile Selection → ProfileManager.LoadSettings() → Apply to AudioManager
   ↓
   └─> Read from UserSettings table
   
Settings Change → ViewModel.SaveSettings() → ProfileManager.SaveSettings()
   ↓
   └─> Upsert to UserSettings table
```

---

## Key Components

### GameEngine
**Responsibility:** Main game loop and state management

```csharp
public class GameEngine : IGameEngine
{
    private readonly ICollisionDetector _collisionDetector;
    private readonly IAudioManager _audioManager;
    private readonly IMapLoader _mapLoader;
    private readonly ISpriteManager _spriteManager;
    
    private TileType[,] _map;
    private Pacman _pacman;
    private List<Ghost> _ghosts;
    
    public void Start()
    {
        _isRunning = true;
        // Game loop logic
    }
    
    public void Update(float deltaTime)
    {
        UpdatePacman(deltaTime);
        UpdateGhosts(deltaTime);
        UpdateCollisions();
        UpdateTimers(deltaTime);
    }
    
    public void Render(Canvas canvas)
    {
        // Draw tiles, collectibles, Pac-Man, and ghosts
    }
}
```

### ProfileManager
**Responsibility:** Manage user profiles, scores, and settings via SQLite

```csharp
public class ProfileManager : IProfileManager
{
    public void SaveSettings(int profileId, Settings settings)
    {
        // Upsert settings to database
    }
    
    public Settings LoadSettings(int profileId)
    {
        // Load settings or return defaults
    }
}
```

### AudioManager
**Responsibility:** Manage all audio playback using SFML.Audio

```csharp
public class AudioManager : IAudioManager
{
    private float _menuMusicVolume;
    private float _gameMusicVolume;
    
    public void SetMenuMusicVolume(float volume)
    {
        _menuMusicVolume = volume;
        // Update if menu music is playing
    }
    
    public void PlayMusic(string name, bool loop = true)
    {
        // Play music and apply correct volume based on type
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
services.AddSingleton<IGameEngine, GameEngine>();
services.AddSingleton<IProfileManager, ProfileManager>();
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
- GameEngine events (ScoreChanged, LifeLost, etc.)

### 6. Singleton Pattern
- Services (AudioManager, SpriteManager, ProfileManager)

---

## Technologies

### Core Framework
- **.NET 9.0:** Modern, cross-platform framework
- **C# 13:** Latest language features

### UI Framework
- **Avalonia UI 11.x:** Cross-platform XAML-based UI
- **ReactiveUI:** MVVM framework with reactive extensions

### Audio
- **SFML.Audio:** Cross-platform audio playback (Windows/Linux)

### Database
- **SQLite:** Local embedded database for persistence

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

**Last Updated:** February 2026
**Author:** Diego Alejandro Botina
**Project:** Pac-Man Educational Recreation
