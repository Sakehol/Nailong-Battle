namespace VoiceBattle.Forms;

partial class LobbyForm
{
    private Label lblWelcome, lblRoomInfo, lblIpLabel;
    private TextBox txtServerIp;
    private Button btnCreateRoom, btnJoinRoom;
    private ListBox lstLog;

    private void InitializeComponent()
    {
        this.Text = "奶龙大作战 - 大厅";
        this.Size = new Size(500, 480);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 46);

        lblWelcome = new Label
        {
            Text = "欢迎！",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(137, 180, 250),
            Location = new Point(20, 15),
            Size = new Size(460, 30)
        };

        btnCreateRoom = new Button
        {
            Text = "🏠 创建房间（我是主机）",
            Location = new Point(20, 60),
            Size = new Size(200, 45),
            BackColor = Color.FromArgb(166, 227, 161),
            ForeColor = Color.FromArgb(30, 30, 46),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCreateRoom.FlatAppearance.BorderSize = 0;
        btnCreateRoom.Click += btnCreateRoom_Click;

        lblIpLabel = new Label
        {
            Text = "服务器IP：",
            ForeColor = Color.White,
            Location = new Point(240, 70),
            Size = new Size(75, 25)
        };

        txtServerIp = new TextBox
        {
            Location = new Point(320, 68),
            Size = new Size(140, 25),
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.White,
            PlaceholderText = "192.168.x.x"
        };

        btnJoinRoom = new Button
        {
            Text = "🔗 加入房间",
            Location = new Point(320, 100),
            Size = new Size(140, 36),
            BackColor = Color.FromArgb(243, 139, 168),
            ForeColor = Color.FromArgb(30, 30, 46),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnJoinRoom.FlatAppearance.BorderSize = 0;
        btnJoinRoom.Click += btnJoinRoom_Click;

        lblRoomInfo = new Label
        {
            Text = "请创建或加入房间",
            ForeColor = Color.FromArgb(203, 166, 247),
            Location = new Point(20, 120),
            Size = new Size(460, 60),
            Font = new Font("微软雅黑", 10)
        };

        lstLog = new ListBox
        {
            Location = new Point(20, 195),
            Size = new Size(455, 240),
            BackColor = Color.FromArgb(24, 24, 37),
            ForeColor = Color.FromArgb(166, 173, 200),
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9)
        };

        this.Controls.AddRange(new Control[]
        {
            lblWelcome, btnCreateRoom, lblIpLabel,
            txtServerIp, btnJoinRoom, lblRoomInfo, lstLog
        });
    }
}