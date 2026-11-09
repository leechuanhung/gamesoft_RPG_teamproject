// Prices and stock follow the moral state, shared by the tavern and the peddler.
public static class ShopRules
{
    public const int MaxArrows = 20, ArrowPrice = 1, BundleSize = 10, BundlePrice = 9;
    public const int BannedWhenElderDead = 1, SpecialMenu = 2;
    public static int Honor(QuestProgress quest) => quest == null ? 0 : quest.Honor;
    public static bool ElderDead(QuestProgress quest) => quest != null && quest.ElderDead;
    // Slaying the boss gives +10 honor, purifying it +30.
    public static int Discount(QuestProgress quest) => Honor(quest) >= 30 ? 20 : Honor(quest) >= 10 ? 10 : 0;
    // Round to the nearest coin, ties in the buyer's favour, never below 1 mun.
    public static int Price(int basePrice, QuestProgress quest) =>
        System.Math.Max(1, (basePrice * (100 - Discount(quest)) + 49) / 100);
    public static bool MenuOpen(int menu, QuestProgress quest) =>
        menu == BannedWhenElderDead ? !ElderDead(quest) : menu != SpecialMenu || Honor(quest) >= 30;
    public static bool BundleOpen(QuestProgress quest) => !ElderDead(quest);
    // After the village's misfortune the peddler keeps his charms packed; food and arrows he still sells.
    public static bool ItemOpen(JinaItem item, QuestProgress quest) =>
        !ElderDead(quest) || item.effect == JinaItem.Effect.Heal || item.effect == JinaItem.Effect.Arrows;
}

public class TavernWallet
{
    public static readonly string[] MenuNames = { "보리밥과 김치 한 상", "장국밥 한 그릇", "삼계탕 한 그릇" };
    public static readonly int[] MenuPrices = { 5, 10, 15 }, MenuHeals = { 40, 80, 100 };
    public int Coins { get; private set; } = 30;
    public bool Buy(int menu, int health, out int restored, QuestProgress quest = null)
    {
        restored = 0;
        if (menu < 0 || menu >= MenuPrices.Length || !ShopRules.MenuOpen(menu, quest) || health <= 0 || health >= PrototypeCombat.MaxPlayerHealth) return false;
        int cost = ShopRules.Price(MenuPrices[menu], quest);
        if (Coins < cost) return false;
        restored = System.Math.Min(PrototypeCombat.MaxPlayerHealth - health, MenuHeals[menu]);
        Coins -= cost;
        return true;
    }
    public bool BuyArrows(bool bundle, int carried, out int bought, QuestProgress quest = null)
    {
        bought = 0;
        if (bundle && !ShopRules.BundleOpen(quest)) return false;
        int count = bundle ? ShopRules.BundleSize : 1;
        if (carried + count > ShopRules.MaxArrows) return false;
        int cost = ShopRules.Price(bundle ? ShopRules.BundlePrice : ShopRules.ArrowPrice, quest);
        if (Coins < cost) return false;
        Coins -= cost;
        bought = count;
        return true;
    }
    // Inventory goods from the peddler's pack (JinaItems), at the same discounts.
    public bool Spend(int basePrice, QuestProgress quest = null)
    {
        int cost = ShopRules.Price(basePrice, quest);
        if (Coins < cost) return false;
        Coins -= cost;
        return true;
    }
    public void Refund(int basePrice, QuestProgress quest = null) { Coins += ShopRules.Price(basePrice, quest); }
    public void Earn(int amount) { Coins += amount; }
    public void Reset() { Coins = 30; }
}
