# CatTalk2D LoRA 학습 환경 구축 가이드

## 요구사항

- Python 3.10+
- NVIDIA GPU (VRAM 8GB 이상 권장, 최소 6GB)
- CUDA 11.8 or 12.1

## 옵션 1: WSL2 (Windows - 권장)

Unsloth는 Linux에서 가장 잘 작동합니다. Windows에서는 WSL2를 권장합니다.

### 1. WSL2 설치 (PowerShell 관리자 권한)
```powershell
wsl --install -d Ubuntu-22.04
```

### 2. WSL2에서 CUDA 설치
```bash
# NVIDIA CUDA toolkit 설치
wget https://developer.download.nvidia.com/compute/cuda/repos/wsl-ubuntu/x86_64/cuda-keyring_1.1-1_all.deb
sudo dpkg -i cuda-keyring_1.1-1_all.deb
sudo apt-get update
sudo apt-get -y install cuda-toolkit-12-1

# 환경변수 설정
echo 'export PATH=/usr/local/cuda-12.1/bin:$PATH' >> ~/.bashrc
echo 'export LD_LIBRARY_PATH=/usr/local/cuda-12.1/lib64:$LD_LIBRARY_PATH' >> ~/.bashrc
source ~/.bashrc
```

### 3. Python 환경 설정
```bash
# Miniconda 설치
wget https://repo.anaconda.com/miniconda/Miniconda3-latest-Linux-x86_64.sh
bash Miniconda3-latest-Linux-x86_64.sh

# 환경 생성
conda create -n cattalk python=3.10
conda activate cattalk

# PyTorch 설치 (CUDA 12.1)
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121
```

### 4. Unsloth 설치
```bash
pip install "unsloth[colab-new] @ git+https://github.com/unslothai/unsloth.git"
pip install transformers datasets accelerate peft bitsandbytes trl
```

### 5. 학습 실행
```bash
# CatTalk2D 폴더로 이동 (WSL 경로)
cd /mnt/c/Users/admin/CatTalk2D/LoraData

# 학습 데이터 경로 확인
ls -la /mnt/c/Users/admin/AppData/Roaming/CatTalk2D/LoRA/

# 학습 실행
python train_lora.py \
  --data "/mnt/c/Users/admin/AppData/Roaming/CatTalk2D/LoRA/training_data_500_*.jsonl" \
  --epochs 3 \
  --batch-size 4
```

---

## 옵션 2: Google Colab (무료 GPU)

VRAM이 부족하거나 WSL 설정이 어려운 경우 Colab을 사용하세요.

### Colab 노트북 코드
```python
# 셀 1: Unsloth 설치
!pip install "unsloth[colab-new] @ git+https://github.com/unslothai/unsloth.git"
!pip install trl datasets

# 셀 2: 구글 드라이브 연결
from google.colab import drive
drive.mount('/content/drive')

# 셀 3: 학습 데이터 업로드 후 실행
# training_data_500.jsonl 파일을 드라이브에 업로드

import json
from unsloth import FastLanguageModel
from datasets import Dataset
from transformers import TrainingArguments
from trl import SFTTrainer

# 데이터 로드
with open('/content/drive/MyDrive/CatTalk2D/training_data_500.jsonl', 'r') as f:
    data = [json.loads(line) for line in f]

# ... (train_lora.py 내용 참조)
```

---

## 옵션 3: Windows Native (제한적)

일부 기능이 제한되지만, Windows에서 직접 실행도 가능합니다.

### 1. CUDA Toolkit 설치
https://developer.nvidia.com/cuda-12-1-0-download-archive

### 2. Python 환경
```cmd
conda create -n cattalk python=3.10
conda activate cattalk
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121
```

### 3. Unsloth 대신 PEFT 직접 사용
```cmd
pip install transformers datasets accelerate peft bitsandbytes trl
```

Windows용 대체 스크립트: `train_lora_windows.py` 사용

---

## 학습 후 Ollama 등록

### 1. GGUF 파일 확인
학습 완료 후 `outputs/cattalk2d_lora_*/` 폴더에 생성됨

### 2. Ollama에 등록
```bash
cd outputs/cattalk2d_lora_*
ollama create cattalk2d-mango -f Modelfile
```

### 3. 테스트
```bash
ollama run cattalk2d-mango "[CONTROL]{\"moodTag\":\"happy\"}[USER]안녕?"
```

---

## 트러블슈팅

### CUDA Out of Memory
- batch-size를 2로 줄이기
- gradient_accumulation_steps를 8로 늘리기
- 모델을 더 작은 것으로 변경 (gemma-2-2b → gemma-2-2b-it-bnb-4bit)

### Unsloth 설치 실패
- Python 버전 확인 (3.10 권장)
- CUDA 버전 확인 (11.8 또는 12.1)
- torch가 CUDA를 인식하는지 확인:
  ```python
  import torch
  print(torch.cuda.is_available())
  print(torch.cuda.get_device_name(0))
  ```

### Windows에서 bitsandbytes 오류
```cmd
pip install bitsandbytes-windows
```
