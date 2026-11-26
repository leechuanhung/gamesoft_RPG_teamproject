using UnityEngine;

// 상점 NPC: 주막(TavernShop)과 보부상(PeddlerShop), 둘이 같이 쓰는 돈과 가격 규칙.
// 대화 바탕은 NpcTalk(MainNpcs.cs).
// 가게는 여러 곳에 있다(ShopStall): 음식은 첫 마을 주막과 한양 국밥집, 물건은 첫 마을 보부상, 콩밭 마을 입구의
// 방물장수, 한양 잡화전. 첫 마을의 상중(노인이 죽음) 규칙은 그 마을 가게에서만 지킨다.

// 가게 한 곳: 자리, 손 닿는 거리, 창 제목, 파는 사람, 안내, 그림(첫 마을 가게는 따로 그린다).
public struct ShopStall
{
    public Vector2 at; public float range; public string title, keeper, prompt, art;
    public ShopStall(Vector2 at, float range, string title, string keeper, string prompt, string art = null)
    { this.at = at; this.range = range; this.title = title; this.keeper = keeper; this.prompt = prompt; this.art = art; }
}

// 첫 마을 밖 가게 사람들의 그림: 전투 오브젝트(카메라) 밑이 아니라 따로 세우고, 가까우면 선비 쪽을 본다.
public static class StallArt
{
    public static SpriteRenderer[] Make(ShopStall[] stalls, string root)
    {
        var parent = new GameObject(root);
        var arts = new SpriteRenderer[stalls.Length];
        for (int i = 1; i < stalls.Length; i++)
        {
            var art = new GameObject(stalls[i].keeper).AddComponent<SpriteRenderer>();
            art.transform.SetParent(parent.transform);
            art.sprite = JinaArt.Load(stalls[i].art);
            art.transform.position = new Vector3(stalls[i].at.x, -.5f, 0);
            art.sortingOrder = 2;
            arts[i] = art;
        }
        return arts;
    }
    // 그림은 왼쪽을 보고 그려져 있다.
    public static void Face(SpriteRenderer[] arts, ShopStall[] stalls, float px)
    {
        for (int i = 1; i < arts.Length; i++) if (arts[i] != null) arts[i].flipX = Mathf.Abs(px - stalls[i].at.x) < 5 && px > stalls[i].at.x;
    }
    public static void Destroy(SpriteRenderer[] arts)
    {
        if (arts == null) return;
        foreach (var a in arts) if (a != null && a.transform.parent != null) { Object.Destroy(a.transform.parent.gameObject); return; }
    }
}

// ===== 주막 (TavernShop) =====
[DefaultExecutionOrder(-110)]
public class TavernShop : NpcTalk
{
    public readonly TavernWallet wallet = new TavernWallet();
    private string message = "어서 오시오. 먼 길에 시장하실 터이니 한술 들고 가시오.";
    public static readonly ShopStall[] Stalls =
    {
        new ShopStall(WorldBuilder.Tavern, 2.2f, "길손 주막", "주모", "주막에서 음식 사 먹기"),
        new ShopStall(WorldBuilder.Gukbap, 1.4f, "한양 국밥집", "국밥집 주모", "국밥집에서 음식 사 먹기", "24_Hanyang/jumo"),
    };
    // 지금 열려 있거나 가장 가까운 가게(Stalls 번호).
    public int Here { get; private set; }
    // 선비가 손 닿는 가게(없으면 -1). 첫 마을 주막은 주모가 피란하면 닫힌다.
    public int NearStall()
    {
        if (combat == null) return -1;
        for (int i = 0; i < Stalls.Length; i++)
        {
            if (i == 0 && combat.Village != null && !combat.Village.KeeperHome) continue;
            if (Within(Stalls[i].at, Stalls[i].range)) return i;
        }
        return -1;
    }
    public bool Near => NearStall() >= 0;
    // 상중 규칙(장국밥을 팔지 않음)은 첫 마을 주막에서만.
    bool Mourning => Here == 0 && ShopRules.ElderDead(Progress);
    private SpriteRenderer[] arts;
    void Start() { arts = StallArt.Make(Stalls, "한양 국밥집"); }
    void LateUpdate() { if (arts != null && combat != null) StallArt.Face(arts, Stalls, combat.player.transform.position.x); }
    void OnDestroy() { StallArt.Destroy(arts); }
    private QuestProgress Progress => combat != null && combat.Quest != null ? combat.Quest.progress : null;
    // 재시작: 주막 상태를 처음으로.
    public void ResetShop()
    {
        wallet.Reset(); IsOpen = false;
        message = "어서 오시오. 먼 길에 시장하실 터이니 한술 들고 가시오.";
    }
    // 선비가 한 일에 대해 주모가 들은 소문(명예 점수가 아니라 실제로 일어난 일), 그리고 그 값.
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
    // 한양 국밥집 주모: 선악이 음수면 선비를 꺼린다.
    public static string HanyangGreeting(QuestProgress q)
    {
        if (ShopRules.Honor(q) < 0) return "…(국자를 멈추고 선비를 훑어본다) 돈만 내면 누구든 먹이지. 소란만 피우지 마시오.";
        if (ShopRules.Honor(q) >= 30) return "소문 난 그 선비시구려! 값은 덜 받고, 아껴 둔 삼계탕도 내어 드리리다.";
        return "과거 보러 온 선비시오? 한양 장국밥 한 그릇 들고 가시오. 내일 시험엔 배가 든든해야지.";
    }
    string Greeting() => Here == 0 ? Greeting(Progress) : HanyangGreeting(Progress);
    // F·클릭으로 주막 창을 열고, F·Esc로 닫는다.
    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }   // 창 안 클릭으로는 닫지 않음
            if (Input.GetKeyDown(KeyCode.Alpha1)) Purchase(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Purchase(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Purchase(2);
        }
        else if (Near && JinaUI.Interact && !combat.Paused)
        {
            Here = NearStall();
            Begin();
            message = Greeting();
        }
    }
    // 가게 `stall`을 바로 연다(검사, 스크린샷).
    public void OpenAt(int stall) { Here = stall; Begin(); message = Greeting(); }
    // 주막 메뉴 하나를 산다.
    public void Purchase(int menu)
    {
        int restored;
        if (!ShopRules.MenuOpen(menu, Progress, Mourning))
        {
            if (menu == ShopRules.BannedWhenElderDead) message = "상중이라 장국밥은 팔지 않소. 다른 것을 드시구려.";
            return;
        }
        if (wallet.Buy(menu, combat.PlayerHealth, out restored, Progress, Mourning))
        {
            combat.RestoreHealth(restored);
            message = "든든히 드셨구려. 부디 남은 길도 평안히 가시오. (체력 +" + restored + ")";
        }
        else message = combat.PlayerHealth >= Combat.MaxPlayerHealth ? "이미 기운이 넉넉하오. 음식값은 받지 않겠소." : "엽전이 모자라는구려.";
    }
    // 주막 창을 그린다.
    void OnGUI()
    {
        if (combat == null) return;
        KoreanUI.Apply();
        int near = NearStall();
        if (near >= 0 && !IsOpen && combat.PlayerHealth > 0 && !combat.Paused) JinaUI.Prompt("F·클릭: " + Stalls[near].prompt);
        if (!IsOpen) return;
        var stall = Stalls[Here];
        bool special = ShopRules.MenuOpen(ShopRules.SpecialMenu, Progress, Mourning);
        int rows = special ? 3 : 2, discount = ShopRules.Discount(Progress);
        const float gap = 8;
        float w = Mathf.Min(620, JinaUI.W - 30);
        float say = JinaUI.SayHeight(stall.keeper + ":  " + message, w - JinaUI.WindowPad * 2);
        float h = JinaUI.TitleHeight + 12 + say + gap + rows * (JinaUI.GoodHeight + gap) + 28 + JinaUI.WindowPad;
        var box = new Rect((JinaUI.W - w) / 2, JinaUI.H - h - 14, w, h);
        var inside = JinaUI.Window(box, stall.title, "엽전 " + wallet.Coins + "문" + (discount > 0 ? "   ·   선행 덕에 " + discount + "% 할인" : "") + "   ·   F / Esc 나가기");
        JinaUI.Say(new Rect(inside.x, inside.y, inside.width, say), stall.keeper, message);
        for (int menu = 0; menu < rows; menu++)
        {
            bool open = ShopRules.MenuOpen(menu, Progress, Mourning);
            int price = ShopRules.Price(TavernWallet.MenuPrices[menu], Progress);
            string cost = price < TavernWallet.MenuPrices[menu] ? TavernWallet.MenuPrices[menu] + " → " + price + "문" : price + "문";
            string title = (menu + 1) + " · " + TavernWallet.MenuNames[menu] + (menu == ShopRules.SpecialMenu ? " (특별)" : "");
            var r = new Rect(inside.x, inside.y + say + gap + menu * (JinaUI.GoodHeight + gap), inside.width, JinaUI.GoodHeight);
            if (JinaUI.Good(r, null, title, open ? cost : "", open ? "체력 +" + TavernWallet.MenuHeals[menu] : "상중이라 팔지 않음", open)) Purchase(menu);
        }
        JinaUI.Text(new Rect(inside.x, box.yMax - JinaUI.WindowPad - 20, inside.width, 20), "구매하면 바로 먹습니다. 메뉴를 보는 동안 전투가 멈춥니다.", 13, JinaUI.TextDim);
    }
}

// ===== 보부상 상점 (PeddlerShop) =====
// 주막 옆 보부상: 화살과 길에서 쓸 물건(JinaItems, 인벤토리로)을 판다.
// 주막과 돈주머니를 같이 쓴다.
[DefaultExecutionOrder(-110)]
public class PeddlerShop : NpcTalk
{
    private string message = "";
    private SpriteRenderer marker;
    public static readonly ShopStall[] Stalls =
    {
        new ShopStall(WorldBuilder.Peddler, 1.8f, "보부상 등짐", "보부상", "보부상 등짐 보기"),
        new ShopStall(WorldBuilder.Bangmul, 1.0f, "방물장수 보따리", "방물장수", "방물장수 보따리 보기", "24_Hanyang/bangmul"),
        new ShopStall(WorldBuilder.Sangin, 1.6f, "한양 잡화전", "상인", "잡화전 물건 보기", "24_Hanyang/sangin"),
    };
    public const int Kongbat = 1, Hanyang = 2;
    public int Here { get; private set; }
    // 방물장수는 콩쥐 요괴와 싸우는 동안 피란한다.
    bool Away(int stall) => stall == Kongbat && combat.KongBoss != null && combat.KongBoss.Fighting;
    // 선비가 손 닿는 가게(없으면 -1). 첫 마을 보부상은 피란하면 닫힌다. 콩밭 마을 사람과 겹치면 사람이 먼저다.
    public int NearStall()
    {
        if (combat == null) return -1;
        for (int i = 0; i < Stalls.Length; i++)
        {
            if (i == 0 && combat.Village != null && !combat.Village.PeddlerHome) continue;
            if (Away(i) || (i == Kongbat && combat.Kong != null && combat.Kong.Nearest().HasValue)) continue;
            if (Within(Stalls[i].at, Stalls[i].range)) return i;
        }
        return -1;
    }
    public bool Near => NearStall() >= 0;
    // 상중 규칙(묶음 화살과 부적을 풀지 않음)은 첫 마을 보부상에게서만.
    bool Mourning => Here == 0 && ShopRules.ElderDead(Progress);
    private SpriteRenderer[] arts;
    public SpriteRenderer Marker => marker;
    private QuestProgress Progress => combat.Quest != null ? combat.Quest.progress : null;
    private TavernWallet Wallet => combat.Tavern.wallet;

    // 보부상을 만든다.
    void Start()
    {
        marker = new GameObject("보부상").AddComponent<SpriteRenderer>();
        marker.transform.position = WorldBuilder.Peddler;
        marker.transform.localScale = new Vector3(.75f, 1.4f, 1);
        marker.sprite = combat.player.GetComponent<SpriteRenderer>().sprite;
        marker.color = new Color(.55f, .45f, .3f);
        JinaActorVisual.Attach(marker, JinaArt.Peddler, combat);
        arts = StallArt.Make(Stalls, "길의 상인들");
    }
    void LateUpdate()
    {
        if (arts == null || combat == null) return;
        StallArt.Face(arts, Stalls, combat.player.transform.position.x);
        if (arts[Kongbat] != null) arts[Kongbat].enabled = !Away(Kongbat);
    }
    public void ResetShop() { IsOpen = false; message = ""; }
    // 보부상이 들은 소문(명예 점수가 아니라 실제로 일어난 일), 그리고 그 값.
    public static string Greeting(QuestProgress q)
    {
        if (ShopRules.ElderDead(q))
            return "노인장 일은 들었소. 흉한 일이 난 마을이라 짐을 다 풀 수가 없구려. 오늘은 낱개로만 파오.";
        string deed = q == null ? null
            : q.KongOutcome == EncounterOutcome.Purified ? "강 건너 콩밭 마을 요괴만 태워 내고 그 집 딸은 살려 내셨다지요? 장터마다 소문이 자자하오."
            : q.KongOutcome == EncounterOutcome.Slain ? "강 건너 콩밭 마을 요괴를 베셨다는 소문이 장터마다 돌더이다."
            : q.Outcome == EncounterOutcome.Purified ? "도깨비를 달래어 돌려보내셨다지요? 장터마다 소문이 자자하오."
            : q.Outcome == EncounterOutcome.Slain ? "숲길이 트였다더니 선비 덕분이었구려." : null;
        if (deed == null) return "등짐 진 보부상이오. 화살도 있고, 먼 길에 요긴한 것들도 챙겨 왔소.";
        if (ShopRules.Honor(q) >= 30) return deed + " 값을 넉넉히 깎아 드리리다.";
        if (ShopRules.Discount(q) > 0) return deed + " 길값 삼아 조금 덜 받겠소.";
        return deed + " 필요한 것 있으면 골라 보시오.";
    }
    // 콩밭 마을 입구의 방물장수: 마을 일을 아는 만큼.
    public static string KongbatGreeting(QuestProgress q, bool revealed)
    {
        if (q != null && q.KongOutcome == EncounterOutcome.Purified) return "콩쥐 아가씨가 돌아왔다지요? 오늘은 덤을 얹어 드리리다. 골라 보시오.";
        if (q != null && q.KongOutcome == EncounterOutcome.Slain) return "…사람이 여럿 상했다지요. 약이라도 넉넉히 챙겨 가시오.";
        if (revealed) return "선비님 낯빛이 왜 그러오? …나는 보름 잔치 전에 이 마을을 뜰 생각이오. 필요한 것 있으면 얼른 사 가시오.";
        return "방물장수요. 바늘, 실, 분, 그리고 먼 길에 요긴한 것들까지 다 있다오. 화살도 몇 묶음 떼어 왔소.";
    }
    // 한양 잡화전 상인: 한양 사람들 말(HanyangTown)을 그대로 한다.
    public static string HanyangGreeting(QuestProgress q) => string.Join(" ", HanyangTown.Lines(HanyangTown.Who.Sangin, ShopRules.Honor(q) < 0, false));
    string Greeting() => Here == Kongbat ? KongbatGreeting(Progress, combat.Kong != null && combat.Kong.Revealed)
        : Here == Hanyang ? HanyangGreeting(Progress) : Greeting(Progress);
    // 가게 `stall`을 바로 연다(검사, 스크린샷).
    public void OpenAt(int stall) { Here = stall; Begin(); message = Greeting(); }
    // F·클릭으로 보부상 창을 열고, F·Esc로 닫는다.
    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }   // 창 안 클릭으로는 닫지 않음
            if (Input.GetKeyDown(KeyCode.Alpha1)) Purchase(false);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Purchase(true);
            for (int i = 0; i < JinaItems.ForSale.Length; i++)
                if (Input.GetKeyDown(KeyCode.Alpha3 + i)) Purchase(JinaItems.ForSale[i]);
        }
        else if (Near && JinaUI.Interact && !combat.Paused)
        {
            Here = NearStall();
            Begin();
            message = Greeting();
        }
    }
    // 화살을 산다(`bundle`: 묶음).
    public void Purchase(bool bundle)
    {
        int bought;
        if (Wallet.BuyArrows(bundle, combat.Arrows, out bought, Progress, Mourning))
        {
            combat.AddArrows(bought);
            message = "화살 " + bought + "대, 여기 있소. 과녁 잘 맞히시구려.";
        }
        else if (bundle && !ShopRules.BundleOpen(Progress, Mourning)) message = "묶음은 풀지 않겠소. 낱개로만 파오.";
        else if (combat.Arrows + (bundle ? ShopRules.BundleSize : 1) > ShopRules.MaxArrows)
            message = "화살통에 더 들어갈 자리가 없소. (최대 " + ShopRules.MaxArrows + "대)";
        else message = "엽전이 모자라는구려.";
    }
    // 물건은 인벤토리로 들어간다. 자리가 없으면 돈을 돌려준다.
    public void Purchase(JinaItem item)
    {
        var bag = JinaItems.Inventory;
        if (!ShopRules.ItemOpen(item, Progress, Mourning)) message = "부적은 흉사가 난 마을에서 풀지 않겠소.";
        else if (bag == null) message = "짐을 담을 보따리가 없구려.";
        else if (!Wallet.Spend(item.price, Progress)) message = "엽전이 모자라는구려.";
        else if (!bag.TryAdd(item)) { Wallet.Refund(item.price, Progress); message = "보따리에 더 넣을 자리가 없소."; }
        else message = item.ItemName + ", 여기 있소. " + Pitch(item);
    }
    // 물건 설명 한 줄.
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

    // 보부상 창을 그린다.
    void OnGUI()
    {
        if (combat == null) return;
        KoreanUI.Apply();
        int near = NearStall();
        if (near >= 0 && !IsOpen && combat.PlayerHealth > 0 && !combat.Paused) JinaUI.Prompt("F·클릭: " + Stalls[near].prompt);
        if (!IsOpen) return;
        var stall = Stalls[Here];
        // 화살, 그다음 한 줄에 둘씩 물건: 아이콘, 이름, 가격, 효과.
        const float gap = 8;
        float w = Mathf.Min(780, JinaUI.W - 30), half = (w - JinaUI.WindowPad * 2 - gap) / 2;
        int rows = 1 + (JinaItems.ForSale.Length + 1) / 2, discount = ShopRules.Discount(Progress);
        float say = JinaUI.SayHeight(stall.keeper + ":  " + message, w - JinaUI.WindowPad * 2);
        float h = JinaUI.TitleHeight + 12 + say + gap + rows * (JinaUI.GoodHeight + gap) + 28 + JinaUI.WindowPad;
        var box = new Rect((JinaUI.W - w) / 2, JinaUI.H - h - 14, w, h);
        var inside = JinaUI.Window(box, stall.title, "엽전 " + Wallet.Coins + "문   ·   화살 " + combat.Arrows + "/" + ShopRules.MaxArrows + (discount > 0 ? "   ·   " + discount + "% 할인" : "") + "   ·   F / Esc 나가기");
        JinaUI.Say(new Rect(inside.x, inside.y, inside.width, say), stall.keeper, message);
        float top = inside.y + say + gap;
        bool bundle = ShopRules.BundleOpen(Progress, Mourning);
        if (JinaUI.Good(new Rect(inside.x, top, half, JinaUI.GoodHeight), JinaItems.Get("arrows").Icon, "1 · 화살 한 대", Cost(ShopRules.ArrowPrice), "화살통에 바로 넣습니다", true)) Purchase(false);
        if (JinaUI.Good(new Rect(inside.x + half + gap, top, half, JinaUI.GoodHeight), JinaItems.Get("arrows").Icon, "2 · 화살 " + ShopRules.BundleSize + "대 묶음",
            bundle ? Cost(ShopRules.BundlePrice) : "", bundle ? "화살통에 바로 넣습니다" : "흉사로 팔지 않음", bundle)) Purchase(true);
        for (int i = 0; i < JinaItems.ForSale.Length; i++)
        {
            var item = JinaItems.ForSale[i];
            var r = new Rect(inside.x + (i % 2) * (half + gap), top + (1 + i / 2) * (JinaUI.GoodHeight + gap), half, JinaUI.GoodHeight);
            bool open = ShopRules.ItemOpen(item, Progress, Mourning);
            string effect = item.Description.Substring(item.Description.LastIndexOf('\n') + 1);
            if (JinaUI.Good(r, item.Icon, (i + 3) + " · " + item.ItemName, open ? Cost(item.price) : "", open ? effect : "흉사로 팔지 않음", open)) Purchase(item);
        }
        JinaUI.Text(new Rect(inside.x, box.yMax - JinaUI.WindowPad - 20, inside.width, 20), "화살은 화살통에 " + ShopRules.MaxArrows + "대까지, 나머지는 보따리(I)에 들어갑니다. 흥정하는 동안 전투가 멈춥니다.", 13, JinaUI.TextDim);
    }
    // 기본값에 선악 할인을 적용한 가격.
    string Cost(int basePrice)
    {
        int price = ShopRules.Price(basePrice, Progress);
        return price < basePrice ? basePrice + " → " + price + "문" : price + "문";
    }
    void OnDestroy() { if (marker != null) Destroy(marker.gameObject); StallArt.Destroy(arts); }
}

// ===== 돈과 가격 규칙 (TavernWallet) =====
// 가격과 물건은 선악 상태를 따른다. 주막과 보부상이 같이 쓴다.
public static class ShopRules
{
    public const int MaxArrows = 20, ArrowPrice = 1, BundleSize = 10, BundlePrice = 9;
    public const int BannedWhenElderDead = 1, SpecialMenu = 2;
    public static int Honor(QuestProgress quest) => quest == null ? 0 : quest.Honor;
    public static bool ElderDead(QuestProgress quest) => quest != null && quest.ElderDead;
    // 도깨비 토벌 +10 명예, 정화 +30.
    public static int Discount(QuestProgress quest) => Honor(quest) >= 30 ? 20 : Honor(quest) >= 10 ? 10 : 0;
    // 가장 가까운 문으로 반올림, 반이면 사는 사람에게 유리하게, 1문 아래로는 안 내려감.
    public static int Price(int basePrice, QuestProgress quest) =>
        System.Math.Max(1, (basePrice * (100 - Discount(quest)) + 49) / 100);
    // 이 메뉴가 지금 팔리는지.
    // `mourning`: 상중인 가게(첫 마을). 다른 마을 가게는 노인이 죽었어도 다 판다.
    public static bool MenuOpen(int menu, QuestProgress quest) => MenuOpen(menu, quest, ElderDead(quest));
    public static bool MenuOpen(int menu, QuestProgress quest, bool mourning) =>
        menu == BannedWhenElderDead ? !mourning : menu != SpecialMenu || Honor(quest) >= 30;
    public static bool BundleOpen(QuestProgress quest) => !ElderDead(quest);
    public static bool BundleOpen(QuestProgress quest, bool mourning) => !mourning;
    // 마을에 불행이 닥친 뒤 보부상은 부적을 싸 두고, 음식과 화살만 판다.
    public static bool ItemOpen(JinaItem item, QuestProgress quest) => ItemOpen(item, quest, ElderDead(quest));
    public static bool ItemOpen(JinaItem item, QuestProgress quest, bool mourning) =>
        !mourning || item.effect == JinaItem.Effect.Heal || item.effect == JinaItem.Effect.Arrows;
}

public class TavernWallet
{
    public static readonly string[] MenuNames = { "보리밥과 김치 한 상", "장국밥 한 그릇", "삼계탕 한 그릇" };
    public static readonly int[] MenuPrices = { 5, 10, 15 }, MenuHeals = { 40, 80, 100 };
    public int Coins { get; private set; } = 30;
    // 주막 음식을 산다. 성공하면 회복량을 돌려준다.
    // `mourning`: 상중인 가게인지(기본은 노인이 죽었는지).
    public bool Buy(int menu, int health, out int restored, QuestProgress quest = null, bool? mourning = null)
    {
        restored = 0;
        if (menu < 0 || menu >= MenuPrices.Length || !ShopRules.MenuOpen(menu, quest, mourning ?? ShopRules.ElderDead(quest)) || health <= 0 || health >= Combat.MaxPlayerHealth) return false;
        int cost = ShopRules.Price(MenuPrices[menu], quest);
        if (Coins < cost) return false;
        restored = System.Math.Min(Combat.MaxPlayerHealth - health, MenuHeals[menu]);
        Coins -= cost;
        return true;
    }
    // 화살을 산다. 성공하면 산 수를 돌려준다.
    public bool BuyArrows(bool bundle, int carried, out int bought, QuestProgress quest = null, bool? mourning = null)
    {
        bought = 0;
        if (bundle && !ShopRules.BundleOpen(quest, mourning ?? ShopRules.ElderDead(quest))) return false;
        int count = bundle ? ShopRules.BundleSize : 1;
        if (carried + count > ShopRules.MaxArrows) return false;
        int cost = ShopRules.Price(bundle ? ShopRules.BundlePrice : ShopRules.ArrowPrice, quest);
        if (Coins < cost) return false;
        Coins -= cost;
        bought = count;
        return true;
    }
    // 보부상 보따리의 인벤토리 물건(JinaItems), 같은 할인.
    public bool Spend(int basePrice, QuestProgress quest = null)
    {
        int cost = ShopRules.Price(basePrice, quest);
        if (Coins < cost) return false;
        Coins -= cost;
        return true;
    }
    public void Refund(int basePrice, QuestProgress quest = null) { Coins += ShopRules.Price(basePrice, quest); }
    public void Earn(int amount) { Coins += amount; }
    public void SetCoins(int amount) { Coins = Mathf.Max(0, amount); }
    public void Reset() { Coins = 30; }
}
