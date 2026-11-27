# Client

Unity 프로젝트입니다. 이 폴더(`Client/`)가 프로젝트 루트입니다.

- Unity 버전: **6000.3.29f1 (Unity 6.3 LTS)**. 팀 전원이 같은 버전을 씁니다.
- 렌더 파이프라인: URP (Universal 3D 템플릿)
- 입력: Input System 전용 (예전 Input Manager는 꺼져 있습니다)

## 여는 방법

1. Unity Hub에서 6000.3.29f1을 설치합니다.
2. Unity Hub의 `Add` → `Add project from disk`에서 이 `Client/` 폴더를 고릅니다.
3. 처음 열 때는 `Library/`를 새로 만드느라 몇 분 걸립니다.

Unity CLI를 쓴다면 `unity install 6000.3.29f1` 뒤에 이 폴더에서 `unity open .`으로 엽니다.

## 들어 있는 패키지

템플릿 기본 패키지 외에 눈여겨볼 것만 적습니다. 전체 목록은 `Packages/manifest.json`에 있습니다.

| 패키지 | 용도 |
|---|---|
| Input System | 입력 처리 |
| AI Navigation | 길찾기 (NavMesh) |
| Cinemachine | 카메라 |
| Pipeline (`com.unity.pipeline`) | Unity CLI로 열려 있는 에디터를 다루기 위한 패키지 |

패키지를 추가하거나 뺄 때는 `manifest.json`을 직접 고치지 말고 Package Manager를 씁니다.

## 지킬 것

- `.meta` 파일은 반드시 함께 커밋합니다.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`는 `.gitignore`로 제외되어 있습니다. 올리지 않습니다.
- 에디터 설정은 Asset Serialization이 Force Text, Meta 파일이 Visible 상태여야 합니다. 바꾸지 않습니다.
