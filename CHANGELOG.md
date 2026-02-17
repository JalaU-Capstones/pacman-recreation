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
- Winget distribution for Windows
- DDNS support for relay server deployment
- Comprehensive documentation (README, DEPLOYMENT, MULTIPLAYER_DESIGN)

### Changed
- Optimized single-player performance (stable 60 FPS)
- Adjusted Pac-Man speed for balanced difficulty
- Improved ghost pathfinding with caching

### Fixed
- Multiplayer input detection and player-role mapping
- Ghost autonomous movement in multiplayer
- Wall collision detection
- Cross-platform database and asset paths
- MVVM compliance across all ViewModels

## [Unreleased]

### Planned
- Additional levels
- Custom maze editor
- Spectator mode improvements
- In-game chat
- Gamepad support
