# Multiplayer Design Document

## 1. Overview

This document outlines the architecture and design of the multiplayer system for the Pac-Man game. The system uses a centralized relay server to facilitate real-time gameplay for up to 5 players and 5 spectators per room.

## 2. Architecture

The multiplayer architecture is based on a client-server model with a relay server.

- **Clients**: The existing `PacmanGame` Avalonia application, with a new `NetworkService` to handle communication.
- **Server**: A new .NET 9.0 console application, `PacmanGame.Server`, acting as an authoritative relay.

This design was chosen to overcome network address translation (NAT) issues, prevent cheating, and centralize game logic.

## 3. Networking

- **Transport Protocol**: UDP is used for its low latency, making it suitable for real-time games.
- **Library**: `LiteNetLib` is a lightweight and reliable UDP networking library for .NET.
- **Update Rate**:
    - **Client to Server**: Player inputs are sent at 60 FPS.
    - **Server to Client**: Game state is broadcast at 20 FPS to conserve bandwidth.
    - **Events**: Critical game events are sent immediately.

## 4. Data Serialization

- **Format**: `MessagePack` is used for serializing network messages. It is a binary format that is faster and more compact than JSON.
- **Shared Contracts**: A `PacmanGame.Shared` class library contains all network message definitions, ensuring both client and server are in sync.

## 5. Room and Player Management

- **Room Types**:
    - **Public**: Discoverable by all players.
    - **Private**: Hidden and require a password to join.
- **Player Roles**:
    - **Pac-Man**: The protagonist.
    - **Ghosts (Blinky, Pinky, Inky, Clyde)**: The antagonists.
    - **Spectator**: A passive observer.
- **Room Administration**: The creator of a room is the admin, with powers to assign roles, start the game, and manage the room.
- **Persistence**: Room and player session data is stored in a local SQLite database on the server to allow for recovery after a server restart.

## 6. Gameplay Synchronization

- **Authoritative Server**: The server is the single source of truth for the game state. All player actions are validated by the server.
- **State Synchronization**: The server periodically broadcasts the complete game state to all clients.
- **Event-Based Updates**: For critical actions (e.g., eating a pellet), the server sends immediate event messages to ensure all clients are updated instantly.
- **Client-Side Prediction**: To be considered for future improvement. For this version, clients will render the state as received from the server.

## 7. Server-Side Logic

- **Game Simulation**: The server runs its own instance of the game logic, processing inputs and updating the game state.
- **Collision Detection**: All collision detection is handled by the server.
- **Rule Enforcement**: The server enforces all game rules, such as ghosts not being able to collect dots.

## 8. Client-Side Implementation

- **Network Service**: A dedicated service in the client application manages the connection to the server and the sending/receiving of messages.
- **UI**: New views are added for multiplayer menus, room creation, lobbies, and the game itself.
- **Rendering**: The client renders the game based on the state received from the server.

## 9. Deployment

The relay server is designed to be deployed on a cloud VM, with **AWS EC2 Free Tier** being the primary target. A systemd service ensures the server runs continuously and restarts on failure.
