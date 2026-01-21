using System.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;

namespace CatDevTools;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // LiveCharts 한글 폰트 설정
        LiveCharts.Configure(config =>
            config.HasGlobalSKTypeface(SKFontManager.Default.MatchFamily("Malgun Gothic")
                ?? SKFontManager.Default.MatchFamily("맑은 고딕")
                ?? SKTypeface.Default));

#if DEBUG
        // 디버그 모드에서 CatLikenessScorer 테스트 실행
        TestRunner.RunScoringTests();
#endif
    }
}
