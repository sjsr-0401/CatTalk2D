# Claude 기능 구현 프롬프트 템플릿

## 기본 템플릿

```
프로젝트: CatTalk2D (Unity 2D 채팅 앱)

구현할 기능: [기능명]

요구사항:
- [요구사항 1]
- [요구사항 2]
- [요구사항 3]

제약사항:
- ctx/conventions.md의 명명 규칙 준수
- ctx/rules.md의 금지사항 확인
- Assets/_Project 구조 내에서 작업

출력 형식:
1. 변경될 파일 목록 먼저 제시
2. 코드 작성/수정
3. 테스트 방법 안내
4. 다음 단계 제안
```

## 예시 1: UI 컴포넌트 생성

```
프로젝트: CatTalk2D

구현할 기능: 채팅 메시지 아이템 프리팹 스크립트

요구사항:
- MessageItem.cs 스크립트 생성
- Text 컴포넌트로 메시지 내용 표시
- 발신자(User/AI)에 따라 배경색 변경
- 타임스탬프 표시 기능

제약사항:
- Scripts/UI/ 폴더에 배치
- SerializeField로 Inspector 노출
- null 체크 필수

참고:
- Message 모델: Scripts/Models/Message.cs
- 컨벤션: ctx/conventions.md

출력 형식:
1. 파일 목록
2. MessageItem.cs 전체 코드
3. Inspector 설정 방법
4. 프리팹 연결 가이드
```

## 예시 2: 데이터 모델 정의

```
프로젝트: CatTalk2D

구현할 기능: 채팅 메시지 데이터 모델

요구사항:
- Message 클래스 정의
- 필드: id, content, sender(enum), timestamp
- JSON 직렬화 가능하도록 [Serializable] 적용
- 생성자 및 기본값 설정

제약사항:
- Scripts/Models/ 폴더에 배치
- immutable 속성 고려 (readonly 또는 { get; private set; })
- SenderType enum도 같은 파일에 정의

출력:
1. Message.cs 전체 코드
2. 사용 예시 코드 스니펫
3. JSON 직렬화 테스트 방법
```

## 예시 3: API 통합

```
프로젝트: CatTalk2D

구현할 기능: Claude API 통신 매니저

요구사항:
- APIClient.cs 싱글톤 구현
- HTTP POST 요청으로 Claude Messages API 호출
- 응답을 Message 객체로 파싱
- 에러 핸들링 (네트워크 오류, API 오류)
- 코루틴으로 비동기 처리

제약사항:
- API 키는 StreamingAssets/config.json에서 로드
- UnityWebRequest 사용 (외부 패키지 금지)
- Scripts/API/ 폴더에 배치

참고:
- Claude API 문서: https://docs.anthropic.com/en/api
- Message 모델: Scripts/Models/Message.cs

출력:
1. APIClient.cs 전체 코드
2. config.json 구조 예시
3. 사용 예시 (ChatManager에서 호출)
4. 에러 처리 시나리오
```

## 예시 4: 저장/로드 시스템

```
프로젝트: CatTalk2D

구현할 기능: 채팅 내역 로컬 저장/로드

요구사항:
- SaveLoadManager 유틸리티 클래스
- List<Message>를 JSON 파일로 저장
- Application.persistentDataPath 사용
- 앱 시작 시 자동 로드
- 저장 실패 시 에러 로깅

제약사항:
- Scripts/Utils/ 폴더에 배치
- 정적 메서드로 구현 (싱글톤 불필요)
- File I/O 예외 처리 필수

출력:
1. SaveLoadManager.cs 전체 코드
2. SaveData wrapper 클래스 (List 직렬화용)
3. ChatManager에서 호출 예시
4. 저장 경로 확인 방법
```

## 프롬프트 작성 팁

### 명확한 요구사항
- 구체적인 기능 명세
- 입출력 정의
- 예상 동작 시나리오

### 제약사항 명시
- 파일 위치
- 사용 가능한 라이브러리
- 금지 사항 (ctx/rules.md 참조)

### 컨텍스트 제공
- 관련 파일 경로
- 연동될 다른 컴포넌트
- 참고 문서 링크

### 출력 형식 지정
- 파일 목록 우선
- 코드 전체 제공 요청
- 사용 예시 포함
- 다음 단계 제안

## 사용 방법

1. 위 템플릿 복사
2. [기능명], [요구사항] 등 채우기
3. ctx/ 문서 참조하여 제약사항 추가
4. Claude에게 프롬프트 전달
5. 결과 검토 후 Codex 리뷰 진행
