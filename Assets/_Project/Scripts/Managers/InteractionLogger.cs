using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using CatTalk2D.Models;

namespace CatTalk2D.Managers
{
    /// <summary>
    /// 상호작용 로그 관리자
    /// - JSON 형식으로 세션별 로그 저장
    /// - 분석 도구에서 사용할 데이터 제공
    /// </summary>
    public class InteractionLogger : MonoBehaviour
    {
        #region 싱글톤
        private static InteractionLogger _instance;
        public static InteractionLogger Instance => _instance;
        #endregion

        #region 설정
        [Header("로그 설정")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private string _logFolderName = "CatLogs";
        #endregion

        #region 내부 변수
        private SessionLog _currentSession;
        private string _sessionFilePath;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            StartNewSession();
        }

        private void OnApplicationQuit()
        {
            SaveSession();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveSession();
            }
        }
        #endregion

        #region 세션 관리
        /// <summary>
        /// 새 세션 시작
        /// </summary>
        public void StartNewSession()
        {
            _currentSession = new SessionLog
            {
                sessionId = Guid.NewGuid().ToString(),
                startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                records = new List<InteractionRecord>()
            };

            // 로그 폴더 생성
            string logFolder = Path.Combine(Application.persistentDataPath, _logFolderName);
            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

            // 파일 경로 설정
            string fileName = $"session_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            _sessionFilePath = Path.Combine(logFolder, fileName);

            Debug.Log($"[InteractionLogger] 새 세션 시작: {_sessionFilePath}");
        }

        /// <summary>
        /// 세션 저장
        /// </summary>
        public void SaveSession()
        {
            if (!_enableLogging || _currentSession == null) return;

            _currentSession.endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _currentSession.totalRecords = _currentSession.records.Count;

            try
            {
                string json = JsonUtility.ToJson(_currentSession, true);
                File.WriteAllText(_sessionFilePath, json);
                Debug.Log($"[InteractionLogger] 세션 저장됨: {_currentSession.totalRecords}개 기록");
            }
            catch (Exception e)
            {
                Debug.LogError($"[InteractionLogger] 저장 실패: {e.Message}");
            }
        }
        #endregion

        #region 로그 기록
        /// <summary>
        /// 상호작용 로그 기록
        /// </summary>
        public void LogInteraction(string actionType, CatStateSnapshot stateSnapshot,
                                   string userText = null, string aiText = null)
        {
            if (!_enableLogging || _currentSession == null) return;

            var record = new InteractionRecord
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                actionType = actionType,
                userText = userText ?? "",
                aiText = aiText ?? "",
                state = stateSnapshot
            };

            _currentSession.records.Add(record);

            Debug.Log($"[InteractionLogger] 기록됨: {actionType} (총 {_currentSession.records.Count}개)");

            // 10개마다 자동 저장
            if (_currentSession.records.Count % 10 == 0)
            {
                SaveSession();
            }
        }

        /// <summary>
        /// 대화 로그 기록 (사용자 입력 + AI 응답)
        /// </summary>
        public void LogConversation(string userText, string aiText, CatStateSnapshot stateSnapshot)
        {
            LogInteraction("talk", stateSnapshot, userText, aiText);
        }

        /// <summary>
        /// 혼잣말 로그 기록
        /// </summary>
        public void LogMonologue(string monologueText, CatStateSnapshot stateSnapshot)
        {
            LogInteraction("monologue", stateSnapshot, null, monologueText);
        }
        #endregion

        #region 유틸리티
        /// <summary>
        /// 로그 폴더 경로 반환
        /// </summary>
        public string GetLogFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, _logFolderName);
        }

        /// <summary>
        /// 현재 세션 기록 수
        /// </summary>
        public int GetCurrentRecordCount()
        {
            return _currentSession?.records.Count ?? 0;
        }
        #endregion
    }

    #region 로그 데이터 클래스
    /// <summary>
    /// 세션 로그 (JSON 루트)
    /// </summary>
    [Serializable]
    public class SessionLog
    {
        public string sessionId;
        public string startTime;
        public string endTime;
        public int totalRecords;
        public List<InteractionRecord> records;
    }

    /// <summary>
    /// 개별 상호작용 기록
    /// </summary>
    [Serializable]
    public class InteractionRecord
    {
        public string timestamp;
        public string actionType;  // feed, pet, play, talk, monologue
        public string userText;    // 사용자 입력 (대화일 때)
        public string aiText;      // AI 응답 (대화/혼잣말일 때)
        public CatStateSnapshot state;  // 상태 스냅샷
    }
    #endregion
}
