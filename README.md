# TheMushroomHunt
스타크래프트 유즈맵 '뒷산에서 버섯 캐기'를 모티브로 한 3D 방치형 성장 RPG 게임입니다.

## 개발 환경
- Unity: `6000.3.6f1`
- Render Pipeline: `URP`
- 운영 환경: `Windows`
- 주요 패키지:
  - `com.unity.render-pipelines.universal`: `17.3.0`
  - `com.unity.ai.navigation`: `2.0.9`
  - `com.unity.cinemachine`: `3.1.6`
  - `com.unity.inputsystem`: `1.18.0`
  - `com.unity.ugui`: `2.0.0`
- Github Desktop
- Codex + ChatGPT 5.4
## 사용 에셋
이 프로젝트를 클론하기 위해선 아래의 에셋을 임포트해야 합니다. 사용된 에셋은 모두 무료이며, 상업적으로 이용 가능합니다.
### 환경
- [Kenney - Nature Kit](https://kenney.nl/assets/nature-kit)
  - 위치: \Assets\ThirdParty\kenney_nature-kit
  - 용도: 전반적인 맵 디자인
- [MatrixRex - Uber-Stylized-Water](https://github.com/MatrixRex/Uber-Stylized-Water)
  - 위치: \Assets
  - 용도: 특정 환경 요소(하늘, 물)
### 캐릭터 / 상호작용
- [GanzSe FREE Modular Character - Fantasy Low Poly Pack](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/ganzse-free-modular-character-fantasy-low-poly-pack-321521)
  - 위치: \Assets\ThirdParty\Free Low Poly Modular Character Pack - Fantasy Dream
  - 용도: 플레이어 캐릭터 모델
- [Kyle's Mushroom Pack (FREE)](https://assetstore.unity.com/packages/3d/vegetation/kyle-s-mushroom-pack-free-pack-357838)
  - 위치: \Assets\ThirdParty\Kyle's Mushroom Pack (FREE)
  - 용도: 버섯 원본 모델 / 프리팹
- [Tool Set - PolyPack](https://alstrainfinite.itch.io/tool-set)
  - 위치: \Assets\ThirdParty\Alstra Infinite - Tool Set
  - 용도: 플레이어 장착용 채집 도구 모델
### UI
- [Kenney - UI Pack Adventure](https://kenney.nl/assets/ui-pack-adventure)
  - 위치: \Assets\ThirdParty\kenney_ui-pack-adventure
  - 용도: HUD / 업그레이드 패널 UI 프레임 및 버튼
### 애니메이션
- Mixamo FBX
### 사운드
- Music by <a href="https://pixabay.com/users/geoffharvey-9096471/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=386992">Geoff Harvey</a> from <a href="https://pixabay.com/music//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=386992">Pixabay</a>
  - 위치: \Assets\ThirdParty\Sounds\BGM_TitleScene.mp3
  - 용도: 타이틀 씬에서 재생
- Music by <a href="https://pixabay.com/users/music_for_video-22579021/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=115485">music_for_video</a> from <a href="https://pixabay.com/music//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=115485">Pixabay</a>
  - 위치: \Assets\ThirdParty\Sounds\BGM_MainScene1.mp3
  - 용도: 메인 씬에서 재생
- Music by <a href="https://pixabay.com/users/cyberwave-orchestra-23801316/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=248801">Cyberwave Orchestra</a> from <a href="https://pixabay.com//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=248801">Pixabay</a>
  - 위치: \Assets\ThirdParty\Sounds\BGM_Guard.mp3
  - 용도: 경비원 스폰 시 재생
### 폰트
- [배달의 민족 주아체](http://font.woowahan.com/jua/)
  - 위치: \Assets\ThirdParty\Fonts
  - 용도: 게임 전반 폰트 디자인


## 에셋 임포트/세팅 참고사항
### GanzSe FREE Modular Character - Fantasy Low Poly Pack
`GanzSe FREE Modular Character - Fantasy Low Poly Pack` 디렉토리를 \Assets\ThirdParty 아래로 이동합니다.

URP 프로젝트에서 머터리얼이 핑크색으로 보일 경우 다음을 확인해야 합니다.
- `Base Palette Material`이 Built-in shader를 보고 있는지 확인
- 가능하면 포함된 URP용 머터리얼을 사용
- 또는 Shader를 `Universal Render Pipeline/Lit`으로 변경
### MatrixRex - Uber-Stylized-Water
[링크](https://github.com/MatrixRex/Uber-Stylized-Water)에서 `Uber.Stylized.Water.v1.1.1.unitypackage`를 다운로드받아 임포트합니다.
억지로 해당 패키지를 \Asset\ThirdParty 아래로 옮길 경우 셰이더 오류가 발생할 수 있습니다.
### \Asset\ThirdParty 예시 이미지
<img width="366" height="146" alt="image" src="https://github.com/user-attachments/assets/8e74abff-1045-4e71-b99c-2198dbd17b3f" />
