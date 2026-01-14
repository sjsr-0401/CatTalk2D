# CatTalk2D 프로젝트 학습 가이드

## 프로젝트 전체 구조 이해

### 게임 개요
2D 고양이 키우기 게임으로, 사용자가 고양이와 상호작용하고 채팅할 수 있습니다.

### 핵심 기능
1. **고양이 상호작용**: 클릭, 터치, 먹이 주기
2. **채팅 시스템**: AI와 대화 (Ollama 연동)
3. **성장 시스템**: 호감도 기반 성숙도 증가
4. **시간 관리**: 실시간 시계, 달력
5. **리소스 관리**: 밥, 장난감, 상점

---

## Unity 핵심 개념

### 1. GameObject와 Component
```csharp
// GameObject: Unity의 모든 오브젝트
GameObject catObj = new GameObject("Cat");

// Component: GameObject에 붙는 기능 모듈
SpriteRenderer spriteRenderer = catObj.AddComponent<SpriteRenderer>();
```

**주요 개념**:
- GameObject는 빈 컨테이너
- Component를 추가해서 기능 부여
- Transform은 모든 GameObject가 가지는 기본 Component

### 2. MonoBehaviour 생명주기
```csharp
public class Example : MonoBehaviour
{
    void Awake()    // 가장 먼저 실행 (인스턴스 생성 직후)
    void Start()    // Awake 이후, 첫 프레임 전
    void Update()   // 매 프레임마다 실행
    void FixedUpdate() // 고정된 시간 간격으로 실행 (물리)
    void OnDestroy()   // 오브젝트 파괴 시
}
```

**실행 순서**:
1. Awake (모든 오브젝트)
2. Start (모든 오브젝트)
3. Update/FixedUpdate (반복)
4. OnDestroy (파괴 시)

### 3. Prefab (프리팹)
- 재사용 가능한 GameObject 템플릿
- Scene에 여러 개 배치 가능
- 원본 수정 시 모든 인스턴스에 반영

**사용 예**:
```
CatCharacter.prefab → Scene에서 여러 마리 배치 가능
```

### 4. Scene (씬)
- 게임의 한 화면/레벨
- GameObject들의 모음
- Scene 전환으로 화면 변경

**프로젝트 구조**:
```
Scenes/
└── Main.unity (메인 게임 씬)
```

---

## C# 기초 개념

### 1. 클래스와 객체
```csharp
// 클래스 정의 (설계도)
public class Cat
{
    public string name;
    public int age;

    public void Meow()
    {
        Debug.Log("야옹!");
    }
}

// 객체 생성 (실제 인스턴스)
Cat myCat = new Cat();
myCat.name = "망고";
myCat.Meow();
```

### 2. 접근 제한자
```csharp
public string publicVar;      // 어디서나 접근 가능
private string privateVar;    // 클래스 내부에서만 접근
protected string protectedVar; // 자식 클래스에서도 접근
```

**Unity 관례**:
- public: Inspector 노출, 외부 접근 필요
- private: 내부 전용, `[SerializeField]`로 Inspector 노출
- `_`로 시작: private 필드 (예: `_messageFont`)

### 3. 프로퍼티 (Property)
```csharp
private int _health;

public int Health
{
    get { return _health; }
    set { _health = Mathf.Max(0, value); } // 0 이하 방지
}

// 자동 프로퍼티
public int MaxHealth { get; private set; } = 100;
```

**사용 이유**:
- 값 검증
- 읽기 전용/쓰기 전용 설정
- 계산된 값 반환

### 4. 네임스페이스
```csharp
namespace CatTalk2D.UI
{
    public class ChatUI : MonoBehaviour
    {
        // ...
    }
}

// 사용
using CatTalk2D.UI;
```

**프로젝트 구조**:
- `CatTalk2D.Cat`: 고양이 관련
- `CatTalk2D.UI`: UI 관련
- `CatTalk2D.Managers`: 매니저
- `CatTalk2D.API`: API 연동

### 5. 제네릭 (Generic)
```csharp
List<string> messages = new List<string>();
Dictionary<string, int> inventory = new Dictionary<string, int>();
```

**자주 사용하는 제네릭**:
- `List<T>`: 동적 배열
- `Dictionary<K, V>`: 키-값 쌍
- `Action<T>`: 파라미터 있는 델리게이트

---

## Unity 디자인 패턴

### 1. 싱글톤 패턴
```csharp
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance => _instance;

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
}

// 사용
GameManager.Instance.SomeMethod();
```

**장점**:
- 전역 접근 가능
- 하나의 인스턴스만 보장

**단점**:
- 테스트 어려움
- 강한 결합

**프로젝트 사용 예**:
- `ChatUI.Instance`
- `OllamaAPIManager.Instance`

### 2. 이벤트 시스템
```csharp
// UnityEvent 사용
[SerializeField] private UnityEvent onGameStart;

private void Start()
{
    onGameStart?.Invoke();
}

// C# 이벤트
public event System.Action OnCatFed;

public void FeedCat()
{
    OnCatFed?.Invoke();
}
```

### 3. 코루틴 패턴
```csharp
// 순차 실행
IEnumerator SequenceCoroutine()
{
    Debug.Log("시작");
    yield return new WaitForSeconds(1f);
    Debug.Log("1초 후");
    yield return new WaitForSeconds(2f);
    Debug.Log("3초 후");
}

// 조건 대기
IEnumerator WaitUntilCondition()
{
    yield return new WaitUntil(() => isReady);
    Debug.Log("준비 완료!");
}

// 다른 코루틴 대기
IEnumerator MainCoroutine()
{
    yield return StartCoroutine(SubCoroutine());
    Debug.Log("서브 코루틴 완료");
}
```

---

## TextMeshPro 심화

### 1. Rich Text 태그
```csharp
// 색상
"<color=red>빨간 텍스트</color>"
"<#FF0000>빨간 텍스트</>"

// 크기
"<size=50>큰 텍스트</size>"

// 스타일
"<b>굵게</b> <i>기울임</i> <u>밑줄</u>"

// 스프라이트
"안녕하세요 <sprite=0> 이모지"
```

### 2. 폰트 에셋 구조
```
TMP_FontAsset
├── Face Info (폰트 메타데이터)
├── Atlas Textures (문자 이미지들)
├── Character Table (문자 매핑)
├── Glyph Table (렌더링 정보)
└── Fallback Font Assets (대체 폰트)
```

### 3. Dynamic vs Static
**Dynamic**:
- 런타임에 문자 추가
- 메모리 효율적
- 초기 로딩 빠름
- Atlas 크기 제한 있음

**Static**:
- 미리 모든 문자 생성
- 빠른 렌더링
- 큰 메모리 사용
- 한글 전체 포함 어려움

---

## Unity UI (UGUI) 심화

### 1. RectTransform 앵커 시스템
```
Anchors (앵커):
┌─────────────┐
│ ●     ●     ●│  Min(0,1)  Max(1,1)
│             │
│ ●     ●     ●│  Min(0,0.5) Max(1,0.5)
│             │
│ ●     ●     ●│  Min(0,0)  Max(1,0)
└─────────────┘
```

**용도**:
- 화면 크기 변경 시 UI 위치 유지
- 다양한 해상도 지원

### 2. Layout 그룹
```csharp
// Horizontal: 가로 배치
HorizontalLayoutGroup hLayout = obj.AddComponent<HorizontalLayoutGroup>();
hLayout.spacing = 10;
hLayout.childAlignment = TextAnchor.MiddleCenter;

// Vertical: 세로 배치
VerticalLayoutGroup vLayout = obj.AddComponent<VerticalLayoutGroup>();
vLayout.childAlignment = TextAnchor.LowerCenter;

// Grid: 격자 배치
GridLayoutGroup gLayout = obj.AddComponent<GridLayoutGroup>();
gLayout.cellSize = new Vector2(100, 100);
```

### 3. Content Size Fitter
```csharp
ContentSizeFitter fitter = obj.AddComponent<ContentSizeFitter>();
fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
```

**Fit Mode**:
- `Unconstrained`: 크기 고정
- `MinSize`: 최소 크기
- `PreferredSize`: 선호 크기 (내용물에 맞춤)

### 4. ScrollRect
```csharp
ScrollRect scroll = obj.AddComponent<ScrollRect>();
scroll.content = contentTransform;
scroll.viewport = viewportTransform;
scroll.vertical = true;
scroll.horizontal = false;
scroll.verticalNormalizedPosition = 0f; // 맨 아래
```

---

## 데이터 저장/로드

### 1. PlayerPrefs (간단한 데이터)
```csharp
// 저장
PlayerPrefs.SetString("CatName", "망고");
PlayerPrefs.SetInt("CatAge", 7);
PlayerPrefs.Save();

// 로드
string catName = PlayerPrefs.GetString("CatName", "기본이름");
int catAge = PlayerPrefs.GetInt("CatAge", 0);
```

### 2. JSON 저장 (복잡한 데이터)
```csharp
[System.Serializable]
public class SaveData
{
    public string catName;
    public int catAge;
    public float affection;
}

// 저장
SaveData data = new SaveData();
string json = JsonUtility.ToJson(data, true);
File.WriteAllText(savePath, json);

// 로드
string json = File.ReadAllText(savePath);
SaveData data = JsonUtility.FromJson<SaveData>(json);
```

### 3. 저장 위치
```csharp
// PC (Windows/Mac)
Application.persistentDataPath
// C:/Users/Username/AppData/LocalLow/CompanyName/GameName

// Android
// /storage/emulated/0/Android/data/com.company.game/files

// iOS
// /var/mobile/Containers/Data/Application/...
```

---

## 디버깅 기법

### 1. 로그 레벨
```csharp
Debug.Log("일반 정보");          // 흰색
Debug.LogWarning("경고");        // 노란색
Debug.LogError("에러");          // 빨간색
```

### 2. 조건부 로그
```csharp
#if UNITY_EDITOR
    Debug.Log("에디터에서만 출력");
#endif

[System.Diagnostics.Conditional("UNITY_EDITOR")]
void DebugLog(string message)
{
    Debug.Log(message);
}
```

### 3. Gizmos (씬 뷰 시각화)
```csharp
void OnDrawGizmos()
{
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, 1f);
}
```

### 4. Inspector 실시간 확인
```csharp
[SerializeField] private int _currentHealth; // 실시간 값 확인
```

---

## 성능 최적화 기초

### 1. Object Pooling
```csharp
// 나쁜 예: 매번 생성/삭제
void SpawnEnemy()
{
    Instantiate(enemyPrefab);
}

// 좋은 예: 재사용
List<GameObject> enemyPool = new List<GameObject>();

GameObject GetEnemy()
{
    foreach (var enemy in enemyPool)
    {
        if (!enemy.activeInHierarchy)
            return enemy;
    }

    var newEnemy = Instantiate(enemyPrefab);
    enemyPool.Add(newEnemy);
    return newEnemy;
}
```

### 2. Update 최적화
```csharp
// 나쁜 예: 매 프레임 찾기
void Update()
{
    GameObject cat = GameObject.Find("Cat");
}

// 좋은 예: 캐싱
private GameObject _cat;

void Start()
{
    _cat = GameObject.Find("Cat");
}

void Update()
{
    // _cat 사용
}
```

### 3. 문자열 최적화
```csharp
// 나쁜 예: + 연산자
string text = "Score: " + score + " Level: " + level;

// 좋은 예: StringBuilder
StringBuilder sb = new StringBuilder();
sb.Append("Score: ").Append(score);
sb.Append(" Level: ").Append(level);
string text = sb.ToString();
```

---

## Git 기본 사용법

### 기본 명령어
```bash
# 상태 확인
git status

# 변경사항 추가
git add .

# 커밋
git commit -m "메시지"

# 푸시
git push

# 풀
git pull

# 브랜치 생성
git branch feature/new-feature

# 브랜치 전환
git checkout feature/new-feature
```

### Unity .gitignore
```
# Unity
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/

# Visual Studio
.vs/
*.csproj
*.sln
```

---

## 프로젝트별 주요 스크립트 설명

### ChatUI.cs
**역할**: 채팅 UI 전체 관리
**주요 기능**:
- 메시지 전송/수신
- ScrollView 관리
- AI API 호출

**핵심 메서드**:
```csharp
AddUserMessage(string message)  // 사용자 메시지 추가
AddCatMessage(string message)   // 고양이 메시지 추가
ScrollToBottom()                 // 스크롤 맨 아래로
```

### MessageBubble.cs
**역할**: 메시지 말풍선 생성
**주요 기능**:
- 고양이/사용자 말풍선 동적 생성
- 레이아웃 자동 조절

**핵심 메서드**:
```csharp
CreateCatMessage()   // 고양이 메시지 생성
CreateUserMessage()  // 사용자 메시지 생성
```

### CatInteraction.cs
**역할**: 고양이 상호작용 처리
**주요 기능**:
- 클릭 감지
- 하트 이펙트
- 호감도 증가

### OllamaAPIManager.cs
**역할**: AI API 연동
**주요 기능**:
- Ollama 로컬 서버 통신
- 대화 기록 관리
- 성장 단계별 프롬프트

---

## 다음 학습 주제

### 단기 (1-2주)
- [ ] Unity UI 마스터
- [ ] C# 비동기 프로그래밍
- [ ] JSON 데이터 처리
- [ ] Git 브랜치 전략

### 중기 (1-2개월)
- [ ] Unity 애니메이션 시스템
- [ ] 모바일 최적화
- [ ] 네트워크 통신
- [ ] 디자인 패턴

### 장기 (3-6개월)
- [ ] Unity 셰이더 기초
- [ ] AI/ML 통합
- [ ] 멀티플랫폼 빌드
- [ ] 게임 배포 프로세스

---

## 추천 학습 자료

### 공식 문서
- Unity Manual: https://docs.unity3d.com/Manual/
- C# Documentation: https://learn.microsoft.com/ko-kr/dotnet/csharp/

### 온라인 강의
- Unity Learn: https://learn.unity.com/
- Brackeys (YouTube): Unity 입문 강의

### 커뮤니티
- Unity Forum: https://forum.unity.com/
- Stack Overflow: 문제 해결

---

## 프로젝트 관리 팁

### 1. 규칙적인 커밋
- 기능 단위로 커밋
- 명확한 커밋 메시지
- 작동하는 상태에서만 커밋

### 2. 백업
- Git 원격 저장소
- 정기적인 빌드 보관
- 중요 에셋 별도 백업

### 3. 문서화
- daily-logs에 작업 기록
- 주석으로 코드 설명
- README 업데이트

### 4. 테스트
- 기능 추가 후 즉시 테스트
- 다양한 시나리오 검증
- 에러 로그 확인

---

## 문제 해결 체크리스트

### Unity 에러 발생 시
1. Console 에러 메시지 확인
2. 스택 트레이스 확인
3. 관련 스크립트 확인
4. Inspector 설정 확인
5. Scene 저장 여부 확인

### 빌드 실패 시
1. Build Settings 확인
2. 플랫폼 설정 확인
3. 필수 에셋 포함 확인
4. 플러그인 호환성 확인

### 성능 문제 시
1. Profiler 실행
2. Update 최적화
3. 메모리 사용량 확인
4. Batching 확인
