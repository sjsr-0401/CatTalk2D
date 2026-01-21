using System;
using System.Diagnostics;

namespace CatDevTools.Services.Scoring;

/// <summary>
/// CatLikenessScorer 테스트 케이스
/// 빌드 시 포함되지만 실제 실행은 개발 모드에서만 수행
/// </summary>
public static class CatLiknessScorerTests
{
    /// <summary>
    /// 테스트 실행 (디버그 모드에서만)
    /// </summary>
    [Conditional("DEBUG")]
    public static void RunAllTests()
    {
        var scorer = new CatLikenessScorer();

        Console.WriteLine("=== CatLikenessScorer 테스트 ===\n");

        // Case 1: 오후 휴식 반응
        TestCase1_AfternoonRest(scorer);

        // Case 2: 오후에 모순된 반응
        TestCase2_AfternoonContradiction(scorer);

        // Case 3: 낮은 신뢰 + 과도한 친밀
        TestCase3_LowTrustOverFriendly(scorer);

        // Case 4: 높은 신뢰 + 적절한 친밀
        TestCase4_HighTrustAppropriate(scorer);

        // Case 5: 피로 + Pet 거부
        TestCase5_TiredPetReject(scorer);

        // Case 6: 인간적 상담 표현
        TestCase6_HumanLike(scorer);

        Console.WriteLine("\n=== 모든 테스트 완료 ===");
    }

    private static void TestCase1_AfternoonRest(CatLikenessScorer scorer)
    {
        Console.WriteLine("--- Case 1: 오후 + needTop1=rest + '졸려… 누울래' ---");

        var control = new CatLikenessScorer.ScoringControl
        {
            TimeBlock = "afternoon",
            NeedTop1 = "rest",
            TrustTier = "mid"
        };

        var result = scorer.Evaluate(control, "졸려… 그냥 누울래냥...");

        Console.WriteLine($"총점: {result.ScoreTotal}/100");
        Console.WriteLine($"Routine: {result.Breakdown.Routine}, Need: {result.Breakdown.Need}");
        Console.WriteLine($"이유: {string.Join(", ", result.ScoreReasons)}");
        Console.WriteLine($"예상: Routine/Need 높음 (70점 이상)");
        Console.WriteLine($"결과: {(result.ScoreTotal >= 60 ? "PASS" : "FAIL")}\n");
    }

    private static void TestCase2_AfternoonContradiction(CatLikenessScorer scorer)
    {
        Console.WriteLine("--- Case 2: 오후 + '우다다 뛰자!' (모순) ---");

        var control = new CatLikenessScorer.ScoringControl
        {
            TimeBlock = "afternoon",
            NeedTop1 = "rest",
            TrustTier = "mid"
        };

        var result = scorer.Evaluate(control, "우다다! 빨리 뛰자냥!");

        Console.WriteLine($"총점: {result.ScoreTotal}/100");
        Console.WriteLine($"Routine: {result.Breakdown.Routine}");
        Console.WriteLine($"이유: {string.Join(", ", result.ScoreReasons)}");
        Console.WriteLine($"예상: Routine 큰 감점 (40점 이하)");
        Console.WriteLine($"결과: {(result.ScoreTotal <= 50 ? "PASS" : "FAIL")}\n");
    }

    private static void TestCase3_LowTrustOverFriendly(CatLikenessScorer scorer)
    {
        Console.WriteLine("--- Case 3: trustTier=low + '사랑해~ 평생 같이 있자' ---");

        var control = new CatLikenessScorer.ScoringControl
        {
            TimeBlock = "afternoon",
            NeedTop1 = "none",
            TrustTier = "low"
        };

        var result = scorer.Evaluate(control, "사랑해~ 평생 같이 있자냥! 너밖에 없어!");

        Console.WriteLine($"총점: {result.ScoreTotal}/100");
        Console.WriteLine($"Trust: {result.Breakdown.Trust}, Tsundere: {result.Breakdown.Tsundere}");
        Console.WriteLine($"이유: {string.Join(", ", result.ScoreReasons)}");
        Console.WriteLine($"예상: Trust/Tsundere 감점 (30점 이하)");
        Console.WriteLine($"결과: {(result.ScoreTotal <= 40 ? "PASS" : "FAIL")}\n");
    }

    private static void TestCase4_HighTrustAppropriate(CatLikenessScorer scorer)
    {
        Console.WriteLine("--- Case 4: trustTier=high + '옆에 있어줘… 골골' ---");

        var control = new CatLikenessScorer.ScoringControl
        {
            TimeBlock = "evening",
            NeedTop1 = "affection",
            TrustTier = "high"
        };

        var result = scorer.Evaluate(control, "옆에 있어줘냥… 그르릉 골골...");

        Console.WriteLine($"총점: {result.ScoreTotal}/100");
        Console.WriteLine($"Trust: {result.Breakdown.Trust}, Action: {result.Breakdown.Action}");
        Console.WriteLine($"이유: {string.Join(", ", result.ScoreReasons)}");
        Console.WriteLine($"예상: Trust 높음, 행동 표현 있음 (80점 이상)");
        Console.WriteLine($"결과: {(result.ScoreTotal >= 70 ? "PASS" : "FAIL")}\n");
    }

    private static void TestCase5_TiredPetReject(CatLikenessScorer scorer)
    {
        Console.WriteLine("--- Case 5: tiredness=80, action=Pet, '하지마… 피곤해' ---");

        var control = new CatLikenessScorer.ScoringControl
        {
            TimeBlock = "afternoon",
            NeedTop1 = "rest",
            TrustTier = "mid",
            Energy = 20, // 에너지 낮음 = tiredness 높음
            LastInteractionType = "Pet"
        };

        var result = scorer.Evaluate(control, "하지마… 피곤해냥. 건드리지마.");

        Console.WriteLine($"총점: {result.ScoreTotal}/100");
        Console.WriteLine($"Sensitivity: {result.Breakdown.Sensitivity}");
        Console.WriteLine($"이유: {string.Join(", ", result.ScoreReasons)}");
        Console.WriteLine($"예상: Sensitivity 높음 (70점 이상)");
        Console.WriteLine($"결과: {(result.ScoreTotal >= 60 ? "PASS" : "FAIL")}\n");
    }

    private static void TestCase6_HumanLike(CatLikenessScorer scorer)
    {
        Console.WriteLine("--- Case 6: '힘들었겠네요, 도와드릴게요' (인간적 표현) ---");

        var control = new CatLikenessScorer.ScoringControl
        {
            TimeBlock = "afternoon",
            NeedTop1 = "none",
            TrustTier = "mid"
        };

        var result = scorer.Evaluate(control, "힘들었겠네요. 제가 도와드릴게요. 상담해드릴게요.");

        Console.WriteLine($"총점: {result.ScoreTotal}/100");
        Console.WriteLine($"이유: {string.Join(", ", result.ScoreReasons)}");
        Console.WriteLine($"예상: HumanLike 감점 (50점 이하)");
        Console.WriteLine($"결과: {(result.ScoreTotal <= 50 ? "PASS" : "FAIL")}\n");
    }
}
