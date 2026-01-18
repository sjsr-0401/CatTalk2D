# Cat Agent 설계: 학습하고 성장하는 고양이

우리는 Claude Code(메인 구현) + Codex(리뷰/리팩토링/테스트)로 협업한다.

- 너(Claude)는 큰 기능 구현과 설계를 담당한다.
- 구현 후 반드시 "Codex 리뷰 체크리스트"를 출력한다.
- 파일 변경 목록 + 테스트 시나리오를 항상 포함한다.
- 기존 기능(만지기/밥주기/대화/놀기/로그)은 절대 깨지면 안 된다.

Codex 리뷰 체크리스트:
1) NullReference 가능 지점
2) 이벤트 구독/해제(OnEnable/OnDisable) 누락
3) TimeManager 없을 때 안전성
4) 로그 스키마/키 통일
5) 성능(매 프레임/매 시간 작업) 문제


## 컨셉

**생후 7일 새끼 고양이 → 환경과 주인을 학습하며 성장하는 Agent**

---

## AI 개발 방향성 / 핵심 원칙

> **중요**: 이 섹션은 모든 AI 관련 구현/리팩토링/추가 기능에서 최우선으로 따라야 한다.

### 0. 핵심 목표
- 자연스러운 한국어 + 상황/분위기 지시를 잘 따르는 고양이 대사 생성
- "실제 고양이를 키우는 느낌"을 주는 장기 성장(성격/호감도/신뢰) 구조 구현
- 모델(Ollama 등)은 교체 가능해야 하고, 성능 비교/평가/고도화가 가능해야 한다

### 1. 설계 철학: '컨트롤 레이어'가 주도권을 가진다

```
┌─────────────────────────────────────────────────────────┐
│                    게임 로직 (CatState)                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │ 행동 판단   │  │ 상태 변화   │  │ 성격 변화   │     │
│  └─────────────┘  └─────────────┘  └─────────────┘     │
│                         │                               │
│                         ▼                               │
│              ┌─────────────────┐                       │
│              │  Control 생성   │                       │
│              └─────────────────┘                       │
└─────────────────────│───────────────────────────────────┘
                      ▼
┌─────────────────────────────────────────────────────────┐
│                    LLM (문장 생성기)                     │
│              "Control을 받아 대사만 생성"                │
└─────────────────────────────────────────────────────────┘
```

**핵심**: LLM은 '판단 엔진'이 아니라 '문장 생성기'로 사용한다.
- 행동 판단/상태 변화/호감도/성격 변화는 반드시 게임 로직(규칙 기반, CatState)에서 결정
- LLM에게는 Control(분위기/말투/길이/상태 요약)을 전달하고, 그에 맞는 대사만 생성하게 함

### 2. Control 입력 규격 (매 요청 동일)

| 필드 | 타입 | 값 범위 | 설명 |
|------|------|---------|------|
| `ageLevel` | enum | child / teen / adult | 성장 단계 |
| `moodTag` | enum | affectionate / neutral / annoyed / stressed / hungry / playful / bored / tired / happy | 현재 기분 |
| `tone` | enum | polite / casual | 말투 |
| `length` | enum | short | 1~2문장 고정 |
| `affectionTier` | enum | low / mid / high | 친밀도 단계 |
| `catState.Hunger` | float | 0~100 | 배고픔 |
| `catState.Energy` | float | 0~100 | 에너지 |
| `catState.Stress` | float | 0~100 | 스트레스 |
| `catState.Fun` | float | 0~100 | 재미 |
| `catState.Affection` | float | 0~100 | 호감도 |
| `catState.Trust` | float | 0~100 | 신뢰도 (Phase 2) |
| `personalityTop2` | string[] | Playful / Curious / Shy / Aggressive | 성격 상위 2개 |

> **참고**: Control 입력 규격은 고양이에 대한 정보를 더 수집한 후 확장할 수 있다. 모든 항목은 수치화 가능해야 한다.

### 3. 출력 규칙 (강제)

- **한국어만 사용** (영어/로마자/이모지 금지)
- **1~2문장 짧게**
- **과한 밈/유행어 금지**
- 상황(moodTag)과 성격(personalityTop2)을 대사 톤에 반영
- 동일 입력에 대해 일관된 캐릭터 유지

### 4. 안정장치: 후처리 필터

```csharp
// 영어/비한국어 문자 감지 시
if (ContainsNonKorean(response))
{
    // 1회 재생성 시도
    // 실패 시 fallback 응답 사용
}

// 응답이 너무 길면 긴대로 갑시다 일단 확인해야하니까.
// if (response.Length > maxLength)
// {
//     // 첫 문장만 추출
// }

// 냥/야옹 없으면
if (!response.Contains("냥") && !response.Contains("야옹"))
{
    // 끝에 "냥" 추가
}
```

### 5. 단계별 개발 로드맵

#### Phase 1: MVP ✅ (현재)
- [x] CatState 시스템 (Hunger, Energy, Stress, Fun, Affection, Personality)
- [x] 상호작용 → CatState 변화 (밥주기/만지기/대화하기/놀아주기)
- [x] 혼잣말(자율 발화) 구현
- [x] JSON 로그 저장 (세션 단위)
- [x] 영어 필터링 후처리

#### Phase 2: 고도화
- [ ] Trust(신뢰도) 변수 추가
- [ ] 성격(Personality) 장기 성장 (천천히 누적 변화)
- [ ] moodTag 자동 계산 로직 고도화
- [ ] 혼잣말/대화 자연스러움 향상

#### Phase 3: 모델 벤치마크/비교
- [ ] 테스트셋(JSON) 구성: 동일 Control+UserText
- [ ] 여러 모델에 입력 → 응답 로그 생성
- [ ] 평가 지표 산출:
  - 한국어-only 준수율
  - moodTag/tone/length 준수율
  - 응답 자연스러움 (휴리스틱)
  - 캐릭터 일관성
- [ ] 모델별 비교 결과 CSV/JSON 저장

#### Phase 4: 모델 고도화 (학습)
- [ ] 우선: 세팅/프롬프트/룰엔진 최적화
- [ ] 필요 시 LoRA/QLoRA SFT:
  - Control 입력 → 고양이 대사(정답) 데이터셋(JSONL)
- [ ] DPO/선호학습 고려:
  - 같은 입력에 대해 좋은/나쁜 답변 쌍 구성
- [ ] 학습된 모델 Ollama 배포

### 6. 성능/유지보수 원칙

| 원칙 | 설명 |
|------|------|
| 기존 기능 보호 | 코드 변경 시 기존 기능이 절대 깨지면 안 됨 |
| 최소 변경 | 코드 변경은 최소화 |
| 단일 책임 | CatStateManager / LLMClient / PromptBuilder / LogWriter 분리 |
| 경량 런타임 | Unity 런타임은 가볍게 유지 |
| 분리된 분석 | 로그 분석/시각화는 별도 Windows(WPF) 프로그램에서 수행 |

### 7. 클래스 구조 (권장)

```
Assets/_Project/Scripts/
├── API/
│   ├── OllamaAPIManager.cs      # LLM 호출
│   ├── PromptBuilder.cs         # Control → 프롬프트 생성 (TODO)
│   └── SentimentAnalyzer.cs     # 사용자 입력 감정 분석
├── Managers/
│   ├── CatStateManager.cs       # 상태 관리 (규칙 기반)
│   ├── InteractionLogger.cs     # JSON 로그 저장
│   └── MonologueManager.cs      # 자율 발화
└── Models/
    └── CatState.cs              # 상태 데이터 구조
```

---

## 핵심 메커니즘

### 1. 성장 단계 (Growth Stages)

```
생후 7일 (New Born) → 1개월 (Kitten) → 3개월 (Young) → 6개월+ (Adult)
```

**각 단계별 특징**:
- **어휘력**: 옹알이 → 단어 → 짧은 문장 → 복잡한 대화
- **기억력**: 1~2회 대화 → 최근 10회 → 전체 대화 요약
- **성격**: 기본 템플릿 → 주인과의 상호작용으로 점진적 변화

### 2. 학습 시스템 (Learning System)

#### A. 단기 기억 (Short-term Memory)
- 최근 5~10회 대화 내역
- 오늘의 상호작용 (놀아준 횟수, 먹이 준 시간)
- 현재 감정 상태

#### B. 장기 기억 (Long-term Memory)
```json
{
  "owner_profile": {
    "name": "주인 이름",
    "call_style": "존댓말/반말",
    "interaction_patterns": ["주로 밤에 대화", "아침에 먹이"],
    "topics_of_interest": ["게임", "음악", "요리"],
    "emotional_tone": "친근함/진지함/장난스러움"
  },
  "environment": {
    "home_type": "아파트/주택",
    "toys": ["공", "쥐 인형"],
    "feeding_schedule": ["08:00", "20:00"],
    "favorite_spots": ["창가", "침대 밑"]
  },
  "personality_traits": {
    "playfulness": 85,      // 0~100
    "affection": 70,
    "curiosity": 90,
    "independence": 30,
    "talkativeness": 60
  },
  "learned_behaviors": [
    "주인이 '밥' 말하면 기대함",
    "저녁 9시쯤 졸림",
    "장난감 공을 좋아함"
  ]
}
```

#### C. 경험 기반 학습 (Experience-based Learning)
- **패턴 인식**: 특정 시간대 주인 행동 학습
- **선호도 학습**: 어떤 놀이/먹이를 좋아하는지
- **감정 연결**: "주인이 슬플 때 위로했더니 좋아함" 학습

### 3. Claude API 프롬프트 설계

#### 기본 구조
```
[시스템 프롬프트]
너는 생후 {age}일 된 고양이야.
현재 성장 단계: {stage}
성격: {personality_summary}

[장기 기억 컨텍스트]
주인 프로필: {owner_profile}
학습한 패턴: {learned_behaviors}

[단기 기억]
최근 대화:
{recent_conversations}

오늘 상호작용:
- 놀아준 횟수: {play_count}
- 마지막 식사: {last_fed}
- 현재 기분: {current_mood}

[제약사항]
- 생후 7일: "냥냥" "으아아" 같은 옹알이 위주
- 생후 1개월: 단어 몇 개 사용 가능 ("배고파", "놀자")
- 생후 3개월+: 짧은 문장 사용
- 성격 특성을 대화에 반영 (playfulness가 높으면 장난스럽게)
```

---

## 구현 로드맵

### Phase 1: 기본 Agent (Day 4~5)
- [ ] 고정된 페르소나 (생후 7일 컨셉)
- [ ] Claude API 연동
- [ ] 단기 기억 (최근 5회 대화)
- [ ] 기본 상태 반영 (기분, 친밀도)

### Phase 2: 성장 시스템 (추가 개발)
- [ ] 성장 단계 정의 (NewBorn → Adult)
- [ ] 나이 추적 (실제 시간 경과 or 상호작용 횟수)
- [ ] 단계별 어휘/말투 변화
- [ ] 성장 이벤트 (첫 단어, 첫 문장 등)

### Phase 3: 학습 시스템 (고급)
- [ ] 장기 기억 데이터 구조
- [ ] 주인 프로필 자동 생성
  - 대화 분석 → 주제 추출
  - 시간대별 패턴 학습
  - 감정 톤 분석
- [ ] 환경 학습
  - 놀이/먹이 선호도 추적
  - 일과 패턴 학습
- [ ] 성격 진화
  - 초기: 기본 템플릿 (호기심 많은 새끼)
  - 변화: 상호작용 패턴에 따라 조정

### Phase 4: 고급 Agent 기능 (장기)
- [ ] **임베딩 기반 기억 검색**
  - 유사한 과거 대화 찾기
  - 관련 경험 회상
- [ ] **능동적 행동**
  - 배고프면 먼저 말 걸기
  - 오래 안 놀아주면 삐지기
- [ ] **멀티모달 학습**
  - 주인이 보낸 사진 기억
  - 장난감/환경 이미지 인식

---

## 기술 스택 제안

### 기본 (현재 프로젝트)
- **Claude API**: 대화 생성
- **JSON 로컬 저장**: 상태, 기억, 학습 데이터

### 고급 (선택적)
- **Vector DB** (예: Pinecone, ChromaDB):
  - 대화 임베딩 저장
  - 유사 경험 검색
- **Fine-tuning** (장기적):
  - 고양이 전용 모델 학습
  - 주인별 맞춤형 모델

---

## 예시 시나리오

### 시나리오 1: 생후 7일
```
주인: "안녕 고양이야?"
고양이: "으으... 냥? 🥺" (옹알이만 가능)

주인: "배고프니?"
고양이: "냥냥! 으아아!" (긍정적 반응, 하지만 단어는 못 함)
```

### 시나리오 2: 생후 1개월 (일부 학습 완료)
```
주인: "밥 먹을래?"
고양이: "밥! 냥냥! 조아 🤤" (단어 사용 시작)

[학습된 정보]
- "밥" = 좋아하는 것
- 주인이 자주 "밥 먹을래?" 물어봄
```

### 시나리오 3: 생후 3개월 (성격 형성)
```
주인: "오늘 기분이 안 좋네..."
고양이: "왜 슬퍼? 나랑 놀면 기분 좋아져! 공 던져줘~ 🥺"

[학습된 패턴]
- 주인이 슬플 때 놀이 제안하면 좋아함
- playfulness: 85 (장난스러운 성격으로 발전)
- 주인의 감정에 반응하는 법 학습
```

### 시나리오 4: 생후 6개월 (완전 학습)
```
주인: "야, 또 침대에 토했니?"
고양이: "미안해... 배가 안 좋았어 😿 저번에 준 그 간식... 별로였나봐.
        원래 먹던 거 주면 안 돼? 아, 그리고 9시쯤 되니까 슬슬 졸려..."

[학습된 정보]
- 특정 간식에 안 좋은 반응
- 주인이 저녁 9시쯤 재운다는 패턴 학습
- 복잡한 문장 구사 가능
- 과거 경험(간식) 회상 가능
```

---

## 데이터 구조 예시

### CatMemory.json
```json
{
  "age_in_days": 45,
  "growth_stage": "Kitten",
  "birth_date": "2025-01-01T00:00:00Z",

  "short_term_memory": {
    "recent_conversations": [
      {
        "timestamp": "2025-02-15T14:30:00Z",
        "user": "밥 먹을래?",
        "cat": "밥! 냥냥! 조아 🤤",
        "mood_after": "Happy"
      }
    ],
    "today_interactions": {
      "play_count": 3,
      "fed_times": ["08:00", "20:00"],
      "pet_count": 5
    }
  },

  "long_term_memory": {
    "owner_profile": {
      "detected_name": "지민",
      "interaction_style": "친근함, 반말",
      "active_hours": ["19:00-23:00"],
      "interests": ["게임", "음악"]
    },
    "learned_patterns": [
      {
        "pattern": "저녁 8시쯤 밥 주는 시간",
        "confidence": 0.95,
        "occurrences": 20
      },
      {
        "pattern": "주인이 '게임' 말하면 흥미로워함",
        "confidence": 0.8,
        "occurrences": 8
      }
    ],
    "personality": {
      "playfulness": 85,
      "affection": 70,
      "curiosity": 90,
      "independence": 30,
      "talkativeness": 60
    }
  }
}
```

---

## 구현 우선순위

### ✅ 즉시 가능 (현재 프로젝트에 바로 추가)
1. 생후 7일 컨셉 (고정 페르소나)
2. 단기 기억 (최근 대화 5~10개)
3. 기본 상태 반영 (기분, 친밀도)
4. 성장 단계 추적 (나이 카운트)

### 🔄 중기 목표 (Day 5 이후)
1. 장기 기억 시스템
2. 주인 프로필 학습
3. 성격 진화
4. 패턴 인식

### 🚀 장기 목표 (MVP 이후)
1. 임베딩 기반 기억 검색
2. 능동적 행동
3. 멀티모달 학습

---

## 결론

**이 컨셉은 완전히 가능합니다!**

- **기본 구현**: Day 4~5에 바로 시작 가능
- **학습 시스템**: 점진적으로 추가 가능
- **확장성**: 계속 발전시킬 수 있는 구조

다음 단계에서 이 설계를 roadmap에 통합할까요?
