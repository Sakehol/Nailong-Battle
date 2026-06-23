namespace VoiceBattle.Database.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}