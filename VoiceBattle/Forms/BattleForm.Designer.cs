namespace VoiceBattle.Forms;

partial class BattleForm
{
    private DoubleBufferedPanel pnlBattle;
    private Label lblTimer, lblVolume, lblTitle, lblHint;
    private ProgressBar pbMicVolume;

    private void InitializeComponent()
    {
        this.Text = "奶龙大作战 - 对战中";
        this.Size = new Size(800, 560);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 46);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        lblTitle = new Label
        {
            Text = "🎤 奶龙大作战",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(137, 180, 250),
            Location = new Point(0, 8),
            Size = new Size(800, 32),
            TextAlign = ContentAlignment.MiddleCenter
        };

        lblTimer = new Label
        {
            Text = "⏱ 60 秒",
            Font = new Font("微软雅黑", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(166, 227, 161),
            Location = new Point(0, 42),
            Size = new Size(800, 40),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // 对战主面板（使用双缓冲 Panel 消除闪烁）
        pnlBattle = new DoubleBufferedPanel
        {
            Location = new Point(10, 88),
            Size = new Size(775, 360),
            BackColor = Color.FromArgb(24, 24, 37)
        };
        pnlBattle.Paint += pnlBattle_Paint;

        // 麦克风音量
        lblVolume = new Label
        {
            Text = "麦克风：0%",
            ForeColor = Color.FromArgb(166, 173, 200),
            Location = new Point(20, 460),
            Size = new Size(100, 20),
            Font = new Font("微软雅黑", 9)
        };

        pbMicVolume = new ProgressBar
        {
            Location = new Point(125, 462),
            Size = new Size(640, 16),
            Maximum = 100
        };

        lblHint = new Label
        {
            Text = "喊「哈」推动进度条！",
            ForeColor = Color.FromArgb(108, 112, 134),
            Location = new Point(0, 495),
            Size = new Size(800, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("微软雅黑", 9)
        };

        this.Controls.AddRange(new Control[]
        {
            lblTitle, lblTimer, pnlBattle,
            lblVolume, pbMicVolume, lblHint
        });
    }
}