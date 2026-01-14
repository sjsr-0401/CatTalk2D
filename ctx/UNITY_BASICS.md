# Unity 기초 용어 및 개념 완벽 정리

## Unity Editor 윈도우 구조

### 1. Hierarchy 창
**위치**: 왼쪽 상단
**역할**: 현재 Scene에 있는 모든 GameObject의 트리 구조 표시

```
Main
├─ Main Camera
├─ Background
├─ Cat
└─ Canvas
    ├─ ChatPanel
    │   ├─ MessageScrollView
    │   └─ InputPanel
    └─ WindowPanel
```

**주요 기능**:
- GameObject 생성/삭제
- 부모-자식 관계 설정 (드래그 앤 드롭)
- GameObject 활성화/비활성화 (왼쪽 체크박스)
- 검색 (상단 검색창)

**사용 팁**:
- GameObject를 다른 GameObject로 드래그하면 자식으로 만들 수 있음
- 우클릭 → Create Empty: 빈 GameObject 생성
- 눈 아이콘: Scene에서만 숨김 (빌드에는 포함)

---

### 2. Scene 뷰
**위치**: 중앙
**역할**: 게임 세계를 편집하는 3D/2D 뷰

**조작법**:
- **마우스 휠**: 확대/축소
- **마우스 중간 버튼 드래그**: 화면 이동
- **마우스 우클릭 드래그**: 화면 회전 (3D)
- **Q, W, E, R, T**: 도구 전환
  - Q: Hand Tool (화면 이동)
  - W: Move Tool (이동)
  - E: Rotate Tool (회전)
  - R: Scale Tool (크기)
  - T: Rect Tool (2D UI 조정)

**2D 모드**:
- Scene 뷰 상단 "2D" 버튼 클릭
- 2D 게임 개발 시 평면 뷰로 작업

---

### 3. Game 뷰
**위치**: Scene 뷰 탭 옆
**역할**: 실제 게임이 보이는 화면 (플레이어 시점)

**Play 모드**:
- 상단 ▶ 버튼: 게임 실행
- ⏸ 버튼: 일시 정지
- ⏭ 버튼: 한 프레임씩 진행

**주의사항**:
- ⚠️ Play 모드에서 변경한 사항은 종료 시 사라짐!
- Play 모드 진입 시 Editor가 약간 어두워짐 (구분용)

---

### 4. Inspector 창
**위치**: 오른쪽
**역할**: 선택한 GameObject의 모든 Component 표시 및 설정

**구성**:
```
Inspector
├─ GameObject 정보 (이름, Tag, Layer)
├─ Transform (위치, 회전, 크기)
├─ Component 1
│   ├─ 공개 변수 (public)
│   └─ SerializeField 변수
├─ Component 2
└─ Add Component 버튼
```

**주요 기능**:
- Component 추가/제거
- public/SerializeField 변수 값 설정
- 다른 GameObject/Component 연결 (드래그 앤 드롭)
- Component 활성화/비활성화 (왼쪽 체크박스)

---

### 5. Project 창
**위치**: 하단
**역할**: 프로젝트의 모든 에셋(파일) 관리

**폴더 구조**:
```
Assets/
├─ _Project/          # 프로젝트 에셋
│   ├─ Scenes/
│   ├─ Scripts/
│   ├─ Prefabs/
│   └─ Sprites/
├─ TextMesh Pro/      # TMP 에셋
└─ StreamingAssets/   # 런타임 로드 에셋
```

**파일 타입**:
- `.cs`: C# 스크립트
- `.unity`: Scene 파일
- `.prefab`: Prefab (재사용 가능한 GameObject)
- `.png, .jpg`: 이미지
- `.asset`: Unity 에셋 (폰트, 설정 등)

---

### 6. Console 창
**위치**: 하단 (Project 창 탭 옆)
**역할**: 로그, 경고, 에러 메시지 표시

**메시지 종류**:
- 💬 **Log** (흰색): `Debug.Log("메시지")`
- ⚠️ **Warning** (노란색): `Debug.LogWarning("경고")`
- ❌ **Error** (빨간색): `Debug.LogError("에러")`

**기능**:
- Clear: 모든 로그 삭제
- Collapse: 중복 메시지 묶기
- 필터: Log/Warning/Error 표시 토글
- 메시지 클릭: 해당 코드로 이동

---

## Unity 핵심 개념

### Scene (씬)
**정의**: 게임의 한 화면 또는 레벨

**예시**:
```
- Main.unity (메인 게임)
- TitleScreen.unity (타이틀 화면)
- GameOver.unity (게임 오버 화면)
```

**Scene 전환**:
```csharp
using UnityEngine.SceneManagement;

SceneManager.LoadScene("Main");
SceneManager.LoadScene(0); // 빌드 인덱스
```

**특징**:
- 하나의 Scene = 하나의 `.unity` 파일
- 여러 Scene을 동시에 로드 가능 (Additive)
- Scene에는 GameObject들의 상태가 저장됨

---

### GameObject (게임 오브젝트)
**정의**: Unity의 모든 것의 기본 단위 (빈 컨테이너)

**생성 방법**:
```csharp
// 코드로 생성
GameObject obj = new GameObject("MyObject");

// 프리팹 생성
GameObject obj = Instantiate(prefab);
```

**특징**:
- GameObject 자체는 아무 기능이 없음
- Component를 추가해서 기능 부여
- 모든 GameObject는 Transform Component를 가짐

---

### Component (컴포넌트)
**정의**: GameObject에 붙어서 기능을 제공하는 모듈

**기본 Component**:
1. **Transform**: 위치, 회전, 크기
2. **SpriteRenderer**: 2D 이미지 표시
3. **Rigidbody2D**: 물리 시뮬레이션
4. **Collider2D**: 충돌 감지
5. **Canvas**: UI 렌더링

**Component 추가**:
```csharp
// 코드로 추가
SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();

// Component 가져오기
SpriteRenderer sr = GetComponent<SpriteRenderer>();

// 자식에서 찾기
SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
```

---

### Transform (트랜스폼)
**정의**: GameObject의 위치, 회전, 크기 정보

**종류**:
1. **Transform**: 3D 공간
2. **RectTransform**: 2D UI 공간

**주요 속성**:
```csharp
// 위치
transform.position = new Vector3(0, 0, 0);  // 월드 좌표
transform.localPosition = Vector3.zero;      // 부모 기준 좌표

// 회전
transform.rotation = Quaternion.identity;
transform.eulerAngles = new Vector3(0, 90, 0);

// 크기
transform.localScale = new Vector3(1, 1, 1);

// 부모-자식
transform.parent = parentTransform;
transform.SetParent(parentTransform, false);

// 자식 접근
Transform child = transform.GetChild(0);
int childCount = transform.childCount;
```

---

### MonoBehaviour 생명주기 (Lifecycle)

**실행 순서**:
```
게임 시작
    ↓
1. Awake()      ← GameObject 생성 직후 (씬 로드 시)
    ↓
2. OnEnable()   ← GameObject 활성화 시
    ↓
3. Start()      ← 첫 프레임 시작 전
    ↓
4. FixedUpdate() ← 고정 시간 간격 (물리 업데이트)
    ↓
5. Update()     ← 매 프레임마다
    ↓
6. LateUpdate() ← 모든 Update 후
    ↓
7. OnDisable()  ← GameObject 비활성화 시
    ↓
8. OnDestroy()  ← GameObject 파괴 시
```

**자세한 설명**:

#### 1. Awake()
```csharp
void Awake()
{
    // 가장 먼저 실행됨
    // 싱글톤 초기화, 컴포넌트 찾기 등
}
```
**사용 예**:
- 싱글톤 패턴 설정
- 다른 스크립트가 필요로 하는 초기화
- 컴포넌트 참조 저장

**특징**:
- GameObject가 비활성화 상태여도 실행됨
- Start()보다 먼저 실행됨
- 모든 GameObject의 Awake가 먼저 실행된 후 Start 실행

#### 2. Start()
```csharp
void Start()
{
    // Awake 후 실행
    // 다른 GameObject/Component에 접근 안전
}
```
**사용 예**:
- 다른 스크립트의 싱글톤 접근
- 초기 UI 설정
- 게임 시작 로직

**Awake vs Start**:
- **Awake**: 내 것만 초기화
- **Start**: 다른 것도 참조 가능

#### 3. Update()
```csharp
void Update()
{
    // 매 프레임마다 실행
    // 프레임 시간 간격이 불규칙함
}
```
**사용 예**:
- 키보드/마우스 입력 처리
- UI 업데이트
- 일반적인 게임 로직

**프레임률**:
- 60fps: 1초에 60번 실행 (약 16ms마다)
- 30fps: 1초에 30번 실행 (약 33ms마다)

**주의**:
- 성능에 민감 (매 프레임 실행)
- 무거운 작업은 피할 것

#### 4. FixedUpdate()
```csharp
void FixedUpdate()
{
    // 고정 시간 간격으로 실행 (기본 0.02초 = 50fps)
    // 물리 업데이트용
}
```
**사용 예**:
- Rigidbody 조작
- 물리 계산
- 정확한 시간 간격이 필요한 작업

**Update vs FixedUpdate**:
- **Update**: 프레임마다 (불규칙)
- **FixedUpdate**: 고정 간격 (규칙적)

#### 5. LateUpdate()
```csharp
void LateUpdate()
{
    // 모든 Update() 실행 후
}
```
**사용 예**:
- 카메라 따라가기
- 애니메이션 후처리

---

### 좌표 시스템

#### 1. World Space (월드 공간)
```csharp
transform.position = new Vector3(0, 0, 0);
```
- 절대 좌표
- Scene 전체 기준

#### 2. Local Space (로컬 공간)
```csharp
transform.localPosition = new Vector3(0, 0, 0);
```
- 부모 기준 상대 좌표

**예시**:
```
부모 Position: (10, 0, 0)
자식 LocalPosition: (5, 0, 0)
→ 자식의 World Position: (15, 0, 0)
```

#### 3. Screen Space (스크린 공간)
```csharp
Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
```
- 화면 픽셀 좌표
- 왼쪽 아래 (0, 0), 오른쪽 위 (width, height)

---

### Prefab (프리팹)
**정의**: 재사용 가능한 GameObject 템플릿

**생성**:
1. Hierarchy에서 GameObject 선택
2. Project 창으로 드래그
3. `.prefab` 파일 생성

**사용**:
```csharp
// 프리팹 생성
GameObject obj = Instantiate(prefab);

// 위치 지정
GameObject obj = Instantiate(prefab, position, rotation);

// 부모 지정
GameObject obj = Instantiate(prefab, parent);
```

**특징**:
- 원본 Prefab 수정 시 모든 인스턴스에 반영
- Override 가능 (일부만 다르게 설정)

---

### SerializeField
**정의**: private 변수를 Inspector에 노출

```csharp
[SerializeField] private int _health = 100;
```

**public vs SerializeField**:

| 특징 | public | SerializeField |
|------|--------|----------------|
| Inspector 노출 | ✅ | ✅ |
| 외부 접근 | ✅ | ❌ |
| 캡슐화 | ❌ | ✅ |

**권장**:
- SerializeField 사용 (캡슐화 유지)
- 외부 접근이 필요하면 프로퍼티 사용

```csharp
[SerializeField] private int _health = 100;

public int Health
{
    get => _health;
    set => _health = Mathf.Max(0, value);
}
```

---

### Tag와 Layer

#### Tag (태그)
**정의**: GameObject 분류용 문자열

**사용**:
```csharp
// Tag 확인
if (gameObject.CompareTag("Player"))
{
    Debug.Log("플레이어 발견!");
}

// Tag로 찾기
GameObject player = GameObject.FindGameObjectWithTag("Player");
```

**기본 Tag**:
- Untagged (기본값)
- Player
- MainCamera
- Respawn
- Finish
- EditorOnly

#### Layer (레이어)
**정의**: GameObject 그룹화 (충돌, 렌더링 제어)

**사용**:
```csharp
// Layer 설정
gameObject.layer = LayerMask.NameToLayer("UI");

// LayerMask (충돌 필터링)
int layerMask = LayerMask.GetMask("UI", "Player");
```

**용도**:
- 카메라 렌더링 필터
- Raycast 충돌 필터
- 물리 충돌 필터

---

### Time 클래스

```csharp
// 프레임 시간 (매 프레임 다름)
float deltaTime = Time.deltaTime;

// 게임 시작 후 경과 시간
float time = Time.time;

// FixedUpdate 시간 간격
float fixedDeltaTime = Time.fixedDeltaTime;

// 타임 스케일 (게임 속도)
Time.timeScale = 0f;  // 일시정지
Time.timeScale = 0.5f; // 느리게
Time.timeScale = 2f;   // 빠르게
```

**이동 예시**:
```csharp
void Update()
{
    // ❌ 잘못된 방법 (프레임률에 따라 속도 다름)
    transform.position += Vector3.right * 5f;

    // ✅ 올바른 방법 (일정한 속도)
    transform.position += Vector3.right * 5f * Time.deltaTime;
}
```

---

### Input 시스템

#### 1. 키보드
```csharp
void Update()
{
    // 키가 눌려 있는 동안
    if (Input.GetKey(KeyCode.Space))
    {
        Debug.Log("스페이스 홀드");
    }

    // 키를 누른 순간 (한 번만)
    if (Input.GetKeyDown(KeyCode.Space))
    {
        Debug.Log("스페이스 다운");
    }

    // 키를 뗀 순간
    if (Input.GetKeyUp(KeyCode.Space))
    {
        Debug.Log("스페이스 업");
    }
}
```

#### 2. 마우스
```csharp
void Update()
{
    // 마우스 버튼 (0=좌클릭, 1=우클릭, 2=중간)
    if (Input.GetMouseButtonDown(0))
    {
        Vector3 mousePos = Input.mousePosition;
        Debug.Log($"클릭 위치: {mousePos}");
    }

    // 마우스 월드 좌표 변환
    Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
}
```

#### 3. 터치 (모바일)
```csharp
void Update()
{
    if (Input.touchCount > 0)
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            Debug.Log("터치 시작");
        }
    }
}
```

---

### Coroutine (코루틴)

**정의**: 비동기 작업을 순차적으로 처리

```csharp
// 코루틴 시작
StartCoroutine(MyCoroutine());

// 코루틴 정의
IEnumerator MyCoroutine()
{
    Debug.Log("시작");

    // 1초 대기
    yield return new WaitForSeconds(1f);
    Debug.Log("1초 후");

    // 다음 프레임까지 대기
    yield return null;
    Debug.Log("다음 프레임");

    // 조건까지 대기
    yield return new WaitUntil(() => isReady);
    Debug.Log("준비 완료");

    // 다른 코루틴 대기
    yield return StartCoroutine(OtherCoroutine());
}

// 코루틴 중지
StopCoroutine(coroutine);
StopAllCoroutines();
```

**사용 예**:
- 타이머
- 애니메이션
- API 통신
- 순차적 이벤트

---

### Instantiate와 Destroy

#### Instantiate (생성)
```csharp
// 프리팹 생성
GameObject obj = Instantiate(prefab);

// 위치/회전 지정
GameObject obj = Instantiate(prefab, position, Quaternion.identity);

// 부모 지정
GameObject obj = Instantiate(prefab, parent);
```

#### Destroy (파괴)
```csharp
// GameObject 파괴
Destroy(gameObject);

// Component만 파괴
Destroy(GetComponent<Rigidbody2D>());

// 3초 후 파괴
Destroy(gameObject, 3f);

// 즉시 파괴 (주의!)
DestroyImmediate(gameObject);
```

---

### DontDestroyOnLoad

**정의**: Scene 전환 시에도 파괴되지 않음

```csharp
void Awake()
{
    DontDestroyOnLoad(gameObject);
}
```

**사용 예**:
- 게임 매니저
- 오디오 매니저
- 플레이어 데이터

---

## Unity가 Windows에서 작동하는 방식

### 1. 프로젝트 구조
```
CatTalk2D/
├─ Assets/              # 게임 에셋 (코드, 이미지 등)
├─ Library/             # Unity가 생성한 캐시 (Git 제외)
├─ Logs/                # 로그 파일
├─ Packages/            # 패키지 설정
├─ ProjectSettings/     # 프로젝트 설정
├─ Temp/                # 임시 파일 (Git 제외)
└─ UserSettings/        # 사용자 설정 (Git 제외)
```

### 2. 빌드 프로세스
```
C# 스크립트 작성
    ↓
Unity Editor에서 컴파일 (자동)
    ↓
.dll 파일 생성 (Library/)
    ↓
Play 모드 또는 빌드
    ↓
실행 파일 생성 (.exe)
```

### 3. 저장 위치

**PlayerPrefs**:
```
레지스트리: HKCU\Software\[CompanyName]\[ProductName]
```

**persistentDataPath**:
```
Windows: C:\Users\[Username]\AppData\LocalLow\[CompanyName]\[ProductName]
Android: /storage/emulated/0/Android/data/[com.company.product]/files
iOS: /var/mobile/Containers/Data/Application/[GUID]/Documents
```

### 4. Unity Editor 실행 과정
```
Unity Hub 실행
    ↓
프로젝트 선택
    ↓
Unity Editor 로드
    ↓
Scene 로드 (Main.unity)
    ↓
모든 GameObject의 Awake() 실행
    ↓
모든 GameObject의 Start() 실행
    ↓
게임 루프 시작 (Update 반복)
```

---

## 실전 팁

### 1. 성능 최적화
```csharp
// ❌ 나쁜 예
void Update()
{
    GetComponent<Rigidbody2D>().velocity = Vector2.zero;
}

// ✅ 좋은 예
private Rigidbody2D _rb;

void Awake()
{
    _rb = GetComponent<Rigidbody2D>();
}

void Update()
{
    _rb.velocity = Vector2.zero;
}
```

### 2. Null 체크
```csharp
// ❌ NullReferenceException 발생 가능
_cat.Meow();

// ✅ 안전
if (_cat != null)
{
    _cat.Meow();
}

// ✅ 더 간결하게
_cat?.Meow();
```

### 3. 디버그 로그
```csharp
Debug.Log("일반 로그");
Debug.LogWarning("경고");
Debug.LogError("에러");

// 조건부 로그 (에디터에서만)
#if UNITY_EDITOR
    Debug.Log("에디터 전용 로그");
#endif
```

---

## 자주 묻는 질문 (FAQ)

### Q1: Awake와 Start의 차이는?
**A**:
- **Awake**: GameObject 생성 직후, 다른 스크립트보다 먼저
- **Start**: 모든 Awake 실행 후, 다른 스크립트 참조 가능

### Q2: Update와 FixedUpdate의 차이는?
**A**:
- **Update**: 매 프레임 (불규칙), 입력/UI 처리
- **FixedUpdate**: 고정 간격 (규칙적), 물리 계산

### Q3: GameObject와 Component의 관계는?
**A**: GameObject는 빈 컨테이너, Component가 기능 제공

### Q4: Scene과 Prefab의 차이는?
**A**:
- **Scene**: 게임 화면 (.unity 파일)
- **Prefab**: 재사용 가능한 GameObject 템플릿 (.prefab 파일)

### Q5: public vs SerializeField?
**A**:
- **public**: 외부 접근 가능, Inspector 노출
- **SerializeField**: 외부 접근 불가, Inspector 노출 (캡슐화 유지)

---

## 다음 학습 주제

1. ✅ Unity Editor 인터페이스 이해
2. ✅ GameObject와 Component 개념
3. ✅ MonoBehaviour 생명주기
4. ⬜ Unity 이벤트 시스템
5. ⬜ Unity UI 고급
6. ⬜ 애니메이션 시스템
7. ⬜ 물리 시스템
8. ⬜ 오디오 시스템
