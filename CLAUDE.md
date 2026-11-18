# 지나가던 선비 (Unity 2022.3.62f1, 2D 횡스크롤)

과거 시험을 보러 가는 선비의 모험. 작업 브랜치는 `seonbi`, 기본 브랜치는 `main`. 사용자와는 한국어로 대화한다.
게임 규칙과 개발 기록은 `시작하기.md`에 있다. 앞으로 만들 맵·스토리·아이템 구상은 `기획서.md`에 있다. 기능을 바꾸면 맨 위에 짧은 항목을 추가한다.
그림 작업은 `Tools/ArtGen/README.md`를 먼저 읽는다.

## 작업 방식

- 스크립트는 역할별로 한 파일에 여러 클래스를 둔다(예: `ActorVisuals.cs`, `RoadEvents.cs`, NPC는 `MainNpcs.cs`·`Townsfolk.cs`·`Shops.cs`). 모두 실행 중에 `AddComponent`로 붙으므로 파일 이름과 클래스 이름이 달라도 된다. 장면에 직접 붙은 `View`·`Player`와 구운 세계에 저장되는 `WorldBuilder`·`WorldArt`·`ParallaxLayer`는 파일 이름과 클래스 이름이 같아야 하고(아니면 빌드된 게임이 장면 데이터를 잘못 읽어 멈춘다), 파일을 옮길 때 `.meta`도 같이 옮긴다(장면은 GUID로 찾는다). 이름 앞에 `Prototype`을 붙이지 않는다(2026-11-17에 모두 뗐다). 주석은 한글로 쓴다.

- 게임 속 캐릭터와 기물은 기존 디자인, 화풍, 크기를 반드시 유지한다. 새 그림은 `Tools/ArtGen/sheet.py`로 기존 캐릭터 옆에 놓고 확인한 뒤 넣는다.
- 플레이하지 않는 연출(콩밭 마을의 하룻밤)과 대화 초상화는 예외다. 화풍을 맞추지 않는다. 하룻밤 연출 그림은 사용자가 GPT로 만든 것을 잘라 쓴다(`make_night.py`, `source/night/`). 콩쥐·팥쥐·노인 초상화는 사용자가 직접 만든 그림을 잘라 쓴다(`make_portraits.py`, `source/kongpat_portraits.webp`, `source/elder_portrait.webp`). 새로 그리지 않는다.
- 기능을 바꾸면 검사(`bash Tools/run_checks.sh`)를 돌린다. 사용자가 직접 플레이해 보고 커밋을 정하는 경우가 많으므로, 커밋은 요청이 있을 때 한다.
- 정리(리팩토링)는 동작을 바꾸지 않는다. 그림은 `make_all.py --check`로, 장면은 캡처 비교로 같음을 확인한다.

## 구조

맵(왼쪽→오른쪽, `Maps`가 맵마다 카메라를 가두고 끝에서 어둠 전환): 비석 터(x<−40) ← 망령의 들판(−40~−12) ← 마을(−12~28, 시작 x=0) → 보스 숲(28~64) → 나루터(64~110, 강 80~100) → 콩밭 마을(110~150) → 고개와 서낭당(150~198, 계단 지형 `PassSteps`) → 뒷산(198~246, 콩쥐 요괴, 높낮이 `HillSteps`·높은 발판 `HillLedges`·엄폐 독 `HillCrocks`) → 봉화 산성(246~292, 퍼즐, `FortSteps`) → 버려진 역참(292~338) → 북망산 무덤길(338~384, `GraveSteps`) → 한양 거리(384~430) → 과거장 앞(430~470) → 좌의정 댁(470~520, 최종 보스, 금빛 표식 `GoalX`=517).
장면은 `Assets/Scenes/MovementTest.unity` 하나. `View.Start`가 `WorldBuilder.Ensure`(씬에 구운 세계가 있으면 그것, 없으면 지형·기물·배경·발판을 코드로 지음)와 `Combat`을 런타임에 만든다.
세계는 장면에 구워 두었다(`세계 (씬에 구운 것)`, 에디터 메뉴 `지나가던 선비 > 세계를 씬에 굽기`, `Editor/WorldBake.cs`). 무리: `기물`·`배경`·`발판`은 씬에서 옮겨도 되고, `땅과 하늘`은 지형에 맞춰져 고르지 못하게 잠겨 있다. 코드에서 기물·지형 배치를 바꾸면 `WorldBuilder.Layout`을 올리고 메뉴로 다시 굽는다(씬에서 옮긴 기물과 새로 넣은 것은 남는다). 그림은 씬에 저장되지 않고 `WorldArt`가 이름으로 다시 입힌다(저장할 때 비움). 이름으로 찾는 기물 `잔치 명단`·`주막 주모`·`뒷산 독`의 이름은 바꾸지 않는다. NPC·보스 자리는 상수이고 씬 창에는 이름표(`WorldBuilder.OnDrawGizmos`, `Markers`)로만 보인다.

| 파일 | 맡은 일 |
|---|---|
| `Combat.cs` | 능력치 상수(선비 체력 100·공격력 10, 도깨비 200·20, 일반 몬스터 30·10, 피해=공격력−방어력, 최소 1), 선비 체력·공격·피격(`PlayerHurtAt`), 재시작(`Restart`, 콩쥐를 만난 뒤 죽으면 뒷산 입구 체크포인트 `RestartAtHill`·`Checkpoint`), `Paused`, 아래 관리 컴포넌트를 붙인다 (모두 같은 오브젝트) |
| `Combat.Boss.cs` | 보스 상태·추격·근접 공격·돌진·도약(`BossMove`), 토벌(`DamageEnemy`)·정화(`TryPurifyBoss`) |
| `Skills.cs`, `DashSlashAttack.cs`, `Combat.DashSlash.cs` (팀원) | 스킬 칸 Q 물러서기(무적만, `StartDash(..., slash: false)`)·W 돌진베기. 돌진베기 피해는 돌진하는 동안 `ApplyDashSlash` 한 곳에서만 하나마다 한 번(도깨비·콩쥐 요괴·유령·뒷산의 독) |
| `Combat.Arrows.cs` | 활 조준(`AimAngle` 30°, `ArrowLaunch`, `ArrowDirection`), 발사(`Shoot`), 비행·명중, 발판 통과(`Wall`) |
| `MainNpcs.cs`의 `NpcTalk` | NPC 대화 공통 바탕: 가까이(`Within`), 열기(`Begin`, 선비 멈춤), 엔터로 넘기기·Esc로 닫기(`TalkKeys`, `Next`, `Close`), 말풍선 안내(`PageHint`), 대사 `lines`·`Page`·`IsOpen`. 노인·콩밭 마을·정화된 도깨비·뱃사공·주막·보부상이 이 위에 있다. 닫을 때 할 일은 `Close`를 덮어 쓴다 |
| `MainNpcs.cs`의 `ElderQuest` / `QuestProgress.cs` | 노인 대화(초상화 창 `TalkWindow`, 콩쥐와 싸우다 돌아오면 `ElderPages(fleeing)`)·비석, 부적 퍼즐 / 선악·퀘스트 상태(Unity 무관). 토벌 +10, 정화 +30, 노인 사망 −40, 콩쥐 토벌 +10·정화 +50(`KongOutcome`), 콩밭 마을 사람 사망 −10(`VillagersLost`) |
| `Camp.cs` | 유령: 2차원 비행 추격(`GhostHealth` 4, `FlySpeed`). 이름(`Name`: 망령·물귀신·떠도는 망령)과 처치 알림(`FelledNote` → `Combat.FindCoins`). 들판 유령(`Initialize`)은 전부 쓰러지면 봉인 해제, 나루터 물귀신(`InitializeRiver`, `combat.River`)은 맵에 들어올 때마다 되살아나고 10마리째에 은비녀 |
| `Townsfolk.cs`의 `VillageAlarm` | 보스가 마을에 들면 경보(`Alarmed`), 주모·보부상 대피, 보스가 쓰러지면 귀가(노인이 죽었으면 먼저 애도) |
| `Shops.cs` | 주막(`TavernShop`)·보부상 상점(`PeddlerShop`), 돈과 가격 규칙(`TavernWallet`, `ShopRules`). 가게는 여러 곳(`ShopStall`, `Stalls`): 음식은 첫 마을 주막·한양 국밥집, 물건은 첫 마을 보부상·콩밭 마을 방물장수(싸움 중 피란)·한양 잡화전. 상중 규칙은 첫 마을 가게에서만(`Mourning`) |
| `Townsfolk.cs`의 `ShopChatter` | 주모·보부상 잡담 |
| `Narration.cs` (`IntroNarration`, `EndingNarration` ← `Narration`) | 인트로·결말 낭독(검은 화면에 한 줄씩, 스페이스 길게 눌러 건너뛰기) |
| `Maps.cs` | 맵 일곱 개, 카메라 가두기(`ClampView`), 경계에서 어둠 전환과 맵 이름 |
| `MainNpcs.cs`의 `KongVillage` (그림은 `ActorVisuals.cs`의 `VillagerVisual`) | 콩밭 마을: 콩쥐·팥쥐·아낙·나무꾼, 괴롭힘 장면(`Scenes`, 아침 뒤 `MorningScenes`), 대화(`Lines`, 콩쥐·팥쥐는 창 안 왼쪽에 초상화가 들어가는 `TalkWindow`), 넷을 모두 만난 뒤(`Met`, `ReadyForNight`) 다시 말을 걸면 콩쥐가 하룻밤을 청함, 팥쥐의 은비녀 이야기는 두 번째 만남부터 한 번만(`KeepsakeTold`), 하룻밤 뒤(`Stayed`)에 열리는 단서 3개와 정체 드러남(`Revealed`). 팥쥐는 콩쥐의 강요로 남들 앞에서 반말과 막말을 한다. 말만 하는 훈장·엿장수·할멈(`TownLines`). 요괴가 마을에 들면 모두 묶이고(`StartRaid`, `Fate`) 팥쥐는 맨 왼쪽으로 달아나며, 오른쪽부터 죽는다(`NextVictim`, `Kill`). 싸움 뒤 대사 `AfterLines`. 콩쥐는 싸우기 전 선비가 고개에 있으면 마을에 없다(`KongBoss.AtHome`) |
| `KongBoss.cs`, `.Fight.cs` | 콩쥐 요괴: 뒷산에서 기다림 → 초상화 대화(`MeetingLines`, 마지막 `LastLine` "젓갈로 담가 주마!") → 변신(`Changing`, 띠·카메라·검은 형체·번쩍·이름 카드) → 싸움(`Fighting`). 체력 260, 뛰어서 이동(동적 `Rigidbody2D`, 선비와 충돌 무시, 강 건너 못 감 `MinX`), 젓갈 침·콩알 산탄·소금 비(절반 아래)·혀 채찍(근접 하나), 높은 곳·벽 앞·제자리면 높이 뛰기(`HighHop`). 마을에서는 묶인 사람을 오른쪽부터 죽이고 칼 두 번에 멈칫(`Stagger`). 토벌(`Slain`), 체력 절반 아래에서 은비녀로 정화(`TryPurify`, F 또는 보따리). 해결할 때까지 뒷산 끝을 두꺼비 독기 장막이 막는다(`Sealed`, 싸우다 산성으로 달아나 건너뛰지 못하게) |
| `KongBoss.Crocks.cs` | 뒷산 큰 독(체력이 아닌 기믹): 요괴 탄 3번(`CrockShots`) 또는 선비의 타격 2번(`CrockBlows`, 칼·화살·돌진베기 무엇이든 피해와 상관없이 한 대)에 깨지고 꿀단지를 떨굼, 닿으면 체력 +20(`HoneyHeal`, 인벤토리 아님) |
| `DevCheats.cs` | 개발자 치트(안내에 없음). F8: 치트 창(`Open`, 게임 멈춤), 숫자키로 1 무적(`God`, `HurtPlayer`가 무시) 2 한 방(`OneHit`, 적이 피해를 받는 곳마다 `Power`를 거침) 3 체력·스테미너·화살 가득 4 엽전 +100 5·6 선악 ±10 7·8 이전·다음 맵 9 이 맵의 적 모두 쓰러뜨리기(`ClearMap`) 0 봉화 산성 풀기. 무적·한 방은 R 뒤에도 남는다. F9: 도깨비 정화 루트 결과(유령 전멸, 노인 이야기, 비석, 부적 정화 +30) 후 나루터 뱃사공 옆으로 이동. F10: 선한 길(선악 +80)로 산성 입구. F11: 악한 길(선악 −60, 관군 3단계)로 역참 입구. F12: 진행 그대로 좌의정 댁 입구로 |
| `RoadMobs.cs` | 한양 가는 길의 일반 몬스터(도깨비불 `Wisp`, 두억시니 `Brute`)와 관군(칼병 `Blade`, 활병 `Bow`). 선악 음수면 일반 몬스터 맵(`SoldierZones`)에 들어설 때 무리를 부른다(`Rank`: −1~−29 1단계, −30~−59 2단계, −60 이하 3단계, `Squad`, `CallSquad`). 좌의정 댁에서는 `CallSquad`(음수)·`CallWitnesses`(0 이상, 물러남)·`Disband`. 물리 없이 `WorldBuilder.Top`을 따라 걷는다. 산성·한양 거리에 들어서면 길 체크포인트(`Combat.SaveOnRoad`) |
| `Beacons.cs`의 `BeaconFort` | 봉화 산성 퍼즐: 군령판(`Orders`), 봉수대 다섯(`All`, 순서 `Order`), 틀리면 모두 꺼짐, 다 오르면 성문(`FortGateX`) 열림 |
| `Hanyang.cs`의 `HanyangTown` | 한양 거리·과거장 앞 사람들(`People`, 말 `Lines`, 선악 음수면 다른 말). 무당의 공수(`HeardProphecy`), 청지기의 초대(`Invited`) |
| `FinalBoss.cs`, `.Fight.cs` | 좌의정 엄탁: 망한 나라의 망령들에게 얼굴을 팔고 그 대가로 길마다 저주를 걸게 해 지방 선비를 막았다. 술상 앞(`MeetRange`)에서 누각 술자리(`BanquetLines`, 선악에 따라 경계하는 까닭, 어린 임금, 망령과의 거래, 마지막 `LastLine`) → 변신(`Changing`, 얼굴이 지워짐 `official_blank`) → 어둑시니 좌의정(`Fighting`, 체력 420, `Sweep`·`Slam`·`Ink`·`Hands`·`Coins`, 절반 아래 `Dark`). 토벌 +10(`QuestProgress.ResolveFinal`), 흩어진 자리에 얼굴 없는 시신(`official_down`) |
| `KongAftermath.cs` | 싸움 뒤 장면(말풍선 큐): 뒷산에서 정화하면 깨어난 콩쥐와 선비(`field`), 마을에서는 오열·팥쥐 시체로 달려가 크게 오열(`wail`)·살아남은 사람들이 콩쥐를 죽이려다 팥쥐와 선비가 말림·자매의 재회(`village`), 토벌 뒤 한마디(`slain`) |
| `KongNightStay.cs` | 저녁·밤·아침 자동 연출(`Now`, `Show`). 세계 밖 x=400에 1280×533(2.4:1, 위아래 검은 띠) 그림(`18_Night`, 128px=1칸)을 겹쳐 쌓아(`Put`, 그림자는 `Shade`) 움직이고, 컷마다 천천히 당겨 찍는다(`CutView`). 노을 외경 → 콩쥐 눈 클로즈업(`stare`, 사용자 그림 `source/kongjwi_stare_cut.webp`) → 달빛 외경 → 손님방 → 문 클로즈업(구멍과 눈) → 새벽 외경 → 아침 방 → 콩쥐의 만류와 선택지(`Parting`, `Choose`: 정중히 거절/대놓고 의심) → 뒷산 부탁. 카메라 `View.FixedX`, `FitSize`. 끝나면 `Kong.WakeUp(Blunt)` |
| `RoadEvents.cs`의 `RoadsideLore` | 길가의 조사 지점(`Spots`, 엔터). 콩밭 마을의 것은 마을 사람들 말과 어긋나는 단서다. 전투 중에는 나오지 않는다 |
| `JinaFx.cs` | 초승달 칼빛(`Arc`), 타격 별(`Hit`), 먼지(`Dust`), 충격파(`Shock`), 잔상(`AfterImage`), 숫자(`Number`), 히트스톱(`Stop`). 게임 상태와 무관한 풀링 스프라이트 |
| `WorldBuilder.cs`의 `MapAmbience` | 맵별 구름·먼지·반딧불·연기·강 물빛, 화면 비네트 |
| `JinaUI.cs` (`JinaUI`, 말풍선 `SpeechBubble`, 배율·글꼴 `KoreanUI`) | UI 공통 모양(한지 창 `Window`, HUD 판 `HudPanel`, 글자 `Text`·`Height`, 타자 효과 `Visible`·`VisibleAuto`·`FinishTyping`, `Button`, 판매 칸 `Good`, 초상화 대화창 `TalkWindow`, 쌓이는 알림 `Toast`, 행동 안내 `Prompt`). UI 좌표는 화면 높이 720 기준의 레이아웃 단위다(`Scale`, `W`, `H`, `FromWorld`). `Screen.width` 대신 `JinaUI.W`를 쓰고, `GUI.Label` 대신 `JinaUI.Text`를 쓴다(그래야 글자가 또렷하다). 새 UI는 고정 높이 대신 `Height`로 재서 쌓고, 알림은 `GUI.Box` 대신 `Toast`·`Prompt`를 쓴다 |
| `Hud.cs` | 상시 화면: 상태·할 일(`Goal`)·조작 안내(H), 선악, 보스 막대, 길 안내. 연출 중에는 숨는다 |
| `RoadEvents.cs`의 `MountainPass` | 고개: 굴러오는 바위(`Step`), 서낭당 회복(`UseShrine`). 지형 높이는 `WorldBuilder.Top` |
| `Townsfolk.cs`의 `Ferry` | 숲 끝 가시덤불(도깨비 해결 전까지 나루터를 막음, `ThornsShut`), 뱃사공 대화(`Lines`), 나룻배(`FerryBoat`), 강에 빠짐(`Splash`) |
| `JinaItems.cs`, `InventoryUI.cs` | 인벤토리 아이템(`JinaItem.Use`: 회복·화살·액막이·짚신), 아이콘 칸 인벤토리 화면(인벤토리 본체는 팀원 담당) |
| `GameOver.cs` | 게임 오버: 검은 화면 위 선비 혼자 무너지는 연출(`player_ko_*`, `player_facedown`), 문구 |
| `BossStory.cs`의 `BossEncounter` | 보스 첫 조우 연출(띠, 카메라 `View.Focus`, 포효, 이름 카드) |
| `BossStory.cs`의 `BossTalk`, `BossRemorse` | 정화된 도깨비의 사연(엔터), 노인을 해친 도깨비의 죄책감 |
| `WorldBuilder.cs` (`WorldBuilder`, `MapAmbience`), `ParallaxLayer.cs` | 땅·기물·배경 배치(땅 충돌체는 높이가 같으면 하나로 합침 `Solids`, 이음매에 선비가 걸리지 않게), 발판(`CampLedges`·`ForestLedges`, 한 방향 `PlatformEffector2D`, 레이어 8), 원경 패럴랙스(±2칸). 무리(`Group`), 구운 세계(`Baked`, `Ensure`, `Layout`), 씬 창 이름표 |
| `WorldArt.cs`, `Editor/WorldBake.cs` | 구운 세계의 그림 이름(`WorldArt.Apply`·`Clear`), 굽기·다시 굽기(옮긴 것 유지)·지우기 메뉴, 저장할 때 그림 비우기, 배치 모드 `WorldBake.BakeGameScene` |
| `JinaArt.cs` | 그림 로딩. `Load`(캐릭터, 경로별 발 위치·배율), `CutFrom`(대기 그림에서 잘라 낸 프레임), `Prop`(기물) |
| `ActorVisuals.cs`의 `GhostVisual` | 유령·물귀신(색만 다름) 공격(`Winding`, `Strike`)·쓰러짐 프레임 |
| `SeonbiVisual.cs` | 선비 프레임(걷기·달리기·점프·공격·조준·쓰러짐) |
| `ActorVisuals.cs` (`JinaActorVisual` ← `BossVisual`, `ElderVisual`, `PeddlerVisual`, `KeeperVisual`, `FerrymanVisual`, `VillagerVisual`, `GhostVisual`, 그리고 `MonsterVisual`) | 원래 사각형 위에 그림을 얹는 공통 바탕. 하위 클래스가 `Pose`에서 이번 `Frame`을 고친다 |

- `Paused`는 인트로·결말·조우·대화·상점·콩밭 하룻밤 연출·콩쥐 요괴와 좌의정의 대화와 변신·한양 사람과의 대화 중에 true다. 그동안 보스와 몬스터가 멈추고 `SeonbiVisual`이 자세를 초기화한다.
- 연출은 시작 시각으로 재생한다: `BossDownAt`, `PurifiedAt`, `PlayerDownAt`, `Quest.ElderStruckAt`, `Village.AlarmAt`·`ClearAt`, `GhostVisual.DownAt`.
- 쓰러져 기우는 그림(노인, 선비)은 부모 없이 둔다. 크기가 비균일한 사각형 아래에서 돌리면 찌그러진다. `JinaArt.Place`는 그림의 실제 부모 크기를 되돌린다.
- 대화 중인 인물은 선비 쪽을 본다(`JinaActorVisual.native`: 보부상 −1, 나머지 +1). 노인 시체는 맞은 순간의 방향으로 고정된다.
- 칼은 3연격이다(`comboStep`, `SeonbiVisual.SwordLength`·`SwordHit`, `SwordFeel`). 첫 칼은 기존 네 프레임 그대로, 둘째는 빠르게 올려 베기, 셋째는 무거운 내려치기. 피해는 모두 `SwordDamage`로 같다.
- UI는 전부 IMGUI(`OnGUI`)다. 모든 `OnGUI`는 처음에 `KoreanUI.Apply()`를 불러 배율을 맞춘다. 글꼴도 여기서 정한다(맑은 고딕). 인물 대사는 `SpeechBubble.Draw`, 화면 밖 인물은 말하지 않는다(`SpeechBubble.OnScreen`). 한글이 들어간 새 UI 파일은 `KoreanUIChecks`의 목록에 넣는다.

## 그림 (요약, 자세한 것은 Tools/ArtGen/README.md)

- 원본 V6 캐릭터(`Resources/JinaV4/01~10`: 선비, 보스, 노인, 유령)는 새로 그리지 않는다. 기존 프레임을 잘라 붙이고 변형한다(`framelib.py`). 예외로 선비는 사용자 요청에 따라 20대 초반으로 얼굴만 고쳤다(수염 없음, `make_young.py`가 V6 프레임에 덮어 쓰고 달리기·피격·조준 프레임은 그 뒤에 만든다).
- 새로 그린 그림(보부상, 주모, 기물, 발판, 배경)은 `pixlib.py`로 논리 도트 격자에 그린다. 1칸 검은 외곽선, 평면 채색과 한 단계 음영. 배경만 외곽선 없이 하늘색에 가까운 실루엣.
- 연출 그림과 초상화는 사용자 그림을 잘라 쓴다(`make_night.py`, `make_portraits.py`). 땅·하늘·이펙트·UI 재료는 `make_ground.py`, `make_fx.py`, `make_ui.py`.
- 소리는 다른 팀원 담당이다. 게임에 소리 코드나 파일을 넣지 않는다.
- 모든 스크립트는 커밋된 그림과 소리를 똑같이 다시 만든다. 스크립트를 고쳐서 다시 생성한다: `python Tools/ArtGen/make_all.py --check`.

## 검사

- `bash Tools/run_checks.sh` 하나로 돌린다(`--art`: 그림 재생성 확인, `--keep`: `validation/` 장면 캡처를 남김. 확인한 뒤 지운다).
  - 규칙 검사 `PlayerArtChecks.Run`(Tavern → Morality → World → Arrow → Quest → KoreanUI)과 플레이 검사 `JinaPlayChecks.Begin`을 차례로 실행한다. 오래 걸리므로 백그라운드나 긴 시간 제한으로 돌린다.
  - Unity 에디터가 열려 있으면 배치 검사를 할 수 없어, 스크립트가 게임·에디터 코드 컴파일만 확인한다. 전체 검사가 필요하면 사용자에게 에디터를 닫아 달라고 한다.
- IMGUI(창, 말풍선, 띠, 이름 카드, 결말 문장)는 플레이 검사의 캡처에 찍히지 않는다. UI를 바꾸면 `bash Tools/run_ui_shots.sh`로 실제 빌드의 스크린샷을 찍어 `validation/ui/`에서 확인하고 지운다(프로젝트 복사본에서 돌아서 에디터가 열려 있어도 된다. 새 UI 상태는 `Assets/Scripts/UiShots.cs`에 추가).
- `JinaPlayChecks`는 partial 클래스다: `JinaPlayChecks.cs`(실행 순서·헬퍼)와 `.Player`·`.Boss`·`.Camp`·`.Elder`·`.Village`·`.Story`·`.Terrain`·`.Ending`·`.Items`(아이템, 맵, 나루터, 고개, 콩밭 마을)·`.KongBoss`(뒷산, 콩쥐 요괴, 마을 습격, 정화 뒤 장면)·`.Hanyang`(한양 가는 길, 관군, 산성 퍼즐, 좌의정, 길 체크포인트, 과거장 결말).
  - 비공개 필드는 `Field`/`Set`(자동 속성은 `<Name>k__BackingField`). 컴포넌트 하나의 `Update`는 `Call(component)`로 돌린다. `SendMessage`는 같은 오브젝트의 관리 컴포넌트 전부를 실행한다.
  - 배치 모드의 `Time.deltaTime`은 0.0001초 수준이다. 이동은 방향만 검사하고, 연출 끝 시점은 길이보다 0.01초 더 지난 시각으로 검사한다.
- Unity 경로: `C:/Program Files/Unity/Hub/Editor/2022.3.62f1/Editor/Unity.exe`.
