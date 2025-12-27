# AI 워크플로우 개발 루프

## 체크리스트

### 1. Claude (구현 단계)
- [ ] 구현할 기능 명확히 정의
- [ ] `skills/claude_feature.md` 템플릿 참고하여 프롬프트 작성
- [ ] Claude에게 기능 구현 요청
- [ ] 생성된 코드 Unity에서 테스트
- [ ] 변경사항 커밋 전 검토 (Library/Temp 제외 확인)

### 2. Codex (검증 단계)
- [ ] `skills/codex_review.md` 템플릿 참고하여 리뷰 요청
- [ ] Codex에게 변경된 파일 목록과 코드 제공
- [ ] 리뷰 피드백 수신 및 검토
- [ ] 필요시 Claude 또는 Codex에게 수정 요청
- [ ] 최종 검증 완료

> **참고**: Claude 사용량이 한계에 도달하면 Codex가 구현도 수행 가능

### 3. Save (저장 단계)
- [ ] `daily-logs/YYYY-MM-DD.md` 작성 (오늘 목표/변경 요약/테스트/배운 점/내일 할 일)
- [ ] 작업 중 발견한 패턴을 `ctx/conventions.md`에 추가
- [ ] 새로운 제약사항이 있다면 `ctx/rules.md`에 기록
- [ ] 유용한 프롬프트는 `skills/` 폴더에 템플릿화
- [ ] `ctx/roadmap.md` 진행 상황 업데이트

### 4. Sync (동기화 단계)
- [ ] 변경된 파일 목록 확인 (`git status`)
- [ ] `.meta` 파일 포함 여부 확인
- [ ] `daily-logs/YYYY-MM-DD.md` 포함 확인
- [ ] 의미 있는 커밋 메시지 작성
- [ ] 커밋 및 푸시
- [ ] 다음 작업 항목으로 이동 → 1단계로 순환

---

## 빠른 참조

| 단계 | 주 담당 | 출력물 |
|------|---------|--------|
| 0. Load | 개발자 | 최신 코드 |
| 1. Claude | Claude | 코드, 에셋 |
| 2. Codex | Codex | 리뷰 피드백 |
| 3. Save | 개발자 | 일지, 문서 |
| 4. Sync | Git | 커밋 |


### 0.Load

- [ ] git pull로 최신 가져오기

- [ ] Unity 열어서 자동 임포트/컴파일

- [ ] 씬/프리팹/플레이 테스트