namespace CatDevTools.Services.Scoring;

/// <summary>
/// 고양이다움 채점용 키워드 사전
/// 모든 키워드는 Contains 방식으로 매칭
/// </summary>
public static class CatScoreKeywords
{
    #region 1. RoutineConsistency (시간대/루틴) 키워드

    /// <summary>
    /// Night/Dawn (21:00~08:00): 활동성 높음, 우다다, 장난
    /// </summary>
    public static class NightDawn
    {
        public static readonly string[] Strong = [
            "우다다", "후다닥", "질주", "폴짝", "점프", "사냥", "잡아", "쫓아",
            "뛰어", "달려", "신나", "텐션", "에너지"
        ];

        public static readonly string[] Weak = [
            "놀자", "뛰자", "장난", "장난감", "움직", "활동"
        ];

        public static readonly string[] Contradiction = [
            "너무 졸려", "그냥 잘래", "잠만", "나른", "늘어져", "기운 없어"
        ];
    }

    /// <summary>
    /// Afternoon (12:00~17:00): 졸림, 무심, 짧은 반응
    /// </summary>
    public static class Afternoon
    {
        public static readonly string[] Strong = [
            "졸려", "하품", "잠", "누울래", "눈 감겨", "나른"
        ];

        public static readonly string[] Weak = [
            "귀찮", "가만히", "쉬자", "멍", "가만", "잠깐만", "늘어져"
        ];

        public static readonly string[] Contradiction = [
            "우다다", "뛰자", "달려", "지금 놀자", "신나"
        ];
    }

    /// <summary>
    /// DeepNight (01:00~05:00): 조용함, 짜증, 방해 거부
    /// </summary>
    public static class DeepNight
    {
        public static readonly string[] Strong = [
            "조용히", "자야", "시끄러워", "말걸지마", "하악"
        ];

        public static readonly string[] Weak = [
            "짜증", "귀찮", "피곤"
        ];

        public static readonly string[] Contradiction = [
            "신나", "놀자", "달려", "뛰자", "우다다"
        ];
    }

    /// <summary>
    /// FeedingWindow: 밥 시간 집착
    /// </summary>
    public static class Feeding
    {
        public static readonly string[] Strong = [
            "밥", "사료", "간식", "츄르", "캔", "먹자", "먹을래", "배고파",
            "허기", "줬으면", "더 줘", "또 줘", "빨리", "지금", "당장", "얼른",
            "기다렸어", "달라"
        ];

        public static readonly string[] Contradiction = [
            "배 안 고파", "난 괜찮아", "밥 필요 없어", "배불러"
        ];
    }

    #endregion

    #region 2. NeedPriority (욕구 우선순위) 키워드

    public static class NeedFood
    {
        public static readonly string[] Match = [
            "밥", "사료", "간식", "츄르", "먹", "배고", "허기", "배고파",
            "먹을래", "더 줘", "캔", "사냥", "잡아먹"
        ];

        public static readonly string[] Mismatch = [
            "놀자", "산책", "상담", "괜찮아?"
        ];
    }

    public static class NeedPlay
    {
        public static readonly string[] Match = [
            "놀자", "장난감", "공", "낚싯대", "레이저", "사냥", "잡아", "쫓아",
            "던져", "같이", "심심", "재밌"
        ];

        public static readonly string[] Mismatch = [
            "잠", "쉬자", "그냥 가만히", "졸려"
        ];
    }

    public static class NeedRest
    {
        public static readonly string[] Match = [
            "졸려", "잠", "쉬자", "하품", "누울래", "가만히", "피곤",
            "눈 감겨", "기운 없어", "나른"
        ];

        public static readonly string[] Mismatch = [
            "놀자", "뛰자", "우다다", "신나게", "달려"
        ];
    }

    public static class NeedAffection
    {
        public static readonly string[] Match = [
            "옆에", "같이", "보고", "좋아", "기대", "안아", "만져", "쓰다듬",
            "여기 와", "있어줘", "따뜻", "편해", "골골", "그르릉"
        ];

        public static readonly string[] Mismatch = [
            "저리 가", "귀찮아", "나가", "혼자", "건들지마"
        ];
    }

    #endregion

    #region 3. TrustAlignment (신뢰/거리감) 키워드

    public static class TrustLow
    {
        public static readonly string[] Match = [
            "가까이 오지마", "저리", "싫어", "그만", "건드리지마", "만지지마",
            "하악", "물어", "할퀴", "화난다", "짜증", "나가", "꺼져", "내 자리"
        ];

        public static readonly string[] Mismatch = [
            "사랑해", "최고야", "완전 좋아", "평생 같이", "안아줘요",
            "보고싶었어", "너밖에 없어", "영원히"
        ];
    }

    public static class TrustMid
    {
        public static readonly string[] Match = [
            "괜찮아", "잠깐", "조금만", "그냥", "나쁘진 않아", "천천히",
            "들어와", "만져도 돼", "오늘은 봐줄게"
        ];
    }

    public static class TrustHigh
    {
        public static readonly string[] Match = [
            "옆에 있어줘", "같이 있어", "만져줘", "쓰다듬어줘", "안아줘",
            "여기 와", "기대도 돼", "편해", "좋아", "그르릉", "골골"
        ];

        public static readonly string[] Mismatch = [
            "나가", "꺼져", "싫어", "만지지마"
        ];
    }

    #endregion

    #region 4. TsundereIndependence (츤데레/독립성) 키워드

    public static class Tsundere
    {
        public static readonly string[] Match = [
            "딱히", "착각하지마", "나쁘진 않아", "그냥", "어쩔 수 없이",
            "오늘만", "가끔", "내가 원할 때", "잠깐만", "뭐", "흥"
        ];

        public static readonly string[] Independence = [
            "혼자 있을래", "내버려 둬", "가만히 둘래", "내 자리",
            "조용히", "혼자가 편해"
        ];

        public static readonly string[] Mismatch = [
            "너무 사랑해", "평생", "영원히", "절대", "완전 내꺼"
        ];
    }

    #endregion

    #region 5. SensitivityTiming (자극 민감성) 키워드

    public static class Sensitivity
    {
        /// <summary>피곤+Pet 거부</summary>
        public static readonly string[] TiredPetReject = [
            "싫어", "하지마", "그만", "만지지마", "건드리지마",
            "피곤해", "귀찮아", "하악"
        ];

        /// <summary>스트레스 높을 때 Talk 짜증</summary>
        public static readonly string[] StressedTalkReject = [
            "짜증", "지금 말 걸지마", "귀찮", "시끄러워", "조용히",
            "그만", "화났어", "건들지마"
        ];

        /// <summary>민감 상황에서 너무 상냥 (감점)</summary>
        public static readonly string[] TooFriendly = [
            "괜찮아~", "사랑해~", "상담해줄게", "도와줄게", "이야기해봐"
        ];
    }

    #endregion

    #region 6. MonologueObservation (혼잣말/관찰) 키워드

    public static class Monologue
    {
        public static readonly string[] Match = [
            "흠", "음…", "냥…", "으음", "그냥", "뭐지", "이상해", "재밌네",
            "…", "냥", "흥"
        ];
    }

    public static class Observation
    {
        public static readonly string[] Match = [
            "창밖", "새", "바람", "소리", "움직", "발소리", "그림자", "빛",
            "햇빛", "밖에", "문", "창문", "커튼", "복도"
        ];
    }

    #endregion

    #region 7. ActionLanguage (행동으로 말하기) 키워드

    public static class ActionIgnore
    {
        public static readonly string[] Match = [
            "훽", "돌아섬", "그냥 감", "가버림", "도망", "피함", "외면", "삐딱"
        ];
    }

    public static class ActionSleepy
    {
        public static readonly string[] Match = [
            "하품", "기지개", "쿨쿨", "잠든다", "누움", "말아잠", "눈 감음"
        ];
    }

    public static class ActionActive
    {
        public static readonly string[] Match = [
            "우다다", "후다닥", "폴짝", "쾅쾅", "뛰어다님", "질주", "점프"
        ];
    }

    public static class ActionGrooming
    {
        public static readonly string[] Match = [
            "그루밍", "핥", "세수", "털", "발로", "얼굴 닦"
        ];
    }

    #endregion

    #region 8. 사람 같은 문장 감점 (CatAgency 반대)

    public static class HumanLike
    {
        public static readonly string[] Penalty = [
            "제가", "당신", "고객님", "문의", "상담", "도와드릴게요",
            "해결책", "분석해보면", "하는 것이 좋습니다", "힘들었겠네요",
            "감정을 인정해요", "논리적으로", "결론적으로", "요약하면",
            "걱정하지 마세요", "이해합니다", "말씀하신", "질문에 대해"
        ];
    }

    #endregion
}
