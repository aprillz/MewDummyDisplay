# MewDummyDisplay 저장소

macOS 전용 가상 더미 디스플레이 유틸리티. .NET + MewUI 기반.
BetterDummy 1.0.11(Swift)을 참조 자료로 삼아 새로 작성한다.

## 구조

- `src/`: 애플리케이션 소스
- `reference/`: 참조 자료. **읽기 전용, 수정 금지**
- `agent/better-dummy-porting/`: 설계·계획 문서

## 참조 문서 (agent/better-dummy-porting/)

문서 허브는 [README.md](agent/better-dummy-porting/README.md). 안내표에서 고른다.
참조(사실) 4편과 결정(선택·순서) 3편으로 나뉜다.

구조·동작에 대한 판단 근거는 위 문서와 소스 코드만 사용한다.

## 절대 규칙

참조 자료:
- `reference/**`는 수정하지 않는다. 참조 범위는 `reference/PROVENANCE.md`에
  고정된 커밋 하나다.
- BetterDisplay(후속작)의 코드·리소스·문자열은 참조하지 않는다.
- 원본의 아이콘·로고 등 브랜딩 자산을 번들에 넣지 않는다.

MewUI:
- `~/Dev/MewUI`를 `ProjectReference`로 참조하되 **MewUI 소스는 이 저장소에서
  수정하지 않는다.** 수정이 필요하면 MewUI 저장소에서 별도 작업으로 처리한다.
- `MacOSInterop`은 `internal`이다. 복사 대상이 아니라 패턴 참고 대상이다.

경계 (가장 중요):
- `MewDummyDisplay`(라이브러리)는 **어떤 UI 프레임워크도 참조하지 않는다.**
  MewUI 참조는 `MewDummyDisplay.App`에만 둔다.
- CLI로 못 하는 일이 라이브러리에 있으면 안 된다. 그것이 경계가 새는 신호다.
- 트레이 메뉴에는 필수 기능만 둔다(토글, 창 열기, 종료). 편집·설정은 창이다.
- `NSStatusItem`/`NSMenu` 코드는 App에 있다. 라이브러리는 이를 모른다.

안전 (사고로 배운 것):
- **`SLSDetectDisplays` 를 절대 호출하지 않는다.** 한 번에 28초, 다음 호출에 140초가
  걸렸고 로그인 세션의 디스플레이 서브시스템 전체를 멈추게 했다. 재부팅으로만
  복구된다. 효과도 없었다.
- 가상 디스플레이를 만드는 명령은 실행 전에 `DisplayHealth.Check` 를 통과해야 한다.
  디스플레이 열거가 2초를 넘으면 이미 이상한 상태이므로 즉시 중단한다.
- 한 프로세스에서 디스플레이 2개, 생성/해제 6사이클을 넘기지 않는다
  (`DisplayHealth.MAX_DISPLAYS_PER_RUN`, `MAX_CYCLES_PER_RUN`).
- 비공개 함수를 새로 시험할 때는 **한 번만** 호출하고 소요 시간을 잰다.
  느려지면 반복하지 않는다. 반복이 사고를 키웠다.
- 더미를 만들 때마다 macOS 가 ColorSync 장치를 등록하고
  (`/Library/Caches/ColorSync/com.apple.colorsync.devices`, root 전용) 더미를
  없애도 등록은 남는다. 죽은 등록이 수백 개 쌓이면 디스플레이 구성이 바뀌는
  순간 `colorsyncd` 가 100% 로 루프에 들어가고 재부팅으로도 안 풀린다.
  등록 해제 API 는 권한으로 막혀 있으니 serial 을 고정해 등록이 늘지 않게 한다
  (`ToolSerials`, `SelfTest.TEST_SERIAL`). 진단과 정리는 `tools/colorsync/`.
- 실험이 멈추면 프로세스부터 정리하고 사용자에게 알린다. WindowServer 를 강제
  종료하지 않는다. 강제 로그아웃이 되어 사용자의 작업이 날아간다.

비공개 API:
- `CGVirtualDisplay*` 호출은 `MewDummyDisplay.Native`에만 둔다.
  `Core`는 네이티브를 참조하지 않는다.
- `alloc`/`init`으로 얻은 객체는 `SafeHandle` 파생 타입으로만 다룬다.
  원시 `nint`를 여러 곳에서 들고 다니지 않는다.
- macOS 버전을 올리면 `reference/private-api/dump-private-api.m`을 다시 돌려
  이전 덤프와 diff 한다.
- 존재만 확인된 API는 실물 검증 전까지 기능으로 약속하지 않는다.

코드:
- static 필드 `_field` (s_ 금지), const는 `UPPER_SNAKE`, 한 글자 변수 금지,
  em dash (U+2014) 금지, 명확한 이분 분기는 if/else.
- 주석: XML doc(`///`)은 호출자용 계약만, `//`는 non-obvious WHY 한 줄만.
  본문 있는 멤버 상단 `//` 금지.

문서/작업:
- 설계·계획 문서는 `agent/<주제>/`에 둔다. 루트에 만들지 않는다.
- 한국어 문서 본문은 한글 표기, 식별자는 원문.
- "검토"/"분석" 요청에는 코드를 수정하지 않는다.

빌드:
- macOS에서 `dotnet build`. .NET 10 SDK.
- **개발 중 확인은 Debug 빌드로 한다. AOT 퍼블리시를 고집하지 않는다.**
  AOT 퍼블리시는 느리고, 확인하려는 것 대부분은 Debug 로 똑같이 확인된다.
  AOT 는 최종 산출물을 만들 때와, AOT 특유의 제약(`[UnmanagedCallersOnly]` 등)을
  검증할 때만 돌린다.
- MewUI 를 ProjectReference 로 쓸 때 참조를 net10.0 으로 고정한다
  (`SetTargetFramework="TargetFramework=net10.0"`). MewUI 는 다중 타깃이고
  net8.0 은 현재 빌드되지 않는다.
- MewUI 의 Release 산출물이 낡으면 "메서드가 없다"는 형태로 실패한다.
  백엔드가 코어의 `Shared/**` 를 소스로 링크하면서 타입은 참조 어셈블리에서
  가져오기 때문이다. `--no-incremental` 로 코어를 다시 만들면 풀린다.
- `.gitignore` 에 `*.app/` 류의 패턴을 쓰지 않는다. 이 파일시스템은 대소문자를
  구분하지 않아 `MewDummyDisplay.App/` 프로젝트까지 조용히 삼킨다.
- 좌표/로직 검증은 stderr 로그 우선, 스크린샷은 시각 확인용만.
