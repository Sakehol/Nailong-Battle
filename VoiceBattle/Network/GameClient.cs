using System.Net.Sockets;
using System.Text.Json;

namespace VoiceBattle.Network;

public class GameClient : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private string _playerName = "";

    public event Action? OnGameStart;
    public event Action<string, float>? OnEnergyReceived;   // (玩家名, 能量)
    public event Action<GameOverPayload>? OnGameOver;
    public event Action<string>? OnLog;

    // 再来一局事件
    public event Action? OnRematchRequest;
    public event Action? OnRematchDecline;
    public event Action? OnRematchStart;

    public bool IsConnected => _client?.Connected ?? false;

    public async Task<bool> ConnectAsync(string host, int port, string playerName)
    {
        try
        {
            _playerName = playerName;
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();

            // 发送登录消息
            await SendAsync(new GameMessage
            {
                Type = MessageType.Login,
                SenderName = playerName,
                Payload = playerName
            });

            // 启动接收循环
            _ = Task.Run(ReceiveLoop);
            // 启动心跳
            _ = Task.Run(HeartbeatLoop);

            OnLog?.Invoke($"已连接到 {host}:{port}");
            return true;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"连接失败：{ex.Message}");
            return false;
        }
    }

    private async Task ReceiveLoop()
    {
        if (_stream == null) return;
        try
        {
            while (_client?.Connected == true)
            {
                byte[] lenBuf = new byte[4];
                int read = await ReadExactAsync(_stream, lenBuf, 4);
                if (read < 4) break;

                int msgLen = BitConverter.ToInt32(lenBuf, 0);
                if (msgLen <= 0 || msgLen > 65536) break;

                byte[] msgBuf = new byte[msgLen];
                read = await ReadExactAsync(_stream, msgBuf, msgLen);
                if (read < msgLen) break;

                var msg = Protocol.Decode(msgBuf);
                if (msg == null) continue;

                switch (msg.Type)
                {
                    case MessageType.GameStart:
                        OnGameStart?.Invoke();
                        break;

                    case MessageType.EnergyUpdate:
                        var ep = JsonSerializer.Deserialize<EnergyPayload>(msg.Payload);
                        if (ep != null)
                            OnEnergyReceived?.Invoke(msg.SenderName, ep.Energy);
                        break;

                    case MessageType.GameOver:
                        var gop = JsonSerializer.Deserialize<GameOverPayload>(msg.Payload);
                        if (gop != null)
                            OnGameOver?.Invoke(gop);
                        break;

                    case MessageType.RematchRequest:
                        OnRematchRequest?.Invoke();
                        break;

                    case MessageType.RematchDecline:
                        OnRematchDecline?.Invoke();
                        break;

                    case MessageType.RematchStart:
                        OnRematchStart?.Invoke();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"接收断开：{ex.Message}");
        }
    }

    private async Task HeartbeatLoop()
    {
        while (_client?.Connected == true)
        {
            await Task.Delay(5000);
            try
            {
                await SendAsync(new GameMessage
                {
                    Type = MessageType.Heartbeat,
                    SenderName = _playerName
                });
            }
            catch { break; }
        }
    }

    public async Task SendEnergyAsync(float energy)
    {
        var payload = JsonSerializer.Serialize(new EnergyPayload { Energy = energy });
        await SendAsync(new GameMessage
        {
            Type = MessageType.EnergyUpdate,
            SenderName = _playerName,
            Payload = payload
        });
    }

    public async Task SendGameOverAsync(string winner, int winnerScore, int loserScore)
    {
        // 不再由客户端主动发送，保留方法留作备用
        var payload = JsonSerializer.Serialize(new GameOverPayload
        {
            Winner = winner,
            BarPosition = winnerScore
        });
        await SendAsync(new GameMessage
        {
            Type = MessageType.GameOver,
            SenderName = _playerName,
            Payload = payload
        });
    }

    public async Task SendRematchRequestAsync()
    {
        await SendAsync(new GameMessage
        {
            Type = MessageType.RematchRequest,
            SenderName = _playerName
        });
    }

    public async Task SendRematchAcceptAsync()
    {
        await SendAsync(new GameMessage
        {
            Type = MessageType.RematchAccept,
            SenderName = _playerName
        });
    }

    public async Task SendRematchDeclineAsync()
    {
        await SendAsync(new GameMessage
        {
            Type = MessageType.RematchDecline,
            SenderName = _playerName
        });
    }

    private async Task SendAsync(GameMessage msg)
    {
        if (_stream == null) return;
        byte[] data = Protocol.Encode(msg);
        await _stream.WriteAsync(data);
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
        _client?.Dispose();
    }
}