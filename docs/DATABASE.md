# Database Documentation

This document describes the SQLite database schema and usage for the Pac-Man Recreation project.

## Overview

The game uses a local SQLite database to store user profiles and high scores. This ensures data persistence across game sessions.

- **Database Engine:** SQLite (via `Microsoft.Data.Sqlite`)
- **File Location:** `AppData/PacmanGame/profiles.db`
  - **Windows:** `C:\Users\<User>\AppData\Roaming\PacmanGame\profiles.db`
  - **Linux:** `/home/<User>/.config/PacmanGame/profiles.db` (or similar, depending on distro)

## Schema

### 1. Profiles Table

Stores user profile information.

```sql
CREATE TABLE Profiles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    AvatarColor TEXT,
    CreatedAt TEXT NOT NULL,
    LastPlayedAt TEXT
);
```

| Column | Type | Description |
|--------|------|-------------|
| `Id` | INTEGER | Unique identifier for the profile. |
| `Name` | TEXT | Display name of the player (unique). |
| `AvatarColor` | TEXT | Hex color code for the profile avatar (e.g., `#FFFF00`). |
| `CreatedAt` | TEXT | ISO 8601 timestamp of creation. |
| `LastPlayedAt` | TEXT | ISO 8601 timestamp of last activity. |

### 2. Scores Table

Stores game results linked to profiles.

```sql
CREATE TABLE Scores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProfileId INTEGER NOT NULL,
    Score INTEGER NOT NULL,
    Level INTEGER NOT NULL,
    Date TEXT NOT NULL,
    FOREIGN KEY (ProfileId) REFERENCES Profiles(Id) ON DELETE CASCADE
);
```

| Column | Type | Description |
|--------|------|-------------|
| `Id` | INTEGER | Unique identifier for the score entry. |
| `ProfileId` | INTEGER | Foreign key linking to `Profiles.Id`. |
| `Score` | INTEGER | Final score achieved. |
| `Level` | INTEGER | Level reached before game over. |
| `Date` | TEXT | ISO 8601 timestamp of the game session. |

## Common Operations

### Create Profile
```sql
INSERT INTO Profiles (Name, AvatarColor, CreatedAt, LastPlayedAt)
VALUES ('Player1', '#FFFF00', '2026-01-30T12:00:00', '2026-01-30T12:00:00');
```

### Save Score
```sql
INSERT INTO Scores (ProfileId, Score, Level, Date)
VALUES (1, 15000, 3, '2026-01-30T12:30:00');
```

### Get Top Scores (Global)
```sql
SELECT p.Name, s.Score, s.Level, s.Date
FROM Scores s
JOIN Profiles p ON s.ProfileId = p.Id
ORDER BY s.Score DESC
LIMIT 10;
```

### Delete Profile
```sql
DELETE FROM Profiles WHERE Id = 1;
-- Note: Scores are automatically deleted due to ON DELETE CASCADE
```

## Migration & Initialization

The database is initialized automatically on the first run by `ProfileManager.InitializeDatabase()`. It checks if the tables exist and creates them if they don't.

There is currently no versioning or migration system. If the schema changes in future versions, the database file may need to be deleted or manually updated.

## Backup & Reset

To reset all data (profiles and scores):
1. Close the game.
2. Navigate to the `AppData/PacmanGame` folder.
3. Delete `profiles.db`.
4. Restart the game (a new empty database will be created).
