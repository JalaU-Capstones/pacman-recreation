# Multiplayer Game Flow Design

This document outlines the network communication and UI flow for the multiplayer mode in the Pac-Man Educational Recreation project.

**Last Updated:** February 11, 2026

---

## Table of Contents

1. [Technical Overview](#technical-overview)
2. [Network Message Definitions](#network-message-definitions)
3. [Client-Side Flow](#client-side-flow)
    - [Connecting to Server](#connecting-to-server)
    - [Creating a Room](#creating-a-room)
    - [Joining a Room](#joining-a-room)
    - [Room Lobby Interaction](#room-lobby-interaction)
    - [Gameplay](#gameplay)
4. [Server-Side Flow](#server-side-flow)
    - [Player Connection](#player-connection)
    - [Room Management](#room-management)
    - [Game Simulation](#game-simulation)
5. [Room Lobby Interface](#room-lobby-interface)
6. [Joining Private Rooms](#joining-private-rooms)

---

## Technical Overview

- **Networking Library:** LiteNetLib
- **Serialization:** MessagePack
- **Architecture:** Client-Server (authoritative server)
- **Transport:** UDP with reliable ordered delivery for critical messages.

---

## Network Message Definitions

All network messages inherit from `NetworkMessageBase` and are identified by a `MessageType` enum.

- `CreateRoomRequest` / `CreateRoomResponse`
- `JoinRoomRequest` / `JoinRoomResponse`
- `AssignRoleRequest` / `RoleAssignedEvent`
- `StartGameRequest` / `GameStartEvent`
- `PlayerInputMessage` (Client -> Server)
- `GameStateMessage` (Server -> Client)
- `GameEndEvent` (Server -> Client)
- `PlayerJoinedEvent` / `PlayerLeftEvent` (Server -> Client)

---

## Client-Side Flow

### Connecting to Server
1. On application startup, `NetworkService` is initialized as a singleton.
2. When the user navigates to the multiplayer menu, `NetworkService.Start()` is called.
3. The client connects to the server IP and port defined in `Constants.cs`.
4. The `OnPeerConnected` event confirms the connection.

### Creating a Room
1. User fills out the form in `CreateRoomView` (Room Name, Visibility, Password).
2. `CreateRoomViewModel` constructs a `CreateRoomRequest` message.
3. `NetworkService.SendCreateRoomRequest()` serializes and sends the message.
4. The client awaits a `CreateRoomResponse`.
5. On `CreateRoomResponse(Success=true)`:
    - `NetworkService` invokes the `OnRoomCreated` event.
    - `CreateRoomViewModel` handles the event and calls `MainWindowViewModel.NavigateToRoomLobby()`.
    - The user is navigated to `RoomLobbyView`.
6. On `CreateRoomResponse(Success=false)`:
    - An error message is displayed to the user.

### Joining a Room
- **Public Room:**
    1. User browses the `RoomListView`.
    2. Clicking "Join" on a room sends a `JoinRoomRequest` with the room name.
- **Private Room:**
    1. User clicks "Join Private Room" to reveal a form.
    2. User enters the room name and password.
    3. Clicking "Join" sends a `JoinRoomRequest` with the room name and password.
4. On `JoinRoomResponse(Success=true)`:
    - The client navigates to the `RoomLobbyView`.
5. On `JoinRoomResponse(Success=false)`:
    - An error message is displayed (e.g., "Incorrect password", "Room full").

### Room Lobby Interaction
1. `RoomLobbyViewModel` receives room state updates from the server.
2. **If Admin:**
    - Can assign roles to players via dropdowns.
    - Each role change sends an `AssignRoleRequest` to the server.
    - Can click "Start Game" once at least one role is assigned. This sends a `StartGameRequest`.
3. **If Player:**
    - Sees their assigned role as a read-only label.
    - Waits for the admin to start the game.
4. All players receive `PlayerJoinedEvent`, `PlayerLeftEvent`, and `RoleAssignedEvent` broadcasts to keep the UI in sync.

### Gameplay
1. On `GameStartEvent`, the client navigates to `MultiplayerGameView`.
2. The client sends `PlayerInputMessage` (current direction) to the server at a fixed interval (e.g., 60 FPS).
3. The client receives `GameStateMessage` from the server at a lower interval (e.g., 20 FPS).
4. The client's `MultiplayerGameViewModel` updates entity positions and game state based on the received data, interpolating for smooth rendering.

---

## Server-Side Flow

### Player Connection
1. `RelayServer` listens for incoming connections.
2. On `OnPeerConnected`, a new `Player` object is created and associated with the `NetPeer`.
3. On `OnPeerDisconnected`, the player is removed from any room they were in, and a `PlayerLeftEvent` is broadcast to other room members.

### Room Management
1. `RoomManager` handles the creation and tracking of rooms.
2. On `CreateRoomRequest`:
    - A new `Room` is created with a unique ID.
    - The requesting player is added as the admin.
    - A `CreateRoomResponse` is sent back to the creator.
3. On `JoinRoomRequest`:
    - The server validates the room's existence, password (if private), and capacity.
    - If successful, the player is added to the room, and a `JoinRoomResponse` is sent back.
    - A `PlayerJoinedEvent` is broadcast to all other players in the room.
4. On `AssignRoleRequest`:
    - The server validates that the role is not already taken.
    - The player's role is updated.
    - A `RoleAssignedEvent` is broadcast to all players in the room.

### Game Simulation
1. On `StartGameRequest` from the admin, the server:
    - Changes the room state to `Playing`.
    - Broadcasts a `GameStartEvent` to all players in the room.
    - Starts the `GameSimulation` loop for that room.
2. The simulation loop runs at a fixed tick rate (e.g., 20 FPS).
3. It processes a queue of `PlayerInputMessage` from all clients in the room.
4. It updates the game state (positions, score, lives).
5. It broadcasts the new `GameStateMessage` to all clients.
6. When a win/loss condition is met, it broadcasts a `GameEndEvent` and stops the simulation.

---

## Room Lobby Interface

### Room Lobby

The lobby is where players gather before the game starts. The admin (room creator) assigns roles to players.

**Layout:**
- Header: Room name, visibility (public/private), room ID
- Left panel (60%): Player list with role assignment
- Right panel (40%): Game rules and spectator count
- Bottom bar: Start Game (admin only) and Leave Room buttons

**Player List:**
- Each entry shows player name, assigned role, and admin/you badges
- Admin view: Dropdowns to assign roles, Kick buttons
- Player view: Read-only role labels

**Role Assignment Rules:**
- 5 roles available: Pac-Man, Blinky, Pinky, Inky, Clyde
- No duplicates (each role can be assigned to only 1 player)
- Game can start with 1+ roles assigned (doesn't require all 5)
- Unassigned players cannot participate but can spectate

**Controls:**
- Start Game: Visible only to admin, enabled when ≥1 role assigned
- Leave Room: Available to all players

---

## Joining Private Rooms

### Private Room Access

Private rooms do not appear in the public room list. To join:

1. Click "Join Private Room" in Room List view
2. Enter the exact room name
3. Enter the room password
4. Click "Join"

**Validation:**
- Room name must match exactly (case-sensitive)
- Password must match
- Room must have space available (< 5 players)
- Player name must be unique in the room

**Error Handling:**
- "Room not found" → Room name is incorrect or room was closed
- "Incorrect password" → Password doesn't match
- "Room is full" → Cannot join as player, can join as spectator
- "Name already taken" → Another player in the room has the same name
