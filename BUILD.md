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

## 서명과 배포 — Developer ID 가 없을 때

`package.sh` 는 **ad-hoc 서명**을 한다. 서명 자체는 유효하고 샌드박스 entitlement 도
붙지만, Apple 이 발급한 인증서가 아니고 공증(notarization)도 받지 않은 상태다.

```
$ codesign -dv MewDummyDisplay.app
Signature=adhoc
TeamIdentifier=not set

$ spctl -a -t exec MewDummyDisplay.app
rejected
```

### 받는 사람에게 무슨 일이 생기나

브라우저·메일·AirDrop 으로 받은 파일에는 macOS 가 `com.apple.quarantine` 속성을
붙인다. 격리된 앱을 처음 열 때 Gatekeeper 가 공증을 확인하고, ad-hoc 은 여기서
걸린다. "Apple이 악성 소프트웨어가 없음을 확인할 수 없습니다" 가 그것이다.

여는 방법은 macOS 버전에 따라 다르다.

| macOS | 여는 방법 |
|---|---|
| 13, 14 | Control 키를 누른 채 클릭 → **열기** → 대화상자에서 다시 **열기** |
| 15 이상 | 한 번 열어서 거부당한 뒤, 시스템 설정 → 개인정보 보호 및 보안 → **그래도 열기** |
| 공통 | `xattr -d com.apple.quarantine /Applications/MewDummyDisplay.app` |

15(Sequoia)에서 Control-클릭 우회가 없어졌다는 점이 중요하다. 예전 안내를 그대로
쓰면 사용자가 막힌다.

### 격리를 피하는 경로

- **소스에서 빌드.** 직접 만든 바이너리에는 격리 속성이 붙지 않는다. 경고도 없다.
  오픈소스 프로젝트에서 가장 깔끔한 길이고, 이 저장소가 기본으로 삼는 방법이다.
- **Homebrew Cask.** cask 도 기본적으로 격리를 붙이므로
  `brew install --cask --no-quarantine ...` 가 필요하다.

### ad-hoc 서명으로도 되는 것과 안 되는 것

되는 것은 샌드박스다. entitlement 는 서명을 요구하는데 ad-hoc 도 서명이라 앱 샌드박스가
정상 동작한다. 이 앱이 쓰는 비공개 가상 디스플레이 API 가 샌드박스 안에서 동작하는 것도
확인했다.

안 되는 것은 **안정적인 코드 식별자**다. ad-hoc 서명은 내용에서 파생되므로 다시 빌드할
때마다 신원이 바뀐다. 시스템이 코드 신원으로 기억하는 권한 부여는 빌드마다 초기화될 수
있다.

### 자체 서명 인증서는 답이 아니다

키체인에서 만든 인증서로 서명해도 Gatekeeper 는 통과하지 못한다. Gatekeeper 가 신뢰하는
것은 Apple 이 발급한 Developer ID 뿐이라, 자체 인증서는 ad-hoc 과 결과가 같다.

### 제대로 하려면

Apple Developer Program(연 $99) → Developer ID Application 인증서 → hardened runtime 으로
서명 → 공증 → staple. 그러면 경고 없이 열린다.

```
codesign --force --options runtime --timestamp \
    --sign "Developer ID Application: 이름 (TEAMID)" \
    --entitlements entitlements.plist MewDummyDisplay.app
xcrun notarytool submit MewDummyDisplay.zip --keychain-profile 프로파일 --wait
xcrun stapler staple MewDummyDisplay.app
```

### Mac App Store 는 해당 없다

멤버십과 별개로, 이 앱은 가상 디스플레이를 만들기 위해 비공개 `CGVirtualDisplay` API 를
쓴다. App Store 심사는 비공개 API 사용을 허용하지 않는다.
