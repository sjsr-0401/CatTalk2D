# CatTalk2D 개발 로드맵 (게임 코어 우선)

## Day 1: Cat Core Loop (no AI chat yet)

### 목표
고양이 오브젝트 1마리 + 화면 안 이동/정지(Idle/Walk) + 클릭/탭 반응

### 작업 목록
- [ ] Unity 프로젝트 구조 설정
  - `Assets/_Project/` 폴더 생성
  - `Scripts/Cat/`, `Prefabs/Cat/`, `Scenes/` 하위 폴더 구조
- [ ] 기본 씬 생성 (Scenes/GameScene.unity)
- [ ] 2D 고양이 오브젝트
  - Placeholder 스프라이트 (임시 이미지, 나중에 교체 가능)
  - Prefabs/Cat/CatCharacter.prefab
  - SpriteRenderer + Collider2D (클릭 감지용)
- [ ] 입력 처리 스크립트 (Scripts/Cat/InputHandler.cs)
  - 마우스 클릭 + 모바일 탭 공통 처리
  - 화면 클릭 위치 감지
  - 고양이 오브젝트 클릭 감지
- [ ] 이동 스크립트 (Scripts/Cat/CatMovement.cs)
  - 목표 위치로 이동 (Lerp 또는 MoveTowards)
  - Idle/Walk 상태 전환
  - 이동 속도 조절 가능
- [ ] 반응 스크립트 (Scripts/Cat/CatInteraction.cs)
  - 클릭 시 반응 (하트 이펙트/야옹 로그/기분 증가)
  - 간단한 피드백 시스템
- [ ] 기본 상태 데이터 모델 (Scripts/Models/CatState.cs)
  - 기분(Mood), 친밀도(Affection) 변수
  - Inspector 노출 (SerializeField)

### 구현 메모
- **입력**: 마우스 클릭 + 모바일 탭 모두 고려 (공통 입력 처리)
- **스프라이트/아트**: Placeholder로 진행 가능 (나중에 교체)
- **스크립트 경로**: `Assets/_Project/Scripts/Cat/` 사용
- **이동 방식**: 2D 평면에서 Transform.position 직접 조작 또는 Rigidbody2D 사용
- **상태 전환**: Idle ↔ Walk (애니메이션 없이 속도 0/1로 구분 가능)

### 완료 조건 (AC)
1. **AC1**: 고양이가 화면 안에서 이동/정지(Idle/Walk) 반복
   - 화면 아무 곳이나 클릭 → 고양이가 해당 위치로 이동
   - 도착하면 Idle 상태로 전환
2. **AC2**: 클릭/탭 시 반응
   - 고양이를 직접 클릭 → 반응 (예: 하트 이펙트/야옹 로그/기분 증가 로그)
   - Console에 반응 로그 출력 확인
3. **AC3**: Play 30초, Console Error 0
   - Unity Play 모드에서 30초간 에러 없이 실행
   - 상태 변수가 Inspector에 노출되어 실시간 변경 확인 가능

---

## Day 2: 상태 시스템 & UI

### 작업 목록
- [ ] 상태 시스템 구현 (Scripts/Managers/CatStateManager.cs)
  - 기분 (Mood): Happy, Normal, Sad
  - 친밀도 (Affection): 0~100
  - 배고픔 (Hunger): 0~100
- [ ] 시간 경과에 따른 상태 변화 로직
- [ ] 상호작용에 따른 상태 업데이트
- [ ] 기본 UI (Prefabs/UI/StatusPanel.prefab)
  - 상태 바 (친밀도, 배고픔)
  - 현재 기분 아이콘/텍스트
- [ ] 상태에 따른 고양이 행동 변화 (idle 애니메이션, 이동 속도 등)

### 완료 조건 (AC)
1. UI에 실시간으로 상태가 표시된다
2. 클릭/시간 경과로 상태가 변경된다
3. 상태에 따라 고양이 행동이 달라진다 (예: Sad일 때 느리게 이동)

---

## Day 3: 저장/로드 시스템

### 작업 목록
- [ ] 저장 데이터 구조 설계 (Scripts/Data/SaveData.cs)
  - CatState 직렬화
  - 마지막 플레이 시간 기록
- [ ] JSON 직렬화/역직렬화 구현
- [ ] 파일 저장/로드 기능 (Scripts/Utils/SaveLoadManager.cs)
- [ ] 앱 시작 시 자동 로드
- [ ] 앱 종료/백그라운드 시 자동 저장
- [ ] 저장 데이터 리셋 기능 (디버그용)

### 완료 조건 (AC)
1. 게임 종료 후 재시작해도 고양이 상태가 유지된다
2. persistentDataPath에 JSON 파일이 생성된다
3. 저장 실패 시 에러 로그가 출력된다

---

## Day 4: 채팅 UI (선택적)

### 작업 목록
- [ ] 채팅 UI 프리팹 생성 (Prefabs/UI/ChatPanel.prefab)
  - 메시지 표시 영역 (ScrollView)
  - 입력 필드 (InputField)
  - 전송 버튼
- [ ] 메시지 아이템 프리팹 (Prefabs/UI/MessageItem.prefab)
- [ ] 채팅 매니저 (Scripts/Managers/ChatManager.cs)
- [ ] Message 데이터 모델 (Scripts/Models/Message.cs)
- [ ] 더미 응답 시스템 (고양이가 랜덤 텍스트로 대답)

### 완료 조건 (AC)
1. 채팅 입력 → 메시지 로그에 표시
2. 고양이가 더미 텍스트로 자동 응답
3. 스크롤이 자동으로 최신 메시지로 이동

---

## Day 5: Claude API 연동 (선택적)

### 작업 목록
- [ ] API 키 관리 구조 설계 (StreamingAssets/config.json)
- [ ] HTTP 요청 매니저 구현 (Scripts/API/APIClient.cs)
- [ ] Claude API 요청/응답 데이터 모델
- [ ] Claude Messages API 통합
- [ ] 에러 핸들링 (네트워크 실패, API 오류)
- [ ] 로딩 상태 UI 표시
- [ ] 고양이 상태를 프롬프트에 반영
- [ ] 실제 대화 테스트

### 완료 조건 (AC)
1. 채팅 입력 → Claude API 호출 → 응답 표시
2. 네트워크 오류 시 사용자에게 알림
3. 고양이 상태(기분/친밀도)가 대화 톤에 반영됨

---

## Day 6: 모바일 입력 & 리뷰

### 작업 목록
- [ ] 터치 입력 지원 (화면 탭 = 클릭)
- [ ] 모바일 UI 크기/레이아웃 조정
- [ ] Android 빌드 테스트
- [ ] 코드 리뷰 체크리스트 작성
- [ ] Codex 리뷰 프롬프트 최적화
- [ ] 리팩토링: 코드 정리 및 주석 추가

### 완료 조건 (AC)
1. Android 기기에서 터치로 조작 가능
2. UI가 모바일 화면에 맞게 표시됨
3. Codex 리뷰 완료 및 주요 피드백 반영

---

## Day 7: 폴리싱 & 빌드

### 작업 목록
- [ ] 전체 워크플로우 테스트 (게임 루프 → 저장/로드 → 채팅)
- [ ] 버그 수정 및 안정화
- [ ] UI/UX 개선 (애니메이션, 사운드 추가)
- [ ] 문서 업데이트 (README, conventions)
- [ ] Windows/Android 빌드 테스트
- [ ] 플레이 가능한 MVP 완성

### 완료 조건 (AC)
1. 게임이 10분간 에러 없이 실행됨
2. Windows + Android 빌드 성공
3. README에 빌드/실행 방법 문서화

---

## 마일스톤

- **M1** (Day 3): 플레이 가능한 고양이 게임 (이동/상태/저장)
- **M2** (Day 5): 채팅 기능 추가 (Claude 연동 선택적)
- **M3** (Day 7): 모바일 빌드 완성

## 참고사항
- 각 Day는 대략적인 가이드이며, 실제 진행에 따라 조정 가능
- **Day 1~3은 필수, Day 4~5는 선택적** (게임 코어만으로도 완성)
- 주요 기능 완료 시 즉시 git 커밋
- AI 리뷰는 Day별 종료 시점에 수행 권장
- 완료 조건(AC)은 각 Day 종료 시 체크리스트로 활용
