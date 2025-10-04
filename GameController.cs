using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using tic_tac_toe_api.Models;
using System.Collections.Concurrent;

namespace tic_tac_toe_api
{
    [ApiController]
    [Route("api/game")]
    public class GameController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, Game> ActiveGames = new();

        private readonly IHubContext<GameHub> _hubContext;

        public GameController(IHubContext<GameHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost("create-room")]
        public IActionResult CreateRoom()
        {
            var roomId = Guid.NewGuid().ToString("N")[..6];
            var playerId = Guid.NewGuid().ToString();

            var game = new Game
            {
                RoomId = roomId,
                Player1 = playerId,
                Board = new string[3, 3],
                CurrentTurn = "X",
                IsGameOver = false
            };
            ActiveGames[roomId] = game;

            Response.Cookies.Append("player-id", playerId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            return Ok(new { roomId });
        }

        [HttpPost("join-room/{roomId}")]
        public async Task<IActionResult> JoinRoomAsync(string roomId)
        {
            if (!ActiveGames.TryGetValue(roomId, out var game))
                return NotFound("Room not found.");

            if (!string.IsNullOrEmpty(game.Player2))
                return BadRequest("Room already has two players.");

            var player2Id = Guid.NewGuid().ToString();
            game.Player2 = player2Id;

            Response.Cookies.Append("player-id", player2Id, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            await _hubContext.Clients.Group(roomId)
                    .SendAsync("PlayerJoined");
            return Ok(new { message = "Joined", roomId });
        }

        [HttpPost("move")]
        public async Task<IActionResult> MakeMove([FromBody] MoveDto move)
        {
            if (!ActiveGames.TryGetValue(move.RoomId, out var game))
                return NotFound("Room not found.");

            if (game.IsGameOver)
                return BadRequest("Game is already over.");

            // Validate player identity via cookie
            var playerIdFromCookie = Request.Cookies["player-id"];
            if (string.IsNullOrEmpty(playerIdFromCookie) || 
                (playerIdFromCookie != game.Player1 && playerIdFromCookie != game.Player2))
            {
                return Unauthorized("Invalid player.");
            }

            // Verify player turn matches
            string expectedPlayerSymbol = playerIdFromCookie == game.Player1 ? "X" : "O";
            if (expectedPlayerSymbol != game.CurrentTurn)
            {
                return BadRequest("Not your turn.");
            }

            // Validate move position
            if (move.Row < 0 || move.Row > 2 || move.Col < 0 || move.Col > 2)
                return BadRequest("Invalid position.");

            if (game.Board[move.Row, move.Col] != null)
                return BadRequest("Invalid move. Cell already taken.");

            game.Board[move.Row, move.Col] = expectedPlayerSymbol;

            if (CheckWinner(game.Board, expectedPlayerSymbol))
            {
                game.IsGameOver = true;
                await _hubContext.Clients.Group(move.RoomId)
                    .SendAsync("GameOver", new {
                        player = expectedPlayerSymbol,
                        row = move.Row,
                        col = move.Col,
                        nextTurn = game.CurrentTurn
                    });
            }
            else
            {
                game.CurrentTurn = game.CurrentTurn == "X" ? "O" : "X";

                await _hubContext.Clients.Group(move.RoomId)
                    .SendAsync("ReceiveMove", new {
                        player = expectedPlayerSymbol,
                        row = move.Row,
                        col = move.Col,
                        nextTurn = game.CurrentTurn
                    });
            }

            return Ok(new { grid = game.Board });
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

        // Optional: cleanup finished games every hour or so
        // Could be run via a hosted service or timer in production
        private static void CleanupFinishedGames()
        {
            var finishedRooms = ActiveGames
                .Where(kvp => kvp.Value.IsGameOver)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var roomId in finishedRooms)
            {
                ActiveGames.TryRemove(roomId, out _);
            }
        }
    }

    public class MoveDto
    {
        public string RoomId { get; set; } = string.Empty;
        public string Player { get; set; } = string.Empty; // Ignored now in server logic
        public int Row { get; set; }
        public int Col { get; set; }
    }
}
