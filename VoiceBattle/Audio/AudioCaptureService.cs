using NAudio.Wave;

namespace VoiceBattle.Audio;

public class AudioCaptureService : IDisposable
{
    private WaveInEvent? _waveIn;
    private readonly VoiceAnalyzer _analyzer;

    // 事件：识别到"哈"声时触发，参数为力度 0~1
    public event Action<float>? OnHaDetected;
    public event Action<float>? OnVolumeChanged;

    public bool IsRunning { get; private set; }

    public AudioCaptureService()
    {
        _analyzer = new VoiceAnalyzer();
    }

    public void Start()
    {
        if (IsRunning) return;

        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(44100, 1), // 44.1kHz 单声道
            BufferMilliseconds = 50               // 每50ms一个回调
        };

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.StartRecording();
        IsRunning = true;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        // 转换为 float 样本
        float[] samples = new float[e.BytesRecorded / 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short s = BitConverter.ToInt16(e.Buffer, i * 2);
            samples[i] = s / 32768f;
        }

        float volume = _analyzer.GetRmsVolume(samples);
        OnVolumeChanged?.Invoke(volume);

        // 检测"哈"声
        float? haStrength = _analyzer.DetectHa(samples, volume);
        if (haStrength.HasValue)
        {
            OnHaDetected?.Invoke(haStrength.Value);
        }
    }

    public void Stop()
    {
        _waveIn?.StopRecording();
        IsRunning = false;
    }

    public void Dispose()
    {
        Stop();
        _waveIn?.Dispose();
    }
}