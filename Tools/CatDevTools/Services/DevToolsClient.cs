using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CatDevTools.Models;

namespace CatDevTools.Services;

/// <summary>
/// Unity DevTools 서버에 연결하는 TCP 클라이언트
/// </summary>
public class DevToolsClient : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private Thread? _receiveThread;
    private bool _isRunning;
    private readonly StringBuilder _buffer = new();

    public bool IsConnected => _client?.Connected ?? false;
    public event Action<CatStateData>? OnStateReceived;
    public event Action<string>? OnConnectionStatusChanged;
    public event Action<string>? OnError;

    private const string HOST = "127.0.0.1";
    private const int PORT = 9999;

    /// <summary>
    /// 서버에 연결 시도
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(HOST, PORT);
            _stream = _client.GetStream();

            _isRunning = true;
            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Start();

            OnConnectionStatusChanged?.Invoke("연결됨");
            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"연결 실패: {ex.Message}");
            OnConnectionStatusChanged?.Invoke("연결 실패 - 게임이 실행 중인지 확인하세요");
            return false;
        }
    }

    /// <summary>
    /// 연결 해제
    /// </summary>
    public void Disconnect()
    {
        _isRunning = false;
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        OnConnectionStatusChanged?.Invoke("연결 끊김");
    }

    /// <summary>
    /// 메시지 수신 루프
    /// </summary>
    private void ReceiveLoop()
    {
        byte[] buffer = new byte[4096];

        while (_isRunning && _client?.Connected == true)
        {
            try
            {
                if (_stream?.DataAvailable == true)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        _buffer.Append(data);

                        // 줄바꿈으로 메시지 분리
                        string content = _buffer.ToString();
                        int newlineIndex;
                        while ((newlineIndex = content.IndexOf('\n')) >= 0)
                        {
                            string message = content.Substring(0, newlineIndex);
                            content = content.Substring(newlineIndex + 1);
                            _buffer.Clear();
                            _buffer.Append(content);

                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                ProcessMessage(message);
                            }
                        }
                    }
                }
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    OnError?.Invoke($"수신 오류: {ex.Message}");
                }
                break;
            }
        }

        OnConnectionStatusChanged?.Invoke("연결 끊김");
    }

    /// <summary>
    /// 수신된 메시지 처리
    /// </summary>
    private void ProcessMessage(string json)
    {
        try
        {
            var state = JsonSerializer.Deserialize<CatStateData>(json);
            if (state != null && state.Type == "state")
            {
                OnStateReceived?.Invoke(state);
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"메시지 파싱 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 상태 변경 요청 전송
    /// </summary>
    public void SendStateChange(StateChangeRequest request)
    {
        var message = new
        {
            type = "set_state",
            payload = JsonSerializer.Serialize(request)
        };
        SendMessage(JsonSerializer.Serialize(message));
    }

    /// <summary>
    /// 날짜 변경 요청 전송
    /// </summary>
    public void SendDateChange(string newDate)
    {
        var message = new
        {
            type = "set_date",
            payload = JsonSerializer.Serialize(new { gameDate = newDate })
        };
        SendMessage(JsonSerializer.Serialize(message));
    }

    /// <summary>
    /// 날짜 증감 요청 전송
    /// </summary>
    public void SendAddDays(int days)
    {
        var message = new
        {
            type = "add_days",
            payload = JsonSerializer.Serialize(new { days })
        };
        SendMessage(JsonSerializer.Serialize(message));
    }

    /// <summary>
    /// 메시지 전송
    /// </summary>
    private void SendMessage(string message)
    {
        try
        {
            if (_stream != null && _client?.Connected == true)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                _stream.Write(data, 0, data.Length);
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"전송 오류: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}
