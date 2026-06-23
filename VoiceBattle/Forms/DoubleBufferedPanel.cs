namespace VoiceBattle.Forms;

/// <summary>
/// 开启双缓冲的 Panel，彻底消除重绘闪烁
/// </summary>
public class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        this.DoubleBuffered = true;
        this.SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
        this.UpdateStyles();
    }
}