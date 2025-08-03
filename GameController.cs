using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using tic_tac_toe_api.Models;
using tic_tac_toe_api;
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
            var roomId = Guid.NewGuid().ToString()[..6];
            var playerId = Guid.NewGuid().ToString(); // Generate player ID

            var game = new Game
            {
                RoomId = roomId,
                Player1 = playerId
            };
            ActiveGames[roomId] = game;
            Response.Cookies.Append("player-id", playerId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Required for SameSite=None (use HTTPS)
                SameSite = SameSiteMode.None, // Required for cross-origin
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            return Ok(new { roomId });
        }

        [HttpPost("join-room/{roomId}")]
        public IActionResult JoinRoom(string roomId)
        {
            if (!ActiveGames.TryGetValue(roomId, out var game))
                return NotFound("Room not found.");

            if (!string.IsNullOrEmpty(game.Player2))
                return BadRequest("Room already has two players.");

            game.Player2 = Guid.NewGuid().ToString(); // Simulate second player ID

            Response.Cookies.Append("player-id", game.Player2, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });
            return Ok(new { message = "Joined", roomId });
        }

        [HttpPost("move")]
        public async Task<IActionResult> MakeMove([FromBody] MoveDto move)
        {
            if (!ActiveGames.TryGetValue(move.RoomId, out var game))
                return NotFound("Room not found.");

            if (game.IsGameOver)
                return BadRequest("Game is already over.");

            if (game.Board[move.Row, move.Col] != null)
                return BadRequest("Invalid move.");

            game.Board[move.Row, move.Col] = game.CurrentTurn;

            if (CheckWinner(game.Board, game.CurrentTurn))
            {
                game.IsGameOver = true;
                await _hubContext.Clients.Group(move.RoomId)
                    .SendAsync("GameOver", game.CurrentTurn);
            }
            else
            {
                game.CurrentTurn = game.CurrentTurn == "X" ? "O" : "X";
                await _hubContext.Clients.Group(move.RoomId)
                    .SendAsync("ReceiveMove", move.Player, move.Row, move.Col, game.CurrentTurn);
            }

            return Ok();
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

    public class MoveDto
    {
        public string RoomId { get; set; } = string.Empty;
        public string Player { get; set; } = string.Empty;
        public int Row { get; set; }
        public int Col { get; set; }
    }
}
