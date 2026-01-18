using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using CatTalk2D.Models;
using CatTalk2D.Managers;

namespace CatTalk2D.DevTools
{
    /// <summary>
    /// DevTools WebSocket 서버
    /// WPF 클라이언트와 실시간 통신
    /// localhost:9999 에서 동작 (외부 접근 불가)
    /// </summary>
    public class DevToolsServer : MonoBehaviour
    {
        private static DevToolsServer _instance;
        public static DevToolsServer Instance => _instance;

        [Header("서버 설정")]
        [SerializeField] private int _port = 9999;
        [SerializeField] private bool _enableServer = true;
        [SerializeField] private float _broadcastInterval = 0.5f;

        [Header("상태")]
        [SerializeField] private bool _isRunning;
        [SerializeField] private int _connectedClients;

        private TcpListener _listener;
        private List<TcpClient> _clients = new List<TcpClient>();
        private Thread _listenerThread;
        private bool _shouldStop;
        private float _lastBroadcastTime;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_enableServer)
            {
                StartServer();
            }
        }

        private void Update()
        {
            if (!_isRunning) return;

            // 주기적으로 상태 브로드캐스트
            if (Time.time - _lastBroadcastTime >= _broadcastInterval)
            {
                _lastBroadcastTime = Time.time;
                BroadcastState();
            }

            // 클라이언트 메시지 처리 (메인 스레드)
            ProcessPendingMessages();
        }

        private void OnDestroy()
        {
            StopServer();
        }

        private void OnApplicationQuit()
        {
            StopServer();
        }

        #region 서버 관리
        public void StartServer()
        {
            if (_isRunning) return;

            try
            {
                _shouldStop = false;
                _listener = new TcpListener(IPAddress.Loopback, _port);  // localhost only
                _listener.Start();
                _isRunning = true;

                _listenerThread = new Thread(ListenForClients);
                _listenerThread.IsBackground = true;
                _listenerThread.Start();

                Debug.Log($"[DevToolsServer] 서버 시작: localhost:{_port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DevToolsServer] 서버 시작 실패: {e.Message}");
            }
        }

        public void StopServer()
        {
            if (!_isRunning) return;

            _shouldStop = true;
            _isRunning = false;

            // 클라이언트 연결 종료
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    try { client.Close(); } catch { }
                }
                _clients.Clear();
            }

            // 리스너 종료
            try { _listener?.Stop(); } catch { }

            Debug.Log("[DevToolsServer] 서버 종료됨");
        }

        private void ListenForClients()
        {
            while (!_shouldStop)
            {
                try
                {
                    if (_listener.Pending())
                    {
                        TcpClient client = _listener.AcceptTcpClient();
                        lock (_clients)
                        {
                            _clients.Add(client);
                            _connectedClients = _clients.Count;
                        }

                        Thread clientThread = new Thread(() => HandleClient(client));
                        clientThread.IsBackground = true;
                        clientThread.Start();

                        Debug.Log($"[DevToolsServer] 클라이언트 연결됨 (총 {_connectedClients}개)");
                    }
                    Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    if (!_shouldStop)
                    {
                        Debug.LogError($"[DevToolsServer] 리스너 오류: {e.Message}");
                    }
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];

            while (!_shouldStop && client.Connected)
            {
                try
                {
                    if (stream.DataAvailable)
                    {
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            EnqueueMessage(message);
                        }
                    }
                    Thread.Sleep(50);
                }
                catch
                {
                    break;
                }
            }

            // 클라이언트 제거
            lock (_clients)
            {
                _clients.Remove(client);
                _connectedClients = _clients.Count;
            }
            try { client.Close(); } catch { }

            Debug.Log($"[DevToolsServer] 클라이언트 연결 해제됨 (총 {_connectedClients}개)");
        }
        #endregion

        #region 메시지 처리
        private Queue<string> _messageQueue = new Queue<string>();
        private readonly object _queueLock = new object();

        private void EnqueueMessage(string message)
        {
            lock (_queueLock)
            {
                _messageQueue.Enqueue(message);
            }
        }

        private void ProcessPendingMessages()
        {
            lock (_queueLock)
            {
                while (_messageQueue.Count > 0)
                {
                    string message = _messageQueue.Dequeue();
                    ProcessMessage(message);
                }
            }
        }

        private void ProcessMessage(string message)
        {
            try
            {
                DevToolsMessage msg = JsonUtility.FromJson<DevToolsMessage>(message);

                switch (msg.type)
                {
                    case "get_state":
                        SendStateToAll();
                        break;

                    case "set_state":
                        HandleSetState(msg.payload);
                        break;

                    case "set_date":
                        HandleSetDate(msg.payload);
                        break;

                    case "add_days":
                        HandleAddDays(msg.payload);
                        break;

                    default:
                        Debug.LogWarning($"[DevToolsServer] 알 수 없는 메시지 타입: {msg.type}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DevToolsServer] 메시지 처리 오류: {e.Message}");
            }
        }

        private void HandleSetState(string payload)
        {
            if (CatStateManager.Instance?.CatState == null) return;

            var state = CatStateManager.Instance.CatState;
            var changes = JsonUtility.FromJson<StateChangeRequest>(payload);

            // 변경된 필드만 업데이트
            if (changes.hunger >= 0)
            {
                LogAndSetState("hunger", state.Hunger.ToString("F1"), changes.hunger.ToString("F1"));
                state.Hunger = changes.hunger;
            }
            if (changes.energy >= 0)
            {
                LogAndSetState("energy", state.Energy.ToString("F1"), changes.energy.ToString("F1"));
                state.Energy = changes.energy;
            }
            if (changes.stress >= 0)
            {
                LogAndSetState("stress", state.Stress.ToString("F1"), changes.stress.ToString("F1"));
                state.Stress = changes.stress;
            }
            if (changes.fun >= 0)
            {
                LogAndSetState("fun", state.Fun.ToString("F1"), changes.fun.ToString("F1"));
                state.Fun = changes.fun;
            }
            if (changes.affection >= 0)
            {
                LogAndSetState("affection", state.Affection.ToString("F1"), changes.affection.ToString("F1"));
                state.Affection = changes.affection;
            }
            if (changes.trust >= 0)
            {
                LogAndSetState("trust", state.Trust.ToString("F1"), changes.trust.ToString("F1"));
                state.Trust = changes.trust;
            }
            if (changes.experience >= 0)
            {
                LogAndSetState("experience", state.Experience.ToString(), changes.experience.ToString());
                state.Experience = changes.experience;
            }

            Debug.Log("[DevToolsServer] 상태 변경 적용됨");
        }

        private void LogAndSetState(string field, string oldValue, string newValue)
        {
            InteractionLogger.Instance?.LogDevOverride(
                CatStateManager.Instance.CatState.CreateSnapshot(),
                field, oldValue, newValue
            );
        }

        private void HandleSetDate(string payload)
        {
            if (TimeManager.Instance == null) return;

            var dateRequest = JsonUtility.FromJson<DateChangeRequest>(payload);
            if (DateTime.TryParse(dateRequest.gameDate, out DateTime newDate))
            {
                TimeManager.Instance.SetGameDate(newDate);
            }
        }

        private void HandleAddDays(string payload)
        {
            if (TimeManager.Instance == null) return;

            var request = JsonUtility.FromJson<AddDaysRequest>(payload);
            TimeManager.Instance.AddDays(request.days);
        }
        #endregion

        #region 상태 브로드캐스트
        private void BroadcastState()
        {
            if (_connectedClients == 0) return;
            SendStateToAll();
        }

        private void SendStateToAll()
        {
            var response = CreateStateResponse();
            string json = JsonUtility.ToJson(response);
            SendToAllClients(json);
        }

        private DevToolsStateResponse CreateStateResponse()
        {
            var catState = CatStateManager.Instance?.CatState;
            var timeSnapshot = TimeManager.Instance?.CreateSnapshot();

            return new DevToolsStateResponse
            {
                type = "state",
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                gameDate = timeSnapshot?.gameDate ?? "",
                catAgeDays = timeSnapshot?.catAgeDays ?? 0,
                currentHour = timeSnapshot?.currentHour ?? 0,
                currentMinute = timeSnapshot?.currentMinute ?? 0,
                hunger = catState?.Hunger ?? 0,
                energy = catState?.Energy ?? 0,
                stress = catState?.Stress ?? 0,
                fun = catState?.Fun ?? 0,
                affection = catState?.Affection ?? 0,
                trust = catState?.Trust ?? 0,
                experience = catState?.Experience ?? 0,
                level = catState?.Level ?? 1,
                playful = catState?.Playful ?? 50,
                shy = catState?.Shy ?? 50,
                aggressive = catState?.Aggressive ?? 50,
                curious = catState?.Curious ?? 50,
                mood = catState?.MoodSummary ?? "neutral"
            };
        }

        private void SendToAllClients(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");

            lock (_clients)
            {
                List<TcpClient> deadClients = new List<TcpClient>();

                foreach (var client in _clients)
                {
                    try
                    {
                        if (client.Connected)
                        {
                            client.GetStream().Write(data, 0, data.Length);
                        }
                        else
                        {
                            deadClients.Add(client);
                        }
                    }
                    catch
                    {
                        deadClients.Add(client);
                    }
                }

                foreach (var dead in deadClients)
                {
                    _clients.Remove(dead);
                    try { dead.Close(); } catch { }
                }

                _connectedClients = _clients.Count;
            }
        }
        #endregion
    }

    #region 메시지 클래스
    [Serializable]
    public class DevToolsMessage
    {
        public string type;     // get_state, set_state, set_date, add_days
        public string payload;  // JSON 문자열
    }

    [Serializable]
    public class DevToolsStateResponse
    {
        public string type;
        public string timestamp;
        public string gameDate;
        public int catAgeDays;
        public int currentHour;
        public int currentMinute;
        public float hunger;
        public float energy;
        public float stress;
        public float fun;
        public float affection;
        public float trust;
        public int experience;
        public int level;
        public float playful;
        public float shy;
        public float aggressive;
        public float curious;
        public string mood;
    }

    [Serializable]
    public class StateChangeRequest
    {
        public float hunger = -1;
        public float energy = -1;
        public float stress = -1;
        public float fun = -1;
        public float affection = -1;
        public float trust = -1;
        public int experience = -1;
    }

    [Serializable]
    public class DateChangeRequest
    {
        public string gameDate;
    }

    [Serializable]
    public class AddDaysRequest
    {
        public int days;
    }
    #endregion
}
