namespace VoiceBattle.Database.Models;

public class GameRecord
{
    public int Id { get; set; }
    public int WinnerId { get; set; }
    public int LoserId { get; set; }
    public string WinnerName { get; set; } = "";
    public string LoserName { get; set; } = "";
    public int WinnerScore { get; set; }
    public int LoserScore { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.Now;
}