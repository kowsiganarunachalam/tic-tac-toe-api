namespace tic_tac_toe_api.Models
{
    public class Game
    {
        public string RoomId { get; set; } = string.Empty;
        public string? Player1 { get; set; }
        public string? Player2 { get; set; }
        public string[,] Board { get; set; } = new string[3, 3];
        public string CurrentTurn { get; set; } = "X";
        public bool IsGameOver { get; set; } = false;
    }
}
