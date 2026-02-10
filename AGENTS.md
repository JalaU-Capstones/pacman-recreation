# 🤖 AGENTS.md - AI Assistant Context Document

**Project:** Pac-Man Educational Recreation  
**Course:** Programming 3 (CSPR-231) - Universidad Jala  
**Framework:** .NET 9.0 + Avalonia UI 11.2.3  
**Timeline:** 4 weeks (Midterm at Week 4, Final at Week 8)  
**Date Created:** January 2026  

---

## 📋 Table of Contents

1. [Project Overview](#project-overview)
2. [Academic Requirements](#academic-requirements)
3. [Technical Stack](#technical-stack)
4. [Architecture](#architecture)
5. [Project Structure](#project-structure)
6. [Assets Inventory](#assets-inventory)
7. [Implementation Status](#implementation-status)
8. [Midterm Requirements (Week 4)](#midterm-requirements-week-4)
9. [Final Requirements (Week 8)](#final-requirements-week-8)
10. [Development Guidelines](#development-guidelines)
11. [Key Constants](#key-constants)
12. [Common Tasks & Prompts](#common-tasks--prompts)

---

## 🎯 Project Overview

### **Goal**
Create a fully functional Pac-Man game clone as an educational project to demonstrate:
- Object-Oriented Programming (OOP) mastery
- MVVM architectural pattern implementation
- Cross-platform desktop development
- Game development fundamentals
- Clean code practices

### **Educational Context**
- **University:** Universidad Jala
- **Course:** Programming 3 (CSPR-231)
- **Semester:** 2026-1
- **Development Period:** 4 weeks total
  - **Midterm Checkpoint:** Week 4 (50% functionality)
  - **Final Delivery:** Week 8 (100% functionality)

### **Constraints & Considerations**
- ✅ **Academic Project** - Focus on code quality over performance
- ✅ **Cross-platform** - Must run on Windows and Linux (Ubuntu 22.04)
- ✅ **Educational** - Code must be well-commented and documented
- ✅ **Legal** - All assets created from scratch (no copyright infringement)
- ✅ **Testable** - Architecture should support unit testing

---

## 📚 Academic Requirements

### **Midterm Delivery (Week 4) - 50% Grade**

**Functional Requirements:**
1. ✅ Main menu with navigation
2. ✅ Basic Pac-Man movement (arrow keys)
3. ✅ Simple maze rendering (static)
4. ✅ 2-3 ghosts with **simple AI** (random or basic pattern movement)
5. ✅ Collision detection (walls, dots, ghosts)
6. ✅ Score system (collecting dots)
7. ✅ Life system (3 lives)
8. ✅ Game over screen with restart
9. ✅ Sound effects for key actions

**Technical Requirements:**
- Clean MVVM separation
- At least 2 unit tests
- Proper exception handling
- Code documentation (XML comments)

**Deliverables:**
- ✅ Source code (GitHub/GitLab)
- ✅ README with instructions
- ✅ Video demo (2-3 minutes)
- ✅ Brief technical report (2-3 pages)

### **Final Delivery (Week 8) - 50% Grade**

**Functional Requirements:**
1. ✅ Complete Pac-Man gameplay
2. ✅ 4 ghosts with **unique AI behaviors**:
   - **Blinky (Red):** Direct chase - Always targets Pac-Man's current position
   - **Pinky (Pink):** Ambush - Targets 4 tiles ahead of Pac-Man
   - **Inky (Cyan):** Flanking - Complex behavior based on Blinky and Pac-Man
   - **Clyde (Orange):** Shy - Chases when far, scatters when close
3. ✅ Power pellet mechanic (ghosts become vulnerable)
4. ✅ Bonus fruits with point values
5. ✅ Multiple levels (at least 3)
6. ✅ Background music + complete SFX
7. ✅ Score persistence (saved to file)
8. ✅ Settings menu (audio, controls)
9. ✅ Smooth animations
10. ✅ Progressive difficulty

**Technical Requirements:**
- Complete unit test suite (>70% coverage)
- Performance optimization (60 FPS)
- Error logging
- Settings persistence

**Deliverables:**
- ✅ Complete source code
- ✅ User manual
- ✅ Technical documentation
- ✅ Video demo (5-7 minutes)
- ✅ Final presentation (10 minutes)

---

## 🛠️ Technical Stack

### **Core Framework**
```
.NET 9.0 SDK (9.0.308)
├── C# 13 (latest language features)
└── global.json enforces SDK version
```

### **UI Framework**
```
Avalonia UI 11.2.3
├── Cross-platform XAML-based UI
├── Fluent Theme
├── ReactiveUI for MVVM
└── Data binding support
```

### **Dependencies (NuGet)**
```xml
<PackageReference Include="Avalonia" Version="11.2.3" />
<PackageReference Include="Avalonia.Desktop" Version="11.2.3" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.3" />
<PackageReference Include="Avalonia.Fonts.Inter" Version="11.2.3" />
<PackageReference Include="Avalonia.ReactiveUI" Version="11.2.3" />
<PackageReference Include="Avalonia.Diagnostics" Version="11.2.3" Condition="'$(Configuration)' == 'Debug'" />
<PackageReference Include="SFML.Audio" Version="2.6.0" />
<PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.0" />
```

### **Audio**
- **SFML.Audio**: Cross-platform audio playback.

### **Testing Framework**
```
xUnit + Moq (planned)
```

---

## 🏗️ Architecture

### **Pattern: MVVM (Model-View-ViewModel)**

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

### **Key Architectural Principles**

1. **Separation of Concerns**
   - Views: UI only, no business logic
   - ViewModels: Presentation logic, user actions
   - Services: Business logic, file I/O, algorithms
   - Models: Data structures, game rules

2. **Dependency Injection**
   - Services injected into ViewModels
   - Interfaces for testability
   - Singleton pattern for managers

3. **Reactive Programming**
   - ReactiveUI for property changes
   - Commands for user actions
   - Automatic UI updates via binding

4. **Design Patterns Used**
   - **MVVM** - Main architectural pattern
   - **Dependency Injection** - Service management
   - **Command Pattern** - User actions
   - **Strategy Pattern** - Ghost AI behaviors
   - **Observer Pattern** - Property changes
   - **Singleton** - Service managers

---

## 📁 Project Structure

```
pacman-recreation/
├── .github/                      # GitHub workflows (CI/CD)
├── src/
│   └── PacmanGame/
│       ├── PacmanGame.csproj    # Project file (.NET 9.0)
│       ├── Program.cs            # Entry point
│       ├── App.axaml(.cs)        # Application definition
│       │
│       ├── Assets/               # Game resources (DO NOT MODIFY)
│       │   ├── Sprites/          # PNG + JSON (88 sprites)
│       │   ├── Audio/
│       │   │   ├── Music/        # 3 WAV tracks
│       │   │   └── SFX/          # 12 WAV sound effects
│       │   └── Maps/             # 3 TXT level files
│       │
│       ├── Models/               # Data structures
│       │   ├── Entities/
│       │   │   ├── Entity.cs     # Abstract base class
│       │   │   ├── Pacman.cs     # Player entity
│       │   │   ├── Ghost.cs      # Enemy entity
│       │   │   └── Collectible.cs # Items
│       │   ├── Enums/
│       │   │   ├── Direction.cs
│       │   │   ├── GhostEnums.cs # GhostType, GhostState
│       │   │   └── GameEnums.cs  # CollectibleType, TileType
│       │   └── Game/
│       │       ├── ScoreEntry.cs # High score data
│       │       ├── Profile.cs    # User profile
│       │       └── Settings.cs   # User settings
│       │
│       ├── ViewModels/           # MVVM ViewModels
│       │   ├── ViewModelBase.cs  # Base class
│       │   ├── MainWindowViewModel.cs
│       │   ├── MainMenuViewModel.cs
│       │   ├── GameViewModel.cs  # Main game logic
│       │   ├── ScoreBoardViewModel.cs
│       │   ├── ProfileCreationViewModel.cs
│       │   ├── ProfileSelectionViewModel.cs
│       │   └── SettingsViewModel.cs
│       │
│       ├── Views/                # MVVM Views
│       │   ├── MainWindow.axaml(.cs)
│       │   ├── MainMenuView.axaml(.cs)
│       │   ├── GameView.axaml(.cs) # Game canvas
│       │   ├── ScoreBoardView.axaml(.cs)
│       │   ├── ProfileCreationView.axaml(.cs)
│       │   ├── ProfileSelectionView.axaml(.cs)
│       │   └── SettingsView.axaml(.cs)
│       │
│       ├── Services/             # Business logic
│       │   ├── Interfaces/       # Service contracts
│       │   │   ├── IMapLoader.cs
│       │   │   ├── ISpriteManager.cs
│       │   │   ├── IAudioManager.cs
│       │   │   ├── ICollisionDetector.cs
│       │   │   └── IProfileManager.cs
│       │   ├── AI/               # Ghost AI
│       │   │   ├── IGhostAI.cs
│       │   │   ├── BlinkyAI.cs
│       │   │   ├── PinkyAI.cs
│       │   │   ├── InkyAI.cs
│       │   │   └── ClydeAI.cs
│       │   ├── Pathfinding/      # Pathfinding algorithms
│       │   │   └── AStarPathfinder.cs
│       │   ├── MapLoader.cs      # Load .txt maps
│       │   ├── SpriteManager.cs  # Load PNG sprites
│       │   ├── AudioManager.cs   # Audio playback
│       │   ├── CollisionDetector.cs # Collision logic
│       │   └── ProfileManager.cs # SQLite database management
│       │
│       ├── Helpers/
│       │   └── Constants.cs      # 100+ game constants
│       │
│       └── Styles/
│           └── ButtonStyles.axaml # Arcade button styles
│
├── tests/
│   └── PacmanGame.Tests/         # Unit tests
│
├── tools/
│   └── AssetGeneration/          # Python scripts (REFERENCE ONLY)
│       ├── generate_pacman_sprites.py
│       ├── generate_ghosts_sprites.py
│       ├── generate_items_sprites.py
│       ├── generate_tiles_sprites.py
│       ├── generate_sound_effects.py
│       └── generate_music.py
│
├── docs/
│   ├── ARCHITECTURE.md           # Architecture documentation
│   ├── PROJECT_STRUCTURE.md      # File structure guide
│   ├── MAP_GUIDE.md              # Level creation guide
│   ├── DATABASE.md               # Database schema
│   └── images/                   # Screenshots
│
├── .gitignore                    # Git ignore rules
├── .gitattributes                # Git attributes
├── .editorconfig                 # Code style rules
├── global.json                   # .NET SDK version lock
├── LICENSE                       # MIT License
├── README.md                     # Main documentation
└── CHANGELOG.md                  # Version history
```

---

## 🎨 Assets Inventory

### **Sprites (All Generated - DO NOT MODIFY)**

**Total: 88 sprites across 4 sheets**

| Sheet | Sprites | Format | Size |
|-------|---------|--------|------|
| **pacman_spritesheet.png** | 23 | PNG | 32×32 each |
| **ghosts_spritesheet.png** | 40 | PNG | 32×32 each |
| **items_spritesheet.png** | 8 | PNG | 32×32 each |
| **tiles_spritesheet.png** | 17 | PNG | 32×32 each |

**Sprite Maps (JSON):**
- Each sprite has coordinates in accompanying `.json` file
- Format: `{ "sprites": { "name": { "x": 0, "y": 0, "width": 32, "height": 32 } } }`

**Naming Conventions:**
```
pacman_right_0        → Pac-Man facing right, frame 0
ghost_blinky_up_1     → Blinky facing up, frame 1
ghost_vulnerable_0    → Vulnerable ghost (blue)
ghost_eyes_left       → Eyes returning to base
item_dot              → Small dot
item_power_pellet_0   → Power pellet (animated)
tile_wall_horizontal  → Wall tile
```

### **Audio (All Generated - DO NOT MODIFY)**

**Music (3 tracks, WAV format, 44.1kHz, 16-bit, mono):**
- `background-theme.wav` (30s, looping)
- `menu-theme.wav` (41s)
- `game-over-theme.wav` (27s)

**Sound Effects (12 files, WAV format):**
- `chomp.wav` - Eating dot
- `eat-power-pellet.wav` - Eating power pellet
- `eat-ghost.wav` - Eating ghost
- `eat-fruit.wav` - Eating fruit
- `death.wav` - Pac-Man death
- `extra-life.wav` - Extra life gained
- `game-start.wav` - Level start
- `level-complete.wav` - Level completed
- `game-over.wav` - Game over
- `menu-select.wav` - Menu selection
- `menu-navigate.wav` - Menu navigation
- `ghost-return.wav` - Ghost returning to base

### **Maps (3 levels, TXT format)**

**Format:** 28 columns × 31 rows, ASCII characters

**Character Legend:**
- `#` = Wall (TileType.Wall)
- `.` = Small Dot (+10 points)
- `o` = Power Pellet (+50 points)
- `P` = Pac-Man spawn (exactly 1)
- `G` = Ghost spawn (4-6 positions)
- `-` = Ghost house door
- `F` = Fruit spawn
- ` ` = Empty space

**Levels:**
- `level1.txt` - Easy (244 dots, classic layout)
- `level2.txt` - Medium (228 dots, more walls)
- `level3.txt` - Hard (220 dots, complex maze)

---

## 📊 Implementation Status

### **✅ Completed (Ready to Use)**

| Component | Status | Lines | Description |
|-----------|--------|-------|-------------|
| **Project Structure** | ✅ | - | Complete file organization |
| **Models** | ✅ | ~400 | All entities and enums |
| **ViewModels** | ✅ | ~500 | All ViewModels |
| **Views (AXAML)** | ✅ | ~400 | All views |
| **Constants** | ✅ | ~200 | 100+ game constants |
| **MapLoader** | ✅ | ~200 | Load maps from .txt |
| **SpriteManager** | ✅ | ~250 | Load and crop sprites |
| **CollisionDetector** | ✅ | ~150 | Collision detection |
| **ButtonStyles** | ✅ | ~50 | Arcade UI styles |
| **Documentation** | ✅ | - | README, ARCHITECTURE, etc. |
| **GameEngine** | ✅ | ~300 | Game loop, entity updates |
| **Rendering System** | ✅ | ~150 | Canvas rendering |
| **Pac-Man Movement** | ✅ | ~100 | Player movement logic |
| **AudioManager** | ✅ | ~200 | SFML.Audio integration |
| **Score/Profile Persistence** | ✅ | ~250 | SQLite database management |
| **Advanced Ghost AI** | ✅ | ~400 | Unique AI for all 4 ghosts |

**Total Code:** ~3,500 lines

### **⚠️ Partially Implemented**

| Component | Status | Notes |
|-----------|--------|-------|
| **Power Pellet Mechanics** | ⚠️ | Ghosts become vulnerable, but no scoring combo yet. |
| **Fruit System** | ⚠️ | Fruit spawns but has no effect. |

### **❌ Not Implemented (TO DO)**

| Component | Priority | For |
|-----------|----------|-----|
| **Multiple Levels** | 🟡 Medium | Final |
| **Progressive Difficulty** | 🟡 Medium | Final |
| **Unit Tests** | 🟢 Low | Final |

---

## 🎯 Midterm Requirements (Week 4)

### **CRITICAL - Must Have:**

1. **Game Loop**
   - 60 FPS update loop
   - Fixed delta time
   - Pause/resume functionality

2. **Rendering System**
   ```csharp
   void RenderGame(Canvas canvas)
   {
       // 1. Draw map tiles
       // 2. Draw collectibles
       // 3. Draw Pac-Man
       // 4. Draw ghosts
       // 5. Draw HUD
   }
   ```

3. **Pac-Man Movement**
   - Arrow key input
   - Smooth movement (not instant)
   - Wall collision
   - Wrapping tunnels

4. **Simple Ghost AI (2-3 ghosts)**
   - **Option A:** Random movement
   - **Option B:** Simple chase (move toward Pac-Man)
   - **Option C:** Pattern movement (predefined paths)
   
   **Recommendation:** Use Option A (random) for Midterm, implement full AI for Final

5. **Collision Detection**
   - Pac-Man vs Walls
   - Pac-Man vs Dots (collect + score)
   - Pac-Man vs Ghosts (lose life)
   
6. **Score System**
   - Small dot = +10
   - Display score in HUD
   
7. **Life System**
   - Start with 3 lives
   - Lose life on ghost collision
   - Game over at 0 lives

8. **Sound Effects** (minimal)
   - Chomp (eating dots)
   - Death
   - Game start/over

### **Technical Debt Allowed:**
- ✅ Power pellets can be regular dots
- ✅ Ghosts can have simple AI
- ✅ No fruits needed
- ✅ Only 1 level needed
- ✅ No settings menu
- ✅ No score persistence

---

## 🏆 Final Requirements (Week 8)

### **Ghost AI - Detailed Specifications**

**All ghosts must have 2 modes:**
1. **Chase Mode** - Active hunting
2. **Scatter Mode** - Return to corners

**Mode switching:** Every 20 seconds (configurable)

#### **Blinky (Red) - "Shadow"**
```
Chase: Target = Pac-Man's current position
Scatter: Target = Top-right corner (0, 27)
Speed: Fastest (100% base speed)
```

#### **Pinky (Pink) - "Speedy"**
```
Chase: Target = 4 tiles ahead of Pac-Man's direction
  - If Pac-Man facing up: target (pacman.Y - 4, pacman.X)
  - Account for direction
Scatter: Target = Top-left corner (0, 0)
Speed: 95% base speed
```

#### **Inky (Cyan) - "Bashful"**
```
Chase: Complex calculation
  1. Get point 2 tiles ahead of Pac-Man
  2. Draw vector from Blinky to that point
  3. Double the vector length
  4. That's Inky's target
Scatter: Target = Bottom-right corner (30, 27)
Speed: 95% base speed
```

#### **Clyde (Orange) - "Pokey"**
```
Chase: 
  - If distance to Pac-Man > 8 tiles: target Pac-Man
  - If distance ≤ 8 tiles: scatter
Scatter: Target = Bottom-left corner (30, 0)
Speed: 90% base speed
```

**Pathfinding:** Use A* algorithm or similar for navigation

### **Power Pellet Mechanics**
- Duration: 6 seconds
- Warning: 2 seconds before end (ghosts flash)
- Vulnerable ghosts:
  - Turn blue
  - Speed reduced to 50%
  - Can be eaten for points
- Eaten ghosts:
  - Turn to eyes only
  - Speed increased to 150%
  - Return to ghost house
  - Respawn after 3 seconds

### **Scoring System**
- Small Dot: 10 points
- Power Pellet: 50 points
- Ghost (1st): 200 points
- Ghost (2nd): 400 points
- Ghost (3rd): 800 points
- Ghost (4th): 1,600 points
- Cherry: 100 points
- Strawberry: 300 points
- Orange: 500 points
- Apple: 700 points
- Melon: 1,000 points
- Extra Life: Every 10,000 points

### **Fruit System**
- Spawn after 70 dots collected
- Spawn at center of map
- Disappear after 10 seconds if not collected
- Type depends on level (level 1 = cherry, etc.)

---

## 💻 Development Guidelines

### **Code Style**

**Follow .editorconfig rules:**
```csharp
// Classes and Methods: PascalCase
public class GameEngine { }
public void StartGame() { }

// Private fields: _camelCase
private int _score;

// Properties: PascalCase
public int Score { get; set; }

// Local variables: camelCase
int currentLevel = 1;

// Constants: PascalCase
public const int MaxLives = 3;

// Interfaces: IPascalCase
public interface IMapLoader { }
```

**XML Comments Required:**
```csharp
/// <summary>
/// Brief description of the method
/// </summary>
/// <param name="paramName">Parameter description</param>
/// <returns>Return value description</returns>
public int MethodName(int paramName)
{
    // Implementation
}
```

### **MVVM Rules**

**✅ DO:**
- ViewModels expose properties and commands
- Views bind to ViewModel properties
- Services contain business logic
- Models are POCOs (Plain Old CLR Objects)

**❌ DON'T:**
- ViewModels reference Views
- Views contain business logic
- Direct file I/O in ViewModels
- Game logic in Views

### **Testing Guidelines**

**What to Test:**
- ✅ Collision detection logic
- ✅ Ghost AI path calculation
- ✅ Score calculation
- ✅ Map loading
- ✅ Game state transitions

**What NOT to Test:**
- ❌ Avalonia UI rendering
- ❌ File I/O (use mocks)
- ❌ Audio playback

### **Performance Targets**

- **FPS:** Consistent 60 FPS
- **Input Lag:** < 16ms
- **Memory:** < 200MB RAM
- **Startup Time:** < 3 seconds
- **Map Load:** < 500ms

### **Error Handling**

```csharp
// Service methods
public TileType[,] LoadMap(string fileName)
{
    try
    {
        // Implementation
    }
    catch (FileNotFoundException ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
        throw; // Re-throw for caller to handle
    }
}

// ViewModel methods
public void StartGame()
{
    try
    {
        var map = _mapLoader.LoadMap("level1.txt");
    }
    catch (Exception ex)
    {
        // Show error to user via dialog or message
        Console.WriteLine($"Failed to start game: {ex.Message}");
    }
}
```

---

## 🔢 Key Constants

### **Grid & Display**
```csharp
MapWidth = 28             // Columns
MapHeight = 31            // Rows
TileSize = 32             // Pixels per tile
WindowWidth = 896         // 28 * 32
WindowHeight = 992        // 31 * 32
TargetFps = 60           // Frames per second
```

### **Speeds (tiles per second)**
```csharp
PacmanSpeed = 4.0f
GhostNormalSpeed = 3.7f
GhostVulnerableSpeed = 2.0f
GhostEatenSpeed = 6.0f
```

### **Game Rules**
```csharp
StartingLives = 3
MaxLives = 5
PowerPelletDuration = 6.0f      // seconds
PowerPelletWarningTime = 2.0f   // seconds
ExtraLifeScore = 10000          // points
```

### **File Paths**
```csharp
SpritesPath = "Assets/Sprites"
AudioPath = "Assets/Audio"
MusicPath = "Assets/Audio/Music"
SfxPath = "Assets/Audio/SFX"
MapsPath = "Assets/Maps"
```

---

## 🤖 Common Tasks & Prompts

### **For AI Assistants Working on This Project:**

#### **Task 1: Implement Game Loop**
```
Create a GameEngine service that:
1. Runs at 60 FPS using DispatcherTimer
2. Updates all entities (Pac-Man, ghosts, collectibles)
3. Checks collisions
4. Updates timers (power pellet, etc.)
5. Triggers rendering

Use the existing ICollisionDetector service.
Follow MVVM pattern - GameViewModel should own the GameEngine.
```

#### **Task 2: Implement Rendering**
```
In GameView.axaml.cs, implement rendering on the Canvas:
1. Clear canvas each frame
2. Draw tiles using SpriteManager.GetTileSprite()
3. Draw collectibles (only if IsActive)
4. Draw Pac-Man with current animation frame
5. Draw ghosts with correct sprite based on state

Use Constants.TileSize (32px) for positioning.
Performance: reuse Image controls rather than recreating each frame.
```

#### **Task 3: Implement Pac-Man Movement**
```
In GameViewModel or GameEngine:
1. Listen for arrow key input from GameView
2. Set Pacman.NextDirection
3. In Update loop, check if can turn (using CollisionDetector)
4. Move Pac-Man smoothly (interpolate between tiles)
5. Handle tunnel wrapping (teleport between edges)

Use Pacman.CanMove() method.
Update animation frame based on movement.
```

#### **Task 4: Implement Simple Ghost AI (Midterm)**
```
Create a SimpleGhostAI class:
1. Each Update, pick random valid direction
2. Don't reverse direction (avoid going back)
3. Use CollisionDetector.CanMoveTo() to validate
4. Move ghost in that direction

This is a placeholder for the final AI implementations.
```

#### **Task 5: Implement Collision Handling**
```
In GameEngine.Update():
1. Check Pac-Man vs Dots collision
   - Collect dot (set IsActive = false)
   - Add score
   - Play chomp sound
2. Check Pac-Man vs Ghost collision
   - If Pac-Man invulnerable: eat ghost
   - Else: lose life, reset positions
3. Check if all dots collected (win condition)

Use existing CollisionDetector service methods.
```

#### **Task 6: Implement Score Persistence**
```
Create a ScoreManager service:
1. SaveScore(ScoreEntry) - append to scores.txt
2. LoadHighScores() - read and parse scores.txt
3. IsHighScore(int score) - check if qualifies

File format: "name,score,level,date" (CSV)
Save to: AppData/PacmanGame/scores.txt
Handle file not existing (create with defaults).
```

#### **Task 7: Integrate Audio (NAudio)**
```
Update AudioManager to use NAudio:
1. Install: dotnet add package NAudio
2. Use WaveOutEvent for playback
3. Implement PlaySoundEffect() with AudioFileReader
4. Implement PlayMusic() with looping
5. Add volume control

Keep existing interface (IAudioManager).
Handle exceptions gracefully.
```

#### **Task 8: Implement Blinky AI (Chase)**
```
Create BlinkyAI implementing IGhostAI:
1. In Chase mode: target Pac-Man's current tile
2. In Scatter mode: target top-right corner (0, 27)
3. Use A* pathfinding to navigate
4. Switch modes every 20 seconds

Return next Direction from GetNextMove().
Use CollisionDetector for valid moves.
```

---

## 📝 Additional Context

### **Important Files to Never Modify**
- All files in `Assets/` (generated once)
- `Constants.cs` (unless adding new constants)
- `*.csproj` (unless adding packages)

### **Files to Frequently Modify**
- `GameViewModel.cs` - Main game logic
- `GameView.axaml.cs` - Rendering and input
- `Services/` - Implement new services here

### **Common Pitfalls**

1. **Sprite Loading:**
   - ⚠️ Always use `avares://PacmanGame/...` URIs
   - ⚠️ Crop requires unsafe code (already implemented)

2. **Collision Detection:**
   - ⚠️ Remember: grid positions (Y, X) vs screen (X, Y)
   - ⚠️ Threshold is 0.5 tiles for entity collisions

3. **MVVM:**
   - ⚠️ Don't access Views from ViewModels
   - ⚠️ Use Commands, not direct method calls
   - ⚠️ Services are injected, not created in VMs

4. **Avalonia:**
   - ⚠️ Use `Dispatcher.UIThread.InvokeAsync()` for UI updates from background threads
   - ⚠️ Canvas children must be added/removed on UI thread

### **Useful Commands**

```bash
# Build
dotnet build

# Run
dotnet run --project src/PacmanGame/PacmanGame.csproj

# Clean
dotnet clean

# Test
dotnet test

# Publish (Linux)
dotnet publish -c Release -r linux-x64 --self-contained

# Publish (Windows)
dotnet publish -c Release -r win-x64 --self-contained
```

### **Debug Tips**

- Enable Avalonia DevTools: Press F12 in Debug mode
- Console output: All services log to Console
- Breakpoints: Set in ViewModels and Services, not Views

---

## 🎓 Academic Integrity

This project is for educational purposes. While AI assistance is permitted:
- ✅ Understand all generated code
- ✅ Be able to explain design decisions
- ✅ Write your own tests
- ✅ Document your thought process

**During presentation, you may be asked:**
- Why did you choose this approach?
- How does the MVVM pattern work here?
- Explain the collision detection algorithm
- What design patterns did you use?

---

## 📚 Reference Documents

- **README.md** - Project overview and setup
- **ARCHITECTURE.md** - Detailed architecture docs
- **PROJECT_STRUCTURE.md** - File organization
- **MAP_GUIDE.md** - Level creation guide
- **SERVICES_README.md** - Service layer documentation

---

## 🚀 Current Priority (Next Steps)

**For Midterm (Week 4):**
1. ✅ Implement GameEngine with 60 FPS loop
2. ✅ Implement rendering system on Canvas
3. ✅ Implement Pac-Man movement
4. ✅ Implement simple ghost AI (random movement)
5. ✅ Implement collision handling
6. ✅ Test and debug

**Timeline:**
- Days 1-2: GameEngine + Rendering
- Days 3-4: Movement + Collision
- Day 5: Ghost AI
- Days 6-7: Testing + Polish

---

**Last Updated:** January 30, 2026  
**Version:** 1.0  
**Maintained By:** Project Team

---

## 💡 Quick Reference for AI Agents

**When asked to implement a feature:**
1. Check if service/interface exists
2. Follow MVVM pattern strictly
3. Use existing Constants
4. Add XML comments
5. Handle exceptions
6. Test manually before committing

**When debugging:**
1. Check Console output first
2. Verify service initialization
3. Check file paths (use Constants)
4. Validate MVVM data flow
5. Use Avalonia DevTools (F12)

**When optimizing:**
1. Profile first (don't guess)
2. Target 60 FPS minimum
3. Reuse objects (sprite caching)
4. Minimize allocations in game loop
5. Use object pooling for frequently created objects

---

**Remember:** This is an educational project. Code quality and understanding are more important than clever optimizations. Write clean, readable, well-documented code that demonstrates mastery of OOP and MVVM principles.

**Good luck! 🎮✨**
