'''
CatTalk2D LoRA Inference API
벤치마크에서 LoRA 모델 추론용
'''
import json
import sys
import torch
from transformers import AutoModelForCausalLM, AutoTokenizer
from peft import PeftModel

# 모델 캐시 (재로딩 방지)
_model_cache = {}

def load_model(base_model: str, lora_path: str):
    cache_key = f'{base_model}:{lora_path}'
    if cache_key in _model_cache:
        return _model_cache[cache_key]

    tokenizer = AutoTokenizer.from_pretrained(base_model, trust_remote_code=True)
    model = AutoModelForCausalLM.from_pretrained(
        base_model,
        torch_dtype=torch.float16,
        device_map='auto',
        trust_remote_code=True,
    )
    model = PeftModel.from_pretrained(model, lora_path)
    model.eval()

    _model_cache[cache_key] = (model, tokenizer)
    return model, tokenizer

def generate(model, tokenizer, user_input: str, control_json: str = '{}'):
    system = '''당신은 '망고'라는 이름의 고양이입니다.
- 모든 문장 끝에 '냥'을 붙입니다
- 행동은 (괄호) 안에 표현합니다
- 1-2문장으로 짧게 답합니다'''

    prompt = f'<start_of_turn>user\n{system}\n\n[CONTROL]{control_json}\n[USER]{user_input}<end_of_turn>\n<start_of_turn>model\n'

    inputs = tokenizer(prompt, return_tensors='pt').to(model.device)

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

    if '<start_of_turn>model' in response:
        response = response.split('<start_of_turn>model')[-1]
    if '<end_of_turn>' in response:
        response = response.split('<end_of_turn>')[0]

    return response.strip()

def main():
    if len(sys.argv) < 2:
        print(json.dumps({'error': 'No input file provided'}))
        sys.exit(1)

    input_file = sys.argv[1]
    with open(input_file, 'r', encoding='utf-8') as f:
        request = json.load(f)

    lora_path = request.get('lora_path', '')
    base_model = request.get('base_model', 'google/gemma-2-2b-it')
    user_input = request.get('user_input', '')
    control_json = request.get('control_json', '{}')

    try:
        model, tokenizer = load_model(base_model, lora_path)
        response = generate(model, tokenizer, user_input, control_json)
        print(json.dumps({'response': response}, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({'error': str(e)}, ensure_ascii=False))
        sys.exit(1)

if __name__ == '__main__':
    main()
