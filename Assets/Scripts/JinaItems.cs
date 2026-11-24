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
        JinaItem.Make("misutgaru", "미숫가루", "볶은 곡식을 곱게 빻아 담은 주머니. 먼 길 떠나는 자식의 봇짐에 어미가 넣어 주던 것이다. 길 위에서 쓰러진 선비들의 봇짐에도 빈 주머니만은 남아 있었다고 한다.\n체력 +20", JinaItem.Effect.Heal, 20, 4, 10),
        JinaItem.Make("jumeokbap", "주먹밥", "주막 주모가 김을 둘러 꼭꼭 뭉친 주먹밥. 고개 너머로 떠나는 길손마다 하나씩 쥐여 보냈다. 주모는 돌아오는 얼굴을 세지 않는다. 세다 보면 밥을 지을 수 없기 때문이다.\n체력 +40", JinaItem.Effect.Heal, 40, 7, 10),
        JinaItem.Make("cheongsimhwan", "청심환", "금박을 입힌 귀한 환약. 본디 궁중 내의원에서만 빚던 것이다. 어린 임금이 즉위한 해부터 저잣거리에 흘러나오기 시작했는데, 그 값을 누가 치렀는지는 아무도 묻지 않았다.\n체력 +60", JinaItem.Effect.Heal, 60, 13, 5),
        JinaItem.Make("arrows", "화살 꾸러미", "삼끈으로 묶은 화살 다섯 대. 깃은 봉화 산성 수비군이 쓰던 꿩 깃이다. 봉홧불이 꺼진 뒤로 산성의 화살은 쏠 곳을 잃고 장터로 흘러나왔다.\n화살 +5", JinaItem.Effect.Arrows, 5, 5, 10),
        JinaItem.Make("ward", "액막이 부적", "붉은 주사로 글자를 그린 작은 부적. 서낭당 무녀가 길손에게 나누어 주던 것이다. 액은 막아도 업은 막지 못하니, 한 번 막고 나면 스스로 타 버린다.\n다음에 받는 공격 한 번을 막는다", JinaItem.Effect.Ward, 1, 12, 3),
        JinaItem.Make("jipsin", "날랜 짚신", "볏짚을 촘촘히 삼은 새 짚신. 걸음이 가벼운 것은 신이 좋아서가 아니라, 아직 길의 무게를 모르기 때문이다. 오래 신으면 해진다.\n15초 동안 이동이 빨라진다", JinaItem.Effect.Haste, 15, 10, 3),
        JinaItem.Make("sansam", "산삼", "깊은 산 서낭목 아래에서 캔 오래된 뿌리. 심마니들은 산삼이 사람을 고른다고 믿었다. 산의 주인이 미혹된 뒤로, 산삼은 더는 사람을 고르지 않는다.\n60초 동안 공격력 +3", JinaItem.Effect.AttackBoost, 3, 0, 10),
        JinaItem.Make("rope", "포승줄", "관아에서 죄인을 묶던 붉은 포승. 묶이는 것은 몸만이 아니어서, 이 줄 앞에서는 요괴의 살갗도 무르게 된다. 좌의정 댁 곳간에는 쓰지 않은 포승이 산처럼 쌓여 있다고 한다.\n현재 필드 일반 몬스터의 방어력 -2 (30초)", JinaItem.Effect.BindDefense, 2, 0, 10),
        JinaItem.Make("yakgwa", "약과", "꿀과 참기름으로 지진 약과. 제사상에 오르던 것이라, 먹는 이의 숨에 조상의 기운이 깃든다고 한다. 북망산 무덤마다 약과 부스러기가 흩어져 있는 까닭이다.\n60초 동안 스태미너 회복 속도 1.5배", JinaItem.Effect.StaminaBoost, 60, 15, 10),
        JinaItem.Make("bell", "방울", "무당이 굿판에서 흔들던 놋방울. 방울 소리가 닿는 곳까지는 잡귀가 함부로 들지 못한다. 소리가 그친 뒤의 일은 무당도 장담하지 않았다.\n60초 동안 방어력 +2", JinaItem.Effect.DefenseBoost, 2, 0, 10),
    };
    // 팔지 않는다: 강의 물귀신에게서 풀려난 것(Combat.FellWaterGhost).
    public static readonly JinaItem Keepsake = JinaItem.Make("binyeo", "은비녀", "콩밭 마을 부잣집 안주인이 강으로 나가던 날 꽂고 있던 은비녀. 물귀신들 사이에서 한 여인의 넋이 내어 주었다. 그날 밤 이후 그 집 맏딸은 이 비녀 곁에 다가서지 못한다. 강물에 잠긴 어미의 마음이 아직 은빛에 남은 까닭이다.\n요괴의 기운이 절반 아래로 꺾였을 때 가까이에서 쓰면(F) 사로잡힌 이를 되찾는다", JinaItem.Effect.Keepsake, 0, 0, 1);
    public static readonly JinaItem Wisp = JinaItem.Make("dokkabibol", "동행 도깨비불", "선돌 곁에서 떨고 있던 작은 도깨비불. 숲의 주인이 미혹되던 밤, 홀로 길을 잃었다. 도깨비불은 주인을 잊지 않는다. 다만 주인이 저를 잊었을까 두려워할 뿐이다.\n숲의 주인에게 데려다주기로 했다.", JinaItem.Effect.QuestWisp, 0, 0, 1);
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
