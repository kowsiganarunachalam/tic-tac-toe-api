namespace tic_tac_toe_api.Models
{
    public class Game
    {
        public string RoomId { get; set; } = string.Empty;
        public string? Player1 { get; set; }
        public string? Player1Name { get; set; }
        public string? Player2 { get; set; }
        public string? Player2Name { get; set; }

        public string[,] Board { get; set; } = new string[3, 3];
        public string CurrentTurn { get; set; } = "X";
        public bool IsGameOver { get; set; }

        // ⬇️ ADD THESE
        public DateTime TurnStartedAt { get; set; }
        public TimeSpan TurnDuration { get; set; } = TimeSpan.FromSeconds(8);
        public bool AutoMoveDone { get; set; }
    }
}
