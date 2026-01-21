using UnityEngine;
using System.Text.RegularExpressions;

namespace CatTalk2D.AI
{
    /// <summary>
    /// LLM 응답 후처리기
    /// - 영어 필터링
    /// - 길이 제한
    /// - 냥/야옹 추가
    /// </summary>
    public static class ResponseProcessor
    {
        private static readonly string[] FallbackResponses = new[]
        {
            "냥?",
            "뭐냥~",
            "응냥!",
            "헤헤냥",
            "야옹~",
            "알았다냥",
            "좋아냥!",
            "싫어냥...",
            "몰라냥",
            "그래냥~"
        };

        /// <summary>
        /// 응답 후처리 결과
        /// </summary>
        public class ProcessResult
        {
            public string Text { get; set; }
            public bool IsValid { get; set; }
            public bool ContainedEnglish { get; set; }
            public bool WasTooLong { get; set; }
            public bool UsedFallback { get; set; }
        }

        /// <summary>
        /// 응답 후처리
        /// </summary>
        public static ProcessResult Process(string rawResponse, int maxLength = 50)
        {
            var result = new ProcessResult
            {
                IsValid = true,
                ContainedEnglish = false,
                WasTooLong = false,
                UsedFallback = false
            };

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                result.Text = GetFallbackResponse();
                result.IsValid = false;
                result.UsedFallback = true;
                return result;
            }

            string text = rawResponse.Trim();

            // 1. 첫 줄만 사용
            int newlineIndex = text.IndexOf('\n');
            if (newlineIndex > 0)
            {
                text = text.Substring(0, newlineIndex).Trim();
            }

            // 2. 첫 문장만 (마침표, 느낌표, 물음표 기준)
            text = ExtractFirstSentence(text);

            // 3. 영어 포함 체크
            if (ContainsEnglish(text))
            {
                result.ContainedEnglish = true;
                Debug.Log($"[ResponseProcessor] 영어 감지됨: {text}");
            }

            // 4. 길이 제한
            if (text.Length > maxLength)
            {
                result.WasTooLong = true;
                text = text.Substring(0, maxLength);
                // 단어 중간에서 자르지 않도록
                int lastSpace = text.LastIndexOf(' ');
                if (lastSpace > maxLength / 2)
                {
                    text = text.Substring(0, lastSpace);
                }
            }

            // 5. 냥/야옹 추가
            text = EnsureCatSuffix(text);

            // 6. 최종 검증
            if (result.ContainedEnglish)
            {
                // 영어가 포함되어 있으면 유효하지 않음
                result.IsValid = false;
            }

            result.Text = text;
            return result;
        }

        /// <summary>
        /// 첫 문장 추출
        /// </summary>
        private static string ExtractFirstSentence(string text)
        {
            char[] endChars = { '.', '!', '?', '~' };

            foreach (char endChar in endChars)
            {
                int idx = text.IndexOf(endChar);
                if (idx > 0 && idx < text.Length - 1)
                {
                    // 냥! 같은 경우는 자르지 않음
                    if (idx < 3) continue;
                    return text.Substring(0, idx + 1);
                }
            }

            return text;
        }

        /// <summary>
        /// 영어 포함 여부 확인
        /// </summary>
        public static bool ContainsEnglish(string text)
        {
            foreach (char c in text)
            {
                // 한글 (가-힣, ㄱ-ㅎ, ㅏ-ㅣ)
                if (c >= '가' && c <= '힣') continue;
                if (c >= 'ㄱ' && c <= 'ㅎ') continue;
                if (c >= 'ㅏ' && c <= 'ㅣ') continue;

                // 숫자
                if (c >= '0' && c <= '9') continue;

                // 기본 문장부호/공백
                if (" .,!?~-…·:;'\"()[]<>".Contains(c)) continue;

                // 영어 알파벳 감지
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    return true;
                }

                // 기타 특수문자는 허용 (이모지 등)
            }

            return false;
        }

        /// <summary>
        /// 냥/야옹 접미사 확인 및 추가
        /// </summary>
        private static string EnsureCatSuffix(string text)
        {
            if (text.Contains("냥") || text.Contains("야옹"))
            {
                return text;
            }

            // 끝 문장부호 제거 후 냥 추가
            text = text.TrimEnd('.', '!', '?', '~', ' ');

            // 랜덤으로 냥 또는 야옹
            string suffix = Random.value > 0.7f ? "야옹" : "냥";
            return text + suffix;
        }

        /// <summary>
        /// 대체 응답 반환
        /// </summary>
        public static string GetFallbackResponse()
        {
            return FallbackResponses[Random.Range(0, FallbackResponses.Length)];
        }

        /// <summary>
        /// 기분에 맞는 대체 응답 반환
        /// </summary>
        public static string GetMoodFallbackResponse(string moodTag)
        {
            return moodTag switch
            {
                "very_hungry" or "hungry" => new[] { "배고파냥...", "밥 줘냥...", "꼬르륵냥..." }[Random.Range(0, 3)],
                "stressed" => new[] { "으으냥...", "짜증나냥...", "건드리지 마냥..." }[Random.Range(0, 3)],
                "bored" => new[] { "심심해냥...", "놀아줘냥...", "지루하다냥..." }[Random.Range(0, 3)],
                "tired" => new[] { "졸려냥...", "쿨쿨냥...", "피곤해냥..." }[Random.Range(0, 3)],
                "happy" => new[] { "기분 좋다냥!", "헤헤냥~", "신난다냥!" }[Random.Range(0, 3)],
                _ => GetFallbackResponse()
            };
        }
    }
}
