# 📝 Logging System Documentation

This document outlines the logging system implemented in the Pac-Man recreation project. The system is designed to provide clear, timestamped, and categorized messages for debugging, troubleshooting, and monitoring application behavior without cluttering the console.

---

## 📍 Log File Location

The application logs all important events to a text file named `pacman.log`. The location of this file varies depending on the operating system:

-   **Windows:**
    `C:\Users\{Username}\AppData\Roaming\PacmanGame\pacman.log`
    (e.g., `C:\Users\JohnDoe\AppData\Roaming\PacmanGame\pacman.log`)

-   **Linux:**
    `~/.config/PacmanGame/pacman.log`
    (e.g., `/home/johndoe/.config/PacmanGame/pacman.log`)

The log directory (`PacmanGame`) is automatically created if it does not exist.

---

## 📄 Log Format and Structure

Each entry in the `pacman.log` file follows a consistent format:

```
[YYYY-MM-DD HH:mm:ss] [LEVEL] Message
```

-   **`[YYYY-MM-DD HH:mm:ss]`**: A timestamp indicating when the log entry was recorded, in `Year-Month-Day Hour:Minute:Second` format.
-   **`[LEVEL]`**: The severity level of the log message.
-   **`Message`**: A descriptive text explaining the event or issue.

**Example Log Entries:**

```
[2026-02-10 18:45:32] [INFO] Game started - Level 1
[2026-02-10 18:45:33] [INFO] Audio system initialized successfully
[2026-02-10 18:45:35] [ERROR] Failed to load sprite 'ghost_blinky_up_3': File not found
[2026-02-10 18:45:40] [WARNING] Ghost pathfinding failed - using fallback random move
[2026-02-10 18:46:12] [INFO] Power pellet collected - Ghosts now vulnerable
[2026-02-10 18:46:15] [DEBUG] Blinky target=(0,27) mode=Scatter
```

---

## 🚦 Log Levels

The logging system categorizes messages into different severity levels to help prioritize and filter information:

-   **`INFO`**:
    -   **Purpose:** Normal application events, significant state changes, and successful operations.
    -   **Examples:** Game start/stop, level loaded, profile created, audio system initialized, settings saved.
    -   **Usage:** Provides a general overview of the application's flow.

-   **`WARNING`**:
    -   **Purpose:** Non-critical issues or potential problems that do not immediately halt execution but might indicate an unexpected condition or a fallback mechanism being used.
    -   **Examples:** Sound effect file not found (but game continues), sprite not found (using default), ghost pathfinding failed (using random move).
    -   **Usage:** Alerts developers to situations that might need attention but aren't immediate show-stoppers.

-   **`ERROR`**:
    -   **Purpose:** Critical errors that impact functionality, prevent an operation from completing, or indicate a serious problem. These often include exception details.
    -   **Examples:** Failed to load map file, database connection failed, unhandled exceptions in services.
    -   **Usage:** Highlights issues that require immediate investigation and resolution.

-   **`DEBUG`**:
    -   **Purpose:** Detailed information primarily used during development for tracing execution flow, variable values, and intricate logic. These messages are typically verbose.
    -   **Examples:** Ghost AI target calculations, pathfinding steps, detailed collision checks.
    -   **Usage:** Should generally be disabled or filtered out in production builds to avoid performance overhead and excessive log file size.

---

## 🔍 How to Read the Log for Troubleshooting

When encountering an issue with the application, the `pacman.log` file is the first place to look:

1.  **Locate the `pacman.log` file** based on your operating system (see [Log File Location](#log-file-location)).
2.  **Open the file** with any text editor.
3.  **Search for `ERROR` entries:** These are the most critical and often point directly to the root cause of a crash or malfunction. Pay attention to the accompanying exception details (stack traces).
4.  **Review `WARNING` entries:** These might explain unexpected behavior or performance quirks. A series of warnings could indicate an underlying problem.
5.  **Examine `INFO` entries:** Trace the sequence of events leading up to the issue. This can help identify which part of the application was active when the problem occurred.
6.  **Use `DEBUG` entries (if enabled):** For very specific issues, enabling debug logging (if implemented) can provide granular details about internal processes.

---

## 🧹 Log File Management

The `pacman.log` file is designed to append new entries on each application run. Over time, this file can grow large.

-   **Clearing the Log:** To clear the log file, simply delete `pacman.log` from its location. A new, empty log file will be created the next time the application runs.
-   **Log Rotation:** For long-running applications, a more sophisticated log rotation mechanism (e.g., archiving old logs, limiting file size) might be implemented in the future. Currently, manual deletion is required.

---

## 🚫 Common Error Messages and What They Mean

-   **`[ERROR] Failed to load map file: [path]`**:
    -   **Meaning:** The game could not find or read a required map file.
    -   **Action:** Verify that the `Assets/Maps` directory exists and contains the expected `.txt` map files. Check file permissions.

-   **`[ERROR] Error initializing SpriteManager: [exception]`**:
    -   **Meaning:** There was a problem loading sprite sheets or their JSON definitions.
    -   **Action:** Ensure `Assets/Sprites` contains all `.png` and `.json` files, and they are not corrupted.

-   **`[ERROR] Database initialization failed: [exception]`**:
    -   **Meaning:** The application failed to create or connect to the `profiles.db` SQLite database.
    -   **Action:** Check file permissions in the `AppData/PacmanGame` directory. Ensure no other process is locking the database file.

-   **`[WARNING] Sound effect file not found: [filename]`**:
    -   **Meaning:** A specific sound effect `.wav` file was requested but not found. The game will continue without that sound.
    -   **Action:** Verify the presence and correctness of `.wav` files in `Assets/Audio/SFX`.

-   **`[WARNING] Ghost pathfinding failed - using fallback random move`**:
    -   **Meaning:** The A\* pathfinding algorithm could not find a valid path for a ghost to its target. The ghost will resort to a simpler, often less intelligent, movement.
    -   **Action:** This might indicate an issue with the map layout, an unreachable target, or a bug in the pathfinding logic.

---

By utilizing the logging system, developers and users can gain valuable insights into the application's behavior and efficiently diagnose any issues that may arise.
