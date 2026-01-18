using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CatDevTools.Models;
using CatDevTools.Services;

namespace CatDevTools.ViewModels;

/// <summary>
/// 메인 윈도우 ViewModel
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly DevToolsClient _client;

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

    public MainViewModel()
    {
        _client = new DevToolsClient();
        _client.OnStateReceived += HandleStateReceived;
        _client.OnConnectionStatusChanged += HandleConnectionStatusChanged;
        _client.OnError += HandleError;
    }

    #region 이벤트 핸들러
    private void HandleStateReceived(CatStateData state)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // 현재 상태 업데이트
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

            // 편집값도 초기화 (처음 연결 시)
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

    #region 명령
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
        _client.Dispose();
    }
}
