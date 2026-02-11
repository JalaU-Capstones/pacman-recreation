# 🏗️ Architecture Documentation

## Table of Contents
- [Overview](#overview)
- [MVVM Pattern](#mvvm-pattern)
- [Project Structure](#project-structure)
- [Layer Responsibilities](#layer-responsibilities)
- [Data Flow](#data-flow)
- [Key Components](#key-components)
- [Level Progression System](#level-progression-system)
- [Ghost AI System](#ghost-ai-system)
- [Logger Service](#logger-service)
- [Design Patterns](#design-patterns)
- [Technologies](#technologies)

---

## Overview

This Pac-Man recreation follows the **MVVM (Model-View-ViewModel)** architectural pattern, which provides:
- Clear separation of concerns
- Testability
- Maintainability
- Reactive UI updates

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                         Views                           │
│              (AXAML + Code-behind)                      │
│    MainMenuView, GameView, ScoreBoardView              │
└────────────────┬────────────────────────────────────────┘
                 │ Data Binding
                 ↓
┌─────────────────────────────────────────────────────────┐
│                     ViewModels                          │
│              (Reactive Properties)                      │
│  MainMenuViewModel, GameViewModel, ScoreBoardViewModel  │
└───────────────────┬─────────────────────────────────────┘
                    │ Business Logic
                    ↓
┌─────────────────────────────────────────────────────────┐
│                      Services                           │
│   GameEngine, MapLoader, SpriteManager, AudioManager    │
└───────────────────┬─────────────────────────────────────┘
                    │ Data Access
                    ↓
┌─────────────────────────────────────────────────────────┐
│                       Models                            │
│          Pacman, Ghost, Collectible, etc.               │
└─────────────────────────────────────────────────────────┘
```

---

## MVVM Pattern

... (rest of the file is unchanged until Key Components)

---

## Key Components

... (rest of the file is unchanged until Ghost AI System)

---

## Level Progression System

The game features 3 levels with progressive difficulty:

**Level 1 (Easy)**
- 244 dots
- Ghost base speed: 100%
- Power pellet: 6 seconds
- Chase/Scatter: 20s/20s

**Level 2 (Medium)**
- 228 dots
- Ghost speed multiplier: 1.05×
- Power pellet: 5 seconds
- Chase/Scatter: 25s/15s

**Level 3 (Hard)**
- 220 dots
- Ghost speed multiplier: 1.10×
- Power pellet: 4 seconds
- Chase/Scatter: 30s/10s

**Victory Condition**
When all dots in Level 3 are collected, the victory screen is displayed. The player can restart from Level 1 or return to the main menu.

**Difficulty Scaling**
All difficulty parameters are defined in `Constants.cs` as `Level{N}GhostSpeedMultiplier`, `Level{N}PowerPelletDuration`, etc. These are applied in `GameEngine.LoadLevel()` when transitioning between levels.

---

## Ghost AI System

... (rest of the file is unchanged)
