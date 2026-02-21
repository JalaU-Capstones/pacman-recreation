# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

## [1.0.1] - Unreleased

### Added
- Global Top 10 Leaderboard system (server-side persistence + client cache + offline pending submission)
- DevConsole (developer console overlay for debugging and quick actions)
- Creative Mode level editor (projects with 1-10 levels, export/import `.pacproj`, play test)

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
- Winget distribution for Windows
- DDNS support for relay server deployment
- Comprehensive documentation (README, DEPLOYMENT, MULTIPLAYER_DESIGN)

### Changed
- High score system now stores only ONE score per profile (maximum score)
- Multiplayer rewards/penalties now tied to ROLES, not players
- Scores persist correctly across role rotations in multiplayer
- Profile Manager now enforces unique high score per profile
- Optimized single-player performance (stable 60 FPS)
- Adjusted Pac-Man speed for balanced difficulty
- Improved ghost pathfinding with caching

### Fixed
- Duplicate high score entries in local database
- Score calculation errors in multiplayer mode
- Multiplayer input detection and player-role mapping
- Ghost autonomous movement in multiplayer
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
