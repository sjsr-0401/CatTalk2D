# LoRA 튜닝 용어 정리집

## 기본 개념

### LoRA (Low-Rank Adaptation)
- **정의**: 대형 언어 모델(LLM)을 효율적으로 미세조정하는 기법
- **원리**: 전체 모델 가중치를 수정하지 않고, 작은 "어댑터" 행렬만 학습
- **장점**: 메모리 사용량 대폭 감소, 빠른 학습, 원본 모델 보존

### Fine-tuning (미세조정)
- **정의**: 사전학습된 모델을 특정 작업에 맞게 추가 학습시키는 과정
- **용도**: 고양이 말투, 특정 캐릭터 성격 등 커스텀 동작 학습

### JSONL (JSON Lines)
- **정의**: 각 줄이 독립적인 JSON 객체인 텍스트 파일 형식
- **확장자**: `.jsonl`
- **용도**: LoRA 학습 데이터셋의 표준 형식

---

## 데이터셋 구조

### Chat Format (대화 형식)
```json
{
  "messages": [
    {"role": "system", "content": "시스템 프롬프트"},
    {"role": "user", "content": "사용자 입력"},
    {"role": "assistant", "content": "AI 응답"}
  ]
}
```

### Role (역할)
| Role | 설명 |
|------|------|
| `system` | AI의 기본 성격/규칙 설정 |
| `user` | 사용자(주인)의 입력 |
| `assistant` | AI(고양이)의 응답 |

---

## Control JSON 필드

### schemaVersion
- **타입**: string
- **예시**: `"1.0"`
- **용도**: 데이터 형식 버전 관리

### catName
- **타입**: string
- **예시**: `"망고"`
- **용도**: 고양이 이름

### ageLevel
- **타입**: string
- **값**: `"child"` | `"teen"` | `"adult"`
- **기준**:
  - child: 0~29일
  - teen: 30~179일
  - adult: 180일 이상

### moodTag
- **타입**: string
- **예시**: `"happy"`, `"tired"`, `"grumpy"`, `"excited"`, `"neutral"`
- **용도**: 현재 기분 상태

### affectionTier
- **타입**: string
- **값**: `"low"` | `"mid"` | `"high"`
- **기준**:
  - low: 0~29
  - mid: 30~69
  - high: 70~100

### personalityTop2
- **타입**: string[]
- **예시**: `["curious", "playful"]`
- **가능 값**: `playful`, `shy`, `aggressive`, `curious`
- **용도**: 가장 높은 성격 특성 2개

### stateSnapshot
- **타입**: object
- **필드**:
  | 필드 | 타입 | 범위 | 설명 |
  |------|------|------|------|
  | hunger | float | 0-100 | 배고픔 (높을수록 배고픔) |
  | energy | float | 0-100 | 에너지 (높을수록 활발) |
  | stress | float | 0-100 | 스트레스 (낮을수록 좋음) |
  | fun | float | 0-100 | 재미 (높을수록 즐거움) |
  | affection | float | 0-100 | 애정도 (높을수록 친밀) |
  | ageDays | int | 0+ | 고양이 나이 (일) |
  | gameDate | string | - | 게임 내 날짜 |

---

## 추출 규칙 용어

### ActionType (행동 타입)
| 타입 | 설명 | 데이터셋 포함 |
|------|------|--------------|
| Talk | 대화 | O |
| Feed | 밥주기 | X |
| Pet | 쓰다듬기 | X |
| Play | 놀아주기 | X |
| Monologue | 혼잣말 | X |

### 필터링 조건
- **빈 응답**: userText 또는 aiText가 비어있으면 제외
- **짧은 응답**: 최소 글자 수 미만이면 제외 (기본: 3자)
- **영어 포함**: 3글자 이상 영어 단어 포함 시 제외 (허용 예외: OK, TV, PC, SNS 등)
- **중복**: 동일한 (userText, aiText) 쌍은 한 번만 포함

---

## 학습 관련 용어

### Epoch
- **정의**: 전체 데이터셋을 한 번 학습하는 단위
- **권장**: 3~5 epoch

### Learning Rate (학습률)
- **정의**: 모델이 한 번에 얼마나 많이 배울지 결정하는 값
- **권장**: 1e-4 ~ 5e-5

### Rank (LoRA 랭크)
- **정의**: LoRA 어댑터 행렬의 크기
- **권장**: 8~64 (작을수록 가볍고, 클수록 표현력 높음)

### Alpha (LoRA 알파)
- **정의**: LoRA 스케일링 파라미터
- **일반 공식**: alpha = rank * 2

---

## 파일 구조

```
Tools/CatDevTools/
├── Services/
│   └── DatasetExporter.cs    # JSONL 변환 서비스
├── Models/
│   └── LogModels.cs          # 로그/데이터셋 모델
├── ViewModels/
│   └── MainViewModel.cs      # UI 바인딩
├── Views/
│   └── AITuningTab.xaml      # 데이터셋 내보내기 UI
└── docs/
    ├── dataset_sample.jsonl  # JSONL 예시 파일
    └── LoRA_Glossary.md      # 이 문서
```

---

## 사용법 요약

1. **로그 폴더 선택**: 게임 로그가 저장된 폴더 경로 지정
2. **옵션 설정**: 영어 제외 여부 등 필터 옵션 설정
3. **내보내기 실행**: "데이터셋 내보내기" 버튼 클릭
4. **결과 확인**: 생성된 `dataset.jsonl` 파일과 통계 확인
5. **LoRA 학습**: 생성된 JSONL로 Unsloth, Axolotl 등 도구로 학습
