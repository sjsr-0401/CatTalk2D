"""
CatTalk2D LoRA 학습 스크립트 (Windows 호환)
Unsloth 없이 PEFT/Transformers만 사용

사용법:
    python train_lora_windows.py --data training_data_500.jsonl --epochs 3

요구사항:
    pip install torch transformers datasets peft accelerate bitsandbytes-windows trl
"""

import argparse
import json
import os
from datetime import datetime

def main():
    parser = argparse.ArgumentParser(description='CatTalk2D LoRA Training (Windows)')
    parser.add_argument('--data', type=str, required=True, help='Training data JSONL file path')
    parser.add_argument('--base-model', type=str, default='google/gemma-2-2b-it',
                        help='Base model to fine-tune')
    parser.add_argument('--epochs', type=int, default=3, help='Number of training epochs')
    parser.add_argument('--batch-size', type=int, default=2, help='Batch size per device')
    parser.add_argument('--lr', type=float, default=2e-4, help='Learning rate')
    parser.add_argument('--lora-r', type=int, default=16, help='LoRA rank')
    parser.add_argument('--lora-alpha', type=int, default=32, help='LoRA alpha')
    parser.add_argument('--output', type=str, default='./outputs', help='Output directory')
    parser.add_argument('--use-4bit', action='store_true', help='Use 4-bit quantization')
    args = parser.parse_args()

    print("=" * 60)
    print("CatTalk2D LoRA Training (Windows)")
    print("=" * 60)
    print(f"Data: {args.data}")
    print(f"Base Model: {args.base_model}")
    print(f"Epochs: {args.epochs}")
    print(f"LoRA r={args.lora_r}, alpha={args.lora_alpha}")
    print(f"4-bit: {args.use_4bit}")
    print("=" * 60)

    import torch
    from datasets import Dataset
    from transformers import (
        AutoModelForCausalLM,
        AutoTokenizer,
        BitsAndBytesConfig,
        TrainingArguments,
    )
    from peft import LoraConfig, get_peft_model, prepare_model_for_kbit_training
    from trl import SFTTrainer

    # GPU 확인
    if not torch.cuda.is_available():
        print("WARNING: CUDA not available. Training will be very slow!")
    else:
        print(f"GPU: {torch.cuda.get_device_name(0)}")
        print(f"VRAM: {torch.cuda.get_device_properties(0).total_memory / 1e9:.1f} GB")

    # 1. 데이터 로드
    print("\n[1/5] Loading training data...")
    with open(args.data, 'r', encoding='utf-8') as f:
        data = [json.loads(line) for line in f]

    print(f"  Loaded {len(data)} samples")

    # 데이터셋 변환
    def format_sample(sample):
        """Chat 형식을 텍스트로 변환"""
        messages = sample['messages']

        system = next((m['content'] for m in messages if m['role'] == 'system'), '')
        user = next((m['content'] for m in messages if m['role'] == 'user'), '')
        assistant = next((m['content'] for m in messages if m['role'] == 'assistant'), '')

        # Gemma 형식
        text = f"<start_of_turn>user\n{system}\n\n{user}<end_of_turn>\n<start_of_turn>model\n{assistant}<end_of_turn>"
        return {'text': text}

    formatted_data = [format_sample(s) for s in data]
    dataset = Dataset.from_list(formatted_data)

    # 2. 모델 로드
    print("\n[2/5] Loading base model...")

    if args.use_4bit:
        bnb_config = BitsAndBytesConfig(
            load_in_4bit=True,
            bnb_4bit_use_double_quant=True,
            bnb_4bit_quant_type="nf4",
            bnb_4bit_compute_dtype=torch.bfloat16
        )
        model = AutoModelForCausalLM.from_pretrained(
            args.base_model,
            quantization_config=bnb_config,
            device_map="auto",
            trust_remote_code=True,
        )
        model = prepare_model_for_kbit_training(model)
    else:
        model = AutoModelForCausalLM.from_pretrained(
            args.base_model,
            torch_dtype=torch.float16,
            device_map="auto",
            trust_remote_code=True,
        )

    tokenizer = AutoTokenizer.from_pretrained(args.base_model, trust_remote_code=True)
    tokenizer.pad_token = tokenizer.eos_token
    tokenizer.padding_side = "right"

    # 3. LoRA 설정
    print("\n[3/5] Configuring LoRA...")

    lora_config = LoraConfig(
        r=args.lora_r,
        lora_alpha=args.lora_alpha,
        target_modules=["q_proj", "k_proj", "v_proj", "o_proj",
                       "gate_proj", "up_proj", "down_proj"],
        lora_dropout=0.05,
        bias="none",
        task_type="CAUSAL_LM",
    )

    model = get_peft_model(model, lora_config)
    model.print_trainable_parameters()

    # 4. 학습 설정
    print("\n[4/5] Setting up training...")
    timestamp = datetime.now().strftime("%Y%m%d_%H%M")
    output_dir = os.path.join(args.output, f"cattalk2d_lora_{timestamp}")

    training_args = TrainingArguments(
        output_dir=output_dir,
        per_device_train_batch_size=args.batch_size,
        gradient_accumulation_steps=8,
        warmup_steps=10,
        num_train_epochs=args.epochs,
        learning_rate=args.lr,
        fp16=True,
        logging_steps=10,
        save_steps=100,
        save_total_limit=2,
        optim="adamw_torch",
        weight_decay=0.01,
        lr_scheduler_type="cosine",
        seed=42,
        report_to="none",  # Disable wandb
    )

    trainer = SFTTrainer(
        model=model,
        tokenizer=tokenizer,
        train_dataset=dataset,
        dataset_text_field="text",
        max_seq_length=2048,
        args=training_args,
    )

    # 5. 학습 실행
    print("\n[5/5] Starting training...")
    print("-" * 40)

    trainer.train()

    # 6. 모델 저장
    print("\n[6/6] Saving model...")

    lora_path = os.path.join(output_dir, "lora_adapter")
    model.save_pretrained(lora_path)
    tokenizer.save_pretrained(lora_path)
    print(f"  LoRA adapter saved to: {lora_path}")

    # 추론용 스크립트 생성
    inference_script = f'''"""
CatTalk2D Inference Script
"""
import torch
from transformers import AutoModelForCausalLM, AutoTokenizer
from peft import PeftModel

# 모델 로드
base_model = "{args.base_model}"
lora_path = "{lora_path}"

tokenizer = AutoTokenizer.from_pretrained(base_model)
model = AutoModelForCausalLM.from_pretrained(base_model, torch_dtype=torch.float16, device_map="auto")
model = PeftModel.from_pretrained(model, lora_path)

def generate(user_input, control_json=""):
    prompt = f"<start_of_turn>user\\n[CONTROL]{{control_json}}\\n[USER]{{user_input}}<end_of_turn>\\n<start_of_turn>model\\n"
    inputs = tokenizer(prompt, return_tensors="pt").to(model.device)

    with torch.no_grad():
        outputs = model.generate(
            **inputs,
            max_new_tokens=100,
            temperature=0.7,
            top_p=0.9,
            do_sample=True,
        )

    response = tokenizer.decode(outputs[0], skip_special_tokens=True)
    return response.split("<start_of_turn>model\\n")[-1].split("<end_of_turn>")[0]

# 테스트
if __name__ == "__main__":
    print(generate("안녕?", '{{"moodTag":"happy","trustTier":"high"}}'))
'''

    inference_path = os.path.join(output_dir, "inference.py")
    with open(inference_path, 'w', encoding='utf-8') as f:
        f.write(inference_script)
    print(f"  Inference script saved to: {inference_path}")

    # GGUF 변환 가이드
    print("\n" + "=" * 60)
    print("Training complete!")
    print("=" * 60)
    print(f"\nLoRA adapter: {lora_path}")
    print(f"\nTo test inference:")
    print(f"  python {inference_path}")
    print(f"\nTo convert to GGUF for Ollama:")
    print(f"  1. Merge LoRA weights with base model")
    print(f"  2. Use llama.cpp for GGUF conversion")
    print(f"  3. Create Modelfile and register with Ollama")

    print("\nDone!")

if __name__ == "__main__":
    main()
