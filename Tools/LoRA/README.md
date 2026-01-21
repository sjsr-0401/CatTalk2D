# CatTalk2D LoRA 튜닝 가이드

이 폴더에는 CatTalk2D 프로젝트의 AI를 튜닝하기 위한 스크립트가 포함되어 있습니다.

## 요구사항

- **GPU**: NVIDIA GPU (VRAM 8GB 이상 권장)
- **Python**: 3.10 이상
- **CUDA**: 11.8 이상

## 설치

```bash
# 가상환경 생성 (권장)
python -m venv venv
venv\Scripts\activate  # Windows
# source venv/bin/activate  # Linux/Mac

# 의존성 설치
pip install -r requirements.txt
```

## 사용법

### 1. 데이터셋 준비

DevTools에서 데이터셋 생성:
1. `CatTalk2D_DevTools.exe` 실행
2. "AI 튜닝" 탭 → "데이터셋 생성"
3. 450개(기본) 또는 900개(확장) 선택
4. JSONL 파일 저장

### 2. LoRA 학습

```bash
# 기본 실행 (450개 데이터셋, 100 스텝)
python train_lora.py --dataset ../../LoraData/dataset.jsonl

# 커스텀 설정
python train_lora.py \
    --dataset ../../LoraData/dataset.jsonl \
    --output my_cat_lora \
    --max-steps 200 \
    --batch-size 4 \
    --lora-r 32

# 학습 후 바로 테스트
python train_lora.py \
    --dataset ../../LoraData/dataset.jsonl \
    --test "안녕 망고야!"
```

### 3. GGUF 변환

```bash
# llama.cpp 설치 필요
# https://github.com/ggerganov/llama.cpp

# 변환 스크립트 실행
convert_to_gguf.bat
```

### 4. Ollama 등록

```bash
# Modelfile이 자동 생성됨
ollama create cheese-cat -f Modelfile

# 테스트
ollama run cheese-cat "안녕 망고야!"
```

### 5. Unity 적용

Unity 에디터에서:
1. `OllamaAPIManager` 컴포넌트 선택
2. `Model Name`을 `cheese-cat`으로 변경
3. Play 모드로 테스트

## 파라미터 설명

| 파라미터 | 기본값 | 설명 |
|---------|--------|------|
| `--base-model` | unsloth/llama-3-8b-Instruct | 기본 모델 |
| `--lora-r` | 16 | LoRA rank (높을수록 표현력↑, 메모리↑) |
| `--lora-alpha` | 16 | LoRA scaling factor |
| `--batch-size` | 2 | 배치 사이즈 (메모리 부족 시 1로) |
| `--max-steps` | 100 | 학습 스텝 (450개→100, 900개→200 권장) |
| `--learning-rate` | 2e-4 | 학습률 |

## 트러블슈팅

### CUDA Out of Memory
```bash
# 배치 사이즈 줄이기
python train_lora.py --dataset ... --batch-size 1 --gradient-accumulation 8

# 또는 더 작은 모델 사용
python train_lora.py --dataset ... --base-model unsloth/tinyllama-chat
```

### 의존성 체크
```bash
python train_lora.py --check-deps
```

### 학습이 느릴 때
- `xformers` 설치 확인
- `--batch-size` 늘리기 (VRAM 허용 범위 내)
- `gradient_checkpointing` 활성화 (기본 활성화됨)
