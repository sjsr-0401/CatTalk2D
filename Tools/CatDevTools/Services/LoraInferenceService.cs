using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CatDevTools.Services;

/// <summary>
/// 학습된 LoRA 모델로 직접 추론 수행
/// Python 스크립트를 통해 실행
/// </summary>
public class LoraInferenceService
{
    private readonly string _pythonPath;
    private readonly string _loraDataPath;
    private readonly string _inferenceScript;

    public LoraInferenceService()
    {
        // Python 경로 (venv 우선)
        var basePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "LoraData");

        _loraDataPath = Path.GetFullPath(basePath);

        var venvPython = Path.Combine(_loraDataPath, "venv", "Scripts", "python.exe");
        _pythonPath = File.Exists(venvPython) ? venvPython : "python";

        _inferenceScript = Path.Combine(_loraDataPath, "inference_api.py");
    }

    /// <summary>
    /// LoRA 모델 경로 유효성 확인
    /// </summary>
    public bool IsValidLoraPath(string loraPath)
    {
        if (string.IsNullOrEmpty(loraPath)) return false;

        // adapter_config.json이 있으면 유효한 LoRA
        var configPath = Path.Combine(loraPath, "adapter_config.json");
        return File.Exists(configPath);
    }

    /// <summary>
    /// 학습된 LoRA 모델 목록 조회
    /// </summary>
    public List<LoraModelInfo> GetTrainedModels()
    {
        var models = new List<LoraModelInfo>();
        var outputsPath = Path.Combine(_loraDataPath, "outputs");

        if (!Directory.Exists(outputsPath)) return models;

        foreach (var dir in Directory.GetDirectories(outputsPath, "cattalk2d_lora_*"))
        {
            var loraPath = Path.Combine(dir, "lora_adapter");
            if (IsValidLoraPath(loraPath))
            {
                var dirInfo = new DirectoryInfo(dir);
                models.Add(new LoraModelInfo
                {
                    Name = dirInfo.Name,
                    Path = loraPath,
                    DisplayName = $"[LoRA] {dirInfo.Name.Replace("cattalk2d_lora_", "")}",
                    Created = dirInfo.CreationTime
                });
            }
        }

        return models;
    }

    /// <summary>
    /// LoRA 모델로 응답 생성
    /// </summary>
    public async Task<string> GenerateAsync(string loraPath, string userInput, string controlJson, string baseModel = "google/gemma-2-2b-it")
    {
        // inference_api.py가 없으면 생성
        await EnsureInferenceScriptAsync();

        var request = new
        {
            lora_path = loraPath,
            base_model = baseModel,
            user_input = userInput,
            control_json = controlJson
        };

        var requestJson = JsonSerializer.Serialize(request);
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, requestJson, Encoding.UTF8);

            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $"\"{_inferenceScript}\" \"{tempFile}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = _loraDataPath
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return $"[Error] {error}";
            }

            // JSON 응답 파싱
            try
            {
                var response = JsonSerializer.Deserialize<InferenceResponse>(output);
                return response?.Response ?? output.Trim();
            }
            catch
            {
                return output.Trim();
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// 배치 추론 (벤치마크용)
    /// </summary>
    public async Task<List<string>> GenerateBatchAsync(
        string loraPath,
        List<(string userInput, string controlJson)> inputs,
        string baseModel = "google/gemma-2-2b-it",
        IProgress<string>? progress = null)
    {
        var results = new List<string>();

        for (int i = 0; i < inputs.Count; i++)
        {
            var (userInput, controlJson) = inputs[i];
            progress?.Report($"LoRA 추론 중... ({i + 1}/{inputs.Count})");

            var response = await GenerateAsync(loraPath, userInput, controlJson, baseModel);
            results.Add(response);
        }

        return results;
    }

    /// <summary>
    /// 추론 스크립트 생성 (없으면)
    /// </summary>
    private async Task EnsureInferenceScriptAsync()
    {
        if (File.Exists(_inferenceScript)) return;

        var script = @"'''
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
";

        await File.WriteAllTextAsync(_inferenceScript, script, Encoding.UTF8);
    }

    private class InferenceResponse
    {
        public string Response { get; set; } = "";
        public string? Error { get; set; }
    }
}

public class LoraModelInfo
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateTime Created { get; set; }
}
