"""
CatTalk2D LoRA 학습 스크립트
DevTools에서 자동 생성됨
"""

import json
import os
from datetime import datetime

# 설정
DATA_PATH = r"C:\Users\admin\AppData\Roaming\CatTalk2D\LoraData\training_data_high_quality.jsonl"
BASE_MODEL = "Qwen/Qwen2.5-1.5B-Instruct"
OUTPUT_DIR = r"C:\Users\admin\CatTalk2D\LoraData\outputs"
EPOCHS = 3
BATCH_SIZE = 1
GRAD_ACCUM = 8
LORA_R = 16
LORA_ALPHA = 32
LR = 0.0002

print("=" * 60)
print("CatTalk2D LoRA Training")
print("=" * 60)

import torch
print(f"PyTorch: {torch.__version__}")
print(f"CUDA: {torch.cuda.is_available()}")
if torch.cuda.is_available():
    print(f"GPU: {torch.cuda.get_device_name(0)}")
    print(f"VRAM: {torch.cuda.get_device_properties(0).total_memory / 1e9:.1f} GB")

# 1. 데이터 로드
print("\n[1/5] Loading training data...")
with open(DATA_PATH, 'r', encoding='utf-8-sig') as f:
    data = [json.loads(line) for line in f]
print(f"  Loaded {len(data)} samples")

# 데이터 변환
def format_sample(sample):
    messages = sample['messages']
    system = next((m['content'] for m in messages if m['role'] == 'system'), '')
    user = next((m['content'] for m in messages if m['role'] == 'user'), '')
    assistant = next((m['content'] for m in messages if m['role'] == 'assistant'), '')
    text = f"<|im_start|>system\n{system}<|im_end|>\n<|im_start|>user\n{user}<|im_end|>\n<|im_start|>assistant\n{assistant}<|im_end|>"
    return {'text': text}

from datasets import Dataset
formatted_data = [format_sample(s) for s in data]
dataset = Dataset.from_list(formatted_data)
print(f"  Dataset ready: {len(dataset)} samples")

# 2. 모델 로드
print("\n[2/5] Loading base model...")
from transformers import AutoModelForCausalLM, AutoTokenizer, BitsAndBytesConfig

bnb_config = BitsAndBytesConfig(
    load_in_4bit=True,
    bnb_4bit_use_double_quant=True,
    bnb_4bit_quant_type="nf4",
    bnb_4bit_compute_dtype=torch.float16
)

model = AutoModelForCausalLM.from_pretrained(
    BASE_MODEL,
    quantization_config=bnb_config,
    device_map="auto",
    trust_remote_code=True,
)

tokenizer = AutoTokenizer.from_pretrained(BASE_MODEL, trust_remote_code=True)
tokenizer.pad_token = tokenizer.eos_token
tokenizer.padding_side = "right"
print(f"  Model loaded: {BASE_MODEL}")

# 3. LoRA 설정
print("\n[3/5] Configuring LoRA...")
from peft import LoraConfig, get_peft_model, prepare_model_for_kbit_training

model = prepare_model_for_kbit_training(model)

lora_config = LoraConfig(
    r=LORA_R,
    lora_alpha=LORA_ALPHA,
    target_modules=["q_proj", "k_proj", "v_proj", "o_proj", "gate_proj", "up_proj", "down_proj"],
    lora_dropout=0.05,
    bias="none",
    task_type="CAUSAL_LM",
)

model = get_peft_model(model, lora_config)
model.print_trainable_parameters()

# 4. 학습 설정
print("\n[4/5] Setting up training...")
timestamp = datetime.now().strftime("%Y%m%d_%H%M")
output_dir = os.path.join(OUTPUT_DIR, f"cattalk2d_lora_{timestamp}")
os.makedirs(output_dir, exist_ok=True)

from trl import SFTTrainer, SFTConfig

training_args = SFTConfig(
    output_dir=output_dir,
    per_device_train_batch_size=BATCH_SIZE,
    gradient_accumulation_steps=GRAD_ACCUM,
    warmup_steps=10,
    num_train_epochs=EPOCHS,
    learning_rate=LR,
    fp16=False,
    bf16=False,
    logging_steps=10,
    save_steps=50,
    save_total_limit=2,
    optim="adamw_torch",
    weight_decay=0.01,
    lr_scheduler_type="cosine",
    seed=42,
    report_to="none",
    dataset_text_field="text",
    max_length=1024,
)

trainer = SFTTrainer(
    model=model,
    processing_class=tokenizer,
    train_dataset=dataset,
    args=training_args,
)

# 5. 학습
print("\n[5/5] Starting training...")
print("-" * 40)

trainer.train()

# 6. 저장
print("\n[6/6] Saving model...")
lora_path = os.path.join(output_dir, "lora_adapter")
model.save_pretrained(lora_path)
tokenizer.save_pretrained(lora_path)
print(f"  Saved to: {lora_path}")

print(f"\n{'='*60}")
print("Training complete!")
print(f"{'='*60}")
print(f"\nOutput: {output_dir}")
