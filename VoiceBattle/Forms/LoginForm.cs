using VoiceBattle.Database;
using VoiceBattle.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace VoiceBattle.Forms;

public partial class LoginForm : Form
{
    private readonly AppDbContext _db;

    public string LoggedInUsername { get; private set; } = "";
    public int LoggedInUserId { get; private set; }

    public LoginForm()
    {
        InitializeComponent();
        _db = new AppDbContext();
        _db.Database.EnsureCreated(); // 自动建表
    }

    private async void btnLogin_Click(object sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show("请输入用户名和密码", "提示");
            return;
        }

        string hash = HashPassword(password);
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username == username && u.PasswordHash == hash);

        if (user == null)
        {
            MessageBox.Show("用户名或密码错误", "登录失败");
            return;
        }

        LoggedInUsername = user.Username;
        LoggedInUserId = user.Id;
        DialogResult = DialogResult.OK;
        Close();
    }

    private async void btnRegister_Click(object sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrEmpty(username) || password.Length < 4)
        {
            MessageBox.Show("用户名不能为空，密码至少4位", "提示");
            return;
        }

        bool exists = await _db.Users.AnyAsync(u => u.Username == username);
        if (exists)
        {
            MessageBox.Show("用户名已存在", "注册失败");
            return;
        }

        _db.Users.Add(new User
        {
            Username = username,
            PasswordHash = HashPassword(password)
        });
        await _db.SaveChangesAsync();
        MessageBox.Show("注册成功，请登录", "成功");
    }

    private static string HashPassword(string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _db.Dispose();
        base.Dispose(disposing);
    }
}