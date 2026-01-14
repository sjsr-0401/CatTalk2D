# UI 기능 설계

## 1. 채팅 UI (대화 가능)

### 구성 요소
```
┌─────────────────────────────┐
│   [시계] 오후 2:30   Day 5  │ ← 상단 상태바
├─────────────────────────────┤
│                             │
│     [고양이 망고]           │ ← 메인 영역
│                             │
│  [창문: 날씨 표시]          │
│  [밥그릇 아이콘]            │
├─────────────────────────────┤
│ 📝 대화 로그               │ ← 채팅 영역
│ 주인: 안녕?                │
│ 망고: 냥냥! 🥺            │
│                             │
│ [입력창] [전송]            │
└─────────────────────────────┘
```

### 구현 방법
**Day 4에 추가** (채팅 UI 구축 단계)

#### A. 채팅 패널
- **위치**: 화면 하단 1/3
- **컴포넌트**:
  - ScrollView (대화 로그)
  - InputField (텍스트 입력)
  - Button (전송)
- **스크립트**: `ChatManager.cs` (이미 Day 4 계획에 있음)

#### B. 대화 플로우
1. 사용자가 입력창에 텍스트 입력
2. 전송 버튼 클릭
3. `ChatManager` → Claude API 호출
4. 응답 받아서 대화 로그에 표시

---

## 2. 하트 이펙트 (클릭 시)

### 현재 상태
- ✅ `CatInteraction.cs`에 `PlayReactionEffect()` 있음 (로그만 출력)

### 추가 구현 (Day 2 권장)

#### A. 하트 파티클 시스템
**방법 1: Unity Particle System** (추천)
```
1. GameObject → Effects → Particle System 생성
2. 하트 모양 텍스처 적용
3. Prefab으로 저장: Prefabs/Effects/HeartParticle.prefab
4. CatInteraction.cs에서 Instantiate
```

**방법 2: 간단한 Sprite 애니메이션**
```
1. 하트 스프라이트 준비
2. 고양이 위에서 위로 떠오르는 애니메이션
3. 2초 후 사라지기
```

#### B. 코드 수정
```csharp
// CatInteraction.cs
[Header("이펙트")]
[SerializeField] private GameObject _heartEffectPrefab;

private void PlayReactionEffect()
{
    // 하트 이펙트 생성
    if (_heartEffectPrefab != null)
    {
        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        GameObject heart = Instantiate(_heartEffectPrefab, spawnPos, Quaternion.identity);
        Destroy(heart, 2f); // 2초 후 삭제
    }

    // 야옹 사운드 재생 (선택)
    // AudioSource.PlayClipAtPoint(meowSound, transform.position);
}
```

---

## 3. Day 시스템 (24시간 사이클)

### 컨셉
- **실시간 연동**: 실제 시간 → 게임 내 시간
- **가속 시간**: 실제 1분 = 게임 1시간 (조정 가능)

### 구현 방법

#### A. TimeManager 싱글톤
```csharp
// Scripts/Managers/TimeManager.cs
public class TimeManager : MonoBehaviour
{
    private static TimeManager _instance;
    public static TimeManager Instance => _instance;

    [Header("시간 설정")]
    [SerializeField] private float _timeScale = 60f; // 1분 = 1시간
    [SerializeField] private int _startHour = 8; // 시작 시간 (오전 8시)

    private float _currentTime; // 0~24 (시간)
    private int _currentDay = 1; // 생후 몇 일

    public int CurrentHour => Mathf.FloorToInt(_currentTime);
    public int CurrentMinute => Mathf.FloorToInt((_currentTime % 1) * 60);
    public int CurrentDay => _currentDay;

    private void Awake()
    {
        _instance = this;
        _currentTime = _startHour;
    }

    private void Update()
    {
        // 시간 진행
        _currentTime += (Time.deltaTime / 60f) * _timeScale;

        // 24시간 넘으면 다음 날
        if (_currentTime >= 24f)
        {
            _currentTime -= 24f;
            _currentDay++;
            OnNewDay();
        }
    }

    private void OnNewDay()
    {
        Debug.Log($"새로운 날! Day {_currentDay}");
        // 고양이 나이 증가, 상태 초기화 등
    }

    public string GetTimeString()
    {
        int hour = CurrentHour;
        string period = hour < 12 ? "오전" : "오후";
        if (hour > 12) hour -= 12;
        if (hour == 0) hour = 12;
        return $"{period} {hour}:{CurrentMinute:D2}";
    }
}
```

#### B. Day에 따른 효과
- **배고픔**: 시간 경과로 증가
- **졸림**: 밤 9시~새벽 6시는 졸린 시간
- **성장**: Day 7 → Day 30 → Day 90 (성장 단계)
- **날씨**: 하루마다 랜덤 변경

---

## 4. UI 시계

### 디자인
```
┌──────────────────┐
│  ⏰ 오후 2:30    │
│  📅 Day 5        │
└──────────────────┘
```

### 구현

#### A. Canvas 설정
```
Canvas (Screen Space - Overlay)
  └─ StatusBar (상단 앵커)
       ├─ ClockText (TextMeshPro)
       └─ DayText (TextMeshPro)
```

#### B. ClockUI 스크립트
```csharp
// Scripts/UI/ClockUI.cs
public class ClockUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private TextMeshProUGUI _dayText;

    private void Update()
    {
        if (TimeManager.Instance != null)
        {
            _timeText.text = TimeManager.Instance.GetTimeString();
            _dayText.text = $"Day {TimeManager.Instance.CurrentDay}";
        }
    }
}
```

---

## 5. 창문 (날씨 표시)

### 컨셉
```
[창문 스프라이트]
  - 맑음: ☀️ 밝은 배경
  - 흐림: ☁️ 회색 배경
  - 비: 🌧️ 빗방울 파티클
  - 밤: 🌙 어두운 배경 + 별
```

### 구현

#### A. 날씨 시스템
```csharp
// Scripts/Managers/WeatherManager.cs
public enum Weather { Sunny, Cloudy, Rainy }

public class WeatherManager : MonoBehaviour
{
    [SerializeField] private Weather _currentWeather = Weather.Sunny;
    [SerializeField] private SpriteRenderer _windowSprite;
    [SerializeField] private ParticleSystem _rainEffect;

    [Header("창문 스프라이트")]
    [SerializeField] private Sprite _sunnyWindow;
    [SerializeField] private Sprite _cloudyWindow;
    [SerializeField] private Sprite _rainyWindow;
    [SerializeField] private Sprite _nightWindow;

    private void Update()
    {
        UpdateWindow();
    }

    private void UpdateWindow()
    {
        int hour = TimeManager.Instance.CurrentHour;
        bool isNight = hour >= 20 || hour < 6;

        if (isNight)
        {
            _windowSprite.sprite = _nightWindow;
        }
        else
        {
            _windowSprite.sprite = _currentWeather switch
            {
                Weather.Sunny => _sunnyWindow,
                Weather.Cloudy => _cloudyWindow,
                Weather.Rainy => _rainyWindow,
                _ => _sunnyWindow
            };
        }

        // 비 오면 파티클 활성화
        if (_currentWeather == Weather.Rainy && !isNight)
        {
            if (!_rainEffect.isPlaying) _rainEffect.Play();
        }
        else
        {
            if (_rainEffect.isPlaying) _rainEffect.Stop();
        }
    }

    public void ChangeWeather(Weather newWeather)
    {
        _currentWeather = newWeather;
    }
}
```

---

## 6. 밥그릇 아이콘

### 컨셉
```
[밥그릇 스프라이트]
  - 가득 참: 🍚 (방금 줌)
  - 반 정도: 🍚 (반투명)
  - 텅 빔: 💢 (배고픔 표시)
```

### 구현

#### A. 밥그릇 UI
```csharp
// Scripts/UI/FoodBowlUI.cs
public class FoodBowlUI : MonoBehaviour
{
    [SerializeField] private Image _bowlImage;
    [SerializeField] private Sprite _fullBowl;
    [SerializeField] private Sprite _emptyBowl;

    private void Update()
    {
        // CatState에서 배고픔 가져오기
        float hunger = GetHungerLevel();

        if (hunger > 50f)
        {
            _bowlImage.sprite = _emptyBowl; // 배고픔
        }
        else
        {
            _bowlImage.sprite = _fullBowl; // 배부름
        }

        // 투명도로 표현
        _bowlImage.color = new Color(1, 1, 1, hunger / 100f);
    }

    // 밥그릇 클릭 시 먹이 주기
    public void OnFeedButtonClicked()
    {
        Debug.Log("밥 먹이기!");
        // CatState.Hunger = 0
        // 친밀도 증가
    }
}
```

---

## 7. PNG 에셋 필요 목록

### ChatGPT에 요청할 것

#### 필수 (Day 2~4)
1. **하트 이펙트** (💖)
   - 크기: 64x64px
   - PNG, 투명 배경
   - 분홍색 하트

2. **시계 아이콘** (⏰)
   - 크기: 128x128px
   - 귀여운 스타일

3. **밥그릇** (🍚)
   - 크기: 128x128px
   - 2종류: 가득 참 / 텅 빔

4. **창문** (🪟)
   - 크기: 256x256px
   - 4종류: 맑음 / 흐림 / 비 / 밤
   - 2D 카툰 스타일

#### 선택 (나중에)
5. **날씨 아이콘** (☀️☁️🌧️)
6. **음식 아이콘** (간식, 사료)
7. **장난감 아이콘** (공, 쥐 인형)

---

## 구현 우선순위

### Day 2: 상태 시스템 & 기본 UI
- [ ] TimeManager 구현 (24시간 사이클)
- [ ] ClockUI 구현 (시계 표시)
- [ ] 하트 이펙트 추가 (CatInteraction)

### Day 3: 환경 시스템
- [ ] WeatherManager 구현
- [ ] 창문 UI 추가
- [ ] 밥그릇 UI 추가
- [ ] 배고픔 시스템 (시간 경과로 증가)

### Day 4: 채팅 UI
- [ ] ChatPanel 구현
- [ ] Claude API 연동
- [ ] 대화 로그 표시

---

## PNG 생성 프롬프트 예시

### ChatGPT에게 요청할 프롬프트

```
"2D 모바일 게임용 PNG 아이콘 생성해줘. 투명 배경, 귀여운 카툰 스타일.

1. 하트 이펙트 (64x64px)
   - 분홍색 하트
   - 반짝이는 효과

2. 나무 창문 프레임 (256x256px)
   - 맑은 날: 파란 하늘, 태양
   - 흐린 날: 회색 하늘, 구름
   - 비 오는 날: 빗방울
   - 밤: 달과 별

3. 고양이 밥그릇 (128x128px)
   - 가득 찬 상태: 사료 보임
   - 빈 상태: 빈 그릇만

4. 시계 아이콘 (128x128px)
   - 귀여운 벽시계 디자인
"
```

---

## 요약

### ✅ 바로 가능한 것
1. **채팅 UI**: Day 4에 이미 계획됨 → 그대로 진행
2. **하트 이펙트**: Day 2에 추가 (간단)
3. **시계 UI**: Day 2에 추가 (TimeManager 필요)

### 🎨 PNG 필요한 것
- 하트, 시계, 밥그릇, 창문 (4종류)
- ChatGPT에 요청하거나, 무료 에셋 사용 가능

### 📅 타임라인
- **Day 2**: TimeManager + 시계 UI + 하트 이펙트
- **Day 3**: 날씨 시스템 + 창문 + 밥그릇
- **Day 4**: 채팅 UI (이미 계획됨)

---

**ChatGPT에 PNG 생성 요청하시겠어요? 아니면 무료 에셋 사이트 추천해드릴까요?**
