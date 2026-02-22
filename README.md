# Pacman Recreation

<a href='https://flathub.org/apps/io.github.jalau_capstones.pacman-recreation'>
  <img width='240' alt='Download on Flathub' 
       src='https://flathub.org/api/badge?locale=en'/>
</a>

<img alt='WinGet' src='https://img.shields.io/badge/WinGet-CodeWithBotina.PacmanRecreation-0078D4?style=for-the-badge' />

A faithful recreation of the classic Pac-Man arcade game, built as an educational project.

## Installation

### Flathub (Recommended)

The easiest way to install Pacman Recreation on Linux is via Flathub:

```bash
flatpak install flathub io.github.jalau_capstones.pacman-recreation
flatpak run io.github.jalau_capstones.pacman-recreation
```

### WinGet (Windows)

```powershell
winget install CodeWithBotina.PacmanRecreation
```

### From Source

Requirements:
- .NET 9.0 SDK
- Linux (Ubuntu 22.04+ or equivalent)

```bash
git clone https://github.com/JalaU-Capstones/pacman-recreation.git
cd pacman-recreation
dotnet restore
dotnet run --project src/PacmanGame/PacmanGame.csproj
```

## Features

- Classic single-player mode with 3 progressively difficult levels
- Online multiplayer for up to 5 players
- Profile system with local and global high score tracking
- Original ghost AI behaviors (Blinky, Pinky, Inky, Clyde)
- Authentic arcade sounds and graphics
- Cross-platform support (Linux x64/ARM64, Windows)

## Upcoming in v1.0.1

- **Global Top 10 Leaderboard** (server-side persistence + client cache + offline queue)
- **DevConsole** (developer console overlay for debugging and quick actions)
- **Creative Mode** project editor (1-10 levels per project, configurable scoring and difficulty, export/import `.pacproj`)
- **FPS Overlay**: press `F1` during gameplay to toggle an FPS counter

## License

This project is licensed under the MIT License.
