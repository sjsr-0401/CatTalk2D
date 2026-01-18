# CatTalk2D DevTools 프로토콜 문서

## 개요

Unity 게임과 WPF DevTools 클라이언트 간의 TCP 통신 프로토콜.

- **서버**: Unity (DevToolsServer.cs)
- **클라이언트**: WPF (DevToolsClient.cs)
- **주소**: localhost:9999
- **형식**: JSON (줄바꿈으로 메시지 구분)

## 메시지 형식

### 서버 → 클라이언트 (상태 브로드캐스트)

```json
{
  "type": "state",
  "timestamp": "2024-01-15 14:30:25.123",
  "gameDate": "2024-02-01",
  "catAgeDays": 45,
  "currentHour": 14,
  "currentMinute": 30,
  "hunger": 75.5,
  "energy": 60.0,
  "stress": 20.0,
  "fun": 80.0,
  "affection": 55.0,
  "trust": 40.0,
  "experience": 150,
  "level": 2,
  "playful": 70.0,
  "shy": 30.0,
  "aggressive": 25.0,
  "curious": 65.0,
  "mood": "happy"
}
```

- 0.5초마다 자동 브로드캐스트
- 클라이언트가 `get_state` 요청 시에도 전송

### 클라이언트 → 서버

#### 상태 조회 요청
```json
{
  "type": "get_state",
  "payload": ""
}
```

#### 상태 변경 요청
```json
{
  "type": "set_state",
  "payload": "{\"hunger\":80,\"energy\":50,\"stress\":10,\"fun\":90,\"affection\":60,\"trust\":45,\"experience\":200}"
}
```

- `-1` 값은 변경하지 않음을 의미
- payload는 JSON 문자열로 이스케이프됨

#### 날짜 설정
```json
{
  "type": "set_date",
  "payload": "{\"gameDate\":\"2024-03-01\"}"
}
```

#### 날짜 증감
```json
{
  "type": "add_days",
  "payload": "{\"days\":7}"
}
```

- 음수 값으로 날짜 감소 가능

## 상태 필드 설명

| 필드 | 타입 | 범위 | 설명 |
|------|------|------|------|
| hunger | float | 0-100 | 배고픔 (0=매우 배고픔) |
| energy | float | 0-100 | 에너지 (0=피곤) |
| stress | float | 0-100 | 스트레스 (0=편안) |
| fun | float | 0-100 | 재미 (0=심심) |
| affection | float | 0-100 | 호감도 |
| trust | float | 0-100 | 신뢰도 |
| experience | int | 0+ | 경험치 |
| level | int | 1+ | 레벨 (100 EXP마다 레벨업) |

## 성격 필드

| 필드 | 설명 |
|------|------|
| playful | 장난기 (높으면 활발) |
| shy | 소심함 (높으면 내성적) |
| aggressive | 까칠함 (높으면 예민) |
| curious | 호기심 (높으면 탐험적) |

## 기분 (mood) 값

| 값 | 조건 |
|---|---|
| very_hungry | hunger < 20 |
| hungry | hunger < 40 |
| stressed | stress > 70 |
| bored | fun < 30 |
| tired | energy < 30 |
| happy | fun > 70 && stress < 30 |
| neutral | 기타 |

## 로그 기록

DevTools로 값을 변경하면 `InteractionLogger`에 `dev_override` 타입으로 기록됨:

```json
{
  "timestamp": "2024-01-15 14:30:25.123",
  "actionType": "dev_override",
  "gameDate": "2024-02-01",
  "catAgeDays": 45,
  "state": { ... },
  "payload": "{\"field\":\"hunger\",\"oldValue\":\"50.0\",\"newValue\":\"80.0\"}"
}
```

## 테스트 시나리오

### 1. 연결 테스트
1. Unity 에디터에서 Play 모드 시작
2. WPF DevTools 실행 → "연결" 클릭
3. 상태 표시가 실시간으로 업데이트되는지 확인

### 2. 상태 변경 테스트
1. DevTools에서 슬라이더로 hunger를 80으로 변경
2. "적용" 클릭
3. Unity 콘솔에 `[DevToolsServer] 상태 변경 적용됨` 로그 확인
4. DevTools에 변경된 값이 다시 반영되는지 확인

### 3. 날짜 변경 테스트
1. "날짜 증감"에서 +7 설정 후 "+" 클릭
2. 게임 날짜가 7일 증가하는지 확인
3. 고양이 나이(AgeDays)도 함께 증가하는지 확인

### 4. 로그 기록 테스트
1. DevTools로 상태 변경 수행
2. `%APPDATA%/../LocalLow/DefaultCompany/CatTalk2D/CatLogs/` 폴더의 최신 로그 확인
3. `dev_override` 레코드가 기록되었는지 확인

### 5. 연결 끊김 테스트
1. Unity Play 모드 종료
2. DevTools가 "연결 끊김" 상태로 변경되는지 확인
3. 다시 Play 모드 시작 → "연결" 클릭하면 재연결되는지 확인

## 보안 참고

- 서버는 `IPAddress.Loopback`만 바인딩 (외부 접근 불가)
- 개발용 도구이므로 릴리스 빌드에서는 `_enableServer = false` 권장
