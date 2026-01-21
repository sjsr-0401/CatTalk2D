"""
CatTalk2D LoRA 튜닝 스크립트
- Unsloth 기반 빠른 LoRA 학습
- 데이터셋: DevTools에서 생성한 JSONL 파일 사용

사용법:
    python train_lora.py --dataset ../../LoraData/dataset.jsonl --output cheese_cat_lora
"""

import argparse
import json
import os

def check_dependencies():
    """의존성 확인"""
    try:
        import torch
        print(f"PyTorch 버전: {torch.__version__}")
        print(f"CUDA 사용 가능: {torch.cuda.is_available()}")
        if torch.cuda.is_available():
            print(f"CUDA 버전: {torch.version.cuda}")
            print(f"GPU: {torch.cuda.get_device_name(0)}")
    except ImportError:
        print("PyTorch가 설치되지 않았습니다.")
        print("설치: pip install torch")
        return False

    try:
        from unsloth import FastLanguageModel
        print("Unsloth 설치됨")
    except ImportError:
        print("Unsloth가 설치되지 않았습니다.")
        print("설치: pip install 'unsloth[colab-new] @ git+https://github.com/unslothai/unsloth.git'")
        return False

    return True


def load_dataset(dataset_path):
    """JSONL 데이터셋 로드 및 Chat 형식으로 변환"""
    samples = []

    with open(dataset_path, 'r', encoding='utf-8') as f:
        for line in f:
            if not line.strip():
                continue
            data = json.loads(line)

            # messages 형식을 텍스트로 변환
            messages = data.get('messages', [])
            if len(messages) >= 3:
                system_msg = messages[0].get('content', '')
                user_msg = messages[1].get('content', '')
                assistant_msg = messages[2].get('content', '')

                # Llama 3 Chat 형식
                text = f"""<|begin_of_text|><|start_header_id|>system<|end_header_id|>

{system_msg}<|eot_id|><|start_header_id|>user<|end_header_id|>

{user_msg}<|eot_id|><|start_header_id|>assistant<|end_header_id|>

{assistant_msg}<|eot_id|>"""

                samples.append({'text': text})

    print(f"로드된 샘플 수: {len(samples)}")
    return samples


def train(args):
    """LoRA 학습 실행"""
    from unsloth import FastLanguageModel
    import torch
    from datasets import Dataset
    from trl import SFTTrainer
    from transformers import TrainingArguments

    print("\n=== 모델 로드 중 ===")
    model, tokenizer = FastLanguageModel.from_pretrained(
        model_name=args.base_model,
        max_seq_length=args.max_seq_length,
        load_in_4bit=True,
        dtype=None,  # 자동 감지
    )

    print("\n=== LoRA 어댑터 추가 ===")
    model = FastLanguageModel.get_peft_model(
        model,
        r=args.lora_r,
        target_modules=["q_proj", "k_proj", "v_proj", "o_proj",
                       "gate_proj", "up_proj", "down_proj"],
        lora_alpha=args.lora_alpha,
        lora_dropout=0,
        bias="none",
        use_gradient_checkpointing="unsloth",
        random_state=42,
    )

    print("\n=== 데이터셋 로드 ===")
    samples = load_dataset(args.dataset)
    dataset = Dataset.from_list(samples)

    print("\n=== 학습 시작 ===")
    trainer = SFTTrainer(
        model=model,
        tokenizer=tokenizer,
        train_dataset=dataset,
        dataset_text_field="text",
        max_seq_length=args.max_seq_length,
        dataset_num_proc=2,
        packing=False,
        args=TrainingArguments(
            per_device_train_batch_size=args.batch_size,
            gradient_accumulation_steps=args.gradient_accumulation,
            warmup_steps=args.warmup_steps,
            max_steps=args.max_steps,
            learning_rate=args.learning_rate,
            fp16=not torch.cuda.is_bf16_supported(),
            bf16=torch.cuda.is_bf16_supported(),
            logging_steps=10,
            optim="adamw_8bit",
            weight_decay=0.01,
            lr_scheduler_type="linear",
            seed=42,
            output_dir=args.output,
            save_strategy="steps",
            save_steps=50,
        ),
    )

    trainer_stats = trainer.train()

    print("\n=== 학습 완료 ===")
    print(f"학습 시간: {trainer_stats.metrics['train_runtime']:.2f}초")
    print(f"총 스텝: {trainer_stats.metrics['train_steps']}")

    print("\n=== 모델 저장 ===")
    model.save_pretrained(args.output)
    tokenizer.save_pretrained(args.output)
    print(f"저장 위치: {args.output}")

    return model, tokenizer


def test_model(model, tokenizer, prompt):
    """학습된 모델 테스트"""
    from unsloth import FastLanguageModel

    FastLanguageModel.for_inference(model)

    messages = [
        {"role": "system", "content": "너는 주황색 치즈냥이 캐릭터다. 한국어로 1~2문장으로 답한다."},
        {"role": "user", "content": prompt}
    ]

    inputs = tokenizer.apply_chat_template(
        messages,
        tokenize=True,
        add_generation_prompt=True,
        return_tensors="pt"
    ).to("cuda")

    outputs = model.generate(
        input_ids=inputs,
        max_new_tokens=64,
        use_cache=True,
        temperature=0.7,
        top_p=0.9,
    )

    response = tokenizer.decode(outputs[0], skip_special_tokens=True)
    return response


def main():
    parser = argparse.ArgumentParser(description='CatTalk2D LoRA 튜닝')

    # 필수 인자
    parser.add_argument('--dataset', type=str, required=True,
                       help='학습 데이터셋 경로 (JSONL)')
    parser.add_argument('--output', type=str, default='cheese_cat_lora',
                       help='출력 디렉토리')

    # 모델 설정
    parser.add_argument('--base-model', type=str, default='unsloth/llama-3-8b-Instruct',
                       help='기본 모델 (HuggingFace 경로)')
    parser.add_argument('--max-seq-length', type=int, default=2048,
                       help='최대 시퀀스 길이')

    # LoRA 설정
    parser.add_argument('--lora-r', type=int, default=16,
                       help='LoRA rank')
    parser.add_argument('--lora-alpha', type=int, default=16,
                       help='LoRA alpha')

    # 학습 설정
    parser.add_argument('--batch-size', type=int, default=2,
                       help='배치 사이즈')
    parser.add_argument('--gradient-accumulation', type=int, default=4,
                       help='Gradient accumulation steps')
    parser.add_argument('--max-steps', type=int, default=100,
                       help='최대 학습 스텝')
    parser.add_argument('--warmup-steps', type=int, default=10,
                       help='Warmup 스텝')
    parser.add_argument('--learning-rate', type=float, default=2e-4,
                       help='학습률')

    # 기타
    parser.add_argument('--check-deps', action='store_true',
                       help='의존성만 확인')
    parser.add_argument('--test', type=str,
                       help='학습 후 테스트할 프롬프트')

    args = parser.parse_args()

    if args.check_deps:
        check_dependencies()
        return

    if not check_dependencies():
        print("\n의존성 설치 후 다시 실행하세요.")
        return

    model, tokenizer = train(args)

    if args.test:
        print("\n=== 모델 테스트 ===")
        response = test_model(model, tokenizer, args.test)
        print(f"프롬프트: {args.test}")
        print(f"응답: {response}")


if __name__ == '__main__':
    main()
