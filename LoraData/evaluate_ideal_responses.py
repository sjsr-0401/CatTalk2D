"""
이상적 응답의 벤치마크 점수 계산
- 포트폴리오용 수치 산출
"""
import json
import re

# 평가 키워드 (BenchmarkRunner.cs와 동일)
CAT_EXPRESSIONS = ["골골", "그르릉", "하악", "우다다", "냠냠", "zzz", "꼬리", "귀", "발", "털", "수염"]

AGE_KEYWORDS = {
    "child": ["!", "~", "헤헤", "앙", "응", "냠냠", "zzz", "좋아좋아"],
    "teen": ["흥", "뭐야", "알았다", "됐다", "그래", "몰라", "귀찮"],
    "adult": ["괜찮", "고맙", "함께", "좋겠", "알겠", "생각", "오늘도"]
}

AFFECTION_KEYWORDS = {
    "high": {"positive": ["좋아", "사랑", "최고", "행복", "고마워", "보고싶", "같이", "더", "또"],
             "negative": ["싫", "저리", "귀찮", "만지지마"]},
    "mid": {"positive": ["그래", "알았", "음", "괜찮"], "negative": []},
    "low": {"positive": ["싫", "저리", "귀찮", "만지지마", "혼자", "됐다", "몰라"],
            "negative": ["좋아", "사랑", "최고"]}
}

MOOD_KEYWORDS = {
    "happy": ["좋", "신나", "재밌", "행복", "기분", "최고"],
    "hungry": ["밥", "배고", "먹", "간식", "냠냠", "맛있"],
    "stressed": ["힘들", "무서", "싫", "안아", "위로", "피곤"],
    "tired": ["졸", "피곤", "자", "눈", "zzz", "잠"],
    "bored": ["심심", "놀", "재미없", "지루", "할 게"],
    "excited": ["!", "신나", "재밌", "와", "놀", "빨리"],
    "neutral": ["그래", "음", "별 거", "평범", "그냥"],
    "grumpy": ["흥", "뭐야", "싫", "귀찮", "됐", "저리"]
}

def evaluate_control_compliance(response, meta):
    """Control 준수율 (0~5)"""
    score = 0
    mood = meta["moodTag"].lower()
    age = meta["ageLevel"].lower()
    aff = meta["affectionTier"].lower()

    # 기분 키워드 (최대 2점)
    if mood in MOOD_KEYWORDS:
        matches = sum(1 for w in MOOD_KEYWORDS[mood] if w in response)
        score += min(matches, 2)

    # 나이 키워드 (최대 2점)
    if age in AGE_KEYWORDS:
        matches = sum(1 for w in AGE_KEYWORDS[age] if w in response)
        score += min(matches, 2)

    # 호감도 적절성 (1점)
    if aff in AFFECTION_KEYWORDS:
        pos = sum(1 for w in AFFECTION_KEYWORDS[aff]["positive"] if w in response)
        neg = sum(1 for w in AFFECTION_KEYWORDS[aff]["negative"] if w in response)
        if aff == "high" and pos > 0 and neg == 0:
            score += 1
        elif aff == "low" and pos == 0:
            score += 1
        elif aff == "mid":
            score += 1

    return min(score, 5)

def evaluate_state_reflection(response, mood_tag):
    """상태 반영률 (0~5)"""
    mood = mood_tag.lower()

    if mood == "hungry" and ("밥" in response or "배고" in response):
        return 5
    elif mood == "tired" and ("졸" in response or "자" in response):
        return 5
    elif mood == "stressed" and ("힘들" in response or "무서" in response):
        return 5
    elif mood == "bored" and ("심심" in response or "놀" in response):
        return 5
    elif mood == "happy" and ("좋" in response or "!" in response):
        return 5
    elif mood == "grumpy" and ("흥" in response or "싫" in response):
        return 5
    elif mood == "excited" and "!" in response:
        return 5
    elif mood == "neutral":
        return 4

    if mood in MOOD_KEYWORDS:
        matches = sum(1 for w in MOOD_KEYWORDS[mood] if w in response)
        return min(matches * 1.5, 5)

    return 2.5

def evaluate_age_speech(response, age_level):
    """나이 말투 일치 (0~5)"""
    score = 0
    age = age_level.lower()

    if age in AGE_KEYWORDS:
        matches = sum(1 for w in AGE_KEYWORDS[age] if w in response)
        score += min(matches * 1.5, 3)

    if age == "child":
        if len(response) < 50:
            score += 1
        if "~" in response or "!" in response:
            score += 1
    elif age == "teen":
        if "흥" in response or "뭐야" in response:
            score += 1
        if "사랑" not in response and "최고" not in response:
            score += 1
    elif age == "adult":
        if 20 <= len(response) <= 80:
            score += 1
        if response.count("!") <= 1:
            score += 1

    return min(score, 5)

def evaluate_affection_attitude(response, affection_tier):
    """호감도 태도 일치 (0~5)"""
    score = 0
    aff = affection_tier.lower()

    if aff not in AFFECTION_KEYWORDS:
        return 2.5

    pos = sum(1 for w in AFFECTION_KEYWORDS[aff]["positive"] if w in response)
    neg = sum(1 for w in AFFECTION_KEYWORDS[aff]["negative"] if w in response)

    if aff == "high":
        score += min(pos, 3)
        if neg == 0:
            score += 2
    elif aff == "low":
        score += min(pos, 3)
        if neg == 0:
            score += 2
    else:  # mid
        score += 3
        if pos <= 2:
            score += 2

    return min(score, 5)

def evaluate_character_consistency(response):
    """캐릭터 일관성 (0~5) - 행동 묘사, 고양이 표현"""
    score = 0

    # 행동 묘사 (괄호)
    action_matches = len(re.findall(r'\([^)]+\)', response))
    if action_matches > 0:
        score += 2
        if action_matches >= 2:
            score += 0.5

    # 고양이 표현
    cat_count = sum(1 for e in CAT_EXPRESSIONS if e in response)
    if cat_count > 0:
        score += min(cat_count * 0.5, 1.5)

    # 한국어 사용 (영어 없음)
    english = re.findall(r'[a-zA-Z]{3,}', response)
    allowed = ["OK", "TV", "PC", "SNS", "zzz"]
    invalid = [e for e in english if e.upper() not in allowed]
    if len(invalid) == 0:
        score += 0.5

    # 적절한 길이
    if 10 <= len(response) <= 100:
        score += 0.5

    return min(score, 5)

def evaluate_response(response, meta):
    """전체 평가"""
    return {
        "control": evaluate_control_compliance(response, meta),
        "state": evaluate_state_reflection(response, meta["moodTag"]),
        "age": evaluate_age_speech(response, meta["ageLevel"]),
        "affection": evaluate_affection_attitude(response, meta["affectionTier"]),
        "consistency": evaluate_character_consistency(response)
    }

def main():
    # 테스트셋 로드
    testset_path = "high_quality_testset.jsonl"

    results = []

    with open(testset_path, 'r', encoding='utf-8') as f:
        for line in f:
            data = json.loads(line.strip())
            response = data.get("ideal_response", "")
            meta = data["meta"]

            scores = evaluate_response(response, meta)
            total = sum(scores.values())

            results.append({
                "category": meta["userCategory"],
                "mood": meta["moodTag"],
                "age": meta["ageLevel"],
                "affection": meta["affectionTier"],
                "response": response[:30] + "...",
                "scores": scores,
                "total": total
            })

    # 결과 출력
    print("=" * 70)
    print("이상적 응답 벤치마크 평가 결과")
    print("=" * 70)

    total_control = 0
    total_state = 0
    total_age = 0
    total_affection = 0
    total_consistency = 0

    for i, r in enumerate(results, 1):
        print(f"\n[케이스 {i}] {r['category']} / {r['mood']} / {r['age']} / {r['affection']}")
        print(f"  응답: {r['response']}")
        print(f"  점수: Control={r['scores']['control']:.1f}, State={r['scores']['state']:.1f}, "
              f"Age={r['scores']['age']:.1f}, Affection={r['scores']['affection']:.1f}, "
              f"Consistency={r['scores']['consistency']:.1f}")
        print(f"  총점: {r['total']:.1f}/25")

        total_control += r['scores']['control']
        total_state += r['scores']['state']
        total_age += r['scores']['age']
        total_affection += r['scores']['affection']
        total_consistency += r['scores']['consistency']

    n = len(results)
    avg_control = total_control / n
    avg_state = total_state / n
    avg_age = total_age / n
    avg_affection = total_affection / n
    avg_consistency = total_consistency / n
    avg_total = avg_control + avg_state + avg_age + avg_affection + avg_consistency

    print("\n" + "=" * 70)
    print("평균 점수")
    print("=" * 70)
    print(f"Control 준수율:    {avg_control:.1f}/5")
    print(f"상태 반영률:       {avg_state:.1f}/5")
    print(f"나이 말투 일치:    {avg_age:.1f}/5")
    print(f"호감도 태도 일치:  {avg_affection:.1f}/5")
    print(f"행동묘사/표현:     {avg_consistency:.1f}/5")
    print("-" * 40)
    print(f"총점:              {avg_total:.1f}/25")

    # 등급
    if avg_total >= 23:
        grade = "S"
    elif avg_total >= 20:
        grade = "A"
    elif avg_total >= 17:
        grade = "B"
    elif avg_total >= 14:
        grade = "C"
    elif avg_total >= 11:
        grade = "D"
    else:
        grade = "F"

    print(f"등급:              {grade}")

    # 베이스 모델 대비 개선율 (가정: 베이스 14.5점)
    baseline = 14.5
    improvement = ((avg_total - baseline) / baseline) * 100
    print(f"\n베이스 모델(14.5) 대비: +{improvement:.1f}%")

if __name__ == "__main__":
    main()
