namespace VoiceBattle.Forms;

partial class LoginForm
{
    private Label lblTitle, lblUsername, lblPassword;
    private TextBox txtUsername, txtPassword;
    private Button btnLogin, btnRegister;

    private void InitializeComponent()
    {
        this.Text = "奶龙大作战 - 登录";
        this.Size = new Size(360, 280);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(30, 30, 46);

        lblTitle = new Label
        {
            Text = "🎤 奶龙大作战",
            Font = new Font("微软雅黑", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(137, 180, 250),
            Location = new Point(60, 20),
            Size = new Size(240, 40),
            TextAlign = ContentAlignment.MiddleCenter
        };

        lblUsername = new Label
        {
            Text = "用户名：",
            ForeColor = Color.White,
            Location = new Point(40, 80),
            Size = new Size(70, 25)
        };

        txtUsername = new TextBox
        {
            Location = new Point(115, 78),
            Size = new Size(180, 25),
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        lblPassword = new Label
        {
            Text = "密码：",
            ForeColor = Color.White,
            Location = new Point(40, 120),
            Size = new Size(70, 25)
        };

        txtPassword = new TextBox
        {
            Location = new Point(115, 118),
            Size = new Size(180, 25),
            PasswordChar = '●',
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        btnLogin = new Button
        {
            Text = "登 录",
            Location = new Point(60, 170),
            Size = new Size(100, 36),
            BackColor = Color.FromArgb(137, 180, 250),
            ForeColor = Color.FromArgb(30, 30, 46),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += btnLogin_Click;

        btnRegister = new Button
        {
            Text = "注 册",
            Location = new Point(185, 170),
            Size = new Size(100, 36),
            BackColor = Color.FromArgb(166, 227, 161),
            ForeColor = Color.FromArgb(30, 30, 46),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnRegister.FlatAppearance.BorderSize = 0;
        btnRegister.Click += btnRegister_Click;

        this.Controls.AddRange(new Control[]
        {
            lblTitle, lblUsername, txtUsername,
            lblPassword, txtPassword, btnLogin, btnRegister
        });
    }
}