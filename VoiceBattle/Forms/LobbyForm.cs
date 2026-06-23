using VoiceBattle.Network;
using VoiceBattle.Database;

namespace VoiceBattle.Forms;

public partial class LobbyForm : Form
{
    private readonly string _username;
    private readonly AppDbContext _db;
    private GameServer? _server;
    private GameClient? _client;
    private bool _isHost = false;

    public LobbyForm(string username)
    {
        InitializeComponent();
        _username = username;
        _db = new AppDbContext();
        lblWelcome.Text = $"欢迎，{username}！";
    }

    // 安全触发异步任务，消除空引用和未等待警告
    private void FireAndForget(Task? task)
    {
        if (task == null) return;
        task.ContinueWith(t =>
        {
            if (t.IsFaulted)
                System.Diagnostics.Debug.WriteLine($"任务失败：{t.Exception?.Message}");
        }, TaskScheduler.Default);
    }

    // 创建房间（本机作为服务端）
    private void btnCreateRoom_Click(object sender, EventArgs e)
    {
        _isHost = true;
        _server = new GameServer(_db);
        _server.OnLog += AppendLog;

        // 服务端也需要作为客户端连接自己
        _server.Start(9527);

        // 显示本机IP
        string localIp = GetLocalIp();
        lblRoomInfo.Text = $"房间已创建！\n本机IP：{localIp}\n端口：9527\n等待对手加入...";
        btnCreateRoom.Enabled = false;
        btnJoinRoom.Enabled = false;

        // 房主也连接自己的服务器（不等待，不阻塞 UI）
        FireAndForget(ConnectAsClient("127.0.0.1", 9527));
    }

    // 加入房间
    private async void btnJoinRoom_Click(object sender, EventArgs e)
    {
        string ip = txtServerIp.Text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            MessageBox.Show("请输入服务器IP", "提示");
            return;
        }

        btnCreateRoom.Enabled = false;
        btnJoinRoom.Enabled = false;
        lblRoomInfo.Text = $"正在连接 {ip}:9527 ...";

        await ConnectAsClient(ip, 9527);
    }

    private async Task ConnectAsClient(string ip, int port)
    {
        _client = new GameClient();
        _client.OnLog += AppendLog;
        _client.OnGameStart += () =>
        {
            this.Invoke(() =>
            {
                AppendLog("游戏开始！");
                OpenBattleForm();
            });
        };

        bool ok = await _client.ConnectAsync(ip, port, _username);
        if (!ok)
        {
            this.Invoke(() =>
            {
                MessageBox.Show("连接失败，请检查IP和服务器状态", "错误");
                btnCreateRoom.Enabled = true;
                btnJoinRoom.Enabled = true;
            });
        }
        else
        {
            this.Invoke(() => lblRoomInfo.Text = "已连接，等待游戏开始...");
        }
    }

    private void OpenBattleForm()
    {
        var battleForm = new BattleForm(_username, _client!, _isHost ? _server : null);
        battleForm.FormClosed += (s, e) =>
        {
            btnCreateRoom.Enabled = true;
            btnJoinRoom.Enabled = true;
            lblRoomInfo.Text = "请创建或加入房间";
        };
        battleForm.Show();
    }

    private void AppendLog(string msg)
    {
        if (lstLog.InvokeRequired)
            lstLog.Invoke(() => lstLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {msg}"));
        else
            lstLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
    }

    private static string GetLocalIp()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "127.0.0.1";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _server?.Dispose();
            _client?.Dispose();
            _db.Dispose();
        }
        base.Dispose(disposing);
    }
}