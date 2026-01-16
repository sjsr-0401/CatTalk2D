using UnityEngine;
using System.Collections.Generic;
using CatTalk2D.Managers;

namespace CatTalk2D.API
{
    /// <summary>
    /// 간단한 감정 분석기 (규칙 기반)
    /// MVP: 키워드 기반 긍정/부정/중립 판별
    /// </summary>
    public static class SentimentAnalyzer
    {
        #region 키워드 사전
        private static readonly HashSet<string> _positiveKeywords = new HashSet<string>
        {
            // 칭찬
            "귀여워", "귀엽다", "예뻐", "예쁘다", "이뻐", "이쁘다",
            "착해", "착하다", "잘했어", "잘한다", "최고", "대단해",
            "사랑해", "좋아해", "보고싶어", "보고싶었어",

            // 긍정 감정
            "좋아", "좋다", "행복해", "기뻐", "고마워", "감사해",
            "멋져", "멋지다", "훌륭해", "완벽해",

            // 애정 표현
            "애기", "아가", "우리", "내새끼", "내꺼",
            "뽀뽀", "쪽", "안아줄게", "쓰담쓰담",

            // 긍정 인사
            "반가워", "보고싶었어", "잘자", "굿나잇"
        };

        private static readonly HashSet<string> _negativeKeywords = new HashSet<string>
        {
            // 부정 감정
            "싫어", "싫다", "짜증나", "짜증", "화나", "화난다",
            "나빠", "나쁘다", "못생겼", "못났어",

            // 욕설/비난
            "바보", "멍청이", "멍청해", "미워", "밉다",
            "꺼져", "저리가", "시끄러", "닥쳐",

            // 부정 명령
            "하지마", "하지 마", "그만해", "그만 해",
            "싫어", "안해", "안 해",

            // 위협
            "때릴거야", "때려", "맞을래", "혼날래"
        };
        #endregion

        #region 분석 메서드
        /// <summary>
        /// 텍스트의 감정 분석
        /// </summary>
        public static SentimentType Analyze(string text)
        {
            if (string.IsNullOrEmpty(text))
                return SentimentType.Neutral;

            string lowerText = text.ToLower().Replace(" ", "");

            int positiveScore = 0;
            int negativeScore = 0;

            // 긍정 키워드 체크
            foreach (var keyword in _positiveKeywords)
            {
                if (lowerText.Contains(keyword.Replace(" ", "")))
                {
                    positiveScore++;
                }
            }

            // 부정 키워드 체크
            foreach (var keyword in _negativeKeywords)
            {
                if (lowerText.Contains(keyword.Replace(" ", "")))
                {
                    negativeScore++;
                }
            }

            // 이모티콘 체크
            positiveScore += CountPositiveEmojis(text);
            negativeScore += CountNegativeEmojis(text);

            Debug.Log($"[SentimentAnalyzer] 분석: '{text}' → 긍정:{positiveScore}, 부정:{negativeScore}");

            // 결과 판정
            if (positiveScore > negativeScore)
                return SentimentType.Positive;
            else if (negativeScore > positiveScore)
                return SentimentType.Negative;
            else
                return SentimentType.Neutral;
        }

        /// <summary>
        /// 긍정 이모지 카운트
        /// </summary>
        private static int CountPositiveEmojis(string text)
        {
            int count = 0;
            string[] positiveEmojis = { "😊", "😄", "😍", "🥰", "❤", "💕", "👍", "😺", "😻", "💖" };
            foreach (var emoji in positiveEmojis)
            {
                if (text.Contains(emoji)) count++;
            }
            return count;
        }

        /// <summary>
        /// 부정 이모지 카운트
        /// </summary>
        private static int CountNegativeEmojis(string text)
        {
            int count = 0;
            string[] negativeEmojis = { "😡", "😠", "💢", "👎", "😤", "🤬", "😾", "💔" };
            foreach (var emoji in negativeEmojis)
            {
                if (text.Contains(emoji)) count++;
            }
            return count;
        }
        #endregion

        #region 확장 메서드
        /// <summary>
        /// 분석 결과를 한국어로 반환
        /// </summary>
        public static string GetSentimentText(SentimentType sentiment)
        {
            return sentiment switch
            {
                SentimentType.Positive => "긍정적",
                SentimentType.Negative => "부정적",
                _ => "중립"
            };
        }
        #endregion
    }
}
