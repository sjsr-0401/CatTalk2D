#!/usr/bin/env python3
"""
CatTalk2D 모델 벤치마크 스크립트
- 여러 Ollama 모델의 성능을 비교 평가
- 한국어 응답률, 응답 시간, 캐릭터 일관성 측정
"""

import json
import time
import requests
import argparse
from pathlib import Path
from dataclasses import dataclass, asdict
from typing import Optional
import re


@dataclass
class BenchmarkResult:
    """개별 테스트 결과"""
    model: str
    prompt: str
    response: str
    response_time_ms: float
    is_korean: bool
    has_cat_suffix: bool
    response_length: int
    error: Optional[str] = None


@dataclass
class ModelSummary:
    """모델별 요약 통계"""
    model: str
    total_tests: int
    korean_rate: float
    cat_suffix_rate: float
    avg_response_time_ms: float
    avg_response_length: float
    error_count: int


OLLAMA_URL = "http://localhost:11434/api/generate"

# 테스트 프롬프트 세트
TEST_PROMPTS = [
    # 일상 대화
    {
        "control": {
            "moodTag": "happy",
            "affectionTier": "high",
            "ageLevel": "teen"
        },
        "userText": "안녕! 오늘 기분 어때?"
    },
    {
        "control": {
            "moodTag": "hungry",
            "affectionTier": "mid",
            "ageLevel": "teen"
        },
        "userText": "밥 먹었어?"
    },
    {
        "control": {
            "moodTag": "neutral",
            "affectionTier": "mid",
            "ageLevel": "teen"
        },
        "userText": "뭐하고 놀까?"
    },
    # 감정 반응
    {
        "control": {
            "moodTag": "stressed",
            "affectionTier": "low",
            "ageLevel": "teen"
        },
        "userText": "왜 그렇게 뚱해?"
    },
    {
        "control": {
            "moodTag": "tired",
            "affectionTier": "high",
            "ageLevel": "teen"
        },
        "userText": "졸려 보이네"
    },
    # 복잡한 요청
    {
        "control": {
            "moodTag": "happy",
            "affectionTier": "high",
            "ageLevel": "adult"
        },
        "userText": "오늘 날씨가 좋으니까 산책 갈까?"
    },
    {
        "control": {
            "moodTag": "bored",
            "affectionTier": "mid",
            "ageLevel": "child"
        },
        "userText": "심심하지 않아?"
    },
]


def build_prompt(control: dict, user_text: str) -> str:
    """Control 정보를 기반으로 프롬프트 생성"""
    mood_desc = {
        "happy": "기분이 좋음",
        "hungry": "배고픔",
        "stressed": "스트레스 받음",
        "tired": "피곤함",
        "bored": "심심함",
        "neutral": "평범함"
    }

    affection_desc = {
        "low": "낮은 호감도 (경계심)",
        "mid": "보통 호감도",
        "high": "높은 호감도 (친밀함)"
    }

    age_desc = {
        "child": "아기 고양이 (귀엽고 순수함)",
        "teen": "청소년 고양이 (장난기 있음)",
        "adult": "어른 고양이 (성숙함)"
    }

    prompt = f"""당신은 '망고'라는 이름의 귀여운 고양이입니다.

현재 상태:
- 기분: {mood_desc.get(control.get('moodTag', 'neutral'), '평범함')}
- 호감도: {affection_desc.get(control.get('affectionTier', 'mid'), '보통')}
- 나이: {age_desc.get(control.get('ageLevel', 'teen'), '청소년')}

규칙:
1. 반드시 한국어로만 대답하세요
2. 문장 끝에 '냥', '냥~', '냥!' 중 하나를 붙이세요
3. 1-2문장으로 짧게 대답하세요
4. 고양이답게 귀엽고 솔직하게 대답하세요

주인이 말합니다: "{user_text}"

망고의 대답:"""

    return prompt


def contains_english(text: str) -> bool:
    """영어 포함 여부 확인"""
    english_pattern = re.compile(r'[a-zA-Z]{2,}')
    # 일부 허용 패턴 제외 (예: OK, TV 등 일상적 외래어)
    allowed = {'ok', 'tv', 'pc', 'sns'}

    matches = english_pattern.findall(text.lower())
    for match in matches:
        if match not in allowed:
            return True
    return False


def has_cat_suffix(text: str) -> bool:
    """고양이 어미 확인"""
    return any(suffix in text for suffix in ['냥', '냐', '야옹', '먀'])


def run_test(model: str, prompt: str, timeout: int = 30) -> BenchmarkResult:
    """단일 테스트 실행"""
    start_time = time.time()

    try:
        response = requests.post(
            OLLAMA_URL,
            json={
                "model": model,
                "prompt": prompt,
                "stream": False,
                "options": {
                    "temperature": 0.7,
                    "top_p": 0.9,
                    "top_k": 40,
                    "repeat_penalty": 1.2
                }
            },
            timeout=timeout
        )

        elapsed_ms = (time.time() - start_time) * 1000

        if response.status_code == 200:
            result = response.json()
            text = result.get("response", "")

            return BenchmarkResult(
                model=model,
                prompt=prompt[:50] + "...",
                response=text,
                response_time_ms=elapsed_ms,
                is_korean=not contains_english(text),
                has_cat_suffix=has_cat_suffix(text),
                response_length=len(text)
            )
        else:
            return BenchmarkResult(
                model=model,
                prompt=prompt[:50] + "...",
                response="",
                response_time_ms=elapsed_ms,
                is_korean=False,
                has_cat_suffix=False,
                response_length=0,
                error=f"HTTP {response.status_code}"
            )

    except Exception as e:
        elapsed_ms = (time.time() - start_time) * 1000
        return BenchmarkResult(
            model=model,
            prompt=prompt[:50] + "...",
            response="",
            response_time_ms=elapsed_ms,
            is_korean=False,
            has_cat_suffix=False,
            response_length=0,
            error=str(e)
        )


def run_benchmark(models: list[str], output_dir: str = ".") -> dict:
    """전체 벤치마크 실행"""
    results = []
    summaries = []

    for model in models:
        print(f"\n{'='*50}")
        print(f"Testing model: {model}")
        print('='*50)

        model_results = []

        for i, test in enumerate(TEST_PROMPTS):
            prompt = build_prompt(test["control"], test["userText"])
            result = run_test(model, prompt)
            model_results.append(result)
            results.append(asdict(result))

            status = "OK" if result.is_korean and result.has_cat_suffix else "WARN"
            print(f"  [{i+1}/{len(TEST_PROMPTS)}] {status} - {result.response_time_ms:.0f}ms")
            if result.response:
                print(f"       Response: {result.response[:60]}...")
            if result.error:
                print(f"       Error: {result.error}")

        # 모델 요약 계산
        valid_results = [r for r in model_results if not r.error]
        if valid_results:
            summary = ModelSummary(
                model=model,
                total_tests=len(model_results),
                korean_rate=sum(1 for r in valid_results if r.is_korean) / len(valid_results) * 100,
                cat_suffix_rate=sum(1 for r in valid_results if r.has_cat_suffix) / len(valid_results) * 100,
                avg_response_time_ms=sum(r.response_time_ms for r in valid_results) / len(valid_results),
                avg_response_length=sum(r.response_length for r in valid_results) / len(valid_results),
                error_count=len(model_results) - len(valid_results)
            )
            summaries.append(asdict(summary))

    # 결과 저장
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)

    with open(output_path / "benchmark_results.json", "w", encoding="utf-8") as f:
        json.dump({
            "timestamp": time.strftime("%Y-%m-%d %H:%M:%S"),
            "models": models,
            "test_count": len(TEST_PROMPTS),
            "results": results,
            "summaries": summaries
        }, f, ensure_ascii=False, indent=2)

    # 요약 출력
    print("\n" + "="*60)
    print("BENCHMARK SUMMARY")
    print("="*60)

    for s in summaries:
        print(f"\n{s['model']}:")
        print(f"  한국어 응답률: {s['korean_rate']:.1f}%")
        print(f"  고양이 어미 사용률: {s['cat_suffix_rate']:.1f}%")
        print(f"  평균 응답 시간: {s['avg_response_time_ms']:.0f}ms")
        print(f"  평균 응답 길이: {s['avg_response_length']:.0f}자")
        print(f"  오류: {s['error_count']}건")

    return {"results": results, "summaries": summaries}


def main():
    parser = argparse.ArgumentParser(description="CatTalk2D 모델 벤치마크")
    parser.add_argument(
        "--models",
        nargs="+",
        default=["aya:8b", "gemma2:9b", "llama3.1:8b"],
        help="테스트할 모델 목록"
    )
    parser.add_argument(
        "--output",
        default="./benchmark_output",
        help="결과 저장 디렉토리"
    )

    args = parser.parse_args()

    print("CatTalk2D Model Benchmark")
    print(f"Models: {', '.join(args.models)}")
    print(f"Output: {args.output}")

    run_benchmark(args.models, args.output)


if __name__ == "__main__":
    main()
