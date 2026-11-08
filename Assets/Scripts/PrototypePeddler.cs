using UnityEngine;

// A bobusang beside the tavern who sells arrows and things for the road (JinaItems, into the inventory);
// shares the tavern's purse.
[DefaultExecutionOrder(-110)]
public class PrototypePeddler : MonoBehaviour
{
    public PrototypeCombat combat;
    public bool IsOpen { get; private set; }
    private string message = "";
    private SpriteRenderer marker;
    public bool Near => combat != null && (combat.Village == null || combat.Village.PeddlerHome)
        && Vector2.Distance(combat.player.transform.position, PrototypeWorld.Peddler) < 1.8f;
    public SpriteRenderer Marker => marker;
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
        return "등짐 진 보부상이오. 화살도 있고, 먼 길에 요긴한 것들도 챙겨 왔소.";
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
            for (int i = 0; i < JinaItems.All.Length; i++)
                if (Input.GetKeyDown(KeyCode.Alpha3 + i)) Purchase(JinaItems.All[i]);
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
    // An item goes into the inventory; with no room there, the coins come back.
    public void Purchase(JinaItem item)
    {
        var bag = JinaItems.Inventory;
        if (!ShopRules.ItemOpen(item, Progress)) message = "부적은 흉사가 난 마을에서 풀지 않겠소.";
        else if (bag == null) message = "짐을 담을 보따리가 없구려.";
        else if (!Wallet.Spend(item.price, Progress)) message = "엽전이 모자라는구려.";
        else if (!bag.TryAdd(item)) { Wallet.Refund(item.price, Progress); message = "보따리에 더 넣을 자리가 없소."; }
        else message = item.ItemName + ", 여기 있소. " + Pitch(item);
    }
    static string Pitch(JinaItem item)
    {
        switch (item.id)
        {
            case "misutgaru": return "물에 타서 한숨 돌릴 때 드시오.";
            case "jumeokbap": return "주모 솜씨요. 식기 전에 드시구려.";
            case "cheongsimhwan": return "귀한 것이니 정말 급할 때 쓰시오.";
            case "arrows": return "화살통이 비면 풀어 쓰시오.";
            case "ward": return "품에 지니면 한 번은 액을 막아 줄 게요.";
            default: return "이걸 신으면 바람처럼 걸을 게요. 금방 해지지만.";
        }
    }

    void OnGUI()
    {
        if (combat == null) return;
        PrototypeKoreanUI.Apply();
        if (Near && !IsOpen && combat.PlayerHealth > 0)
            GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height - 55, 440, 35), "E: 보부상 등짐 보기");
        if (!IsOpen) return;
        // A solid window at the bottom (the map strip hides while it is open): arrows, then the goods two to a row,
        // each with its icon, name, price and (in green) what it does.
        const float row = 56, gap = 8;
        float w = Mathf.Min(780, Screen.width - 30), half = (w - 28 - gap) / 2;
        int rows = 1 + (JinaItems.All.Length + 1) / 2;
        float h = 60 + rows * (row + gap) + 30;
        var box = new Rect((Screen.width - w) / 2, Screen.height - h - 12, w, h);
        int discount = ShopRules.Discount(Progress);
        var inside = JinaUI.Window(box, "보부상 등짐", "E / Esc 나가기");
        string purse = "엽전 " + Wallet.Coins + "문   ·   화살 " + combat.Arrows + "/" + ShopRules.MaxArrows + (discount > 0 ? "   ·   " + discount + "% 할인" : "");
        JinaUI.Text(new Rect(box.x + 140, box.y + 4, w - 300, 26), purse, 15, JinaUI.Gold, TextAnchor.MiddleCenter, true);

        bool bundle = ShopRules.BundleOpen(Progress);
        if (Good(new Rect(inside.x, inside.y, half, row), null, "1 · 화살 한 대", Cost(ShopRules.ArrowPrice), "화살통에 바로", true)) Purchase(false);
        if (Good(new Rect(inside.x + half + gap, inside.y, half, row), null, "2 · 화살 " + ShopRules.BundleSize + "대 묶음",
            bundle ? Cost(ShopRules.BundlePrice) : "", bundle ? "화살통에 바로" : "흉사로 팔지 않음", bundle)) Purchase(true);
        for (int i = 0; i < JinaItems.All.Length; i++)
        {
            var item = JinaItems.All[i];
            var r = new Rect(inside.x + (i % 2) * (half + gap), inside.y + (1 + i / 2) * (row + gap), half, row);
            bool open = ShopRules.ItemOpen(item, Progress);
            string effect = item.Description.Substring(item.Description.LastIndexOf('\n') + 1);
            if (Good(r, item.Icon, (i + 3) + " · " + item.ItemName, open ? Cost(item.price) : "", open ? effect : "흉사로 팔지 않음", open)) Purchase(item);
        }
        JinaUI.Text(new Rect(inside.x, box.yMax - 28, inside.width, 22), "화살은 화살통에 " + ShopRules.MaxArrows + "대까지, 나머지는 보따리(I)에 들어갑니다. 흥정하는 동안 전투가 멈춥니다.", 13, JinaUI.Ink);
        SpeechBubble.Draw(new Vector3(PrototypeWorld.Peddler.x, 1.55f, 0), "보부상", message);
    }
    // One good on sale: a slot that lights up under the mouse; true when clicked (and on sale).
    static bool Good(Rect r, Sprite icon, string name, string price, string effect, bool open)
    {
        bool over = open && r.Contains(Event.current.mousePosition);
        JinaUI.Frame(r, over ? JinaUI.Hover : JinaUI.SlotBack, over ? JinaUI.Gold : JinaUI.SlotEdge, over ? 2 : 1);
        float x = r.x + 10;
        if (icon != null)
        {
            var saved = GUI.color; if (!open) GUI.color = new Color(1, 1, 1, .4f);
            InventoryUI.DrawIcon(icon, new Rect(r.x + 8, r.y + 8, 40, 40));
            GUI.color = saved; x = r.x + 58;
        }
        JinaUI.Text(new Rect(x, r.y + 7, r.xMax - x - 10, 22), name, 16, open ? JinaUI.Ink : JinaUI.Dim, TextAnchor.UpperLeft, true);
        JinaUI.Text(new Rect(x, r.y + 7, r.xMax - x - 10, 22), price, 16, JinaUI.Gold, TextAnchor.UpperRight, true);
        JinaUI.Text(new Rect(x, r.y + 31, r.xMax - x - 10, 20), effect, 13, open ? JinaUI.Effect : JinaUI.Dim);
        return GUI.Button(r, "", GUIStyle.none) && open;
    }
    string Cost(int basePrice)
    {
        int price = ShopRules.Price(basePrice, Progress);
        return price < basePrice ? basePrice + " → " + price + "문" : price + "문";
    }
    void OnDestroy() { if (marker != null) Destroy(marker.gameObject); }
}
