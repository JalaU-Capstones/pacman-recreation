# Creative Mode

Creative Mode is an in-game level editor for Pac-Man Recreation. It lets you design custom levels, tune gameplay settings, and share projects as `.pacproj` files.

## Unlock Requirement

- Creative Mode is intended for players who have completed all 3 normal levels.
- Global features (like Global Top 10 score submission) also require completing all 3 levels.

## Opening Creative Mode

1. Open the in-game console with `Ctrl+C`.
2. Run: `/active creative`

## Editor Layout

- Left: keyboard shortcut reference.
- Center: 28x31 grid canvas with zoom controls.
- Right: toolbox (Tools tab) and configuration (Config tab), plus actions.

## Tools

- Walls: block, line, corner
- Ghost House: places a required 7x5 structure with a `-` gate
- Power Pellet: place at least 4 per level
- Dots: auto-generated for reachable empty cells during export/play test

## Keyboard Shortcuts

- Arrow keys: move cursor
- `Enter`: place selected tool
- `Delete`: clear cell (clearing inside the ghost house removes the whole house)
- `Tab`: cycle tools
- `R`: rotate selected wall piece
- `Ctrl+P`: play test
- `Ctrl+E`: export

## Configuration

The Config tab supports:

- Global settings: lives, win score, level count (1-10)
- Per-level settings:
  - Pac-Man speed multiplier
  - Ghost speed multiplier
  - Frightened duration (power pellet duration)
  - Fruit points
  - Ghost eat base points

## Multi-Level Projects

- Increase **Number of Levels** to create a multi-level project.
- Use **Prev Level / Next Level** to edit each level independently.
- Export writes `level1.txt`..`levelN.txt` plus `project.json` into a `.pacproj`.
- Play test and imported projects can run as multi-level sessions in `GameView`.

## Export / Import

### Export

1. Click **Export**.
2. Choose project name and whether it is editable after import.
3. Pick a save location for the `.pacproj`.

Export validates each level (spawn, ghost house, pellets, dots) and aborts if any level is invalid.

### Import

1. Click **Import**.
2. Select a `.pacproj`.
3. Review the preview (name/author/levels/editable).
4. Choose **Play** or **Edit** (Edit is disabled for play-only projects).

## File Format (`.pacproj`)

`.pacproj` is a ZIP archive containing:

- `project.json`: metadata + global/per-level configuration
- `level1.txt` .. `levelN.txt`: map layouts (28 columns x 31 rows)

Map legend:

- `#` wall
- `.` small dot
- `o` / `O` power pellet
- `P` Pac-Man spawn
- `G` ghost spawn/interior markers (editor)
- `-` ghost house gate (ghost door)
- space: empty

