using UnityEngine;

[DefaultExecutionOrder(-110)]
public class PrototypeTavern : MonoBehaviour
{
    public PrototypeCombat combat;
    public readonly TavernWallet wallet = new TavernWallet();
    public bool IsOpen { get; private set; }
    private string message = "어서 오시오. 먼 길에 시장하실 터이니 한술 들고 가시오.";
    public bool Near => combat != null && Vector2.Distance(combat.player.transform.position, PrototypeWorld.Tavern) < 2.2f;
    private QuestProgress Progress => combat != null && combat.Quest != null ? combat.Quest.progress : null;
    public void ResetShop()
    {
        wallet.Reset(); IsOpen = false;
        message = "어서 오시오. 먼 길에 시장하실 터이니 한술 들고 가시오.";
    }
    string Greeting()
    {
        if (ShopRules.ElderDead(Progress))
            return "노인장께서 그리 가셨다니… 마을이 상중이라 오늘은 장국밥 솥을 걸지 않았소.";
        if (ShopRules.Honor(Progress) >= 30)
            return "도깨비의 저주를 풀어 주신 선비시구려! 값은 덜 받고, 아껴 둔 삼계탕도 내어 드리리다.";
        if (ShopRules.Discount(Progress) > 0)
            return "도깨비를 물리치셨다지요? 고마운 마음에 음식값을 조금 덜 받겠소.";
        return "어서 오시오. 먼 길에 시장하실 터이니 한술 들고 가시오.";
    }
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
        else if (Near && Input.GetKeyDown(KeyCode.E) && (combat.Quest == null || !combat.Quest.IsDialogOpen))
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
        else message = combat.PlayerHealth >= 5 ? "이미 기운이 넉넉하오. 음식값은 받지 않겠소." : "엽전이 모자라는구려.";
    }
    void OnGUI()
    {
        if (combat == null) return;
        PrototypeKoreanUI.Apply();
        GUI.Box(new Rect(15, 275, 235, 35), "노잣돈: " + wallet.Coins + "문 · 상평통보");
        if (Near && !IsOpen && combat.PlayerHealth > 0)
            GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height - 55, 440, 35), "E: 주막에서 음식 사 먹기");
        if (!IsOpen) return;
        bool special = ShopRules.MenuOpen(ShopRules.SpecialMenu, Progress);
        float w = Mathf.Min(650, Screen.width - 30), h = special ? 275 : 225;
        var box = new Rect((Screen.width - w) / 2, Screen.height - h - 15, w, h);
        int discount = ShopRules.Discount(Progress);
        GUI.Box(box, "길손 주막 · 가진 돈 " + wallet.Coins + "문" + (discount > 0 ? " · 선행 덕에 " + discount + "% 할인" : ""));
        for (int menu = 0; menu < TavernWallet.MenuPrices.Length; menu++)
        {
            if (menu == ShopRules.SpecialMenu && !special) continue;
            bool open = ShopRules.MenuOpen(menu, Progress);
            int price = ShopRules.Price(TavernWallet.MenuPrices[menu], Progress);
            string cost = price < TavernWallet.MenuPrices[menu] ? TavernWallet.MenuPrices[menu] + "문 → " + price + "문" : price + "문";
            string label = (menu + 1) + " · " + TavernWallet.MenuNames[menu] + (menu == ShopRules.SpecialMenu ? " (특별)" : "")
                + (open ? " — " + cost + " / 체력 " + TavernWallet.MenuHeals[menu] + " 회복" : " — 상중이라 팔지 않음");
            GUI.enabled = open;
            if (GUI.Button(new Rect(box.x + 20, box.y + 40 + menu * 50, w - 40, 42), label)) Purchase(menu);
            GUI.enabled = true;
        }
        GUI.Label(new Rect(box.x + 20, box.y + h - 72, w - 40, 55), "구매하면 바로 먹습니다. 최대 체력은 5입니다.\nE / Esc: 나가기 · 메뉴를 보는 동안 전투가 멈춥니다.");
        SpeechBubble.Draw(new Vector3(PrototypeWorld.Tavern.x, 1.4f, 0), "주모", message);
    }
}
