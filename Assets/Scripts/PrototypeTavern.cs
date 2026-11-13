using UnityEngine;

[DefaultExecutionOrder(-110)]
public class PrototypeTavern : MonoBehaviour
{
    public PrototypeCombat combat;
    public readonly TavernWallet wallet = new TavernWallet();
    public bool IsOpen { get; private set; }
    private string message = "어서 오시오. 먼 길에 시장하실 터이니 한술 들고 가시오.";
    public bool Near => combat != null && (combat.Village == null || combat.Village.KeeperHome)
        && Vector2.Distance(combat.player.transform.position, PrototypeWorld.Tavern) < 2.2f;
    private QuestProgress Progress => combat != null && combat.Quest != null ? combat.Quest.progress : null;
    public void ResetShop()
    {
        wallet.Reset(); IsOpen = false;
        message = "어서 오시오. 먼 길에 시장하실 터이니 한술 들고 가시오.";
    }
    // What she has heard he did (from what really happened, not from his honour), then what that earns him here.
    public static string Greeting(QuestProgress q)
    {
        if (ShopRules.ElderDead(q))
            return "노인장께서 그리 가셨다니… 마을이 상중이라 오늘은 장국밥 솥을 걸지 않았소.";
        string deed = q == null ? null
            : q.KongOutcome == EncounterOutcome.Purified ? "강 건너 콩밭 마을 요괴에게서 그 집 딸을 살려 내셨다지요?"
            : q.KongOutcome == EncounterOutcome.Slain ? "강 건너 콩밭 마을 요괴까지 물리치셨다지요?"
            : q.Outcome == EncounterOutcome.Purified ? "도깨비의 저주를 풀어 주신 선비시구려!"
            : q.Outcome == EncounterOutcome.Slain ? "도깨비를 물리치셨다지요?" : null;
        if (deed == null) return "어서 오시오. 먼 길에 시장하실 터이니 한술 들고 가시오.";
        if (ShopRules.Honor(q) >= 30) return deed + " 값은 덜 받고, 아껴 둔 삼계탕도 내어 드리리다.";
        if (ShopRules.Discount(q) > 0) return deed + " 고마운 마음에 음식값을 조금 덜 받겠소.";
        return deed + " 어서 한술 들고 가시오.";
    }
    string Greeting() => Greeting(Progress);
    void Close() { IsOpen = false; combat.player.CanMove = combat.PlayerHealth > 0; }
    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.Alpha1)) Purchase(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Purchase(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Purchase(2);
        }
        else if (Near && Input.GetKeyDown(KeyCode.E) && !combat.Paused)
        {
            IsOpen = true;
            message = Greeting();
            combat.player.CanMove = false;
        }
    }
    void Purchase(int menu)
    {
        int restored;
        if (!ShopRules.MenuOpen(menu, Progress))
        {
            if (menu == ShopRules.BannedWhenElderDead) message = "상중이라 장국밥은 팔지 않소. 다른 것을 드시구려.";
            return;
        }
        if (wallet.Buy(menu, combat.PlayerHealth, out restored, Progress))
        {
            combat.RestoreHealth(restored);
            message = "든든히 드셨구려. 부디 남은 길도 평안히 가시오. (체력 +" + restored + ")";
        }
        else message = combat.PlayerHealth >= PrototypeCombat.MaxPlayerHealth ? "이미 기운이 넉넉하오. 음식값은 받지 않겠소." : "엽전이 모자라는구려.";
    }
    void OnGUI()
    {
        if (combat == null) return;
        PrototypeKoreanUI.Apply();
        if (Near && !IsOpen && combat.PlayerHealth > 0 && !combat.Paused) JinaUI.Prompt("E: 주막에서 음식 사 먹기");
        if (!IsOpen) return;
        bool special = ShopRules.MenuOpen(ShopRules.SpecialMenu, Progress);
        int rows = special ? 3 : 2, discount = ShopRules.Discount(Progress);
        const float gap = 8;
        float w = Mathf.Min(620, JinaUI.W - 30);
        float say = JinaUI.SayHeight("주모:  " + message, w - JinaUI.WindowPad * 2);
        float h = JinaUI.TitleHeight + 12 + say + gap + rows * (JinaUI.GoodHeight + gap) + 28 + JinaUI.WindowPad;
        var box = new Rect((JinaUI.W - w) / 2, JinaUI.H - h - 14, w, h);
        var inside = JinaUI.Window(box, "길손 주막", "엽전 " + wallet.Coins + "문" + (discount > 0 ? "   ·   선행 덕에 " + discount + "% 할인" : "") + "   ·   E / Esc 나가기");
        JinaUI.Say(new Rect(inside.x, inside.y, inside.width, say), "주모", message);
        for (int menu = 0; menu < rows; menu++)
        {
            bool open = ShopRules.MenuOpen(menu, Progress);
            int price = ShopRules.Price(TavernWallet.MenuPrices[menu], Progress);
            string cost = price < TavernWallet.MenuPrices[menu] ? TavernWallet.MenuPrices[menu] + " → " + price + "문" : price + "문";
            string title = (menu + 1) + " · " + TavernWallet.MenuNames[menu] + (menu == ShopRules.SpecialMenu ? " (특별)" : "");
            var r = new Rect(inside.x, inside.y + say + gap + menu * (JinaUI.GoodHeight + gap), inside.width, JinaUI.GoodHeight);
            if (JinaUI.Good(r, null, title, open ? cost : "", open ? "체력 +" + TavernWallet.MenuHeals[menu] : "상중이라 팔지 않음", open)) Purchase(menu);
        }
        JinaUI.Text(new Rect(inside.x, box.yMax - JinaUI.WindowPad - 20, inside.width, 20), "구매하면 바로 먹습니다. 메뉴를 보는 동안 전투가 멈춥니다.", 13, JinaUI.TextDim);
    }
}
