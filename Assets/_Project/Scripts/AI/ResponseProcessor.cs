using UnityEngine;
using System.Text.RegularExpressions;

namespace CatTalk2D.AI
{
    /// <summary>
    /// LLM 응답 후처리기
    /// - [ACT]/[TEXT] 포맷 파싱
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

        private static readonly string[] FallbackActions = new[]
        {
            "가만히 앉아있음",
            "꼬리를 살랑살랑 흔듦",
            "귀를 쫑긋 세움",
            "고개를 갸웃거림",
            "하품을 함",
            "앞발로 세수함",
            "눈을 깜빡임",
            "자리에서 몸을 돌림"
        };

        /// <summary>
        /// 응답 후처리 결과 (확장)
        /// </summary>
        public class ProcessResult
        {
            public string Text { get; set; }
            public string Action { get; set; } = "";
            public string RawResponse { get; set; } = "";
            public bool IsValid { get; set; }
            public bool ContainedEnglish { get; set; }
            public bool WasTooLong { get; set; }
            public bool UsedFallback { get; set; }
            public bool HasActTextFormat { get; set; }
        }

        /// <summary>
        /// 응답 후처리 (기존 호환)
        /// </summary>
        public static ProcessResult Process(string rawResponse, int maxLength = 50)
        {
            return ProcessWithAction(rawResponse, maxLength);
        }

        /// <summary>
        /// [ACT]/[TEXT] 포맷 응답 후처리
        /// </summary>
        public static ProcessResult ProcessWithAction(string rawResponse, int maxLength = 50)
        {
            var result = new ProcessResult
            {
                IsValid = true,
                ContainedEnglish = false,
                WasTooLong = false,
                UsedFallback = false,
                HasActTextFormat = false,
                RawResponse = rawResponse ?? ""
            };

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                result.Text = GetFallbackResponse();
                result.Action = GetFallbackAction();
                result.IsValid = false;
                result.UsedFallback = true;
                return result;
            }

            // 1. [ACT]/[TEXT] 포맷 파싱 시도
            var (action, text) = ParseActTextFormat(rawResponse);
            result.HasActTextFormat = !string.IsNullOrEmpty(action) && !string.IsNullOrEmpty(text);

            if (result.HasActTextFormat)
            {
                result.Action = action;
            }
            else
            {
                // 포맷이 없으면 기존 방식으로 처리
                text = rawResponse.Trim();
                result.Action = GetFallbackAction();

                // 괄호로 된 행동 묘사 찾기 (레거시)
                var legacyAction = ExtractLegacyAction(ref text);
                if (!string.IsNullOrEmpty(legacyAction))
                {
                    result.Action = legacyAction;
                }
            }

            // 2. 첫 줄만 사용
            int newlineIndex = text.IndexOf('\n');
            if (newlineIndex > 0)
            {
                text = text.Substring(0, newlineIndex).Trim();
            }

            // 3. 첫 문장만 (마침표, 느낌표, 물음표 기준)
            text = ExtractFirstSentence(text);

            // 4. 영어 포함 체크
            if (ContainsEnglish(text))
            {
                result.ContainedEnglish = true;
                Debug.Log($"[ResponseProcessor] 영어 감지됨: {text}");
            }

            // 5. 길이 제한
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

            // 6. 냥/야옹 추가
            text = EnsureCatSuffix(text);

            // 7. 최종 검증
            if (result.ContainedEnglish)
            {
                result.IsValid = false;
            }

            result.Text = text;
            return result;
        }

        /// <summary>
        /// [ACT]...[/ACT][TEXT]...[/TEXT] 포맷 파싱
        /// </summary>
        public static (string action, string text) ParseActTextFormat(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return ("", "");

            string action = "";
            string text = "";

            // [ACT]...[/ACT] 파싱
            int actStart = response.IndexOf("[ACT]");
            int actEnd = response.IndexOf("[/ACT]");
            if (actStart >= 0 && actEnd > actStart)
            {
                action = response.Substring(actStart + 5, actEnd - actStart - 5).Trim();
            }

            // [TEXT]...[/TEXT] 파싱
            int textStart = response.IndexOf("[TEXT]");
            int textEnd = response.IndexOf("[/TEXT]");
            if (textStart >= 0 && textEnd > textStart)
            {
                text = response.Substring(textStart + 6, textEnd - textStart - 6).Trim();
            }

            return (action, text);
        }

        /// <summary>
        /// [ACT]/[TEXT] 포맷 여부 확인
        /// </summary>
        public static bool IsActTextFormat(string response)
        {
            return response.Contains("[ACT]") && response.Contains("[/ACT]") &&
                   response.Contains("[TEXT]") && response.Contains("[/TEXT]");
        }

        /// <summary>
        /// 레거시 괄호 형식 행동 추출: (행동) 텍스트
        /// </summary>
        private static string ExtractLegacyAction(ref string text)
        {
            int parenStart = text.IndexOf('(');
            int parenEnd = text.IndexOf(')');

            if (parenStart >= 0 && parenEnd > parenStart && parenStart < 5)
            {
                string action = text.Substring(parenStart + 1, parenEnd - parenStart - 1).Trim();
                text = text.Substring(parenEnd + 1).Trim();
                return action;
            }

            return "";
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
        /// 대체 행동 반환
        /// </summary>
        public static string GetFallbackAction()
        {
            return FallbackActions[Random.Range(0, FallbackActions.Length)];
        }

        /// <summary>
        /// 기분에 맞는 대체 행동 반환
        /// </summary>
        public static string GetMoodFallbackAction(string moodTag)
        {
            return moodTag switch
            {
                "very_hungry" or "hungry" => new[] { "배를 쓰다듬음", "밥그릇을 쳐다봄", "입맛을 다심" }[Random.Range(0, 3)],
                "stressed" => new[] { "털을 곤두세움", "경계하며 주변을 둘러봄", "귀를 뒤로 젖힘" }[Random.Range(0, 3)],
                "bored" => new[] { "하품을 함", "발로 바닥을 긁음", "창밖을 멍하니 봄" }[Random.Range(0, 3)],
                "tired" => new[] { "눈을 비빔", "기지개를 켬", "졸린 듯 눈을 깜빡임" }[Random.Range(0, 3)],
                "happy" => new[] { "꼬리를 세우고 흔듦", "그루밍을 함", "기분 좋게 골골거림" }[Random.Range(0, 3)],
                _ => GetFallbackAction()
            };
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
