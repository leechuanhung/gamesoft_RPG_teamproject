using UnityEngine;

[DefaultExecutionOrder(-110)]
public class PrototypeTavern : MonoBehaviour
{
    public PrototypeCombat combat;
    public readonly TavernWallet wallet = new TavernWallet();
    public bool IsOpen { get; private set; }
    private string message = "어서 오시오. 먼 길에 시장하실 터이니 한술 들고 가시오.";
    public bool Near => combat != null && Vector2.Distance(combat.player.transform.position, PrototypeWorld.Tavern) < 2.2f;
    public void ResetShop()
    {
        wallet.Reset(); IsOpen = false;
        message = "어서 오시오. 먼 길에 시장하실 터이니 한술 들고 가시오.";
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
        }
        else if (Near && Input.GetKeyDown(KeyCode.E) && (combat.Quest == null || !combat.Quest.IsDialogOpen))
        {
            IsOpen = true;
            combat.player.CanMove = false;
        }
    }
    void Purchase(int menu)
    {
        int restored;
        if (wallet.Buy(menu, combat.PlayerHealth, out restored))
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
        float w = Mathf.Min(650, Screen.width - 30);
        var box = new Rect((Screen.width - w) / 2, Screen.height - 290, w, 275);
        GUI.Box(box, "길손 주막 · 가진 돈 " + wallet.Coins + "문");
        GUI.Label(new Rect(box.x + 20, box.y + 35, w - 40, 50), message);
        if (GUI.Button(new Rect(box.x + 20, box.y + 95, w - 40, 42), "1 · 보리밥과 김치 한 상 — 5문 / 체력 2 회복")) Purchase(0);
        if (GUI.Button(new Rect(box.x + 20, box.y + 145, w - 40, 42), "2 · 장국밥 한 그릇 — 10문 / 체력 4 회복")) Purchase(1);
        GUI.Label(new Rect(box.x + 20, box.y + 203, w - 40, 55), "구매하면 바로 먹습니다. 최대 체력은 5입니다.\nE / Esc: 나가기 · 메뉴를 보는 동안 전투가 멈춥니다.");
    }
}
