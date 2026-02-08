# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned for Final (v1.0.0)
- Advanced ghost AI (4 unique behaviors)
- Power pellet mechanic
- Bonus fruits system
- Multiple levels
- UI polish and animations

## [0.2.0] - 2026-02-02

### Added
- **User Profiles**: Complete profile system with creation, selection, and deletion.
- **Persistent Scores**: SQLite database integration for saving high scores per profile.
- **Settings Menu**: New settings screen for profile management and audio controls.
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

### Assets Created
- Pac-Man sprite sheet (23 sprites)
- Ghosts sprite sheet (40 sprites)
- Items sprite sheet (8 sprites)
- Tiles sprite sheet (17 sprites)
- Sound effects (12 WAV files)
- Background music (3 WAV tracks)
- Level maps (3 TXT files)

---

## Version History

### Version Format
- **Major.Minor.Patch** (e.g., 1.0.0)
- **Major:** Incompatible API changes
- **Minor:** Backwards-compatible functionality
- **Patch:** Backwards-compatible bug fixes

### Release Tags
- `v0.1.0` - Midterm release (Week 4)
- `v1.0.0` - Final release (Week 8)

---

## Categories

### Added
For new features.

### Changed
For changes in existing functionality.

### Deprecated
For soon-to-be removed features.

### Removed
For now removed features.

### Fixed
For any bug fixes.

### Security
In case of vulnerabilities.

---

[Unreleased]: https://github.com/JalaU-Capstones/pacman-recreation/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/JalaU-Capstones/pacman-recreation/releases/tag/v0.1.0
[0.0.1]: https://github.com/JalaU-Capstones/pacman-recreation/releases/tag/v0.0.1
