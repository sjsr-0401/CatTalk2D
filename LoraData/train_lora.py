"""
CatTalk2D LoRA 학습 스크립트
Unsloth 기반의 효율적인 LoRA 미세조정

사용법:
    python train_lora.py --data training_data_500.jsonl --epochs 3

요구사항:
    pip install unsloth transformers datasets peft accelerate bitsandbytes
"""

import argparse
import json
import os
from datetime import datetime

def main():
    parser = argparse.ArgumentParser(description='CatTalk2D LoRA Training')
    parser.add_argument('--data', type=str, required=True, help='Training data JSONL file path')
    parser.add_argument('--base-model', type=str, default='unsloth/gemma-2-2b-it-bnb-4bit',
                        help='Base model to fine-tune')
    parser.add_argument('--epochs', type=int, default=3, help='Number of training epochs')
    parser.add_argument('--batch-size', type=int, default=4, help='Batch size per device')
    parser.add_argument('--lr', type=float, default=2e-4, help='Learning rate')
    parser.add_argument('--lora-r', type=int, default=16, help='LoRA rank')
    parser.add_argument('--lora-alpha', type=int, default=32, help='LoRA alpha')
    parser.add_argument('--output', type=str, default='./outputs', help='Output directory')
    args = parser.parse_args()

    print("=" * 60)
    print("CatTalk2D LoRA Training")
    print("=" * 60)
    print(f"Data: {args.data}")
    print(f"Base Model: {args.base_model}")
    print(f"Epochs: {args.epochs}")
    print(f"LoRA r={args.lora_r}, alpha={args.lora_alpha}")
    print("=" * 60)

    # Unsloth 임포트
    try:
        from unsloth import FastLanguageModel
        from unsloth.chat_templates import get_chat_template
    except ImportError:
        print("ERROR: unsloth not installed. Run: pip install unsloth")
        return

    from datasets import Dataset
    from transformers import TrainingArguments
    from trl import SFTTrainer

    # 1. 데이터 로드
    print("\n[1/5] Loading training data...")
    with open(args.data, 'r', encoding='utf-8') as f:
        data = [json.loads(line) for line in f]

    print(f"  Loaded {len(data)} samples")

    # 데이터셋 변환
    def format_sample(sample):
        """Chat 형식을 텍스트로 변환"""
        messages = sample['messages']

        # System + User + Assistant 형식으로 변환
        system = next((m['content'] for m in messages if m['role'] == 'system'), '')
        user = next((m['content'] for m in messages if m['role'] == 'user'), '')
        assistant = next((m['content'] for m in messages if m['role'] == 'assistant'), '')

        # Gemma 형식으로 포맷팅
        text = f"<start_of_turn>user\n{system}\n\n{user}<end_of_turn>\n<start_of_turn>model\n{assistant}<end_of_turn>"
        return {'text': text}

    formatted_data = [format_sample(s) for s in data]
    dataset = Dataset.from_list(formatted_data)

    # 2. 모델 로드
    print("\n[2/5] Loading base model...")
    model, tokenizer = FastLanguageModel.from_pretrained(
        model_name=args.base_model,
        max_seq_length=2048,
        dtype=None,  # Auto-detect
        load_in_4bit=True,
    )

    # 3. LoRA 설정
    print("\n[3/5] Configuring LoRA...")
    model = FastLanguageModel.get_peft_model(
        model,
        r=args.lora_r,
        target_modules=["q_proj", "k_proj", "v_proj", "o_proj",
                       "gate_proj", "up_proj", "down_proj"],
        lora_alpha=args.lora_alpha,
        lora_dropout=0.05,
        bias="none",
        use_gradient_checkpointing="unsloth",
        random_state=42,
    )

    # 4. 학습 설정
    print("\n[4/5] Setting up training...")
    timestamp = datetime.now().strftime("%Y%m%d_%H%M")
    output_dir = os.path.join(args.output, f"cattalk2d_lora_{timestamp}")

    training_args = TrainingArguments(
        output_dir=output_dir,
        per_device_train_batch_size=args.batch_size,
        gradient_accumulation_steps=4,
        warmup_steps=10,
        num_train_epochs=args.epochs,
        learning_rate=args.lr,
        fp16=True,
        logging_steps=10,
        save_steps=100,
        save_total_limit=2,
        optim="adamw_8bit",
        weight_decay=0.01,
        lr_scheduler_type="cosine",
        seed=42,
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

    # LoRA 어댑터 저장
    lora_path = os.path.join(output_dir, "lora_adapter")
    model.save_pretrained(lora_path)
    tokenizer.save_pretrained(lora_path)
    print(f"  LoRA adapter saved to: {lora_path}")

    # GGUF 변환 (Ollama용)
    print("\n Converting to GGUF for Ollama...")
    gguf_path = os.path.join(output_dir, "cattalk2d.gguf")

    try:
        model.save_pretrained_gguf(
            gguf_path.replace('.gguf', ''),
            tokenizer,
            quantization_method="q4_k_m"
        )
        print(f"  GGUF saved to: {gguf_path}")

        # Ollama Modelfile 생성
        modelfile_content = f'''FROM {gguf_path}

PARAMETER temperature 0.7
PARAMETER top_p 0.9
PARAMETER top_k 40
PARAMETER num_ctx 2048

SYSTEM """
너는 '망고'라는 이름의 고양이 캐릭터다.
반드시 한국어로만 대답하고, 문장 끝에 '냥'을 붙인다.
[CONTROL] 정보를 참고해서 현재 상태와 기분에 맞게 행동하고 대답한다.
행동은 괄호로 표현한다. 예: (하품) (우다다) (골골)
응답은 1-2문장으로 짧게 한다.
"""
'''
        modelfile_path = os.path.join(output_dir, "Modelfile")
        with open(modelfile_path, 'w', encoding='utf-8') as f:
            f.write(modelfile_content)
        print(f"  Modelfile saved to: {modelfile_path}")

        print("\n" + "=" * 60)
        print("Training complete!")
        print("=" * 60)
        print(f"\nTo register with Ollama:")
        print(f"  cd {output_dir}")
        print(f"  ollama create cattalk2d-mango -f Modelfile")
        print(f"\nTo test:")
        print(f"  ollama run cattalk2d-mango")

    except Exception as e:
        print(f"  GGUF conversion failed: {e}")
        print("  You can convert manually using llama.cpp")

    print("\nDone!")

if __name__ == "__main__":
    main()
