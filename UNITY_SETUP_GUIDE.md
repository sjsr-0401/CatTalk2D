# Unity 에디터에서 해야 할 작업

코드는 모두 작성되었습니다! 이제 Unity 에디터에서 UI를 구성하고 스크립트를 연결해야 합니다.

---

## 1. Ollama 설치 및 실행 (먼저!)

### Ollama 설치
1. https://ollama.com/download 접속
2. Windows용 다운로드 및 설치

### 모델 다운로드 및 실행
```bash
# 터미널(CMD) 열기
ollama pull llama2

# Ollama 서버 실행 (백그라운드에서 자동 실행됨)
ollama serve
```

**확인**: 브라우저에서 `http://localhost:11434` 접속 → "Ollama is running" 메시지 확인

---

## 2. 스프라이트 임포트 설정

### 모든 이미지를 Sprite로 설정
Project 창에서 각 이미지 선택 → Inspector에서:
1. **Texture Type**: `Sprite (2D and UI)` 선택
2. **Apply** 클릭

**적용할 파일들**:
- `Assets/_Project/Sprites/UI/` 폴더의 모든 PNG
- `Assets/_Project/Sprites/Effects/hart.png`
- `Assets/_Project/Sprites/Backgrounds/` 폴더의 모든 PNG

---

## 3. 시계 UI 구성

### Hierarchy 구조 만들기
```
Canvas
  └─ ClockPanel (UI > Panel)
       ├─ ClockFace (UI > Image)
       │    └─ HourHand (UI > Image)
       │         └─ MinuteHand (UI > Image)
       └─ TimeText (TextMeshPro - Text)
```

### 설정
1. **ClockFace**
   - Source Image: `clock face` 스프라이트
   - RectTransform 크기: 200x200

2. **HourHand**
   - Source Image: `hour hand` 스프라이트
   - Anchor/Pivot: 중앙 하단 (0.5, 0)
   - RectTransform 크기: 20x80

3. **MinuteHand**
   - Source Image: `minute hand` 스프라이트
   - Anchor/Pivot: 중앙 하단 (0.5, 0)
   - RectTransform 크기: 15x100

4. **ClockPanel에 ClockUI.cs 스크립트 추가**
   - Hour Hand: HourHand 오브젝트 드래그
   - Minute Hand: MinuteHand 오브젝트 드래그
   - Time Text: TimeText 오브젝트 드래그 (선택)

---

## 4. 창문 UI 구성

### Hierarchy 구조
```
Canvas
  └─ WindowPanel (UI > Image)
```

### 설정
1. **WindowPanel**
   - Anchor: 왼쪽 상단 또는 원하는 위치
   - RectTransform 크기: 300x300

2. **WindowPanel에 WindowManager.cs 스크립트 추가**
   - Window Image: 자기 자신(WindowPanel의 Image) 할당
   - Morning Window: `window_morning` 스프라이트
   - Afternoon Window: `window_afternoon` 스프라이트
   - Evening Window: `window_before sunset` 스프라이트
   - Night Window: `window_night` 스프라이트

---

## 5. 밥그릇 UI 구성

### Hierarchy 구조
```
Canvas
  └─ FoodBowlPanel (UI > Panel)
       ├─ BowlImage (UI > Image)
       └─ FeedButton (UI > Button)
            └─ Text (TextMeshPro - Text): "밥 주기"
```

### 설정
1. **BowlImage**
   - Source Image: `empty feed bin` (초기 상태)

2. **FoodBowlPanel에 FoodBowlUI.cs 스크립트 추가**
   - Bowl Image: BowlImage 할당
   - Full Bowl: `Full feed bin` 스프라이트
   - Empty Bowl: `empty feed bin` 스프라이트
   - Feed Button: FeedButton 할당

---

## 6. 하트 이펙트 프리팹 만들기

### 방법
1. **Hierarchy에 빈 GameObject 생성** → 이름: `HeartEffect`
2. **Add Component** → Sprite Renderer
   - Sprite: `hart` 스프라이트
   - Sorting Layer: 높은 값 (예: 10)
3. **Add Component** → `HeartEffect.cs` 스크립트
4. **Prefab으로 저장**
   - HeartEffect를 `Assets/_Project/Prefabs/Effects/` 폴더로 드래그
5. **Hierarchy에서 HeartEffect 삭제**

### Cat 오브젝트에 연결
1. Hierarchy에서 **Cat** 클릭
2. Inspector에서 **CatInteraction** 스크립트 찾기
3. **Heart Effect Prefab**: HeartEffect 프리팹 할당

---

## 7. 채팅 UI 구성 (투명 메신저)

### Hierarchy 구조
```
Canvas
  └─ ChatPanel (UI > Panel)
       ├─ Background (UI > Image) - 투명도 0.8
       ├─ MessageScrollView (UI > Scroll View)
       │    └─ Viewport
       │         └─ MessageContainer (Vertical Layout Group)
       ├─ InputPanel (UI > Panel)
       │    ├─ InputField (UI > Input Field - TextMeshPro)
       │    └─ SendButton (UI > Button)
       │         └─ Text: "전송"
       └─ ProfilePanel
            ├─ CatProfileImage (UI > Image)
            └─ CatNameText (TextMeshPro - Text): "망고"
```

### 설정
1. **ChatPanel**
   - Anchor: 화면 오른쪽
   - RectTransform: 화면 오른쪽 1/3 차지

2. **Background**
   - Color: 흰색, Alpha: 0.8 (투명도)

3. **MessageContainer**
   - Add Component: Vertical Layout Group
   - Spacing: 10
   - Child Force Expand: Width만 체크

4. **ChatPanel에 ChatUI.cs 스크립트 추가**
   - Scroll Rect: MessageScrollView 할당
   - Message Container: MessageContainer 할당
   - Input Field: InputField 할당
   - Send Button: SendButton 할당

---

## 8. 메시지 프리팹 만들기

### 사용자 메시지 프리팹
```
UserMessage (UI > Panel)
  └─ Text (TextMeshPro - Text)
```
- Background 색: 연한 파란색
- 정렬: 오른쪽
- Prefab으로 저장: `Prefabs/UI/UserMessage`

### 고양이 메시지 프리팹
```
CatMessage (UI > Panel)
  ├─ ProfileImage (UI > Image) - 고양이 프로필
  └─ Text (TextMeshPro - Text)
```
- Background 색: 연한 회색
- 정렬: 왼쪽
- Prefab으로 저장: `Prefabs/UI/CatMessage`

### ChatUI에 연결
- User Message Prefab: UserMessage 할당
- Cat Message Prefab: CatMessage 할당

---

## 9. 매니저 오브젝트 설정

### Hierarchy에 빈 GameObject들 생성
```
GameManagers (빈 GameObject)
  ├─ TimeManager (빈 GameObject)
  ├─ OllamaAPIManager (빈 GameObject)
  └─ GameManager (이미 있음 - InputHandler 포함)
```

### 각각 스크립트 추가
1. **TimeManager**
   - Add Component: `TimeManager.cs`

2. **OllamaAPIManager**
   - Add Component: `OllamaAPIManager.cs`
   - Cat Interaction: Cat 오브젝트 할당

---

## 10. Cat 오브젝트에 스크립트 추가

Hierarchy에서 **Cat** 선택 → Inspector → Add Component:
- `CatHunger.cs`
  - Cat Interaction: 자동 할당됨 (같은 오브젝트)
  - Food Bowl: FoodBowlPanel 할당

---

## 11. 테스트

### Play 버튼 클릭 후 확인
- ✅ 시계 시침/분침 회전
- ✅ 시간 지나면 창문 변화
- ✅ 밥 주기 버튼 → 밥그릇 채워짐
- ✅ 시간 지나면 고양이 배고파함
- ✅ 배고프면 밥 먹기 → 하트 나옴
- ✅ 고양이 클릭 → 하트 나옴
- ✅ 채팅 입력 → Ollama 응답 (Ollama 실행 중일 때)

---

## 문제 해결

### Ollama 연결 안 됨
```bash
# 터미널에서 Ollama 재실행
ollama serve

# 모델 재다운로드
ollama pull llama2
```

### 스크립트 컴파일 에러
- Unity 메뉴: Assets → Reimport All

### 하트 안 나옴
- HeartEffect 프리팹이 Cat의 CatInteraction에 할당되었는지 확인

---

## 완료!

모든 설정이 끝나면 **Play 버튼**을 누르고 테스트하세요!
