# 🎮 Pac-Man - Educational Recreation

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-UI-8B5CF6)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux-blue)](https://github.com)

> A modern recreation of the classic Pac-Man arcade game built with .NET 9.0 and Avalonia UI for cross-platform desktop environments.

**⚠️ Educational Project** - Created as part of Programming 3 course at Universidad Jala.

![Pac-Man Preview](docs/images/preview.png)

*Screenshot will be added during development*

---

## 📋 Table of Contents

- [About](#-about)
- [Features](#-features)
- [User Interface](#-user-interface)
- [Tech Stack](#-tech-stack)
- [Getting Started](#-getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Running the Game](#running-the-game)
- [Project Structure](#-project-structure)
- [Game Controls](#-game-controls)
- [Logging & Troubleshooting](#-logging--troubleshooting)
- [Development Roadmap](#-development-roadmap)
- [Assets](#-assets)
- [Contributing](#-contributing)
- [License](#-license)
- [Acknowledgments](#-acknowledgments)
- [Contact](#-contact)

---

## 🎯 About

This project is an educational recreation of the iconic **Pac-Man** arcade game, developed to demonstrate:

- Object-Oriented Programming (OOP) principles in C#
- MVVM (Model-View-ViewModel) architectural pattern
- Cross-platform desktop application development with Avalonia UI
- Game development concepts (sprite management, collision detection, AI)
- Audio integration and resource management
- File I/O for score persistence

### 🎓 Academic Context

- **Course:** Programming 3 (CSPR-231)
- **Institution:** Universidad Jala
- **Semester:** 2026-1
- **Project Type:** Educational Recreation
- **Development Period:** 4 weeks

---

## ✨ Features

### Core Gameplay
- ✅ Main menu with navigation
- ✅ Classic Pac-Man movement (arrow keys)
- ✅ Basic maze with walls and collectibles
- ✅ Score system & Life system (3 lives)
- ✅ Game over screen with restart option
- ✅ Sound effects for actions (using SFML.Audio)
- ✅ User Profiles: Create, select, and manage multiple player profiles.
- ✅ Persistent Scores: High scores are saved to a local SQLite database.
- ✅ Settings Menu: Manage profiles and audio preferences.
- ✅ Advanced Ghost AI: Unique AI for all 4 ghosts (Blinky, Pinky, Inky, Clyde).
- ✅ Chase/Scatter Modes: Ghosts alternate between chasing Pac-Man and retreating to their corners.
- ✅ A* Pathfinding: Ghosts use A* algorithm to navigate the maze intelligently.
- ✅ Progressive Difficulty: Ghosts get faster and more aggressive in higher levels.
- ✅ Death Animation: Smooth 11-frame Pac-Man death sequence.
- ✅ Level Progression: Automatic transition to next level when completed.

### Final Version (Week 8)
- 🎮 Complete Pac-Man gameplay
- 💊 Power pellets that make ghosts vulnerable
- 🍒 Bonus fruits with special effects
- 🎵 Background music and comprehensive SFX
- 🗺️ Multiple levels with different mazes

---

## 🖥️ User Interface

The game features a clean, arcade-style interface with:
- **Left Sidebar**: Real-time score, level, and lives counter
- **Center**: 28×31 tile maze rendered at 896×992 pixels
- **Right Sidebar**: Game controls (Pause/Resume/Menu)
- **Modal Dialogs**: Game Over screen with restart options

---

## 🛠️ Tech Stack

### Core Technologies
- **Framework:** [.NET 9.0](https://dotnet.microsoft.com/)
- **UI Framework:** [Avalonia UI 11.x](https://avaloniaui.net/)
- **Language:** C# 13
- **Architecture:** MVVM (Model-View-ViewModel)
- **Database:** SQLite (Microsoft.Data.Sqlite)

### Development Tools
- **IDE:** Visual Studio Code / Visual Studio 2022 / JetBrains Rider
- **Version Control:** Git
- **Package Manager:** NuGet
- **Asset Generation:** Python 3.8+ with NumPy and Pillow

#### Platform Support
- ✅ **Windows 10/11** (x64, ARM64)
- ✅ **Linux** (x64, ARM64)
  - Tested on: Ubuntu 22.04, Debian 12, Fedora 39
- ✅ **macOS** (x64, ARM64) - Theoretical support (not tested)

### Libraries & Dependencies
```xml
<PackageReference Include="Avalonia" Version="11.3.11" />
<PackageReference Include="Avalonia.Desktop" Version="11.3.11" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.11" />
<PackageReference Include="Avalonia.ReactiveUI" Version="11.3.8" />
<PackageReference Include="SFML.Audio" Version="2.6.0" />
<PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.0" />
```

---

## 🚀 Getting Started

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

## 📁 Project Structure

A detailed breakdown of the project structure can be found in [`docs/PROJECT_STRUCTURE.md`](docs/PROJECT_STRUCTURE.md).

---

## 🎮 Game Controls

### Menu Navigation
- **Arrow Keys (↑↓):** Navigate menu options
- **Enter:** Select option
- **Escape:** Go back / Exit

### In-Game
- **Arrow Keys (←→↑↓):** Move Pac-Man
- **Escape:** Pause/Resume game

---

## 📝 Logging & Troubleshooting

The application logs all important events to a log file for troubleshooting:

**Log location:** 
- Windows: `C:\Users\{Username}\AppData\Roaming\PacmanGame\pacman.log`
- Linux: `~/.config/PacmanGame/pacman.log`

---

## 🗓️ Development Roadmap

- [x] **v0.1.0:** Basic gameplay loop, rendering, movement, simple AI.
- [x] **v0.2.0:** User profiles, persistent scores & settings via SQLite.
- [x] **v0.3.0:** Advanced ghost AI (Blinky, Pinky, Inky, Clyde) with pathfinding.
- [x] **v0.4.0:** UI redesign, level progression, death animation, game over screen.
- [ ] **v1.0.0 (Final):** Power pellets, bonus fruits, multiple levels, polish.

---

## 🎨 Assets

All game assets (sprites, audio, maps) were **procedurally generated** and are included in this repository under the MIT License. Details can be found in [`docs/ASSETS.md`](docs/ASSETS.md).

---

## 🤝 Contributing

This is an educational project, but contributions are welcome! Please see [`CONTRIBUTING.md`](CONTRIBUTING.md) for details.

---

## 📜 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

**Disclaimer:** This project is an educational recreation and is not affiliated with Bandai Namco Entertainment Inc.

---

## 🙏 Acknowledgments

- **Universidad Jala** - Programming 3 Course
- Original Pac-Man game by Toru Iwatani and Namco (1980)
- The .NET and Avalonia UI communities

---

## 📞 Contact

**Project Author:** Diego Alejandro Botina
- GitHub: [@CodeWithBotinaOficial](https://github.com/CodeWithBotinaOficial)
- Email: support@codewithbotina.com
- LinkedIn: [codewithbotinaoficial](https://linkedin.com/in/codewithbotinaoficial/)
