using System.IO;

namespace CatDevTools.Services;

/// <summary>
/// Ollama 통신 로그 기록 및 UI 실시간 전파
/// </summary>
public sealed class OllamaLogService
{
    private static readonly Lazy<OllamaLogService> LazyInstance = new(() => new OllamaLogService());
    public static OllamaLogService Instance => LazyInstance.Value;

    private readonly object _lock = new();
    private string _logFolder = "";
    private string _logFilePath = "";

    public event Action<string>? OnLog;

    private OllamaLogService() { }

    public string LogFolder => _logFolder;
    public string LogFilePath => _logFilePath;

    public void Initialize(string? folderPath = null)
    {
        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            _logFolder = folderPath;
        }

        if (string.IsNullOrWhiteSpace(_logFolder))
        {
            _logFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CatDevTools",
                "OllamaLogs");
        }

        Directory.CreateDirectory(_logFolder);
        _logFilePath = Path.Combine(_logFolder, $"ollama_{DateTime.Now:yyyyMMdd_HHmmss}.log");
    }

    public void Log(string message)
    {
        EnsureInitialized();

        message = message.Replace("resLen=", "chars=");
        string line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
        lock (_lock)
        {
            File.AppendAllText(_logFilePath, line + Environment.NewLine);
        }

        OnLog?.Invoke(line);
    }

    private void EnsureInitialized()
    {
        if (!string.IsNullOrWhiteSpace(_logFilePath)) return;
        Initialize();
    }
}
