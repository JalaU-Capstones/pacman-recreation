# Pac-Man - Educational Recreation

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-UI-8B5CF6)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux-blue)](https://github.com)

> A modern recreation of the classic Pac-Man arcade game built with .NET 9.0 and Avalonia UI for cross-platform desktop environments.

**Educational Project** - Created as part of Programming 3 course at Universidad Jala.

![Pac-Man Preview](docs/images/preview.png)

*Screenshot will be added during development*

---

## Table of Contents

- [About](#about)
- [Features](#features)
- [Multiplayer Quick Start](#multiplayer-quick-start)
- [Gameplay](#gameplay)
- [User Interface](#user-interface)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [Server Deployment](#server-deployment)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [Game Controls](#game-controls)
- [Logging & Troubleshooting](#logging--troubleshooting)
- [Development Roadmap](#development-roadmap)
- [Assets](#assets)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgments](#acknowledgments)
- [Contact](#contact)

---

## About

This project is an educational recreation of the iconic **Pac-Man** arcade game, developed to demonstrate:

- Object-Oriented Programming (OOP) principles in C#
- MVVM (Model-View-ViewModel) architectural pattern
- Cross-platform desktop application development with Avalonia UI
- Game development concepts (sprite management, collision detection, AI)
- Real-time multiplayer networking
- Audio integration and resource management
- File I/O for score persistence

### Academic Context

- **Course:** Programming 3 (CSPR-231)
- **Institution:** Universidad Jala
- **Semester:** 2026-1
- **Project Type:** Educational Recreation
- **Development Period:** 4 weeks

---

## Features

### Multiplayer (NEW)
- **Up to 5 Players**: Control Pac-Man or one of the 4 ghosts
- **Spectator Mode**: Watch games with up to 5 spectators
- **Public & Private Rooms**: Create public rooms or password-protected private rooms
- **Role Assignment**: Room admin assigns characters to players
- **Synchronized Gameplay**: Real-time updates via relay server
- **Victory Conditions**: Pac-Man must complete 3 levels, Ghosts must eliminate Pac-Man

### Gameplay (Single-Player & Multiplayer)
- 3 Progressive Levels
- 4 Unique Ghost AI
- Classic Mechanics
- Victory Celebration

### Technical Features
- Cross-platform (Windows & Linux)
- MVVM architecture with clean separation of concerns
- Professional logging system
- User profile management with persistent scores
- Adjustable audio settings per profile
- 60 FPS smooth gameplay

---

## Multiplayer Quick Start

1. Launch game → Main Menu → Multiplayer
2. Create Room:
   - Enter room name
   - Choose Public or Private (+ password)
   - Wait for players to join
   - Assign roles to players
   - Click Start Game
3. Join Room:
   - Select room from list
   - Click Join
   - Wait for admin to assign role
   - Game starts when admin clicks Start

---

## Gameplay

### Objective
Navigate Pac-Man through three increasingly difficult mazes, collecting all dots while avoiding ghosts.

### Levels
- **Level 1**: Introduction - Standard ghost speed, 6-second power pellets
- **Level 2**: Intermediate - Ghosts 5% faster, 5-second power pellets
- **Level 3**: Expert - Ghosts 10% faster, 4-second power pellets, maximum aggression

### Scoring
- Small Dot: 10 points
- Power Pellet: 50 points
- Ghosts (when vulnerable): 200, 400, 800, 1600 points (combo multiplier)
- Extra Life: Awarded at 10,000 points

### Victory
Complete all 220 dots in Level 3 to see the victory screen and your final score. Challenge yourself to beat your high score or compete with other profiles!

---

## User Interface

The game features a clean, arcade-style interface with:
- **Left Sidebar**: Real-time score, level, and lives counter
- **Center**: 28x31 tile maze rendered at 896x992 pixels
- **Right Sidebar**: Game controls (Pause/Resume/Menu)
- **Modal Dialogs**: Game Over screen with restart options

---

## Tech Stack

### Core Technologies
- **Framework:** [.NET 9.0](https://dotnet.microsoft.com/)
- **UI Framework:** [Avalonia UI 11.x](https://avaloniaui.net/)
- **Language:** C# 13
- **Architecture:** MVVM (Model-View-ViewModel)
- **Networking:** LiteNetLib (UDP)
- **Database:** SQLite (Microsoft.Data.Sqlite)

### Development Tools
- **IDE:** Visual Studio Code / Visual Studio 2022 / JetBrains Rider
- **Version Control:** Git
- **Package Manager:** NuGet
- **Asset Generation:** Python 3.8+ with NumPy and Pillow

#### Platform Support
- **Windows 10/11** (x64, ARM64)
- **Linux** (x64, ARM64)
  - Tested on: Ubuntu 22.04, Debian 12, Fedora 39
- **macOS** (x64, ARM64) - Theoretical support (not tested)

### Libraries & Dependencies
```xml
<PackageReference Include="Avalonia" Version="11.3.11" />
<PackageReference Include="Avalonia.Desktop" Version="11.3.11" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.11" />
<PackageReference Include="Avalonia.ReactiveUI" Version="11.3.8" />
<PackageReference Include="SFML.Audio" Version="2.6.0" />
<PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.0" />
<PackageReference Include="LiteNetLib" Version="1.2.0" />
<PackageReference Include="MessagePack" Version="2.6.95" />
```

---

## Getting Started

### Prerequisites

#### Required
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Git

#### Optional (for asset regeneration)
- Python 3.8+ with NumPy and Pillow

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/JalaU-Capstones/pacman-recreation.git
   cd pacman-recreation
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

### Running the Game

```bash
dotnet run --project src/PacmanGame/PacmanGame.csproj
```

---

## Server Deployment

The relay server is designed to run on **AWS EC2 Free Tier**. See [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md) for complete setup instructions.

**Server Requirements:**
- Ubuntu 24.04
- .NET 9.0 Runtime
- **Instance Type:** `t2.micro` (1 vCPU, 1 GB RAM)
- Open UDP port 9050

---

## Testing

The project includes a comprehensive unit test suite covering core game logic.

### Running Tests

```bash
cd tests/PacmanGame.Tests
dotnet test
```

### Coverage

Current test coverage: **70%+** of core business logic

**Test Framework:** xUnit  
**Mocking:** Moq  
**Assertions:** FluentAssertions

### Test Organization

- `GameEngineTests.cs` - Game loop and state management
- `CollisionDetectorTests.cs` - Collision detection algorithms
- `MapLoaderTests.cs` - Map parsing and loading
- `BlinkyAI/Pinky/Inky/ClydeTests.cs` - Ghost AI behaviors
- `AStarPathfinderTests.cs` - Pathfinding algorithm
- `PacmanTests.cs` / `GhostTests.cs` - Entity logic
- `ProfileManagerTests.cs` - Database operations

---

## Project Structure

A detailed breakdown of the project structure can be found in [`docs/PROJECT_STRUCTURE.md`](docs/PROJECT_STRUCTURE.md).

---

## Game Controls

### Menu Navigation
- **Arrow Keys (Up/Down):** Navigate menu options
- **Enter:** Select option
- **Escape:** Go back / Exit

### In-Game
- **Arrow Keys (Left/Right/Up/Down):** Move Pac-Man
- **Escape:** Pause/Resume game

---

## Logging & Troubleshooting

The application logs all important events to a log file for troubleshooting:

**Log location:** 
- Windows: `C:\Users\{Username}\AppData\Roaming\PacmanGame\pacman.log`
- Linux: `~/.config/PacmanGame/pacman.log`

---

## Development Roadmap

- [x] **v0.1.0:** Basic gameplay loop, rendering, movement, simple AI.
- [x] **v0.2.0:** User profiles, persistent scores & settings via SQLite.
- [x] **v0.3.0:** Advanced ghost AI (Blinky, Pinky, Inky, Clyde) with pathfinding.
- [x] **v0.4.0:** UI redesign, level progression, death animation, game over screen.
- [x] **v1.0.0:** Power pellets, bonus fruits, multiple levels, polish.
- [ ] **v1.1.0 (Current):** Multiplayer implementation.

---

## Assets

All game assets (sprites, audio, maps) were **procedurally generated** and are included in this repository under the MIT License. Details can be found in [`docs/ASSETS.md`](docs/ASSETS.md).

---

## Contributing

This is an educational project, but contributions are welcome! Please see [`CONTRIBUTING.md`](CONTRIBUTING.md) for details.

---

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

**Disclaimer:** This project is an educational recreation and is not affiliated with Bandai Namco Entertainment Inc.

---

## Acknowledgments

- **Universidad Jala** - Programming 3 Course
- Original Pac-Man game by Toru Iwatani and Namco (1980)
- The .NET and Avalonia UI communities

---

## Contact

**Project Author:** Diego Alejandro Botina
- GitHub: [@CodeWithBotinaOficial](https://github.com/CodeWithBotinaOficial)
- Email: support@codewithbotina.com
- LinkedIn: [codewithbotinaoficial](https://linkedin.com/in/codewithbotinaoficial/)
