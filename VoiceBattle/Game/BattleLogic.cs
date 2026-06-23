namespace VoiceBattle.Game;

public class BattleLogic
{
    // 进度条位置 0~100，50为中心
    // >50 我方领先，<50 对手领先
    public float BarPosition { get; private set; } = 50f;

    private const float MaxPosition = 100f;
    private const float PushStrength = 8f;      // 每次“哈”推动进度条的力度
    private const float SmoothFactor = 0.2f;    // 进度条平滑系数

    private float _targetPosition = 50f;

    public int TimeLeftSeconds { get; private set; } = 60;
    private DateTime _startTime;
    private bool _timerStarted = false;

    public bool IsGameOver { get; private set; } = false;

    // 本地不再判断胜负，只暴露是否触顶供服务端查询
    public bool IsTopReached => _targetPosition >= 98f || _targetPosition <= 2f;

    private readonly string _myName;

    public BattleLogic(string myName)
    {
        _myName = myName;
    }

    public void StartTimer()
    {
        _startTime = DateTime.Now;
        _timerStarted = true;
    }

    /// <summary>
    /// 本地玩家“哈”一声，进度条向右推
    /// </summary>
    public void PushByMe(float strength)
    {
        if (IsGameOver) return;
        _targetPosition = Math.Clamp(_targetPosition + PushStrength * strength, 0f, MaxPosition);
    }

    /// <summary>
    /// 对手“哈”一声，进度条向左推
    /// </summary>
    public void PushByOpponent(float strength)
    {
        if (IsGameOver) return;
        _targetPosition = Math.Clamp(_targetPosition - PushStrength * strength, 0f, MaxPosition);
    }

    /// <summary>
    /// 每帧调用
    /// </summary>
    public void Update()
    {
        if (IsGameOver) return;

        // 平滑插值进度条（视觉效果，不影响逻辑）
        BarPosition += (_targetPosition - BarPosition) * SmoothFactor;

        // 更新倒计时
        if (_timerStarted)
        {
            int elapsed = (int)(DateTime.Now - _startTime).TotalSeconds;
            TimeLeftSeconds = Math.Max(0, 60 - elapsed);
        }
    }

    // 由外部（服务端通知后）调用，强制结束
    public void ForceEnd()
    {
        IsGameOver = true;
    }

    // 服务端同步进度条位置（可选，用于强制同步）
    public void SyncBarPosition(float position)
    {
        _targetPosition = position;
        BarPosition = position;
    }

    public float GetTargetPosition() => _targetPosition;

    public void Reset()
    {
        BarPosition = 50f;
        _targetPosition = 50f;
        TimeLeftSeconds = 60;
        _timerStarted = false;
        IsGameOver = false;
    }
}