using System.Reflection;
using VoiceBattle.Audio;
using VoiceBattle.Game;
using VoiceBattle.Network;

namespace VoiceBattle.Forms;

public partial class BattleForm : Form
{
    private readonly string _myName;
    private readonly GameClient _client;
    private readonly GameServer? _server;

    private readonly AudioCaptureService _audio;
    private readonly BattleLogic _logic;
    // 直接初始化，消除空引用警告
    private readonly System.Windows.Forms.Timer _gameTimer = new System.Windows.Forms.Timer { Interval = 16 };

    // GIF 动画相关
    private Image? _myGif;
    private Image? _opponentGif;
    private Stream? _myGifStream;
    private Stream? _opponentGifStream;
    private bool _myAnimating = false;
    private bool _opponentAnimating = false;
    private System.Windows.Forms.Timer? _myAnimTimer;
    private System.Windows.Forms.Timer? _opponentAnimTimer;

    // 特效
    private float _flashAlpha = 0f;
    private int _screenShake = 0;

    // 防止重复弹出结算
    private bool _resultShown = false;

    public BattleForm(string myName, GameClient client, GameServer? server)
    {
        // 主窗体双缓冲，消除闪烁
        this.DoubleBuffered = true;

        InitializeComponent();
        _myName = myName;
        _client = client;
        _server = server;

        _logic = new BattleLogic(myName);

        // 音频
        _audio = new AudioCaptureService();
        _audio.OnHaDetected += OnHaDetected;
        _audio.OnVolumeChanged += vol =>
        {
            this.Invoke(() =>
            {
                pbMicVolume.Value = (int)Math.Clamp(vol * 100, 0, 100);
                lblVolume.Text = $"麦克风：{vol:P0}";
            });
        };

        // 网络：收到对手推送
        _client.OnEnergyReceived += (name, strength) =>
        {
            _logic.PushByOpponent(strength);
            this.Invoke(() => TriggerOpponentAnimation());
        };

        // 网络：收到服务端统一结算
        _client.OnGameOver += payload =>
        {
            if (payload == null) return;
            this.Invoke(() =>
            {
                if (_resultShown) return;
                _resultShown = true;
                _gameTimer.Stop();
                _audio.Stop();
                _logic.ForceEnd();
                ShowResult(payload.Winner, payload.BarPosition);
            });
        };

        // 再来一局事件
        _client.OnRematchRequest += () =>
        {
            this.Invoke(() => HandleRematchRequest());
        };

        _client.OnRematchDecline += () =>
        {
            this.Invoke(() =>
            {
                MessageBox.Show("对方拒绝了再来一局", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            });
        };

        _client.OnRematchStart += () =>
        {
            this.Invoke(() => DoReset());
        };

        // 游戏循环
        _gameTimer.Tick += GameLoop;

        // 加载内嵌 GIF
        LoadDefaultGifs();

        _logic.StartTimer();
        _gameTimer.Start();
        _audio.Start();
    }

    // ── 异步调用安全包装（消除未等待警告和空引用） ─────
    private void FireAndForget(Task? task)
    {
        if (task == null) return;
        task.ContinueWith(t =>
        {
            if (t.IsFaulted)
                System.Diagnostics.Debug.WriteLine($"网络发送失败：{t.Exception?.Message}");
        }, TaskScheduler.Default);
    }

    // ── GIF 加载（从嵌入资源） ────────────────────────────

    private void LoadDefaultGifs()
    {
        var assembly = Assembly.GetExecutingAssembly();

        _myGifStream = assembly.GetManifestResourceStream("VoiceBattle.Assets.my_character.gif");
        _opponentGifStream = assembly.GetManifestResourceStream("VoiceBattle.Assets.opponent_character.gif");

        if (_myGifStream != null)
            _myGif = Image.FromStream(_myGifStream);

        if (_opponentGifStream != null)
            _opponentGif = Image.FromStream(_opponentGifStream);
    }

    // ── GIF 动画触发 ──────────────────────────────────────

    private void TriggerMyAnimation()
    {
        if (_myGif == null) return;

        _myAnimTimer?.Stop();
        _myAnimTimer?.Dispose();

        ImageAnimator.StopAnimate(_myGif, OnMyGifFrameChanged);
        ImageAnimator.Animate(_myGif, OnMyGifFrameChanged);
        _myAnimating = true;

        _myAnimTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        _myAnimTimer.Tick += (s, e) =>
        {
            ImageAnimator.StopAnimate(_myGif, OnMyGifFrameChanged);
            _myAnimating = false;
            _myAnimTimer?.Stop();
        };
        _myAnimTimer.Start();
    }

    private void TriggerOpponentAnimation()
    {
        if (_opponentGif == null) return;

        _opponentAnimTimer?.Stop();
        _opponentAnimTimer?.Dispose();

        ImageAnimator.StopAnimate(_opponentGif, OnOpponentGifFrameChanged);
        ImageAnimator.Animate(_opponentGif, OnOpponentGifFrameChanged);
        _opponentAnimating = true;

        _opponentAnimTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        _opponentAnimTimer.Tick += (s, e) =>
        {
            ImageAnimator.StopAnimate(_opponentGif, OnOpponentGifFrameChanged);
            _opponentAnimating = false;
            _opponentAnimTimer?.Stop();
        };
        _opponentAnimTimer.Start();
    }

    private void OnMyGifFrameChanged(object? sender, EventArgs e)
        => pnlBattle.Invalidate();

    private void OnOpponentGifFrameChanged(object? sender, EventArgs e)
        => pnlBattle.Invalidate();

    // ── 音频回调 ──────────────────────────────────────────

    private void OnHaDetected(float strength)
    {
        _logic.PushByMe(strength);
        _flashAlpha = 0.5f;
        _screenShake = 4;

        this.Invoke(() => TriggerMyAnimation());

        FireAndForget(_client.SendEnergyAsync(strength));
    }

    // ── 游戏循环 ──────────────────────────────────────────

    private void GameLoop(object? sender, EventArgs e)
    {
        if (_logic.IsGameOver) return;

        _logic.Update();

        _flashAlpha = Math.Max(0f, _flashAlpha - 0.04f);
        _screenShake = Math.Max(0, _screenShake - 1);

        // 倒计时显示
        lblTimer.Text = $"⏱ {_logic.TimeLeftSeconds:D2} 秒";
        if (_logic.TimeLeftSeconds <= 10)
            lblTimer.ForeColor = Color.FromArgb(243, 139, 168);
        else
            lblTimer.ForeColor = Color.FromArgb(166, 227, 161);

        // 更新 GIF 帧
        if (_myAnimating && _myGif != null)
            ImageAnimator.UpdateFrames(_myGif);
        if (_opponentAnimating && _opponentGif != null)
            ImageAnimator.UpdateFrames(_opponentGif);

        pnlBattle.Refresh();
    }

    // ── 绘制 ──────────────────────────────────────────────

    private void pnlBattle_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int w = pnlBattle.Width;
        int h = pnlBattle.Height;

        int sx = _screenShake > 0 ? Random.Shared.Next(-_screenShake, _screenShake) : 0;
        int sy = _screenShake > 0 ? Random.Shared.Next(-_screenShake, _screenShake) : 0;
        g.TranslateTransform(sx, sy);

        g.Clear(Color.FromArgb(30, 30, 46));

        // ── 角色绘制 ──
        int charSize = 120;
        int charY = h / 2 - charSize / 2 - 20;

        Rectangle myRect = new Rectangle(20, charY, charSize, charSize);
        if (_myGif != null)
            g.DrawImage(_myGif, myRect);
        else
            DrawDefaultCharacter(g, myRect, Color.FromArgb(137, 180, 250), "我");

        Rectangle opRect = new Rectangle(w - charSize - 20, charY, charSize, charSize);
        if (_opponentGif != null)
            g.DrawImage(_opponentGif, opRect);
        else
            DrawDefaultCharacter(g, opRect, Color.FromArgb(243, 139, 168), "敌");

        // ── 拔河进度条 ──
        int barY = h - 80;
        int barH = 36;
        int barMargin = 20;
        int barW = w - barMargin * 2;

        g.FillRectangle(new SolidBrush(Color.FromArgb(49, 50, 68)),
            barMargin, barY, barW, barH);

        float ratio = _logic.BarPosition / 100f;
        int fillW = (int)(barW * ratio);

        if (fillW > 0)
        {
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(barMargin, barY, barW, barH),
                Color.FromArgb(243, 139, 168),
                Color.FromArgb(137, 180, 250),
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
            g.FillRectangle(brush, barMargin, barY, fillW, barH);
        }

        int centerX = barMargin + barW / 2;
        g.FillRectangle(Brushes.White, centerX - 2, barY - 8, 4, barH + 16);
        g.DrawRectangle(new Pen(Color.FromArgb(100, 100, 120), 2),
            barMargin, barY, barW, barH);

        g.DrawString("我方", new Font("微软雅黑", 9), Brushes.LightBlue,
            barMargin, barY + barH + 4);
        g.DrawString("对手", new Font("微软雅黑", 9), Brushes.LightPink,
            w - barMargin - 35, barY + barH + 4);

        // ── 闪光特效 ──
        if (_flashAlpha > 0)
        {
            using var fb = new SolidBrush(Color.FromArgb((int)(_flashAlpha * 80), 255, 255, 255));
            g.FillRectangle(fb, 0, 0, w, h);
        }
    }

    private void DrawDefaultCharacter(Graphics g, Rectangle rect, Color color, string label)
    {
        g.FillEllipse(new SolidBrush(color), rect);
        var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(label, new Font("微软雅黑", 16, FontStyle.Bold), Brushes.White, rect, sf);
    }

    // ── 胜负处理（完全由服务端统一通知触发） ──────────────

    private void ShowResult(string winner, float barPosition)
    {
        bool iWon = winner == _myName;
        string emoji = iWon ? "🎉" : "😢";
        string resultText = iWon ? $"{emoji} 你赢了！" : $"{emoji} 你输了！";
        string detail = $"最终进度：{barPosition:F0} / 100";

        MessageBox.Show(
            resultText + "\n" + detail,
            "游戏结束",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        var rematch = MessageBox.Show(
            "是否请求再来一局？",
            "再来一局",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (rematch == DialogResult.Yes)
        {
            FireAndForget(_client.SendRematchRequestAsync());
            lblTimer.Text = "等待对方确认...";
            lblTimer.ForeColor = Color.FromArgb(250, 179, 135);
        }
        else
        {
            FireAndForget(_client.SendRematchDeclineAsync());
            Close();
        }
    }

    private void HandleRematchRequest()
    {
        this.BeginInvoke(new Action(() =>
        {
            var result = MessageBox.Show(
                "对方请求再来一局，是否同意？",
                "再来一局",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                FireAndForget(_client.SendRematchAcceptAsync());
                lblTimer.Text = "等待开始...";
            }
            else
            {
                FireAndForget(_client.SendRematchDeclineAsync());
                Close();
            }
        }));
    }

    private void DoReset()
    {
        _resultShown = false;
        _logic.Reset();
        _logic.StartTimer();

        if (_myGif != null)
        {
            ImageAnimator.StopAnimate(_myGif, OnMyGifFrameChanged);
            _myAnimating = false;
        }
        if (_opponentGif != null)
        {
            ImageAnimator.StopAnimate(_opponentGif, OnOpponentGifFrameChanged);
            _opponentAnimating = false;
        }

        _flashAlpha = 0f;
        _screenShake = 0;

        _audio.Start();
        _gameTimer.Start();
        pnlBattle.Refresh();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _audio.Dispose();
            _gameTimer.Dispose();
            _myAnimTimer?.Dispose();
            _opponentAnimTimer?.Dispose();
            _myGif?.Dispose();
            _opponentGif?.Dispose();
            _myGifStream?.Dispose();
            _opponentGifStream?.Dispose();
        }
        base.Dispose(disposing);
    }
}