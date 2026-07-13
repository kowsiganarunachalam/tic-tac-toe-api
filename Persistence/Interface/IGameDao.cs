namespace tic_tac_toe_api.Persistence.Interface
{
    public interface IGameDao
    {
        Task<long> InsertPlayer(string guid, string name);

        Task<int> InsertIntoRoom(long player, long roomId);

        Task<long> InsertRoom(long player, string roomId);

        Task<long> InsertMatchDetails(long roomId, long winner);
    }
}