# Tic-Tac-Toe API

Real-time multiplayer tic-tac-toe backend built with **ASP.NET Core** and **SignalR**. Two players connect via short shareable room codes, and every move is broadcast to the room in real time over WebSockets.

Frontend repo: [tic-tac-toe](https://github.com/kowsiganarunachalam/tic-tac-toe) (Next.js + React)

## How it works

1. Player 1 calls `CreateRoom` on the SignalR hub → a 6-character room code is generated and the connection joins a SignalR group.
2. Player 2 calls `JoinRoom` with the code → both clients receive a `PlayerJoined` event and the game starts.
3. Moves are sent through `MakeMove(roomId, row, col, player)` → the hub validates the move, checks win/draw conditions, and broadcasts the updated board to the group.
4. Disconnects are handled in `OnDisconnectedAsync` so the remaining player is notified.

Game state is held in a `ConcurrentDictionary` keyed by room code, with players and results persisted to SQL Server through a lightweight DAO layer.

## Tech stack

- ASP.NET Core (.NET 8) Web API
- SignalR for real-time bidirectional communication
- SQL Server for persistence (players, game results)
- Swagger / OpenAPI for API documentation
- CORS configured for the Next.js dev origin

## Endpoints

| Type | Route / Method | Purpose |
|------|----------------|---------|
| SignalR hub | `/gamehub` | `CreateRoom`, `JoinRoom`, `MakeMove` + server events (`RoomCreated`, `PlayerJoined`, board updates) |
| REST | `POST /api/game/create-room` | Create a room (cookie-based player identity) |
| REST | `POST /api/game/join-room/{roomId}` | Join an existing room |
| REST | `POST /api/game/move` | Submit a move |
| Swagger | `/swagger` | Interactive API docs (Development only) |

## Running locally

Prerequisites: .NET 8 SDK, SQL Server (LocalDB or full instance).

1. Clone the repo:
   ```bash
   git clone https://github.com/kowsiganarunachalam/tic-tac-toe-api.git
   cd tic-tac-toe-api
   ```
2. Add your connection string to `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TicTacToe;Trusted_Connection=True;"
     }
   }
   ```
3. Run the API:
   ```bash
   dotnet run
   ```
   The API starts on `http://localhost:5054` (see `Properties/launchSettings.json`).
4. Start the [frontend](https://github.com/kowsiganarunachalam/tic-tac-toe) on `http://localhost:3000` and play.

## Project structure

```
├── Program.cs          # Host setup: SignalR, CORS, Swagger, session
├── GameHub.cs          # SignalR hub: rooms, moves, win detection, disconnect handling
├── GameController.cs   # REST endpoints for room lifecycle
├── Model/              # Game domain model
└── Persistence/        # DAO layer for SQL Server
```
