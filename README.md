# MewDummyDisplay

macOS용 가상 더미 디스플레이 유틸리티. 메뉴 바에 살면서 가상 디스플레이를 만들고,
실제 모니터가 그 화면을 미러링하게 한다.

macOS는 4K 미만 모니터에 HiDPI("Retina") 모드를 주지 않는다. 원하는 해상도를 가진
가상 디스플레이를 만들고 실제 모니터를 거기에 미러링하면, 모니터가 그 해상도로
돌아간다. 이 앱은 그 가상 디스플레이를 관리한다.

[BetterDummy](https://github.com/waydabber/BetterDummy) 1.0.11을 참조 자료로 삼아
.NET 10 / [MewUI](https://github.com/aprillz) 기반으로 새로 작성했다. 고지는
[NOTICE](NOTICE)에 있다.

## 요구 사항

- macOS 13.0 이상
- Apple Silicon 또는 Intel (배포본은 유니버설 바이너리)

## 쓰는 법

메뉴 바 아이콘을 누르면 정의해 둔 더미 목록이 나온다. 각 항목의 스위치가 그 더미를
켜고 끈다. `Manage dummy displays...` 는 창을 열어 더미를 만들고, 이름을 바꾸고,
해상도를 고르고, 어느 모니터가 그것을 미러링할지 정하게 한다.

더미를 "정의하는 것"과 "켜는 것"은 다르다. 정의는 설정에 남고, 켜는 것은 그 순간
macOS에 디스플레이를 붙이는 일이다. 정의는 유지한 채 꺼 둘 수 있다.

## 설치

**빌드된 앱을 배포하지 않는다.** Releases 에 내려받을 파일이 없고, 직접 빌드해서 쓴다.
Developer ID 인증서가 없어서 공증받은 바이너리를 낼 수 없기 때문이다.

직접 빌드한 앱에는 격리 속성이 붙지 않으므로, 다운로드한 앱에 나오는 Gatekeeper 경고를
겪을 일이 없다. 그 이유와 예외는 [BUILD.md](BUILD.md) 에 있다.

MewUI 와 MewVG 를 형제 디렉터리에 둔다.

```
git clone https://github.com/aprillz/MewDummyDisplay.git
cd MewDummyDisplay
./build/package.sh
open .artifacts/dist/MewDummyDisplay.app
```

## 만들기

```
dotnet build src/MewDummyDisplay.slnx     # 라이브러리, CLI, 앱, 테스트
dotnet test src/MewDummyDisplay.Tests
./build/package.sh                        # 유니버설 .app 과 zip
./build/package.sh --debug                # 개발용, 이 머신 아키텍처만
```

앱은 `--self-test` 로 스스로를 검사한다. 더미를 하나 만들고, 켜고, 끄고, 창을 열어
목록을 확인한 뒤 정리한다.

## 명령줄

`mdd` 는 개발용 도구다. 라이브러리만 쓰고 MewUI 에 의존하지 않는다.

```
dotnet run --project src/MewDummyDisplay.Cli -- list
```

| 명령 | 하는 일 |
|---|---|
| `list` | 붙어 있는 디스플레이 |
| `definitions` | 만들 수 있는 더미 종류 |
| `create <id> [--hold <초>]` | 더미를 만들어 잠시 유지 |
| `modes <displayId>` | 그 디스플레이의 모드 |
| `setmode <displayId> <w> <h>` | 모드 변경 |
| `mirror <대상> <원본>` | 미러링 |
| `probe` | 가상 디스플레이가 이 시스템에서 되는지 |
| `stress` | 반복 생성·해제 |

## 저장소 구조

| 경로 | 내용 |
|---|---|
| `src/MewDummyDisplay/` | 라이브러리. 가상 디스플레이 생성, 모드, 미러링 |
| `src/MewDummyDisplay.App/` | 메뉴 바 애플리케이션 |
| `src/MewDummyDisplay.Cli/` | `mdd` 개발 도구 |
| `src/MewDummyDisplay.Tests/` | 테스트 |
| `src/Shared/ObjCRuntime/` | 라이브러리와 앱이 함께 쓰는 Objective-C 헬퍼 |
| `build/` | 패키징 |
| `docs/` | macOS 쪽에서 겪은 것들. [ColorSync 등록부](docs/colorsync.md) |

## 라이선스

MIT. 참조 원본 BetterDummy 도 MIT 이며, 고지는 [NOTICE](NOTICE) 에 있다.
