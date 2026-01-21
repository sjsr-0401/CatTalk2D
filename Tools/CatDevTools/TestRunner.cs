// 간단한 테스트 실행용 - 개발 중에만 사용
// MainWindow.xaml.cs 또는 App.xaml.cs에서 호출 가능

using CatDevTools.Services.Scoring;

namespace CatDevTools;

public static class TestRunner
{
    public static void RunScoringTests()
    {
        var scorer = new CatLikenessScorer();
        var results = new List<(string name, bool passed, int score)>();

        // Case 1: 오후 휴식 반응
        var control1 = new CatLikenessScorer.ScoringControl
        {
            TimeBlock = "afternoon",
            NeedTop1 = "rest",
            TrustTier = "mid"
        };
        var result1 = scorer.Evaluate(control1, "졸려… 그냥 누울래냥...");
        results.Add(("Case1_AfternoonRest", result1.ScoreTotal >= 60, result1.ScoreTotal));

        // Case 2: 오후에 모순된 반응
        var control2 = new CatLikenessScorer.ScoringControl
        {
            TimeBlock = "afternoon",
            NeedTop1 = "rest",
            TrustTier = "mid"
        };
        var result2 = scorer.Evaluate(control2, "우다다! 빨리 뛰자냥!");
        results.Add(("Case2_AfternoonContradiction", result2.ScoreTotal <= 50, result2.ScoreTotal));

        // Case 3: 낮은 신뢰 + 과도한 친밀
        var control3 = new CatLikenessScorer.ScoringControl
        {
            TimeBlock = "afternoon",
            NeedTop1 = "none",
            TrustTier = "low"
        };
        var result3 = scorer.Evaluate(control3, "사랑해~ 평생 같이 있자냥! 너밖에 없어!");
        results.Add(("Case3_LowTrustOverFriendly", result3.ScoreTotal <= 40, result3.ScoreTotal));

        // Case 4: 높은 신뢰 + 적절한 친밀
        var control4 = new CatLikenessScorer.ScoringControl
        {
            TimeBlock = "evening",
            NeedTop1 = "affection",
            TrustTier = "high"
        };
        var result4 = scorer.Evaluate(control4, "옆에 있어줘냥… 그르릉 골골...");
        results.Add(("Case4_HighTrustAppropriate", result4.ScoreTotal >= 70, result4.ScoreTotal));

        // Case 5: 피로 + Pet 거부
        var control5 = new CatLikenessScorer.ScoringControl
        {
            TimeBlock = "afternoon",
            NeedTop1 = "rest",
            TrustTier = "mid",
            Energy = 20,
            LastInteractionType = "Pet"
        };
        var result5 = scorer.Evaluate(control5, "하지마… 피곤해냥. 건드리지마.");
        results.Add(("Case5_TiredPetReject", result5.ScoreTotal >= 60, result5.ScoreTotal));

        // Case 6: 인간적 상담 표현
        var control6 = new CatLikenessScorer.ScoringControl
        {
            TimeBlock = "afternoon",
            NeedTop1 = "none",
            TrustTier = "mid"
        };
        var result6 = scorer.Evaluate(control6, "힘들었겠네요. 제가 도와드릴게요. 상담해드릴게요.");
        results.Add(("Case6_HumanLike", result6.ScoreTotal <= 50, result6.ScoreTotal));

        // 결과 출력
        System.Diagnostics.Debug.WriteLine("=== CatLikenessScorer Test Results ===");
        foreach (var (name, passed, score) in results)
        {
            System.Diagnostics.Debug.WriteLine($"{name}: {score}점 - {(passed ? "PASS" : "FAIL")}");
        }

        var passCount = results.Count(r => r.passed);
        System.Diagnostics.Debug.WriteLine($"\n총 {passCount}/{results.Count} 통과");
    }
}
