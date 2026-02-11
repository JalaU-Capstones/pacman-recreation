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
- [Gameplay](#-gameplay)
- [User Interface](#-user-interface)
- [Tech Stack](#-tech-stack)
- [Getting Started](#-getting-started)
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

### Complete Pac-Man Gameplay
- **3 Progressive Levels**: Increasing difficulty with faster ghosts and shorter power-ups
- **4 Unique Ghost AI**: Each ghost (Blinky, Pinky, Inky, Clyde) has distinct behavior
- **Classic Mechanics**: Power pellets, score system, extra lives, death animation
- **Victory Celebration**: Complete all 3 levels to see the victory screen

### Technical Features
- Cross-platform (Windows & Linux)
- MVVM architecture with clean separation of concerns
- Professional logging system
- User profile management with persistent scores
- Adjustable audio settings per profile
- 60 FPS smooth gameplay

---

## 🕹️ Gameplay

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

---

## 🚀 Getting Started

... (rest of the file is unchanged)
