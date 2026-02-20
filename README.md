# Pacman Recreation

<a href='https://flathub.org/apps/io.github.jalaucapstones.pacman-recreation'>
  <img width='240' alt='Download on Flathub' 
       src='https://flathub.org/api/badge?locale=en'/>
</a>

A faithful recreation of the classic Pac-Man arcade game, built as an educational project.

## Installation

### Flathub (Recommended)

The easiest way to install Pacman Recreation on Linux is via Flathub:

```bash
flatpak install flathub io.github.jalaucapstones.pacman-recreation
flatpak run io.github.jalaucapstones.pacman-recreation
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
- **Global Top 10 Leaderboard** - Compete with players worldwide
  - Unlock submission by completing all 3 levels
  - Client-side cache + offline pending submission queue
- Profile system with local and global high score tracking
- **Creative Mode** - Design and share custom levels
  - Up to 10 levels per project
  - Configure lives, win score, and per-level difficulty settings
  - Export/import shareable `.pacproj` packages
- Original ghost AI behaviors (Blinky, Pinky, Inky, Clyde)
- Authentic arcade sounds and graphics
- Cross-platform support (Linux x64/ARM64, Windows)

## License

This project is licensed under the MIT License.
