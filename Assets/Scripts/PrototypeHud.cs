using UnityEngine;

// The always-on display: the seonbi's state and current goal (top left), a controls guide under them (H folds it),
// the moral gauge and counts (top right), the boss's bar (top centre) and the route between maps (bottom centre).
// Panels size themselves to their text. It steps aside for cinematics, and shrinks while a window is open.
public class PrototypeHud : MonoBehaviour
{
    public PrototypeCombat combat;
    public const float PanelWidth = 292, Margin = 12;
    public bool GuideOpen { get; private set; } = true;

    static readonly string[] Guide =
    {
        "이동 A·D / ←→     달리기 Shift",
        "도약 스페이스     물러서기 K",
        "공격 J·클릭     무기 바꾸기 Q",
        "대화·조사 E     정화 F     보따리 I",
        "주황빛은 공격 예고     다시 시작 R",
    };

    // Nothing over the intro, the ending, the encounter or the game-over screen.
    public bool Hidden => combat == null || combat.Quest == null || combat.PlayerHealth <= 0
        || (combat.Intro != null && combat.Intro.IsPlaying) || (combat.Ending != null && combat.Ending.Active)
        || (combat.Encounter != null && combat.Encounter.Playing) || (combat.Stay != null && combat.Stay.Playing);

    void Update()
    {
        if (combat != null && !combat.Paused && Input.GetKeyDown(KeyCode.H)) GuideOpen = !GuideOpen;
    }

    // What to do next, from where he is and how the story stands.
    public string Goal()
    {
        var q = combat.Quest.progress;
        float x = combat.player.transform.position.x;
        if (!combat.EnemyDefeated)
        {
            if (q.HasCharm) return "도깨비 가까이에서 F로 저주를 풀거나, 칼과 활로 토벌하세요. 해결하면 숲 끝의 가시덤불이 걷힙니다.";
            if (combat.Camp != null && combat.Camp.Remaining > 0 && x < PrototypeWorld.ForestStart)
                return "숲의 도깨비를 해결해야 나루터로 갈 수 있습니다. 서쪽 유령을 모두 물리치면 비석 터에서 정화 부적을 얻을 수 있습니다.";
            return "숲의 도깨비를 토벌하거나, 비석 터의 부적으로 정화하세요. 해결하면 숲 끝의 가시덤불이 걷힙니다.";
        }
        if (x < PrototypeWorld.FerryStart) return "숲 동쪽 끝을 지나 나루터로 가세요.";
        if (x < PrototypeWorld.RiverRight) return "나룻배를 타고 강을 건너세요.";
        if (x < PrototypeWorld.KongEnd && combat.Kong != null && !combat.Kong.Revealed)
        {
            if (!combat.Kong.Stayed) return "콩밭 마을 사람들의 이야기를 들어 보세요. 콩쥐와 팥쥐에게도 말을 걸어 보세요.";
            if (combat.Kong.Clues < 3) return "간밤의 소리가 마음에 걸립니다. 팥쥐에게 말을 걸어 보고, 장독대 둘레를 살펴보세요.";
            return "이제 콩쥐에게 따져 물을 때입니다.";
        }
        return "고개를 넘어 한양으로 가는 금빛 표식까지 가세요.";
    }

    void OnGUI()
    {
        if (Hidden) { JinaUI.ToastTop = 14; return; }
        GUI.depth = 10;   // every window draws over the HUD
        PrototypeKoreanUI.Apply();
        DrawStatus();
        if (!combat.InventoryPauseActive) DrawMorals();
        DrawBossBar();
        DrawRoute();
    }

    void DrawStatus()
    {
        float x = Margin, w = PanelWidth, inner = w - 24, y = Margin;
        bool full = !combat.Paused;
        string goal = full ? Goal() : null;
        string charms = (combat.Quest.progress.HasCharm ? "정화 부적" : "")
            + (combat.Warded ? (combat.Quest.progress.HasCharm ? " · " : "") + "액막이" : "");
        if (combat.player.Hasted) charms += (charms == "" ? "" : " · ") + "짚신 " + Mathf.CeilToInt(combat.player.HasteUntil - Time.time) + "초";
        float charmHeight = JinaUI.Height(charms, 13, inner), goalHeight = JinaUI.Height(goal, 13, inner);
        float h = 88 + (charms == "" ? 0 : charmHeight + 4) + (goal == null ? 0 : goalHeight + 30);
        JinaUI.HudPanel(new Rect(x, y, w, h));
        float cy = y + 10;
        JinaUI.Text(new Rect(x + 12, cy, inner, 20), "선비", 16, JinaUI.Gold, TextAnchor.MiddleLeft, true);
        JinaUI.Text(new Rect(x + 12, cy, inner, 20), combat.UsingBow ? "활" : "환도", 14, JinaUI.Bright, TextAnchor.MiddleRight);
        cy += 24;
        JinaUI.Bar(new Rect(x + 12, cy, inner, 20), combat.PlayerHealth / (float)PrototypeCombat.MaxPlayerHealth, new Color(.72f, .2f, .17f),
            "체력 " + combat.PlayerHealth + " / " + PrototypeCombat.MaxPlayerHealth);
        cy += 26;
        JinaUI.Text(new Rect(x + 12, cy, inner, 18), "화살 " + combat.Arrows + " / " + ShopRules.MaxArrows, 14, JinaUI.Bright, TextAnchor.MiddleLeft);
        JinaUI.Text(new Rect(x + 12, cy, inner, 18), "엽전 " + (combat.Tavern != null ? combat.Tavern.wallet.Coins : 0) + "문", 14, JinaUI.Gold, TextAnchor.MiddleRight);
        cy += 22;
        if (charms != "") { JinaUI.Text(new Rect(x + 12, cy, inner, charmHeight), charms, 13, new Color(.7f, .9f, .75f)); cy += charmHeight + 4; }
        if (goal != null)
        {
            JinaUI.Fill(new Rect(x + 12, cy + 2, inner, 1), JinaUI.WoodLight);
            JinaUI.Text(new Rect(x + 12, cy + 6, inner, 16), "할 일", 12, JinaUI.Gold, TextAnchor.UpperLeft, true);
            JinaUI.Text(new Rect(x + 12, cy + 24, inner, goalHeight), goal, 13, JinaUI.Bright);
        }
        if (!full) return;
        y += h + 6;
        if (!GuideOpen)
        {
            var tag = new Rect(x, y, 120, 24);
            JinaUI.HudPanel(tag);
            JinaUI.Text(tag, "H: 조작 안내", 12, JinaUI.Faint, TextAnchor.MiddleCenter);
            return;
        }
        float guideHeight = Guide.Length * 19 + 34;
        JinaUI.HudPanel(new Rect(x, y, w, guideHeight));
        JinaUI.Text(new Rect(x + 12, y + 7, inner, 16), "조작", 12, JinaUI.Gold, TextAnchor.UpperLeft, true);
        JinaUI.Text(new Rect(x + 12, y + 7, inner, 16), "H: 접기", 12, JinaUI.Faint, TextAnchor.UpperRight);
        for (int i = 0; i < Guide.Length; i++) JinaUI.Text(new Rect(x + 12, y + 26 + i * 19, inner, 18), Guide[i], 13, JinaUI.Bright);
    }

    void DrawMorals()
    {
        var q = combat.Quest.progress;
        float w = PanelWidth, x = Screen.width - w - Margin, inner = w - 24;
        string[] rows =
        {
            q.ElderDead ? "노인 희생 (선악 −40)" : "노인 생존",
            combat.Camp != null && combat.Camp.Remaining > 0 ? "들판 유령 " + combat.Camp.Remaining + "마리 남음" : null,
            combat.River != null && combat.RiverFelled < PrototypeCombat.KeepsakeAt && (combat.RiverFelled > 0 || combat.River.Inside(combat.player.transform.position))
                ? "물귀신 " + combat.RiverFelled + " / " + PrototypeCombat.KeepsakeAt : null,
            combat.Kong != null && combat.Kong.InVillage && !combat.Kong.Revealed ? "콩밭 단서 " + combat.Kong.Clues + " / 3" : null,
        };
        int shown = 0; foreach (var row in rows) if (row != null) shown++;
        JinaUI.HudPanel(new Rect(x, Margin, w, 58 + shown * 19));
        float y = Margin + 9;
        string state = q.Honor > 0 ? "선행" : q.Honor < 0 ? "악행" : "중립";
        JinaUI.Text(new Rect(x + 12, y, inner, 18), "선악", 14, JinaUI.Gold, TextAnchor.MiddleLeft, true);
        JinaUI.Text(new Rect(x + 12, y, inner, 18), state + " " + (q.Honor > 0 ? "+" : "") + q.Honor, 14, JinaUI.Bright, TextAnchor.MiddleRight);
        y += 22;
        // The gauge: red for evil, green for good, a white marker at the current honour.
        JinaUI.Fill(new Rect(x + 12, y, inner / 2, 10), new Color(.62f, .22f, .2f));
        JinaUI.Fill(new Rect(x + 12 + inner / 2, y, inner / 2, 10), new Color(.26f, .56f, .38f));
        JinaUI.Fill(new Rect(x + 12 + inner * (Mathf.Clamp(q.Honor, -100, 100) + 100) / 200f - 2, y - 3, 4, 16), JinaUI.PaperLight);
        y += 17;
        foreach (var row in rows)
            if (row != null) { JinaUI.Text(new Rect(x + 12, y, inner, 18), row, 13, JinaUI.Bright); y += 19; }
    }

    void DrawBossBar()
    {
        JinaUI.ToastTop = Margin + 2;
        if (!combat.BossAlerted || combat.EnemyDefeated || combat.Paused) return;
        float w = JinaUI.CenterWidth, x = (Screen.width - w) / 2;
        JinaUI.HudPanel(new Rect(x, Margin, w, 52));
        JinaUI.Text(new Rect(x, Margin + 6, w, 18), PrototypeEncounter.Name, 14, JinaUI.Gold, TextAnchor.MiddleCenter, true);
        JinaUI.Bar(new Rect(x + 12, Margin + 26, w - 24, 18), combat.EnemyHealth / (float)PrototypeCombat.EnemyMaxHealth, new Color(.55f, .14f, .2f),
            combat.EnemyHealth + " / " + PrototypeCombat.EnemyMaxHealth);
        JinaUI.ToastTop = Margin + 58;
    }

    // Only the neighbours of the current map, so it always fits on one line.
    void DrawRoute()
    {
        if (combat.Paused || combat.Maps == null) return;
        int i = combat.Maps.Current; var all = PrototypeMaps.All;
        string route = (i > 0 ? all[i - 1].name + "  ←  " : "") + "[ " + all[i].name + " ]" + (i + 1 < all.Length ? "  →  " + all[i + 1].name : "");
        float w = Mathf.Min(JinaUI.Width(route, 13) + 28, Screen.width - 20);
        var r = new Rect((Screen.width - w) / 2, Screen.height - 34, w, 26);
        JinaUI.HudPanel(r);
        JinaUI.Text(r, route, 13, JinaUI.Bright, TextAnchor.MiddleCenter);
    }
}
