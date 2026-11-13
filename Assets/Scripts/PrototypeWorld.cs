using UnityEngine;

// Connected areas keep quest state while the player travels between them (PrototypeMaps shows each as its own map).
public class PrototypeWorld : MonoBehaviour
{
    public const float VillageLeft = -12f;
    public const float ForestStart = 28f;
    public const float EnemyHome = 46f;
    public const float EnemyMin = -10f;
    public const float GateX = -40f;
    public const float EnemyMax = 58f;
    // The ferry crossing east of the forest: a near bank, the river (crossed by boat), the far bank with the road.
    public const float FerryStart = 64f, RiverLeft = 80f, RiverRight = 100f, FerryEnd = 110f;
    // The mountain pass beyond the ferry: steps up to the shrine plateau and down again to the road to Hanyang.
    // Kongbat village across the river, where Kongjwi and Patjwi live (PrototypeKongVillage).
    public const float KongStart = FerryEnd, KongEnd = 150f;
    public static readonly Vector2 Kongjwi = new Vector2(133f, .2f), Patjwi = new Vector2(135.2f, .2f),
        Anak = new Vector2(116.5f, .2f), Namu = new Vector2(123f, .2f),
        Hunjang = new Vector2(140.4f, .2f), Yeot = new Vector2(130.2f, .2f), Halmi = new Vector2(121.3f, .2f);
    // Where Patjwi runs when the demon comes into the village: the far left, so she is the last it reaches.
    public const float PatjwiRaidX = 112.4f;
    public const float JangdokX = 146.5f, FeastListX = 143.9f, KongHouseX = 141.5f;
    public const float PassStart = KongEnd, PassEnd = 198f, ShrineX = 180.5f, RockStart = 185.5f;
    public static readonly (float left, float right, float top)[] PassSteps =
        { (150, 162, -.5f), (162, 168, 1f), (168, 186, 2.5f), (186, 190, 1f), (190, 198, -.5f) };
    // The back hill beyond the pass, where Kongjwi waits (PrototypeKongBoss): uneven ground, many ledges stacked high,
    // and great crocks that stop what she throws. The road to Hanyang (the golden marker) is at its far end.
    public const float HillStart = PassEnd, HillEnd = 246f, KongLair = 231f;
    public static readonly (float left, float right, float top)[] HillSteps =
        { (198, 204, -.5f), (204, 210, .7f), (210, 221, -.5f), (221, 225, 1.1f), (225, 232, -.5f), (232, 236, .6f), (236, 246, -.5f) };
    // (centre x, top, art): three tiers, each about 1.7 above the one below, so they can be climbed one by one.
    public static readonly (float x, float top, string art)[] HillLedges =
    {
        (201.5f, 1.4f, "ledge_stone"), (205.5f, 2.4f, "ledge_roof"), (209f, 4f, "ledge_log"),
        (213f, 1.3f, "ledge_stone"), (216.5f, 2.9f, "ledge_roof"), (213.5f, 4.5f, "ledge_stone"),
        (223f, 2.8f, "ledge_log"), (227.5f, 1.3f, "ledge_roof"), (231f, 2.9f, "ledge_stone"), (227.5f, 4.5f, "ledge_log"),
        (239.5f, 1.3f, "ledge_roof"), (242.5f, 3f, "ledge_stone"),
    };
    // The crocks standing about the hill as cover.
    public static readonly float[] HillCrocks = { 207.5f, 218.5f, 229.5f, 238f };
    public const float GoalX = 243f;
    public static readonly Vector2 Ferryman = new Vector2(77f, .2f);
    // The water strip's bottom; its surface sits a little below the banks and the boat's deck.
    public const float WaterBottom = -1.5f;
    public const float DetectionRange = 18f;
    public static readonly Vector2 Spawn = new Vector2(0, .3f);
    // East of the peddler, so a boss coming into the village reaches the elder first.
    public static readonly Vector2 Elder = new Vector2(21.5f, .2f);
    public static readonly Vector2 Tavern = new Vector2(10, .2f);
    public static readonly Vector2 Peddler = new Vector2(17.5f, .2f);
    public static readonly Vector2 Stone = new Vector2(-57, .5f);
    // Ledges to jump onto: (centre x, top surface y). Jumps clear about 2 units, so each tier is ~1.7 above the last.
    public static readonly Vector2[] CampLedges = { new Vector2(-16, 1.2f), new Vector2(-21, 2.9f), new Vector2(-27, 1.2f), new Vector2(-32, 2.7f), new Vector2(-36.5f, 1.3f) };
    // Forest ledges sit in the gaps between the pines (x 32 + 6n).
    public static readonly Vector2[] ForestLedges = { new Vector2(35, 1.3f), new Vector2(41, 2.9f), new Vector2(47, 1.3f), new Vector2(53, 2.8f), new Vector2(58.5f, 1.4f) };
    public const float LedgeWidth = 3f;
    // Each area's centre and its background art, behind the props (far drifts with the camera, near stays).
    static readonly (string art, float x)[] Backdrops = { ("stele", -55), ("camp", -26), ("village", 8), ("forest", 46), ("ferry", 87), ("kong", 130), ("pass", 174), ("pass", 222) };
    // What each map stands on and under: its name in the art, and its span.
    static readonly (string art, float left, float right)[] Lands = { ("stele", -70.5f, GateX), ("camp", GateX, VillageLeft), ("village", VillageLeft, ForestStart), ("forest", ForestStart, FerryStart), ("ferry", FerryStart, FerryEnd), ("kong", KongStart, KongEnd), ("pass", PassStart, PassEnd), ("hill", HillStart, HillEnd + 1) };
    // The walking surface sits this many dots below the top of a ground strip (grass stands above it).
    const float GroundLip = 6;

    public static string Area(float x) => x < GateX ? "옛 비석 터" : x < VillageLeft ? "일반 몬스터 지역" : x >= HillStart ? "뒷산" : x >= PassStart ? "고개와 서낭당" : x >= KongStart ? "콩밭 마을" : x >= FerryStart ? "나루터" : x >= ForestStart ? "보스 지역" : "리스폰 마을";
    public static bool CanDetect(Vector2 player, Vector2 enemy) => player.x >= ForestStart && player.x < FerryStart
        && Mathf.Abs(player.x - enemy.x) <= DetectionRange && Mathf.Abs(player.y - enemy.y) < 4f;

    public void Build(Sprite sprite)
    {
        foreach (var name in new[] { "Ground", "Step", "Island", "Finish Ground", "Goal" })
        {
            var old = GameObject.Find(name);
            if (old != null) old.SetActive(false);
        }
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
        Tile("오른쪽 끝", HillEnd + .5f, 3, 1, 8, new Color(.4f,.38f,.3f), sprite, true);
        Tile("한양으로 가는 길", GoalX, 1, .4f, 3, new Color(1,.75f,.2f), sprite, false);
        for (int i = 0; i < 5; i++) Prop("숲 소나무", "pine", 32 + i * 6, -10, i % 2 == 1);
        Prop("마을 기와집", "house", 4.5f, -10);
        // The tavern door sits about one unit left of the building's centre, behind the keeper.
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
        // The river: water strips just below the banks, between them (it has no floor; see PrototypeFerry).
        for (float x = RiverLeft + 2; x < RiverRight; x += 4) Prop("강물", "water", x, 6).transform.position = new Vector3(x, WaterBottom, 0);
        foreach (var (art, x) in Backdrops)
        {
            Prop("원경 " + art, "bg_" + art + "_far", x, -19).gameObject.AddComponent<PrototypeParallax>().home = x;
            Prop("근경 " + art, "bg_" + art + "_near", x, -18);
        }
        foreach (var (art, left, right) in Lands)
        {
            Sky(art == "hill" ? "pass" : art, left, right);
            if (art == "pass") foreach (var (l, r, top) in PassSteps) Land(art, l, r, top);
            else if (art == "hill") foreach (var (l, r, top) in HillSteps) Land("pass", l, r == HillEnd ? r + 1 : r, top);
            else if (art == "ferry") { Land(art, left, RiverLeft, -.5f); Land(art, RiverRight, right, -.5f); River(); }
            else Land(art, left, right, -.5f);
        }
        foreach (var ledge in CampLedges) Ledge("바위 발판", "ledge_stone", ledge);
        foreach (var ledge in ForestLedges) Ledge("통나무 발판", "ledge_log", ledge);
        foreach (var (x, top, art) in HillLedges) Ledge("뒷산 발판", art, new Vector2(x, top));
        foreach (var x in HillCrocks) Prop("뒷산 독", "crock", x, -4).transform.position = new Vector3(x, Top(x), 0);
    }

    // The ground's picture over its collider: earth down past the bottom of the screen, grass on top.
    void Land(string art, float left, float right, float top)
    {
        var land = new GameObject("지면 " + art).AddComponent<SpriteRenderer>();
        land.transform.SetParent(transform);
        land.sprite = JinaArt.Strip("ground_" + art);
        land.drawMode = SpriteDrawMode.Tiled;
        land.size = new Vector2(right - left, land.sprite.bounds.size.y);
        land.transform.position = new Vector3((left + right) / 2, top + GroundLip / JinaArt.PropPixelsPerUnit, 0);
        land.sortingOrder = 2;
    }
    // The map's sky: a column of colour stretched across it, its horizon near the ground.
    void Sky(string art, float left, float right)
    {
        var sky = new GameObject("하늘 " + art).AddComponent<SpriteRenderer>();
        sky.transform.SetParent(transform);
        sky.sprite = JinaArt.Prop("sky_" + art);
        sky.transform.position = new Vector3((left + right) / 2, -4f, 0);
        sky.transform.localScale = new Vector3((right - left) * JinaArt.PropPixelsPerUnit, 1, 1);
        sky.sortingOrder = -21;
    }
    // The dark water under the river's surface strip.
    void River()
    {
        var deep = new GameObject("깊은 강물").AddComponent<SpriteRenderer>();
        deep.transform.SetParent(transform);
        deep.sprite = JinaArt.Strip("river_deep");
        deep.drawMode = SpriteDrawMode.Tiled;
        deep.size = new Vector2(RiverRight - RiverLeft, deep.sprite.bounds.size.y);
        deep.transform.position = new Vector3((RiverLeft + RiverRight) / 2, WaterBottom + .1f, 0);
        deep.sortingOrder = 5;
    }

    // A one-way ledge: jump up through it from below, land on its top.
    void Ledge(string label, string art, Vector2 top)
    {
        var image = Prop(label, art, top.x, -3);
        // The drawn lip sits two art pixels below the top of the picture.
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

    // The ground's top at x on the pass and the back hill (the banks and the other maps are at -0.5).
    public static float Top(float x)
    {
        foreach (var (left, right, top) in PassSteps) if (x >= left && x < right) return top;
        foreach (var (left, right, top) in HillSteps) if (x >= left && x < right) return top;
        return -.5f;
    }

    // Pixel-art props stand on the ground surface (y = -0.5).
    SpriteRenderer Prop(string label, string art, float x, int order, bool flip = false)
    {
        var prop = new GameObject(label);
        prop.transform.SetParent(transform);
        prop.transform.position = new Vector3(x, -.5f, 0);
        var image = prop.AddComponent<SpriteRenderer>();
        image.sprite = JinaArt.Prop(art);
        image.sortingOrder = order;
        image.flipX = flip;
        return image;
    }

    void Tile(string label, float x, float y, float width, float height, Color color, Sprite sprite, bool solid, int order = 0)
    {
        var tile = new GameObject(label);
        tile.transform.SetParent(transform);
        tile.transform.position = new Vector3(x, y, 0);
        tile.transform.localScale = new Vector3(width, height, 1);
        var art = tile.AddComponent<SpriteRenderer>();
        art.sprite = sprite;
        art.color = color;
        art.sortingOrder = order;
        if (solid) { tile.layer = 8; tile.AddComponent<BoxCollider2D>(); }
    }
}
