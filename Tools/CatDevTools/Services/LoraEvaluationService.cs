using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using CatDevTools.Services.Scoring;

namespace CatDevTools.Services;

/// <summary>
/// LoRA 학습 품질 평가 서비스
/// CatScoreKeywords 기반 품질 검증 및 학습 결과 평가
/// </summary>
public class LoraEvaluationService
{
    #region CatScoreKeywords 기반 품질 규칙

    /// <summary>
    /// TimeBlock별 기대 키워드 (CatScoreKeywords.Routine 기반)
    /// </summary>
    private static string[] GetTimeBlockKeywords(string timeBlock)
    {
        return timeBlock.ToLower() switch
        {
            "night" or "dawn" => CatScoreKeywords.NightDawn.Strong.Concat(CatScoreKeywords.NightDawn.Weak).ToArray(),
            "afternoon" => CatScoreKeywords.Afternoon.Strong.Concat(CatScoreKeywords.Afternoon.Weak).ToArray(),
            "deepnight" => CatScoreKeywords.DeepNight.Strong.Concat(CatScoreKeywords.DeepNight.Weak).ToArray(),
            "morning" => new[] { "기지개", "일어났", "밥", "배고", "아침", "눈 떠" },
            "evening" => new[] { "보고싶었", "기다렸", "왔다", "간식", "저녁", "같이", "옆에" },
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// TimeBlock별 금지 키워드 (모순되는 표현)
    /// </summary>
    private static string[] GetTimeBlockContradictions(string timeBlock)
    {
        return timeBlock.ToLower() switch
        {
            "night" or "dawn" => CatScoreKeywords.NightDawn.Contradiction,
            "afternoon" => CatScoreKeywords.Afternoon.Contradiction,
            "deepnight" => CatScoreKeywords.DeepNight.Contradiction,
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// NeedTop1별 기대 키워드 (CatScoreKeywords.Need 기반)
    /// </summary>
    private static string[] GetNeedKeywords(string needTop1)
    {
        return needTop1.ToLower() switch
        {
            "food" => CatScoreKeywords.NeedFood.Match,
            "play" => CatScoreKeywords.NeedPlay.Match,
            "rest" => CatScoreKeywords.NeedRest.Match,
            "affection" => CatScoreKeywords.NeedAffection.Match,
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// TrustTier별 기대 키워드 (CatScoreKeywords.Trust 기반)
    /// </summary>
    private static string[] GetTrustKeywords(string trustTier)
    {
        return trustTier.ToLower() switch
        {
            "low" => CatScoreKeywords.TrustLow.Match,
            "mid" => CatScoreKeywords.TrustMid.Match,
            "high" => CatScoreKeywords.TrustHigh.Match,
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// TrustTier별 금지 키워드 (모순되는 표현)
    /// </summary>
    private static string[] GetTrustContradictions(string trustTier)
    {
        return trustTier.ToLower() switch
        {
            "low" => CatScoreKeywords.TrustLow.Mismatch,
            "high" => CatScoreKeywords.TrustHigh.Mismatch,
            _ => Array.Empty<string>()
        };
    }

    #endregion

    #region 품질 검증

    /// <summary>
    /// 단일 응답의 품질 검증 (CatScoreKeywords 기반)
    /// </summary>
    public DataQualityResult ValidateResponse(string response, SlimControl control)
    {
        var result = new DataQualityResult();
        var text = response.ToLower();

        // 1. 행동 묘사 검증 (Action 점수에 영향)
        result.HasAction = Regex.IsMatch(response, @"\([가-힣\s]+\)");

        // 행동 키워드 체크 (CatScoreKeywords.Action 기반)
        result.HasActionIgnore = CatScoreKeywords.ActionIgnore.Match.Any(k => text.Contains(k));
        result.HasActionSleepy = CatScoreKeywords.ActionSleepy.Match.Any(k => text.Contains(k));
        result.HasActionActive = CatScoreKeywords.ActionActive.Match.Any(k => text.Contains(k));
        result.HasActionGrooming = CatScoreKeywords.ActionGrooming.Match.Any(k => text.Contains(k));

        // 2. 응답 길이 검증
        result.ResponseLength = response.Length;
        result.LengthInRange = response.Length >= 15 && response.Length <= 100;

        // 3. TrustTier 규칙 검증 (CatScoreKeywords.Trust 기반)
        var trustKeywords = GetTrustKeywords(control.TrustTier);
        var trustContradictions = GetTrustContradictions(control.TrustTier);

        result.TrustRequiredCount = trustKeywords.Count(k => text.Contains(k));
        result.TrustRequiredMet = result.TrustRequiredCount > 0;

        result.TrustForbiddenViolations = trustContradictions
            .Where(f => text.Contains(f))
            .ToList();
        result.TrustForbiddenMet = result.TrustForbiddenViolations.Count == 0;

        // 4. TimeBlock 규칙 검증 (CatScoreKeywords.Routine 기반)
        var timeKeywords = GetTimeBlockKeywords(control.TimeBlock);
        var timeContradictions = GetTimeBlockContradictions(control.TimeBlock);

        result.TimeBlockActionMet = timeKeywords.Any(k => text.Contains(k));
        result.TimeBlockForbiddenViolation = timeContradictions.Any(k => text.Contains(k));

        // 5. Need 규칙 검증 (CatScoreKeywords.Need 기반)
        var needKeywords = GetNeedKeywords(control.NeedTop1);
        if (needKeywords.Length > 0)
        {
            result.NeedKeywordMet = needKeywords.Any(k => text.Contains(k));
        }
        else
        {
            result.NeedKeywordMet = true; // none인 경우 통과
        }

        // 6. 츤데레/독립성 검증 (CatScoreKeywords.Tsundere 기반)
        result.HasTsundere = CatScoreKeywords.Tsundere.Match.Any(k => text.Contains(k));
        result.HasIndependence = CatScoreKeywords.Tsundere.Independence.Any(k => text.Contains(k));

        // 7. 혼잣말/관찰 검증 (CatScoreKeywords.Monologue 기반)
        result.HasMonologue = CatScoreKeywords.Monologue.Match.Any(k => text.Contains(k));
        result.HasObservation = CatScoreKeywords.Observation.Match.Any(k => text.Contains(k));

        // 8. 사람 같은 표현 감점 (CatScoreKeywords.HumanLike 기반)
        result.HumanLikeViolations = CatScoreKeywords.HumanLike.Penalty
            .Where(p => text.Contains(p))
            .ToList();

        // 9. 전체 점수 계산
        result.CalculateScore();

        return result;
    }

    /// <summary>
    /// 데이터셋 전체 품질 평가 (CatLikenessScore 기반)
    /// </summary>
    public DatasetQualityReport EvaluateDataset(List<(string response, SlimControl control)> samples)
    {
        var report = new DatasetQualityReport
        {
            TotalSamples = samples.Count
        };

        foreach (var (response, control) in samples)
        {
            var result = ValidateResponse(response, control);
            report.Results.Add(result);

            // CatLikenessScore 항목별 집계
            // Routine (TimeBlock)
            if (result.TimeBlockActionMet) report.TimeBlockMetCount++;
            if (!result.TimeBlockForbiddenViolation) report.TimeBlockNoViolationCount++;

            // Need
            if (result.NeedKeywordMet) report.NeedMetCount++;

            // Trust
            if (result.TrustRequiredMet) report.TrustRequiredMetCount++;
            if (result.TrustForbiddenMet) report.TrustForbiddenMetCount++;

            // Tsundere
            if (result.HasTsundere) report.TsundereCount++;
            if (result.HasIndependence) report.IndependenceCount++;

            // Monologue/Observation
            if (result.HasMonologue) report.MonologueCount++;
            if (result.HasObservation) report.ObservationCount++;

            // Action
            if (result.HasAction) report.ActionCount++;
            if (result.HasActionIgnore || result.HasActionSleepy || result.HasActionActive || result.HasActionGrooming)
                report.ActionKeywordCount++;

            // 기타
            if (result.LengthInRange) report.LengthOkCount++;
            if (result.HumanLikeViolations.Count > 0) report.HumanLikeViolationCount++;
        }

        report.CalculateMetrics();
        return report;
    }

    #endregion

    #region JSONL 데이터 평가

    /// <summary>
    /// JSONL 학습 데이터 파일 평가
    /// </summary>
    public DatasetQualityReport EvaluateJsonlFile(string jsonlPath)
    {
        var samples = new List<(string response, SlimControl control)>();

        foreach (var line in File.ReadLines(jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var doc = JsonDocument.Parse(line);
                var messages = doc.RootElement.GetProperty("messages");

                string? response = null;
                SlimControl? control = null;

                foreach (var msg in messages.EnumerateArray())
                {
                    var role = msg.GetProperty("role").GetString();
                    var content = msg.GetProperty("content").GetString() ?? "";

                    if (role == "assistant")
                    {
                        response = content;
                    }
                    else if (role == "user" && content.Contains("[CONTROL]"))
                    {
                        // [CONTROL]{...}\n[USER]... 형식에서 control 추출
                        var controlMatch = Regex.Match(content, @"\[CONTROL\](\{[^}]+\})");
                        if (controlMatch.Success)
                        {
                            control = JsonSerializer.Deserialize<SlimControl>(
                                controlMatch.Groups[1].Value,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                    }
                }

                if (response != null && control != null)
                {
                    samples.Add((response, control));
                }
            }
            catch
            {
                // 파싱 실패시 무시
            }
        }

        return EvaluateDataset(samples);
    }

    /// <summary>
    /// TrustTier별 품질 분석
    /// </summary>
    public Dictionary<string, DatasetQualityReport> AnalyzeByTrustTier(string jsonlPath)
    {
        var samplesByTier = new Dictionary<string, List<(string response, SlimControl control)>>
        {
            ["low"] = new(),
            ["mid"] = new(),
            ["high"] = new()
        };

        foreach (var line in File.ReadLines(jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var doc = JsonDocument.Parse(line);
                var messages = doc.RootElement.GetProperty("messages");

                string? response = null;
                SlimControl? control = null;

                foreach (var msg in messages.EnumerateArray())
                {
                    var role = msg.GetProperty("role").GetString();
                    var content = msg.GetProperty("content").GetString() ?? "";

                    if (role == "assistant")
                    {
                        response = content;
                    }
                    else if (role == "user" && content.Contains("[CONTROL]"))
                    {
                        var controlMatch = Regex.Match(content, @"\[CONTROL\](\{[^}]+\})");
                        if (controlMatch.Success)
                        {
                            control = JsonSerializer.Deserialize<SlimControl>(
                                controlMatch.Groups[1].Value,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                    }
                }

                if (response != null && control != null && samplesByTier.ContainsKey(control.TrustTier))
                {
                    samplesByTier[control.TrustTier].Add((response, control));
                }
            }
            catch { }
        }

        var result = new Dictionary<string, DatasetQualityReport>();
        foreach (var (tier, samples) in samplesByTier)
        {
            result[tier] = EvaluateDataset(samples);
        }
        return result;
    }

    /// <summary>
    /// 조건 조합 커버리지 분석
    /// </summary>
    public CoverageAnalysis AnalyzeCoverage(string jsonlPath)
    {
        var coverage = new CoverageAnalysis();
        var seenCombinations = new HashSet<string>();

        foreach (var line in File.ReadLines(jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var doc = JsonDocument.Parse(line);
                var messages = doc.RootElement.GetProperty("messages");

                foreach (var msg in messages.EnumerateArray())
                {
                    var role = msg.GetProperty("role").GetString();
                    var content = msg.GetProperty("content").GetString() ?? "";

                    if (role == "user" && content.Contains("[CONTROL]"))
                    {
                        var controlMatch = Regex.Match(content, @"\[CONTROL\](\{[^}]+\})");
                        if (controlMatch.Success)
                        {
                            var control = JsonSerializer.Deserialize<SlimControl>(
                                controlMatch.Groups[1].Value,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (control != null)
                            {
                                coverage.AgeLevels.Add(control.AgeLevel);
                                coverage.TrustTiers.Add(control.TrustTier);
                                coverage.TimeBlocks.Add(control.TimeBlock);
                                coverage.NeedTop1s.Add(control.NeedTop1);
                                coverage.MoodTags.Add(control.MoodTag);

                                var combo = $"{control.AgeLevel}_{control.TrustTier}_{control.TimeBlock}_{control.NeedTop1}";
                                seenCombinations.Add(combo);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // 전체 가능한 조합 수 계산
        coverage.TotalPossibleCombinations = 3 * 3 * 6 * 5; // AgeLevel * TrustTier * TimeBlock * NeedTop1
        coverage.CoveredCombinations = seenCombinations.Count;
        coverage.CoverageRate = (double)coverage.CoveredCombinations / coverage.TotalPossibleCombinations * 100;

        return coverage;
    }

    #endregion

    #region LoRA 학습 결과 평가

    /// <summary>
    /// 학습 전후 비교 평가
    /// </summary>
    public LoraComparisonResult CompareModels(
        List<(string response, SlimControl control)> baselineResponses,
        List<(string response, SlimControl control)> loraResponses)
    {
        var baselineReport = EvaluateDataset(baselineResponses);
        var loraReport = EvaluateDataset(loraResponses);

        return new LoraComparisonResult
        {
            BaselineReport = baselineReport,
            LoraReport = loraReport,
            ActionRateChange = loraReport.ActionRate - baselineReport.ActionRate,
            TsundereRateChange = loraReport.TsundereRate - baselineReport.TsundereRate,
            TrustComplianceChange = loraReport.TrustComplianceRate - baselineReport.TrustComplianceRate,
            TimeBlockComplianceChange = loraReport.TimeBlockComplianceRate - baselineReport.TimeBlockComplianceRate,
            NeedComplianceChange = loraReport.NeedComplianceRate - baselineReport.NeedComplianceRate,
            OverallScoreChange = loraReport.AverageScore - baselineReport.AverageScore
        };
    }

    #endregion
}

/// <summary>
/// 조건 조합 커버리지 분석 결과
/// </summary>
public class CoverageAnalysis
{
    public HashSet<string> AgeLevels { get; set; } = new();
    public HashSet<string> TrustTiers { get; set; } = new();
    public HashSet<string> TimeBlocks { get; set; } = new();
    public HashSet<string> NeedTop1s { get; set; } = new();
    public HashSet<string> MoodTags { get; set; } = new();

    public int TotalPossibleCombinations { get; set; }
    public int CoveredCombinations { get; set; }
    public double CoverageRate { get; set; }

    public string GetSummary()
    {
        return $"커버리지: {CoveredCombinations}/{TotalPossibleCombinations} ({CoverageRate:F1}%)\n" +
               $"  - AgeLevel: {AgeLevels.Count}/3 ({string.Join(", ", AgeLevels)})\n" +
               $"  - TrustTier: {TrustTiers.Count}/3 ({string.Join(", ", TrustTiers)})\n" +
               $"  - TimeBlock: {TimeBlocks.Count}/6 ({string.Join(", ", TimeBlocks)})\n" +
               $"  - NeedTop1: {NeedTop1s.Count}/5 ({string.Join(", ", NeedTop1s)})\n" +
               $"  - MoodTag: {MoodTags.Count}/8 ({string.Join(", ", MoodTags)})";
    }
}

#region 품질 결과 DTO

/// <summary>
/// CatLikenessScore 기반 품질 결과
/// </summary>
public class DataQualityResult
{
    // === 행동 묘사 (Action: 10점) ===
    public bool HasAction { get; set; }  // 괄호 형식 행동 묘사
    public bool HasActionIgnore { get; set; }  // 훽 돌아섬, 외면 등
    public bool HasActionSleepy { get; set; }  // 하품, 기지개 등
    public bool HasActionActive { get; set; }  // 우다다, 폴짝 등
    public bool HasActionGrooming { get; set; }  // 그루밍, 핥 등

    // === 기본 정보 ===
    public int ResponseLength { get; set; }
    public bool LengthInRange { get; set; }

    // === TrustTier 규칙 (Trust: 20점) ===
    public int TrustRequiredCount { get; set; }
    public bool TrustRequiredMet { get; set; }
    public List<string> TrustForbiddenViolations { get; set; } = new();
    public bool TrustForbiddenMet { get; set; }

    // === TimeBlock 규칙 (Routine: 20점) ===
    public bool TimeBlockActionMet { get; set; }
    public bool TimeBlockForbiddenViolation { get; set; }

    // === Need 규칙 (Need: 25점) ===
    public bool NeedKeywordMet { get; set; }

    // === 츤데레/독립성 (Tsundere: 10점) ===
    public bool HasTsundere { get; set; }
    public bool HasIndependence { get; set; }

    // === 혼잣말/관찰 (Monologue: 5점) ===
    public bool HasMonologue { get; set; }
    public bool HasObservation { get; set; }

    // === 사람 같은 표현 감점 ===
    public List<string> HumanLikeViolations { get; set; } = new();

    // === 종합 점수 (0~100, CatLikenessScore 배점 기준) ===
    public int Score { get; set; }

    /// <summary>
    /// CatLikenessScore와 동일한 배점 기준으로 점수 계산
    /// </summary>
    public void CalculateScore()
    {
        int score = 0;

        // 1. Routine/TimeBlock (20점)
        if (TimeBlockActionMet) score += 16;
        if (!TimeBlockForbiddenViolation) score += 4;

        // 2. Need (25점)
        if (NeedKeywordMet) score += 25;

        // 3. Trust (20점)
        if (TrustRequiredMet) score += 12;
        if (TrustForbiddenMet) score += 8;

        // 4. Tsundere (10점)
        if (HasTsundere) score += 5;
        if (HasIndependence) score += 5;

        // 5. Monologue/Observation (5점)
        if (HasMonologue) score += 2;
        if (HasObservation) score += 3;

        // 6. Action (10점)
        if (HasAction) score += 4;  // 괄호 형식
        if (HasActionIgnore || HasActionSleepy || HasActionActive || HasActionGrooming)
            score += 6;

        // 7. 길이 보너스 (5점)
        if (LengthInRange) score += 5;

        // 8. HumanLike 감점 (-5점 per violation, 최대 -15점)
        int humanPenalty = Math.Min(HumanLikeViolations.Count * 5, 15);
        score -= humanPenalty;

        Score = Math.Clamp(score, 0, 100);
    }
}

/// <summary>
/// CatLikenessScore 기반 데이터셋 품질 리포트
/// </summary>
public class DatasetQualityReport
{
    public int TotalSamples { get; set; }
    public List<DataQualityResult> Results { get; set; } = new();

    // === CatLikenessScore 항목별 집계 ===
    // Routine (TimeBlock)
    public int TimeBlockMetCount { get; set; }
    public int TimeBlockNoViolationCount { get; set; }

    // Need
    public int NeedMetCount { get; set; }

    // Trust
    public int TrustRequiredMetCount { get; set; }
    public int TrustForbiddenMetCount { get; set; }

    // Tsundere
    public int TsundereCount { get; set; }
    public int IndependenceCount { get; set; }

    // Monologue/Observation
    public int MonologueCount { get; set; }
    public int ObservationCount { get; set; }

    // Action
    public int ActionCount { get; set; }
    public int ActionKeywordCount { get; set; }  // 행동 키워드 (훽, 하품, 우다다 등)

    // 기타
    public int LengthOkCount { get; set; }
    public int HumanLikeViolationCount { get; set; }

    // === 비율 (CatLikenessScore 가중치 적용) ===
    public double RoutineRate { get; set; }      // TimeBlock 준수율
    public double NeedRate { get; set; }         // Need 반영율
    public double TrustRate { get; set; }        // Trust 준수율
    public double TsundereRate { get; set; }     // 츤데레/독립성 비율
    public double MonologueRate { get; set; }    // 혼잣말/관찰 비율
    public double ActionRate { get; set; }       // 행동 묘사 비율
    public double AverageScore { get; set; }     // 종합 점수

    // 호환성용 (기존 UI)
    public double TrustComplianceRate => TrustRate;
    public double TimeBlockComplianceRate => RoutineRate;
    public double NeedComplianceRate => NeedRate;

    public void CalculateMetrics()
    {
        if (TotalSamples == 0) return;

        // CatLikenessScore 항목별 비율 계산
        RoutineRate = (double)(TimeBlockMetCount + TimeBlockNoViolationCount) / (TotalSamples * 2) * 100;
        NeedRate = (double)NeedMetCount / TotalSamples * 100;
        TrustRate = (double)(TrustRequiredMetCount + TrustForbiddenMetCount) / (TotalSamples * 2) * 100;
        TsundereRate = (double)(TsundereCount + IndependenceCount) / (TotalSamples * 2) * 100;
        MonologueRate = (double)(MonologueCount + ObservationCount) / (TotalSamples * 2) * 100;
        ActionRate = (double)(ActionCount + ActionKeywordCount) / (TotalSamples * 2) * 100;

        AverageScore = Results.Count > 0 ? Results.Average(r => r.Score) : 0;
    }
}

/// <summary>
/// LoRA 학습 전후 비교 결과 (CatLikenessScore 기반)
/// </summary>
public class LoraComparisonResult
{
    public DatasetQualityReport BaselineReport { get; set; } = new();
    public DatasetQualityReport LoraReport { get; set; } = new();

    // 변화량 (양수 = 개선)
    public double ActionRateChange { get; set; }
    public double TsundereRateChange { get; set; }
    public double TrustComplianceChange { get; set; }
    public double TimeBlockComplianceChange { get; set; }
    public double NeedComplianceChange { get; set; }
    public double OverallScoreChange { get; set; }

    public string GetSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== LoRA 학습 전후 비교 (CatLikenessScore 기준) ===");
        sb.AppendLine();
        sb.AppendLine($"Routine (TimeBlock): {BaselineReport.RoutineRate:F1}% → {LoraReport.RoutineRate:F1}% ({TimeBlockComplianceChange:+0.0;-0.0}%)");
        sb.AppendLine($"Need (욕구 반영): {BaselineReport.NeedRate:F1}% → {LoraReport.NeedRate:F1}% ({NeedComplianceChange:+0.0;-0.0}%)");
        sb.AppendLine($"Trust (신뢰도): {BaselineReport.TrustRate:F1}% → {LoraReport.TrustRate:F1}% ({TrustComplianceChange:+0.0;-0.0}%)");
        sb.AppendLine($"Tsundere (츤데레): {BaselineReport.TsundereRate:F1}% → {LoraReport.TsundereRate:F1}% ({TsundereRateChange:+0.0;-0.0}%)");
        sb.AppendLine($"Action (행동 묘사): {BaselineReport.ActionRate:F1}% → {LoraReport.ActionRate:F1}% ({ActionRateChange:+0.0;-0.0}%)");
        sb.AppendLine();
        sb.AppendLine($"종합 품질 점수: {BaselineReport.AverageScore:F1} → {LoraReport.AverageScore:F1} ({OverallScoreChange:+0.0;-0.0})");

        return sb.ToString();
    }
}

#endregion
