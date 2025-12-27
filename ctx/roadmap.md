# CatTalk2D 개발 로드맵

## Day 1: 기본 채팅 UI 구축
- [ ] Unity 프로젝트 구조 설정 (Assets/_Project 폴더)
- [ ] 기본 씬 생성 (Scenes/MainScene.unity)
- [ ] 채팅 UI 프리팹 생성 (Prefabs/UI/ChatPanel.prefab)
  - 메시지 표시 영역 (ScrollView)
  - 입력 필드 (InputField)
  - 전송 버튼
- [ ] 메시지 아이템 프리팹 (Prefabs/UI/MessageItem.prefab)
- [ ] 기본 메시지 표시 스크립트 작성

## Day 2: 채팅 상태 관리
- [ ] ChatManager 싱글톤 구현 (Scripts/Managers/ChatManager.cs)
- [ ] Message 데이터 모델 정의 (Scripts/Models/Message.cs)
- [ ] 메시지 리스트 관리 (추가/삭제/조회)
- [ ] UI와 데이터 바인딩 구현
- [ ] 스크롤 자동 이동 기능

## Day 3: 로컬 저장 시스템
- [ ] 저장 데이터 구조 설계 (Scripts/Data/SaveData.cs)
- [ ] JSON 직렬화/역직렬화 구현
- [ ] 파일 저장/로드 기능 (Scripts/Utils/SaveLoadManager.cs)
- [ ] 앱 시작 시 대화 내역 복원
- [ ] 대화 삭제 기능

## Day 4: Claude API 연동 준비
- [ ] API 키 관리 구조 설계 (StreamingAssets/config.json)
- [ ] HTTP 요청 매니저 구현 (Scripts/API/APIClient.cs)
- [ ] Claude API 요청/응답 데이터 모델
- [ ] 테스트용 Mock 응답 구현

## Day 5: Claude API 실제 연결
- [ ] Claude Messages API 통합
- [ ] 스트리밍 응답 처리
- [ ] 에러 핸들링 (네트워크 실패, API 오류)
- [ ] 로딩 상태 UI 표시
- [ ] 실제 대화 테스트

## Day 6: Codex 리뷰 루프 구축
- [ ] 코드 리뷰 체크리스트 작성
- [ ] Codex 리뷰 프롬프트 최적화
- [ ] 리팩토링: 코드 정리 및 주석 추가
- [ ] 단위 테스트 고려 (선택사항)

## Day 7: 통합 및 폴리싱
- [ ] 전체 워크플로우 테스트
- [ ] 버그 수정 및 안정화
- [ ] UI/UX 개선
- [ ] 문서 업데이트 (README, conventions)
- [ ] 최종 빌드 테스트

---

## 마일스톤

- **M1** (Day 3): 오프라인 채팅 앱 (저장/로드 가능)
- **M2** (Day 5): Claude 연동 완료
- **M3** (Day 7): AI 워크플로우 완성

## 참고사항
- 각 Day는 대략적인 가이드이며, 실제 진행에 따라 조정 가능
- 주요 기능 완료 시 즉시 git 커밋
- AI 리뷰는 Day별 종료 시점에 수행 권장
