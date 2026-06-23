using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoiceBattle.Database;
using VoiceBattle.Database.Models;

namespace VoiceBattle.Network;

public class GameServer : IDisposable
{
    private TcpListener? _listener;
    private readonly List<TcpClient> _clients = new();
    private readonly List<string> _playerNames = new();
    private bool _gameStarted = false;
    private bool _gameOver = false;
    private readonly AppDbContext _db;

    // 服务端维护进度条（0~100，50为初始中心）
    private float _barPosition = 50f;
    private DateTime _gameStartTime;
    private System.Threading.Timer? _gameTimer;

    // 再来一局：记录谁发起了请求
    private string _rematchRequester = "";

    // 事件：通知 UI 层
    public event Action<string>? OnLog;
    public event Action<string>? OnPlayerJoined;
    public event Action<string>? OnGameOver;

    public GameServer(AppDbContext db)
    {
        _db = db;
    }

    public void Start(int port = 9527)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        OnLog?.Invoke($"服务器已启动，监听端口 {port}");
        Task.Run(AcceptLoop);
    }

    private async Task AcceptLoop()
    {
        while (_listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync();
                _clients.Add(client);
                OnLog?.Invoke($"新客户端连接：{client.Client.RemoteEndPoint}");
                _ = Task.Run(() => HandleClient(client));
            }
            catch { break; }
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        var stream = client.GetStream();
        string playerName = "";

        try
        {
            while (client.Connected)
            {
                // 读取4字节长度前缀
                byte[] lenBuf = new byte[4];
                int read = await ReadExactAsync(stream, lenBuf, 4);
                if (read < 4) break;

                int msgLen = BitConverter.ToInt32(lenBuf, 0);
                if (msgLen <= 0 || msgLen > 65536) break;

                byte[] msgBuf = new byte[msgLen];
                read = await ReadExactAsync(stream, msgBuf, msgLen);
                if (read < msgLen) break;

                var msg = Protocol.Decode(msgBuf);
                if (msg == null) continue;

                switch (msg.Type)
                {
                    case MessageType.Login:
                        playerName = msg.SenderName;
                        if (!_playerNames.Contains(playerName))
                            _playerNames.Add(playerName);
                        OnPlayerJoined?.Invoke(playerName);
                        OnLog?.Invoke($"玩家 {playerName} 加入房间");

                        // 2人到齐自动开始
                        if (_playerNames.Count >= 2 && !_gameStarted)
                        {
                            _gameStarted = true;
                            _gameOver = false;
                            _barPosition = 50f;
                            _gameStartTime = DateTime.Now;
                            StartGameTimer();

                            await BroadcastAsync(new GameMessage
                            {
                                Type = MessageType.GameStart,
                                Payload = "start"
                            });
                        }
                        break;

                    case MessageType.EnergyUpdate:
                        if (_gameOver) break;
                        var ep = JsonSerializer.Deserialize<EnergyPayload>(msg.Payload);
                        if (ep == null) break;

                        // 服务端更新进度条：第一个玩家（index 0）向右推，第二个向左推
                        if (msg.SenderName == _playerNames.ElementAtOrDefault(0))
                            _barPosition = Math.Clamp(_barPosition + ep.Energy * 8f, 0f, 100f);
                        else
                            _barPosition = Math.Clamp(_barPosition - ep.Energy * 8f, 0f, 100f);

                        // 转发给对手
                        await BroadcastExceptAsync(client, msg);

                        // 检查是否触顶
                        await CheckGameOver();
                        break;

                    case MessageType.GameOver:
                        // 由客户端发送已废弃，不处理
                        break;

                    case MessageType.RematchRequest:
                        _rematchRequester = msg.SenderName;
                        await BroadcastExceptAsync(client, msg);
                        break;

                    case MessageType.RematchAccept:
                        // 双方同意，重置游戏状态并广播重新开始
                        _gameOver = false;
                        _barPosition = 50f;
                        _gameStartTime = DateTime.Now;
                        StartGameTimer();
                        await BroadcastAsync(new GameMessage
                        {
                            Type = MessageType.RematchStart,
                            Payload = "start"
                        });
                        break;

                    case MessageType.RematchDecline:
                        await BroadcastExceptAsync(client, msg);
                        break;

                    case MessageType.Heartbeat:
                        await SendToAsync(client, new GameMessage { Type = MessageType.Heartbeat });
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"客户端断开：{ex.Message}");
        }
        finally
        {
            _clients.Remove(client);
            if (!string.IsNullOrEmpty(playerName))
                _playerNames.Remove(playerName);
            client.Dispose();
        }
    }

    private void StartGameTimer()
    {
        _gameTimer?.Dispose();
        // 60秒后服务端强制判定
        _gameTimer = new System.Threading.Timer(async _ =>
        {
            if (!_gameOver)
                await CheckGameOver(forceEnd: true);
        }, null, 60000, Timeout.Infinite);
    }

    private async Task CheckGameOver(bool forceEnd = false)
    {
        if (_gameOver) return;
        if (!forceEnd && _barPosition > 2f && _barPosition < 98f) return;

        _gameOver = true;
        _gameTimer?.Dispose();

        // 第一个玩家为"我方"（index 0），第二个为"对手"（index 1）
        string p0 = _playerNames.ElementAtOrDefault(0) ?? "";
        string p1 = _playerNames.ElementAtOrDefault(1) ?? "";
        string winner = _barPosition >= 50f ? p0 : p1;

        OnGameOver?.Invoke(winner);

        var payload = JsonSerializer.Serialize(new GameOverPayload
        {
            Winner = winner,
            BarPosition = _barPosition
        });

        await BroadcastAsync(new GameMessage
        {
            Type = MessageType.GameOver,
            Payload = payload
        });

        await SaveGameRecord(winner, p0, p1);
    }

    private async Task SaveGameRecord(string winner, string p0, string p1)
    {
        try
        {
            string loser = winner == p0 ? p1 : p0;
            var winnerUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == winner);
            var loserUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == loser);

            if (winnerUser != null) winnerUser.TotalWins++;
            if (loserUser != null) loserUser.TotalLosses++;

            _db.GameRecords.Add(new GameRecord
            {
                WinnerName = winner,
                LoserName = loser,
                WinnerId = winnerUser?.Id ?? 0,
                LoserId = loserUser?.Id ?? 0,
                WinnerScore = (int)_barPosition,
                LoserScore = (int)(100 - _barPosition)
            });

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"保存记录失败：{ex.Message}");
        }
    }

    public async Task BroadcastAsync(GameMessage msg)
    {
        byte[] data = Protocol.Encode(msg);
        foreach (var c in _clients.ToList())
        {
            try { await c.GetStream().WriteAsync(data); }
            catch { }
        }
    }

    private async Task BroadcastExceptAsync(TcpClient except, GameMessage msg)
    {
        byte[] data = Protocol.Encode(msg);
        foreach (var c in _clients.ToList())
        {
            if (c == except) continue;
            try { await c.GetStream().WriteAsync(data); }
            catch { }
        }
    }

    private async Task SendToAsync(TcpClient client, GameMessage msg)
    {
        byte[] data = Protocol.Encode(msg);
        try { await client.GetStream().WriteAsync(data); }
        catch { }
    }

    private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buf, int count)
    {
        int total = 0;
        while (total < count)
        {
            int n = await stream.ReadAsync(buf.AsMemory(total, count - total));
            if (n == 0) break;
            total += n;
        }
        return total;
    }

    public void Dispose()
    {
        _gameTimer?.Dispose();
        _listener?.Stop();
        foreach (var c in _clients) c.Dispose();
    }
}