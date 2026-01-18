using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CatLogAnalyzer.Models;
using CatLogAnalyzer.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Microsoft.Win32;

namespace CatLogAnalyzer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly LogParserService _parserService = new();
    private readonly StatisticsService _statsService = new();

    #region Properties

    [ObservableProperty]
    private string _logFolderPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LogFileItem> _logFiles = new();

    [ObservableProperty]
    private LogFileItem? _selectedLogFile;

    [ObservableProperty]
    private LogSession? _currentSession;

    [ObservableProperty]
    private SessionStatistics? _statistics;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "폴더를 선택하세요";

    // 차트 토글
    [ObservableProperty]
    private bool _showHunger = true;

    [ObservableProperty]
    private bool _showStress = true;

    [ObservableProperty]
    private bool _showEnergy;

    [ObservableProperty]
    private bool _showFun;

    #endregion

    #region Charts

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
            NamePaint = new SolidColorPaint(SKColor.Parse("#666666")) { SKTypeface = SKFontManager.Default.MatchCharacter('가') },
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#666666")) { SKTypeface = SKFontManager.Default.MatchCharacter('가') },
            NameTextSize = 10,
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
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#666666")) { SKTypeface = SKFontManager.Default.MatchCharacter('가') },
            TextSize = 11
        }
    };

    #endregion

    #region Commands

    [RelayCommand]
    private void SelectFolder()
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

    [RelayCommand]
    private async Task LoadSelectedFile()
    {
        if (SelectedLogFile == null) return;

        IsLoading = true;
        StatusMessage = "로딩 중...";

        try
        {
            CurrentSession = await Task.Run(() =>
                _parserService.ParseLogFile(SelectedLogFile.FilePath));

            if (CurrentSession != null)
            {
                Statistics = _statsService.CalculateStatistics(CurrentSession);
                UpdateCharts();
                StatusMessage = $"로드됨: {CurrentSession.Records.Count}개 기록";
            }
            else
            {
                StatusMessage = "파일 로드 실패";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void RefreshCharts()
    {
        if (CurrentSession != null)
        {
            UpdateStateChart();
        }
    }

    #endregion

    #region Private Methods

    private void LoadLogFiles()
    {
        LogFiles.Clear();

        var files = _parserService.GetLogFiles(LogFolderPath);

        foreach (var file in files)
        {
            LogFiles.Add(new LogFileItem
            {
                FileName = file.Name,
                FilePath = file.FullName,
                LastModified = file.LastWriteTime
            });
        }

        StatusMessage = $"{files.Count}개 파일 발견";
    }

    private void UpdateCharts()
    {
        if (CurrentSession == null) return;

        UpdateAffectionChart();
        UpdateStateChart();
        UpdateActionChart();
        UpdatePersonalityChart();
    }

    private void UpdateAffectionChart()
    {
        var data = _statsService.GetTimeSeriesData(CurrentSession!, s => s.Affection);

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
            new Axis
            {
                Labels = labels,
                LabelsRotation = 0,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#666666")) { SKTypeface = SKFontManager.Default.MatchCharacter('가') },
                TextSize = 10
            }
        };
    }

    private void UpdateStateChart()
    {
        if (CurrentSession == null) return;

        var series = new List<ISeries>();

        if (ShowHunger)
        {
            var hungerData = _statsService.GetTimeSeriesData(CurrentSession, s => s.Hunger);
            if (hungerData.Count > 0)
            {
                series.Add(new LineSeries<double>
                {
                    Values = hungerData.Select(d => (double)d.value).ToArray(),
                    Name = "배고픔",
                    Stroke = new SolidColorPaint(SKColor.Parse("#FDCB6E")) { StrokeThickness = 2 },
                    GeometrySize = 0
                });
            }
        }

        if (ShowStress)
        {
            var stressData = _statsService.GetTimeSeriesData(CurrentSession, s => s.Stress);
            if (stressData.Count > 0)
            {
                series.Add(new LineSeries<double>
                {
                    Values = stressData.Select(d => (double)d.value).ToArray(),
                    Name = "스트레스",
                    Stroke = new SolidColorPaint(SKColor.Parse("#D63031")) { StrokeThickness = 2 },
                    GeometrySize = 0
                });
            }
        }

        if (ShowEnergy)
        {
            var energyData = _statsService.GetTimeSeriesData(CurrentSession, s => s.Energy);
            if (energyData.Count > 0)
            {
                series.Add(new LineSeries<double>
                {
                    Values = energyData.Select(d => (double)d.value).ToArray(),
                    Name = "에너지",
                    Stroke = new SolidColorPaint(SKColor.Parse("#00B894")) { StrokeThickness = 2 },
                    GeometrySize = 0
                });
            }
        }

        if (ShowFun)
        {
            var funData = _statsService.GetTimeSeriesData(CurrentSession, s => s.Fun);
            if (funData.Count > 0)
            {
                series.Add(new LineSeries<double>
                {
                    Values = funData.Select(d => (double)d.value).ToArray(),
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
        var counts = _statsService.GetActionCounts(CurrentSession!);

        var labels = new[] { "밥주기", "쓰다듬기", "놀기", "대화", "혼잣말" };
        var values = new double[]
        {
            counts.GetValueOrDefault(ActionType.Feed, 0),
            counts.GetValueOrDefault(ActionType.Pet, 0),
            counts.GetValueOrDefault(ActionType.Play, 0),
            counts.GetValueOrDefault(ActionType.Talk, 0),
            counts.GetValueOrDefault(ActionType.Monologue, 0)
        };

        var colors = new[]
        {
            SKColor.Parse("#FDCB6E"), // Feed - Yellow
            SKColor.Parse("#E17055"), // Pet - Orange
            SKColor.Parse("#6C5CE7"), // Play - Purple
            SKColor.Parse("#00B894"), // Talk - Green
            SKColor.Parse("#74B9FF")  // Monologue - Blue
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
        var personality = _statsService.GetLatestPersonality(CurrentSession!);

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
            new PolarAxis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#666666")) { SKTypeface = SKFontManager.Default.MatchCharacter('가') },
                TextSize = 11
            }
        };
    }

    partial void OnSelectedLogFileChanged(LogFileItem? value)
    {
        if (value != null)
        {
            LoadSelectedFileCommand.Execute(null);
        }
    }

    partial void OnShowHungerChanged(bool value) => RefreshChartsCommand.Execute(null);
    partial void OnShowStressChanged(bool value) => RefreshChartsCommand.Execute(null);
    partial void OnShowEnergyChanged(bool value) => RefreshChartsCommand.Execute(null);
    partial void OnShowFunChanged(bool value) => RefreshChartsCommand.Execute(null);

    #endregion
}

/// <summary>
/// 로그 파일 목록 아이템
/// </summary>
public class LogFileItem
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }

    public string DisplayName => $"{FileName} ({LastModified:MM-dd HH:mm})";
}
