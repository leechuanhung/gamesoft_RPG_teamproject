# 지나가던 선비 (Unity 2022.3.62f1, 2D 횡스크롤)

과거 시험을 보러 가는 선비의 모험. 작업 브랜치는 `seonbi`, 기본 브랜치는 `main`. 사용자와는 한국어로 대화한다.
게임 규칙과 개발 기록은 `시작하기.md`에 있다. 앞으로 만들 맵·스토리·아이템 구상은 `기획서.md`에 있다. 기능을 바꾸면 맨 위에 짧은 항목을 추가한다.
그림 작업은 `Tools/ArtGen/README.md`를 먼저 읽는다.

## 작업 방식

- 게임 속 캐릭터와 기물은 기존 디자인, 화풍, 크기를 반드시 유지한다. 새 그림은 `Tools/ArtGen/sheet.py`로 기존 캐릭터 옆에 놓고 확인한 뒤 넣는다.
- 플레이하지 않는 연출(콩밭 마을의 하룻밤)과 대화 초상화는 예외다. 사용자가 화풍을 맞출 필요 없이 고퀄리티 픽셀 아트로 하라고 했다(`hqlib.py`). 콩쥐·팥쥐 초상화는 사용자가 직접 만든 시트를 잘라 쓴다(`make_portraits.py`, `source/kongpat_portraits.webp`). 새로 그리지 않는다.
- 기능을 바꾸면 검사(`bash Tools/run_checks.sh`)를 돌린다. 사용자가 직접 플레이해 보고 커밋을 정하는 경우가 많으므로, 커밋은 요청이 있을 때 한다.
- 정리(리팩토링)는 동작을 바꾸지 않는다. 그림은 `make_all.py --check`로, 장면은 캡처 비교로 같음을 확인한다.

## 구조

맵(왼쪽→오른쪽, `PrototypeMaps`가 맵마다 카메라를 가두고 끝에서 어둠 전환): 비석 터(x<−40) ← 망령의 들판(−40~−12) ← 마을(−12~28, 시작 x=0) → 보스 숲(28~64) → 나루터(64~110, 강 80~100) → 콩밭 마을(110~150) → 고개와 서낭당(150~198, 계단 지형 `PassSteps`, 금빛 표식 x=195).
장면은 `Assets/Scenes/MovementTest.unity` 하나. `PrototypeView.Start`가 `PrototypeWorld`(지형·기물·배경·발판)와 `PrototypeCombat`을 런타임에 만든다.

| 파일 | 맡은 일 |
|---|---|
| `PrototypeCombat.cs` | 능력치 상수(선비 체력 100·공격력 10, 도깨비 200·20, 일반 몬스터 30·10, 피해=공격력−방어력, 최소 1), 선비 체력·공격·피격(`PlayerHurtAt`), 재시작(`Restart`), `Paused`, 아래 관리 컴포넌트를 붙인다 (모두 같은 오브젝트) |
| `PrototypeCombat.Boss.cs` | 보스 상태·추격·근접 공격·돌진·도약(`BossMove`), 토벌(`DamageEnemy`)·정화(`TryPurifyBoss`) |
| `PrototypeCombat.Arrows.cs` | 활 조준(`AimAngle` 30°, `ArrowLaunch`, `ArrowDirection`), 발사(`Shoot`), 비행·명중, 발판 통과(`Wall`) |
| `PrototypeQuest.cs` / `QuestProgress.cs` | 노인·비석 대화, 부적 퍼즐 / 선악·퀘스트 상태(Unity 무관). 토벌 +10, 정화 +30, 노인 사망 −40 |
| `PrototypeCamp.cs` | 유령: 2차원 비행 추격(`GhostHealth` 4, `FlySpeed`). 들판 유령(`Initialize`)은 전부 쓰러지면 봉인 해제, 나루터 물귀신(`InitializeRiver`, `combat.River`)은 맵에 들어올 때마다 되살아나고 10마리째에 은비녀 |
| `PrototypeVillage.cs` | 보스가 마을에 들면 경보(`Alarmed`), 주모·보부상 대피, 보스가 쓰러지면 귀가(노인이 죽었으면 먼저 애도) |
| `PrototypeTavern.cs`, `PrototypePeddler.cs`, `TavernWallet.cs` | 주막·보부상 상점, 가격 규칙(`ShopRules`) |
| `PrototypeIntro.cs`, `PrototypeEnding.cs` ← `PrototypeNarration.cs` | 인트로·결말 낭독(검은 화면에 한 줄씩, 스페이스 길게 눌러 건너뛰기) |
| `PrototypeMaps.cs` | 맵 일곱 개, 카메라 가두기(`ClampView`), 경계에서 어둠 전환과 맵 이름 |
| `PrototypeKongVillage.cs`, `VillagerVisual.cs` | 콩밭 마을: 콩쥐·팥쥐·아낙·나무꾼, 괴롭힘 장면(`Scenes`, 아침 뒤 `MorningScenes`), 대화(`Lines`, 콩쥐·팥쥐는 초상화가 창 위로 올라오는 `TalkWindow`), 하룻밤 뒤(`Stayed`)에 열리는 단서 3개와 정체 드러남(`Revealed`). 팥쥐는 콩쥐의 강요로 남들 앞에서 반말과 막말을 한다. 뒷산 보스전은 아직 없다(기획서 1-1) |
| `PrototypeKongStay.cs` | 저녁·밤·아침 자동 연출(`Now`, `Show`). 세계 밖 x=400에 640×360 그림(`18_Night`)을 겹쳐 쌓아(`Put`) 움직인다. 노을 외경 → 콩쥐 눈 클로즈업(`stare`) → 달빛 외경 → 손님방 → 문 클로즈업(구멍과 눈) → 새벽 외경 → 아침 방 → 콩쥐의 만류와 선택지(`Parting`, `Choose`: 정중히 거절/대놓고 의심) → 뒷산 부탁. 카메라 `PrototypeView.FixedX`, `FitSize`. 끝나면 `Kong.WakeUp(Blunt)` |
| `PrototypeLore.cs` | 길가의 조사 지점(`Spots`, E). 콩밭 마을의 것은 마을 사람들 말과 어긋나는 단서다. 전투 중에는 나오지 않는다 |
| `JinaFx.cs` | 초승달 칼빛(`Arc`), 타격 별(`Hit`), 먼지(`Dust`), 충격파(`Shock`), 잔상(`AfterImage`), 숫자(`Number`), 히트스톱(`Stop`). 게임 상태와 무관한 풀링 스프라이트 |
| `PrototypeAmbience.cs` | 맵별 구름·먼지·반딧불·연기·강 물빛, 화면 비네트 |
| `JinaUI.cs` | UI 공통 모양(한지 창 `Window`, HUD 판 `HudPanel`, 글자 `Text`·`Height`, 타자 효과 `Visible`·`VisibleAuto`·`FinishTyping`, `Button`, 판매 칸 `Good`, 쌓이는 알림 `Toast`, 행동 안내 `Prompt`). UI 좌표는 화면 높이 720 기준의 레이아웃 단위다(`Scale`, `W`, `H`, `FromWorld`). `Screen.width` 대신 `JinaUI.W`를 쓰고, `GUI.Label` 대신 `JinaUI.Text`를 쓴다(그래야 글자가 또렷하다). 새 UI는 고정 높이 대신 `Height`로 재서 쌓고, 알림은 `GUI.Box` 대신 `Toast`·`Prompt`를 쓴다 |
| `PrototypeHud.cs` | 상시 화면: 상태·할 일(`Goal`)·조작 안내(H), 선악, 보스 막대, 길 안내. 연출 중에는 숨는다 |
| `PrototypePass.cs` | 고개: 굴러오는 바위(`Step`), 서낭당 회복(`UseShrine`). 지형 높이는 `PrototypeWorld.Top` |
| `PrototypeFerry.cs` | 숲 끝 가시덤불(도깨비 해결 전까지 나루터를 막음, `ThornsShut`), 뱃사공 대화(`Lines`), 나룻배(`PrototypeBoat`), 강에 빠짐(`Splash`) |
| `JinaItems.cs`, `InventoryUI.cs` | 인벤토리 아이템(`JinaItem.Use`: 회복·화살·액막이·짚신), 아이콘 칸 인벤토리 화면(인벤토리 본체는 팀원 담당) |
| `PrototypeGameOver.cs` | 게임 오버: 검은 화면 위 선비 혼자 무너지는 연출(`player_ko_*`, `player_facedown`), 문구 |
| `PrototypeEncounter.cs` | 보스 첫 조우 연출(띠, 카메라 `PrototypeView.Focus`, 포효, 이름 카드) |
| `PrototypeChatter.cs`, `PrototypeBossTalk.cs`, `BossRemorse.cs` | 주모·보부상 잡담, 정화된 도깨비의 사연(E), 노인을 해친 도깨비의 죄책감 |
| `PrototypeWorld.cs`, `PrototypeParallax.cs` | 땅·기물·배경 배치, 발판(`CampLedges`·`ForestLedges`, 한 방향 `PlatformEffector2D`, 레이어 8), 원경 패럴랙스(±2칸) |
| `JinaArt.cs` | 그림 로딩. `Load`(캐릭터, 경로별 발 위치·배율), `CutFrom`(대기 그림에서 잘라 낸 프레임), `Prop`(기물) |
| `GhostVisual.cs` | 유령·물귀신(색만 다름) 공격(`Winding`, `Strike`)·쓰러짐 프레임 |
| `SeonbiVisual.cs` | 선비 프레임(걷기·달리기·점프·공격·조준·쓰러짐) |
| `JinaActorVisual.cs` ← `BossVisual`, `ElderVisual`, `PeddlerVisual`, `GhostVisual` | 원래 사각형 위에 그림을 얹는 공통 바탕. 하위 클래스가 `Pose`에서 이번 `Frame`을 고친다 |
| `KeeperVisual.cs` | 주모(기물 스프라이트)의 프레임 |

- `Paused`는 인트로·결말·조우·대화·상점·콩밭 하룻밤 연출 중에 true다. 그동안 보스와 몬스터가 멈추고 `SeonbiVisual`이 자세를 초기화한다.
- 연출은 시작 시각으로 재생한다: `BossDownAt`, `PurifiedAt`, `PlayerDownAt`, `Quest.ElderStruckAt`, `Village.AlarmAt`·`ClearAt`, `GhostVisual.DownAt`.
- 쓰러져 기우는 그림(노인, 선비)은 부모 없이 둔다. 크기가 비균일한 사각형 아래에서 돌리면 찌그러진다. `JinaArt.Place`는 그림의 실제 부모 크기를 되돌린다.
- 대화 중인 인물은 선비 쪽을 본다(`JinaActorVisual.native`: 보부상 −1, 나머지 +1). 노인 시체는 맞은 순간의 방향으로 고정된다.
- 칼은 3연격이다(`comboStep`, `SeonbiVisual.SwordLength`·`SwordHit`, `SwordFeel`). 첫 칼은 기존 네 프레임 그대로, 둘째는 빠르게 올려 베기, 셋째는 무거운 내려치기. 피해는 모두 `SwordDamage`로 같다.
- UI는 전부 IMGUI(`OnGUI`)다. 모든 `OnGUI`는 처음에 `PrototypeKoreanUI.Apply()`를 불러 배율을 맞춘다. 글꼴도 여기서 정한다(맑은 고딕). 인물 대사는 `SpeechBubble.Draw`, 화면 밖 인물은 말하지 않는다(`SpeechBubble.OnScreen`). 한글이 들어간 새 UI 파일은 `KoreanUIChecks`의 목록에 넣는다.

## 그림 (요약, 자세한 것은 Tools/ArtGen/README.md)

- 원본 V6 캐릭터(`Resources/JinaV4/01~10`: 선비, 보스, 노인, 유령)는 새로 그리지 않는다. 기존 프레임을 잘라 붙이고 변형한다(`framelib.py`). 예외로 선비는 사용자 요청에 따라 20대 초반으로 얼굴만 고쳤다(수염 없음, `make_young.py`가 V6 프레임에 덮어 쓰고 달리기·피격·조준 프레임은 그 뒤에 만든다).
- 새로 그린 그림(보부상, 주모, 기물, 발판, 배경)은 `pixlib.py`로 논리 도트 격자에 그린다. 1칸 검은 외곽선, 평면 채색과 한 단계 음영. 배경만 외곽선 없이 하늘색에 가까운 실루엣.
- 연출 그림과 초상화는 `hqlib.py`로 그린다: 재질별 색 단계, 빛, 디더링, 장면별 팔레트. 땅·하늘·이펙트·UI 재료는 `make_ground.py`, `make_fx.py`, `make_ui.py`.
- 소리는 다른 팀원 담당이다. 게임에 소리 코드나 파일을 넣지 않는다.
- 모든 스크립트는 커밋된 그림과 소리를 똑같이 다시 만든다. 스크립트를 고쳐서 다시 생성한다: `python Tools/ArtGen/make_all.py --check`.

## 검사

- `bash Tools/run_checks.sh` 하나로 돌린다(`--art`: 그림 재생성 확인, `--keep`: `validation/` 장면 캡처를 남김. 확인한 뒤 지운다).
  - 규칙 검사 `PlayerArtChecks.Run`(Tavern → Morality → World → Arrow → Quest → KoreanUI)과 플레이 검사 `JinaPlayChecks.Begin`을 차례로 실행한다. 오래 걸리므로 백그라운드나 긴 시간 제한으로 돌린다.
  - Unity 에디터가 열려 있으면 배치 검사를 할 수 없어, 스크립트가 게임·에디터 코드 컴파일만 확인한다. 전체 검사가 필요하면 사용자에게 에디터를 닫아 달라고 한다.
- IMGUI(창, 말풍선, 띠, 이름 카드, 결말 문장)는 플레이 검사의 캡처에 찍히지 않는다. UI를 바꾸면 `bash Tools/run_ui_shots.sh`로 실제 빌드의 스크린샷을 찍어 `validation/ui/`에서 확인하고 지운다(프로젝트 복사본에서 돌아서 에디터가 열려 있어도 된다. 새 UI 상태는 `Assets/Scripts/UiShots.cs`에 추가).
- `JinaPlayChecks`는 partial 클래스다: `JinaPlayChecks.cs`(실행 순서·헬퍼)와 `.Player`·`.Boss`·`.Camp`·`.Elder`·`.Village`·`.Story`·`.Terrain`·`.Ending`·`.Items`(아이템, 맵, 나루터, 고개, 콩밭 마을).
  - 비공개 필드는 `Field`/`Set`(자동 속성은 `<Name>k__BackingField`). 컴포넌트 하나의 `Update`는 `Call(component)`로 돌린다. `SendMessage`는 같은 오브젝트의 관리 컴포넌트 전부를 실행한다.
  - 배치 모드의 `Time.deltaTime`은 0.0001초 수준이다. 이동은 방향만 검사하고, 연출 끝 시점은 길이보다 0.01초 더 지난 시각으로 검사한다.
- Unity 경로: `C:/Program Files/Unity/Hub/Editor/2022.3.62f1/Editor/Unity.exe`.
