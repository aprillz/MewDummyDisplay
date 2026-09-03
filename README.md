# MewDummyDisplay

macOS용 가상 더미 디스플레이 유틸리티. [BetterDummy](https://github.com/waydabber/BetterDummy)
1.0.11을 참조 자료로 삼아 .NET / [MewUI](https://github.com/) 기반으로 새로 만든다.

macOS는 4K 미만 디스플레이에 HiDPI("Retina") 모드를 제공하지 않는다.
소프트웨어 가상 디스플레이를 만들고 실제 모니터를 거기에 미러링하면
원하는 HiDPI 해상도를 쓸 수 있다. 이 앱은 그 가상 디스플레이를 관리한다.

## 상태

**설계 단계.** 아직 구현 코드가 없다.
계획은 [agent/better-dummy-porting/plan.md](agent/better-dummy-porting/plan.md).

## 요구 사항

- macOS 13.0 이상
- Apple Silicon 또는 Intel

## 저장소 구조

| 경로 | 내용 |
|---|---|
| `src/` | 애플리케이션 소스 |
| `reference/` | 참조 자료 (읽기 전용). [출처](reference/PROVENANCE.md) |
| `agent/` | 설계·계획 문서. [문서 안내](agent/better-dummy-porting/README.md) |

## 라이선스

MIT. 참조 원본 BetterDummy도 MIT이며, 고지는 [NOTICE](NOTICE)에 있다.
검토 내용은 [agent/better-dummy-porting/license.md](agent/better-dummy-porting/license.md).
