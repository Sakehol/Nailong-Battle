namespace VoiceBattle.Audio;

/// <summary>
/// "哈"声识别：
/// 1. 音量超过阈值（排除环境噪音）
/// 2. 频段集中在 500~3000Hz（"哈"声的主要频段）
/// 3. 短促爆发特征（能量快速上升）
/// </summary>
public class VoiceAnalyzer
{
    private const float NoiseThreshold = 0.05f;    // 噪音门限
    private const float HaThreshold = 0.12f;       // "哈"声触发门限
    private const int SampleRate = 44100;

    private float _prevVolume = 0f;
    private long _lastHaTime = 0;
    private const int HaCooldownMs = 300;           // "哈"声冷却，防止连续误触

    public float GetRmsVolume(float[] samples)
    {
        if (samples.Length == 0) return 0f;
        double sum = 0;
        foreach (var s in samples) sum += s * s;
        return (float)Math.Sqrt(sum / samples.Length);
    }

    /// <summary>
    /// 返回 null 表示未检测到"哈"声，返回 float 表示力度 0~1
    /// </summary>
    public float? DetectHa(float[] samples, float currentVolume)
    {
        // 1. 音量门限过滤
        if (currentVolume < HaThreshold)
        {
            _prevVolume = currentVolume;
            return null;
        }

        // 2. 冷却时间检查
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now - _lastHaTime < HaCooldownMs)
        {
            _prevVolume = currentVolume;
            return null;
        }

        // 3. 爆发特征：音量快速上升（"哈"声是爆破音）
        float rise = currentVolume - _prevVolume;
        _prevVolume = currentVolume;

        if (rise < 0.05f) return null; // 不是爆发，是持续音

        // 4. 频段分析：检查 500~3000Hz 能量占比
        float haFreqRatio = GetFrequencyRatio(samples, 500, 3000);
        if (haFreqRatio < 0.3f) return null; // 频段不符合

        // 通过所有检测，记录时间并返回力度
        _lastHaTime = now;
        float strength = Math.Clamp(currentVolume * 2f, 0f, 1f);
        return strength;
    }

    /// <summary>
    /// 简化 DFT：计算指定频段能量占总能量的比例
    /// </summary>
    private float GetFrequencyRatio(float[] samples, float lowHz, float highHz)
    {
        int n = Math.Min(samples.Length, 2048); // 只取前2048个样本
        double totalEnergy = 0;
        double targetEnergy = 0;

        for (int k = 1; k < n / 2; k++)
        {
            float freq = (float)k * SampleRate / n;

            // 计算该频率的能量（简化DFT）
            double re = 0, im = 0;
            for (int t = 0; t < n; t++)
            {
                double angle = 2 * Math.PI * k * t / n;
                re += samples[t] * Math.Cos(angle);
                im -= samples[t] * Math.Sin(angle);
            }
            double energy = re * re + im * im;

            totalEnergy += energy;
            if (freq >= lowHz && freq <= highHz)
                targetEnergy += energy;
        }

        if (totalEnergy < 1e-10) return 0f;
        return (float)(targetEnergy / totalEnergy);
    }
}