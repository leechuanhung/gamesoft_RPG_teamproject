# 지나가던 선비 (Unity 2022.3.62f1, 2D 횡스크롤)

과거 시험을 보러 가는 선비의 모험. 작업 브랜치는 `seonbi`, 기본 브랜치는 `main`. 사용자와는 한국어로 대화한다.
게임 규칙과 개발 기록은 `시작하기.md`에 있다. 기능을 바꾸면 맨 위에 짧은 항목을 추가한다.

## 구조

- 장면은 `Assets/Scenes/MovementTest.unity` 하나. `PrototypeView.Start`가 `PrototypeWorld`(지형·기물)와 `PrototypeCombat`을 런타임에 만든다.
- `PrototypeCombat`이 중심이다. 보스, 화살, 체력, 재시작, `Paused`를 맡고 `Quest`, `Camp`, `Tavern`, `Peddler`, `Intro`를 붙인다.
- `Paused`는 인트로·대화·상점 중에 true다. 그동안 보스와 몬스터가 멈추고, `SeonbiVisual`이 자세를 초기화한다.
- 선악과 퀘스트 상태는 `QuestProgress`(Unity에 의존하지 않음)에 있다. 수치: 토벌 +10, 정화 +30, 노인 사망 −40.
- 가격과 판매 규칙은 `TavernWallet.cs`의 `ShopRules`에 있다.
- UI는 전부 IMGUI(`OnGUI`)다. 글꼴은 `PrototypeKoreanUI.Apply()`로 맑은 고딕을 쓴다. 인물의 대사는 `SpeechBubble.Draw`(말풍선), 비석과 쪽지는 기존 상자로 표시한다.
- 그림 로딩은 `JinaArt.Load`(캐릭터)와 `JinaArt.Prop`(기물)이 맡는다. 화면 표시는 `SeonbiVisual`(플레이어)과 `JinaActorVisual`(보스, 노인, 유령, 보부상)이 원래 사각형 스프라이트 위에 자식 SpriteRenderer를 얹는 방식이다.

## 그림 규칙 (화풍 일치)

- 원본 캐릭터 그림(`Assets/Resources/JinaV4/01~10`)은 외부에서 받은 V6 에셋이다. 해상도가 높은 가짜 도트(한 칸 약 10~12px)라 격자가 정확하지 않고, 가장자리에 반투명 테두리가 있다.
  - 이 캐릭터들의 새 모션은 새로 그리지 말고 기존 프레임을 잘라 붙이고 변형해서 만든다. 예: `Tools/ArtGen/make_run.py`는 걷기 프레임에 행 단위 기울이기(10px 단위)와 보폭 벌리기를 적용한다.
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
- 결과를 확인할 때는 확대 미리보기(`--preview`)나 기존 캐릭터와 나란히 놓은 비교 시트를 scratchpad에 만들어 본다.
- 텍스처 임포트 설정(Point, 무압축, Readable)은 `Assets/Editor/JinaTextureImport.cs`가 `Resources/JinaV4/` 아래에 자동 적용한다.

## 보스 관련 현재 상태 (다음 작업 참고)

- 보스 프레임: `05_Boss_Brainwashed/boss_brainwashed_idle`, `boss_walk_00~03`, `boss_attack_00~03`. 정화 후 모습: `06_Boss_Purified/boss_purified_idle`.
- `JinaActorVisual.Refresh`가 대기·걷기·공격 프레임을 고르고, `PrototypeCombat.BossAttackFrame`이 공격 단계를 정한다.
- 토벌: `DamageEnemy`에서 체력이 0이 되면 `enemy.enabled=false`로 바로 사라진다(죽는 모션 없음).
- 정화: `TryPurifyBoss`에서 바로 정화 모습으로 바뀐다(전환 연출 없음).
- 노인 사망: 보스 공격이 맞으면 `Quest.ShowElderDeath()`가 실행되고, 노인 그림이 80° 기울어진 회색으로 바뀐다.

## 검사

- Unity 에디터가 열려 있으면 배치 검사를 돌릴 수 없다. 이때는 사용자에게 에디터를 닫아 달라고 하거나, 아래의 컴파일만 확인한다.
- 규칙 검사(장면 없이): `Unity.exe -batchmode -quit -projectPath <프로젝트> -executeMethod PlayerArtChecks.Run -logFile <log>`
  - Tavern → Morality → World → Arrow → Quest → KoreanUI 검사가 이어서 실행된다. 성공하면 로그에 `*_PASSED`가 찍힌다.
- 플레이 검사: `-executeMethod JinaPlayChecks.Begin`(`-quit` 없음, 스스로 종료). 장면 렌더 캡처를 프로젝트 루트의 `validation/`에 남기므로, 확인한 뒤 지운다. IMGUI(말풍선, 연출)는 캡처에 찍히지 않는다.
- Unity 경로: `C:/Program Files/Unity/Hub/Editor/2022.3.62f1/Editor/Unity.exe`. 오래 걸리므로 백그라운드로 실행한다.
- 에디터가 열려 있을 때 컴파일만 확인하려면 `Library/Bee/artifacts/*/Assembly-CSharp.rsp`의 참조로 `Editor/Data/DotNetSdkRoslyn/csc.dll`을 `Editor/Data/NetCoreRuntime/dotnet.exe`로 실행한다. 이때 `MSYS_NO_PATHCONV=1`이 필요하다.
- 검사 코드에서 비공개 필드에 접근할 때는 `JinaPlayChecks`의 `Set`/`Field` 반사 헬퍼를 쓴다. 자동 속성은 `<Name>k__BackingField`.
