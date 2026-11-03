# 지나가던 선비 (Unity 2022.3.62f1, 2D 횡스크롤)

과거 시험을 보러 가는 선비의 모험. 작업 브랜치는 `seonbi`, 기본 브랜치는 `main`. 사용자와는 한국어로 대화한다.
게임 규칙과 개발 기록은 `시작하기.md`에 있다. 기능을 바꾸면 맨 위에 짧은 항목을 추가한다.

## 구조

- 장면은 `Assets/Scenes/MovementTest.unity` 하나. `PrototypeView.Start`가 `PrototypeWorld`(지형·기물)와 `PrototypeCombat`을 런타임에 만든다.
- `PrototypeCombat`이 중심이다. 보스, 화살, 체력, 재시작, `Paused`를 맡고 `Quest`, `Camp`, `Tavern`, `Peddler`, `Village`, `Intro`를 붙인다.
- `PrototypeVillage`는 보스가 마을에 들어오면(`BossInVillage`) 경보를 울리고(`Alarmed`/`AlarmAt`), 주모·보부상을 기와집(`Shelter`)으로 대피시킨다. 보스가 쓰러지면(`Cleared`/`ClearAt`) 걸어서 돌아오게 한다. 대피 중에는 `Tavern.Near`와 `Peddler.Near`가 false다. 이 클래스는 위치만 옮기고, 프레임은 각 인물의 Visual이 고른다.
- 대화 중인 인물은 선비 쪽을 본다. `JinaActorVisual.native`는 그림이 원래 보는 방향이다(보부상 −1, 나머지 +1).
- 인트로(`PrototypeIntro`)와 결말(`PrototypeEnding`)은 `PrototypeNarration`(검은 화면에 한 줄씩, 스페이스 길게 눌러 건너뛰기)을 함께 쓴다. 결말은 보스를 처리하고 `GoalX`에 닿으면 시작되고, R을 누를 때까지 `Active`다.
- 죄책감: 노인이 죽은 뒤 정화하면 `BossRemorse`가 정화 연출 뒤(`Start`)부터 대사를 띄우고, `BossVisual.Grieve`가 무릎 꿇기(`06_Boss_Purified/boss_grieve_00~01`, 눈 감고 입 벌림)와 눈물을 그린다.
- 말풍선은 화면 밖 인물이면 띄우지 않는다(`SpeechBubble.OnScreen`). 애도 대사는 `PrototypeVillage.Villager.speakAt`으로 한 사람씩 이어진다.
- 쓰러짐 시각: 선비 `PrototypeCombat.PlayerDownAt`(이때 `PrototypePlayer.Downed`로 중력만 받는다), 유령 `GhostVisual.DownAt`.
- `Paused`는 인트로·결말·대화·상점 중에 true다. 그동안 보스와 몬스터가 멈추고, `SeonbiVisual`이 자세를 초기화한다.
- 선악과 퀘스트 상태는 `QuestProgress`(Unity에 의존하지 않음)에 있다. 수치: 토벌 +10, 정화 +30, 노인 사망 −40.
- 가격과 판매 규칙은 `TavernWallet.cs`의 `ShopRules`에 있다.
- UI는 전부 IMGUI(`OnGUI`)다. 글꼴은 `PrototypeKoreanUI.Apply()`로 맑은 고딕을 쓴다. 인물의 대사는 `SpeechBubble.Draw`(말풍선), 비석과 쪽지는 기존 상자로 표시한다.
- 그림 로딩은 `JinaArt.Load`(캐릭터)와 `JinaArt.Prop`(기물)이 맡는다. `Load`의 경로별 발 위치·배율은 "공유 캔버스 애니메이션 / 대기 그림에서 잘라 낸 프레임 / 대기 그림 / 무기" 순으로 묶여 있다.
- 화면 표시는 원래 사각형 스프라이트 위에 자식 SpriteRenderer를 얹는 방식이다.
  - `SeonbiVisual`: 플레이어.
  - `JinaActorVisual`: 공통 바탕(프레임 배치, 방향, 피격 색, 섬광). 유령은 이것을 그대로 쓴다. `Attach`가 종류에 맞는 하위 클래스를 붙인다.
  - `BossVisual`(걷기·공격·죽음·정화·죄책감), `ElderVisual`(대화 시선·경보·칼·쓰러짐·시체), `PeddlerVisual`(달리기·시선), `GhostVisual`(죽음): 각자 `Pose`에서 이번 프레임(`Frame`)을 고친다.
  - 쓰러져 기우는 그림(노인, 선비의 몸과 무기)은 부모 없이 둔다. 크기가 비균일한 사각형 아래에서 돌리면 찌그러지기 때문이다. `JinaArt.Place`는 그림의 실제 부모 크기를 되돌린다.
  - `KeeperVisual`: 주모는 기물 스프라이트("주막 주모")라서 자기 렌더러의 프레임을 직접 바꾼다.

## 그림 규칙 (화풍 일치)

- 원본 캐릭터 그림(`Assets/Resources/JinaV4/01~10`)은 외부에서 받은 V6 에셋이다. 해상도가 높은 가짜 도트(한 칸 약 10~12px)라 격자가 정확하지 않고, 가장자리에 반투명 테두리가 있다.
  - 이 캐릭터들의 새 모션은 새로 그리지 말고 기존 프레임을 잘라 붙이고 변형해서 만든다. 예: `Tools/ArtGen/make_run.py`는 걷기 프레임에 행 단위 기울이기(10px 단위)와 보폭 벌리기를 적용한다.
  - 공통 변형 도구는 `Tools/ArtGen/framelib.py`에 있다: `bend`(행 기울이기·띠 제거), `clean`(아래 정리), `scatter`(조각으로 부서짐), `preview_sheet`.
  - 변형한 뒤에는 alpha 128 미만을 지우고, 5×5 열림(opening) 연산으로 1~4px 잔선을 없앤다. 이렇게 하지 않으면 행을 밀었을 때 가는 선이 남는다.
  - 프레임 캔버스는 같은 애니메이션 안에서 같은 크기로 하고, 발은 아래에서 8px 위에 둔다. 크기 기준은 `JinaArt.Load` 안 경로별 ppu에 맞춘다. 예: 걷기·달리기 `546/1.9`, 보스 걷기 `586/2.6`, 보스 공격 `480/2.6`, 보스 대기는 bbox 높이/2.6.
- 새로 그린 그림(보부상, 기물, 주모, 부적)은 `Tools/ArtGen/pixlib.py`로 논리 도트 격자에 그린다.
  - 부위마다 1칸 검은 외곽선(`K=(11,9,11)`)을 두르고, 평면 채색에 음영은 한 단계만 쓴다. 앞쪽 부위가 뒤쪽 위에 그려진다.
  - 보부상은 12px 블록으로 확대해 저장한다. 기물(`13_Props`)은 1텍셀=1도트로 저장하고 `JinaArt.PropPixelsPerUnit`(=546/1.9/12)으로 캐릭터와 같은 도트 밀도를 맞춘다. 피벗은 아래 가운데, 땅 높이는 y=−0.5.
  - 팔레트(살색, 짚, 나무, 남색, 돌 등)는 `make_peddler.py`와 `make_props.py` 위쪽에 있다. 새 그림도 이 값을 재사용한다.
- 모든 생성 스크립트는 현재 커밋된 그림을 픽셀 단위로 똑같이 다시 만든다. 그림을 고칠 때는 스크립트를 고쳐서 다시 생성한다.
  - `python Tools/ArtGen/make_props.py Assets/Resources/JinaV4/13_Props`
  - `python Tools/ArtGen/make_peddler.py Assets/Resources/JinaV4/12_Peddler/peddler_idle.png`
  - `python Tools/ArtGen/make_run.py Assets/Resources/JinaV4/10_Player_Walk Assets/Resources/JinaV4/11_Player_Run`
  - `python Tools/ArtGen/make_boss_death.py Assets/Resources/JinaV4` (보스 죽음)
  - `python Tools/ArtGen/make_elder.py Assets/Resources/JinaV4` (노인 경보·칼 뽑기·피격·갓 덮인 시체, 잘라 낸 지팡이)
  - `python Tools/ArtGen/make_player_down.py Assets/Resources/JinaV4` (선비 쓰러짐. 캔버스를 좌우 48px 넓힘)
  - `python Tools/ArtGen/make_ghost_death.py Assets/Resources/JinaV4` (유령 죽음)
  - `python Tools/ArtGen/make_boss_remorse.py Assets/Resources/JinaV4` (정화된 도깨비의 무릎 꿇기. 눈과 송곳니만 기존 색으로 덮어 표정을 바꿈)
  - 대기 그림에서 잘라 낸 프레임의 발 위치·배율은 `JinaArt.CutFrom`이 정한다. `_corpse`는 그 캔버스를 90° 돌린 것으로 본다.
  - `make_peddler.py`는 `peddler_run_00~03`을, `make_props.py`는 `keeper_run_00~03`을 함께 만든다. 달리기 프레임은 대기 그림의 상자에 한 칸씩 여백을 둔 같은 캔버스를 쓴다.
- 결과를 확인할 때는 확대 미리보기(`--preview`)나 기존 캐릭터와 나란히 놓은 비교 시트를 scratchpad에 만들어 본다.
- 텍스처 임포트 설정(Point, 무압축, Readable)은 `Assets/Editor/JinaTextureImport.cs`가 `Resources/JinaV4/` 아래에 자동 적용한다.

## 보스 관련 현재 상태 (다음 작업 참고)

- 보스 프레임: `05_Boss_Brainwashed/boss_brainwashed_idle`, `boss_walk_00~03`, `boss_attack_00~03`, `boss_death_00~04`. 정화 후 모습: `06_Boss_Purified/boss_purified_idle`. 노인 피격: `08_Village_Elder/elder_hit_00~01`.
- `BossVisual.Pose`가 대기·걷기·공격·죽음 프레임을 고르고, `PrototypeCombat.BossAttackFrame`이 공격 단계를 정한다.
- 연출은 시작 시각으로 재생한다: 토벌 `PrototypeCombat.BossDownAt`, 정화 `PurifiedAt`, 노인 사망 `PrototypeQuest.ElderStruckAt`. 길이는 `BossVisual.DeathLength`·`PurifyLength`, `ElderVisual.FallLength`·`DrawLength`·`SheatheDelay`다.
- 토벌: `DamageEnemy`에서 체력이 0이 되면 `enemy`는 켜진 채로 두고, 죽는 모션이 끝나면 그림만 숨긴다.
- 노인 그림은 기울여 그리므로 크기가 비균일한 표식의 자식으로 두지 않는다(두면 찌그러진다). 노인 위치는 보부상 오른쪽(`PrototypeWorld.Elder`).
- 노인을 칠 때는 화면이 흔들리고(`PrototypeView.Shake`) 공격 후 경직이 길다.
- 노인 경보: `elder_startle` → `elder_draw_00`(지팡이 없음) → `elder_draw_01`(겨눔). 환도는 선비가 쓰는 `04_Weapons/hwando`를 같은 크기로 손(발에서 앞 .30, 위 .68)에 쥐여 준다. 경보 뒤의 피격·쓰러짐은 지팡이 없는 몸으로 그린다.
- 노인 시체: 90° 넘어진 뒤 `elder_corpse`(지팡이 없는 몸을 90° 돌린 그림)로 바뀐다. 방향은 맞은 순간에 고정한다(`deadFacing`). 보스가 지나가거나 쪽지를 읽어도 돌아보지 않는다.

## 검사

- Unity 에디터가 열려 있으면 배치 검사를 돌릴 수 없다. 이때는 사용자에게 에디터를 닫아 달라고 하거나, 아래의 컴파일만 확인한다.
- 규칙 검사(장면 없이): `Unity.exe -batchmode -quit -projectPath <프로젝트> -executeMethod PlayerArtChecks.Run -logFile <log>`
  - Tavern → Morality → World → Arrow → Quest → KoreanUI 검사가 이어서 실행된다. 성공하면 로그에 `*_PASSED`가 찍힌다.
- 플레이 검사: `-executeMethod JinaPlayChecks.Begin`(`-quit` 없음, 스스로 종료). 장면 렌더 캡처를 프로젝트 루트의 `validation/`에 남기므로, 확인한 뒤 지운다. IMGUI(말풍선, 연출)는 캡처에 찍히지 않는다.
  - `JinaPlayChecks`는 partial 클래스다. `JinaPlayChecks.cs`(실행 순서·헬퍼)와 주제별 파일 `.Player`·`.Boss`·`.Camp`·`.Elder`·`.Village`·`.Ending`으로 나뉜다.
  - 리팩토링 전후 비교: 캡처를 scratchpad에 복사해 두고 픽셀 단위로 비교한다. 화살 발사와 경보 장면의 보스 가장자리는 프레임 시간에 따라 1~2px씩 달라질 수 있다.
- Unity 경로: `C:/Program Files/Unity/Hub/Editor/2022.3.62f1/Editor/Unity.exe`. 오래 걸리므로 백그라운드로 실행한다.
- 에디터가 열려 있을 때 컴파일만 확인하려면 `Library/Bee/artifacts/*/Assembly-CSharp.rsp`의 참조로 `Editor/Data/DotNetSdkRoslyn/csc.dll`을 `Editor/Data/NetCoreRuntime/dotnet.exe`로 실행한다. 이때 `MSYS_NO_PATHCONV=1`이 필요하다.
- 검사 코드에서 비공개 필드에 접근할 때는 `JinaPlayChecks`의 `Set`/`Field` 반사 헬퍼를 쓴다. 자동 속성은 `<Name>k__BackingField`.
