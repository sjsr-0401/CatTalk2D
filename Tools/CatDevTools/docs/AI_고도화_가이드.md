# CatTalk2D AI 고도화 가이드

> 이 문서는 CatTalk2D 프로젝트의 AI 시스템을 테스트하고 개선하는 전체 과정을 설명합니다.

## 목차
1. [A) Unity에서 AI 대화 테스트](#a-unity에서-ai-대화-테스트)
2. [B) LoRA 튜닝 환경 구축](#b-lora-튜닝-환경-구축)
3. [C) 벤치마크 실행](#c-벤치마크-실행)
4. [전체 워크플로우](#전체-워크플로우)

---

## A) Unity에서 AI 대화 테스트

### 사전 준비

1. **Ollama 설치 및 실행**
   ```bash
   # Ollama 다운로드: https://ollama.ai
   # 설치 후 자동 실행됨 (localhost:11434)

   # 모델 다운로드 (추천: aya:8b - 한국어 지원)
   ollama pull aya:8b
   ```

2. **Unity 프로젝트 열기**
   - Unity Hub에서 CatTalk2D 프로젝트 열기
   - 버전: Unity 2022.3+ 권장

### 테스트 방법

1. **Play 모드 진입**
   - Unity 에디터에서 Play 버튼 클릭
   - 또는 `Ctrl + P`

2. **고양이와 대화**
   - 채팅창에 메시지 입력
   - 예시 입력:
     ```
     "안녕 망고야!"
     "밥 줄까?"
     "오늘 기분 어때?"
     "같이 놀자!"
     ```

3. **로그 확인**
   - Console 창에서 `[OllamaAPI]` 로그 확인
   - Control JSON 확인하려면:
     - `OllamaAPIManager` 컴포넌트에서 `Log Control Json` 체크

4. **상호작용 로그 파일**
   - 위치: `CatTalk2D/Logs/interactions_YYYY-MM-DD.jsonl`
   - 모든 대화 기록 저장됨

### 모델 변경하기

```
Unity 에디터 → Hierarchy → OllamaAPIManager
→ Inspector → Model Name 변경
```

또는 게임 내 설정 UI에서 변경 가능 (구현되어 있다면)

---

## B) LoRA 튜닝 환경 구축

### 개요

Ollama는 **이미 튜닝된 모델을 실행**만 합니다.
직접 학습하려면 별도 도구가 필요합니다.

```
[데이터셋 생성] → [LoRA 학습] → [GGUF 변환] → [Ollama 등록]
   DevTools        Unsloth      llama.cpp       Modelfile
```

### Step 1: 데이터셋 생성 (DevTools)

1. **DevTools 실행**
   ```
   경로: Tools/CatDevTools/bin/Release/net8.0-windows/CatTalk2D_DevTools.exe
   ```

2. **AI 튜닝 탭 → 데이터셋 생성**
   - **기본 450개**: Age(3) × Mood(5) × Affection(3) × Category(10)
   - **확장 900개**: 기본 × CareProfile(CP01, CP05)
   - **Pair 데이터**: 같은 입력에 CP01 vs CP05 비교

3. **저장 위치 선택**
   - 예: `LoraData/training_data.jsonl`

4. **생성된 파일 형식**
   ```json
   {"messages":[
     {"role":"system","content":"너는 주황색 치즈냥이 캐릭터다. 한국어로 1~2문장으로 답한다."},
     {"role":"user","content":"[CONTROL]{...}\n[USER]안녕 망고야!"},
     {"role":"assistant","content":"야옹~ 나 완전 행복해! 최고야!"}
   ],"meta":{...}}
   ```

### Step 2: Python 환경 구축

1. **Python 3.10+ 설치**
   ```bash
   # Windows: Microsoft Store에서 Python 3.10 설치
   # 또는 https://www.python.org/downloads/
   ```

2. **가상환경 생성**
   ```bash
   cd CatTalk2D/Tools/LoRA
   python -m venv venv

   # Windows
   venv\Scripts\activate

   # Mac/Linux
   source venv/bin/activate
   ```

3. **Unsloth 설치** (GPU 있는 경우)
   ```bash
   pip install "unsloth[colab-new] @ git+https://github.com/unslothai/unsloth.git"
   pip install --no-deps xformers trl peft accelerate bitsandbytes
   ```

   **CPU만 있는 경우** (느림, 테스트용)
   ```bash
   pip install transformers datasets peft accelerate
   ```

### Step 3: 학습 스크립트 실행

**train_lora.py** 예시:
```python
from unsloth import FastLanguageModel
import torch

# 1. 기본 모델 로드 (Llama 3.1 8B 또는 Gemma 2)
model, tokenizer = FastLanguageModel.from_pretrained(
    model_name = "unsloth/llama-3-8b-Instruct",
    max_seq_length = 2048,
    load_in_4bit = True,  # 메모리 절약
)

# 2. LoRA 어댑터 추가
model = FastLanguageModel.get_peft_model(
    model,
    r = 16,  # LoRA rank
    target_modules = ["q_proj", "k_proj", "v_proj", "o_proj"],
    lora_alpha = 16,
    lora_dropout = 0,
    bias = "none",
)

# 3. 데이터셋 로드
from datasets import load_dataset
dataset = load_dataset("json", data_files="../../LoraData/training_data.jsonl")

# 4. 학습
from trl import SFTTrainer
from transformers import TrainingArguments

trainer = SFTTrainer(
    model = model,
    train_dataset = dataset["train"],
    dataset_text_field = "text",  # 또는 커스텀 포맷터
    max_seq_length = 2048,
    args = TrainingArguments(
        per_device_train_batch_size = 2,
        gradient_accumulation_steps = 4,
        warmup_steps = 10,
        max_steps = 100,  # 450개면 100~200 스텝 권장
        learning_rate = 2e-4,
        fp16 = True,
        output_dir = "outputs",
    ),
)
trainer.train()

# 5. 저장
model.save_pretrained("cheese_cat_lora")
```

### Step 4: GGUF 변환

```bash
# llama.cpp 설치
git clone https://github.com/ggerganov/llama.cpp
cd llama.cpp
make

# LoRA를 base 모델에 병합 후 GGUF 변환
python convert.py ../cheese_cat_lora --outtype f16 --outfile cheese_cat.gguf

# 양자화 (선택, 용량 줄이기)
./quantize cheese_cat.gguf cheese_cat_q4.gguf q4_k_m
```

### Step 5: Ollama에 등록

1. **Modelfile 작성**
   ```
   # Modelfile
   FROM ./cheese_cat_q4.gguf

   SYSTEM """너는 주황색 치즈냥이 캐릭터 '망고'다.
   항상 한국어로 1~2문장으로 답하고, 말 끝에 '냥'을 붙인다.
   """

   PARAMETER temperature 0.7
   PARAMETER top_p 0.9
   ```

2. **모델 생성**
   ```bash
   ollama create cheese-cat -f Modelfile
   ```

3. **테스트**
   ```bash
   ollama run cheese-cat "안녕 망고야!"
   ```

4. **Unity에서 사용**
   - `OllamaAPIManager` → `Model Name`을 `cheese-cat`으로 변경

---

## C) 벤치마크 실행

### 테스트셋 생성

1. **DevTools 실행**
2. **AI 튜닝 탭 → 테스트셋 생성**
   - 30개 고정 테스트 케이스 생성
   - 저장: `LoraData/test_set.jsonl`

### 벤치마크 실행

1. **벤치마크 탭 이동**
2. **모델 선택** (태그 형식으로 여러 개 추가 가능)
3. **테스트셋 파일 선택**
4. **실행 버튼 클릭**

### 평가 시스템 (이중 평가)

벤치마크는 두 가지 점수를 동시에 계산합니다:

#### 1. 기본 평가 (25점) - Control 준수도

| 지표 | 설명 | 만점 |
|------|------|------|
| Control 준수율 | moodTag, ageLevel, affectionTier 반영 | 5 |
| 상태 반영률 | 기분에 맞는 반응 (hungry→밥 언급 등) | 5 |
| 나이 말투 일치 | Child(활발), Teen(츤데레), Adult(차분) | 5 |
| 호감도 태도 일치 | high(애정), mid(중립), low(거리두기) | 5 |
| 캐릭터 일관성 | 냥체 사용, 한국어, 적절한 길이 | 5 |

#### 2. CatLikenessScore (100점) - 고양이다움 점수

> 자세한 채점 규칙은 `docs/cat_scoring_rules.md` 참조

| 영역 | 설명 | 기본 배점 |
|------|------|----------|
| Routine | 시간대별 행동 일관성 (우다다/졸림/밥타임) | 20 |
| Need | 욕구 우선순위 반영 (배고픔>놀이>휴식>애정) | 25 |
| Trust | 신뢰도에 따른 거리감 표현 | 20 |
| Tsundere | 츤데레/독립성 표현 | 10 |
| Sensitivity | 상태에 따른 자극 반응 | 10 |
| Monologue | 혼잣말/관찰 묘사 | 5 |
| Action | 행동으로 말하기 (의성어/의태어) | 10 |

**HumanLike 감점**: "제가", "도와드릴게요", "상담" 등 사람 같은 표현은 최대 -15점

### 등급표 (기본 평가 기준)

| 등급 | 점수 | 설명 |
|------|------|------|
| S | 23~25 | 완벽한 고양이다움 |
| A | 18~22 | 우수함 |
| B | 13~17 | 양호함 |
| C | 8~12 | 개선 필요 |
| D | 0~7 | 튜닝 필요 |

### 결과 예시

```
=== aya:8b 벤치마크 결과 ===
기본 점수: 18.5/25 (A등급)
CatLikenessScore: 72/100

[기본 점수 세부]
- Control 준수율: 3.8/5
- 상태 반영률: 4.2/5
- 나이 말투 일치: 3.5/5
- 호감도 태도 일치: 3.5/5
- 캐릭터 일관성: 3.5/5

[CatLikenessScore 세부]
- Routine: 15/20
- Need: 18/25
- Trust: 14/20
- Tsundere: 7/10
- Sensitivity: 8/10
- Monologue: 3/5
- Action: 7/10

테스트 케이스: 30/30
오류: 0개
```

### Export 기능

벤치마크 결과는 **요약(Summary)** + **상세(Detailed)** 두 가지 형식으로 저장됩니다.

#### 요약 Export (모델별 평균)
- **저장 위치**: `%AppData%/CatTalk2D/Benchmarks/`
- **파일명**: `benchmark_YYYYMMDD_HHMM.json/.csv`
- **내용**: 모델별 평균 점수, 등급, CatLikenessScore 요약

#### 상세 Export (케이스×모델별)
- **저장 위치**: `%AppData%/CatTalk2D/Benchmarks/Detailed/`
- **파일명**: `benchmark_detailed_YYYYMMDD_HHMM.json/.csv`
- **내용**: 각 테스트 케이스 × 각 모델 조합별 상세 데이터
- **CSV 컬럼 (30+개)**:
  - 식별: `timestamp`, `model`, `caseKey`
  - Control: `ageLevel`, `ageDays`, `moodTag`, `affectionTier`, `trustTier`, `timeBlock`, `needTop1`
  - 상태: `energy`, `stress`, `hunger`, `fun`, `affection`, `trust`, `isFeedingWindow`
  - 입출력: `userText`, `response`
  - Basic 점수: `basicTotal`, `basicControl`, `basicState`, `basicAge`, `basicAffection`, `basicConsistency`
  - CatScore: `catScoreTotal`, `catRoutine`, `catNeed`, `catTrust`, `catTsundere`, `catSensitivity`, `catMonologue`, `catAction`
  - 분석: `scoreReasonsUser`, `debug_reasons_joined`, `matchedKeywords`

#### ScoreReasons 분리
- **scoreReasonsUser**: 유의미한 이유만 (|delta| >= 4, 영역당 최대 2개, 전체 최대 6개)
- **scoreReasonsDebug**: 모든 평가 이유 (기본값 포함)
- **prefix 규칙**: `[Routine]`, `[Need]`, `[Trust]`, `[Tsundere]`, `[Sensitivity]`, `[Monologue]`, `[Action]`, `[HumanLike]`

예시:
```
[Routine] Afternoon 졸림 키워드 '졸려,잠'(+16)
[Need] needTop1=rest이고 '피곤,잠' 언급(+25)
[Trust] trust=low인데 '사랑해' 과한 애정(-12)
```

---

## 전체 워크플로우

```
┌─────────────────────────────────────────────────────────────┐
│                    AI 고도화 워크플로우                       │
└─────────────────────────────────────────────────────────────┘

[1단계: 기준선 측정]
    │
    ├─→ Ollama에 기본 모델 설치 (aya:8b)
    ├─→ DevTools에서 테스트셋 생성 (30개)
    └─→ 벤치마크 실행 → 점수 기록

[2단계: 데이터셋 준비]
    │
    ├─→ DevTools에서 학습 데이터 생성
    │     - 기본 450개 또는 확장 900개
    │     - JSONL 형식으로 저장
    └─→ 데이터 검토 및 수정 (필요시)

[3단계: 모델 학습] (선택)
    │
    ├─→ Python + Unsloth 환경 구축
    ├─→ LoRA 학습 실행
    ├─→ GGUF로 변환
    └─→ Ollama에 등록

[4단계: 검증]
    │
    ├─→ 벤치마크 재실행
    ├─→ 점수 비교 (기존 vs 튜닝)
    └─→ Unity에서 실제 플레이 테스트

[5단계: 반복]
    │
    └─→ 결과가 불만족스러우면 2단계로
```

---

## 트러블슈팅

### Ollama 연결 실패
```bash
# Ollama 실행 확인
ollama list

# 서비스 재시작
# Windows: 작업 관리자에서 Ollama 종료 후 재실행
# Mac: brew services restart ollama
```

### 영어 응답이 나올 때
- `aya:8b` 모델 사용 (한국어 지원 최적화)
- 또는 system 프롬프트에 "반드시 한국어로만 답해" 추가
- Unity의 `ResponseProcessor`가 영어 응답 필터링함

### CUDA 오류 (학습 시)
```bash
# CUDA 버전 확인
nvidia-smi

# PyTorch CUDA 버전 맞추기
pip install torch --index-url https://download.pytorch.org/whl/cu118
```

### 메모리 부족 (학습 시)
```python
# 4bit 양자화 사용
load_in_4bit = True

# 배치 사이즈 줄이기
per_device_train_batch_size = 1
gradient_accumulation_steps = 8
```

---

## 파일 구조

```
CatTalk2D/
├── Assets/_Project/Scripts/
│   ├── API/
│   │   └── OllamaAPIManager.cs    # Ollama 연동
│   ├── AI/
│   │   ├── ControlBuilder.cs      # Control JSON 생성
│   │   ├── PromptBuilder.cs       # 프롬프트 생성
│   │   └── ResponseProcessor.cs   # 응답 후처리
│   └── Managers/
│       └── InteractionLogger.cs   # 로그 기록
│
├── Tools/
│   ├── CatDevTools/               # WPF 관리 도구
│   │   ├── Services/
│   │   │   ├── DatasetGenerator.cs
│   │   │   ├── BenchmarkRunner.cs         # 기본 평가 (25점)
│   │   │   ├── BenchmarkExporter.cs       # 요약 Export
│   │   │   ├── BenchmarkDetailedExporter.cs # 상세 Export (케이스×모델)
│   │   │   ├── OllamaService.cs
│   │   │   └── Scoring/                   # CatLikenessScore 시스템
│   │   │       ├── CatLikenessScorer.cs   # 평가 엔진 (User/Debug reasons 분리)
│   │   │       └── CatScoreKeywords.cs    # 키워드 사전
│   │   └── docs/
│   │       ├── AI_고도화_가이드.md   # 이 문서
│   │       ├── cat_scoring_rules.md # 고양이다움 채점 규칙
│   │       ├── dataset_schema.md
│   │       └── LoRA_Glossary.md
│   │
│   └── LoRA/                      # 학습 스크립트 (추가 필요)
│       ├── train_lora.py
│       └── convert_gguf.sh
│
├── LoraData/                      # 학습 데이터
│   ├── training_data.jsonl
│   └── test_set.jsonl
│
├── %AppData%/CatTalk2D/Benchmarks/  # 벤치마크 결과
│   ├── benchmark_YYYYMMDD_HHMM.json     # 요약 (모델별 평균)
│   ├── benchmark_YYYYMMDD_HHMM.csv
│   └── Detailed/                        # 상세 결과
│       ├── benchmark_detailed_YYYYMMDD_HHMM.json  # 케이스×모델 전체
│       └── benchmark_detailed_YYYYMMDD_HHMM.csv   # 엑셀 분석용
│
└── Logs/                          # 게임 로그
    └── interactions_*.jsonl
```

---

## 요약: 빠른 시작

```bash
# 1. Ollama 설치 및 모델 다운로드
ollama pull aya:8b

# 2. DevTools로 데이터셋 생성
# Tools/CatDevTools/bin/Release/.../CatTalk2D_DevTools.exe 실행
# AI 튜닝 탭 → 데이터셋 생성

# 3. 벤치마크 실행
# 벤치마크 탭 → 모델 선택 → 실행

# 4. Unity에서 테스트
# Unity 에디터 → Play → 고양이와 대화
```
