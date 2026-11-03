using UnityEngine;

// A bobusang beside the tavern who sells arrows; shares the tavern's purse.
[DefaultExecutionOrder(-110)]
public class PrototypePeddler : MonoBehaviour
{
    public PrototypeCombat combat;
    public bool IsOpen { get; private set; }
    private string message = "";
    private SpriteRenderer marker;
    public bool Near => combat != null && Vector2.Distance(combat.player.transform.position, PrototypeWorld.Peddler) < 1.8f;
    private QuestProgress Progress => combat.Quest != null ? combat.Quest.progress : null;
    private TavernWallet Wallet => combat.Tavern.wallet;

    void Start()
    {
        marker = new GameObject("보부상").AddComponent<SpriteRenderer>();
        marker.transform.position = PrototypeWorld.Peddler;
        marker.transform.localScale = new Vector3(.75f, 1.4f, 1);
        marker.sprite = combat.player.GetComponent<SpriteRenderer>().sprite;
        marker.color = new Color(.55f, .45f, .3f);
        JinaActorVisual.Attach(marker, JinaArt.Peddler, combat);
    }
    public void ResetShop() { IsOpen = false; message = ""; }
    string Greeting()
    {
        if (ShopRules.ElderDead(Progress))
            return "노인장 일은 들었소. 흉한 일이 난 마을이라 짐을 다 풀 수가 없구려. 오늘은 낱개로만 파오.";
        if (ShopRules.Honor(Progress) >= 30)
            return "도깨비를 달래어 돌려보내셨다지요? 장터마다 소문이 자자하오. 값을 넉넉히 깎아 드리리다.";
        if (ShopRules.Discount(Progress) > 0)
            return "숲길이 트였다더니 선비 덕분이었구려. 길값 삼아 조금 덜 받겠소.";
        return "등짐 진 보부상이오. 활 쓰는 선비라면 화살이 넉넉해야지요.";
    }
    void Close() { IsOpen = false; combat.player.CanMove = combat.PlayerHealth > 0; }
    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.Alpha1)) Purchase(false);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Purchase(true);
        }
        else if (Near && Input.GetKeyDown(KeyCode.E) && !combat.Paused)
        {
            IsOpen = true;
            message = Greeting();
            combat.player.CanMove = false;
        }
    }
    void Purchase(bool bundle)
    {
        int bought;
        if (Wallet.BuyArrows(bundle, combat.Arrows, out bought, Progress))
        {
            combat.AddArrows(bought);
            message = "화살 " + bought + "대, 여기 있소. 과녁 잘 맞히시구려.";
        }
        else if (bundle && !ShopRules.BundleOpen(Progress)) message = "묶음은 풀지 않겠소. 낱개로만 파오.";
        else if (combat.Arrows + (bundle ? ShopRules.BundleSize : 1) > ShopRules.MaxArrows)
            message = "화살통에 더 들어갈 자리가 없소. (최대 " + ShopRules.MaxArrows + "대)";
        else message = "엽전이 모자라는구려.";
    }
    void OnGUI()
    {
        if (combat == null) return;
        PrototypeKoreanUI.Apply();
        if (Near && !IsOpen && combat.PlayerHealth > 0)
            GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height - 55, 440, 35), "E: 보부상에게서 화살 사기");
        if (!IsOpen) return;
        float w = Mathf.Min(650, Screen.width - 30);
        var box = new Rect((Screen.width - w) / 2, Screen.height - 240, w, 225);
        int discount = ShopRules.Discount(Progress);
        GUI.Box(box, "보부상 등짐 · 가진 돈 " + Wallet.Coins + "문 · 화살 " + combat.Arrows + "/" + ShopRules.MaxArrows
            + (discount > 0 ? " · " + discount + "% 할인" : ""));
        GUI.enabled = true;
        if (GUI.Button(new Rect(box.x + 20, box.y + 40, w - 40, 42), "1 · 화살 한 대 — " + Cost(ShopRules.ArrowPrice))) Purchase(false);
        bool bundle = ShopRules.BundleOpen(Progress);
        GUI.enabled = bundle;
        if (GUI.Button(new Rect(box.x + 20, box.y + 90, w - 40, 42), bundle
            ? "2 · 화살 " + ShopRules.BundleSize + "대 묶음 — " + Cost(ShopRules.BundlePrice)
            : "2 · 화살 묶음 — 마을의 흉사로 팔지 않음")) Purchase(true);
        GUI.enabled = true;
        GUI.Label(new Rect(box.x + 20, box.y + 153, w - 40, 55), "화살통에는 " + ShopRules.MaxArrows + "대까지 담을 수 있습니다.\nE / Esc: 나가기 · 흥정하는 동안 전투가 멈춥니다.");
        SpeechBubble.Draw(new Vector3(PrototypeWorld.Peddler.x, 1.55f, 0), "보부상", message);
    }
    string Cost(int basePrice)
    {
        int price = ShopRules.Price(basePrice, Progress);
        return price < basePrice ? basePrice + "문 → " + price + "문" : price + "문";
    }
    void OnDestroy() { if (marker != null) Destroy(marker.gameObject); }
}
