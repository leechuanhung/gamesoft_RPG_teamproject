using UnityEngine;

// The things the seonbi can carry in the inventory and use from it (icons: Tools/ArtGen/make_items.py).
// None of them touch attack or defence, which do not exist yet; they heal, refill the quiver, ward off one blow
// or quicken his feet for a while. The peddler sells them all.
public class JinaItem : ItemData
{
    // Keepsake: a story item; it does nothing yet (the kongjwi boss in 기획서.md will use it).
    public enum Effect { Heal, Arrows, Ward, Haste, Keepsake }
    public Effect effect;
    public int amount, price;
    public string id;

    public static JinaItem Make(string id, string name, string text, Effect effect, int amount, int price, int stack)
    {
        var item = CreateInstance<JinaItem>();
        item.name = id; item.id = id; item.effect = effect; item.amount = amount; item.price = price;
        item.Setup(name, text, JinaArt.Item("item_" + id), stack);
        return item;
    }

    // Uses it on the seonbi: what happened, and whether it was spent (a full belly or quiver keeps it).
    public bool Use(PrototypeCombat combat, out string message)
    {
        if (effect == Effect.Keepsake) { message = "누군가 아끼던 물건입니다. 지금은 품에 간직할 뿐입니다."; return false; }
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
    // Not for sale: freed from the river's ghosts (PrototypeCombat.FellWaterGhost).
    public static readonly JinaItem Keepsake = JinaItem.Make("binyeo", "은비녀", "누군가 아끼던 은비녀. 강물에 오래 잠겨 있었다. 물귀신들 사이에서 한 여인의 넋이 남기고 갔다.\n아직 쓸 곳을 모른다", JinaItem.Effect.Keepsake, 0, 0, 1);
    public static JinaItem Get(string id) => System.Array.Find(All, i => i.id == id);

    // The inventory the teammate's InventorySystem keeps in the scene (null in scenes without it).
    static InventoryManager inventory;
    public static InventoryManager Inventory => inventory != null ? inventory : inventory = Object.FindObjectOfType<InventoryManager>();
}
