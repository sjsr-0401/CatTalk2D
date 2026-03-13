"""
CatTalk2D LoRA → Ollama 변환 스크립트

사용법:
    python convert_to_ollama.py --lora outputs/cattalk2d_lora_20260124_2056/lora_adapter --name cattalk-v1

요구사항:
    pip install torch transformers peft llama-cpp-python
    ollama가 설치되어 있어야 함
"""

import argparse
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description='Convert LoRA to Ollama model')
    parser.add_argument('--lora', type=str, required=True, help='Path to LoRA adapter folder')
    parser.add_argument('--name', type=str, default='cattalk-custom', help='Name for Ollama model')
    parser.add_argument('--base-model', type=str, default='google/gemma-2-2b-it', help='Base model name')
    parser.add_argument('--quantize', type=str, default='q4_k_m',
                        choices=['f16', 'q8_0', 'q4_k_m', 'q4_0'], help='Quantization type')
    parser.add_argument('--skip-merge', action='store_true', help='Skip merging if already done')
    args = parser.parse_args()

    lora_path = Path(args.lora)
    if not lora_path.exists():
        print(f"Error: LoRA path not found: {lora_path}")
        sys.exit(1)

    output_dir = lora_path.parent
    merged_path = output_dir / "merged_model"
    gguf_path = output_dir / f"{args.name}.gguf"
    modelfile_path = output_dir / "Modelfile"

    print("=" * 60)
    print("CatTalk2D LoRA → Ollama Converter")
    print("=" * 60)
    print(f"LoRA: {lora_path}")
    print(f"Base Model: {args.base_model}")
    print(f"Output Name: {args.name}")
    print(f"Quantization: {args.quantize}")
    print("=" * 60)

    # Step 1: Merge LoRA with base model
    if not args.skip_merge:
        print("\n[1/4] Merging LoRA with base model...")
        merge_lora(args.base_model, str(lora_path), str(merged_path))
    else:
        print("\n[1/4] Skipping merge (--skip-merge)")

    # Step 2: Convert to GGUF
    print("\n[2/4] Converting to GGUF format...")
    convert_to_gguf(str(merged_path), str(gguf_path), args.quantize)

    # Step 3: Create Modelfile
    print("\n[3/4] Creating Modelfile...")
    create_modelfile(str(gguf_path), str(modelfile_path))

    # Step 4: Register with Ollama
    print("\n[4/4] Registering with Ollama...")
    register_ollama(args.name, str(modelfile_path))

    print("\n" + "=" * 60)
    print("Conversion complete!")
    print("=" * 60)
    print(f"\nOllama model created: {args.name}")
    print(f"\nTo test:")
    print(f"  ollama run {args.name} \"안녕 망고야!\"")
    print(f"\nThe model should now appear in DevTools benchmark!")


def merge_lora(base_model: str, lora_path: str, output_path: str):
    """LoRA를 베이스 모델에 병합"""
    try:
        import torch
        from transformers import AutoModelForCausalLM, AutoTokenizer
        from peft import PeftModel
    except ImportError:
        print("Error: Required packages not found.")
        print("Install: pip install torch transformers peft")
        sys.exit(1)

    print(f"  Loading base model: {base_model}")
    tokenizer = AutoTokenizer.from_pretrained(base_model, trust_remote_code=True)
    model = AutoModelForCausalLM.from_pretrained(
        base_model,
        torch_dtype=torch.float16,
        device_map="auto",
        trust_remote_code=True,
    )

    print(f"  Loading LoRA adapter: {lora_path}")
    model = PeftModel.from_pretrained(model, lora_path)

    print("  Merging weights...")
    model = model.merge_and_unload()

    print(f"  Saving merged model: {output_path}")
    os.makedirs(output_path, exist_ok=True)
    model.save_pretrained(output_path, safe_serialization=True)
    tokenizer.save_pretrained(output_path)

    print("  Merge complete!")


def convert_to_gguf(model_path: str, gguf_path: str, quantize: str):
    """모델을 GGUF 형식으로 변환"""

    # llama.cpp convert script 경로 찾기
    llama_cpp_paths = [
        Path.home() / "llama.cpp",
        Path("C:/llama.cpp"),
        Path("D:/llama.cpp"),
    ]

    convert_script = None
    for llama_path in llama_cpp_paths:
        candidate = llama_path / "convert_hf_to_gguf.py"
        if candidate.exists():
            convert_script = candidate
            break

    if convert_script is None:
        print("  Warning: llama.cpp not found. Trying alternative method...")
        convert_with_llamacpp_python(model_path, gguf_path, quantize)
        return

    print(f"  Using llama.cpp: {convert_script.parent}")

    # HF → GGUF 변환
    cmd = [
        sys.executable, str(convert_script),
        model_path,
        "--outfile", gguf_path,
        "--outtype", quantize,
    ]

    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"  Error: {result.stderr}")
        print("  Trying alternative method...")
        convert_with_llamacpp_python(model_path, gguf_path, quantize)
    else:
        print(f"  GGUF saved: {gguf_path}")


def convert_with_llamacpp_python(model_path: str, gguf_path: str, quantize: str):
    """llama-cpp-python을 사용한 대체 변환 방법"""
    print("  Alternative: Using Hugging Face Hub GGUF...")

    # 간단한 방법: TheBloke나 다른 변환된 모델 사용 안내
    print("""
  ========================================
  GGUF 변환을 위해 다음 중 하나를 선택하세요:

  방법 1: llama.cpp 설치 (권장)
    git clone https://github.com/ggerganov/llama.cpp
    cd llama.cpp
    pip install -r requirements.txt
    python convert_hf_to_gguf.py {model_path} --outfile {gguf_path}

  방법 2: 온라인 변환 서비스 사용
    https://huggingface.co/spaces/ggml-org/gguf-my-repo

  방법 3: 이미 GGUF인 모델로 직접 학습
    DevTools에서 'gemma-2-2b-it' 대신 GGUF 베이스 모델 사용
  ========================================
  """.format(model_path=model_path, gguf_path=gguf_path))

    # 임시로 더미 파일 생성 (나중에 수동 교체 필요)
    with open(gguf_path, 'w') as f:
        f.write("# PLACEHOLDER - Replace with actual GGUF file\n")
    print(f"  Placeholder created: {gguf_path}")
    print("  Please replace with actual GGUF file!")


def create_modelfile(gguf_path: str, modelfile_path: str):
    """Ollama Modelfile 생성"""
    content = f'''FROM {gguf_path}

TEMPLATE """<start_of_turn>user
{{{{ .System }}}}

{{{{ .Prompt }}}}<end_of_turn>
<start_of_turn>model
{{{{ .Response }}}}<end_of_turn>
"""

SYSTEM """당신은 '망고'라는 이름의 고양이입니다.
- 모든 문장 끝에 '냥'을 붙입니다
- 행동은 (괄호) 안에 표현합니다
- 1-2문장으로 짧게 답합니다
- 고양이답게 츤데레하거나 애교 있게 반응합니다"""

PARAMETER temperature 0.7
PARAMETER top_p 0.9
PARAMETER repeat_penalty 1.1
PARAMETER num_ctx 2048
'''

    with open(modelfile_path, 'w', encoding='utf-8') as f:
        f.write(content)

    print(f"  Modelfile saved: {modelfile_path}")


def register_ollama(name: str, modelfile_path: str):
    """Ollama에 모델 등록"""
    cmd = ["ollama", "create", name, "-f", modelfile_path]

    print(f"  Running: {' '.join(cmd)}")

    try:
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=300)
        if result.returncode == 0:
            print(f"  Success! Model '{name}' registered with Ollama")
        else:
            print(f"  Error: {result.stderr}")
            print("\n  Manual registration:")
            print(f"    ollama create {name} -f {modelfile_path}")
    except FileNotFoundError:
        print("  Error: Ollama not found in PATH")
        print("\n  Manual registration:")
        print(f"    ollama create {name} -f {modelfile_path}")
    except subprocess.TimeoutExpired:
        print("  Timeout - registration may still be in progress")


if __name__ == "__main__":
    main()
