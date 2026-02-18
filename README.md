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

- Single-player mode with multiple levels
- Multiplayer mode for up to 5 players
- Faithful ghost AI implementation
- Score tracking and profile system

## License

This project is licensed under the MIT License.
