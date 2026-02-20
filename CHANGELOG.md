# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-02-17

### Added
- Global Top 10 Leaderboard system with server-side persistence
- Client-side leaderboard cache to minimize server requests
- Profile progression system (must complete all 3 levels to submit scores)
- Multiplayer score rewards and penalties system
  - Pac-Man wins: +5000 bonus points
  - Pac-Man loses: Ghosts get +1200 points each
  - Score penalties for ghosts when Pac-Man wins
- Creative Mode level editor (unlock after completing all 3 levels)
  - 28x31 grid editor with tools (walls, ghost house, power pellets)
  - Tools/Config tabs with zoom controls and cursor preview
  - Per-project and per-level configuration (lives, win score, speed multipliers, points)
  - Export/import .pacproj (editable or play-only) with preview
  - Multi-level projects (1-10 levels) with Prev/Next navigation
  - Play test launches GameView with custom maps/settings
- New Profile database fields: HasCompletedAllLevels, GlobalProfileId
- Global Leaderboard View with submission capability
- Navigation from Local Scoreboard to Global Top 10
- ARM64 architecture support for Flathub
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
