# ColorSync 장치 등록부와 데몬 폭주

2026-09-04 에 이 머신에서 하루를 태운 문제와 그 해결 기록. macOS 가 가상 디스플레이를
어떻게 기억하는지에 대한 것이라, 같은 종류의 앱을 만들면 누구나 밟는다.

## 증상

더미를 하나라도 연결하면 `colorsync.displayservices` 가 코어 하나를 붙잡고 놓지
않았다. 부팅 후 53분 중 두 데몬이 각각 17분씩 태웠다.

```
PID  %CPU  COMMAND
294  63    /usr/libexec/colorsync.displayservices
298  37    /usr/libexec/colorsyncd
```

시스템이 전반적으로 밀리고, 시스템 설정의 색상 프로파일 목록이 비었다. 더미를 모두
꺼도 돌아오지 않았고, **재부팅해도 첫 더미를 연결하는 순간 다시 시작됐다.**

## 무슨 일이 벌어지고 있었나

로그가 초당 15건의 같은 순환을 보여줬다.

```
received XPC_DISPLAY_INFO_REQUEST ...
ColorSyncProfileCreateDeviceProfile(0x2) ... succeeded. Profile desc: sRGB IEC61966-2.1
ColorSyncDisplayServicesAgent: updating display profiles for user = ...
ColorSyncDisplayServicesAgent: sending display_profile_info ...
```

요청마다 실제 모니터의 팩토리 `.icc` 가 새로 쓰였다. 파일 크기는 그대로인데 md5 는
3초마다 달랐다. 그 쓰기가 다시 변경 알림을 낳고, 화면을 캡처하던 클라이언트가 다시
조회하고, 순환이 닫힌다.

캐시가 죽었다는 것은 호출 비용으로도 보였다.

```
50 x CGDisplayCopyColorSpace(2): 602 ms  (호출당 12 ms)
```

정상이면 마이크로초 단위다.

## 원인이 아니었던 것들

**`talagentd`.** 분당 595회 `XPC_PROFILE_CACHE_QUERY ... Connection invalid` 를 남기고
있어서 유력해 보였지만, 죽여도 요청 빈도가 그대로였다. 캐시가 깨진 결과였지 원인이
아니었다.

**화면 캡처 클라이언트.** RustDesk 가 프레임마다 프로파일을 조회하니 의심스러웠지만,
캐시가 정상이면 그 조회는 공짜다. 부하를 드러냈을 뿐이다.

**더미의 개수와 미러링.** 등록부를 정리한 뒤에는 더미 하나도, 둘 동시에도, 미러링을
걸어도 요청이 0 건이었다. 처음에 의심했던 "둘째 가상 화면을 연결하면 굳는다" 는
관계가 없었다.

**데몬 재시작.** `launchctl kickstart` 는 SIP 에 막히고 `sudo kill` 은 되지만, 새로 뜬
데몬이 같은 상태로 즉시 되돌아갔다. 상태가 데몬 밖에 있었기 때문이다.

## 원인

macOS 는 디스플레이를, 가상이든 실물이든, ColorSync 장치로 등록하고 팩토리 프로파일을
만든다. **디스플레이를 없애도 등록은 남는다.**

등록부는 파일 하나다.

```
/Library/Caches/ColorSync/com.apple.colorsync.devices     ----------  root  admin
```

이 머신에서 그 파일은 447KB 였고, 등록된 디스플레이 장치가 **170개**, 그중 프로파일
파일이 이미 사라진 죽은 항목이 **149개**였다. 전부 이 프로젝트가 만들었다 지운
더미다. 개발 중 셀프테스트와 CLI 가 매번 임의의 serial 로 더미를 만들었기 때문이다.

데몬은 요청마다 이 목록을 훑는다. 목록이 그만큼 자라자 한 번의 조회가 끝나기 전에 다음
조회가 들어오는 상태가 됐다.

## 해결

등록 해제 API 는 듣지 않는다. `ColorSyncDeviceUnregister` 는 공개 API 지만 AnyUser
범위 항목을 거부한다. root 로 실행해도 마찬가지고, 로그가 이유를 말한다. 이 머신에서
성공한 적이 한 번도 없다.

```
ColorSyncXPCDeviceRegistryUtilsSetAnyUserInfo - connection not authorized
```

그래서 등록부 파일 자체를 치웠다. `Caches` 아래라 데몬이 현재 붙어 있는 디스플레이로
다시 만든다.

```
sudo mv /Library/Caches/ColorSync/com.apple.colorsync.devices \
        /Library/Caches/ColorSync/com.apple.colorsync.devices.bak
sudo pkill -9 -f colorsync
```

직후 상태다.

| | 조치 전 | 조치 후 |
|---|---|---|
| 등록된 디스플레이 | 170 | 3 |
| 죽은 항목 | 149 | 0 |
| `XPC_DISPLAY_INFO_REQUEST` | 61 건 / 10초 | 0 |
| 두 데몬 CPU | 63% + 38% | 0% + 0% |
| 등록부 크기 | 447 KB | 9 KB |

그 뒤 더미를 하나 켜고, 둘 켜고, 앱으로 복원까지 해봤지만 모두 0 건 / 0% 였다.

사용자 선택(모니터별 색상 프로파일 지정 등)은 이 파일이 아니라 사용자 기본 설정에
있어서 날아가지 않는다.

## 다시 쌓이지 않게 하기

**serial 을 고정한다.** 임의 serial 은 실행할 때마다 새 장치를 만든다. 개발 도구와
셀프테스트가 정의마다 하나의 고정 serial 을 쓴다 (`ToolSerials`,
`SelfTest.TEST_SERIAL`). 앱이 사용자 대신 만드는 더미는 serial 을 설정에 저장해 다시
쓴다.

**다만 완전히 막지는 못한다.** 같은 serial 이라도 디스플레이 UUID 의 뒷부분이 세션마다
달라진다.

```
MewDisplay 1-7101580B-4E86-A402-9DE9-0966D8C1CFC8.icc
MewDisplay 1-7101580B-4E86-A402-C90B-A2661D91F570.icc
```

앞부분은 serial 에서 나와 같고 뒤가 다르다. 관찰된 꼬리 값은 몇 개뿐이라 세션마다 하나
늘어나는 정도로 그치지만, 0 은 아니다. 왜 달라지는지는 아직 모른다.

## 색상 프로파일에 대해

**모니터마다 프로파일이 생기는 것은 정상이다.** macOS 가 등록 시 만든다. 앱이 만드는
것이 아니고 막을 수도 없다.

생성 자체는 못 막지만 **어느 프로파일을 쓸지는 지정할 수 있다.** 공개
`ColorSyncDeviceSetCustomProfiles` 가 그 일을 하고, 지정하면 장치 기록에
`CustomProfiles` 로 남아 팩토리 프로파일보다 우선한다.

```
CustomProfiles = { 1 = "file:///System/Library/ColorSync/Profiles/sRGB Profile.icc" }
FactoryProfiles = { 1 = { DeviceProfileURL = ".../Displays/MewDisplay 1-<UUID>.icc" } }
```

색이 물빠져 보인다면 실제 모니터에 범용 sRGB 가 지정되어 자기 팩토리 프로파일이 밀린
경우를 먼저 본다. 시스템 설정 → 디스플레이 → 색상 프로파일에서 모니터 이름이 붙은
쪽을 고르면 된다.

## 도구

`tools/colorsync/` 에 있다. 이 저장소는 그 디렉터리를 배포하지 않는다. 한 머신의 상태를
들여다보는 일회성 스크립트라 빌드의 일부가 아니다.

```
swiftc -O list-display-devices.swift -o list-display-devices
swiftc -O remove-display-devices.swift -o remove-display-devices

./list-display-devices                        # 등록된 디스플레이 장치 전부
./list-display-devices | grep -c 'exists=false'   # 죽은 항목 수
./remove-display-devices "MewDummy" "Self test"   # 이름으로 골라 dry run
```

`remove-display-devices --apply` 는 이 머신에서 한 번도 성공하지 못했다. 149 개를
지우려 했을 때도, 남은 2 개를 지우려 했을 때도, root 로 실행했을 때도 전부 실패했다.
등록 해제는 목록을 확인하는 용도로만 믿고, 실제로 비우는 것은 등록부 파일을 치우는
쪽으로 한다.

프로파일 파일은 별개다. 등록부를 비워도 지워지지 않으니 직접 지운다. 실제 모니터의
것은 남긴다.

```
cd /Library/ColorSync/Profiles/Displays
sudo rm -f MewDummy*.icc "Self test-"*.icc "Test 1-"*.icc "Test 2-"*.icc
```

## 남은 의문

- 등록부가 커지면 데몬이 왜 순환에 빠지는지, 그 안쪽은 관찰할 수 없었다. 149 개라는
  숫자와 증상의 상관은 확인했지만 임계값은 모른다.
- 같은 serial 의 디스플레이 UUID 꼬리가 세션마다 바뀌는 이유.
- 이 머신의 실제 모니터와 더미 양쪽에 범용 sRGB 가 `CustomProfiles` 로 지정되어 있는데,
  누가 언제 지정했는지 확인하지 못했다.
