using System.Collections.Generic;
using UnityEngine;

// 땅에 떨어진 무기 (환도·활).
// 몹이 쓰러진 자리에서 땅으로 떨어진 뒤 작게 떠서 둥실거리고,
// 선비가 가까이 오면 주워서 보따리(무기 탭)에 들어간다. 떨어지고 0.5초 동안은 못 줍는다.
public class WeaponPickup : MonoBehaviour
{
    public const float PickupDelay = .5f;   // 떨어진 뒤 못 줍는 시간(초)
    public const float PickupRange = .8f;   // 이만큼 가까이 가면 주움(칸)
    public const float Size = .55f;         // 땅 위에 보이는 크기(칸)
    public const float FloatHeight = .35f;  // 땅에서 떠 있는 높이(칸)

    // 지금 땅에 떨어져 있는 무기들
    static readonly List<WeaponPickup> lying = new List<WeaponPickup>();
    public static bool Lying(string id) => lying.Exists(p => p != null && p.id == id);
    // 이미 보따리에 있는 무기인지
    public static bool Owned(string id)
    {
        var ui = FindObjectOfType<InventoryUI>();
        return ui == null || ui.HasWeapon(id);
    }
    // 재시작할 때 떨어진 무기 모두 치우기
    public static void ClearAll()
    {
        foreach (var p in lying.ToArray()) if (p != null) Destroy(p.gameObject);
        lying.Clear();
    }

    private string id;
    private Combat combat;
    private SpriteRenderer art;
    private float droppedAt, startY, restY;

    // at 위치(몹이 쓰러진 곳)에 무기를 떨어뜨림
    public static void Drop(Combat combat, string id, Vector3 at)
    {
        var obj = new GameObject("떨어진 무기 " + id);
        var p = obj.AddComponent<WeaponPickup>();
        p.id = id;
        p.combat = combat;
        p.droppedAt = Time.time;
        p.art = obj.AddComponent<SpriteRenderer>();
        var stats = Weapon.Get(id);
        p.art.sprite = JinaArt.Load(stats.ranged ? (stats.bowArt ?? "04_Weapons/bow") : stats.art ?? "04_Weapons/hwando");
        p.art.sortingOrder = 4;
        // 그림이 몇 픽셀이든 Size 칸에 맞게 줄임
        var size = p.art.sprite.bounds.size;
        obj.transform.localScale = Vector3.one * (Size / Mathf.Max(size.x, size.y));
        // 환도는 칼날이 40° 들려 그려져 있어서 눕히고, 활은 옆으로 눕힘
        obj.transform.rotation = Quaternion.Euler(0, 0, stats.ranged ? -90 : -40);
        // 아래쪽 땅(레이어 8) 찾기
        var hit = Physics2D.Raycast(at, Vector2.down, 30, 1 << 8);
        float ground = hit.collider != null ? hit.point.y : at.y - 1;
        p.startY = at.y;
        p.restY = ground + FloatHeight;
        obj.transform.position = at;
        lying.Add(p);
        JinaFx.Glint(at, new Color(1, .9f, .5f));
    }

    void Update()
    {
        if (combat == null || combat.player == null) return;
        float t = Time.time - droppedAt;
        // 0.35초 동안 땅으로 떨어진 뒤, 위아래로 살짝 둥실거림
        float fall = Mathf.Clamp01(t / .35f);
        float y = Mathf.Lerp(startY, restY, fall * fall) + (fall >= 1 ? Mathf.Sin(t * 3f) * .06f : 0);
        transform.position = new Vector3(transform.position.x, y, 0);

        // 떨어지고 0.5초가 지나야 주울 수 있음
        if (t < PickupDelay || combat.Paused || combat.PlayerHealth <= 0) return;
        var body = combat.player.GetComponent<SpriteRenderer>().bounds;
        bool near = Mathf.Abs(body.center.x - transform.position.x) < PickupRange
                 && transform.position.y > body.min.y - .5f && transform.position.y < body.max.y + .5f;
        if (!near) return;

        var ui = FindObjectOfType<InventoryUI>();
        if (ui == null || !ui.GiveWeapon(id)) return;
        string name = Weapon.Get(id).name;
        JinaFx.Glint(transform.position, new Color(1, .9f, .5f));
        GameAudio.Play(GameAudio.Sfx.Rare);
        JinaFx.Word(transform.position + Vector3.up * .8f, name + " 획득!", new Color(1f, .9f, .5f), 22);
        Destroy(gameObject);
    }

    void OnDestroy() { lying.Remove(this); }
}