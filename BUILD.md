# 빌드와 배포

## 준비

- .NET 10 SDK
- Xcode 명령줄 도구 (NativeAOT 가 clang 과 ld 를 쓴다)
- macOS 13 이상

앱은 형제 저장소 두 개를 프로젝트 참조로 가져온다. 라이브러리와 CLI 는 둘 다
필요 없다.

```
~/Dev/MewDummyDisplay
~/Dev/MewUI
~/Dev/MewVG
```

다른 곳에 두었다면 `MewUIRepoPath` 로 알려준다.

```
dotnet build src/MewDummyDisplay.App -p:MewUIRepoPath=/경로/MewUI
```

## 빌드

```
dotnet build src/MewDummyDisplay.slnx
dotnet test src/MewDummyDisplay.Tests
```

## 패키징

```
./build/package.sh                 # arm64 + x86_64 유니버설, zip 까지
./build/package.sh --arch arm64    # 한 아키텍처만
./build/package.sh --debug         # 개발용. 이 머신 아키텍처, Debug 레이아웃
./build/package.sh --no-publish    # 이미 publish 한 결과를 재사용
```

결과는 `.artifacts/dist/` 에 나온다.

배포본은 NativeAOT 다. 아키텍처마다 하나씩 만든 Mach-O 를 `lipo` 로 합쳐 유니버설
바이너리를 만들기 때문에, Apple Silicon 과 Intel 이 같은 파일 하나를 받는다. 런타임을
따로 설치할 필요가 없다.

Debug 번들은 프레임워크 의존 레이아웃 그대로다. apphost 가 관리 어셈블리를 옆에서
찾기 때문이고, 만들기가 빨라서 개발 중에는 이쪽을 쓴다.

## 배포 — 소스만 공개한다

이 저장소는 **빌드된 앱을 배포하지 않는다.** GitHub Releases 도, 설치 파일도 없다.
쓰려면 직접 빌드한다. Developer ID 인증서가 없기 때문이고, 그 상태로 바이너리를
뿌리는 것이 받는 사람에게 어떤 일인지는 아래에 적는다.

### 직접 빌드하면 경고가 없다

macOS 가 앱을 막는 근거는 서명이 아니라 `com.apple.quarantine` 이라는 확장 속성이다.
이 속성은 **다운로드한 파일**에 붙는다. 브라우저, 메일, 메시지, AirDrop, 그리고
격리를 붙이도록 만들어진 다른 앱들이 붙인다.

자기 머신에서 빌드한 결과물에는 붙지 않는다. 그래서 `./build/package.sh` 로 만든
번들은 Gatekeeper 대화상자 없이 그냥 열린다. 서명이 ad-hoc 인 것과 무관하다.

```
git clone https://github.com/aprillz/MewDummyDisplay.git
cd MewDummyDisplay
./build/package.sh
open .artifacts/dist/MewDummyDisplay.app
```

MewUI 와 MewVG 를 형제 디렉터리에 두는 것이 전제다. 위의 준비 항목을 본다.

### 빌드한 것을 다른 Mac 으로 옮긴다면

옮기는 방법이 격리 여부를 정한다.

| 옮기는 방법 | 격리 |
|---|---|
| `scp`, `rsync`, USB 로 복사 | 붙지 않음 |
| AirDrop, 메일, 클라우드 드라이브, 브라우저 다운로드 | 붙음 |

격리가 붙었다면 여는 방법은 macOS 버전마다 다르다.

| macOS | 여는 방법 |
|---|---|
| 13, 14 | Control 키를 누른 채 클릭 → **열기** → 대화상자에서 다시 **열기** |
| 15 이상 | 한 번 열어서 거부당한 뒤, 시스템 설정 → 개인정보 보호 및 보안 → **그래도 열기** |
| 공통 | `xattr -d com.apple.quarantine /경로/MewDummyDisplay.app` |

15(Sequoia)에서 Control-클릭 우회가 없어졌다. 인터넷에 흔한 옛 안내를 그대로 따르면
막힌다.

### ad-hoc 서명으로 되는 것과 안 되는 것

`package.sh` 는 ad-hoc 으로 서명한다. 서명은 유효하지만 Apple 이 발급한 인증서가
아니고 공증도 받지 않았다.

```
$ codesign -dv MewDummyDisplay.app
Signature=adhoc
TeamIdentifier=not set

$ spctl -a -t exec MewDummyDisplay.app
rejected
```

`spctl` 이 거부한다는 것은 **격리된 사본을 열 때** 어떻게 되는지를 말할 뿐이다.
격리되지 않은 사본에는 이 판정이 적용되지 않는다.

되는 것은 샌드박스다. entitlement 는 서명을 요구하는데 ad-hoc 도 서명이라 앱 샌드박스가
정상 동작하고, 이 앱이 쓰는 비공개 가상 디스플레이 API 가 그 안에서 동작하는 것도
확인했다.

안 되는 것은 **안정적인 코드 식별자**다. ad-hoc 서명은 내용에서 파생되므로 다시 빌드할
때마다 신원이 바뀐다. 시스템이 코드 신원으로 기억하는 권한 부여는 빌드마다 초기화될 수
있다. 개발 중에 권한을 다시 묻는다면 그 때문이다.

### 자체 서명 인증서는 답이 아니다

키체인에서 만든 인증서로 서명해도 결과는 ad-hoc 과 같다. Gatekeeper 가 신뢰하는 것은
Apple 이 발급한 Developer ID 뿐이다.

### 언젠가 바이너리를 배포한다면

Apple Developer Program(연 $99) → Developer ID Application 인증서 → hardened runtime 으로
서명 → 공증 → staple. 그러면 격리된 사본도 경고 없이 열린다.

```
codesign --force --options runtime --timestamp \
    --sign "Developer ID Application: 이름 (TEAMID)" \
    --entitlements entitlements.plist MewDummyDisplay.app
xcrun notarytool submit MewDummyDisplay.zip --keychain-profile 프로파일 --wait
xcrun stapler staple MewDummyDisplay.app
```

Mac App Store 는 멤버십과 별개로 해당이 없다. 이 앱은 가상 디스플레이를 만들기 위해
비공개 `CGVirtualDisplay` API 를 쓰고, App Store 심사는 비공개 API 사용을 허용하지 않는다.
