using Microsoft.EntityFrameworkCore;
using VoiceBattle.Database.Models;

namespace VoiceBattle.Database;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<GameRecord> GameRecords { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // 数据库文件保存在程序运行目录
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voicebattle.db");
        options.UseSqlite($"Data Source={dbPath}");
    }
}