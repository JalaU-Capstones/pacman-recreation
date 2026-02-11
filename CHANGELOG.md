# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-02-15

### Added
- **Multiplayer System**: Up to 5 players + 5 spectators per room
  - Public and private rooms with password protection
  - Room admin can assign player roles (Pac-Man, Blinky, Pinky, Inky, Clyde)
  - Real-time synchronized gameplay via relay server
  - Spectator mode with independent pause
  - Victory/defeat conditions for Pac-Man vs Ghosts
- **Relay Server**: Authoritative server for multiplayer (PacmanGame.Server project)
  - UDP networking with LiteNetLib
  - Room management with SQLite persistence
  - 20 FPS state broadcast, 60 FPS input processing
  - Deployed on Oracle Cloud Free Tier
- **New Views**: MultiplayerMenu, RoomList, CreateRoom, RoomLobby, MultiplayerGame
- **Network Service**: Client-side networking with automatic reconnection
- **Deployment Guide**: Complete Oracle Cloud setup instructions (docs/DEPLOYMENT.md)

## [1.0.0] - 2026-02-12

### Added
- **Level 3 implementation**: Final and hardest level with maximum difficulty
  - Ghost speed increased to 110% (Blinky), 108% (Pinky), 106% (Inky), 100% (Clyde)
  - Power pellet duration reduced to 4 seconds
  - Chase mode extended to 30 seconds, scatter reduced to 10 seconds
  - Ghost respawn time reduced to 1.5 seconds
- **Victory screen**: Celebration dialog with trophy icon when completing all 3 levels
  - "CONGRATULATIONS!" message
  - Final score display
  - Options to play again or return to menu
- **Complete 3-level game loop**: Smooth progression from Level 1 → 2 → 3 → Victory

### Changed
- Ghost AI becomes progressively more aggressive across all 3 levels
- Power pellet effectiveness decreases with each level
- Ghost respawn timing accelerates in higher levels

### Fixed
- All visual bugs from previous versions
- Ghost AI verified against original Pac-Man specifications
- MVVM architecture compliance ensured throughout codebase

## [0.5.0] - 2026-02-12

### Added
- Comprehensive unit test suite with 70%+ code coverage
- xUnit, Moq, and FluentAssertions test infrastructure
- Tests for GameEngine, CollisionDetector, MapLoader
- Tests for all 4 Ghost AI implementations (Blinky, Pinky, Inky, Clyde)
- Tests for A* pathfinding algorithm
- Tests for Pac-Man and Ghost entity logic
- Tests for ProfileManager and database operations
- Coverage reports via coverlet

## [0.4.0] - 2026-02-08

### Added
- Vertical sidebar UI layout for better screen space utilization
- Left sidebar: Score, Level, Lives display
- Right sidebar: Game controls (Pause, Resume, Menu)
- Comprehensive Ghost AI rule verification and fixes
- MVVM architecture compliance audit

### Changed
- Window size adjusted to 1156×1020 to accommodate new sidebar layout
- Game canvas remains centered at 896×992
- HUD and controls now scale better with different screen sizes

### Fixed
- Bottom control bar no longer cut off by canvas
- All Ghost AI behaviors verified against original Pac-Man specifications
- MVVM violations corrected throughout codebase

## [0.3.0] - 2026-02-05

### Added
- **Advanced Ghost AI**: Implemented unique AI for all 4 ghosts (Blinky, Pinky, Inky, Clyde).
- **Chase/Scatter Modes**: Ghosts now alternate between chasing Pac-Man and retreating to their corners.
- **A* Pathfinding**: Ghosts use A* algorithm to navigate the maze intelligently.

### Changed
- **Game Engine**: Updated to manage ghost AI modes and state transitions.
- **Ghost Behavior**: Replaced simple random movement with complex, personality-driven AI.

## [0.2.0] - 2026-02-02

### Added
- **User Profiles**: Complete profile system with creation, selection, and deletion.
- **Persistent Scores**: SQLite database integration for saving high scores per profile.
- **Persistent Settings**: Audio preferences (volume, mute) are now saved per profile in the database.
- **Settings Menu**: New settings screen for profile management and granular audio controls.
- **Database Documentation**: Added `docs/DATABASE.md` detailing the SQLite schema.

### Fixed
- **Main Menu Music**: Fixed issue where music wasn't playing on the main menu.
- **Button Styles**: Fixed button hover states in new profile views to match arcade aesthetic.
- **UI Consistency**: Standardized button styling across all views.

## [0.1.0] - 2026-01-30

### Added
- **Game Engine**: Implemented `GameEngine` service to manage the game loop, entity updates, and state management.
- **Rendering System**: Added canvas-based rendering in `GameView` via `GameEngine.Render()`.
- **Audio System**: Integrated `SFML.Audio` for cross-platform sound playback (Windows/Linux).
- **Pac-Man Movement**: Implemented smooth movement, wall collision, and tunnel wrapping.
- **Ghost AI**: Added basic random movement AI for ghosts.
- **Collision Detection**: Implemented Pac-Man vs. Walls, Dots, and Ghosts collisions.
- **Score System**: Added scoring for dots (10 pts) and ghosts (200+ pts).
- **Life System**: Implemented life loss on ghost collision and game over state.
- **UI Updates**: Removed emojis from all views for consistent cross-platform rendering.
- **Responsive Layout**: Fixed `GameView` layout to adapt to different window sizes.

### Changed
- **Audio Library**: Switched from stub implementation to `SFML.Audio` (v2.6.0).
- **Game Loop**: Now runs at fixed 60 FPS using `DispatcherTimer`.
- **Project Structure**: Added `IGameEngine` interface and implementation.
- **Documentation**: Updated README, ARCHITECTURE, and PROJECT_STRUCTURE to reflect current state.

## [0.0.1] - 2026-01-27

### Added
- Initial project structure
- Asset generation scripts (sprites, audio, maps)
- Project documentation (README, MAP_GUIDE)
- MIT License
- .gitignore and .gitattributes
- EditorConfig for code style
- global.json for .NET SDK version
