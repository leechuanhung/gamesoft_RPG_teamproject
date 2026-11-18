using System.Collections.Generic;
using UnityEngine;

// ===== 땅·기물·배경·발판 배치 (WorldBuilder) =====
// 이어진 구역들은 플레이어가 오가는 동안 퀘스트 상태를 유지한다(Maps가 각각을 맵으로 보여 줌).
// 세계는 두 가지로 생긴다: 씬에 아무것도 없으면 실행할 때 코드로 짓고(Build), 에디터 메뉴 "지나가던 선비 > 세계를
// 씬에 굽기"로 구워 두었으면 씬의 것을 그대로 쓴다(Baked, Ensure). 구운 세계는 무리별로 묶여 있다:
// 기물·배경·발판은 씬에서 옮기고 복사해도 되고(게임 논리와 상관없는 꾸밈), 땅과 하늘(땅 띠, 하늘, 강, 땅 충돌체,
// 끝 벽)은 맵 경계와 지형 높이(Top)에 맞춰져 있으므로 씬에서 고르지 못하게 잠가 둔다.
// NPC·보스·봉수대·조사 지점의 자리는 아래 상수들이 정하고, 씬 창에는 이름표로만 보인다(OnDrawGizmos).
public class WorldBuilder : MonoBehaviour
{
    // 씬에 구운 세계인지, 그리고 구울 때의 배치 판. 코드에서 기물·지형 배치를 바꾸면 Layout을 올린다:
    // 그러면 오래된 구운 세계로 실행할 때 다시 구우라고 알린다(다시 구워도 씬에서 옮긴 기물은 남는다).
    public bool Baked;
    public int BakedLayout;
    public const int Layout = 1;
    public const string GroundGroup = "땅과 하늘 (지형에 맞춰져 있음)", BackdropGroup = "배경", PropGroup = "기물", LedgeGroup = "발판";
    // 실행할 때 쓸 세계: 씬에 구운 것이 있으면 그것, 없으면 코드로 새로 짓는다.
    public static WorldBuilder Ensure(Sprite square)
    {
        foreach (var w in FindObjectsOfType<WorldBuilder>())
        {
            if (!w.Baked) continue;
            if (w.BakedLayout != Layout)
                Debug.LogWarning("씬에 구운 세계가 지금 코드의 배치보다 오래됐습니다. 메뉴 '지나가던 선비 > 세계를 씬에 굽기'로 다시 구우세요(씬에서 옮긴 기물은 그대로 남습니다).");
            HidePlaceholders();
            return w;
        }
        var world = new GameObject("네 구역 지도").AddComponent<WorldBuilder>();
        world.Build(square);
        return world;
    }
    // 무리 하나(없으면 만든다). 구운 세계를 씬에서 다루기 쉽게 오브젝트를 무리별로 묶는다.
    public Transform Group(string name)
    {
        var found = transform.Find(name);
        if (found != null) return found;
        var group = new GameObject(name).transform;
        group.SetParent(transform, false);
        return group;
    }
    // 처음 테스트 장면의 자리 표시 상자를 숨긴다.
    static void HidePlaceholders()
    {
        foreach (var name in new[] { "Ground", "Step", "Island", "Finish Ground", "Goal" })
        {
            var old = GameObject.Find(name);
            if (old != null) old.SetActive(false);
        }
    }
    public const float VillageLeft = -12f;
    public const float ForestStart = 28f;
    public const float EnemyHome = 46f;
    public const float EnemyMin = -10f;
    public const float GateX = -40f;
    public const float EnemyMax = 58f;
    // 숲 동쪽의 나루터: 가까운 둑, 강(배로 건넘), 길이 있는 건너편 둑.
    public const float FerryStart = 64f, RiverLeft = 80f, RiverRight = 100f, FerryEnd = 110f;
    // 나루터 너머 고개: 서낭당 고원까지 오르는 계단, 그리고 한양 가는 길로 다시 내려감.
    // 강 건너 콩쥐와 팥쥐가 사는 콩밭 마을(KongVillage).
    public const float KongStart = FerryEnd, KongEnd = 150f;
    public static readonly Vector2 Kongjwi = new Vector2(133f, .2f), Patjwi = new Vector2(135.2f, .2f),
        Anak = new Vector2(116.5f, .2f), Namu = new Vector2(123f, .2f),
        Hunjang = new Vector2(140.4f, .2f), Yeot = new Vector2(130.2f, .2f), Halmi = new Vector2(121.3f, .2f);
    // 요괴가 마을에 들어오면 팥쥐가 달아나는 곳: 맨 왼쪽, 그래서 요괴가 마지막에 닿는다.
    public const float PatjwiRaidX = 112.4f;
    public const float JangdokX = 146.5f, FeastListX = 143.9f, KongHouseX = 141.5f;
    public const float PassStart = KongEnd, PassEnd = 198f, ShrineX = 180.5f, RockStart = 185.5f;
    // 고개의 계단: (왼쪽, 오른쪽, 윗면).
    public static readonly (float left, float right, float top)[] PassSteps =
        { (150, 162, -.5f), (162, 168, 1f), (168, 186, 2.5f), (186, 190, 1f), (190, 198, -.5f) };
    // 고개 너머 콩쥐가 기다리는 뒷산(KongBoss): 울퉁불퉁한 땅, 높이 쌓인 발판 여럿,
    // 요괴가 던지는 것을 막는 큰 독. 한양 가는 길(금빛 표식)은 맨 끝에 있다.
    public const float HillStart = PassEnd, HillEnd = 246f, KongLair = 231f;
    // 뒷산의 높낮이: (왼쪽, 오른쪽, 윗면).
    public static readonly (float left, float right, float top)[] HillSteps =
        { (198, 204, -.5f), (204, 210, .7f), (210, 221, -.5f), (221, 225, 1.1f), (225, 232, -.5f), (232, 236, .6f), (236, 246, -.5f) };
    // (중심 x, 윗면, 그림): 세 층, 층마다 아래보다 약 1.7 높아서 하나씩 오를 수 있다.
    public static readonly (float x, float top, string art)[] HillLedges =
    {
        (201.5f, 1.4f, "ledge_stone"), (205.5f, 2.4f, "ledge_roof"), (209f, 4f, "ledge_log"),
        (213f, 1.3f, "ledge_stone"), (216.5f, 2.9f, "ledge_roof"), (213.5f, 4.5f, "ledge_stone"),
        (223f, 2.8f, "ledge_log"), (227.5f, 1.3f, "ledge_roof"), (231f, 2.9f, "ledge_stone"), (227.5f, 4.5f, "ledge_log"),
        (239.5f, 1.3f, "ledge_roof"), (242.5f, 3f, "ledge_stone"),
    };
    // 뒷산 곳곳에 엄폐물로 선 독.
    public static readonly float[] HillCrocks = { 207.5f, 218.5f, 229.5f, 238f };
    // 뒷산 너머 한양 가는 길(기획서.md 1-3): 봉화 산성(퍼즐, BeaconFort), 버려진 역참과 북망산 무덤길(일반 몬스터와
    // 관군, RoadMobs), 한양 거리와 과거장 앞(HanyangTown), 좌의정 댁(최종 보스, FinalBoss).
    // 과거장으로 가는 금빛 표식은 맨 끝에 있다.
    public const float FortStart = HillEnd, FortEnd = 292f, StationEnd = 338f, GraveEnd = 384f, CityEnd = 430f, HallEnd = 470f;
    public const float ManorStart = HallEnd, ManorEnd = 520f, WorldEnd = ManorEnd;
    public const float FortGateX = 289f, OfficialX = 503f;
    // 첫 마을 밖의 가게: 콩밭 마을 입구의 방물장수, 한양 잡화전 상인과 국밥집 주모(Shops.cs).
    public static readonly Vector2 Bangmul = new Vector2(111.2f, .2f), Sangin = new Vector2(393.5f, .2f), Gukbap = new Vector2(414.5f, .2f);
    // 산성과 무덤길의 높낮이: (왼쪽, 오른쪽, 윗면).
    public static readonly (float left, float right, float top)[] FortSteps = { (246, 252, -.5f), (252, 258, .8f), (258, 266, 2f), (266, 272, .8f), (272, 292, -.5f) };
    public static readonly (float left, float right, float top)[] GraveSteps = { (338, 346, -.5f), (346, 352, .8f), (352, 364, -.5f), (364, 370, 1.1f), (370, 384, -.5f) };
    // 산성의 가장 높은 발판(봉수대 하나가 그 위에 있다), 역참과 무덤길의 발판.
    public static readonly Vector2 FortPeak = new Vector2(269f, 3.7f);
    public static readonly Vector2[] StationLedges = { new Vector2(299, 1.3f), new Vector2(304, 2.9f), new Vector2(321, 1.3f), new Vector2(326.5f, 2.9f) };
    public static readonly Vector2[] GraveLedges = { new Vector2(349, 2.5f), new Vector2(358, 1.3f), new Vector2(367, 2.8f), new Vector2(377, 1.3f) };
    public static readonly float[] Graves = { 342f, 356f, 361f, 374f, 381f };
    public const float GoalX = WorldEnd - 3f;
    public static readonly Vector2 Ferryman = new Vector2(77f, .2f);
    // 물 띠의 바닥. 수면은 둑과 배 갑판보다 조금 아래.
    public const float WaterBottom = -1.5f;
    public const float DetectionRange = 18f;
    public static readonly Vector2 Spawn = new Vector2(0, .3f);
    // 보부상 동쪽, 그래서 마을에 들어온 도깨비가 노인에게 먼저 닿는다.
    public static readonly Vector2 Elder = new Vector2(21.5f, .2f);
    public static readonly Vector2 Tavern = new Vector2(10, .2f);
    public static readonly Vector2 Peddler = new Vector2(17.5f, .2f);
    public static readonly Vector2 Stone = new Vector2(-57, .5f);
    // 뛰어오를 발판: (중심 x, 윗면 y). 점프가 약 2칸이라 층마다 앞보다 ~1.7 높다.
    public static readonly Vector2[] CampLedges = { new Vector2(-16, 1.2f), new Vector2(-21, 2.9f), new Vector2(-27, 1.2f), new Vector2(-32, 2.7f), new Vector2(-36.5f, 1.3f) };
    // 숲 발판은 소나무 사이 틈에 있다(x 32 + 6n).
    public static readonly Vector2[] ForestLedges = { new Vector2(35, 1.3f), new Vector2(41, 2.9f), new Vector2(47, 1.3f), new Vector2(53, 2.8f), new Vector2(58.5f, 1.4f) };
    public const float LedgeWidth = 3f;
    // 구역마다 중심과 기물 뒤 배경 그림(먼 것은 카메라 따라 흐르고, 가까운 것은 고정).
    static readonly (string art, float x)[] Backdrops = { ("stele", -55), ("camp", -26), ("village", 8), ("forest", 46), ("ferry", 87), ("kong", 130), ("pass", 174), ("pass", 222),
        ("fort", 269), ("station", 315), ("grave", 361), ("city", 407), ("hall", 450), ("manor", 495) };
    // 맵마다 딛고 선 땅과 하늘: 그림 이름과 범위.
    static readonly (string art, float left, float right)[] Lands = { ("stele", -70.5f, GateX), ("camp", GateX, VillageLeft), ("village", VillageLeft, ForestStart), ("forest", ForestStart, FerryStart), ("ferry", FerryStart, FerryEnd), ("kong", KongStart, KongEnd), ("pass", PassStart, PassEnd), ("hill", HillStart, HillEnd),
        ("fort", FortStart, FortEnd), ("station", FortEnd, StationEnd), ("grave", StationEnd, GraveEnd), ("city", GraveEnd, CityEnd), ("hall", CityEnd, HallEnd), ("manor", ManorStart, ManorEnd + 1) };
    // 걷는 면은 땅 띠 윗면보다 이만큼 도트 아래(풀이 그 위로 솟음).
    const float GroundLip = 6;

    public static string Area(float x) => x < GateX ? "옛 비석 터" : x < VillageLeft ? "일반 몬스터 지역" : x >= ManorStart ? "좌의정 댁" : x >= CityEnd ? "과거장 앞"
        : x >= GraveEnd ? "한양 거리" : x >= StationEnd ? "북망산 무덤길" : x >= FortEnd ? "버려진 역참" : x >= FortStart ? "봉화 산성" : x >= HillStart ? "뒷산" : x >= PassStart ? "고개와 서낭당" : x >= KongStart ? "콩밭 마을" : x >= FerryStart ? "나루터" : x >= ForestStart ? "보스 지역" : "리스폰 마을";
    // 도깨비가 선비를 알아챌 수 있는지(숲 안).
    public static bool CanDetect(Vector2 player, Vector2 enemy) => player.x >= ForestStart && player.x < FerryStart
        && Mathf.Abs(player.x - enemy.x) <= DetectionRange && Mathf.Abs(player.y - enemy.y) < 4f;

    // 땅, 기물, 배경, 발판을 모두 배치한다.
    public void Build(Sprite sprite)
    {
        HidePlaceholders();
        Tile("비석 지역 땅", -55, -1, 30, 1, new Color(.36f,.34f,.43f), sprite, true);
        Tile("일반 몬스터 땅", -26, -1, 28, 1, new Color(.4f,.3f,.27f), sprite, true);
        Tile("마을 땅", 8, -1, 40, 1, new Color(.48f,.4f,.28f), sprite, true);
        Tile("숲 땅", 46, -1, 36, 1, new Color(.2f,.36f,.26f), sprite, true);
        Tile("나루터 땅", (FerryStart + RiverLeft) / 2, -1, RiverLeft - FerryStart, 1, new Color(.5f,.45f,.33f), sprite, true);
        Tile("건너편 강가", (RiverRight + FerryEnd) / 2, -1, FerryEnd - RiverRight, 1, new Color(.5f,.45f,.33f), sprite, true);
        Tile("비석 배경", -55, 4, 30, 10, new Color(.17f,.18f,.28f), sprite, false, -22);
        Tile("일반 지역 배경", -26, 4, 28, 10, new Color(.3f,.2f,.2f), sprite, false, -22);
        Tile("마을 배경", 8, 4, 40, 10, new Color(.24f,.3f,.34f), sprite, false, -22);
        Tile("숲 배경", 46, 4, 36, 10, new Color(.1f,.23f,.2f), sprite, false, -22);
        foreach (var (left, right, top) in PassSteps)
            Tile("고갯길", (left + right) / 2, (top - 1.5f) / 2, right - left, top + 1.5f, new Color(.42f,.38f,.36f), sprite, true);
        Tile("콩밭 마을 땅", (KongStart + KongEnd) / 2, -1, KongEnd - KongStart, 1, new Color(.48f,.4f,.28f), sprite, true);
        Tile("콩밭 마을 배경", (KongStart + KongEnd) / 2, 4, KongEnd - KongStart, 10, new Color(.24f,.3f,.34f), sprite, false, -22);
        Tile("고개 배경", (PassStart + PassEnd) / 2, 4, PassEnd - PassStart, 12, new Color(.27f,.26f,.34f), sprite, false, -22);
        foreach (var (left, right, top) in HillSteps)
            Tile("뒷산 땅", (left + right) / 2, (top - 1.5f) / 2, right - left, top + 1.5f, new Color(.4f,.36f,.36f), sprite, true);
        Tile("뒷산 배경", (HillStart + HillEnd) / 2, 4, HillEnd - HillStart, 12, new Color(.25f,.22f,.3f), sprite, false, -22);
        Tile("나루터 배경", (FerryStart + FerryEnd) / 2, 3.5f, FerryEnd - FerryStart, 11, new Color(.23f,.3f,.37f), sprite, false, -22);
        Tile("왼쪽 끝", -70.5f, 3, 1, 8, new Color(.3f,.3f,.38f), sprite, true);
        foreach (var (left, right, top) in FortSteps)
            Tile("산성 땅", (left + right) / 2, (top - 1.5f) / 2, right - left, top + 1.5f, new Color(.42f,.44f,.4f), sprite, true);
        Tile("역참 땅", (FortEnd + StationEnd) / 2, -1, StationEnd - FortEnd, 1, new Color(.56f,.48f,.36f), sprite, true);
        foreach (var (left, right, top) in GraveSteps)
            Tile("무덤길 땅", (left + right) / 2, (top - 1.5f) / 2, right - left, top + 1.5f, new Color(.32f,.28f,.34f), sprite, true);
        Tile("한양 땅", (GraveEnd + WorldEnd) / 2, -1, WorldEnd - GraveEnd, 1, new Color(.56f,.55f,.53f), sprite, true);
        Tile("오른쪽 끝", WorldEnd + .5f, 3, 1, 8, new Color(.4f,.38f,.3f), sprite, true);
        Tile("과거장으로 가는 길", GoalX, 1, .4f, 3, new Color(1,.75f,.2f), sprite, false);
        Solids();
        for (int i = 0; i < 5; i++) Prop("숲 소나무", "pine", 32 + i * 6, -10, i % 2 == 1);
        Prop("마을 기와집", "house", 4.5f, -10);
        // 주막 문은 건물 중심에서 왼쪽으로 약 1칸, 주모 뒤.
        Prop("주막", "tavern", 11, -8);
        Prop("주막 주모", "keeper", Tavern.x, 1);
        Prop("주막 평상", "bench", 14, -5);
        Prop("거북 받침 비석", "stele", Stone.x, 0);
        Prop("뱃사공 오두막", "ferry_hut", 70, -10);
        Prop("콩밭 마을 초가", "ferry_hut", 113, -10);
        Prop("콩밭 마을 우물", "well", 119.5f, -6);
        Prop("콩밭 마을 빨랫줄", "laundry", 128, -7);
        Prop("팥쥐네 기와집", "house", 141.5f, -10);
        Prop("팥쥐네 평상", "bench", 138.5f, -5);
        Prop("잔치 명단", "feast_list", FeastListX, -4);
        Prop("팥쥐네 장독대", "jangdok", JangdokX, -6);
        Prop("장승", "jangseung", 154, -6);
        Prop("서낭나무", "seonang_tree", 176.5f, -10).transform.position = new Vector3(176.5f, Top(176.5f), 0);
        Prop("서낭당 돌무더기", "cairn", ShrineX, -4).transform.position = new Vector3(ShrineX, Top(ShrineX), 0);
        foreach (var x in new[] { 66.5f, 79.2f, 100.8f, 109f }) Prop("갈대", "reeds", x, -4, x > 90);
        // 강: 둑 사이, 둑 바로 아래의 물 띠(바닥이 없다. Ferry 참고).
        for (float x = RiverLeft + 2; x < RiverRight; x += 4) Prop("강물", "water", x, 6, false, Group(GroundGroup)).transform.position = new Vector3(x, WaterBottom, 0);
        RoadProps();
        foreach (var (art, x) in Backdrops)
        {
            Prop("원경 " + art, "bg_" + art + "_far", x, -19, false, Group(BackdropGroup)).gameObject.AddComponent<ParallaxLayer>().home = x;
            Prop("근경 " + art, "bg_" + art + "_near", x, -18, false, Group(BackdropGroup));
        }
        foreach (var (art, left, right) in Lands)
        {
            Sky(art == "hill" ? "pass" : art, left, right);
            if (art == "pass") foreach (var (l, r, top) in PassSteps) Land(art, l, r, top);
            else if (art == "hill") foreach (var (l, r, top) in HillSteps) Land("pass", l, r, top);
            else if (art == "fort") foreach (var (l, r, top) in FortSteps) Land(art, l, r, top);
            else if (art == "grave") foreach (var (l, r, top) in GraveSteps) Land(art, l, r, top);
            else if (art == "manor") Land(art, left, right, -.5f);
            else if (art == "ferry") { Land(art, left, RiverLeft, -.5f); Land(art, RiverRight, right, -.5f); River(); }
            else Land(art, left, right, -.5f);
        }
        foreach (var ledge in CampLedges) Ledge("바위 발판", "ledge_stone", ledge);
        foreach (var ledge in ForestLedges) Ledge("통나무 발판", "ledge_log", ledge);
        foreach (var (x, top, art) in HillLedges) Ledge("뒷산 발판", art, new Vector2(x, top));
        foreach (var x in HillCrocks) Prop("뒷산 독", "crock", x, -4).transform.position = new Vector3(x, Top(x), 0);
        Ledge("산성 발판", "ledge_stone", FortPeak);
        foreach (var ledge in StationLedges) Ledge("역참 발판", "ledge_log", ledge);
        foreach (var ledge in GraveLedges) Ledge("무덤길 발판", "ledge_stone", ledge);
    }

    // 한양 가는 길의 기물: 산성의 군령판·소나무·성가퀴·우물, 역참과 수레, 무덤, 한양의 가게와 초롱, 홍살문, 누각.
    void RoadProps()
    {
        Prop("봉수 군령판", "exam_board", BeaconFort.BoardX, -6);
        Prop("산성 소나무", "pine", 249.5f, -10);
        Prop("무너진 성가퀴", "fort_wall_broken", 255f, -10).transform.position = new Vector3(255f, Top(255f), 0);
        Prop("산성 성벽", "fort_wall", 263f, -12).transform.position = new Vector3(263f, Top(263f), 0);
        Prop("산성 우물", "well", 275.8f, -6);
        Prop("버려진 역참", "station_house", 311f, -10);
        Prop("부서진 수레", "cart", 300.5f, -6);
        Prop("역참 마른 나무", "dead_tree", 331.5f, -10);
        foreach (var x in Graves) Prop("무덤", "grave", x, -6).transform.position = new Vector3(x, Top(x), 0);
        Prop("무덤길 마른 나무", "dead_tree", 353.5f, -10);
        Prop("무덤길 마른 나무", "dead_tree", 371.5f, -10, true);
        Prop("한양 가게", "shop", 390.5f, -10);
        Prop("한양 기와집", "house", 403f, -10);
        Prop("한양 가게", "shop", 416.5f, -10, true);
        foreach (var x in new[] { 397f, 413.2f, 423f, 451f, 467f, 479f, 487f, 516f }) Prop("청사초롱", "lantern", x, -5);
        Prop("홍살문", "hongsal", 437f, -10);
        Prop("과거 방", "exam_board", 447.5f, -6);
        Prop("과거장 기와집", "house", 461f, -10);
        Prop("좌의정 댁 솟을대문", "manor_gate", 473.5f, -10);
        foreach (var x in new[] { 483.5f, 494.5f }) Prop("석등", "stone_lantern", x, -6);
        Prop("좌의정 댁 누각", "pavilion", 504f, -12);
    }

    // 충돌체 위의 땅 그림: 화면 아래 너머까지 흙, 위에 풀.
    void Land(string art, float left, float right, float top)
    {
        var land = new GameObject("지면 " + art).AddComponent<SpriteRenderer>();
        land.transform.SetParent(Group(GroundGroup));
        land.sprite = JinaArt.Strip("ground_" + art);
        land.drawMode = SpriteDrawMode.Tiled;
        land.size = new Vector2(right - left, land.sprite.bounds.size.y);
        land.transform.position = new Vector3((left + right) / 2, top + GroundLip / JinaArt.PropPixelsPerUnit, 0);
        land.sortingOrder = 2;
        WorldArt.Mark(land, WorldArt.Kind.Strip, "ground_" + art);
    }
    // 맵의 하늘: 색 한 줄을 가로로 늘이고, 지평선은 땅 가까이.
    void Sky(string art, float left, float right)
    {
        var sky = new GameObject("하늘 " + art).AddComponent<SpriteRenderer>();
        sky.transform.SetParent(Group(GroundGroup));
        sky.sprite = JinaArt.Prop("sky_" + art);
        sky.transform.position = new Vector3((left + right) / 2, -4f, 0);
        sky.transform.localScale = new Vector3((right - left) * JinaArt.PropPixelsPerUnit, 1, 1);
        sky.sortingOrder = -21;
        WorldArt.Mark(sky, WorldArt.Kind.Prop, "sky_" + art);
    }
    // 강 수면 띠 아래의 어두운 물.
    void River()
    {
        var deep = new GameObject("깊은 강물").AddComponent<SpriteRenderer>();
        deep.transform.SetParent(Group(GroundGroup));
        deep.sprite = JinaArt.Strip("river_deep");
        deep.drawMode = SpriteDrawMode.Tiled;
        deep.size = new Vector2(RiverRight - RiverLeft, deep.sprite.bounds.size.y);
        deep.transform.position = new Vector3((RiverLeft + RiverRight) / 2, WaterBottom + .1f, 0);
        deep.sortingOrder = 5;
        WorldArt.Mark(deep, WorldArt.Kind.Strip, "river_deep");
    }

    // 한 방향 발판: 아래에서 뚫고 뛰어올라 윗면에 선다.
    void Ledge(string label, string art, Vector2 top)
    {
        var image = Prop(label, art, top.x, -3, false, Group(LedgeGroup));
        // 그려진 턱은 그림 윗변에서 2도트 아래.
        float height = image.sprite.bounds.size.y, lip = 2 / JinaArt.PropPixelsPerUnit;
        image.transform.position = new Vector3(top.x, top.y - height + lip, 0);
        image.gameObject.layer = 8;
        var box = image.gameObject.AddComponent<BoxCollider2D>();
        box.size = new Vector2(LedgeWidth - .1f, .25f);
        box.offset = new Vector2(0, height - lip - .125f);
        box.usedByEffector = true;
        var effector = image.gameObject.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 160;
    }

    // 고개와 뒷산에서 x의 땅 윗면(둑과 다른 맵은 -0.5).
    public static float Top(float x)
    {
        foreach (var (left, right, top) in PassSteps) if (x >= left && x < right) return top;
        foreach (var (left, right, top) in HillSteps) if (x >= left && x < right) return top;
        foreach (var (left, right, top) in FortSteps) if (x >= left && x < right) return top;
        foreach (var (left, right, top) in GraveSteps) if (x >= left && x < right) return top;
        return -.5f;
    }

    // 도트 기물은 땅 표면(y = -0.5)에 선다. `group`을 주지 않으면 기물 무리에 든다.
    SpriteRenderer Prop(string label, string art, float x, int order, bool flip = false, Transform group = null)
    {
        var prop = new GameObject(label);
        prop.transform.SetParent(group != null ? group : Group(PropGroup));
        prop.transform.position = new Vector3(x, -.5f, 0);
        var image = prop.AddComponent<SpriteRenderer>();
        image.sprite = JinaArt.Prop(art);
        image.sortingOrder = order;
        image.flipX = flip;
        WorldArt.Mark(image, WorldArt.Kind.Prop, art);
        return image;
    }

    // 땅 조각 하나를 놓는다(`solid`면 충돌체 포함).
    void Tile(string label, float x, float y, float width, float height, Color color, Sprite sprite, bool solid, int order = 0)
    {
        var tile = new GameObject(label);
        tile.transform.SetParent(Group(GroundGroup));
        tile.transform.position = new Vector3(x, y, 0);
        tile.transform.localScale = new Vector3(width, height, 1);
        var art = tile.AddComponent<SpriteRenderer>();
        art.sprite = sprite;
        art.color = color;
        art.sortingOrder = order;
        if (solid) { tile.layer = 8; solids.Add(new Rect(x - width / 2, y - height / 2, width, height)); }
    }

    // 단단한 땅과 벽의 범위. 충돌체는 Build 끝에 Solids가 붙인다.
    readonly List<Rect> solids = new List<Rect>();

    // 땅의 충돌체: 윗면과 바닥이 같은 조각이 맞닿으면 상자 하나로 합친다. 조각마다 상자를 따로 두면
    // 이음매에서 옆 상자의 모서리가 걷는 선비의 상자를 가로막아, 맵 경계(땅 조각이 바뀌는 곳)를
    // 걸어서는 못 지나가고 뛰어야 넘을 때가 있었다.
    void Solids()
    {
        solids.Sort((a, b) => a.xMin.CompareTo(b.xMin));
        for (int i = 0; i < solids.Count; i++)
        {
            var run = solids[i];
            while (i + 1 < solids.Count && Joins(run, solids[i + 1])) run.xMax = solids[++i].xMax;
            var box = new GameObject("땅 충돌체");
            box.transform.SetParent(Group(GroundGroup));
            box.transform.position = run.center;
            box.layer = 8;
            box.AddComponent<BoxCollider2D>().size = run.size;
        }
        solids.Clear();
    }
    static bool Joins(Rect a, Rect b) => Mathf.Abs(a.xMax - b.xMin) < .01f && Mathf.Abs(a.yMin - b.yMin) < .01f && Mathf.Abs(a.yMax - b.yMax) < .01f;

#if UNITY_EDITOR
    // ── 씬 창의 이름표 ──
    // 게임 논리가 쓰는 자리(코드의 상수)를 씬 창에 보여 준다. 씬에서 옮길 수는 없고, 옮기려면 위의 상수를 고친다.
    public static System.Collections.Generic.IEnumerable<(Vector2 at, string label, Color colour)> Markers()
    {
        Color who = new Color(1, .86f, .45f), boss = new Color(1, .42f, .42f), thing = new Color(.6f, .9f, 1), look = new Color(.78f, .78f, .78f);
        yield return (Spawn, "선비 시작", Color.white);
        yield return (Tavern, "주모 (주막)", who);
        yield return (Peddler, "보부상", who);
        yield return (Elder, "노인", who);
        yield return (Stone, "비석 (부적 퍼즐)", thing);
        yield return (new Vector2(EnemyHome, .2f), "도깨비", boss);
        yield return (Ferryman, "뱃사공", who);
        yield return (Kongjwi, "콩쥐", who); yield return (Patjwi, "팥쥐", who); yield return (Anak, "아낙", who); yield return (Namu, "나무꾼", who);
        yield return (Hunjang, "훈장", who); yield return (Yeot, "엿장수", who); yield return (Halmi, "할멈", who);
        yield return (new Vector2(KongLair, Top(KongLair)), "콩쥐 요괴", boss);
        yield return (new Vector2(ShrineX, Top(ShrineX)), "서낭당", thing);
        foreach (var b in BeaconFort.All) yield return (new Vector2(b.x, b.top), "봉수대: " + b.place, thing);
        yield return (new Vector2(FortGateX, -.5f), "산성 문", thing);
        foreach (var p in HanyangTown.People) yield return (new Vector2(p.x, -.5f), p.name, who);
        for (int i = 1; i < TavernShop.Stalls.Length; i++) yield return (TavernShop.Stalls[i].at, TavernShop.Stalls[i].title, who);
        for (int i = 1; i < PeddlerShop.Stalls.Length; i++) yield return (PeddlerShop.Stalls[i].at, PeddlerShop.Stalls[i].title, who);
        yield return (new Vector2(OfficialX, -.5f), "좌의정 (최종 보스)", boss);
        yield return (new Vector2(GoalX, -.5f), "과거장 (결말)", new Color(1, .75f, .2f));
        foreach (var s in RoadsideLore.Spots) yield return (new Vector2(s.x, Top(s.x)), "조사: " + s.prompt, look);
    }
    // 맵 경계와 이름, 자리 이름표를 그린다(씬 창의 Gizmos로 켜고 끈다).
    void OnDrawGizmos()
    {
        var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel) { fontSize = 11 };
        foreach (var map in Maps.All)
        {
            Gizmos.color = new Color(1, 1, 1, .35f);
            Gizmos.DrawLine(new Vector3(map.left, -2, 0), new Vector3(map.left, 9, 0));
            style.normal.textColor = new Color(1, 1, 1, .8f);
            UnityEditor.Handles.Label(new Vector3(map.left + .3f, 8.8f, 0), map.name, style);
        }
        foreach (var (at, label, colour) in Markers())
        {
            var feet = new Vector3(at.x, at.y < -.4f ? at.y : at.y - .7f, 0);
            Gizmos.color = colour;
            Gizmos.DrawWireCube(feet + Vector3.up * .9f, new Vector3(.8f, 1.8f, 0));
            style.normal.textColor = colour;
            UnityEditor.Handles.Label(feet + Vector3.up * 2.3f, label, style);
        }
    }
#endif
}

// ===== 맵별 분위기 효과 (MapAmbience) =====
// 맵마다 사람 사는 느낌을 주는 것, 플레이에는 영향 없음: 산 뒤로 흐르는 구름, 공중의 티끌
// (비석 터의 반딧불, 들판의 재, 숲의 잎, 강가의 안개와 잠자리, 콩밭의 씨앗,
// 고개의 바람), 주막 굴뚝 연기, 강의 물빛, 전체에 옅은 비네트.
public class MapAmbience : MonoBehaviour
{
    public Combat combat;
    enum Kind { Firefly, Ash, Petal, Leaf, Mist, Seed, Wind }
    struct Air { public Kind kind; public Color a, b; public int count; public Color cloud; public int clouds; }
    // Maps.All 순서.
    static readonly Air[] AirByMap =
    {
        new Air { kind = Kind.Firefly, a = new Color(.72f, 1, .78f), b = new Color(.5f, .8f, 1), count = 16, cloud = new Color(.32f, .34f, .5f, .55f), clouds = 3 },
        new Air { kind = Kind.Ash, a = new Color(1, .62f, .3f), b = new Color(.45f, .42f, .42f), count = 26, cloud = new Color(.42f, .2f, .22f, .75f), clouds = 4 },
        new Air { kind = Kind.Petal, a = new Color(1, .86f, .9f), b = new Color(1, .96f, .9f), count = 12, cloud = new Color(1, 1, 1, .9f), clouds = 5 },
        new Air { kind = Kind.Leaf, a = new Color(.56f, .72f, .36f), b = new Color(.8f, .72f, .36f), count = 22, cloud = new Color(.2f, .36f, .34f, .4f), clouds = 2 },
        new Air { kind = Kind.Mist, a = new Color(.9f, .94f, .96f), b = new Color(.76f, .86f, .9f), count = 18, cloud = new Color(.94f, .96f, .98f, .85f), clouds = 5 },
        new Air { kind = Kind.Seed, a = new Color(1, .96f, .78f), b = new Color(1, .86f, .56f), count = 18, cloud = new Color(1, .96f, .86f, .92f), clouds = 5 },
        new Air { kind = Kind.Wind, a = new Color(1, .84f, .74f), b = new Color(.9f, .66f, .62f), count = 16, cloud = new Color(.9f, .56f, .5f, .75f), clouds = 5 },
        new Air { kind = Kind.Ash, a = new Color(.62f, .5f, .7f), b = new Color(.36f, .3f, .4f), count = 22, cloud = new Color(.5f, .36f, .46f, .8f), clouds = 4 },
        // 봉화 산성: 아침 솔잎, 역참: 마른 바람, 무덤길: 도깨비불 같은 반딧불, 한양: 꽃잎, 과거장: 은행잎, 좌의정 댁: 붉은 재.
        new Air { kind = Kind.Leaf, a = new Color(.5f, .66f, .42f), b = new Color(.7f, .74f, .5f), count = 16, cloud = new Color(.92f, .95f, .96f, .85f), clouds = 4 },
        new Air { kind = Kind.Wind, a = new Color(.9f, .84f, .7f), b = new Color(.78f, .72f, .6f), count = 18, cloud = new Color(.86f, .86f, .84f, .9f), clouds = 5 },
        new Air { kind = Kind.Firefly, a = new Color(.5f, .8f, 1), b = new Color(.7f, .9f, 1), count = 18, cloud = new Color(.36f, .26f, .44f, .7f), clouds = 3 },
        new Air { kind = Kind.Petal, a = new Color(1, .86f, .9f), b = new Color(1, .96f, .9f), count = 12, cloud = new Color(1, 1, 1, .9f), clouds = 5 },
        new Air { kind = Kind.Leaf, a = new Color(.96f, .82f, .3f), b = new Color(.86f, .7f, .24f), count = 20, cloud = new Color(1, .94f, .84f, .9f), clouds = 4 },
        new Air { kind = Kind.Ash, a = new Color(1, .42f, .3f), b = new Color(.5f, .2f, .3f), count = 24, cloud = new Color(.3f, .12f, .2f, .7f), clouds = 3 },
    };
    const float ViewHalfW = 11, ViewHalfH = 5.6f;
    const int MaxMotes = 30, MaxClouds = 5, SmokeBits = 10;

    class Mote { public SpriteRenderer art; public float x, y, phase, speed; }
    class Cloud { public SpriteRenderer art; public float offset, height, speed; }
    readonly Mote[] motes = new Mote[MaxMotes];
    readonly Cloud[] clouds = new Cloud[MaxClouds];
    readonly SpriteRenderer[] smoke = new SpriteRenderer[SmokeBits];
    readonly SpriteRenderer[] shimmer = new SpriteRenderer[14];
    int shownMap = -1;

    // 구름, 티끌, 연기, 물빛, 비네트 렌더러를 만든다.
    void Start()
    {
        for (int i = 0; i < MaxMotes; i++)
        {
            motes[i] = new Mote { art = Dot("공기 " + i, 2), phase = Random.value * 100, x = Random.value, y = Random.value, speed = Random.Range(.7f, 1.3f) };
            motes[i].art.enabled = false;
        }
        for (int i = 0; i < MaxClouds; i++)
        {
            var art = new GameObject("구름 " + i).AddComponent<SpriteRenderer>();
            art.transform.SetParent(transform);
            art.sprite = JinaArt.Prop("cloud_" + i % 3); art.sortingOrder = -20;
            clouds[i] = new Cloud { art = art, offset = Random.value * 60, height = Random.Range(3.6f, 6.6f), speed = Random.Range(.12f, .3f) };
            art.enabled = false;
        }
        for (int i = 0; i < SmokeBits; i++) { smoke[i] = Dot("주막 연기 " + i, -7); smoke[i].enabled = false; }
        for (int i = 0; i < shimmer.Length; i++) { shimmer[i] = Dot("강 물빛 " + i, 3); shimmer[i].enabled = false; }
    }
    // 티끌 하나용 렌더러를 만든다.
    SpriteRenderer Dot(string label, int order)
    {
        var art = new GameObject(label).AddComponent<SpriteRenderer>();
        art.transform.SetParent(transform);
        art.sprite = JinaArt.Dot(); art.sortingOrder = order;
        art.transform.localScale = Vector3.one * .5f;
        return art;
    }

    // 지금 맵의 분위기 효과를 움직인다.
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || combat == null || combat.Maps == null) return;
        bool away = combat.Stay != null && combat.Stay.InRoom;
        int map = combat.Maps.Current;
        var air = AirByMap[Mathf.Clamp(map, 0, AirByMap.Length - 1)];
        float cx = cam.transform.position.x, t = Time.time;

        for (int i = 0; i < MaxMotes; i++)
        {
            var m = motes[i];
            m.art.enabled = !away && i < air.count;
            if (!m.art.enabled) continue;
            StepMote(m, air, i, t, cx);
        }
        var bounds = Maps.All[map];
        float span = bounds.right - bounds.left + 20;
        for (int i = 0; i < MaxClouds; i++)
        {
            var c = clouds[i];
            c.art.enabled = !away && i < air.clouds;
            if (!c.art.enabled) continue;
            // 맵을 가로질러 다시 돌아온다. 화면의 무엇보다 느리게.
            float x = bounds.left - 10 + Mathf.Repeat(c.offset * 13 + i * span / MaxClouds + t * c.speed, span);
            c.art.transform.position = new Vector3(x, c.height, 0);
            c.art.color = air.cloud;
        }
        StepSmoke(t, !away && map == 2);
        StepRiver(t, !away && map == 4, cx);
    }

    // 티끌 하나를 맵 종류에 맞게 움직인다.
    void StepMote(Mote m, Air air, int i, float t, float cx)
    {
        float phase = m.phase;
        float wrapX(float v) => cx - ViewHalfW + Mathf.Repeat(v, ViewHalfW * 2);
        float wrapY(float v) => -.2f + Mathf.Repeat(v, ViewHalfH * 1.6f);
        Vector3 p; float alpha = 1; float size = .5f;
        switch (air.kind)
        {
            case Kind.Firefly:
                p = new Vector3(wrapX(phase * 31 + Mathf.Sin(t * .4f + phase) * 1.5f), .2f + Mathf.Repeat(phase * 7, 4.5f) + Mathf.Sin(t * .7f + phase * 3) * .6f, 0);
                alpha = Mathf.Clamp01(Mathf.Sin(t * 1.3f * m.speed + phase) * 1.5f);
                break;
            case Kind.Ash:
                p = new Vector3(wrapX(phase * 31 + t * .25f * m.speed + Mathf.Sin(t + phase)), wrapY(phase * 17 + t * .55f * m.speed), 0);
                alpha = i % 3 == 0 ? .95f : .6f; size = i % 3 == 0 ? .5f : .4f;
                break;
            case Kind.Petal:
                p = new Vector3(wrapX(phase * 31 + t * .35f * m.speed + Mathf.Sin(t * .8f + phase) * .8f), wrapY(phase * 17 - t * .4f * m.speed), 0);
                alpha = .85f;
                break;
            case Kind.Leaf:
                p = new Vector3(wrapX(phase * 31 - t * .3f * m.speed + Mathf.Sin(t * 1.1f + phase) * 1.1f), wrapY(phase * 17 - t * .6f * m.speed), 0);
                alpha = .9f; size = .6f;
                break;
            case Kind.Mist:
                p = new Vector3(wrapX(phase * 31 + t * .18f * m.speed), -.1f + Mathf.Repeat(phase * 7, 1.6f) + Mathf.Sin(t * .3f + phase) * .15f, 0);
                alpha = .18f + .1f * Mathf.Sin(t * .5f + phase); size = 1.2f + i % 3 * .6f;
                break;
            case Kind.Seed:
                p = new Vector3(wrapX(phase * 31 + t * .3f * m.speed + Mathf.Sin(t * .6f + phase)), .2f + Mathf.Repeat(phase * 11 + t * .1f, 5f) + Mathf.Sin(t * .9f + phase) * .3f, 0);
                alpha = .55f + .3f * Mathf.Sin(t * 2 + phase);
                break;
            default:
                p = new Vector3(wrapX(phase * 31 - t * (2.4f + m.speed)), wrapY(phase * 17) + Mathf.Sin(t * 1.3f + phase) * .2f, 0);
                alpha = .45f; size = .4f;
                break;
        }
        m.art.transform.position = p;
        m.art.transform.localScale = air.kind == Kind.Wind ? new Vector3(size * 4, size * .6f, 1) : air.kind == Kind.Mist ? new Vector3(size * 4, size, 1) : Vector3.one * size;
        var c = i % 2 == 0 ? air.a : air.b; c.a = alpha;
        m.art.color = c;
    }

    // 주막 지붕에서 오르는 가는 연기.
    void StepSmoke(float t, bool show)
    {
        for (int i = 0; i < SmokeBits; i++)
        {
            smoke[i].enabled = show;
            if (!show) continue;
            float life = Mathf.Repeat(t * .22f + i * .1f, 1);
            smoke[i].transform.position = new Vector3(WorldBuilder.Tavern.x + 2.4f + Mathf.Sin(life * 6 + t) * .1f + life * .5f, 3.0f + life * 2.6f, 0);
            smoke[i].transform.localScale = Vector3.one * (.5f + life * 1.3f);
            smoke[i].color = new Color(.92f, .92f, .9f, .42f * (1 - life));
        }
    }

    // 강 수면에서 움직이는 반짝임.
    void StepRiver(float t, bool show, float cx)
    {
        for (int i = 0; i < shimmer.Length; i++)
        {
            shimmer[i].enabled = show;
            if (!show) continue;
            float x = WorldBuilder.RiverLeft + Mathf.Repeat(i * 1.9f + t * .24f * (i % 2 == 0 ? 1 : -1), WorldBuilder.RiverRight - WorldBuilder.RiverLeft);
            float y = -1.1f - (i % 4) * .5f;
            float pulse = Mathf.Clamp01(Mathf.Sin(t * 1.4f + i * 1.7f) * 1.5f);
            shimmer[i].transform.position = new Vector3(x, y, 0);
            shimmer[i].transform.localScale = new Vector3(1.8f + i % 3, .5f, 1);
            shimmer[i].color = new Color(.84f, .94f, 1, .6f * pulse);
        }
    }

    // 화면 비네트를 그린다.
    void OnGUI()
    {
        if (combat == null || (combat.Stay != null && combat.Stay.InRoom)) return;
        GUI.depth = 20;
        KoreanUI.Apply();
        var v = JinaUI.Art("vignette");
        if (v == null) return;
        v.wrapMode = TextureWrapMode.Clamp; v.filterMode = FilterMode.Bilinear;
        var saved = GUI.color; GUI.color = new Color(1, 1, 1, .62f);
        GUI.DrawTexture(new Rect(0, 0, JinaUI.W, JinaUI.H), v);
        GUI.color = saved;
    }
}
