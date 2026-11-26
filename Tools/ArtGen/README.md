# 그림 생성 도구 (Tools/ArtGen)

게임의 새 그림은 모두 여기 있는 스크립트로 만든다. 직접 그린 파일을 넣지 않고, 스크립트를 고친 뒤 다시 생성한다.
모든 스크립트는 지금 커밋된 그림을 픽셀 단위로 똑같이 다시 만든다.

## 한 번에 확인하기

```bash
python Tools/ArtGen/make_all.py --check      # 전부 다시 만들고, 바뀐 그림이 있으면 실패
python Tools/ArtGen/sheet.py <scratchpad>/out.png 01_Player/player_idle 13_Props/keeper --sky village
```

- `make_all.py`는 아래 스크립트를 모두 돌린다. 새 스크립트를 만들면 `JOBS`에 추가한다.
- `sheet.py`는 그림을 게임 속 크기로 나란히 놓는다(발 높이를 맞추고 `JinaArt.Load`의 배율을 따름, 겨눈 그림 `_aim_`은 넓힌 아래쪽만큼 올림). 새 그림은 반드시 기존 캐릭터 옆에 놓고 본다. 결과는 scratchpad에 저장하고 `Assets`에는 두지 않는다.

## 두 종류의 그림

| | 원본 V6 캐릭터 (선비, 보스, 노인, 유령) | 새로 그린 그림 (보부상, 주모, 기물, 배경) |
|---|---|---|
| 원본 | `Resources/JinaV4/01~10`, 외부 에셋 | 스크립트가 처음부터 그림 |
| 새 모션 | **새로 그리지 않는다.** 기존 프레임을 잘라 붙이고 변형한다 (`framelib.py`) | 논리 도트 격자에 그린다 (`pixlib.py`) |
| 도트 크기 | 한 칸 약 10~12px(노인·유령은 약 25px). 격자가 정확하지 않고 반투명 테두리가 있다 | 보부상은 12px 블록으로 확대, 기물·배경은 1텍셀=1도트 |
| 외곽선 | 원본 그대로 | 부위마다 1칸 `K=(11,9,11)`. 배경만 외곽선 없음 |

## framelib.py (V6 변형)

- `bend(a, lean, top, hip, step, cut)`: `hip` 위의 행을 `lean`px만큼 민다(`top`에서 최대, `hip`에서 0). `cut=(y0, y1)`이면 그 행들을 지워서 위쪽이 내려앉는다(무릎 꺾기).
- `clean(a)`: alpha 128 미만을 지우고 5×5 열림 연산으로 1~4px 잔선을 없앤다. 행을 민 뒤에는 반드시 쓴다.
- `scatter(a, level, step, seed)`: 블록 단위로 부서져 흩어진다(보스·유령 죽음).
- 90° 회전은 `np.rot90`으로 하면 손실이 없다(시체). 그 밖의 각도는 원래 해상도에서 최근접 회전을 한다(`make_aim.py`). 10px 격자 단위로 돌리면 가는 선(활, 시위)이 끊긴다.
- 프레임 캔버스: 같은 애니메이션 안에서는 같은 크기로 하고, 발 높이를 고정한다. 대기 그림에서 잘라 낸 프레임은 대기 그림의 캔버스를 그대로 쓰거나(필요하면 좌우로 넓힘) 그 넓힌 양을 `JinaArt.CutFrom`에 적는다.

## pixlib.py (새 그림)

- `Canvas(w, h)`, 마스크(`rect`, `poly`, `ellipse`, `line`, `spans`), `layer(mask, color)`(외곽선과 함께 칠함), `paint(mask, color)`(외곽선 없이).
- 평면 채색에 음영은 한 단계만 쓴다. 앞쪽 부위를 나중에 그린다.
- 팔레트는 `make_props.py`(나무, 짚, 돌, 이끼, 소나무 등)와 `make_peddler.py`(살색, 무명, 남색 등) 위쪽에 있다. 새 그림도 이 값을 쓴다.
- 기물의 피벗은 아래 가운데이고 땅 높이는 y=−0.5다. 배율은 `JinaArt.PropPixelsPerUnit`(=546/1.9/12, 1유닛에 약 24도트).

## 연출 그림과 초상화 (사용자 그림)

플레이하지 않는 연출과 대화 초상화는 캐릭터 도트와 맞추지 않는다. 사용자가 GPT로 만든 그림을 잘라 쓰고, 새로 그리지 않는다.

- 초상화: `source/kongpat_portraits.webp`, `source/elder_portrait.webp` → `make_portraits.py`가 칸 안쪽을 112×121로 줄인다.
- 하룻밤 연출: `source/night/` → `make_night.py`가 가운데 띠를 잘라 2.4:1(1280×533, 128px가 1유닛)로 맞춘다. 편집본은 원본에 정렬한 뒤 바뀐 부분만 투명 레이어로 뺀다. 게임의 `JinaArt.Night`가 부드럽게(Bilinear) 읽는다.
- 예전에 직접 그리던 연출 도구(`hqlib.py`)는 지웠다. 필요하면 git 기록에 있다.

## 스크립트 목록

| 스크립트 | 만드는 그림 | 방식 |
|---|---|---|
| `make_props.py` | `13_Props`: 주막, 기와집, 소나무, 비석, 평상, 주모(+`keeper_run_00~03`), 부적, 발판 `ledge_stone`·`ledge_log` | pixlib |
| `make_backdrops.py` | `13_Props/bg_<맵>_far·_near` | pixlib, 하늘색에 가까운 실루엣. 4칸 넓게 그린 뒤 맵 폭 그대로 잘라 저장(옆 맵에 끼어들지 않게) |
| `make_peddler.py` | `12_Peddler/peddler_idle`, `peddler_run_00~03` | pixlib (다리 위치를 인자로) |
| `make_run.py` | `11_Player_Run` | 걷기 프레임 기울이기·보폭 |
| `make_player_down.py` | `01_Player/player_hit_00~01`, `player_corpse` | bend, rot90 (캔버스 좌우 48px 넓힘) |
| `make_player_ko.py` | `01_Player/player_ko_00~02`, `player_facedown` (게임 오버) | bend, 상체를 엉덩이 중심으로 회전, rot90 시계 방향 (캔버스 좌우 240px 넓힘) |
| `make_items.py` | `14_Items/item_*` 인벤토리 아이콘 (24도트 격자) | pixlib |
| `make_portraits.py` | `17_Portraits` 대화 초상화 7장. 사용자가 직접 만든 그림(`source/kongpat_portraits.webp`의 6칸, `source/elder_portrait.webp`의 노인)의 칸 안쪽을 잘라 112×121로 줄임(대화창에서 2배) | 사용자 그림 |
| `make_young.py` | 선비 V6 프레임 17장의 얼굴과 깃을 고쳐 수염을 없앰. 고정 칸을 덮어 쓰므로 다시 돌려도 같다. `make_run`·`make_player_down`·`make_player_ko`·`make_aim`이 그 뒤에 돌아야 한다 | 선비 원본 위에 덧칠 |
| `make_night.py` | `18_Night`: 콩밭 마을 하룻밤 연출. 사용자가 GPT로 만든 그림(`source/night/A1~F1, S`, 요청문은 `source/night/요청문.md`)의 가운데 띠를 잘라 2.4:1(1280×533)로 맞추고, 편집본은 원본에 정렬(`FIT`)해 바뀐 부분만 레이어로 뺀다. 문 창호지(그림자가 비치는 곳)와 바닥 달빛은 밝기로 가려낸다. 콩쥐 눈은 1280×720 | 사용자 그림 |
| `make_ground.py` | `13_Props/ground_<맵>`(한양 가는 길: fort, station, grave, city, hall, manor) (옆으로 이어지는 땅, 320×150, 걷는 면은 위에서 6도트), `sky_<맵>`, `river_deep`, `cloud_0~2` | numpy |
| `make_fx.py` | `20_Fx`: 칼빛 `arc`, 타격 `spark`, 먼지 `dust`, 충격파 `ring`, `glint`, 봉인 `seal`. 흰색으로 그리고 게임에서 색을 입힌다 | numpy |
| `make_ui.py` | `19_UI`: 이어지는 결(`grain_paper·wood·night`), 놋쇠 모서리 `corner`, HUD 아이콘, `vignette`, 낙관 `seal`, 단청 띠 `dancheong`, 두루마리 축 `roller`, 붓 자국 `brush`. 스킬 아이콘은 그리지 않는다(팀원 몫) | numpy, pixlib |
| `make_kong.py` | `16_Kong/kongjwi·patjwi·anak·namu·hunjang·yeot·halmi` (보부상 12px 블록, 격자 전체 폭 유지라 발 x=138), 콩쥐의 `kongjwi_sad·kneel·bow`(무릎 꿇고 울기, `bow`는 `framelib.bend`로 숙임), 시체 `<이름>_down`(서 있는 그림을 시계 방향 90° 회전, 잘라서 가운데 발), `13_Props/jangdok·well·laundry·feast_list` | 블록 격자, pixlib |
| `make_kong_boss.py` | `21_KongBoss/kong_boss_<idle_00·idle_01·crouch·hop·open·tongue·cast·crack·death_00~02>`: 밑 빠진 독에 낀 두꺼비, 콩쥐의 노란 소매가 앞다리. 같은 12px 블록, 모든 프레임이 72×74 블록 캔버스(발은 맨 아래 가운데, 위 8칸은 눈이 넘치지 않게 비움). `13_Props/kong_spit·bean·salt·brine·tongue·tongue_tip·bind`, 뒷산의 `crock·crock_cracked·crock_broken·kong_honey·ledge_roof` | 블록 격자(pixlib), `framelib.scatter` |
| `make_pass.py` | `13_Props/jangseung·seonang_tree·cairn·boulder` | pixlib |
| `make_ferry.py` | `15_Ferryman/ferryman_idle` (보부상과 같은 12px 블록, 발 x=126), `13_Props/ferry_boat·ferry_hut·reeds·water·thorn_wall` | 블록 격자, pixlib |
| `make_aim.py` | 마우스로 겨눈 공격: `03_Player_Attack_Bow/bow_aim_<u·d><15~90>_00~03`(15°마다), `02_Player_Attack_Sword/sword_aim_<u·d><30·60·90>_00~03`과 칼 없는 `sword_aim_empty_…` | 활은 앞팔·활·시위를 잘라 어깨 중심으로 돌려 몸 뒤에 그림(먼 쪽 팔이라 곧게 위로 쏘면 활이 갓 뒤로 감). 칼은 3번째 그림만 팔째 어깨 중심으로, 0·3번은 칼날만 손잡이 중심으로 돌리고, 1번(머리 위)은 그대로. 어깨 위는 겨눈 쪽으로 기움(90°에서 60px, `bend`와 같은 모양). 캔버스는 위아래 150px·오른쪽 100px 넓힘(`JinaArt.AimPad`) |
| `make_boss_death.py` | `05_Boss_Brainwashed/boss_death_00~04` | bend, scatter |
| `make_boss_remorse.py` | `06_Boss_Purified/boss_grieve_00~01` | bend. 눈(감김)과 송곳니만 기존 색으로 덮음 |
| `make_ghost_attack.py` | `07_Ghost/ghost_attack_00~02` | 팔을 어깨 중심으로 25px 격자에서 회전하고 외곽선을 다시 두름, bend. 물귀신은 같은 프레임에 색만 입힘 |
| `make_ghost_death.py` | `07_Ghost/ghost_death_00~02` | bend, scatter |
| `make_soldier.py` | `22_Soldier/<blade·bow>_<1~3>_<idle·walk_00~03·ready·strike·hurt·down>`: 관군. 선비와 같은 12px 도트·키(45칸), 오른쪽을 봄. 1단계 벙거지와 검은 쾌자, 2단계 두정갑, 3단계 붉은 갑옷과 쇠 투구. 칼은 쇠 코등이에 붉은 술(선비 환도는 금빛 코등이), 활은 붉은 칠(선비 각궁은 갈색). 48×56칸 캔버스에 발은 맨 아래 가운데. `13_Props/soldier_arrow` | pixlib(선비 도트) |
| `make_road_mobs.py` | `23_Mobs/wisp_*`(도깨비불, 가운데에 걺), `brute_*`(두억시니, 48×46칸, 발은 맨 아래 가운데) | pixlib(선비 도트) |
| `make_hanyang.py` | `24_Hanyang/sangin·yusaeng·cheongji·ai·mudang·jumo·bangmul`(콩밭 마을과 같은 격자, 발 x=138. 방물장수 `bangmul`은 콩밭 마을 입구에 선다), `13_Props/beacon_off·beacon_on·fort_gate_shut·fort_gate_open·fort_wall·station_house·cart·grave·dead_tree·shop·lantern·hongsal·exam_board·pavilion·drink_table` | 블록 격자, pixlib |
| `make_final_boss.py` | `25_Final/official_<idle·cup·blank>`(좌의정, 선비 도트), `final_<idle_00·idle_01·open·reach·raise·cast·death_00~02>`(어둑시니 좌의정, 100×112칸, 발은 맨 아래 가운데), `13_Props/final_ink·coin·hand·seal·mark` | pixlib, `framelib.scatter` |
| `make_elder.py` | `08_Village_Elder/elder_cane·startle·draw_00~01·hit_00~01·down·corpse` | 지팡이 잘라 내기, bend, 갓 내리기, rot90 |
| `make_sound.py` (+ `soundlib.py`) | `Resources/JinaSound`의 `bgm_<pass·hill·kong_fight·fort·station·grave·city·hall·manor·final_fight·ending>`, `sfx_<charm·keepsake·beacon·…>`, 무기 `sfx_w_<myeonggeom·saingeom·byeokjo·dagger>`, 보스 공격 `sfx_dk_*`(도깨비)·`sfx_kong_*`(콩쥐 요괴)·`sfx_fb_*`(좌의정). 새 wav의 .meta는 `sfx_hit.wav.meta`를 본떠 normalize를 끈다 | 그림이 아니라 소리. 가야금·대금·해금·피리·나발·장구·북·징·꽹과리·종을 numpy로 흉내 내 합성(고정 시드). 배경 음악은 끝 여운과 메아리를 앞에 접어 이음매 없이 되풀이. 팀원 wav는 건드리지 않음. 다른 컴퓨터의 소수점 오차(2 이하)로는 다시 쓰지 않음. `--only 이름`으로 하나만 |

## 새 그림을 게임에 넣을 때

1. 스크립트를 만들거나 고친다. 결과를 `sheet.py`로 기존 캐릭터 옆에 놓고 본다(필요하면 `--zoom`).
2. `make_all.py`의 `JOBS`에 넣는다.
3. 캐릭터 프레임이면 `Assets/Scripts/JinaArt.cs`의 `Load`에 발 위치(anchor)와 배율(ppu)을 맞춘다. 대기 그림에서 잘라 낸 프레임이면 `CutFrom`에 경로를 더한다. `sheet.py`의 `ppu()`도 같이 맞춘다.
4. 텍스처 설정(Point, 무압축, Readable)은 `Assets/Editor/JinaTextureImport.cs`가 자동으로 붙인다. `.meta`는 Unity가 처음 열 때 만든다.
5. `bash Tools/run_checks.sh --art --keep`로 검사하고 `validation/` 장면 캡처를 확인한 뒤 지운다.
