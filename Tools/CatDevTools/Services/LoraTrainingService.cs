using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatDevTools.Services;

/// <summary>
/// LoRA 학습 및 Ollama 등록 서비스
/// </summary>
public class LoraTrainingService
{
    private Process? _trainingProcess;

    /// <summary>
    /// 사용 가능한 베이스 모델 목록 (GGUF 변환 지원)
    /// </summary>
    public static readonly (string Id, string Name, string Architecture, bool GgufSupport)[] AvailableModels = new[]
    {
        // GGUF 변환 지원 모델 (권장)
        ("Qwen/Qwen2.5-1.5B-Instruct", "Qwen2.5 1.5B (한국어 좋음, 권장)", "Qwen2", true),
        ("Qwen/Qwen2.5-3B-Instruct", "Qwen2.5 3B (한국어 우수)", "Qwen2", true),
        ("google/gemma-2-2b-it", "Gemma2 2B (균형)", "Gemma2", true),
        ("TinyLlama/TinyLlama-1.1B-Chat-v1.0", "TinyLlama 1.1B (경량)", "Llama", true),
        ("meta-llama/Meta-Llama-3-8B-Instruct", "Llama 3 8B (고품질, VRAM 16GB+)", "Llama3", true),

        // GGUF 변환 불가 (레거시)
        ("EleutherAI/polyglot-ko-1.3b", "Polyglot-ko 1.3B (GGUF 불가)", "GPT-NeoX", false),
    };

    /// <summary>
    /// Python 가상환경 경로
    /// </summary>
    public string VenvPath { get; set; } = "";

    /// <summary>
    /// 학습 스크립트 경로
    /// </summary>
    public string TrainScriptPath { get; set; } = "";

    /// <summary>
    /// 학습 데이터 경로
    /// </summary>
    public string TrainingDataPath { get; set; } = "";

    /// <summary>
    /// 출력 경로
    /// </summary>
    public string OutputPath { get; set; } = "";

    /// <summary>
    /// 선택된 베이스 모델
    /// </summary>
    public string BaseModel { get; set; } = "Qwen/Qwen2.5-1.5B-Instruct";

    /// <summary>
    /// 학습 에폭 수
    /// </summary>
    public int Epochs { get; set; } = 3;

    /// <summary>
    /// LoRA Rank
    /// </summary>
    public int LoraR { get; set; } = 16;

    /// <summary>
    /// LoRA Alpha
    /// </summary>
    public int LoraAlpha { get; set; } = 32;

    /// <summary>
    /// 학습률
    /// </summary>
    public double LearningRate { get; set; } = 2e-4;

    public LoraTrainingService()
    {
        // 기본 경로 설정
        var loraDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "CatTalk2D", "LoraData");

        VenvPath = Path.Combine(loraDataPath, "venv");
        TrainScriptPath = Path.Combine(loraDataPath, "train_cattalk.py");
        OutputPath = Path.Combine(loraDataPath, "outputs");
    }

    /// <summary>
    /// Python 환경 확인
    /// </summary>
    public async Task<(bool success, string message)> CheckEnvironmentAsync()
    {
        var pythonExe = Path.Combine(VenvPath, "Scripts", "python.exe");

        if (!File.Exists(pythonExe))
        {
            return (false, $"Python 가상환경을 찾을 수 없습니다.\n경로: {VenvPath}");
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = "-c \"import torch; print(f'PyTorch {torch.__version__}, CUDA: {torch.cuda.is_available()}')\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return (false, "프로세스 시작 실패");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                return (true, output.Trim());
            }
            else
            {
                return (false, $"오류: {error}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"환경 확인 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 학습 스크립트 생성
    /// </summary>
    public void GenerateTrainScript()
    {
        // 모델별 LoRA 타겟 모듈
        string targetModules;
        if (BaseModel.Contains("polyglot") || BaseModel.Contains("KoAlpaca"))
            targetModules = "[\"query_key_value\", \"dense\", \"dense_h_to_4h\", \"dense_4h_to_h\"]";
        else // Qwen, Gemma, Llama 계열 모두 동일
            targetModules = "[\"q_proj\", \"k_proj\", \"v_proj\", \"o_proj\", \"gate_proj\", \"up_proj\", \"down_proj\"]";

        // 모델별 프롬프트 포맷
        string promptFormat;
        if (BaseModel.Contains("polyglot") || BaseModel.Contains("KoAlpaca"))
            promptFormat = "### 시스템:\\n{system}\\n\\n### 사용자:\\n{user}\\n\\n### 고양이:\\n{assistant}";
        else if (BaseModel.Contains("Qwen"))
            promptFormat = "<|im_start|>system\\n{system}<|im_end|>\\n<|im_start|>user\\n{user}<|im_end|>\\n<|im_start|>assistant\\n{assistant}<|im_end|>";
        else if (BaseModel.Contains("gemma"))
            promptFormat = "<start_of_turn>user\\n{system}\\n\\n{user}<end_of_turn>\\n<start_of_turn>model\\n{assistant}<end_of_turn>";
        else if (BaseModel.Contains("Llama-3") || BaseModel.Contains("llama-3"))
            promptFormat = "<|begin_of_text|><|start_header_id|>system<|end_header_id|>\\n\\n{system}<|eot_id|><|start_header_id|>user<|end_header_id|}\\n\\n{user}<|eot_id|><|start_header_id|>assistant<|end_header_id|>\\n\\n{assistant}<|eot_id|>";
        else
            promptFormat = "<|system|>\\n{system}</s>\\n<|user|>\\n{user}</s>\\n<|assistant|>\\n{assistant}</s>";

        var sb = new StringBuilder();
        sb.AppendLine("\"\"\"");
        sb.AppendLine("CatTalk2D LoRA 학습 스크립트");
        sb.AppendLine("DevTools에서 자동 생성됨");
        sb.AppendLine("\"\"\"");
        sb.AppendLine();
        sb.AppendLine("import json");
        sb.AppendLine("import os");
        sb.AppendLine("from datetime import datetime");
        sb.AppendLine();
        sb.AppendLine("# 설정");
        sb.AppendLine($"DATA_PATH = r\"{TrainingDataPath}\"");
        sb.AppendLine($"BASE_MODEL = \"{BaseModel}\"");
        sb.AppendLine($"OUTPUT_DIR = r\"{OutputPath}\"");
        sb.AppendLine($"EPOCHS = {Epochs}");
        sb.AppendLine("BATCH_SIZE = 1");
        sb.AppendLine("GRAD_ACCUM = 8");
        sb.AppendLine($"LORA_R = {LoraR}");
        sb.AppendLine($"LORA_ALPHA = {LoraAlpha}");
        sb.AppendLine($"LR = {LearningRate}");
        sb.AppendLine();
        sb.AppendLine("print(\"=\" * 60)");
        sb.AppendLine("print(\"CatTalk2D LoRA Training\")");
        sb.AppendLine("print(\"=\" * 60)");
        sb.AppendLine();
        sb.AppendLine("import torch");
        sb.AppendLine("print(f\"PyTorch: {torch.__version__}\")");
        sb.AppendLine("print(f\"CUDA: {torch.cuda.is_available()}\")");
        sb.AppendLine("if torch.cuda.is_available():");
        sb.AppendLine("    print(f\"GPU: {torch.cuda.get_device_name(0)}\")");
        sb.AppendLine("    print(f\"VRAM: {torch.cuda.get_device_properties(0).total_memory / 1e9:.1f} GB\")");
        sb.AppendLine();
        sb.AppendLine("# 1. 데이터 로드");
        sb.AppendLine("print(\"\\n[1/5] Loading training data...\")");
        sb.AppendLine("with open(DATA_PATH, 'r', encoding='utf-8-sig') as f:");
        sb.AppendLine("    data = [json.loads(line) for line in f]");
        sb.AppendLine("print(f\"  Loaded {len(data)} samples\")");
        sb.AppendLine();
        sb.AppendLine("# 데이터 변환");
        sb.AppendLine("def format_sample(sample):");
        sb.AppendLine("    messages = sample['messages']");
        sb.AppendLine("    system = next((m['content'] for m in messages if m['role'] == 'system'), '')");
        sb.AppendLine("    user = next((m['content'] for m in messages if m['role'] == 'user'), '')");
        sb.AppendLine("    assistant = next((m['content'] for m in messages if m['role'] == 'assistant'), '')");
        sb.AppendLine($"    text = f\"{promptFormat}\"");
        sb.AppendLine("    return {'text': text}");
        sb.AppendLine();
        sb.AppendLine("from datasets import Dataset");
        sb.AppendLine("formatted_data = [format_sample(s) for s in data]");
        sb.AppendLine("dataset = Dataset.from_list(formatted_data)");
        sb.AppendLine("print(f\"  Dataset ready: {len(dataset)} samples\")");
        sb.AppendLine();
        sb.AppendLine("# 2. 모델 로드");
        sb.AppendLine("print(\"\\n[2/5] Loading base model...\")");
        sb.AppendLine("from transformers import AutoModelForCausalLM, AutoTokenizer, BitsAndBytesConfig");
        sb.AppendLine();
        sb.AppendLine("bnb_config = BitsAndBytesConfig(");
        sb.AppendLine("    load_in_4bit=True,");
        sb.AppendLine("    bnb_4bit_use_double_quant=True,");
        sb.AppendLine("    bnb_4bit_quant_type=\"nf4\",");
        sb.AppendLine("    bnb_4bit_compute_dtype=torch.float16");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("model = AutoModelForCausalLM.from_pretrained(");
        sb.AppendLine("    BASE_MODEL,");
        sb.AppendLine("    quantization_config=bnb_config,");
        sb.AppendLine("    device_map=\"auto\",");
        sb.AppendLine("    trust_remote_code=True,");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("tokenizer = AutoTokenizer.from_pretrained(BASE_MODEL, trust_remote_code=True)");
        sb.AppendLine("tokenizer.pad_token = tokenizer.eos_token");
        sb.AppendLine("tokenizer.padding_side = \"right\"");
        sb.AppendLine("print(f\"  Model loaded: {BASE_MODEL}\")");
        sb.AppendLine();
        sb.AppendLine("# 3. LoRA 설정");
        sb.AppendLine("print(\"\\n[3/5] Configuring LoRA...\")");
        sb.AppendLine("from peft import LoraConfig, get_peft_model, prepare_model_for_kbit_training");
        sb.AppendLine();
        sb.AppendLine("model = prepare_model_for_kbit_training(model)");
        sb.AppendLine();
        sb.AppendLine("lora_config = LoraConfig(");
        sb.AppendLine("    r=LORA_R,");
        sb.AppendLine("    lora_alpha=LORA_ALPHA,");
        sb.AppendLine($"    target_modules={targetModules},");
        sb.AppendLine("    lora_dropout=0.05,");
        sb.AppendLine("    bias=\"none\",");
        sb.AppendLine("    task_type=\"CAUSAL_LM\",");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("model = get_peft_model(model, lora_config)");
        sb.AppendLine("model.print_trainable_parameters()");
        sb.AppendLine();
        sb.AppendLine("# 4. 학습 설정");
        sb.AppendLine("print(\"\\n[4/5] Setting up training...\")");
        sb.AppendLine("timestamp = datetime.now().strftime(\"%Y%m%d_%H%M\")");
        sb.AppendLine("output_dir = os.path.join(OUTPUT_DIR, f\"cattalk2d_lora_{timestamp}\")");
        sb.AppendLine("os.makedirs(output_dir, exist_ok=True)");
        sb.AppendLine();
        sb.AppendLine("from trl import SFTTrainer, SFTConfig");
        sb.AppendLine();
        sb.AppendLine("training_args = SFTConfig(");
        sb.AppendLine("    output_dir=output_dir,");
        sb.AppendLine("    per_device_train_batch_size=BATCH_SIZE,");
        sb.AppendLine("    gradient_accumulation_steps=GRAD_ACCUM,");
        sb.AppendLine("    warmup_steps=10,");
        sb.AppendLine("    num_train_epochs=EPOCHS,");
        sb.AppendLine("    learning_rate=LR,");
        sb.AppendLine("    fp16=False,");
        sb.AppendLine("    bf16=False,");
        sb.AppendLine("    logging_steps=10,");
        sb.AppendLine("    save_steps=50,");
        sb.AppendLine("    save_total_limit=2,");
        sb.AppendLine("    optim=\"adamw_torch\",");
        sb.AppendLine("    weight_decay=0.01,");
        sb.AppendLine("    lr_scheduler_type=\"cosine\",");
        sb.AppendLine("    seed=42,");
        sb.AppendLine("    report_to=\"none\",");
        sb.AppendLine("    dataset_text_field=\"text\",");
        sb.AppendLine("    max_length=1024,");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("trainer = SFTTrainer(");
        sb.AppendLine("    model=model,");
        sb.AppendLine("    processing_class=tokenizer,");
        sb.AppendLine("    train_dataset=dataset,");
        sb.AppendLine("    args=training_args,");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("# 5. 학습");
        sb.AppendLine("print(\"\\n[5/5] Starting training...\")");
        sb.AppendLine("print(\"-\" * 40)");
        sb.AppendLine();
        sb.AppendLine("trainer.train()");
        sb.AppendLine();
        sb.AppendLine("# 6. 저장");
        sb.AppendLine("print(\"\\n[6/6] Saving model...\")");
        sb.AppendLine("lora_path = os.path.join(output_dir, \"lora_adapter\")");
        sb.AppendLine("model.save_pretrained(lora_path)");
        sb.AppendLine("tokenizer.save_pretrained(lora_path)");
        sb.AppendLine("print(f\"  Saved to: {lora_path}\")");
        sb.AppendLine();
        sb.AppendLine("print(f\"\\n{'='*60}\")");
        sb.AppendLine("print(\"Training complete!\")");
        sb.AppendLine("print(f\"{'='*60}\")");
        sb.AppendLine("print(f\"\\nOutput: {output_dir}\")");

        Directory.CreateDirectory(Path.GetDirectoryName(TrainScriptPath)!);
        File.WriteAllText(TrainScriptPath, sb.ToString());
    }

    /// <summary>
    /// 학습 실행
    /// </summary>
    public async Task<bool> RunTrainingAsync(IProgress<string>? progress = null)
    {
        var pythonExe = Path.Combine(VenvPath, "Scripts", "python.exe");

        if (!File.Exists(pythonExe))
        {
            progress?.Report("오류: Python 가상환경을 찾을 수 없습니다.");
            return false;
        }

        if (!File.Exists(TrainScriptPath))
        {
            progress?.Report("학습 스크립트 생성 중...");
            GenerateTrainScript();
        }

        progress?.Report("학습 시작...");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{TrainScriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(TrainScriptPath)
            };

            _trainingProcess = new Process { StartInfo = startInfo };
            _trainingProcess.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    progress?.Report(e.Data);
            };
            _trainingProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    progress?.Report($"[ERR] {e.Data}");
            };

            _trainingProcess.Start();
            _trainingProcess.BeginOutputReadLine();
            _trainingProcess.BeginErrorReadLine();

            await _trainingProcess.WaitForExitAsync();

            var success = _trainingProcess.ExitCode == 0;
            progress?.Report(success ? "✅ 학습 완료!" : $"❌ 학습 실패 (exit code: {_trainingProcess.ExitCode})");

            return success;
        }
        catch (Exception ex)
        {
            progress?.Report($"오류: {ex.Message}");
            return false;
        }
        finally
        {
            _trainingProcess = null;
        }
    }

    /// <summary>
    /// 학습 중단
    /// </summary>
    public void CancelTraining()
    {
        if (_trainingProcess != null && !_trainingProcess.HasExited)
        {
            _trainingProcess.Kill(true);
        }
    }

    /// <summary>
    /// 최신 학습 결과 경로 가져오기
    /// </summary>
    public string? GetLatestLoraPath()
    {
        if (!Directory.Exists(OutputPath)) return null;

        var dirs = Directory.GetDirectories(OutputPath, "cattalk2d_lora_*")
            .OrderByDescending(d => Directory.GetCreationTime(d))
            .FirstOrDefault();

        if (dirs == null) return null;

        var loraPath = Path.Combine(dirs, "lora_adapter");
        return Directory.Exists(loraPath) ? loraPath : null;
    }

    /// <summary>
    /// 학습된 LoRA 모델 목록
    /// </summary>
    public List<(string name, string path, DateTime created)> GetTrainedModels()
    {
        var models = new List<(string, string, DateTime)>();

        if (!Directory.Exists(OutputPath)) return models;

        foreach (var dir in Directory.GetDirectories(OutputPath, "cattalk2d_lora_*"))
        {
            var loraPath = Path.Combine(dir, "lora_adapter");
            if (Directory.Exists(loraPath))
            {
                var name = Path.GetFileName(dir);
                var created = Directory.GetCreationTime(dir);
                models.Add((name, loraPath, created));
            }
        }

        return models.OrderByDescending(m => m.Item3).ToList();
    }

    /// <summary>
    /// GGUF 변환 및 Ollama 등록
    /// </summary>
    public async Task<(bool success, string message)> ConvertToGgufAndRegisterAsync(
        string loraPath,
        string modelName,
        IProgress<string>? progress = null,
        string quantize = "q8_0")
    {
        var pythonExe = Path.Combine(VenvPath, "Scripts", "python.exe");
        if (!File.Exists(pythonExe))
        {
            return (false, "Python 가상환경을 찾을 수 없습니다.");
        }

        // llama.cpp 경로
        var llamaCppPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "llama.cpp");
        var convertScript = Path.Combine(llamaCppPath, "convert_hf_to_gguf.py");

        if (!File.Exists(convertScript))
        {
            return (false, $"llama.cpp를 찾을 수 없습니다.\n경로: {llamaCppPath}");
        }

        var loraDir = Path.GetDirectoryName(loraPath)!;
        var mergedPath = Path.Combine(loraDir, "merged_model");
        var ggufPath = Path.Combine(loraDir, $"{modelName}.gguf");
        var modelfilePath = Path.Combine(loraDir, "Modelfile");

        try
        {
            // Step 1: LoRA 병합
            progress?.Report("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            progress?.Report("[1/4] LoRA 병합 중...");
            var (mergeSuccess, detectedBaseModel) = await MergeLoraAsync(pythonExe, loraPath, mergedPath, progress);
            if (!mergeSuccess)
            {
                return (false, "LoRA 병합 실패");
            }

            // Step 2: GGUF 변환
            progress?.Report("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            progress?.Report($"[2/4] GGUF 변환 중... (양자화: {quantize})");
            var convertSuccess = await ConvertToGgufAsync(pythonExe, convertScript, mergedPath, ggufPath, quantize, progress);
            if (!convertSuccess)
            {
                return (false, "GGUF 변환 실패");
            }

            // Step 3: Modelfile 생성
            progress?.Report("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            progress?.Report("[3/4] Modelfile 생성 중...");
            CreateModelfile(ggufPath, modelfilePath, detectedBaseModel);
            progress?.Report($"  ✓ Modelfile 생성: {modelfilePath}");

            // Step 4: Ollama 등록
            progress?.Report("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            progress?.Report("[4/4] Ollama에 등록 중...");
            var registerSuccess = await RegisterToOllamaAsync(modelName, modelfilePath, progress);
            if (!registerSuccess)
            {
                progress?.Report("⚠️ Ollama 등록 실패. 수동 등록:");
                progress?.Report($"  ollama create {modelName} -f \"{modelfilePath}\"");
                return (false, "Ollama 등록 실패 (수동 등록 필요)");
            }

            progress?.Report("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            progress?.Report($"✅ 변환 완료! 모델: {modelName}");
            progress?.Report($"테스트: ollama run {modelName} \"안녕 망고야!\"");

            return (true, $"모델 '{modelName}' 등록 완료");
        }
        catch (Exception ex)
        {
            progress?.Report($"❌ 오류: {ex.Message}");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// LoRA를 베이스 모델에 병합
    /// </summary>
    /// <returns>(성공 여부, 베이스 모델 ID)</returns>
    private async Task<(bool success, string baseModel)> MergeLoraAsync(string pythonExe, string loraPath, string outputPath, IProgress<string>? progress)
    {
        // adapter_config.json에서 base_model_name_or_path 읽기
        var configPath = Path.Combine(loraPath, "adapter_config.json");
        string baseModel = "Qwen/Qwen2.5-1.5B-Instruct"; // 기본값

        if (File.Exists(configPath))
        {
            try
            {
                var configJson = await File.ReadAllTextAsync(configPath);
                var config = System.Text.Json.JsonDocument.Parse(configJson);
                if (config.RootElement.TryGetProperty("base_model_name_or_path", out var baseProp))
                {
                    baseModel = baseProp.GetString() ?? baseModel;
                }
            }
            catch { }
        }

        progress?.Report($"  베이스 모델: {baseModel}");
        progress?.Report($"  LoRA: {loraPath}");

        // 병합 스크립트 생성
        var mergeScript = Path.Combine(Path.GetTempPath(), "merge_lora_temp.py");
        var scriptContent = $@"
import torch
from transformers import AutoModelForCausalLM, AutoTokenizer
from peft import PeftModel
import os

print('Loading base model...')
tokenizer = AutoTokenizer.from_pretrained(r'{baseModel}', trust_remote_code=True)
model = AutoModelForCausalLM.from_pretrained(
    r'{baseModel}',
    torch_dtype=torch.float16,
    device_map='cpu',
    trust_remote_code=True,
)

print('Loading LoRA adapter...')
model = PeftModel.from_pretrained(model, r'{loraPath.Replace("\\", "\\\\")}')

print('Merging weights...')
model = model.merge_and_unload()

print('Saving merged model...')
os.makedirs(r'{outputPath.Replace("\\", "\\\\")}', exist_ok=True)
model.save_pretrained(r'{outputPath.Replace("\\", "\\\\")}', safe_serialization=True)
tokenizer.save_pretrained(r'{outputPath.Replace("\\", "\\\\")}')

print('Merge complete!')
";
        await File.WriteAllTextAsync(mergeScript, scriptContent);

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{mergeScript}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                progress?.Report($"  {e.Data}");
        };
        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data) && !e.Data.Contains("warnings"))
                progress?.Report($"  [!] {e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        // 임시 스크립트 삭제
        try { File.Delete(mergeScript); } catch { }

        var success = process.ExitCode == 0 && Directory.Exists(outputPath);
        if (success)
            progress?.Report("  ✓ 병합 완료");
        else
            progress?.Report($"  ✗ 병합 실패 (exit: {process.ExitCode})");

        return (success, baseModel);
    }

    /// <summary>
    /// HuggingFace 모델을 GGUF로 변환
    /// </summary>
    private async Task<bool> ConvertToGgufAsync(string pythonExe, string convertScript, string modelPath, string ggufPath, string quantize, IProgress<string>? progress)
    {
        progress?.Report($"  입력: {modelPath}");
        progress?.Report($"  출력: {ggufPath}");

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{convertScript}\" \"{modelPath}\" --outfile \"{ggufPath}\" --outtype {quantize}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(convertScript)
        };

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                progress?.Report($"  {e.Data}");
        };
        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                progress?.Report($"  [!] {e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        var success = process.ExitCode == 0 && File.Exists(ggufPath);
        if (success)
        {
            var fileInfo = new FileInfo(ggufPath);
            progress?.Report($"  ✓ GGUF 생성 완료 ({fileInfo.Length / 1024 / 1024}MB)");
        }
        else
        {
            progress?.Report($"  ✗ GGUF 변환 실패 (exit: {process.ExitCode})");
        }

        return success;
    }

    /// <summary>
    /// Ollama Modelfile 생성 (모델별 템플릿)
    /// </summary>
    private void CreateModelfile(string ggufPath, string modelfilePath, string baseModel)
    {
        // 모델별 템플릿과 stop 토큰
        string template;
        string stopToken;

        if (baseModel.Contains("Llama-3") || baseModel.Contains("llama-3"))
        {
            // Llama 3 포맷
            template = @"<|begin_of_text|><|start_header_id|>system<|end_header_id|}

{{ .System }}<|eot_id|><|start_header_id|>user<|end_header_id|>

{{ .Prompt }}<|eot_id|><|start_header_id|>assistant<|end_header_id|>

{{ .Response }}<|eot_id|>";
            stopToken = "<|eot_id|>";
        }
        else if (baseModel.Contains("gemma"))
        {
            // Gemma 포맷
            template = @"<start_of_turn>user
{{ .System }}

{{ .Prompt }}<end_of_turn>
<start_of_turn>model
{{ .Response }}<end_of_turn>";
            stopToken = "<end_of_turn>";
        }
        else
        {
            // Qwen/ChatML 포맷 (기본)
            template = @"<|im_start|>system
{{ .System }}<|im_end|>
<|im_start|>user
{{ .Prompt }}<|im_end|>
<|im_start|>assistant
{{ .Response }}<|im_end|>";
            stopToken = "<|im_end|>";
        }

        var content = $@"FROM {ggufPath}

TEMPLATE """"""{template}""""""

SYSTEM """"""당신은 '망고'라는 이름의 고양이입니다.
- 모든 문장 끝에 '냥'을 붙입니다
- 행동은 (괄호) 안에 표현합니다
- 1-2문장으로 짧게 답합니다
- 고양이답게 츤데레하거나 애교 있게 반응합니다""""""

PARAMETER temperature 0.7
PARAMETER top_p 0.9
PARAMETER repeat_penalty 1.1
PARAMETER num_ctx 2048
PARAMETER stop {stopToken}
";

        File.WriteAllText(modelfilePath, content);
    }

    /// <summary>
    /// Ollama에 모델 등록
    /// </summary>
    private async Task<bool> RegisterToOllamaAsync(string modelName, string modelfilePath, IProgress<string>? progress)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ollama",
            Arguments = $"create {modelName} -f \"{modelfilePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(modelfilePath)
        };

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    progress?.Report($"  {e.Data}");
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    progress?.Report($"  [!] {e.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                progress?.Report($"  ✓ Ollama 등록 완료: {modelName}");
                return true;
            }
            else
            {
                progress?.Report($"  ✗ Ollama 등록 실패 (exit: {process.ExitCode})");
                return false;
            }
        }
        catch (Exception ex)
        {
            progress?.Report($"  ✗ Ollama 실행 오류: {ex.Message}");
            return false;
        }
    }
}
