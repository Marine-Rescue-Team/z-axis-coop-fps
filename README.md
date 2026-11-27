# Z-Axis 협동 FPS (가제)

Meshy **Z-Axis Awards** 게임 트랙 출품작. 세 명이 거점의 문을 지키는 일인칭 협동 방어전이고, 싸우는 도중 맵의 구조가 바뀝니다.

- 제출 마감: **2027년 1월 25일 08:59 (한국 시간)** / 1월 24일 23:59 UTC
- 마일스톤 일정과 통과 기준: [GitHub 마일스톤](https://github.com/Marine-Rescue-Team/z-axis-coop-fps/milestones)
- 화면 예시가 들어간 소개 페이지: [`Planning/intro-page/index.html`](Planning/intro-page/index.html)

## 폴더 구조

| 폴더 | 내용 |
|---|---|
| [`Client/`](Client) | Unity 프로젝트 |
| [`Server/`](Server) | 서버 코드 |
| [`Planning/`](Planning) | 기획 문서, 대회 정보, 마일스톤과 작업 목록 |

## 먼저 읽을 문서

1. [대회 정보](Planning/01-contest.md): 일정, 제출물, 평가 기준
2. [게임 기획](Planning/02-game-design.md): 무엇을 만드는지, 아직 정해지지 않은 것
3. [GitHub 마일스톤](https://github.com/Marine-Rescue-Team/z-axis-coop-fps/milestones): 단계별 일정과 통과 기준. 고칠 때는 [`Planning/milestones.yml`](Planning/milestones.yml)
4. [작업 분해(WBS) 초안](Planning/03-wbs-draft.md): 단계별로 필요한 작업을 미리 나눠 본 참고 목록
5. [작업 방식 안내](Planning/workflow/index.html): 폴더 담당, 브랜치, 티켓으로 일하는 순서. 내려받아 브라우저로 엽니다
6. [`AGENTS.md`](AGENTS.md): AI 에이전트가 따르는 작업 규칙. 작업 방식 안내와 같은 내용

## 팀

| 역할 | 맡는 일 |
|---|---|
| 클라 1 | 플레이어 조작, 캐릭터 스킬, 게임 진행, 첫 보스 |
| 클라 2 | 일반 적, 웨이브, 맵 변형, 보스 2와 3, 최종전 |
| 기획 | 세계관과 캐릭터 기획, 밸런스, AI 3D 에셋, 영상과 제출 자료 |
| 서버 | 방 만들기와 입장, 동기화 |

담당은 초안이며 팀 회의에서 조정합니다.

## 아직 정하지 않은 것

- 프로젝트 이름, 세계관과 캐릭터 컨셉
- 맵 변형의 구체적인 방식과 횟수
- Unity 버전, 멀티플레이 방식, 서버 기술
- Git LFS 사용 여부
