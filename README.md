# Pacman Recreation

A faithful recreation of the classic Pac-Man arcade game with modern multiplayer support.

## Features

- Classic single-player mode with 3 levels
- Multiplayer mode supporting up to 5 players (Pac-Man + 4 ghosts)
- Original ghost AI behaviors (Blinky, Pinky, Inky, Clyde)
- Profile system with score tracking
- Cross-platform support (Windows, Linux)
- Authentic arcade sounds and graphics

## Installation

### Linux (Flatpak)

```bash
flatpak install flathub com.codewithbotina.PacmanRecreation
flatpak run com.codewithbotina.PacmanRecreation
```

The Flatpak package uses the `org.freedesktop.Platform 24.08` runtime, which will be automatically installed if not present.

### Windows (Winget)

```powershell
winget install CodeWithBotina.PacmanRecreation
```

Or download the latest release from [GitHub Releases](https://github.com/JalaU-Capstones/pacman-recreation/releases).

## Gameplay

### Single Player

Navigate the maze as Pac-Man, collect all dots while avoiding ghosts. Use power pellets to turn the tables and eat the ghosts for bonus points. Complete all 3 levels to win.

### Multiplayer

Join a multiplayer room where one player controls Pac-Man and up to 4 players control the ghosts. Coordinate with other ghost players to catch Pac-Man, or survive as Pac-Man against human-controlled ghosts.

## Building from Source

### Prerequisites

- .NET 9.0 SDK
- Python 3.8+ (for asset generation)

### Build

```bash
dotnet restore
dotnet build
```

### Run

```bash
dotnet run --project src/PacmanGame
```

### Generate Assets

```bash
cd tools/AssetGeneration
python generate-sprites.py
python generate-icons.py
```

## Multiplayer Server

The game connects to a relay server for multiplayer functionality. The default server is hosted at `pacmanserver.codewithbotina.com:9050`.

To host your own server, see [DEPLOYMENT.md](docs/DEPLOYMENT.md).

## Credits

- Original Pac-Man design by Toru Iwatani (Namco, 1980)
- Built with [Avalonia UI](https://avaloniaui.net/)
- Networking via [LiteNetLib](https://github.com/RevenantX/LiteNetLib)
- Audio via [SFML.Net](https://www.sfml-dev.org/)

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

This is an academic capstone project. Contributions are welcome after the initial release.
