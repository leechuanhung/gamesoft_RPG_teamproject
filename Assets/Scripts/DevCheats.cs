using UnityEngine;

// 개발자 치트, 플레이어용 조작 안내에는 없다.
//   F8: 치트 창을 열고 닫는다(열려 있는 동안 게임이 멈춘다). 창에서 숫자키로 고른다:
//       1 무적(켜기/끄기), 2 한 방에 쓰러뜨리기(켜기/끄기), 3 체력·스테미너·화살 가득, 4 엽전 +100,
//       5 선악 +10, 6 선악 −10, 7 이전 맵으로, 8 다음 맵으로, 9 이 맵의 적 모두 쓰러뜨리기, 0 봉화 산성 퍼즐 풀기.
//       켜고 끄는 치트(무적, 한 방)는 R로 다시 시작해도 남고, 켜져 있으면 화면 아래에 표시된다.
//   F7: 보따리에 등록된 모든 일반 아이템과 은비녀를 하나씩 지급한다.
//   F9: 도깨비 정화 루트를 끝낸 상태로 만든다. 들판 유령 전멸(동전 획득,
//       비석 봉인 해제), 노인 이야기 듣기, 비석 표식(달·강·산) 읽기, 도깨비에게 부적 사용
//       (+30, 가시덤불이 시듦), 그다음 선비를 나루터 뱃사공 옆에 둔다.
//       도깨비를 이미 해결했으면 나루터로 옮기기만 한다.
//   F10: 선한 길로 한양 가는 길까지. 도깨비 정화(+30), 콩쥐 요괴 정화(+50) → 선악 +80, 봉화 산성 입구로.
//   F11: 악한 길로 한양 가는 길까지. 노인을 잃고(−40) 도깨비 토벌(+10), 콩밭 마을 사람 넷을 잃고(−40)
//        콩쥐 요괴 토벌(+10) → 선악 −60(관군 3단계), 산성 너머 버려진 역참 입구로.
//   F12: 진행과 선악은 그대로 두고 좌의정 댁 입구로(최종 보스를 바로 보려고).
public class DevCheats : MonoBehaviour
{
    public Combat combat;
    public const KeyCode AllItems = KeyCode.F7, Panel = KeyCode.F8, PurifyAndFerry = KeyCode.F9, GoodRoad = KeyCode.F10, WickedRoad = KeyCode.F11, ToManor = KeyCode.F12;
    public static readonly Vector2 FerrySpot = new Vector2(WorldBuilder.Ferryman.x - 2, .3f);
    public const int OneHitDamage = 9999, CoinGift = 100, HonorStep = 10;
    public float UsedAt { get; private set; } = -1;
    private string said = "";
    // 치트 창이 열려 있는지(Combat.Paused).
    public bool Open { get; private set; }
    // 무적: 어떤 공격에도 다치지 않는다(Combat.HurtPlayer).
    public bool God { get; private set; }
    // 한 방: 선비의 공격이 무엇이든 한 번에 쓰러뜨린다(적마다 피해를 받는 곳에서 Power를 거친다).
    public bool OneHit { get; private set; }
    public int Power(int amount) => OneHit ? OneHitDamage : amount;
    public string Said => said;

    void Update()
    {
        if (combat == null) return;
        if (Input.GetKeyDown(AllItems)) { GiveAllItems(); return; }
        if (Input.GetKeyDown(Panel))
        {
            if (Open) Close();
            else if (!combat.Paused && combat.PlayerHealth > 0) Show();
            return;
        }
        if (Open)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            for (int i = 0; i < 10; i++)
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i)) Use(i);
        }
        // F9~F12는 창이 열려 있어도 쓸 수 있다(창을 닫고 옮긴다).
        if (Input.GetKeyDown(PurifyAndFerry)) { Close(); SkipToFerry(); }
        if (Input.GetKeyDown(GoodRoad)) { Close(); SkipToRoad(false); }
        if (Input.GetKeyDown(WickedRoad)) { Close(); SkipToRoad(true); }
        if (Input.GetKeyDown(ToManor)) { Close(); SkipToManor(); }
    }

    // 등록된 일반 아이템과 은비녀를 각각 하나씩 보따리에 넣는다. 기존 수량과 효과는 그대로 둔다.
    public void GiveAllItems()
    {
        var bag = JinaItems.Inventory;
        if (bag == null) { Say("보따리가 없어 아이템을 지급하지 못했습니다."); return; }
        int given = 0, failed = 0;
        foreach (var item in JinaItems.All)
        {
            if (bag.TryAdd(item)) given++;
            else failed++;
        }
        if (bag.TryAdd(JinaItems.Keepsake)) given++;
        else failed++;
        if (bag.TryAdd(JinaItems.Wisp)) given++;
        else failed++;
        // 정화 부적은 보따리 슬롯이 아니라 QuestProgress.HasCharm으로 보관된다.
        var quest = combat.Quest != null ? combat.Quest.progress : null;
        if (quest != null && !quest.HasCharm && quest.Outcome == EncounterOutcome.Unresolved)
        {
            quest.HearClue();
            int[] signs = { 2, 1, 3 };
            while (!quest.HasCharm && quest.PuzzleStep < signs.Length)
                quest.ChooseSymbol(signs[quest.PuzzleStep]);
        }
        Say("아이템 " + given + "종을 1개씩 지급했습니다. 정화 부적: "
            + (quest != null && quest.HasCharm ? "보유" : "획득 불가(퀘스트 진행 상태)")
            + (failed > 0 ? " (보따리 공간 부족: " + failed + "종 미지급)" : ""));
    }

    // 치트 창을 연다: 선비를 멈춘다.
    public void Show() { Open = true; combat.player.CanMove = false; }
    public void Close()
    {
        if (!Open) return;
        Open = false;
        combat.player.CanMove = combat.PlayerHealth > 0;
    }

    // 창의 숫자키 `key`(0~9)의 치트를 쓴다. 결과 문장을 돌려준다.
    public string Use(int key)
    {
        if (combat == null || combat.Quest == null) return "";
        var q = combat.Quest.progress;
        switch (key)
        {
            case 1: God = !God; return Say(God ? "무적을 켰습니다. 어떤 공격에도 다치지 않습니다." : "무적을 껐습니다.");
            case 2: OneHit = !OneHit; return Say(OneHit ? "한 방을 켰습니다. 무엇으로 치든 한 번에 쓰러집니다." : "한 방을 껐습니다.");
            case 3:
                combat.RestoreHealth(Combat.MaxPlayerHealth);
                combat.player.RefillStamina();
                combat.AddArrows(ShopRules.MaxArrows);
                return Say("체력, 스테미너, 화살을 가득 채웠습니다.");
            case 4:
                if (combat.Tavern != null) combat.Tavern.wallet.Earn(CoinGift);
                return Say("엽전 " + CoinGift + "문을 얻었습니다. (지금 " + Coins + "문)");
            case 5: q.CheatHonor(HonorStep); return Say(HonorNow(q));
            case 6: q.CheatHonor(-HonorStep); return Say(HonorNow(q));
            case 7: return GoMap(combat.Maps.Current - 1);
            case 8: return GoMap(combat.Maps.Current + 1);
            case 9: return Say("이 맵의 적 " + ClearMap() + "을(를) 쓰러뜨렸습니다.");
            default: return SolveFort();
        }
    }
    string Say(string s) { said = "[치트] " + s; UsedAt = Time.time; return said; }
    int Coins => combat.Tavern != null ? combat.Tavern.wallet.Coins : 0;
    static string HonorNow(QuestProgress q) => "선악 " + q.Honor + " (관군 " + (RoadMobs.Rank(q.Honor) == 0 ? "안 나옴" : RoadMobs.Rank(q.Honor) + "단계") + ")";

    // 선비를 `at`에 세우고 그 맵으로 바로 둔다(어둠 전환 없이).
    void Put(Vector2 at)
    {
        var player = combat.player;
        player.transform.position = at;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) { body.velocity = Vector2.zero; body.position = at; }
        if (combat.Maps != null) combat.Maps.Jump();
        View.Focus(0, 0);
    }
    // `map`번 맵의 왼쪽 입구로 옮긴다.
    public string GoMap(int map)
    {
        map = Mathf.Clamp(map, 0, Maps.All.Length - 1);
        float x = Maps.All[map].left + 1.5f;
        Put(new Vector2(x, WorldBuilder.Top(x) + .8f));
        return Say(Maps.All[map].name + "(으)로 왔습니다.");
    }
    // 지금 맵의 적을 모두 쓰러뜨린다: 유령, 도깨비불·두억시니·관군, 이 맵에서 싸우는 보스. 쓰러뜨린 수.
    public int ClearMap()
    {
        int map = combat.Maps.Current, n = 0;
        foreach (var camp in combat.Camps)
        {
            if (camp == null) continue;
            foreach (var m in camp.mobs)
                if (m.hp > 0 && Maps.Index(m.art.transform.position.x) == map) { camp.Hit(m, OneHitDamage); n++; }
        }
        if (combat.Mobs != null)
            foreach (var m in combat.Mobs.mobs)
                if (m.Alive && !m.witness && !m.leaving && m.map == map) { combat.Mobs.Hit(m, OneHitDamage); n++; }
        if (!combat.EnemyDefeated && Maps.Index(combat.BossPosition.x) == map) { combat.SlayBossForCheat(); n++; }
        var kong = combat.KongBoss;
        if (kong != null && kong.Fighting && Maps.Index(kong.Feet.x) == map && kong.Damage(OneHitDamage, false)) n++;
        var final = combat.FinalBoss;
        if (final != null && final.Fighting && final.Damage(OneHitDamage)) n++;
        return n;
    }
    // 봉화를 남은 차례대로 모두 올려 산성 문을 연다.
    public string SolveFort()
    {
        var fort = combat.Fort;
        if (fort == null) return "";
        if (fort.Open) return Say("봉화 산성 문은 이미 열려 있습니다.");
        for (int s = fort.Step; s < BeaconFort.Order.Length; s++) fort.Light(BeaconFort.Order[s]);
        return Say("봉화 다섯이 모두 올라 산성 문이 열렸습니다.");
    }

    // 진행은 그대로 두고 좌의정 댁 입구로 옮긴다.
    public bool SkipToManor()
    {
        if (combat == null || combat.Quest == null) return false;
        if (combat.Intro != null && combat.Intro.IsPlaying) combat.Intro.Skip();
        if (combat.PlayerHealth <= 0 || combat.Paused) return false;
        Put(new Vector2(WorldBuilder.ManorStart + 1.5f, .3f));
        Say("좌의정 댁 입구로 왔습니다. 누각으로 다가가면 술자리가 시작됩니다. (" + HonorNow(combat.Quest.progress) + ")");
        return true;
    }

    // 도깨비와 콩쥐 요괴를 해결한 상태로 한양 가는 길에 선다(`wicked`: 악한 길).
    public bool SkipToRoad(bool wicked)
    {
        if (combat == null || combat.Quest == null) return false;
        if (combat.Intro != null && combat.Intro.IsPlaying) combat.Intro.Skip();
        if (combat.PlayerHealth <= 0 || combat.Paused) return false;
        var q = combat.Quest.progress;
        if (q.Outcome == EncounterOutcome.Unresolved)
        {
            if (wicked) { q.SacrificeElder(); combat.SlayBossForCheat(); }
            else
            {
                if (combat.Camp != null) { foreach (var m in combat.Camp.mobs) combat.Camp.Hit(m, Camp.GhostHealth); combat.Camp.Unseal(); }
                q.HearClue();
                int[] signs = { 2, 1, 3 };
                for (int i = 0; i < 3 && !q.HasCharm; i++) q.ChooseSymbol(signs[q.PuzzleStep]);
                combat.TryPurifyBoss();
            }
        }
        if (combat.KongBoss != null && !combat.KongBoss.Resolved)
        {
            if (wicked) for (int i = 0; i < 4; i++) q.LoseVillager();
            combat.KongBoss.SkipFight(!wicked);
        }
        Put(wicked ? new Vector2(WorldBuilder.FortEnd + 1.5f, .3f) : new Vector2(WorldBuilder.FortStart + 1.5f, .3f));
        said = wicked ? "[개발자] 악한 길로 버려진 역참에 왔습니다. (선악 " + q.Honor + ", 관군 " + RoadMobs.Rank(q.Honor) + "단계)"
            : "[개발자] 선한 길로 봉화 산성에 왔습니다. (선악 " + q.Honor + ")";
        UsedAt = Time.time;
        return true;
    }

    // 도깨비 정화 루트를 끝낸 상태로 만들고 나루터로 옮긴다.
    public bool SkipToFerry()
    {
        if (combat == null || combat.Quest == null) return false;
        if (combat.Intro != null && combat.Intro.IsPlaying) combat.Intro.Skip();
        if (combat.PlayerHealth <= 0 || combat.Paused) return false;
        var q = combat.Quest.progress;
        bool purified = false;
        if (q.Outcome == EncounterOutcome.Unresolved)
        {
            if (combat.Camp != null) { foreach (var m in combat.Camp.mobs) combat.Camp.Hit(m, Camp.GhostHealth); combat.Camp.Unseal(); }
            q.HearClue();
            // 달, 강, 산. 비석에서 어디까지 했든 거기서부터.
            int[] signs = { 2, 1, 3 };
            for (int i = 0; i < 3 && !q.HasCharm; i++) q.ChooseSymbol(signs[q.PuzzleStep]);
            purified = combat.TryPurifyBoss();
        }
        Put(FerrySpot);
        UsedAt = Time.time;
        said = purified ? "[개발자] 도깨비 정화 루트를 마치고 나루터로 왔습니다. (선악 +30)" : "[개발자] 나루터로 왔습니다.";
        return true;
    }

    // 치트 창, 켜진 치트 표시, 쓴 뒤의 알림.
    void OnGUI()
    {
        if (combat == null) return;
        KoreanUI.Apply();
        if (Open) { DrawPanel(); return; }
        if (combat.Paused || combat.PlayerHealth <= 0) return;
        if (God || OneHit)
        {
            string on = "치트: " + (God ? "무적" : "") + (God && OneHit ? " · " : "") + (OneHit ? "한 방" : "");
            float w = JinaUI.Width(on, 13, true) + 24;
            var r = new Rect((JinaUI.W - w) / 2, JinaUI.H - 60, w, 22);
            JinaUI.HudPanel(r);
            JinaUI.Text(r, on, 13, new Color(1, .6f, .5f), TextAnchor.MiddleCenter, true);
        }
        if (UsedAt >= 0 && Time.time - UsedAt < 3f) JinaUI.Toast(said);
    }

    // 창에 보일 줄: 숫자키 치트, 그다음 F키 이동.
    public string[] Lines()
    {
        var q = combat.Quest != null ? combat.Quest.progress : null;
        string On(bool b) => b ? "켜짐" : "꺼짐";
        return new[]
        {
            "1   무적   (" + On(God) + ")",
            "2   한 방에 쓰러뜨리기   (" + On(OneHit) + ")",
            "3   체력 · 스테미너 · 화살 가득",
            "4   엽전 +" + CoinGift + "   (지금 " + Coins + "문)",
            "5   선악 +" + HonorStep + "      6   선악 −" + HonorStep + "   (" + (q != null ? HonorNow(q) : "") + ")",
            "7   이전 맵으로      8   다음 맵으로   (지금: " + (combat.Maps != null ? combat.Maps.Here.name : "") + ")",
            "9   이 맵의 적 모두 쓰러뜨리기",
            "0   봉화 산성 퍼즐 풀기" + (combat.Fort != null && combat.Fort.Open ? "   (열림)" : ""),
            "",
            "F7    모든 아이템 1개씩 지급 (보따리)",
            "F9    도깨비 정화하고 나루터로",
            "F10   선한 길로 봉화 산성 (선악 +80)",
            "F11   악한 길로 버려진 역참 (선악 −60, 관군 3단계)",
            "F12   좌의정 댁 입구로 (진행은 그대로)",
        };
    }
    void DrawPanel()
    {
        GUI.depth = -60;
        var lines = Lines();
        const float line = 26;
        float w = Mathf.Min(600, JinaUI.W - 40), h = 56 + lines.Length * line + 64;
        var inner = JinaUI.Window(new Rect((JinaUI.W - w) / 2, (JinaUI.H - h) / 2, w, h), "개발자 치트", "F8 · Esc 닫기");
        for (int i = 0; i < lines.Length; i++)
            JinaUI.Text(new Rect(inner.x, inner.y + i * line, inner.width, line), lines[i], 16, i < 8 ? JinaUI.TextDark : JinaUI.TextDim, TextAnchor.MiddleLeft, i < 8);
        string foot = UsedAt >= 0 && Time.time - UsedAt < 4f ? said : "숫자키로 고르세요. 창이 열려 있는 동안 게임이 멈춥니다.";
        JinaUI.Text(new Rect(inner.x, inner.y + lines.Length * line + 8, inner.width, 40), foot, 14, JinaUI.Red, TextAnchor.UpperLeft);
    }
}
