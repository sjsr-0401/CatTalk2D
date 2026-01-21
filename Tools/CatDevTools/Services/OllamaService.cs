using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CatDevTools.Services;

/// <summary>
/// Ollama API 클라이언트 서비스
/// </summary>
public class OllamaService
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = "http://localhost:11434";

    public OllamaService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // 모델 다운로드용
    }

    public string BaseUrl
    {
        get => _baseUrl;
        set => _baseUrl = value.TrimEnd('/');
    }

    /// <summary>
    /// 설치된 모델 목록 가져오기
    /// </summary>
    public async Task<List<OllamaModel>> GetModelsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OllamaTagsResponse>(json);

            return result?.Models ?? new List<OllamaModel>();
        }
        catch (Exception ex)
        {
            OllamaLogService.Instance.Log($"[Tags] 실패: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"모델 목록 가져오기 실패: {ex.Message}");
            return new List<OllamaModel>();
        }
    }

    /// <summary>
    /// 모델 다운로드 (Pull)
    /// </summary>
    public async Task<bool> PullModelAsync(string modelName, IProgress<OllamaPullProgress>? progress = null)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            OllamaLogService.Instance.Log($"[Pull] 시작: {modelName}");

            var request = new { name = modelName };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/pull", content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                OllamaLogService.Instance.Log($"[Pull] 실패: {modelName} {(int)response.StatusCode} {response.ReasonPhrase} {OllamaLogHelpers.Truncate(body, 200)}");
                return false;
            }

            // 스트리밍 응답 처리
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    var pullResponse = JsonSerializer.Deserialize<OllamaPullResponse>(line);
                    if (pullResponse != null)
                    {
                        progress?.Report(new OllamaPullProgress
                        {
                            Status = pullResponse.Status ?? "",
                            Completed = pullResponse.Completed,
                            Total = pullResponse.Total,
                            Digest = pullResponse.Digest
                        });
                    }
                }
                catch { /* JSON 파싱 실패 무시 */ }
            }

            OllamaLogService.Instance.Log($"[Pull] 완료: {modelName} ({sw.ElapsedMilliseconds}ms)");
            return true;
        }
        catch (Exception ex)
        {
            OllamaLogService.Instance.Log($"[Pull] 예외: {modelName} {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"모델 다운로드 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 모델 삭제
    /// </summary>
    public async Task<bool> DeleteModelAsync(string modelName)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/api/delete")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { name = modelName }),
                    Encoding.UTF8,
                    "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            OllamaLogService.Instance.Log($"[Delete] 예외: {modelName} {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"모델 삭제 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 텍스트 생성 (테스트용)
    /// </summary>
    public async Task<string?> GenerateAsync(string model, string prompt, float temperature = 0.7f)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            OllamaLogService.Instance.Log($"[Generate] -> {model} promptLen={prompt?.Length ?? 0}");

            var request = new
            {
                model = model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = temperature
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                OllamaLogService.Instance.Log($"[Generate] 실패: {model} {(int)response.StatusCode} {response.ReasonPhrase} {OllamaLogHelpers.Truncate(body, 200)}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(json);
            LogTokenCountsIfPresent("[Generate]", model, result?.PromptEvalCount, result?.EvalCount);

            OllamaLogService.Instance.Log($"[Generate] 완료: {model} ({sw.ElapsedMilliseconds}ms, resLen={result?.Response?.Length ?? 0})");
            return result?.Response;
        }
        catch (Exception ex)
        {
            OllamaLogService.Instance.Log($"[Generate] 예외: {model} {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"텍스트 생성 실패: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Chat API를 사용한 대화 생성 (system + user 메시지)
    /// </summary>
    public async Task<string?> ChatAsync(string model, string systemMessage, string userMessage, float temperature = 0.7f, int timeoutSeconds = 60)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            OllamaLogService.Instance.Log($"[Chat] -> {model} sysLen={systemMessage?.Length ?? 0} userLen={userMessage?.Length ?? 0}");

            var request = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userMessage }
                },
                stream = false,
                options = new
                {
                    temperature = temperature,
                    num_predict = 150  // 응답 길이 제한 (속도 향상)
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/chat", content, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                OllamaLogService.Instance.Log($"[Chat] 실패: {model} {(int)response.StatusCode} {response.ReasonPhrase} {OllamaLogHelpers.Truncate(body, 200)}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OllamaChatResponse>(json);
            LogTokenCountsIfPresent("[Chat]", model, result?.PromptEvalCount, result?.EvalCount);

            OllamaLogService.Instance.Log($"[Chat] 완료: {model} ({sw.ElapsedMilliseconds}ms, resLen={result?.Message?.Content?.Length ?? 0})");
            return result?.Message?.Content;
        }
        catch (TaskCanceledException)
        {
            OllamaLogService.Instance.Log($"[Chat] 타임아웃: {model} ({timeoutSeconds}s)");
            System.Diagnostics.Debug.WriteLine($"Chat 생성 타임아웃: {model}");
            return null;
        }
        catch (Exception ex)
        {
            OllamaLogService.Instance.Log($"[Chat] 예외: {model} {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Chat 생성 실패: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Ollama 서버 상태 확인
    /// </summary>
    private static void LogTokenCountsIfPresent(string scope, string model, int? promptEvalCount, int? evalCount)
    {
        if (!promptEvalCount.HasValue && !evalCount.HasValue) return;

        var sb = new StringBuilder();
        if (promptEvalCount.HasValue)
        {
            sb.Append($"promptTok={promptEvalCount.Value}");
        }

        if (evalCount.HasValue)
        {
            if (sb.Length > 0)
            {
                sb.Append(", ");
            }
            sb.Append($"evalTok={evalCount.Value}");
        }

        OllamaLogService.Instance.Log($"{scope} tokens: {model} ({sb})");
    }

    public async Task<bool> IsServerRunningAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            OllamaLogService.Instance.Log("[Tags] 서버 연결 실패");
            return false;
        }
    }
}

file static class OllamaLogHelpers
{
    public static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= max ? value : value.Substring(0, max) + "...";
    }
}

#region JSON 응답 모델

public class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModel>? Models { get; set; }
}

public class OllamaModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("modified_at")]
    public string? ModifiedAt { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("digest")]
    public string? Digest { get; set; }

    [JsonPropertyName("details")]
    public OllamaModelDetails? Details { get; set; }

    // 표시용
    public string SizeDisplay => Size switch
    {
        < 1024 => $"{Size} B",
        < 1024 * 1024 => $"{Size / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{Size / (1024.0 * 1024):F1} MB",
        _ => $"{Size / (1024.0 * 1024 * 1024):F2} GB"
    };
}

public class OllamaModelDetails
{
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("parameter_size")]
    public string? ParameterSize { get; set; }

    [JsonPropertyName("quantization_level")]
    public string? QuantizationLevel { get; set; }
}

public class OllamaPullResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("digest")]
    public string? Digest { get; set; }

    [JsonPropertyName("total")]
    public long Total { get; set; }

    [JsonPropertyName("completed")]
    public long Completed { get; set; }
}

public class OllamaPullProgress
{
    public string Status { get; set; } = string.Empty;
    public long Total { get; set; }
    public long Completed { get; set; }
    public string? Digest { get; set; }

    public double Percentage => Total > 0 ? (double)Completed / Total * 100 : 0;
}

public class OllamaGenerateResponse
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("response")]
    public string? Response { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int? PromptEvalCount { get; set; }

    [JsonPropertyName("eval_count")]
    public int? EvalCount { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

public class OllamaChatResponse
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("message")]
    public OllamaChatMessage? Message { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int? PromptEvalCount { get; set; }

    [JsonPropertyName("eval_count")]
    public int? EvalCount { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

public class OllamaChatMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

#endregion
