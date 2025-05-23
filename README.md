# 🧠 스마트팩토리 GPT 챗봇 시스템

이 프로젝트는 **Unity 기반의 스마트팩토리 시뮬레이터**에 **OpenAI GPT 챗봇**을 통합한 시스템입니다. 사용자는 자연어로 AMR(자율이동로봇)을 제어하거나, 설비 상태를 확인하고, 예지보전 알림을 받을 수 있습니다.

---

## 📦 주요 구성 요소

- **GPTChatbot.cs**  
  → 사용자 입력 처리, 역할 기반 명령어 실행, GPT 응답 처리

- **MESStatusReader.cs**  
  → Node.js 서버에서 진동·온도 데이터 수신 및 비정상 상태 감지

- **ARMWaypointMovement.cs / ARMMovement.cs**  
  → 자동·수동 AMR 제어 및 팔레트 적재/하역 로직

- **GraphUpdater.cs / GraphUIRenderer.cs**  
  → 최근 진동 수치 시각화 (그래프 출력)

- **index.js (Node.js)**  
  → 진동/온도 데이터 시뮬레이션 및 SQLite DB 저장, REST API 제공

---

## 💡 주요 기능

- 자연어 명령 기반 AMR 제어 (`정지`, `시작`, `수동`, `자동`, `적재`, `하역`, `진단` 등)
- 역할(작업자/관리자/엔지니어)에 따른 권한 제어
- 진동 이상 감지 시 경고 알림
- 실시간 진동 그래프 시각화
- 챗봇 UI On/Off toggle 기능 제공

---

## 🛠️ 실행 방법

### 1. 백엔드 서버 실행
```bash
cd Backend
npm install
node index.js
```
## 2. Unity 프로젝트 실행

- `Assets/Resources/apikey.json` 파일에 OpenAI API 키를 입력합니다.
- Unity 에디터에서 프로젝트를 실행하면 챗봇과 AMR 시뮬레이션이 동작합니다.

---

## 📌 사용 기술

- **Unity**: UI 구성, Rigidbody 기반 AMR 제어, 자연어 명령 처리
- **Node.js**: Express 서버, 센서 시뮬레이션, SQLite 데이터 저장 및 API 제공
- **OpenAI API**: GPT-3.5-turbo를 활용한 자연어 처리

---

## 🔒 주의사항

- `.gitignore`에 아래 파일을 반드시 추가해야 합니다:
  - `apikey.json` (API 키 포함)
  - `factory.db` (로컬 센서 로그 DB)
- OpenAI API 키는 절대로 외부에 노출되지 않도록 관리하세요.

---

## 📷 시연 예시

> 🗨️ `"진동 상태 알려줘"`  
> → 📈 그래프 시각화 + 요약 응답 + 예지보전 메시지 제공

> 🗨️ `"AMR 시작"`  
> → AMR 자동 주행 시작

> 🗨️ `"정지"`  
> → AMR 중지 및 수동 전환
