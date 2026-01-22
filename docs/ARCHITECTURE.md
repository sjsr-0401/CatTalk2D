# CatTalk2D 아키텍처 가이드

## 목차
1. [시스템 전체 구조](#1-시스템-전체-구조)
2. [대화 플로우](#2-대화-플로우)
3. [Control 생성 과정](#3-control-생성-과정)
4. [TrustTier별 응답 분기](#4-trusttier별-응답-분기)
5. [벤치마크 평가 플로우](#5-벤치마크-평가-플로우)
6. [학습 데이터 생성 플로우](#6-학습-데이터-생성-플로우)
7. [LoRA 학습 파이프라인](#7-lora-학습-파이프라인)
8. [용어 정리](#8-용어-정리)

---

## 1. 시스템 전체 구조

```mermaid
flowchart TB
    subgraph Unity ["Unity (게임)"]
        UI[ChatUI<br/>사용자 입력]
        State[CatStateManager<br/>상태 관리]
        Control[ControlBuilder<br/>Control JSON 생성]
        Prompt[PromptBuilder<br/>프롬프트 조합]
        API[OllamaAPIManager<br/>API 호출]
        Response[ResponseProcessor<br/>응답 후처리]
        Logger[InteractionLogger<br/>로그 기록]
        Mono[MonologueManager<br/>말풍선 출력]
    end

    subgraph Ollama ["Ollama (AI 서버)"]
        LLM[로컬 LLM<br/>gemma/qwen/aya]
    end

    subgraph DevTools ["DevTools (개발도구)"]
        Bench[BenchmarkRunner<br/>성능 측정]
        Scorer[CatLikenessScorer<br/>점수 계산]
        TrainGen[TrainingDataGenerator<br/>학습 데이터 생성]
        Export[BenchmarkExporter<br/>결과 내보내기]
    end

    subgraph LoRA ["LoRA 학습"]
        Data[training_data.jsonl]
        Train[Unsloth 학습]
        GGUF[GGUF 변환]
    end

    UI --> State
    State --> Control
    Control --> Prompt
    Prompt --> API
    API <--> LLM
    API --> Response
    Response --> Logger
    Response --> Mono

    Bench <--> LLM
    Bench --> Scorer
    Scorer --> Export

    TrainGen --> Data
    Data --> Train
    Train --> GGUF
    GGUF --> LLM
```

---

## 2. 대화 플로우

사용자가 "안녕?"이라고 입력했을 때의 전체 흐름:

```mermaid
sequenceDiagram
    autonumber
    participant User as 사용자
    participant Chat as ChatUI
    participant State as CatStateManager
    participant Control as ControlBuilder
    participant Prompt as PromptBuilder
    participant API as OllamaAPI
    participant LLM as Ollama LLM
    participant Proc as ResponseProcessor
    participant Log as InteractionLogger
    participant Bubble as MessageBubble

    User->>Chat: "안녕?"
    Chat->>State: 현재 상태 조회
    State-->>Control: CatState (hunger=30, trust=75, ...)
    Control->>Control: Control JSON 생성
    Note over Control: trustTier: "high"<br/>timeBlock: "afternoon"<br/>needTop1: "food"
    Control->>Prompt: Control + UserText
    Prompt->>Prompt: 시스템 프롬프트 조합
    Prompt->>API: 완성된 프롬프트
    API->>LLM: HTTP POST /api/generate
    LLM-->>API: "(하품) 어, 왔냥?"
    API->>Proc: Raw Response
    Proc->>Proc: 길이 제한, 후처리
    Proc->>Log: 대화 기록 저장
    Proc->>Bubble: 말풍선 표시
    Bubble-->>User: "(하품) 어, 왔냥?"
```

---

## 3. Control 생성 과정

CatState에서 Control JSON으로 변환하는 과정:

```mermaid
flowchart LR
    subgraph CatState ["CatState (현재 상태)"]
        H[hunger: 30]
        E[energy: 80]
        S[stress: 20]
        F[fun: 60]
        A[affection: 70]
        T[trust: 75]
    end

    subgraph Calculate ["계산 로직"]
        N[NeedTop1 계산]
        TB[TimeBlock 계산]
        TT[TrustTier 계산]
        M[MoodTag 계산]
    end

    subgraph Control ["Control JSON"]
        C["{<br/>trustTier: high,<br/>timeBlock: afternoon,<br/>needTop1: food,<br/>moodTag: happy<br/>}"]
    end

    H --> N
    E --> N
    F --> N
    A --> N

    T --> TT

    N --> C
    TB --> C
    TT --> C
    M --> C
```

### NeedTop1 결정 로직

```mermaid
flowchart TD
    Start([CatState 입력]) --> CheckHunger{hunger < 40?}
    CheckHunger -->|Yes| Food[needTop1 = food]
    CheckHunger -->|No| CheckEnergy{energy < 30?}
    CheckEnergy -->|Yes| Rest[needTop1 = rest]
    CheckEnergy -->|No| CheckFun{fun < 40?}
    CheckFun -->|Yes| Play[needTop1 = play]
    CheckFun -->|No| CheckAffection{affection > 70?}
    CheckAffection -->|Yes| Affection[needTop1 = affection]
    CheckAffection -->|No| None[needTop1 = none]
```

### TrustTier 결정 로직

```mermaid
flowchart TD
    Start([trust 값]) --> Check1{trust ≤ 30?}
    Check1 -->|Yes| Low["TrustTier = LOW<br/>(경계, 거리두기)"]
    Check1 -->|No| Check2{trust ≤ 70?}
    Check2 -->|Yes| Mid["TrustTier = MID<br/>(츤데레, 중립)"]
    Check2 -->|No| High["TrustTier = HIGH<br/>(친밀, 애착)"]

    Low --> LowEx["응답 예시:<br/>'(피함) 건드리지마냥.'"]
    Mid --> MidEx["응답 예시:<br/>'(머리 기울임) 뭐, 나쁘진 않냥.'"]
    High --> HighEx["응답 예시:<br/>'(골골) 기다렸어냥!'"]
```

---

## 4. TrustTier별 응답 분기

```mermaid
flowchart TB
    Input["사용자 입력: '안녕?'"]

    Input --> TrustCheck{TrustTier?}

    TrustCheck -->|LOW| LowPath
    TrustCheck -->|MID| MidPath
    TrustCheck -->|HIGH| HighPath

    subgraph LowPath ["LOW (신뢰도 0-30)"]
        L1[톤: 경계, 거리두기]
        L2[행동: ignore, reject, alert]
        L3["응답 템플릿:<br/>'{action} ...뭐야냥.'<br/>'{action} 건드리지마냥.'"]
        L1 --> L2 --> L3
    end

    subgraph MidPath ["MID (신뢰도 31-70)"]
        M1[톤: 츤데레, 중립]
        M2[행동: observe, 상황별]
        M3["응답 템플릿:<br/>'{action} 어, 왔냥?'<br/>'뭐, 나쁘진 않냥. {action}'"]
        M1 --> M2 --> M3
    end

    subgraph HighPath ["HIGH (신뢰도 71-100)"]
        H1[톤: 친밀, 애착]
        H2[행동: affection, active]
        H3["응답 템플릿:<br/>'{action} 왔다냥! 기다렸어냥!'<br/>'골골... 좋아냥~ {action}'"]
        H1 --> H2 --> H3
    end

    L3 --> ActionSelect
    M3 --> ActionSelect
    H3 --> ActionSelect

    ActionSelect[행동 묘사 선택<br/>context 기반]

    ActionSelect --> FinalResponse["최종 응답:<br/>'(하품) 어, 왔냥?'"]
```

---

## 5. 벤치마크 평가 플로우

```mermaid
flowchart TB
    subgraph Input ["입력"]
        TestSet[testset.jsonl<br/>테스트 케이스들]
        Models[테스트 모델 목록<br/>gemma, qwen, aya, ...]
    end

    subgraph Process ["처리"]
        Runner[BenchmarkRunner]
        Ollama[Ollama API]
        Collect[응답 수집]
    end

    subgraph Scoring ["점수 계산"]
        Basic[BasicScore<br/>25점 만점]
        Cat[CatLikenessScore<br/>100점 만점]
    end

    subgraph Output ["출력"]
        Rank[랭킹표]
        Chart[차트]
        CSV[CSV Export]
        JSON[JSON Export]
    end

    TestSet --> Runner
    Models --> Runner
    Runner --> Ollama
    Ollama --> Collect
    Collect --> Basic
    Collect --> Cat
    Basic --> Rank
    Cat --> Rank
    Rank --> Chart
    Rank --> CSV
    Rank --> JSON
```

### CatLikenessScore 세부 구성

```mermaid
pie showData
    title CatLikenessScore 구성 (100점 만점)
    "Need (욕구 반영)" : 20
    "Routine (시간대 행동)" : 15
    "Trust (신뢰 표현)" : 15
    "Tsundere (츤데레)" : 15
    "Action (행동 묘사)" : 15
    "Sensitivity (자극 반응)" : 10
    "Monologue (혼잣말)" : 10
```

### 점수 계산 상세

```mermaid
flowchart LR
    Response[AI 응답] --> Analyze[키워드 분석]

    Analyze --> R[Routine<br/>우다다/졸림/기지개]
    Analyze --> N[Need<br/>배고파/놀자/쉬고싶어]
    Analyze --> T[Trust<br/>거리두기/친밀 표현]
    Analyze --> TS[Tsundere<br/>흥/어쩔수없이/뭐]
    Analyze --> S[Sensitivity<br/>예민/놀람/반응]
    Analyze --> M[Monologue<br/>...냥/혼잣말]
    Analyze --> A[Action<br/>(하품)/(골골)/(우다다)]

    R --> Sum[총점 합산]
    N --> Sum
    T --> Sum
    TS --> Sum
    S --> Sum
    M --> Sum
    A --> Sum

    Sum --> Final[CatLikenessScore<br/>0-100점]
```

---

## 6. 학습 데이터 생성 플로우

```mermaid
flowchart TB
    subgraph Variables ["조합 변수"]
        Age[AgeLevel<br/>child, teen, adult]
        Trust[TrustTier<br/>low, mid, high]
        Time[TimeBlock<br/>morning, afternoon, ...]
        Need[NeedTop1<br/>food, play, rest, ...]
        Mood[MoodTag<br/>happy, tired, ...]
        Cat[UserCategory<br/>greeting, question, ...]
    end

    subgraph Generate ["생성 과정"]
        Combine[조합 생성<br/>3×3×6×5×8×7 = 15,120]
        SelectTemplate[TrustTier별<br/>응답 템플릿 선택]
        SelectAction[컨텍스트 기반<br/>행동 묘사 선택]
        Replace["{action}" 치환]
    end

    subgraph Output ["출력"]
        JSONL[training_data.jsonl]
        Sample["{'messages': [<br/>  {role: 'system', ...},<br/>  {role: 'user', ...},<br/>  {role: 'assistant', ...}<br/>]}"]
    end

    Age --> Combine
    Trust --> Combine
    Time --> Combine
    Need --> Combine
    Mood --> Combine
    Cat --> Combine

    Combine --> SelectTemplate
    SelectTemplate --> SelectAction
    SelectAction --> Replace
    Replace --> JSONL
    JSONL --> Sample
```

### 행동 묘사 선택 로직

```mermaid
flowchart TD
    Start([Context 입력]) --> CheckTime{timeBlock?}

    CheckTime -->|afternoon + tired| Sleepy["sleepy<br/>(하품), (눈 감김)"]
    CheckTime -->|night + playful| Active["active<br/>(우다다), (폴짝)"]
    CheckTime -->|other| CheckNeed{needTop1?}

    CheckNeed -->|food| Hungry["hungry<br/>(밥그릇 쳐다봄)"]
    CheckNeed -->|rest| Sleepy2["sleepy"]
    CheckNeed -->|affection & high trust| Affection["affection<br/>(골골), (비빔)"]
    CheckNeed -->|other| CheckTrust{trustTier?}

    CheckTrust -->|low| IgnoreReject["ignore/reject<br/>(훽 돌아섬), (피함)"]
    CheckTrust -->|high| AffectionActive["affection/active"]
    CheckTrust -->|mid| CheckMood{moodTag?}

    CheckMood -->|happy/playful| Active2["active"]
    CheckMood -->|grumpy| Ignore["ignore"]
    CheckMood -->|lonely| Affection2["affection"]
    CheckMood -->|other| Observe["observe<br/>(창밖 봄)"]
```

---

## 7. LoRA 학습 파이프라인

```mermaid
flowchart TB
    subgraph Phase1 ["Phase 1: 데이터 준비"]
        DevTools[DevTools]
        Generator[TrainingDataGenerator]
        Data[training_data_500.jsonl]

        DevTools --> Generator
        Generator --> Data
    end

    subgraph Phase2 ["Phase 2: LoRA 학습"]
        Base[Base Model<br/>gemma-2-2b]
        Unsloth[Unsloth]
        LoRA[LoRA Adapter]

        Data --> Unsloth
        Base --> Unsloth
        Unsloth --> LoRA
    end

    subgraph Phase3 ["Phase 3: 변환 및 배포"]
        GGUF[GGUF 변환]
        Modelfile[Modelfile 생성]
        OllamaCreate[ollama create]
        NewModel[cattalk2d-mango]

        LoRA --> GGUF
        GGUF --> Modelfile
        Modelfile --> OllamaCreate
        OllamaCreate --> NewModel
    end

    subgraph Phase4 ["Phase 4: 평가"]
        Benchmark[벤치마크 실행]
        Compare[점수 비교]
        Decision{개선됨?}

        NewModel --> Benchmark
        Benchmark --> Compare
        Compare --> Decision
        Decision -->|Yes| Deploy[배포]
        Decision -->|No| Data
    end
```

### 학습 파라미터

```mermaid
flowchart LR
    subgraph Config ["학습 설정"]
        R[LoRA Rank: 16]
        A[LoRA Alpha: 32]
        LR[Learning Rate: 2e-4]
        E[Epochs: 3]
        B[Batch Size: 4]
    end

    subgraph Target ["타겟 모듈"]
        Q[q_proj]
        K[k_proj]
        V[v_proj]
        O[o_proj]
        G[gate_proj]
        U[up_proj]
        D[down_proj]
    end

    Config --> Train[학습 실행]
    Target --> Train
    Train --> Output[LoRA Adapter<br/>~50MB]
```

---

## 8. 용어 정리

### 핵심 개념

```mermaid
mindmap
  root((CatTalk2D))
    Unity 게임
      CatState
        hunger
        energy
        trust
        affection
      Control
        trustTier
        timeBlock
        needTop1
        moodTag
      UI
        ChatUI
        MessageBubble
    Ollama AI
      LLM 모델
        gemma
        qwen
        aya
      API
        /api/generate
        /api/tags
    DevTools
      벤치마크
        BasicScore
        CatLikenessScore
      학습 데이터
        TrainingDataGenerator
        JSONL
    LoRA 학습
      Unsloth
      GGUF
      Modelfile
```

### TrustTier 요약

| TrustTier | 신뢰도 범위 | 톤 | 대표 행동 | 응답 예시 |
|-----------|------------|-----|----------|----------|
| **LOW** | 0-30 | 경계, 거리두기 | ignore, reject | "(피함) 건드리지마냥." |
| **MID** | 31-70 | 츤데레, 중립 | observe, 상황별 | "(머리 기울임) 뭐, 나쁘진 않냥." |
| **HIGH** | 71-100 | 친밀, 애착 | affection, active | "(골골) 기다렸어냥!" |

### CatLikenessScore 영역

| 영역 | 배점 | 측정 내용 | 키워드 예시 |
|------|------|----------|------------|
| **Routine** | 15점 | 시간대별 행동 일관성 | 우다다, 졸림, 기지개 |
| **Need** | 20점 | 욕구 우선순위 반영 | 배고파, 놀자, 쉬고싶어 |
| **Trust** | 15점 | 신뢰도에 따른 표현 | 거리두기, 다가가기 |
| **Tsundere** | 15점 | 츤데레/독립성 | 흥, 어쩔수없이, 뭐 |
| **Sensitivity** | 10점 | 자극 반응 | 깜짝, 뭐야, 놀람 |
| **Monologue** | 10점 | 혼잣말/관찰 | ...냥, 음, 글쎄 |
| **Action** | 15점 | 행동 묘사 | (하품), (골골), (우다다) |

### 행동 묘사 카테고리

| 카테고리 | 행동 예시 | 사용 상황 |
|----------|----------|----------|
| **sleepy** | (하품), (눈 감김), (늘어짐) | afternoon, tired, rest |
| **active** | (우다다), (폴짝), (질주) | night, playful, happy |
| **ignore** | (훽 돌아섬), (외면) | low trust, grumpy |
| **affection** | (골골), (그르릉), (비빔) | high trust, affection |
| **hungry** | (밥그릇 쳐다봄), (냥냥 울음) | food need |
| **observe** | (창밖 봄), (머리 기울임) | bored, neutral |
| **reject** | (피함), (하악) | low trust, discomfort |

---

## 파일 구조

```
CatTalk2D/
├── Assets/_Project/Scripts/
│   ├── AI/               # ControlBuilder, PromptBuilder, ResponseProcessor
│   ├── API/              # OllamaAPIManager
│   ├── Managers/         # CatStateManager, InteractionLogger, MonologueManager
│   └── UI/               # ChatUI, MessageBubble
│
├── Tools/CatDevTools/
│   ├── Services/         # BenchmarkRunner, TrainingDataGenerator
│   │   └── Scoring/      # CatLikenessScorer
│   └── ViewModels/       # MainViewModel
│
├── LoraData/
│   ├── action_templates.json
│   ├── train_lora.py
│   └── requirements.txt
│
└── docs/
    └── ARCHITECTURE.md   # 이 문서
```
