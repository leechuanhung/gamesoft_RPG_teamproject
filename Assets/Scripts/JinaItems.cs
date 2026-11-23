using UnityEngine;

// 선비가 인벤토리에 넣고 쓸 수 있는 물건(아이콘: Tools/ArtGen/make_items.py).
// 공격력·방어력은 아직 없어서 건드리지 않는다. 회복, 화살 채우기, 한 번 막기,
// 잠시 빨라지기만 한다. 보부상이 전부 판다.
public class JinaItem : ItemData
{
    // 은비녀: 물에 빠져 죽은 어머니의 비녀. 콩쥐 요괴 체력이 절반 아래일 때 가까이서 쓰면 정화한다
    // (KongBoss.TryPurify). 그 밖에서는 지니고만 있는다.
    public enum Effect { Heal, Arrows, Ward, Haste, Keepsake, AttackBoost, BindDefense, StaminaBoost, DefenseBoost, QuestWisp }
    public Effect effect;
    public int amount, price;
    public string id;

    // 아이템 하나를 만든다.
    public static JinaItem Make(string id, string name, string text, Effect effect, int amount, int price, int stack)
    {
        var item = CreateInstance<JinaItem>();
        item.name = id; item.id = id; item.effect = effect; item.amount = amount; item.price = price;
        item.Setup(name, text, JinaArt.Item("item_" + id), stack);
        return item;
    }

    // 선비에게 쓴다: 무슨 일이 있었는지와 소모됐는지(체력이나 화살통이 가득하면 남는다).
    public bool Use(Combat combat, out string message)
    {
        if (effect == Effect.QuestWisp) { message = "도깨비불이 선비와 함께하고 있습니다."; return false; }
        if (effect == Effect.Keepsake)
        {
            if (combat.KongBoss == null || !combat.KongBoss.Fighting) { message = "누군가 아끼던 물건입니다. 지금은 품에 간직할 뿐입니다."; return false; }
            bool done = combat.KongBoss.TryPurify(false, out message);
            if (done) message = "은비녀를 높이 들었습니다. 강물 속 넋이 빛이 되어 독 안으로 스며듭니다…";
            return done;
        }
        if (combat.PlayerHealth <= 0) { message = "쓰러진 몸으로는 쓸 수 없습니다."; return false; }
        switch (effect)
        {
            case Effect.Heal:
                if (combat.PlayerHealth >= Combat.MaxPlayerHealth) { message = "기운이 이미 넘칩니다."; return false; }
                int before = combat.PlayerHealth; combat.RestoreHealth(amount);
                message = ItemName + "을(를) 먹었습니다. 체력 +" + (combat.PlayerHealth - before); return true;
            case Effect.Arrows:
                if (combat.Arrows >= ShopRules.MaxArrows) { message = "화살통이 가득 찼습니다."; return false; }
                int had = combat.Arrows; combat.AddArrows(amount);
                message = "화살통에 화살 " + (combat.Arrows - had) + "대를 채웠습니다."; return true;
            case Effect.AttackBoost:
                JinaItems.RefreshAttack(); message = "산삼의 기운이 돕니다. 60초 동안 공격력 +3"; return true;
            case Effect.BindDefense:
                JinaItems.RefreshRope(combat); message = "포승줄의 기운이 퍼집니다. 현재 필드의 일반 몬스터 방어력 -2 (30초)"; return true;
            case Effect.StaminaBoost:
                JinaItems.RefreshStamina(); message = "약과를 먹었습니다. 60초 동안 스태미너 회복 속도 1.5배"; return true;
            case Effect.DefenseBoost:
                JinaItems.RefreshDefense(); message = "방울이 울립니다. 60초 동안 방어력 +2"; return true;
            case Effect.Ward:
                if (combat.Warded) { message = "이미 액막이 부적을 지니고 있습니다."; return false; }
                combat.GiveWard(); message = "부적을 품었습니다. 다음 공격 한 번을 막아 줍니다."; return true;
            default:
                combat.player.HasteUntil = Time.time + amount;
                message = "짚신을 갈아 신었습니다. " + amount + "초 동안 발이 가벼워집니다."; return true;
        }
    }
}

public static class JinaItems
{
    public static readonly JinaItem[] All =
    {
        JinaItem.Make("misutgaru", "미숫가루", "볶은 곡식 가루 한 주머니. 물에 타 마시면 기운이 조금 돈다.\n체력 +20", JinaItem.Effect.Heal, 20, 4, 10),
        JinaItem.Make("jumeokbap", "주먹밥", "주모가 싸 준 김 두른 주먹밥.\n체력 +40", JinaItem.Effect.Heal, 40, 7, 10),
        JinaItem.Make("cheongsimhwan", "청심환", "금박을 입힌 귀한 환약. 막힌 기운을 단숨에 뚫는다.\n체력 +60", JinaItem.Effect.Heal, 60, 13, 5),
        JinaItem.Make("arrows", "화살 꾸러미", "끈으로 묶은 화살 다섯 대. 화살통이 비었을 때 푼다.\n화살 +5", JinaItem.Effect.Arrows, 5, 5, 10),
        JinaItem.Make("ward", "액막이 부적", "붉은 글씨로 액을 막는 작은 부적.\n다음에 받는 공격 한 번을 막는다", JinaItem.Effect.Ward, 1, 12, 3),
        JinaItem.Make("jipsin", "날랜 짚신", "가볍게 삼은 새 짚신. 오래 신으면 해진다.\n15초 동안 이동이 빨라진다", JinaItem.Effect.Haste, 15, 10, 3),
        JinaItem.Make("sansam", "산삼", "60초 동안 공격력 +3", JinaItem.Effect.AttackBoost, 3, 0, 10),
        JinaItem.Make("rope", "포승줄", "현재 필드 일반 몬스터의 방어력 -2 (30초)", JinaItem.Effect.BindDefense, 2, 0, 10),
        JinaItem.Make("yakgwa", "약과", "60초 동안 스태미너 회복 속도 1.5배", JinaItem.Effect.StaminaBoost, 60, 15, 10),
        JinaItem.Make("bell", "방울", "60초 동안 방어력 +2", JinaItem.Effect.DefenseBoost, 2, 0, 10),
    };
    // 팔지 않는다: 강의 물귀신에게서 풀려난 것(Combat.FellWaterGhost).
    public static readonly JinaItem Keepsake = JinaItem.Make("binyeo", "은비녀", "누군가 아끼던 은비녀. 강물에 오래 잠겨 있었다. 물귀신들 사이에서 한 여인의 넋이 남기고 갔다.\n요괴의 기운이 절반 아래로 꺾였을 때 가까이에서 쓰면(F) 사로잡힌 이를 되찾는다", JinaItem.Effect.Keepsake, 0, 0, 1);
    public static readonly JinaItem Wisp = JinaItem.Make("dokkabibol", "동행 도깨비불", "선돌에서 만난 작은 도깨비불. 숲의 주인에게 데려다주기로 했다.", JinaItem.Effect.QuestWisp, 0, 0, 1);
    public static JinaItem Get(string id) => System.Array.Find(All, i => i.id == id);
    // 기존 여섯 물건과 약과만 판매한다. 드랍/퀘스트 전용 아이템은 상점 목록에 포함하지 않는다.
    public static readonly JinaItem[] ForSale = { All[0], All[1], All[2], All[3], All[4], All[5], All[8] };

    // 퀘스트는 아이템 ID와 수량만 전달하면 된다. 효과와 수량 처리는 여기와 기존 보따리가 담당한다.
    public static bool Grant(string id, int amount = 1)
    {
        var item = Get(id);
        return item != null && Inventory != null && Inventory.TryAdd(item, amount);
    }

    static float attackUntil, staminaUntil, defenseUntil, ropeUntil;
    static int ropeMap = -1;
    static readonly System.Collections.Generic.HashSet<Camp.Mob> ropedGhosts = new System.Collections.Generic.HashSet<Camp.Mob>();
    static readonly System.Collections.Generic.HashSet<RoadMobs.Mob> ropedRoad = new System.Collections.Generic.HashSet<RoadMobs.Mob>();
    public static int AttackBonus(Combat combat) => combat != null && combat.PlayerHealth > 0 && Time.time < attackUntil ? 3 : 0;
    public static int DefenseBonus(Combat combat) => combat != null && combat.PlayerHealth > 0 && Time.time < defenseUntil ? 2 : 0;
    public static float StaminaMultiplier(Player player) => player != null && !player.Downed && Time.time < staminaUntil ? 1.5f : 1f;
    public static void RefreshAttack() { attackUntil = Time.time + 60f; }
    public static void RefreshStamina() { staminaUntil = Time.time + 60f; }
    public static void RefreshDefense() { defenseUntil = Time.time + 60f; }
    public static void RefreshRope(Combat combat)
    {
        int map = combat.Maps != null ? combat.Maps.Current : Maps.Index(combat.player.transform.position.x);
        // 같은 필드에서 다시 쓰면 기존 대상은 유지하고, 새로 생성된 대상은 추가하지 않는다.
        if (Time.time < ropeUntil && ropeMap == map) { ropeUntil = Time.time + 30f; return; }
        ropedGhosts.Clear(); ropedRoad.Clear(); ropeMap = map;
        foreach (var camp in combat.Camps)
            if (camp != null)
                foreach (var mob in camp.mobs)
                    if (mob != null && mob.hp > 0 && mob.art != null && Maps.Index(mob.art.transform.position.x) == map)
                        ropedGhosts.Add(mob);
        if (combat.Mobs != null)
            foreach (var mob in combat.Mobs.mobs)
                if (mob.Alive && !mob.soldier && !mob.witness && Maps.Index(mob.pos.x) == map)
                    ropedRoad.Add(mob);
        ropeUntil = Time.time + 30f;
    }
    public static int RopeDefense(Camp.Mob mob, int defense, Combat combat) =>
        RopeActive(combat) && ropedGhosts.Contains(mob) ? Mathf.Max(0, defense - 2) : defense;
    public static int RopeDefense(RoadMobs.Mob mob, int defense, Combat combat) =>
        RopeActive(combat) && ropedRoad.Contains(mob) ? Mathf.Max(0, defense - 2) : defense;
    static bool RopeActive(Combat combat) => combat != null && combat.PlayerHealth > 0 && Time.time < ropeUntil
        && (combat.Maps != null ? combat.Maps.Current : Maps.Index(combat.player.transform.position.x)) == ropeMap;
    public static void ForgetMob(Camp.Mob mob) { ropedGhosts.Remove(mob); }
    public static void ForgetMob(RoadMobs.Mob mob) { ropedRoad.Remove(mob); }
    public static void UpdateEffects(Combat combat)
    {
        if (combat == null || combat.PlayerHealth <= 0) { ClearEffects(); return; }
        if (ropeMap >= 0 && (Time.time >= ropeUntil || (combat.Maps != null && combat.Maps.Current != ropeMap)))
        { ropeUntil = 0; ropeMap = -1; ropedGhosts.Clear(); ropedRoad.Clear(); }
    }
    public static void ClearEffects()
    {
        attackUntil = staminaUntil = defenseUntil = ropeUntil = 0;
        ropeMap = -1; ropedGhosts.Clear(); ropedRoad.Clear();
    }
    // 처치마다 독립적으로 각각 1%를 판정하며 기존의 획득 문구처럼 텍스트만 표시한다.
    public static void RollDrops(Vector3 at)
    {
        foreach (var id in new[] { "sansam", "rope" })
            if (Random.value < .01f && Grant(id))
                JinaFx.Word(at + Vector3.up * 1.3f, Get(id).ItemName + " 획득!", new Color(1f, .9f, .5f), 20);
    }

    // 팀원의 InventorySystem이 장면에 두는 인벤토리(없는 장면에서는 null).
    static InventoryManager inventory;
    public static InventoryManager Inventory => inventory != null ? inventory : inventory = Object.FindObjectOfType<InventoryManager>();
}
