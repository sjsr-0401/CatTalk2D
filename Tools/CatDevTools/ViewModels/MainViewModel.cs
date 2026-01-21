using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CatDevTools.Models;
using CatDevTools.Services;
using CatDevTools.Services.Scoring;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Microsoft.Win32;

namespace CatDevTools.ViewModels;

/// <summary>
/// 통합 메인 ViewModel (DevTools + LogAnalyzer + AI Tuning)
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly DevToolsClient _client;
    private readonly LogParserService _logParser = new();
    private readonly StatisticsService _statsService = new();
    private readonly OllamaService _ollamaService = new();
    private readonly List<string> _ollamaLogLines = new();
    private readonly DatasetExporter _datasetExporter = new();
    private readonly DatasetGenerator _datasetGenerator = new();
    private readonly BenchmarkRunner _benchmarkRunner = new();

    #region 공통
    [ObservableProperty]
    private int _selectedTabIndex;
    #endregion

    #region 연결 상태
    [ObservableProperty]
    private string _connectionStatus = "연결 안됨";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _lastError = "";

    [ObservableProperty]
    private string _lastUpdate = "";
    #endregion

    #region 현재 상태 (읽기 전용)
    [ObservableProperty]
    private string _gameDate = "-";

    [ObservableProperty]
    private int _catAgeDays;

    [ObservableProperty]
    private string _currentTime = "-";

    [ObservableProperty]
    private int _level = 1;

    [ObservableProperty]
    private int _experience;

    [ObservableProperty]
    private string _mood = "-";

    [ObservableProperty]
    private float _currentHunger;

    [ObservableProperty]
    private float _currentEnergy;

    [ObservableProperty]
    private float _currentStress;

    [ObservableProperty]
    private float _currentFun;

    [ObservableProperty]
    private float _currentAffection;

    [ObservableProperty]
    private float _currentTrust;

    [ObservableProperty]
    private float _playful;

    [ObservableProperty]
    private float _shy;

    [ObservableProperty]
    private float _aggressive;

    [ObservableProperty]
    private float _curious;
    #endregion

    #region 편집용 값
    [ObservableProperty]
    private float _editHunger;

    [ObservableProperty]
    private float _editEnergy;

    [ObservableProperty]
    private float _editStress;

    [ObservableProperty]
    private float _editFun;

    [ObservableProperty]
    private float _editAffection;

    [ObservableProperty]
    private float _editTrust;

    [ObservableProperty]
    private int _editExperience;

    [ObservableProperty]
    private string _editGameDate = "";

    [ObservableProperty]
    private int _addDaysValue = 1;
    #endregion

    #region 로그 분석기
    [ObservableProperty]
    private string _logFolderPath = "";

    [ObservableProperty]
    private ObservableCollection<LogFileItem> _logFiles = new();

    [ObservableProperty]
    private LogFileItem? _selectedLogFile;

    [ObservableProperty]
    private LogSession? _currentLogSession;

    [ObservableProperty]
    private SessionStatistics? _logStatistics;

    [ObservableProperty]
    private bool _isLogLoading;

    [ObservableProperty]
    private string _logStatusMessage = "폴더를 선택하세요";

    // 차트 토글
    [ObservableProperty]
    private bool _showHunger = true;

    [ObservableProperty]
    private bool _showStress = true;

    [ObservableProperty]
    private bool _showEnergy;

    [ObservableProperty]
    private bool _showFun;

    // 차트 시리즈
    [ObservableProperty]
    private ISeries[] _affectionSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _stateSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _actionSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _personalitySeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _timeAxis = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _valueAxis = new Axis[]
    {
        new Axis
        {
            MinLimit = 0,
            MaxLimit = 100,
            TextSize = 10
        }
    };

    [ObservableProperty]
    private PolarAxis[] _personalityAngles = Array.Empty<PolarAxis>();

    [ObservableProperty]
    private PolarAxis[] _personalityRadius = new PolarAxis[]
    {
        new PolarAxis { MinLimit = 0, MaxLimit = 100 }
    };

    [ObservableProperty]
    private Axis[] _actionXAxis = new Axis[]
    {
        new Axis
        {
            Labels = new[] { "밥주기", "쓰다듬기", "놀기", "대화", "혼잣말" },
            LabelsRotation = 0,
            TextSize = 11
        }
    };
    #endregion

    #region AI 튜닝
    [ObservableProperty]
    private bool _isOllamaRunning;

    [ObservableProperty]
    private string _ollamaStatus = "확인 중...";

    [ObservableProperty]
    private ObservableCollection<OllamaModel> _ollamaModels = new();

    [ObservableProperty]
    private OllamaModel? _selectedModel;

    [ObservableProperty]
    private string _newModelName = "";

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _downloadStatus = "";

    [ObservableProperty]
    private string _testPrompt = "안녕! 오늘 기분이 어때?";

    [ObservableProperty]
    private string _testResult = "";

    [ObservableProperty]
    private string _testStatus = "";

    [ObservableProperty]
    private string _trainingLogFolder = "";

    [ObservableProperty]
    private string _trainingDataResult = "";

    // Ollama 로그
    [ObservableProperty]
    private string _ollamaLogFolder = "";

    [ObservableProperty]
    private string _ollamaLogText = "";

    [ObservableProperty]
    private string _ollamaLogFilter = "";

    [ObservableProperty]
    private bool _ollamaLogAutoScroll = true;

    public bool CanDownload => !string.IsNullOrWhiteSpace(NewModelName) && !IsDownloading;

    // 벤치마크
    [ObservableProperty]
    private string _benchmarkModels = "aya:8b, gemma2:9b";

    [ObservableProperty]
    private string _benchmarkStatus = "";

    [ObservableProperty]
    private bool _isBenchmarking;

    // 태그 스타일 모델 선택
    [ObservableProperty]
    private ObservableCollection<string> _selectedBenchmarkModels = new();

    [ObservableProperty]
    private OllamaModel? _benchmarkModelToAdd;

    [ObservableProperty]
    private ISeries[] _benchmarkQualitySeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _benchmarkTimeSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _benchmarkModelAxis = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _benchmarkPercentAxis = new Axis[]
    {
        new Axis { MinLimit = 0, MaxLimit = 100, TextSize = 10 }
    };

    [ObservableProperty]
    private Axis[] _benchmarkTimeAxis = new Axis[]
    {
        new Axis { MinLimit = 0, TextSize = 10 }
    };

    [ObservableProperty]
    private ObservableCollection<TestScenario> _testScenarios = new();

    [ObservableProperty]
    private TestScenario? _selectedScenario;

    public bool CanRunBenchmark => !IsBenchmarking && SelectedBenchmarkModels.Count > 0;

    // 데이터셋 내보내기
    [ObservableProperty]
    private string _datasetLogFolder = "";

    [ObservableProperty]
    private string _datasetResult = "";

    [ObservableProperty]
    private bool _datasetExcludeEnglish = true;

    [ObservableProperty]
    private bool _isExportingDataset;

    // 새 벤치마크 (고양이다움)
    [ObservableProperty]
    private ObservableCollection<BenchmarkRankingItem> _benchmarkRankings = new();

    [ObservableProperty]
    private bool _hasBenchmarkResults;

    // 벤치마크 결과 내보내기
    private readonly BenchmarkExporter _benchmarkExporter = new();
    private readonly CatLikenessScorer _catLikenessScorer = new();
    private BenchmarkExportData? _lastBenchmarkExportData;

    [ObservableProperty]
    private bool _autoExportBenchmark = true;

    [ObservableProperty]
    private string _lastExportPath = "";

    [ObservableProperty]
    private ISeries[] _benchmarkScoreSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _benchmarkTotalSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _benchmarkMetricAxis = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _benchmarkScoreAxis = new Axis[]
    {
        new Axis { MinLimit = 0, MaxLimit = 5, TextSize = 10 }
    };

    [ObservableProperty]
    private Axis[] _benchmarkTotalAxis = new Axis[]
    {
        new Axis { MinLimit = 0, MaxLimit = 25, TextSize = 10 }
    };

    // 데이터셋 생성기
    [ObservableProperty]
    private string _generatorCatName = "망고";

    [ObservableProperty]
    private int _generatorTargetCount = 500;

    [ObservableProperty]
    private string _generatorOutputPath = "";

    [ObservableProperty]
    private string _generatorResult = "";

    // 벤치마크용 테스트셋 경로 (생성 시 자동 설정)
    [ObservableProperty]
    private string _testSetPath = "";
    #endregion

    public MainViewModel()
    {
        _client = new DevToolsClient();
        _client.OnStateReceived += HandleStateReceived;
        _client.OnConnectionStatusChanged += HandleConnectionStatusChanged;
        _client.OnError += HandleError;

        InitializeOllamaLogging();

        // AI 튜닝 탭 초기화
        _ = InitializeOllamaAsync();
        InitializeTestScenarios();

        // 벤치마크 모델 선택 컬렉션 변경 감지
        SelectedBenchmarkModels.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CanRunBenchmark));
    }

    private void InitializeOllamaLogging()
    {
        OllamaLogFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CatDevTools",
            "OllamaLogs");

        OllamaLogService.Instance.Initialize(OllamaLogFolder);
        OllamaLogService.Instance.OnLog += HandleOllamaLog;
    }

    private void HandleOllamaLog(string line)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _ollamaLogLines.Add(line);

            const int maxLines = 500;
            if (_ollamaLogLines.Count > maxLines)
            {
                _ollamaLogLines.RemoveRange(0, _ollamaLogLines.Count - maxLines);
            }

            UpdateOllamaLogText();
        });
    }

    partial void OnOllamaLogFilterChanged(string value)
    {
        UpdateOllamaLogText();
    }

    private void UpdateOllamaLogText()
    {
        if (_ollamaLogLines.Count == 0)
        {
            OllamaLogText = "";
            return;
        }

        string filter = OllamaLogFilter?.Trim() ?? "";
        if (filter.Length == 0)
        {
            OllamaLogText = string.Join(Environment.NewLine, _ollamaLogLines);
            return;
        }

        var sb = new StringBuilder();
        foreach (var line in _ollamaLogLines)
        {
            if (line.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(line);
            }
        }

        OllamaLogText = sb.ToString();
    }

    #region DevTools 이벤트 핸들러
    private void HandleStateReceived(CatStateData state)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            GameDate = state.GameDate;
            CatAgeDays = state.CatAgeDays;
            CurrentTime = $"{state.CurrentHour:D2}:{state.CurrentMinute:D2}";
            Level = state.Level;
            Experience = state.Experience;
            Mood = GetMoodText(state.Mood);

            CurrentHunger = state.Hunger;
            CurrentEnergy = state.Energy;
            CurrentStress = state.Stress;
            CurrentFun = state.Fun;
            CurrentAffection = state.Affection;
            CurrentTrust = state.Trust;

            Playful = state.Playful;
            Shy = state.Shy;
            Aggressive = state.Aggressive;
            Curious = state.Curious;

            LastUpdate = state.Timestamp;

            if (EditGameDate == "")
            {
                SyncEditValues();
            }
        });
    }

    private void HandleConnectionStatusChanged(string status)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ConnectionStatus = status;
            IsConnected = _client.IsConnected;
        });
    }

    private void HandleError(string error)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LastError = error;
        });
    }
    #endregion

    #region DevTools 명령
    [RelayCommand]
    private async Task Connect()
    {
        await _client.ConnectAsync();
    }

    [RelayCommand]
    private void Disconnect()
    {
        _client.Disconnect();
    }

    [RelayCommand]
    private void SyncEditValues()
    {
        EditHunger = CurrentHunger;
        EditEnergy = CurrentEnergy;
        EditStress = CurrentStress;
        EditFun = CurrentFun;
        EditAffection = CurrentAffection;
        EditTrust = CurrentTrust;
        EditExperience = Experience;
        EditGameDate = GameDate;
    }

    [RelayCommand]
    private void ApplyStateChanges()
    {
        var request = new StateChangeRequest
        {
            Hunger = EditHunger,
            Energy = EditEnergy,
            Stress = EditStress,
            Fun = EditFun,
            Affection = EditAffection,
            Trust = EditTrust,
            Experience = EditExperience
        };
        _client.SendStateChange(request);
    }

    [RelayCommand]
    private void ApplyDateChange()
    {
        if (!string.IsNullOrWhiteSpace(EditGameDate))
        {
            _client.SendDateChange(EditGameDate);
        }
    }

    [RelayCommand]
    private void AddDays()
    {
        _client.SendAddDays(AddDaysValue);
    }

    [RelayCommand]
    private void SubtractDays()
    {
        _client.SendAddDays(-AddDaysValue);
    }

    [RelayCommand]
    private void SetMaxStats()
    {
        EditHunger = 100;
        EditEnergy = 100;
        EditStress = 0;
        EditFun = 100;
        EditAffection = 100;
        EditTrust = 100;
        ApplyStateChanges();
    }

    [RelayCommand]
    private void SetMinStats()
    {
        EditHunger = 0;
        EditEnergy = 0;
        EditStress = 100;
        EditFun = 0;
        EditAffection = 0;
        EditTrust = 0;
        ApplyStateChanges();
    }
    #endregion

    #region 로그 분석기 명령
    [RelayCommand]
    private void SelectLogFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "로그 폴더 선택"
        };

        if (dialog.ShowDialog() == true)
        {
            LogFolderPath = dialog.FolderName;
            LoadLogFiles();
        }
    }

    private void LoadLogFiles()
    {
        LogFiles.Clear();
        var files = _logParser.GetLogFiles(LogFolderPath);

        foreach (var file in files)
        {
            LogFiles.Add(new LogFileItem
            {
                FileName = file.Name,
                FilePath = file.FullName,
                LastModified = file.LastWriteTime
            });
        }

        LogStatusMessage = $"{files.Count}개 파일 발견";
    }

    [RelayCommand]
    private async Task LoadSelectedLogFile()
    {
        if (SelectedLogFile == null) return;

        IsLogLoading = true;
        LogStatusMessage = "로딩 중...";

        try
        {
            CurrentLogSession = await Task.Run(() =>
                _logParser.ParseLogFile(SelectedLogFile.FilePath));

            if (CurrentLogSession != null)
            {
                LogStatistics = _statsService.CalculateStatistics(CurrentLogSession);
                UpdateLogCharts();
                LogStatusMessage = $"로드됨: {CurrentLogSession.Records.Count}개 기록";
            }
            else
            {
                LogStatusMessage = "파일 로드 실패";
            }
        }
        finally
        {
            IsLogLoading = false;
        }
    }

    partial void OnSelectedLogFileChanged(LogFileItem? value)
    {
        if (value != null)
        {
            LoadSelectedLogFileCommand.Execute(null);
        }
    }

    partial void OnShowHungerChanged(bool value) => UpdateStateChart();
    partial void OnShowStressChanged(bool value) => UpdateStateChart();
    partial void OnShowEnergyChanged(bool value) => UpdateStateChart();
    partial void OnShowFunChanged(bool value) => UpdateStateChart();

    private void UpdateLogCharts()
    {
        if (CurrentLogSession == null) return;

        UpdateAffectionChart();
        UpdateStateChart();
        UpdateActionChart();
        UpdatePersonalityChart();
    }

    private void UpdateAffectionChart()
    {
        var data = _statsService.GetTimeSeriesData(CurrentLogSession!, s => s.Affection);
        if (data.Count == 0) return;

        var values = data.Select(d => (double)d.value).ToArray();
        var labels = data.Select(d => d.time.ToString("HH:mm")).ToArray();

        AffectionSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = values,
                Name = "호감도",
                Stroke = new SolidColorPaint(SKColor.Parse("#E17055")) { StrokeThickness = 3 },
                Fill = new SolidColorPaint(SKColor.Parse("#E17055").WithAlpha(50)),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#E17055")) { StrokeThickness = 2 },
                GeometrySize = 8
            }
        };

        TimeAxis = new Axis[]
        {
            new Axis { Labels = labels, LabelsRotation = 0, TextSize = 10 }
        };
    }

    private void UpdateStateChart()
    {
        if (CurrentLogSession == null) return;

        var series = new List<ISeries>();

        if (ShowHunger)
        {
            var data = _statsService.GetTimeSeriesData(CurrentLogSession, s => s.Hunger);
            if (data.Count > 0)
            {
                series.Add(new LineSeries<double>
                {
                    Values = data.Select(d => (double)d.value).ToArray(),
                    Name = "배고픔",
                    Stroke = new SolidColorPaint(SKColor.Parse("#FDCB6E")) { StrokeThickness = 2 },
                    GeometrySize = 0
                });
            }
        }

        if (ShowStress)
        {
            var data = _statsService.GetTimeSeriesData(CurrentLogSession, s => s.Stress);
            if (data.Count > 0)
            {
                series.Add(new LineSeries<double>
                {
                    Values = data.Select(d => (double)d.value).ToArray(),
                    Name = "스트레스",
                    Stroke = new SolidColorPaint(SKColor.Parse("#D63031")) { StrokeThickness = 2 },
                    GeometrySize = 0
                });
            }
        }

        if (ShowEnergy)
        {
            var data = _statsService.GetTimeSeriesData(CurrentLogSession, s => s.Energy);
            if (data.Count > 0)
            {
                series.Add(new LineSeries<double>
                {
                    Values = data.Select(d => (double)d.value).ToArray(),
                    Name = "에너지",
                    Stroke = new SolidColorPaint(SKColor.Parse("#00B894")) { StrokeThickness = 2 },
                    GeometrySize = 0
                });
            }
        }

        if (ShowFun)
        {
            var data = _statsService.GetTimeSeriesData(CurrentLogSession, s => s.Fun);
            if (data.Count > 0)
            {
                series.Add(new LineSeries<double>
                {
                    Values = data.Select(d => (double)d.value).ToArray(),
                    Name = "재미",
                    Stroke = new SolidColorPaint(SKColor.Parse("#6C5CE7")) { StrokeThickness = 2 },
                    GeometrySize = 0
                });
            }
        }

        StateSeries = series.ToArray();
    }

    private void UpdateActionChart()
    {
        var counts = _statsService.GetActionCounts(CurrentLogSession!);

        var values = new double[]
        {
            counts.GetValueOrDefault(LogActionType.Feed, 0),
            counts.GetValueOrDefault(LogActionType.Pet, 0),
            counts.GetValueOrDefault(LogActionType.Play, 0),
            counts.GetValueOrDefault(LogActionType.Talk, 0),
            counts.GetValueOrDefault(LogActionType.Monologue, 0)
        };

        ActionSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = values,
                Name = "행동 횟수",
                Fill = new SolidColorPaint(SKColor.Parse("#FF6B35"))
            }
        };
    }

    private void UpdatePersonalityChart()
    {
        var personality = _statsService.GetLatestPersonality(CurrentLogSession!);
        if (personality.Count == 0) return;

        var labels = personality.Keys.ToArray();
        var values = personality.Values.Select(v => (double)v).ToArray();

        PersonalitySeries = new ISeries[]
        {
            new PolarLineSeries<double>
            {
                Values = values,
                Name = "성격",
                GeometrySize = 10,
                Stroke = new SolidColorPaint(SKColor.Parse("#FF6B35")) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColor.Parse("#FF6B35").WithAlpha(100)),
                LineSmoothness = 0,
                IsClosed = true
            }
        };

        PersonalityAngles = new PolarAxis[]
        {
            new PolarAxis { Labels = labels, TextSize = 11 }
        };
    }
    #endregion

    #region AI 튜닝 명령
    private async Task InitializeOllamaAsync()
    {
        IsOllamaRunning = await _ollamaService.IsServerRunningAsync();
        OllamaStatus = IsOllamaRunning ? "Ollama 서버 실행 중" : "Ollama 서버 연결 안됨";

        if (IsOllamaRunning)
        {
            await RefreshModels();
        }
    }

    [RelayCommand]
    private async Task RefreshModels()
    {
        IsOllamaRunning = await _ollamaService.IsServerRunningAsync();
        OllamaStatus = IsOllamaRunning ? "Ollama 서버 실행 중" : "Ollama 서버 연결 안됨";

        if (!IsOllamaRunning) return;

        var models = await _ollamaService.GetModelsAsync();
        OllamaModels.Clear();
        foreach (var model in models)
        {
            OllamaModels.Add(model);
        }

        // 이전 선택 복원 또는 첫 번째 모델 선택
        if (OllamaModels.Count > 0)
        {
            SelectedModel = OllamaModels.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void ApplyModel()
    {
        if (SelectedModel != null)
        {
            // Unity에 모델 변경 명령 전송 (나중에 구현)
            // _client.SendModelChange(SelectedModel.Name);
            MessageBox.Show($"모델 '{SelectedModel.Name}' 선택됨\n\nUnity 연동은 추후 구현 예정",
                "모델 선택", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    partial void OnNewModelNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanDownload));
    }

    [RelayCommand]
    private async Task DownloadModel()
    {
        if (string.IsNullOrWhiteSpace(NewModelName)) return;

        IsDownloading = true;
        DownloadProgress = 0;
        DownloadStatus = "다운로드 준비 중...";

        var progress = new Progress<OllamaPullProgress>(p =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DownloadStatus = p.Status;
                if (p.Total > 0)
                {
                    DownloadProgress = p.Percentage;
                }
            });
        });

        var success = await _ollamaService.PullModelAsync(NewModelName, progress);

        IsDownloading = false;

        if (success)
        {
            DownloadStatus = "다운로드 완료!";
            NewModelName = "";
            await RefreshModels();
        }
        else
        {
            DownloadStatus = "다운로드 실패";
        }
    }

    [RelayCommand]
    private async Task RunTest()
    {
        if (SelectedModel == null)
        {
            TestResult = "모델을 먼저 선택하세요.";
            return;
        }

        TestStatus = "생성 중...";
        TestResult = "";

        var result = await _ollamaService.GenerateAsync(SelectedModel.Name, TestPrompt);

        TestStatus = "완료";
        TestResult = result ?? "응답 없음";
    }

    [RelayCommand]
    private void ClearOllamaLog()
    {
        _ollamaLogLines.Clear();
        OllamaLogText = "";
    }

    [RelayCommand]
    private void OpenOllamaLogFolder()
    {
        if (string.IsNullOrWhiteSpace(OllamaLogFolder) || !Directory.Exists(OllamaLogFolder)) return;
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = OllamaLogFolder,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void SelectTrainingFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "학습 로그 폴더 선택"
        };

        if (dialog.ShowDialog() == true)
        {
            TrainingLogFolder = dialog.FolderName;
        }
    }

    [RelayCommand]
    private async Task GenerateTrainingData()
    {
        if (string.IsNullOrWhiteSpace(TrainingLogFolder))
        {
            TrainingDataResult = "폴더를 먼저 선택하세요.";
            return;
        }

        TrainingDataResult = "생성 중...";

        try
        {
            var files = _logParser.GetLogFiles(TrainingLogFolder);
            var sessions = await _logParser.ParseMultipleFilesAsync(files.Select(f => f.FullName));

            int totalSamples = 0;
            int extendedSamples = 0;  // Control 정보 포함된 샘플
            int scoredSamples = 0;    // 점수 평가된 샘플
            var outputPath = Path.Combine(TrainingLogFolder, "training_data.jsonl");
            var extendedOutputPath = Path.Combine(TrainingLogFolder, "training_data_extended.jsonl");

            using var writer = new StreamWriter(outputPath);
            using var extendedWriter = new StreamWriter(extendedOutputPath);

            foreach (var session in sessions)
            {
                foreach (var record in session.Records)
                {
                    if (record.ParsedActionType == LogActionType.Talk &&
                        !string.IsNullOrEmpty(record.UserText) &&
                        !string.IsNullOrEmpty(record.AiText))
                    {
                        // 기본 학습 데이터 (input/output만)
                        var simpleSample = new
                        {
                            input = record.UserText,
                            output = record.AiText,
                            state = record.State
                        };
                        await writer.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(simpleSample));
                        totalSamples++;

                        // 확장 학습 데이터 (Control 정보 포함)
                        if (!string.IsNullOrEmpty(record.InputControl))
                        {
                            var extendedSample = new
                            {
                                control = record.InputControl,
                                model = record.ModelName,
                                userText = record.UserText,
                                rawResponse = record.RawResponse,
                                finalResponse = record.FinalResponse,
                                score = record.Score,
                                state = record.State,
                                timestamp = record.Timestamp
                            };
                            await extendedWriter.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(extendedSample));
                            extendedSamples++;

                            if (record.Score.HasValue)
                            {
                                scoredSamples++;
                            }
                        }
                    }
                }
            }

            TrainingDataResult = $"생성 완료!\n\n" +
                                $"세션: {sessions.Count}개\n" +
                                $"기본 샘플: {totalSamples}개\n" +
                                $"확장 샘플: {extendedSamples}개\n" +
                                $"평가된 샘플: {scoredSamples}개\n\n" +
                                $"저장 위치:\n" +
                                $"• {Path.GetFileName(outputPath)}\n" +
                                $"• {Path.GetFileName(extendedOutputPath)}";
        }
        catch (Exception ex)
        {
            TrainingDataResult = $"오류: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SelectDatasetFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "데이터셋 로그 폴더 선택"
        };

        if (dialog.ShowDialog() == true)
        {
            DatasetLogFolder = dialog.FolderName;
        }
    }

    [RelayCommand]
    private async Task ExportDataset()
    {
        if (string.IsNullOrWhiteSpace(DatasetLogFolder))
        {
            DatasetResult = "폴더를 먼저 선택하세요.";
            return;
        }

        IsExportingDataset = true;
        DatasetResult = "데이터셋 생성 중...";

        try
        {
            var outputPath = Path.Combine(DatasetLogFolder, "dataset.jsonl");
            var options = new DatasetExportOptions
            {
                ExcludeEnglish = DatasetExcludeEnglish
            };

            var result = await _datasetExporter.ExportDatasetAsync(
                DatasetLogFolder,
                outputPath,
                options);

            DatasetResult = result.GetSummary();
        }
        catch (Exception ex)
        {
            DatasetResult = $"오류: {ex.Message}";
        }
        finally
        {
            IsExportingDataset = false;
        }
    }

    private void InitializeTestScenarios()
    {
        TestScenarios = new ObservableCollection<TestScenario>
        {
            new() { MoodTag = "happy", AffectionTier = "high", AgeLevel = "teen", UserText = "안녕! 오늘 기분 어때?" },
            new() { MoodTag = "hungry", AffectionTier = "mid", AgeLevel = "teen", UserText = "밥 먹었어?" },
            new() { MoodTag = "neutral", AffectionTier = "mid", AgeLevel = "teen", UserText = "뭐하고 놀까?" },
            new() { MoodTag = "stressed", AffectionTier = "low", AgeLevel = "teen", UserText = "왜 그렇게 뚱해?" },
            new() { MoodTag = "tired", AffectionTier = "high", AgeLevel = "teen", UserText = "졸려 보이네" },
            new() { MoodTag = "bored", AffectionTier = "mid", AgeLevel = "child", UserText = "심심하지 않아?" }
        };
    }

    [RelayCommand]
    private void ResetTestScenarios()
    {
        InitializeTestScenarios();
    }

    [RelayCommand]
    private void DeleteSelectedScenario()
    {
        if (SelectedScenario != null)
        {
            TestScenarios.Remove(SelectedScenario);
            SelectedScenario = null;
        }
    }

    partial void OnIsBenchmarkingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRunBenchmark));
    }

    partial void OnBenchmarkModelsChanged(string value)
    {
        OnPropertyChanged(nameof(CanRunBenchmark));
    }

    [RelayCommand]
    private void AddBenchmarkModel()
    {
        if (BenchmarkModelToAdd == null) return;

        var modelName = BenchmarkModelToAdd.Name;
        if (!SelectedBenchmarkModels.Contains(modelName))
        {
            SelectedBenchmarkModels.Add(modelName);
            UpdateBenchmarkModelsFromSelection();
        }
    }

    [RelayCommand]
    private void RemoveBenchmarkModel(string modelName)
    {
        if (string.IsNullOrEmpty(modelName)) return;

        SelectedBenchmarkModels.Remove(modelName);
        UpdateBenchmarkModelsFromSelection();
    }

    private void UpdateBenchmarkModelsFromSelection()
    {
        BenchmarkModels = string.Join(", ", SelectedBenchmarkModels);
        OnPropertyChanged(nameof(CanRunBenchmark));
    }

    [RelayCommand]
    private async Task RunBenchmark()
    {
        if (string.IsNullOrWhiteSpace(BenchmarkModels)) return;

        IsBenchmarking = true;
        BenchmarkStatus = "벤치마크 준비 중...";

        var models = BenchmarkModels.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(m => m.Trim())
                                    .ToList();

        var results = new Dictionary<string, BenchmarkModelResult>();

        foreach (var model in models)
        {
            results[model] = new BenchmarkModelResult { Model = model };
        }

        int totalTests = models.Count * TestScenarios.Count;
        int currentTest = 0;

        foreach (var model in models)
        {
            foreach (var scenario in TestScenarios)
            {
                currentTest++;
                BenchmarkStatus = $"테스트 중... ({currentTest}/{totalTests}) - {model}";

                var prompt = BuildBenchmarkPrompt(scenario);
                var startTime = DateTime.Now;

                try
                {
                    var response = await _ollamaService.GenerateAsync(model, prompt);
                    var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

                    if (!string.IsNullOrEmpty(response))
                    {
                        results[model].TotalTests++;
                        results[model].TotalResponseTime += elapsed;

                        if (!ContainsEnglish(response))
                            results[model].KoreanCount++;

                        if (HasCatSuffix(response))
                            results[model].CatSuffixCount++;
                    }
                }
                catch
                {
                    results[model].ErrorCount++;
                }
            }
        }

        // 차트 업데이트
        UpdateBenchmarkCharts(results.Values.ToList());

        IsBenchmarking = false;
        BenchmarkStatus = $"완료 - {models.Count}개 모델, {TestScenarios.Count}개 시나리오";
    }

    private string BuildBenchmarkPrompt(TestScenario scenario)
    {
        var moodDesc = scenario.MoodTag switch
        {
            "happy" => "기분이 좋음",
            "hungry" => "배고픔",
            "stressed" => "스트레스 받음",
            "tired" => "피곤함",
            "bored" => "심심함",
            _ => "평범함"
        };

        var affectionDesc = scenario.AffectionTier switch
        {
            "low" => "낮은 호감도",
            "high" => "높은 호감도",
            _ => "보통 호감도"
        };

        return $"""
            당신은 '망고'라는 이름의 귀여운 고양이입니다.

            현재 상태:
            - 기분: {moodDesc}
            - 호감도: {affectionDesc}

            규칙:
            1. 반드시 한국어로만 대답하세요
            2. 문장 끝에 '냥', '냥~', '냥!' 중 하나를 붙이세요
            3. 1-2문장으로 짧게 대답하세요

            주인이 말합니다: "{scenario.UserText}"

            망고의 대답:
            """;
    }

    private static bool ContainsEnglish(string text)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(text, @"[a-zA-Z]{3,}");
    }

    private static bool HasCatSuffix(string text)
    {
        return text.Contains("냥") || text.Contains("냐") || text.Contains("야옹");
    }

    private void UpdateBenchmarkCharts(List<BenchmarkModelResult> results)
    {
        var modelNames = results.Select(r => r.Model).ToArray();

        // 품질 차트 (한국어 비율, 고양이 어미 비율)
        var koreanRates = results.Select(r =>
            r.TotalTests > 0 ? (double)r.KoreanCount / r.TotalTests * 100 : 0).ToArray();
        var catSuffixRates = results.Select(r =>
            r.TotalTests > 0 ? (double)r.CatSuffixCount / r.TotalTests * 100 : 0).ToArray();

        BenchmarkQualitySeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = koreanRates,
                Name = "한국어 응답률",
                Fill = new SolidColorPaint(SKColor.Parse("#00B894"))
            },
            new ColumnSeries<double>
            {
                Values = catSuffixRates,
                Name = "고양이 어미",
                Fill = new SolidColorPaint(SKColor.Parse("#FF6B35"))
            }
        };

        // 시간 차트
        var avgTimes = results.Select(r =>
            r.TotalTests > 0 ? r.TotalResponseTime / r.TotalTests : 0).ToArray();

        BenchmarkTimeSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = avgTimes,
                Name = "평균 응답시간",
                Fill = new SolidColorPaint(SKColor.Parse("#6C5CE7"))
            }
        };

        BenchmarkModelAxis = new Axis[]
        {
            new Axis { Labels = modelNames, LabelsRotation = 0, TextSize = 11 }
        };
    }

    [RelayCommand]
    private async Task RunCatBenchmark()
    {
        if (string.IsNullOrWhiteSpace(BenchmarkModels)) return;

        IsBenchmarking = true;
        HasBenchmarkResults = false;
        BenchmarkStatus = "벤치마크 준비 중...";

        var models = BenchmarkModels.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(m => m.Trim())
                                    .ToList();

        // testset.jsonl 경로 (우선순위: 1. 생성된 경로, 2. docs 폴더)
        var testSetPath = TestSetPath;

        // 생성된 경로가 없거나 파일이 없으면 docs 폴더 시도
        if (string.IsNullOrEmpty(testSetPath) || !File.Exists(testSetPath))
        {
            testSetPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "docs", "testset.jsonl");
        }

        if (!File.Exists(testSetPath))
        {
            testSetPath = Path.Combine(
                Directory.GetCurrentDirectory(), "docs", "testset.jsonl");
        }

        // 여전히 없으면 GeneratorOutputPath에서 찾기
        if (!File.Exists(testSetPath) && !string.IsNullOrEmpty(GeneratorOutputPath))
        {
            testSetPath = Path.Combine(GeneratorOutputPath, "testset.jsonl");
        }

        if (!File.Exists(testSetPath))
        {
            BenchmarkStatus = "testset.jsonl 파일을 찾을 수 없습니다.\n아래 '테스트셋 생성' 버튼을 먼저 눌러주세요.";
            IsBenchmarking = false;
            return;
        }

        // 찾은 경로 저장
        TestSetPath = testSetPath;

        try
        {
            var progress = new Progress<string>(msg =>
            {
                Application.Current.Dispatcher.Invoke(() => BenchmarkStatus = msg);
            });

            var results = await _benchmarkRunner.RunCompareBenchmarkAsync(models, testSetPath, progress);

            // 랭킹 업데이트
            BenchmarkRankings.Clear();
            int rank = 1;
            foreach (var result in results)
            {
                BenchmarkRankings.Add(new BenchmarkRankingItem
                {
                    Rank = rank++,
                    ModelName = result.ModelName,
                    TotalScore = result.TotalScore,
                    Grade = result.Grade,
                    ControlScore = result.ControlScore,
                    StateScore = result.StateReflectionScore,
                    AgeScore = result.AgeSpeechScore,
                    AffectionScore = result.AffectionAttitudeScore,
                    ConsistencyScore = result.CharacterConsistencyScore
                });
            }

            // 차트 업데이트
            UpdateCatBenchmarkCharts(results);

            // Export용 데이터 저장 (CatLikenessScore 계산 포함)
            _lastBenchmarkExportData = new BenchmarkExportData
            {
                Timestamp = DateTime.Now,
                TestSetPath = testSetPath,
                TestCaseCount = results.FirstOrDefault()?.TotalCases ?? 0,
                Results = results.Select(r => {
                    // 각 모델의 케이스별 CatLikenessScore 계산
                    var catScores = r.CaseResults.Select(caseResult => {
                        var control = ConvertToScoringControl(caseResult);
                        return _catLikenessScorer.Evaluate(control, caseResult.Response);
                    }).ToList();

                    // 평균 CatLikenessScore 계산
                    CatLikenessScoreExport? avgCatScore = null;
                    if (catScores.Count > 0)
                    {
                        avgCatScore = new CatLikenessScoreExport
                        {
                            ScoreTotal = (int)catScores.Average(s => s.ScoreTotal),
                            Breakdown = new CatScoreBreakdownExport
                            {
                                Routine = (int)catScores.Average(s => s.Breakdown.Routine),
                                Need = (int)catScores.Average(s => s.Breakdown.Need),
                                Trust = (int)catScores.Average(s => s.Breakdown.Trust),
                                Tsundere = (int)catScores.Average(s => s.Breakdown.Tsundere),
                                Sensitivity = (int)catScores.Average(s => s.Breakdown.Sensitivity),
                                Monologue = (int)catScores.Average(s => s.Breakdown.Monologue),
                                Action = (int)catScores.Average(s => s.Breakdown.Action)
                            },
                            ScoreReasons = catScores.SelectMany(s => s.ScoreReasons).Distinct().Take(10).ToList(),
                            MatchedTags = catScores.SelectMany(s => s.MatchedTags).Distinct().Take(10).ToList()
                        };
                    }

                    return new BenchmarkExportResult
                    {
                        ModelName = r.ModelName,
                        TotalScore = r.TotalScore,
                        Grade = r.Grade,
                        ControlScore = r.ControlScore,
                        StateReflectionScore = r.StateReflectionScore,
                        AgeSpeechScore = r.AgeSpeechScore,
                        AffectionAttitudeScore = r.AffectionAttitudeScore,
                        CharacterConsistencyScore = r.CharacterConsistencyScore,
                        CatLikenessScore = avgCatScore
                    };
                }).ToList()
            };

            // 자동 Export
            if (AutoExportBenchmark)
            {
                var (jsonPath, _) = _benchmarkExporter.ExportBoth(_lastBenchmarkExportData);
                LastExportPath = jsonPath;
            }

            HasBenchmarkResults = true;
            BenchmarkStatus = $"완료 - {models.Count}개 모델 평가" + (AutoExportBenchmark ? " (자동 저장됨)" : "");
        }
        catch (Exception ex)
        {
            BenchmarkStatus = $"오류: {ex.Message}";
        }
        finally
        {
            IsBenchmarking = false;
        }
    }

    /// <summary>
    /// BenchmarkCaseResult를 CatLikenessScorer용 ScoringControl로 변환
    /// </summary>
    private static CatLikenessScorer.ScoringControl ConvertToScoringControl(BenchmarkCaseResult caseResult)
    {
        // moodTag -> timeBlock 추정 (테스트셋에 timeBlock이 없는 경우)
        var timeBlock = caseResult.MoodTag.ToLower() switch
        {
            "tired" or "sleepy" => "afternoon",
            "excited" or "playful" => "night",
            "grumpy" => "deepnight",
            _ => "afternoon"
        };

        // moodTag -> needTop1 추정
        var needTop1 = caseResult.MoodTag.ToLower() switch
        {
            "hungry" => "food",
            "bored" or "playful" or "excited" => "play",
            "tired" or "sleepy" => "rest",
            "lonely" => "affection",
            _ => "none"
        };

        // affectionTier -> trustTier (현재 테스트셋에서 동일하게 사용)
        var trustTier = caseResult.AffectionTier.ToLower() switch
        {
            "high" => "high",
            "low" => "low",
            _ => "mid"
        };

        return new CatLikenessScorer.ScoringControl
        {
            AgeLevel = caseResult.AgeLevel,
            TimeBlock = timeBlock,
            NeedTop1 = needTop1,
            TrustTier = trustTier,
            AffectionTier = caseResult.AffectionTier,
            MoodSummary = caseResult.MoodTag,
            MoodState = caseResult.MoodTag
        };
    }

    private void UpdateCatBenchmarkCharts(List<BenchmarkResult> results)
    {
        var modelNames = results.Select(r => r.ModelName).ToArray();
        var metricNames = new[] { "Control", "상태반영", "나이말투", "호감도", "일관성" };

        // 지표별 점수 차트 (각 모델별 시리즈)
        var seriesList = new List<ISeries>();
        var colors = new[] { "#FF6B35", "#00B894", "#6C5CE7", "#FDCB6E", "#E17055" };

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var scores = new double[]
            {
                result.ControlScore,
                result.StateReflectionScore,
                result.AgeSpeechScore,
                result.AffectionAttitudeScore,
                result.CharacterConsistencyScore
            };

            seriesList.Add(new ColumnSeries<double>
            {
                Values = scores,
                Name = result.ModelName,
                Fill = new SolidColorPaint(SKColor.Parse(colors[i % colors.Length]))
            });
        }

        BenchmarkScoreSeries = seriesList.ToArray();
        BenchmarkMetricAxis = new Axis[]
        {
            new Axis { Labels = metricNames, LabelsRotation = 0, TextSize = 10 }
        };

        // 총점 비교 차트
        var totalScores = results.Select(r => (double)r.TotalScore).ToArray();

        BenchmarkTotalSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = totalScores,
                Name = "총점",
                Fill = new SolidColorPaint(SKColor.Parse("#00B894"))
            }
        };

        BenchmarkModelAxis = new Axis[]
        {
            new Axis { Labels = modelNames, LabelsRotation = 0, TextSize = 11 }
        };
    }

    [RelayCommand]
    private void SelectGeneratorOutput()
    {
        var dialog = new OpenFolderDialog { Title = "저장 폴더 선택" };
        if (dialog.ShowDialog() == true)
        {
            GeneratorOutputPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private async Task GenerateDataset()
    {
        if (string.IsNullOrWhiteSpace(GeneratorOutputPath))
        {
            GeneratorResult = "저장 경로를 먼저 선택하세요.";
            return;
        }

        GeneratorResult = "데이터셋 생성 중...";

        try
        {
            var outputPath = Path.Combine(GeneratorOutputPath, "dataset.jsonl");
            var options = new DatasetGenerateOptions
            {
                CatName = GeneratorCatName
            };

            // 450개 기본 데이터셋 또는 900개 확장 데이터셋
            var result = GeneratorTargetCount <= 450
                ? await _datasetGenerator.GenerateBasicDatasetAsync(outputPath, options)
                : await _datasetGenerator.GenerateExtendedDatasetAsync(outputPath, options);

            GeneratorResult = result.GetSummary();
        }
        catch (Exception ex)
        {
            GeneratorResult = $"오류: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GenerateTestSet()
    {
        if (string.IsNullOrWhiteSpace(GeneratorOutputPath))
        {
            GeneratorResult = "저장 경로를 먼저 선택하세요.";
            return;
        }

        GeneratorResult = "테스트셋 생성 중...";

        try
        {
            var outputPath = Path.Combine(GeneratorOutputPath, "testset.jsonl");
            var options = new DatasetGenerateOptions { CatName = GeneratorCatName };
            var result = await _datasetGenerator.GenerateTestSetAsync(outputPath, options);

            // 벤치마크에서 사용할 경로 저장
            TestSetPath = outputPath;

            GeneratorResult = $"테스트셋 생성 완료: {result.GeneratedCount}개\n저장 위치: {outputPath}\n\n벤치마크에서 이 테스트셋을 사용합니다.";
        }
        catch (Exception ex)
        {
            GeneratorResult = $"오류: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportBenchmarkResults()
    {
        if (_lastBenchmarkExportData == null || !HasBenchmarkResults)
        {
            BenchmarkStatus = "내보낼 벤치마크 결과가 없습니다.";
            return;
        }

        try
        {
            var (jsonPath, csvPath) = _benchmarkExporter.ExportBoth(_lastBenchmarkExportData);
            LastExportPath = jsonPath;
            BenchmarkStatus = $"저장됨: {Path.GetFileName(jsonPath)}";
        }
        catch (Exception ex)
        {
            BenchmarkStatus = $"저장 오류: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenBenchmarkExportFolder()
    {
        var folder = _benchmarkExporter.ExportFolder;
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = folder,
            UseShellExecute = true
        });
    }
    #endregion

    #region 헬퍼
    private static string GetMoodText(string mood)
    {
        return mood switch
        {
            "very_hungry" => "매우 배고픔",
            "hungry" => "배고픔",
            "stressed" => "스트레스",
            "bored" => "심심함",
            "tired" => "피곤함",
            "happy" => "행복",
            "neutral" => "보통",
            _ => mood
        };
    }
    #endregion

    public void Dispose()
    {
        OllamaLogService.Instance.OnLog -= HandleOllamaLog;
        _client.Dispose();
    }
}

/// <summary>
/// 테스트 시나리오
/// </summary>
public class TestScenario
{
    public string MoodTag { get; set; } = "neutral";
    public string AffectionTier { get; set; } = "mid";
    public string AgeLevel { get; set; } = "teen";
    public string UserText { get; set; } = "";
}

/// <summary>
/// 벤치마크 모델 결과
/// </summary>
public class BenchmarkModelResult
{
    public string Model { get; set; } = "";
    public int TotalTests { get; set; }
    public int KoreanCount { get; set; }
    public int CatSuffixCount { get; set; }
    public double TotalResponseTime { get; set; }
    public int ErrorCount { get; set; }
}

/// <summary>
/// 고양이다움 벤치마크 랭킹 아이템
/// </summary>
public class BenchmarkRankingItem
{
    public int Rank { get; set; }
    public string ModelName { get; set; } = "";
    public float TotalScore { get; set; }
    public string Grade { get; set; } = "D";
    public float ControlScore { get; set; }
    public float StateScore { get; set; }
    public float AgeScore { get; set; }
    public float AffectionScore { get; set; }
    public float ConsistencyScore { get; set; }
}
