using UnityEngine;

// The always-on display: the seonbi's state (top left), what to do next under it, a controls guide (H folds it, and it
// folds itself once he has found his feet), the moral gauge and counts (top right), the boss's bar (top centre) and the
// route between maps (bottom centre). Panels size themselves to their text. It steps aside for cinematics, and shrinks
// while a window is open. A blow reddens the edges of the screen, and so does being close to death.
public class PrototypeHud : MonoBehaviour
{
    public PrototypeCombat combat;
    public const float PanelWidth = 276, Margin = 12, GuideFoldsAfter = 22f;
    public bool GuideOpen { get; private set; } = true;
    private bool guideTouched;

    static readonly string[] Guide =
    {
        "이동 A·D / ←→     달리기 Shift",
        "도약 스페이스     대시 A·D 두 번     물러서기 K",
        "공격 J·클릭(연달아 3연격)     무기 Q",
        "대화·조사 E     정화 F     보따리 I",
        "다시 시작 R",
        "주황빛은 공격 예고입니다",
    };

    // Nothing over the intro, the ending, the encounter or the game-over screen.
    public bool Hidden => combat == null || combat.Quest == null || combat.PlayerHealth <= 0
        || (combat.Intro != null && combat.Intro.IsPlaying) || (combat.Ending != null && combat.Ending.Active)
        || (combat.Encounter != null && combat.Encounter.Playing) || (combat.Stay != null && combat.Stay.Playing);

    void Update()
    {
        if (combat == null) return;
        if (!combat.Paused && Input.GetKeyDown(KeyCode.H)) { GuideOpen = !GuideOpen; guideTouched = true; }
        // Once he has played a while the guide gets out of the way; H brings it back.
        if (!guideTouched && GuideOpen && Time.timeSinceLevelLoad > GuideFoldsAfter + 40 && !combat.Paused) { GuideOpen = false; guideTouched = true; }
    }

    // What to do next, from where he is and how the story stands. Short: it sits on the screen all the time.
    public string Goal()
    {
        var q = combat.Quest.progress;
        float x = combat.player.transform.position.x;
        if (!combat.EnemyDefeated)
        {
            if (q.HasCharm) return "도깨비 가까이에서 F로 저주를 풀거나, 칼로 쓰러뜨리세요. 해결하면 숲 끝 가시덤불이 걷힙니다.";
            if (combat.Camp != null && combat.Camp.Remaining > 0 && x < PrototypeWorld.ForestStart)
                return "서쪽 유령을 물리치면 비석 터에서 정화 부적을 얻습니다. 도깨비를 칼로만 상대해도 됩니다.";
            return "숲의 도깨비를 토벌하거나, 비석 터의 부적으로 정화하세요. 해결하면 숲 끝 가시덤불이 걷힙니다.";
        }
        if (x < PrototypeWorld.FerryStart) return "숲 동쪽 끝을 지나 나루터로 가세요.";
        if (x < PrototypeWorld.RiverRight) return "뱃사공과 이야기하고, 나룻배로 강을 건너세요.";
        if (x < PrototypeWorld.KongEnd && combat.Kong != null && !combat.Kong.Revealed)
        {
            if (!combat.Kong.Stayed) return combat.Kong.ReadyForNight ? "마을 사람들과 모두 이야기했습니다. 콩쥐에게 다시 말을 걸어 보세요."
                : "콩밭 마을 사람들의 이야기를 들어 보세요. 콩쥐, 팥쥐, 아낙, 나무꾼 모두에게 말을 걸어 보세요.";
            if (combat.Kong.Clues < 3) return Clue();
            return "모든 것을 알았습니다. 콩쥐와 마주하세요.";
        }
        if (combat.Kong != null && combat.Kong.Revealed) return x < PrototypeWorld.PassStart ? "콩쥐가 뒷산에서 기다립니다. 먼저 팥쥐의 말을 들어 보세요." : "콩쥐가 말한 뒷산은 고개 너머입니다. 금빛 표식까지 가세요.";
        return "고개를 넘어 한양으로 가는 금빛 표식까지 가세요.";
    }
    string Clue()
    {
        var k = combat.Kong;
        if (!k.HeardPatjwi) return "콩쥐가 뒷산으로 오라 합니다. 가기 전에 팥쥐에게 넌지시 물어보세요.";
        if (!k.SawCrocks) return "팥쥐가 말린 장독대를 직접 살펴보세요.";
        return "장독대 곁에 떨어진 종이를 읽어 보세요.";
    }

    void OnGUI()
    {
        if (combat == null) return;
        PrototypeKoreanUI.Apply();
        // The edges of the screen: a flash at each blow, a slow pulse near death. Over the world, under every panel.
        if (combat.PlayerHealth > 0 && !Hidden) DrawHurt();
        if (Hidden) { JinaUI.ToastTop = 14; return; }
        GUI.depth = 10;   // every window draws over the HUD
        DrawStatus();
        if (!combat.InventoryPauseActive) DrawMorals();
        DrawBossBar();
        DrawRoute();
    }

    void DrawHurt()
    {
        float flash = combat.PlayerHurtAt < 0 ? 0 : Mathf.Clamp01(1 - (Time.time - combat.PlayerHurtAt) / .32f);
        float low = combat.PlayerHealth <= PrototypeCombat.MaxPlayerHealth / 4 ? .3f + .16f * Mathf.Sin(Time.time * 4.4f) : 0;
        float a = Mathf.Max(flash * .85f, low);
        if (a <= 0) return;
        var v = JinaUI.Art("vignette");
        if (v == null) return;
        // The picture is black; a red copy comes from drawing it over a red wash with its alpha.
        var saved = GUI.color;
        GUI.color = new Color(.78f, .06f, .05f, a);
        var mat = GUI.matrix;
        GUI.DrawTexture(new Rect(0, 0, JinaUI.W, JinaUI.H), Red(v));
        GUI.color = saved;
    }
    static Texture2D redVignette;
    static Texture2D Red(Texture2D source)
    {
        if (redVignette != null) return redVignette;
        redVignette = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = source.GetPixels32();
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, (byte)Mathf.Min(255, pixels[i].a * 2));
        redVignette.SetPixels32(pixels); redVignette.Apply();
        return redVignette;
    }

    void DrawStatus()
    {
        float x = Margin, w = PanelWidth, inner = w - 24, y = Margin;
        bool full = !combat.Paused;
        // The panel proper: his weapon in a little frame, health, arrows and coins.
        const float height = 72;
        JinaUI.HudPanel(new Rect(x, y, w, height));
        var frame = new Rect(x + 8, y + 8, 40, 40);
        JinaUI.Box(frame, new Color(.2f, .16f, .14f), JinaUI.WoodLight, 1);
        JinaUI.Icon(combat.UsingBow ? "icon_bow" : "icon_sword", frame.x + 4, frame.y + 4, 2);
        JinaUI.Text(new Rect(x + 8, y + 49, 40, 16), "Q", 11, JinaUI.Faint, TextAnchor.MiddleCenter);
        float left = x + 56, width = w - 56 - 10;
        JinaUI.Text(new Rect(left, y + 7, width, 18), "선비", 15, JinaUI.Gold, TextAnchor.MiddleLeft, true);
        // The combo's three small pips while the sword is in reach of another blow.
        if (!combat.UsingBow && combat.ComboStep >= 0)
            for (int i = 0; i < 3; i++)
                JinaUI.Fill(new Rect(left + width - 34 + i * 12, y + 12, 8, 8), i <= combat.ComboStep ? JinaUI.Gold : new Color(.3f, .26f, .22f));
        // Health is drawn over his head now (PrototypeCombat.DrawPlayerHealthBar).
        JinaUI.Icon("icon_arrow", left - 2, y + 47, 1.25f);
        JinaUI.Text(new Rect(left + 20, y + 48, 70, 18), combat.Arrows + " / " + ShopRules.MaxArrows, 13, combat.Arrows == 0 ? new Color(1, .5f, .4f) : JinaUI.Bright, TextAnchor.MiddleLeft);
        string coins = (combat.Tavern != null ? combat.Tavern.wallet.Coins : 0) + "문";
        JinaUI.Icon("icon_coin", left + width - JinaUI.Width(coins, 13) - 24, y + 47, 1.25f);
        JinaUI.Text(new Rect(left, y + 48, width, 18), coins, 13, JinaUI.Gold, TextAnchor.MiddleRight);
        y += height + 6;
        // What he carries that matters right now: small tags under the panel.
        float tagX = x;
        if (combat.Quest.progress.HasCharm) tagX = Tag(tagX, y, "icon_charm", "정화 부적");
        if (combat.Warded) tagX = Tag(tagX, y, "icon_ward", "액막이");
        if (combat.player.Hasted) tagX = Tag(tagX, y, "icon_sandal", "짚신 " + Mathf.CeilToInt(combat.player.HasteUntil - Time.time) + "초");
        if (tagX > x) y += 30;
        if (!full) return;
        // What to do next.
        string goal = Goal();
        float goalHeight = JinaUI.Height(goal, 13, inner - 18);
        JinaUI.HudPanel(new Rect(x, y, w, goalHeight + 16));
        JinaUI.Icon("icon_goal", x + 7, y + 7, 1);
        JinaUI.Text(new Rect(x + 28, y + 8, inner - 18, goalHeight), goal, 13, JinaUI.Bright);
        y += goalHeight + 22;
        if (!GuideOpen)
        {
            var tag = new Rect(x, y, 104, 22);
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
    static float Tag(float x, float y, string icon, string label)
    {
        float w = 30 + JinaUI.Width(label, 12) + 8;
        var r = new Rect(x, y, w, 24);
        JinaUI.HudPanel(r);
        JinaUI.Icon(icon, x + 5, y + 4, 1);
        JinaUI.Text(new Rect(x + 24, y, w - 26, 24), label, 12, new Color(.8f, .95f, .82f), TextAnchor.MiddleLeft);
        return x + w + 4;
    }

    void DrawMorals()
    {
        var q = combat.Quest.progress;
        float w = 236, x = JinaUI.W - w - Margin, inner = w - 24;
        string[] rows =
        {
            q.ElderDead ? "노인 희생" : null,
            combat.Camp != null && combat.Camp.Remaining > 0 && combat.player.transform.position.x < PrototypeWorld.ForestStart ? "들판 유령 " + combat.Camp.Remaining + "마리 남음" : null,
            combat.River != null && combat.RiverFelled < PrototypeCombat.KeepsakeAt && (combat.RiverFelled > 0 || combat.River.Inside(combat.player.transform.position))
                ? "물귀신 " + combat.RiverFelled + " / " + PrototypeCombat.KeepsakeAt : null,
            combat.Kong != null && combat.Kong.InVillage && !combat.Kong.Revealed ? "콩밭 단서 " + combat.Kong.Clues + " / 3" : null,
        };
        int shown = 0; foreach (var row in rows) if (row != null) shown++;
        JinaUI.HudPanel(new Rect(x, Margin, w, 52 + shown * 18));
        float y = Margin + 8;
        string state = q.Honor > 0 ? "선행" : q.Honor < 0 ? "악행" : "중립";
        JinaUI.Text(new Rect(x + 12, y, inner, 18), "선악", 13, JinaUI.Gold, TextAnchor.MiddleLeft, true);
        JinaUI.Text(new Rect(x + 12, y, inner, 18), state + " " + (q.Honor > 0 ? "+" : "") + q.Honor, 13, q.Honor > 0 ? new Color(.66f, .94f, .72f) : q.Honor < 0 ? new Color(1, .6f, .52f) : JinaUI.Bright, TextAnchor.MiddleRight);
        y += 21;
        // The gauge: red for evil, green for good, a centre mark and a white marker at the current honour.
        JinaUI.Fill(new Rect(x + 12, y, inner, 10), JinaUI.Ink);
        JinaUI.Fill(new Rect(x + 13, y + 1, inner / 2 - 1, 8), new Color(.62f, .22f, .2f));
        JinaUI.Fill(new Rect(x + 12 + inner / 2, y + 1, inner / 2 - 1, 8), new Color(.26f, .56f, .38f));
        JinaUI.Fill(new Rect(x + 13, y + 1, inner - 2, 2), new Color(1, 1, 1, .18f));
        JinaUI.Fill(new Rect(x + 12 + inner / 2 - 1, y - 1, 2, 12), JinaUI.Ink);
        float at = x + 12 + inner * (Mathf.Clamp(q.Honor, -100, 100) + 100) / 200f;
        JinaUI.Fill(new Rect(at - 3, y - 3, 6, 16), JinaUI.Ink);
        JinaUI.Fill(new Rect(at - 2, y - 2, 4, 14), JinaUI.PaperLight);
        y += 16;
        foreach (var row in rows)
            if (row != null) { JinaUI.Text(new Rect(x + 12, y, inner, 18), row, 12, JinaUI.Bright); y += 18; }
    }

    void DrawBossBar()
    {
        JinaUI.ToastTop = Margin + 2;
        // The bar itself is the teammate's, drawn by PrototypeCombat.DrawBossHealthBar; toasts go below it.
        if (!combat.BossAlerted || combat.EnemyDefeated || combat.Paused) return;
        JinaUI.ToastTop = Margin + 56;
    }

    // Only the neighbours of the current map, so it always fits on one line.
    void DrawRoute()
    {
        if (combat.Paused || combat.Maps == null) return;
        int i = combat.Maps.Current; var all = PrototypeMaps.All;
        string route = (i > 0 ? all[i - 1].name + "  ←  " : "") + "[ " + all[i].name + " ]" + (i + 1 < all.Length ? "  →  " + all[i + 1].name : "");
        float w = Mathf.Min(JinaUI.Width(route, 12) + 24, JinaUI.W - 20);
        var r = new Rect((JinaUI.W - w) / 2, JinaUI.H - 30, w, 22);
        JinaUI.HudPanel(r);
        JinaUI.Text(r, route, 12, JinaUI.Bright, TextAnchor.MiddleCenter);
    }
}
