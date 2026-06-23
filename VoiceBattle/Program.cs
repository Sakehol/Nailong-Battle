using VoiceBattle.Forms;

namespace VoiceBattle;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var loginForm = new LoginForm();
        if (loginForm.ShowDialog() != DialogResult.OK)
            return;

        string username = loginForm.LoggedInUsername;
        Application.Run(new LobbyForm(username));
    }
}