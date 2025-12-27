# Codex 리뷰 프롬프트 템플릿

## 기본 템플릿

```
프로젝트: CatTalk2D (Unity 2D 채팅 앱)

리뷰 요청: [파일명 또는 기능명]

목표: [구현 목표]

변경된 파일:
- [파일1 경로]
- [파일2 경로]

리뷰 포인트:
1. 코드 품질 및 가독성
2. Unity 베스트 프랙티스 준수
3. 에러 핸들링 적절성
4. 성능 이슈 여부
5. 보안 취약점 (API 키 노출 등)

제약사항 확인:
- ctx/conventions.md 명명 규칙
- ctx/rules.md 금지사항
- .meta 파일 포함 여부

코드:
[변경된 코드 전체 또는 diff]
```

## 예시 1: 새 스크립트 리뷰

```
프로젝트: CatTalk2D

리뷰 요청: ChatManager.cs (신규 작성)

목표: 채팅 메시지 관리 싱글톤 매니저 구현

변경된 파일:
- Assets/_Project/Scripts/Managers/ChatManager.cs (신규)
- Assets/_Project/Scripts/Managers/ChatManager.cs.meta (신규)

리뷰 포인트:
1. 싱글톤 패턴이 올바르게 구현되었는가?
2. List<Message> 관리가 thread-safe한가?
3. UI 업데이트 로직이 효율적인가?
4. null 체크가 충분한가?
5. 메모리 누수 가능성은 없는가?

코드:
```csharp
[전체 ChatManager.cs 코드]
```

추가 질문:
- Awake vs Start 중 초기화 위치가 적절한가?
- Inspector에 노출할 필드가 적절히 선택되었는가?
```

## 예시 2: API 통합 리뷰

```
프로젝트: CatTalk2D

리뷰 요청: APIClient.cs (Claude API 통합)

목표: Claude Messages API 호출 및 응답 처리

변경된 파일:
- Assets/_Project/Scripts/API/APIClient.cs (신규)
- Assets/StreamingAssets/config.json (신규)

리뷰 포인트:
1. API 키가 코드에 하드코딩되지 않았는가?
2. HTTP 요청/응답 에러 핸들링이 충분한가?
3. JSON 파싱 실패 시나리오를 고려했는가?
4. 코루틴 사용이 적절한가?
5. 타임아웃 설정이 있는가?

보안 체크:
- config.json이 .gitignore에 포함되어야 하는가?
- API 키 노출 위험은 없는가?

코드:
```csharp
[APIClient.cs 전체 코드]
```

```json
// config.json
[config.json 내용]
```

질문:
- UnityWebRequest 대신 다른 HTTP 클라이언트를 고려해야 하는가?
- 응답 캐싱이 필요한가?
```

## 예시 3: 리팩토링 리뷰

```
프로젝트: CatTalk2D

리뷰 요청: ChatManager.cs 리팩토링

목표: 메시지 전송 로직을 비동기로 개선

변경된 파일:
- Assets/_Project/Scripts/Managers/ChatManager.cs (수정)

변경 내용:
- SendMessage()를 SendMessageAsync()로 변경
- 콜백 패턴에서 async/await 패턴으로 전환
- UI 업데이트 로직 분리

리뷰 포인트:
1. async/await 사용이 Unity에서 안전한가?
2. UI 스레드 처리가 올바른가?
3. 이전 코드와의 호환성은?
4. 가독성이 개선되었는가?
5. 에러 핸들링이 누락되지 않았는가?

Diff:
```diff
[변경 전후 diff]
```

질문:
- Task 대신 UniTask를 고려해야 하는가?
- 기존 콜백 방식도 병행 지원해야 하는가?
```

## 예시 4: 커밋 전 전체 리뷰

```
프로젝트: CatTalk2D

리뷰 요청: Day 1 완료 - 기본 채팅 UI 구축

목표: 첫 번째 마일스톤 커밋 전 전체 검토

변경된 파일:
- Assets/_Project/Scenes/MainScene.unity (수정)
- Assets/_Project/Scripts/Managers/ChatManager.cs (신규)
- Assets/_Project/Scripts/UI/MessageItem.cs (신규)
- Assets/_Project/Scripts/Models/Message.cs (신규)
- Assets/_Project/Prefabs/UI/ChatPanel.prefab (신규)
- Assets/_Project/Prefabs/UI/MessageItem.prefab (신규)
- [각각의 .meta 파일들]

리뷰 포인트:
1. 전체 아키텍처가 확장 가능한가?
2. 파일 구조가 ctx/conventions.md를 따르는가?
3. 명명 규칙이 일관적인가?
4. 커밋하면 안 되는 파일(Library, Temp)이 포함되지 않았는가?
5. .meta 파일이 모두 포함되었는가?
6. 코드 중복이 있는가?
7. 다음 단계(Day 2)로 진행 가능한 상태인가?

Git 상태:
```
git status 출력 결과
```

종합 평가 요청:
- 커밋해도 되는 상태인가?
- 개선이 필요한 부분은?
- Day 2 진행 전 해결해야 할 이슈는?
```

## 리뷰 결과 활용

### 리뷰 피드백 분류
1. **Critical**: 즉시 수정 필요 (버그, 보안 이슈)
2. **Important**: 커밋 전 수정 권장 (베스트 프랙티스 위반)
3. **Nice-to-have**: 추후 개선 고려 (최적화, 리팩토링)

### 피드백 처리 프로세스
1. Critical 이슈 → Claude/Codex에게 즉시 수정 요청
2. Important 이슈 → 현재 작업에 포함하여 수정
3. Nice-to-have → `ctx/roadmap.md`에 TODO 추가

### 학습 및 축적
- 반복되는 피드백 → `ctx/rules.md`에 규칙 추가
- 유용한 패턴 → `ctx/conventions.md`에 예시 추가
- 새로운 템플릿 → `skills/` 폴더에 추가

## 프롬프트 작성 팁

### 컨텍스트 충분히 제공
- 변경 목표와 배경 설명
- 관련 파일 전체 경로
- 프로젝트 규칙 참조

### 구체적인 리뷰 요청
- "코드 리뷰해줘" (X)
- "싱글톤 패턴 구현과 메모리 누수 가능성 검토" (O)

### 코드 전체 제공
- 일부만 제공하면 맥락 부족
- 파일 전체 또는 의미 있는 단위로 제공

### 질문 포함
- 불확실한 부분 명시
- 대안 제시 요청
- 트레이드오프 설명 요청

## 사용 방법

1. Claude로 구현 완료
2. 위 템플릿 선택 (신규/리팩토링/커밋 전)
3. 변경 파일 및 코드 삽입
4. Codex에게 리뷰 요청
5. 피드백 분류 및 처리
6. Critical/Important 이슈 해결 후 커밋
