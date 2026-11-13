using UnityEngine;

// ===== 주막 (PrototypeTavern) =====
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
    string Greeting() => Greeting(Progress);
    void Close() { IsOpen = false; combat.player.CanMove = combat.PlayerHealth > 0; }
    // E로 주막 창을 열고 닫는다.
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
    // 주막 메뉴 하나를 산다.
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
    // 주막 창을 그린다.
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

// ===== 보부상 상점 (PrototypePeddler) =====
// 주막 옆 보부상: 화살과 길에서 쓸 물건(JinaItems, 인벤토리로)을 판다.
// 주막과 돈주머니를 같이 쓴다.
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

    // 보부상을 만든다.
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
    string Greeting() => Greeting(Progress);
    void Close() { IsOpen = false; combat.player.CanMove = combat.PlayerHealth > 0; }
    // E로 보부상 창을 열고 닫는다.
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
    // 화살을 산다(`bundle`: 묶음).
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
    // 물건은 인벤토리로 들어간다. 자리가 없으면 돈을 돌려준다.
    public void Purchase(JinaItem item)
    {
        var bag = JinaItems.Inventory;
        if (!ShopRules.ItemOpen(item, Progress)) message = "부적은 흉사가 난 마을에서 풀지 않겠소.";
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
        PrototypeKoreanUI.Apply();
        if (Near && !IsOpen && combat.PlayerHealth > 0 && !combat.Paused) JinaUI.Prompt("E: 보부상 등짐 보기");
        if (!IsOpen) return;
        // 화살, 그다음 한 줄에 둘씩 물건: 아이콘, 이름, 가격, 효과.
        const float gap = 8;
        float w = Mathf.Min(780, JinaUI.W - 30), half = (w - JinaUI.WindowPad * 2 - gap) / 2;
        int rows = 1 + (JinaItems.All.Length + 1) / 2, discount = ShopRules.Discount(Progress);
        float say = JinaUI.SayHeight("보부상:  " + message, w - JinaUI.WindowPad * 2);
        float h = JinaUI.TitleHeight + 12 + say + gap + rows * (JinaUI.GoodHeight + gap) + 28 + JinaUI.WindowPad;
        var box = new Rect((JinaUI.W - w) / 2, JinaUI.H - h - 14, w, h);
        var inside = JinaUI.Window(box, "보부상 등짐", "엽전 " + Wallet.Coins + "문   ·   화살 " + combat.Arrows + "/" + ShopRules.MaxArrows + (discount > 0 ? "   ·   " + discount + "% 할인" : "") + "   ·   E / Esc 나가기");
        JinaUI.Say(new Rect(inside.x, inside.y, inside.width, say), "보부상", message);
        float top = inside.y + say + gap;
        bool bundle = ShopRules.BundleOpen(Progress);
        if (JinaUI.Good(new Rect(inside.x, top, half, JinaUI.GoodHeight), JinaItems.Get("arrows").Icon, "1 · 화살 한 대", Cost(ShopRules.ArrowPrice), "화살통에 바로 넣습니다", true)) Purchase(false);
        if (JinaUI.Good(new Rect(inside.x + half + gap, top, half, JinaUI.GoodHeight), JinaItems.Get("arrows").Icon, "2 · 화살 " + ShopRules.BundleSize + "대 묶음",
            bundle ? Cost(ShopRules.BundlePrice) : "", bundle ? "화살통에 바로 넣습니다" : "흉사로 팔지 않음", bundle)) Purchase(true);
        for (int i = 0; i < JinaItems.All.Length; i++)
        {
            var item = JinaItems.All[i];
            var r = new Rect(inside.x + (i % 2) * (half + gap), top + (1 + i / 2) * (JinaUI.GoodHeight + gap), half, JinaUI.GoodHeight);
            bool open = ShopRules.ItemOpen(item, Progress);
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
    void OnDestroy() { if (marker != null) Destroy(marker.gameObject); }
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
    public static bool MenuOpen(int menu, QuestProgress quest) =>
        menu == BannedWhenElderDead ? !ElderDead(quest) : menu != SpecialMenu || Honor(quest) >= 30;
    public static bool BundleOpen(QuestProgress quest) => !ElderDead(quest);
    // 마을에 불행이 닥친 뒤 보부상은 부적을 싸 두고, 음식과 화살만 판다.
    public static bool ItemOpen(JinaItem item, QuestProgress quest) =>
        !ElderDead(quest) || item.effect == JinaItem.Effect.Heal || item.effect == JinaItem.Effect.Arrows;
}

public class TavernWallet
{
    public static readonly string[] MenuNames = { "보리밥과 김치 한 상", "장국밥 한 그릇", "삼계탕 한 그릇" };
    public static readonly int[] MenuPrices = { 5, 10, 15 }, MenuHeals = { 40, 80, 100 };
    public int Coins { get; private set; } = 30;
    // 주막 음식을 산다. 성공하면 회복량을 돌려준다.
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
    // 화살을 산다. 성공하면 산 수를 돌려준다.
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
    public void Reset() { Coins = 30; }
}

// ===== 주모·보부상 잡담 (PrototypeChatter) =====
// 선비가 가까이 있으면 주모와 보부상이 잡담한다: 이따금 한 사람이 말하고
// 다른 사람이 답한다. 이야기는 진행을 따른다(도깨비가 날뜀, 토벌, 정화, 노인이 죽음).
public class PrototypeChatter : MonoBehaviour
{
    public PrototypeCombat combat;
    // 한 줄의 초, 주고받기 사이, 첫 주고받기 전, 엿들으려면 선비가 얼마나 가까워야 하는지.
    public const float LineLength = 3.4f, Gap = 8f, FirstAt = 4f, Range = 8f;
    // 주고받기 하나는 주모 한 줄, 보부상 한 줄.
    static readonly string[][] Uneasy =
    {
        new[] { "요즘 숲 쪽에서 밤마다 도깨비불이 보인다지요?", "그래서 산 넘는 장꾼이 줄었소. 장사가 영 시원찮구려." },
        new[] { "노인장께서 오늘도 숲 쪽을 한참 바라보시더이다.", "젊어서는 칼 꽤나 쓰셨다는 말이 있지요. 지금도 칼을 품고 다니신다던가." },
        new[] { "저 선비님 행색을 보니 과거 보러 가시는 길인가 보오.", "한양까지는 아직 멀었소. 화살은 넉넉히 챙겨 가시구려." },
        new[] { "서쪽 비석 터에 요괴가 들끓는다니 해가 지면 문을 걸어 잠가야겠소.", "망한 나라 군졸 귀신들이라던데… 무덤도 없이 떠도는 것들이지." },
        new[] { "숲을 지나면 나루터요. 뱃사공 영감이 요새 통 배를 띄우지 않는다더이다.", "강에서 밤마다 누가 운다지 뭐요. 물에 빠진 이들의 넋이라던가." },
        new[] { "강 건너 콩밭 마을에 콩쥐라는 효녀가 있다지요? 의붓동생한테 그리 시달린다던데.", "팥쥐 그것 말이오? 제 어미가 강에 빠져 죽은 것도 다 그 딸 업보지, 쯧쯧." },
        new[] { "그 집 젓갈이 그리 맛있다지요? 한번 들여와 볼까.", "값이 싸서 나도 두어 번 받아 왔는데… 이상하게 팔고 나면 찜찜합디다. 왜 그런지는 모르겠소." },
        new[] { "손님, 낮술은 안 되오. 과거 보러 가는 분이 얼굴 벌게서 어쩌시려고.", "주모는 저래 놓고 밤이면 혼자 한 사발씩 하지요." },
        new[] { "우리 집 국밥은 뼈를 사흘 고아요. 아무 데서나 못 먹는 맛이지.", "그 소리를 삼 년째 듣는데, 뼈는 늘 그 뼈 같단 말이오." },
    };
    static readonly string[][] Slain =
    {
        new[] { "도깨비가 쓰러졌다니 이제 밤길도 걸을 만하겠소.", "산 너머 장도 다시 서겠구려. 내일은 짐을 더 꾸려 와야겠소." },
        new[] { "그런데 그 도깨비, 본디 산을 지키던 녀석이었다던데…", "쉿, 다 지난 일이오. 산이 조용해졌으면 그만이지." },
        new[] { "산은 조용해졌는데, 강 건너 고개에 도적 떼가 생겼다더이다.", "도적들 눈빛이 도깨비처럼 퀭하다던데… 같은 것에 홀린 게 아닌가 싶소." },
        new[] { "나무하러 가던 아이들이 그 자리에 돌을 던지더이다. 잘 죽었다고.", "…애들 눈이 무섭구려. 노인장은 아무 말 없이 숲만 보고 계십디다." },
    };
    static readonly string[][] Purified =
    {
        new[] { "도깨비가 순해졌다더니, 아까 숲 어귀에서 꾸벅 인사를 하더이다.", "허허, 산길 길동무로 삼아도 되겠소. 장꾼들이 좋아하겠구려." },
        new[] { "칼 대신 부적을 쓰셨다니, 보기 드문 선비시오.", "그러게 말이오. 저런 분이 급제해야 나라가 바로 서지." },
        new[] { "도깨비 말이, 망령들이 강과 고개에도 손을 뻗었다더이다.", "가는 길마다 홀린 것이 있겠구려. 선비님, 부적은 넉넉하시오?" },
        new[] { "도깨비가 나뭇짐을 져다 주길래 막걸리 한 사발 줬더니 울더이다.", "허허. 몇백 년 만에 얻어먹는 술이었겠구려." },
    };
    static readonly string[][] Mourning =
    {
        new[] { "노인장 계시던 자리가 텅 비었구려…", "장터마다 이 소식을 전해야 하니 발걸음이 무겁소." },
        new[] { "그 연세에 칼을 뽑아 우리를 먼저 피하게 하셨지요.", "…우리 목숨은 노인장께 빚진 것이오." },
        new[] { "아침마다 노인장 드시던 국 한 그릇을 저도 모르게 푸고 있구려.", "…내려놓지 마시오. 그 자리에 놓아 두시오." },
    };

    private float nextAt = FirstAt, startedAt = -1;
    private int count;
    private string[] current;

    // 진행 상태에 맞는 잡담 주제.
    public static string[][] Topics(QuestProgress q) => q.ElderDead ? Mourning
        : q.Outcome == EncounterOutcome.Purified ? Purified : q.Outcome == EncounterOutcome.Slain ? Slain : Uneasy;
    public bool Talking => startedAt >= 0;
    // 둘 다 제자리, 다른 일 없음, 선비가 주막과 좌판에서 들리는 거리에 있음.
    public bool CanChat => combat != null && combat.Quest != null && combat.Village != null && !combat.Paused
        && combat.PlayerHealth > 0 && combat.Village.KeeperHome && combat.Village.PeddlerHome
        && (!combat.Village.Alarmed || combat.Village.Cleared)
        && Mathf.Abs(combat.player.transform.position.x - (PrototypeWorld.Tavern.x + PrototypeWorld.Peddler.x) / 2) < Range;

    public void ResetChatter() { nextAt = Time.time + FirstAt; startedAt = -1; count = 0; }
    // 잡담을 시작한다.
    public void Begin()
    {
        var topics = Topics(combat.Quest.progress);
        current = topics[count++ % topics.Length];
        startedAt = Time.time;
    }

    // 잡담 줄을 넘기고, 끊기면 기다린다.
    void Update()
    {
        if (combat == null) return;
        if (Talking)
        {
            // 끊겼거나(가게가 열림, 경보가 울림) 끝났으면 다음까지 잠시 기다린다.
            if (!CanChat || Time.time - startedAt >= LineLength * 2) { startedAt = -1; nextAt = Time.time + Gap; }
        }
        else if (Time.time >= nextAt && CanChat) Begin();
    }

    // 잡담 말풍선을 그린다.
    void OnGUI()
    {
        if (!Talking || !CanChat) return;
        bool keeper = Time.time - startedAt < LineLength;
        // 가게 가까이에서는 가게 안내가 화면 아래를 쓰고, 말풍선은 말하는 사람 위에 뜬다.
        var at = keeper ? new Vector3(PrototypeWorld.Tavern.x, 1.4f, 0) : new Vector3(PrototypeWorld.Peddler.x, 1.55f, 0);
        if (SpeechBubble.OnScreen(at)) SpeechBubble.Draw(at, keeper ? "주모" : "보부상", current[keeper ? 0 : 1]);
    }
}
