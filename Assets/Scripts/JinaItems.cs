using UnityEngine;

// 선비가 인벤토리에 넣고 쓸 수 있는 물건(아이콘: Tools/ArtGen/make_items.py).
// 공격력·방어력은 아직 없어서 건드리지 않는다. 회복, 화살 채우기, 한 번 막기,
// 잠시 빨라지기만 한다. 보부상이 전부 판다.
public class JinaItem : ItemData
{
    // 은비녀: 물에 빠져 죽은 어머니의 비녀. 콩쥐 요괴 체력이 절반 아래일 때 가까이서 쓰면 정화한다
    // (KongBoss.TryPurify). 그 밖에서는 지니고만 있는다.
    public enum Effect { Heal, Arrows, Ward, Haste, Keepsake }
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
    public bool Use(PrototypeCombat combat, out string message)
    {
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
                if (combat.PlayerHealth >= PrototypeCombat.MaxPlayerHealth) { message = "기운이 이미 넘칩니다."; return false; }
                int before = combat.PlayerHealth; combat.RestoreHealth(amount);
                message = ItemName + "을(를) 먹었습니다. 체력 +" + (combat.PlayerHealth - before); return true;
            case Effect.Arrows:
                if (combat.Arrows >= ShopRules.MaxArrows) { message = "화살통이 가득 찼습니다."; return false; }
                int had = combat.Arrows; combat.AddArrows(amount);
                message = "화살통에 화살 " + (combat.Arrows - had) + "대를 채웠습니다."; return true;
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
    };
    // 팔지 않는다: 강의 물귀신에게서 풀려난 것(PrototypeCombat.FellWaterGhost).
    public static readonly JinaItem Keepsake = JinaItem.Make("binyeo", "은비녀", "누군가 아끼던 은비녀. 강물에 오래 잠겨 있었다. 물귀신들 사이에서 한 여인의 넋이 남기고 갔다.\n요괴의 기운이 절반 아래로 꺾였을 때 가까이에서 쓰면(F) 사로잡힌 이를 되찾는다", JinaItem.Effect.Keepsake, 0, 0, 1);
    public static JinaItem Get(string id) => System.Array.Find(All, i => i.id == id);

    // 팀원의 InventorySystem이 장면에 두는 인벤토리(없는 장면에서는 null).
    static InventoryManager inventory;
    public static InventoryManager Inventory => inventory != null ? inventory : inventory = Object.FindObjectOfType<InventoryManager>();
}
