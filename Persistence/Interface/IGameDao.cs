
namespace tic_tac_toe_api.Persistence.Interface
{
    
    public interface IGameDao
    {
        public Task<long> InsertPlayer(string guid,string name);

        public Task<long> InsertIntoRoom(long player,long roomId);
        public Task<long> InsertRoom(long player,string roomId);
        public Task<long> InsertMatchDetails(long roomId, long winner);
    }
}