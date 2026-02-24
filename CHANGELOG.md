# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 24-02-2026

### Fixed
- `HasCompletedAllLevels` now correctly persists to the profile after completing Level 3 in single-player normal mode.
- Completing all 3 levels now unlocks:
    - Global Leaderboard submission
    - Creative Mode access

### Added
- Custom keybindings system (per-profile)
  - Movement, pause, system shortcuts, and Creative Mode controls can be remapped in Settings.
  - Conflict detection with optional reassign (previous action becomes unbound).
  - Changes apply immediately (no restart).
- Mute audio keybinding (default: `M`)
  - Works globally (menus, gameplay, creative mode)
  - Persists across sessions per profile
  - Visual “MUTED” indicator in the UI
- Winget manifest update (winget-pkgs) enabling terminal command shims:
  - `pacman-recreation` (full)
  - `pacman` (alias)

### Changed
- Local profile database is now encrypted (SQLCipher) to prevent external tampering.
- Existing v1.0.1 plaintext databases are migrated to encrypted format on first v1.0.2 launch (data preserved).

### Security
- Database encryption blocks external tools (e.g., `sqlite3`) from reading/modifying:
    - `HasCompletedAllLevels`
    - Scores
    - Settings (including mute state)
    - Keybindings

## [1.0.1] - 21-02-2026

### Added
- Global Top 10 Leaderboard system (server-side persistence + client cache + offline pending submission)
- DevConsole overlay for debugging and quick actions
- Creative Mode project editor (1-10 levels, export/import `.pacproj`, play test)
- In-game FPS counter overlay (toggle with `F1`) in single-player, multiplayer, and creative playtests

### Changed
- Creative Mode configuration now scales dynamically with project level count:
  - Victory reward (win score) limits:
    - 1 level: 100 to 1,000
    - 2 levels: 1,000 to 5,000
    - 3 levels: 5,000 to 12,000
    - 4+ levels: max increases by +5,000 per extra level (e.g. 4 levels: max 17,000)
  - Frightened timer max starts at 20s (level 1) and decreases by 2s per level
  - Fruit points max starts at 5 (level 1) and increases by +5 per level
  - Ghost eat points max starts at 30 (level 1) and increases by +15 per level
  - Speed multipliers are constrained to safe ranges to reduce collision clipping risk
- Creative Mode exports now include `metadata.json` alongside `project.json` for richer project metadata.
- UI now scales responsively within VM/work-area constraints (downscales to prevent overflow).
- Global UI theme resources applied for consistent contrast (no white-on-white).
- Ghost frightened behavior now uses random movement (prefer non-reverse) and slowed speed for authentic feel.
- Eaten ghosts now return to the ghost house base before respawning (no teleport respawns).
- Level speed progression rebalanced: slower Level 1, consistent increases for Levels 2-3, Pac-Man slightly faster than ghosts in straight lines.

### Fixed
- Window sizing issues where content exceeded the screen (common in VMs).
- Contrast issues caused by hardcoded view-level colors.
- Creative Mode ghost house validation now reliably detects a valid structure and seeds missing ghost spawns.
- Creative Mode newly created levels now initialize with boundary walls and a valid ghost house template (prevents empty-canvas validation errors).
- Creative Mode editor supports selecting, moving, and deleting placed objects.
- Creative Mode canvas renders real sprites (WYSIWYG) instead of placeholder blocks.
- Eaten ghosts now path back to the ghost house base before respawning.
- FPS overlay calculation fixed (was incorrectly using Stopwatch ticks as TimeSpan ticks).
- Fixed Windows shortcut shell integration (Desktop + Start Menu shortcuts) and icon persistence (shortcuts point at `Assets/icon.ico`).

## [1.0.0] - 2026-02-17

### Added
- Single-player mode with 3 levels
- Classic Pac-Man gameplay with original maze design
- Ghost AI with authentic behaviors (Blinky, Pinky, Inky, Clyde)
- Power pellets with temporary ghost vulnerability
- Multiplayer mode supporting up to 5 players
- Room system with public and private rooms
- Player role assignment (Pac-Man, Blinky, Pinky, Inky, Clyde, Spectator)
- Profile system with SQLite database
- Score tracking and leaderboards
- Audio system with background music and sound effects
- Cross-platform support (Windows, Linux)
- Flatpak distribution for Linux
- Wall collision detection
- Cross-platform database and asset paths
- MVVM compliance across all ViewModels

### Server
- Added LeaderboardService with SQLite backend
- Implemented request queue with SemaphoreSlim for thread safety
- New network messages: LeaderboardGetTop10, LeaderboardSubmitScore
- Database location: /var/lib/pacman-server/global_leaderboard.db

## [Unreleased]

### Planned
- Additional levels
- Spectator mode improvements
- In-game chat
- Gamepad support
