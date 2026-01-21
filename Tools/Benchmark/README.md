# CatTalk2D Model Benchmark

Ollama 모델 성능 비교 도구

## 설치

```bash
pip install -r requirements.txt
```

## 사용법

```bash
# 기본 모델 테스트 (aya:8b, gemma2:9b, llama3.1:8b)
python benchmark_models.py

# 특정 모델만 테스트
python benchmark_models.py --models aya:8b qwen2.5:7b

# 출력 디렉토리 지정
python benchmark_models.py --output ./results
```

## 평가 항목

1. **한국어 응답률**: 영어 없이 한국어로만 응답한 비율
2. **고양이 어미 사용률**: '냥', '냐' 등 고양이 어미를 사용한 비율
3. **평균 응답 시간**: API 요청부터 응답까지 걸린 시간 (ms)
4. **평균 응답 길이**: 응답 텍스트의 평균 길이

## 출력 파일

`benchmark_results.json` 파일이 생성되며 다음 내용을 포함:
- 개별 테스트 결과
- 모델별 요약 통계

## 테스트 시나리오

- 일상 대화 (인사, 밥, 놀이)
- 감정 반응 (스트레스, 피곤함)
- 다양한 호감도 수준 (low, mid, high)
- 다양한 나이 단계 (child, teen, adult)
