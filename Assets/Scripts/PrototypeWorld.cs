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
    public static readonly Vector2 Elder = new Vector2(-3, .2f);
    public static readonly Vector2 Tavern = new Vector2(10, .2f);
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
        for (int i = 0; i < 5; i++)
        {
            Tile("숲 나무", 32 + i * 6, 2, .6f, 5, new Color(.15f,.25f,.21f), sprite, false, -10);
            Tile("숲 수관", 32 + i * 6, 4.2f, 3.5f, 2.5f, new Color(.13f,.3f,.23f), sprite, false, -9);
        }
        Tile("마을 집", 5, 1, 4, 3, new Color(.4f,.35f,.3f), sprite, false, -10);
        Tile("마을 지붕", 5, 2.7f, 5, .6f, new Color(.22f,.24f,.28f), sprite, false, -9);
        Tile("주막 건물", 11, 1.3f, 6, 3.6f, new Color(.6f,.42f,.25f), sprite, false, -8);
        Tile("주막 초가지붕", 11, 3.3f, 7, .8f, new Color(.7f,.58f,.29f), sprite, false, -7);
        Tile("주막 문", 10, .6f, 1.4f, 2.2f, new Color(.24f,.17f,.12f), sprite, false, -6);
        Tile("주막 주인", Tavern.x, .2f, .75f, 1.4f, new Color(.92f,.7f,.4f), sprite, false, 1);
        Tile("주막 평상", 14, -.2f, 2, .6f, new Color(.45f,.28f,.16f), sprite, false, -5);
        Tile("비석 받침", Stone.x, -.25f, 2.4f, .5f, new Color(.5f,.49f,.57f), sprite, false);
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
