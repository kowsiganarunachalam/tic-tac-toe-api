using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace tic_tac_toe_api
{
    public class GameHub : Hub
    {
        // This is a simple in-memory room store (not persisted or thread-safe for production)
        private static ConcurrentDictionary<string, List<string>> Rooms = new();

        // Called by the first player
        public async Task<string> CreateRoom()
        {
            var roomId = Guid.NewGuid().ToString()[..6]; // short room code
            Rooms[roomId] = [Context.ConnectionId];

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("RoomCreated", roomId);
            return roomId;
        }

        // Called by the second player
        public async Task<bool> JoinRoom(string roomId)
        {
            if (Rooms.TryGetValue(roomId, out var players))
            {
                if (players.Count >= 2)
                {
                    await Clients.Caller.SendAsync("RoomFull");
                    return false;
                }

                players.Add(Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

                // Notify both players that the game is starting
                await Clients.Group(roomId).SendAsync("StartGame", roomId, players);

                return true;
            }

            await Clients.Caller.SendAsync("RoomNotFound");
            return false;
        }

        public async Task MakeMove(string roomId, int row, int col, string player)
        {
            await Clients.Group(roomId).SendAsync("ReceiveMove", player, row, col);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            foreach (var room in Rooms)
            {
                if (room.Value.Contains(Context.ConnectionId))
                {
                    room.Value.Remove(Context.ConnectionId);
                    await Clients.Group(room.Key).SendAsync("PlayerLeft", Context.ConnectionId);

                    if (room.Value.Count == 0)
                        Rooms.TryRemove(room.Key, out _);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
