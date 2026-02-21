# Global Top 10 Leaderboard System

## Overview

The Global Top 10 Leaderboard allows players who have completed all 3 levels to compete for a spot in the worldwide top 10 highest scores.

**Release target:** v1.0.1

## Architecture

### Storage
- **Server:** SQLite database on the relay server (example: `pacmanserver.yourdomain.com`)
- **Client:** JSON cache file in `$XDG_CACHE_HOME/pacman-recreation/`

### Network Protocol
- Uses existing LiteNetLib connection to multiplayer server
- New MessageTypes: LeaderboardGetTop10, LeaderboardSubmitScore
- Requests are queued server-side using SemaphoreSlim

### Cache Strategy
- Cache expires after 5 minutes
- Refresh on view open
- Pending updates flush on application exit
- Offline-tolerant: Updates queue until server is reachable

## Eligibility

To submit scores to the global leaderboard:
1. Complete all 3 single-player levels
2. Navigate to Scoreboard → Global Top 10
3. Click "Submit My Score" button

## Score Calculation

### Single-player
- Only your highest score is stored locally
- Subsequent plays only update if new score > old score

### Multiplayer
Scores are tied to ROLES, not players:

**When Pac-Man WINS:**
- Pac-Man: Game points + 5000 bonus
- Ghosts: Lose points equal to what Pac-Man ate (excluding bonus)

**When Pac-Man LOSES:**
- Pac-Man: Game points only
- Ghosts: +1200 each

**Role rotations:**
- Scores from previous round persist
- New round = new rewards based on NEW roles

## Database Schema

```sql
CREATE TABLE GlobalLeaderboard (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProfileId TEXT NOT NULL UNIQUE,
    ProfileName TEXT NOT NULL UNIQUE,
    HighScore INTEGER NOT NULL,
    LastUpdated INTEGER NOT NULL
);
```

## API Endpoints

### Get Top 10
Request: `LeaderboardGetTop10Request`
Response: `LeaderboardGetTop10Response` with list of entries

### Submit Score
Request: `LeaderboardSubmitScoreRequest` with profileId, name, score
Response: `LeaderboardSubmitScoreResponse` with success status and rank

## Implementation Files

### Server
- `PacmanGame.Server/Services/LeaderboardService.cs`
- `PacmanGame.Server/RelayServer.cs` (message handlers)

### Client
- `PacmanGame/Services/GlobalLeaderboardCache.cs`
- `PacmanGame/ViewModels/GlobalLeaderboardViewModel.cs`
- `PacmanGame/Views/GlobalLeaderboardView.axaml`

### Shared
- `PacmanGame.Shared/NetworkMessages.cs` (new message types)

## Security Considerations

1. Profile names must be unique globally
2. Profile IDs are UUIDs, not predictable
3. Server validates all submissions
4. Cache verification prevents stale data
5. No authentication system (trust-based, suitable for game)

## Future Improvements

- Add timestamp display for when scores were achieved
- Implement profile verification with email
- Add regional leaderboards
- Monthly/seasonal leaderboard resets
