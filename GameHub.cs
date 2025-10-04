using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using tic_tac_toe_api.Models;
using Newtonsoft.Json;
using tic_tac_toe_api.Persistence.Implementation;

namespace tic_tac_toe_api
{
    public class GameHub(IHttpContextAccessor httpContextAccessor, IConfiguration configuration) : Hub
    {
        private static ConcurrentDictionary<string, Game> Rooms = new();
        private static ConcurrentDictionary<string, long> _dbIds = new();
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        //private static readonly IConfiguration _configuration = configuration;
        private const string LogFolder = "errorlog";
        private readonly GameDao gameDao = new(configuration.GetConnectionString("DefaultConnection")??""); 
        private async Task LogErrorAsync(Exception ex, string methodName)
        {
            try
            {
                if (!Directory.Exists(LogFolder))
                {
                    Directory.CreateDirectory(LogFolder);
                }

                string logFilePath = Path.Combine(LogFolder, $"error_{DateTime.UtcNow:yyyyMMdd}.log");

                string logEntry = new StringBuilder()
                    .AppendLine($"[{DateTime.UtcNow:O}] Error in {methodName}:")
                    .AppendLine(ex.ToString())
                    .AppendLine("------------------------------------------------")
                    .ToString();

                await File.AppendAllTextAsync(logFilePath, logEntry);
            }
            catch
            {
                // If logging fails, swallow to avoid crashing the app
            }
        }

        public async Task<string> CreateRoom(string playername)
        {
            try
            {
                var roomId = Guid.NewGuid().ToString()[..6]; // short room code
                Rooms[roomId] = new Game() { Player1 = Context.ConnectionId,Player1Name=playername };

                
                var playerOneDB = await gameDao.InsertPlayer(Context.ConnectionId,playername);
                _dbIds[Context.ConnectionId] = playerOneDB;
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Caller.SendAsync("RoomCreated", roomId,playername);
                return roomId;
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, nameof(CreateRoom));
                throw; // Re-throw so client can handle error if needed
            }
        }

        public async Task<bool> JoinRoom(string roomId,string playername)
        {
            try
            {
                if (Rooms.TryGetValue(roomId, out var game))
                {
                    if (!string.IsNullOrEmpty(game.Player2) && !string.IsNullOrEmpty(game.Player2))
                    {
                        await Clients.Caller.SendAsync("RoomFull");
                        return false;
                    }

                    game.Player2 = Context.ConnectionId;
                    game.Player2Name=playername;
                    await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                    
                    await Clients.Group(roomId).SendAsync("StartGame", roomId,game.Player1Name,game.Player2Name,JsonConvert.SerializeObject(game));
                    var playerTwoDB = await gameDao.InsertPlayer(Context.ConnectionId,playername);
                    _dbIds[Context.ConnectionId] = playerTwoDB;
                    var roomIdDb = await gameDao.InsertRoom(_dbIds[game.Player1],roomId);
                    _dbIds["roomIdDb"]=roomIdDb;
                    _ = await gameDao.InsertIntoRoom(_dbIds[game.Player1],roomIdDb);
                    _ = await gameDao.InsertIntoRoom(_dbIds[game.Player2],roomIdDb);
                    return true;
                }

                await Clients.Caller.SendAsync("RoomNotFound");
                return false;
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, nameof(JoinRoom));
                throw;
            }
        }

        public async Task MakeMove(string roomId, int row, int col, string player)
        {
            try
            {
                if (!Rooms.TryGetValue(roomId, out var game))
                {
                    await Clients.Caller.SendAsync("Alert","Error", "Room not found.");
                    return;
                }
                else
                {
                    if (string.IsNullOrEmpty(game.Player2))
                    {
                        await Clients.Caller.SendAsync("Alert","Info", "Player 2 needs to join.");
                        return;
                    }
                }

                if (game.IsGameOver)
                {
                    await Clients.Caller.SendAsync("Alert","Info", "Game is already over.");
                    return;
                }

                // Determine player symbol based on connection ID
                string expectedPlayerSymbol = Context.ConnectionId == game.Player1 ? "X" : "O";

                if (expectedPlayerSymbol != game.CurrentTurn)
                {
                    await Clients.Caller.SendAsync("Alert","Warning", "Not your turn.");
                    return;
                }

                // Validate move bounds
                if (row < 0 || row > 2 || col < 0 || col > 2)
                {
                    await Clients.Caller.SendAsync("Alert","Warning", "Invalid position.");
                    return;
                }

                if (game.Board[row, col] != null)
                {
                    await Clients.Caller.SendAsync("Alert","Warning", "Invalid move. Cell already taken.");
                    return;
                }

                // Make the move
                game.Board[row, col] = expectedPlayerSymbol;

                // Check for winner
                if (CheckWinner(game.Board, expectedPlayerSymbol))
                {
                    game.IsGameOver = true;

                    await Clients.Group(roomId).SendAsync("GameOver", new
                    {
                        player = expectedPlayerSymbol,
                        row,
                        col,
                        nextTurn = game.CurrentTurn
                    });
                    _ = gameDao.InsertMatchDetails(_dbIds["roomIdDb"],_dbIds[Context.ConnectionId]);
                    
                }
                else
                {
                    // Switch turns
                    game.CurrentTurn = game.CurrentTurn == "X" ? "O" : "X";

                    await Clients.Group(roomId).SendAsync("ReceiveMove", new
                    {
                        player = expectedPlayerSymbol,
                        row,
                        col,
                        nextTurn = game.CurrentTurn
                    });
                }
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, nameof(MakeMove));
                await Clients.Caller.SendAsync("Error", "An unexpected error occurred.");
            }
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                foreach (var room in Rooms)
                {
                    if (room.Value.Player1 == Context.ConnectionId || room.Value.Player2 == Context.ConnectionId)
                    {
                        if (room.Value.Player1 == Context.ConnectionId)
                        { room.Value.Player1 = null; }
                        else { room.Value.Player2 = null; }
                        await Clients.Group(room.Key).SendAsync("PlayerLeft", Context.ConnectionId);

                        if (room.Value.Player1 == null && room.Value.Player2 == null)
                            Rooms.TryRemove(room.Key, out _);
                    }
                }
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, nameof(OnDisconnectedAsync));
            }

            await base.OnDisconnectedAsync(exception);
        }

        private static bool CheckWinner(string[,] board, string symbol)
        {
            for (int i = 0; i < 3; i++)
            {
                if ((board[i, 0] == symbol && board[i, 1] == symbol && board[i, 2] == symbol) ||
                    (board[0, i] == symbol && board[1, i] == symbol && board[2, i] == symbol))
                    return true;
            }

            return (board[0, 0] == symbol && board[1, 1] == symbol && board[2, 2] == symbol) ||
                   (board[0, 2] == symbol && board[1, 1] == symbol && board[2, 0] == symbol);
        }
    }
}
