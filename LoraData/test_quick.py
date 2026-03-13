import torch
from transformers import AutoModelForCausalLM, AutoTokenizer
from peft import PeftModel
import sys
import io

# UTF-8 설정
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

base_model = "EleutherAI/polyglot-ko-1.3b"
lora_path = r"C:\Users\admin\CatTalk2D\LoraData\outputs\cattalk2d_lora_20260122_2302\lora_adapter"

print("Loading model...")
tokenizer = AutoTokenizer.from_pretrained(base_model)
model = AutoModelForCausalLM.from_pretrained(base_model, torch_dtype=torch.float16, device_map="auto")
model = PeftModel.from_pretrained(model, lora_path)
print("Model loaded!")

def chat(user_input, control='{"trustTier":"mid","moodTag":"neutral"}'):
    system = "너는 '망고'라는 이름의 고양이 캐릭터다. 반드시 한국어로만 대답하고, 문장 끝에 '냥'을 붙인다."
    prompt = f"### 시스템:\n{system}\n\n### 사용자:\n[CONTROL]{control}\n[USER]{user_input}\n\n### 고양이:\n"

    inputs = tokenizer(prompt, return_tensors="pt")
    input_ids = inputs["input_ids"].to(model.device)
    attention_mask = inputs["attention_mask"].to(model.device)
    with torch.no_grad():
        outputs = model.generate(input_ids=input_ids, attention_mask=attention_mask, max_new_tokens=50, temperature=0.7, do_sample=True, pad_token_id=tokenizer.eos_token_id)

    response = tokenizer.decode(outputs[0], skip_special_tokens=True)
    return response.split("### 고양이:\n")[-1].split("###")[0].strip()

# 테스트
tests = [
    ("안녕?", '{"trustTier":"mid","moodTag":"neutral"}'),
    ("배고파?", '{"trustTier":"high","moodTag":"hungry"}'),
    ("놀아줄까?", '{"trustTier":"high","moodTag":"playful"}'),
    ("왜 그렇게 무뚝뚝해?", '{"trustTier":"low","moodTag":"grumpy"}'),
]

results = []
for user_input, control in tests:
    response = chat(user_input, control)
    results.append(f"Input: {user_input}\nControl: {control}\nResponse: {response}\n")

# 결과 파일로 저장
with open(r"C:\Users\admin\CatTalk2D\LoraData\test_results.txt", 'w', encoding='utf-8') as f:
    f.write("=" * 60 + "\n")
    f.write("CatTalk2D LoRA Model Test Results\n")
    f.write("=" * 60 + "\n\n")
    for r in results:
        f.write(r + "\n")
        f.write("-" * 40 + "\n")

print("Test results saved to test_results.txt")
