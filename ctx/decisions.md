# Decisions (ADR-lite)

## YYYY-MM-DD — [결정 제목]
- Decision:
- Why:
- Alternatives:
- Consequences / Follow-up:

## 2025-12-27 — Manager 싱글톤 사용 정책
- Decision: Manager는 기본적으로 씬 참조 우선. 싱글톤은 씬 전환에도 유지가 필요한 경우에만 사용.
- Why: 초기 과설계/전역 의존을 줄이고, 테스트/유지보수성을 확보.
- Alternatives: 모든 Manager 싱글톤 강제 / 완전 DI 컨테이너 도입
- Consequences / Follow-up: 싱글톤을 쓰는 Manager는 이유와 DontDestroyOnLoad 여부를 함께 기록.
