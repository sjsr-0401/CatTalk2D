# Unity 프로젝트 규칙 및 컨벤션

## 프로젝트 구조

```
Assets/
  _Project/               # 모든 프로젝트 파일은 여기에
    Scenes/               # 씬 파일
    Scripts/              # C# 스크립트
      Managers/           # 싱글톤 매니저
      Models/             # 데이터 모델
      UI/                 # UI 관련 스크립트
      API/                # API 통신
      Utils/              # 유틸리티
    Prefabs/              # 프리팹
      UI/                 # UI 프리팹
      Characters/         # 캐릭터 관련
    Resources/            # Resources.Load용
    Materials/            # 머티리얼
    Sprites/              # 스프라이트
    Fonts/                # 폰트
  StreamingAssets/        # 런타임 파일 (config 등)
```

## 명명 규칙

### 씬 (Scenes)
- PascalCase 사용
- 예: `MainScene.unity`, `ChatScene.unity`

### 프리팹 (Prefabs)
- PascalCase 사용
- 기능 명확히 표현
- 예: `ChatPanel.prefab`, `MessageItem.prefab`, `UserAvatar.prefab`

### 스크립트 (Scripts)
- PascalCase, 파일명 = 클래스명
- Manager 접미사: 싱글톤 매니저
- 예: `ChatManager.cs`, `Message.cs`, `APIClient.cs`

### 변수/메서드
- 변수: camelCase (`messageText`, `isConnected`)
- Private 필드: `_camelCase` (`_chatHistory`, `_apiKey`)
- 메서드: PascalCase (`SendMessage()`, `LoadData()`)
- 상수: UPPER_SNAKE_CASE (`MAX_MESSAGE_LENGTH`)

## Git 및 메타 파일

### 필수 설정
- **Edit > Project Settings > Editor**
  - Version Control Mode: `Visible Meta Files`
  - Asset Serialization Mode: `Force Text`

### .gitignore 규칙
- `/Library/` - Unity 캐시 (절대 커밋 금지)
- `/Temp/` - 임시 파일 (절대 커밋 금지)
- `/Logs/` - 로그 파일
- `*.csproj`, `*.sln` - IDE 생성 파일 (선택적)
- **주의**: `.meta` 파일은 **반드시 포함**

### 커밋 규칙
- 모든 에셋과 함께 `.meta` 파일 커밋
- 씬 변경 시 씬 파일과 `.meta` 함께 커밋
- 의미 있는 단위로 커밋 (기능별, 파일 그룹별)

## Unity 작업 제약사항

### Unity 에디터에서만 수행
- 씬 파일 편집
- 프리팹 생성/수정
- 에셋 이동/이름 변경 (메타 파일 동기화 위해)
- Prefab 계층 구조 변경

### 텍스트 에디터/AI로 가능
- C# 스크립트 작성/수정
- JSON 데이터 편집
- Markdown 문서 작성
- 설정 파일 수정

## 코드 스타일

### C# 스타일
```csharp
public class ChatManager : MonoBehaviour
{
    private static ChatManager _instance;
    public static ChatManager Instance => _instance;

    [SerializeField] private Transform _messageContainer;
    private List<Message> _messages = new List<Message>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void SendMessage(string content)
    {
        // 구현
    }
}
```

### 주석 규칙
- Public API는 XML 주석 사용
- 복잡한 로직에만 인라인 주석
- 불필요한 주석 지양 (자명한 코드)

## 의존성 관리

### 외부 패키지
- Package Manager 통해 설치
- `Packages/manifest.json`에 기록됨
- 버전 명시 권장

### 플러그인
- `Assets/_Project/Plugins/` 하위에 배치
- 라이선스 파일 함께 포함
