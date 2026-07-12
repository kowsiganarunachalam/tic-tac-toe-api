using tic_tac_toe_api.Persistence.Interface;
using System.Data;
using Npgsql;
using Dapper;

namespace tic_tac_toe_api.Persistence.Implementation;

public class GameDao(string connectionString) : IGameDao
{
    private readonly string _connectionString = connectionString;

    private NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
    public async Task<long> InsertRoom(long player,string roomId)
    {
        var sql = @"
        INSERT INTO rooms (room_code,created_by) 
        VALUES (@RoomCode,@CreatedBy) 
        RETURNING id;";

        using var conn = GetConnection();
        conn.Open();

        //await conn.ExecuteAsync(sql, new { RoomId = roomId, PlayerId = player });
        return await conn.ExecuteScalarAsync<long>(sql, new
        {
            RoomCode = roomId,
            CreatedBy = player
        });
    }
    public async Task<long> InsertPlayer(string guid,string name)
    {
        const string sql = @"
            INSERT INTO players (name,playername,created_on) 
            VALUES (@Name,@PlayerName, now()) 
            RETURNING id;";

        using var conn = GetConnection();
        conn.Open();
        return await conn.ExecuteScalarAsync<long>(sql, new { Name = guid,PlayerName=name });
    }

    public async Task<long> InsertMatchDetails(long roomId, long winner)
    {
        const string sql = @"
            INSERT INTO matches (room_id, winner, created_on, created_by)
            VALUES (@RoomId, @Winner, now(), @CreatedBy)
            RETURNING id;";

        using var conn = GetConnection();
        conn.Open();

        var id = await conn.ExecuteScalarAsync<long>(sql, new
        {
            RoomId = roomId,
            Winner = winner,
            CreatedBy = winner
        });
        conn.Close();
        return id;
    }

    public async Task<long> InsertIntoRoom(long player, long roomId)
    {
        var sql = @"
        INSERT INTO room_players(room_id, player_id) VALUES (@RoomId, @PlayerId)
                    ON CONFLICT DO NOTHING;";

        using var conn = GetConnection();
        conn.Open();
        return await conn.ExecuteScalarAsync<long>(sql,  new { RoomId = roomId, PlayerId = player });
    }
}