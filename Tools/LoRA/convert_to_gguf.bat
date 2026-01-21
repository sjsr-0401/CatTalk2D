@echo off
REM CatTalk2D GGUF 변환 스크립트
REM LoRA 학습 후 Ollama에서 사용할 수 있는 GGUF로 변환

echo === CatTalk2D GGUF 변환 ===
echo.

REM 변수 설정
set LORA_PATH=cheese_cat_lora
set OUTPUT_NAME=cheese_cat
set QUANTIZATION=q4_k_m

REM llama.cpp 경로 (수정 필요)
set LLAMA_CPP_PATH=C:\llama.cpp

echo 1. LoRA 모델 경로: %LORA_PATH%
echo 2. 출력 이름: %OUTPUT_NAME%
echo 3. 양자화: %QUANTIZATION%
echo.

REM llama.cpp 존재 확인
if not exist "%LLAMA_CPP_PATH%" (
    echo [오류] llama.cpp를 찾을 수 없습니다: %LLAMA_CPP_PATH%
    echo.
    echo llama.cpp 설치 방법:
    echo   git clone https://github.com/ggerganov/llama.cpp
    echo   cd llama.cpp
    echo   cmake -B build
    echo   cmake --build build --config Release
    echo.
    pause
    exit /b 1
)

echo [단계 1/3] HuggingFace 형식으로 변환 중...
python %LLAMA_CPP_PATH%\convert-hf-to-gguf.py %LORA_PATH% --outfile %OUTPUT_NAME%.gguf --outtype f16

if errorlevel 1 (
    echo [오류] 변환 실패
    pause
    exit /b 1
)

echo.
echo [단계 2/3] 양자화 중 (%QUANTIZATION%)...
%LLAMA_CPP_PATH%\build\bin\Release\llama-quantize.exe %OUTPUT_NAME%.gguf %OUTPUT_NAME%_%QUANTIZATION%.gguf %QUANTIZATION%

if errorlevel 1 (
    echo [오류] 양자화 실패
    pause
    exit /b 1
)

echo.
echo [단계 3/3] Modelfile 생성 중...
(
echo FROM ./%OUTPUT_NAME%_%QUANTIZATION%.gguf
echo.
echo SYSTEM """너는 주황색 치즈냥이 캐릭터 '망고'다.
echo 항상 한국어로 1~2문장으로 답하고, 말 끝에 '냥'을 붙인다.
echo """
echo.
echo PARAMETER temperature 0.7
echo PARAMETER top_p 0.9
echo PARAMETER repeat_penalty 1.1
) > Modelfile

echo.
echo === 변환 완료! ===
echo.
echo 생성된 파일:
echo   - %OUTPUT_NAME%.gguf (원본 F16)
echo   - %OUTPUT_NAME%_%QUANTIZATION%.gguf (양자화됨)
echo   - Modelfile
echo.
echo Ollama에 등록하려면:
echo   ollama create cheese-cat -f Modelfile
echo.
echo Unity에서 사용:
echo   OllamaAPIManager의 Model Name을 'cheese-cat'으로 변경
echo.
pause
