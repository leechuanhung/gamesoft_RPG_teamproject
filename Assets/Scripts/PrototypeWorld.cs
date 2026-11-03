using UnityEngine;

// Three connected areas keep quest state while the player travels between them.
public class PrototypeWorld : MonoBehaviour
{
    public const float VillageLeft = -12f;
    public const float ForestStart = 28f;
    public const float EnemyHome = 46f;
    public const float EnemyMin = -10f;
    public const float GateX = -40f;
    public const float EnemyMax = 58f;
    public const float GoalX = 60f;
    public const float DetectionRange = 18f;
    public static readonly Vector2 Spawn = new Vector2(0, .3f);
    // East of the peddler, so a boss coming into the village reaches the elder first.
    public static readonly Vector2 Elder = new Vector2(21.5f, .2f);
    public static readonly Vector2 Tavern = new Vector2(10, .2f);
    public static readonly Vector2 Peddler = new Vector2(17.5f, .2f);
    public static readonly Vector2 Stone = new Vector2(-57, .5f);

    public static string Area(float x) => x < GateX ? "옛 비석 터" : x < VillageLeft ? "일반 몬스터 지역" : x >= ForestStart ? "보스 지역" : "리스폰 마을";
    public static bool CanDetect(Vector2 player, Vector2 enemy) => player.x >= ForestStart
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
        Tile("비석 배경", -55, 4, 30, 10, new Color(.17f,.18f,.28f), sprite, false, -20);
        Tile("일반 지역 배경", -26, 4, 28, 10, new Color(.3f,.2f,.2f), sprite, false, -20);
        Tile("마을 배경", 8, 4, 40, 10, new Color(.24f,.3f,.34f), sprite, false, -20);
        Tile("숲 배경", 46, 4, 36, 10, new Color(.1f,.23f,.2f), sprite, false, -20);
        Tile("왼쪽 끝", -70.5f, 3, 1, 8, new Color(.3f,.3f,.38f), sprite, true);
        Tile("오른쪽 끝", 64.5f, 3, 1, 8, new Color(.18f,.3f,.23f), sprite, true);
        Tile("한양으로 가는 길", GoalX, 1, .4f, 3, new Color(1,.75f,.2f), sprite, false);
        for (int i = 0; i < 5; i++) Prop("숲 소나무", "pine", 32 + i * 6, -10, i % 2 == 1);
        Prop("마을 기와집", "house", 4.5f, -10);
        // The tavern door sits about one unit left of the building's centre, behind the keeper.
        Prop("주막", "tavern", 11, -8);
        Prop("주막 주모", "keeper", Tavern.x, 1);
        Prop("주막 평상", "bench", 14, -5);
        Prop("거북 받침 비석", "stele", Stone.x, 0);
    }

    // Pixel-art props stand on the ground surface (y = -0.5).
    void Prop(string label, string art, float x, int order, bool flip = false)
    {
        var prop = new GameObject(label);
        prop.transform.SetParent(transform);
        prop.transform.position = new Vector3(x, -.5f, 0);
        var image = prop.AddComponent<SpriteRenderer>();
        image.sprite = JinaArt.Prop(art);
        image.sortingOrder = order;
        image.flipX = flip;
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
