using System.Text;
using System.Text.Json;

namespace VoiceBattle.Network;

// 消息类型枚举
public enum MessageType
{
    Login,          // 登录/加入房间
    Ready,          // 准备开始
    GameStart,      // 游戏开始
    EnergyUpdate,   // 能量值更新
    GameOver,       // 服务端广播最终结果
    Heartbeat,      // 心跳保活
    RematchRequest, // 请求再来一局
    RematchAccept,  // 同意再来一局
    RematchDecline, // 拒绝再来一局
    RematchStart,   // 服务端确认，双方同时开始
    Chat            // 聊天（可选）
}

// 统一消息结构
public class GameMessage
{
    public MessageType Type { get; set; }
    public string Payload { get; set; } = "";
    public string SenderName { get; set; } = "";
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

// 能量更新数据
public class EnergyPayload
{
    public float Energy { get; set; }   // 0~100 力度
}

// 游戏结束数据（由服务端统一广播）
public class GameOverPayload
{
    public string Winner { get; set; } = "";
    public float BarPosition { get; set; }
}

// 协议工具类：序列化/反序列化 + 带长度前缀的帧
public static class Protocol
{
    public static byte[] Encode(GameMessage msg)
    {
        string json = JsonSerializer.Serialize(msg);
        byte[] data = Encoding.UTF8.GetBytes(json);
        // 4字节长度前缀 + 数据
        byte[] frame = new byte[4 + data.Length];
        BitConverter.GetBytes(data.Length).CopyTo(frame, 0);
        data.CopyTo(frame, 4);
        return frame;
    }

    public static GameMessage? Decode(byte[] data)
    {
        string json = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<GameMessage>(json);
    }
}