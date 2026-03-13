"""
CatTalk2D LoRA 모델 테스트 스크립트

Ollama 변환 없이 직접 LoRA 모델을 테스트합니다.

사용법:
    python test_lora.py --lora outputs/cattalk2d_lora_20260124_2056/lora_adapter

요구사항:
    pip install torch transformers peft
"""

import argparse
import json
import sys
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description='Test LoRA model directly')
    parser.add_argument('--lora', type=str, required=True, help='Path to LoRA adapter folder')
    parser.add_argument('--base-model', type=str, default='google/gemma-2-2b-it', help='Base model')
    parser.add_argument('--interactive', action='store_true', help='Interactive mode')
    args = parser.parse_args()

    lora_path = Path(args.lora)
    if not lora_path.exists():
        print(f"Error: LoRA path not found: {lora_path}")
        sys.exit(1)

    print("=" * 60)
    print("CatTalk2D LoRA Test")
    print("=" * 60)
    print(f"Base: {args.base_model}")
    print(f"LoRA: {lora_path}")
    print("=" * 60)

    # 모델 로드
    print("\nLoading model...")
    model, tokenizer = load_model(args.base_model, str(lora_path))

    # 테스트 케이스
    test_cases = [
        {"user": "안녕?", "control": {"trustTier": "mid", "moodTag": "neutral"}},
        {"user": "배고파?", "control": {"trustTier": "low", "needTop1": "food"}},
        {"user": "놀자!", "control": {"trustTier": "high", "needTop1": "play", "timeBlock": "night"}},
        {"user": "쓰다듬어줄게", "control": {"trustTier": "high", "needTop1": "affection"}},
        {"user": "뭐해?", "control": {"trustTier": "mid", "timeBlock": "afternoon"}},
    ]

    print("\n" + "=" * 60)
    print("Test Results")
    print("=" * 60)

    for i, tc in enumerate(test_cases, 1):
        control_json = json.dumps(tc["control"], ensure_ascii=False)
        response = generate(model, tokenizer, tc["user"], control_json)

        print(f"\n[Test {i}]")
        print(f"  Control: {tc['control']}")
        print(f"  User: {tc['user']}")
        print(f"  Cat: {response}")

    if args.interactive:
        print("\n" + "=" * 60)
        print("Interactive Mode (type 'quit' to exit)")
        print("=" * 60)

        while True:
            user_input = input("\nYou: ").strip()
            if user_input.lower() in ['quit', 'exit', 'q']:
                break

            # 기본 컨트롤
            control = {"trustTier": "mid", "moodTag": "neutral"}
            control_json = json.dumps(control, ensure_ascii=False)

            response = generate(model, tokenizer, user_input, control_json)
            print(f"Cat: {response}")


def load_model(base_model: str, lora_path: str):
    """모델 로드"""
    import torch
    from transformers import AutoModelForCausalLM, AutoTokenizer
    from peft import PeftModel

    tokenizer = AutoTokenizer.from_pretrained(base_model, trust_remote_code=True)
    model = AutoModelForCausalLM.from_pretrained(
        base_model,
        torch_dtype=torch.float16,
        device_map="auto",
        trust_remote_code=True,
    )
    model = PeftModel.from_pretrained(model, lora_path)
    model.eval()

    return model, tokenizer


def generate(model, tokenizer, user_input: str, control_json: str = "{}"):
    """응답 생성"""
    import torch

    system = """당신은 '망고'라는 이름의 고양이입니다.
- 행동은 (괄호) 안에 표현합니다
- 1-2문장으로 짧게 답합니다"""

    prompt = f"<start_of_turn>user\n{system}\n\n[CONTROL]{control_json}\n[USER]{user_input}<end_of_turn>\n<start_of_turn>model\n"

    inputs = tokenizer(prompt, return_tensors="pt").to(model.device)

    with torch.no_grad():
        outputs = model.generate(
            **inputs,
            max_new_tokens=80,
            temperature=0.7,
            top_p=0.9,
            do_sample=True,
            pad_token_id=tokenizer.eos_token_id,
        )

    response = tokenizer.decode(outputs[0], skip_special_tokens=True)

    # 응답 추출
    if "<start_of_turn>model" in response:
        response = response.split("<start_of_turn>model")[-1]
    if "<end_of_turn>" in response:
        response = response.split("<end_of_turn>")[0]

    return response.strip()


if __name__ == "__main__":
    main()
