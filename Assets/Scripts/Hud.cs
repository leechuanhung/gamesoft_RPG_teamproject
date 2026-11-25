using UnityEngine;

// 상시 화면: 선비 이름패와 화살·엽전(왼쪽 위), 그 아래 할 일, 조작 안내(H로 접고, 익숙해지면
// 저절로 접힘), 선악 게이지와 수(오른쪽 위), 보스 막대(위 가운데, 두루마리), 팀원 스킬창(Skills.cs) 둘레의
// 체력·기력·무기·레벨(아래 가운데), 맵 사이 길 안내(왼쪽 아래). 판은 글자에 맞춰 크기가 정해진다. 연출 중에는 비켜 주고,
// 창이 열리면 작아진다. 맞으면 화면 가장자리가 붉어지고, 죽을 때가 가까워도 붉어진다.
public class Hud : MonoBehaviour
{
    public Combat combat;
    public const float PanelWidth = 288, Margin = 12, GuideFoldsAfter = 22f;
    public bool GuideOpen { get; private set; } = true;
    private bool guideTouched;

    static readonly string[] Guide =
    {
        "이동 A·D / ←→     달리기 Shift",
        "도약 스페이스     무기 바꾸기 S",
        "물러서기 Q     돌진베기 W",
        "공격 J·클릭(마우스 쪽으로, 3연격)",
        "대화·조사 Enter     정화 F     보따리 I",
        "다시 시작 R",
        "주황빛은 공격 예고입니다",
    };

    // 인트로, 결말, 조우, 게임 오버 화면 위에는 아무것도 없다.
    public bool Hidden => combat == null || combat.Quest == null || combat.PlayerHealth <= 0
        || (combat.Intro != null && combat.Intro.IsPlaying) || (combat.Ending != null && combat.Ending.Active)
        || (combat.Encounter != null && combat.Encounter.Playing) || (combat.Stay != null && combat.Stay.Playing)
        || (combat.KongBoss != null && combat.KongBoss.Cutscene);

    // H로 조작 안내를 접고 펴며, 한동안 플레이하면 저절로 접는다.
    void Update()
    {
        if (combat == null) return;
        if (!combat.Paused && Input.GetKeyDown(KeyCode.H)) { GuideOpen = !GuideOpen; guideTouched = true; }
        // 한동안 플레이하면 안내가 비켜 준다. H로 다시 연다.
        if (!guideTouched && GuideOpen && Time.timeSinceLevelLoad > GuideFoldsAfter + 40 && !combat.Paused) { GuideOpen = false; guideTouched = true; }
    }

    // 지금 어디 있는지와 이야기 진행에 따라 할 일. 늘 화면에 있으니 짧게.
    public string Goal()
    {
        var q = combat.Quest.progress;
        float x = combat.player.transform.position.x;
        // 관군은 어느 맵에서든 가장 급한 일이다.
        if (combat.Mobs != null && combat.Maps != null && combat.Mobs.SoldiersAlive(combat.Maps.Current) > 0)
            return "관군이 선비를 쫓고 있습니다! 물리치거나 맵 밖으로 빠져나가세요.";
        if (!combat.EnemyDefeated)
        {
            if (q.HasCharm) return "도깨비 가까이에서 F로 저주를 풀거나, 칼로 쓰러뜨리세요. 해결하면 숲 끝 가시덤불이 걷힙니다.";
            if (combat.Camp != null && !combat.Camp.Cleared && x < WorldBuilder.ForestStart)
                return "서쪽 유령을 " + Camp.SealKills + "번 물리치면 비석 터에서 정화 부적을 얻습니다. 도깨비를 칼로만 상대해도 됩니다.";
            return "숲의 도깨비를 토벌하거나, 비석 터의 부적으로 정화하세요. 해결하면 숲 끝 가시덤불이 걷힙니다.";
        }
        var boss = combat.KongBoss;
        if (boss != null && boss.Fighting) return KongGoal(x);
        if (x >= WorldBuilder.FortStart) return RoadGoal(x);
        if (x < WorldBuilder.FerryStart) return "숲 동쪽 끝을 지나 나루터로 가세요.";
        if (x < WorldBuilder.RiverRight) return "뱃사공과 이야기하고, 나룻배로 강을 건너세요.";
        if (boss != null && boss.Resolved) return "독기가 걷혔습니다. 뒷산 너머 산성으로 가세요. 산성 너머가 한양입니다.";
        if (x < WorldBuilder.KongEnd && combat.Kong != null && !combat.Kong.Revealed)
        {
            if (!combat.Kong.Stayed) return combat.Kong.ReadyForNight ? "마을 사람들과 모두 이야기했습니다. 콩쥐에게 다시 말을 걸어 보세요."
                : "콩밭 마을 사람들의 이야기를 들어 보세요. 콩쥐, 팥쥐, 아낙, 나무꾼 모두에게 말을 걸어 보세요.";
            if (combat.Kong.Clues < 3) return Clue();
            return "모든 것을 알았습니다. 콩쥐와 마주하세요.";
        }
        if (combat.Kong != null && combat.Kong.Revealed) return x < WorldBuilder.PassStart ? "콩쥐가 뒷산에서 기다립니다. 먼저 팥쥐의 말을 들어 보세요." : "콩쥐가 말한 뒷산은 고개 너머입니다.";
        return "고개를 넘어 뒷산을 지나면 한양으로 가는 산성이 있습니다.";
    }
    // 뒷산 너머 한양 가는 길: 산성 퍼즐, 관군, 한양 사람들, 좌의정.
    string RoadGoal(float x)
    {
        var fort = combat.Fort; var fin = combat.FinalBoss; var town = combat.Town; var mobs = combat.Mobs;
        if (fin != null && fin.Fighting) return fin.Dark ? "어둠이 짙어졌습니다! 그림자가 지는 자리를 피하며 좌의정을 쓰러뜨리세요."
            : "어둑시니 좌의정을 쓰러뜨리세요. 땅에 그림자가 지면 피하고, 팔을 휩쓸면 뛰어넘으세요.";
        if (fin != null && fin.Resolved) return "좌의정 댁 오른쪽 끝 금빛 표식을 지나면 과거장입니다.";
        if (x < WorldBuilder.FortEnd && fort != null && !fort.Open)
            return fort.ReadOrders ? BeaconFort.Route + " (" + fort.Step + "/5)" : "산성 문이 닫혀 있습니다. 산성 입구의 봉수 군령판을 읽어 보세요.";
        if (x < WorldBuilder.GraveEnd) return "역참과 무덤길을 지나 한양으로 가세요. 도깨비불은 빛을 모은 뒤 날아듭니다.";
        if (x < WorldBuilder.CityEnd) return town != null && town.Invited ? "좌의정이 과거장 앞을 지나 자기 집 누각으로 오라 했습니다."
            : "한양 사람들의 이야기를 들어 보세요. 거리 끝에서 누가 선비를 기다립니다.";
        if (x < WorldBuilder.ManorStart) return "과거는 내일 아침입니다. 오늘 밤 좌의정 댁 누각으로 가세요.";
        return "누각의 좌의정에게 다가가세요.";
    }
    // 콩쥐 요괴와 싸우는 동안: 요괴가 어디 있고 무엇을 할 수 있는지.
    string KongGoal(float x)
    {
        var boss = combat.KongBoss; var kong = combat.Kong;
        bool hairpin = JinaItems.Inventory != null && JinaItems.Inventory.GetQuantity(JinaItems.Keepsake) > 0;
        if (x < WorldBuilder.RiverRight)
            return kong != null && kong.AnyoneToTake ? "강 건너 마을에서 콩쥐 요괴가 사람들을 해치고 있습니다. 채비를 마치면 나룻배로 돌아가세요."
                : "콩쥐 요괴는 강을 건너지 못합니다. 채비를 마치면 나룻배로 돌아가세요.";
        if (kong != null && kong.Raided && kong.AnyoneToTake && x < WorldBuilder.KongEnd) return "콩쥐 요괴가 묶인 마을 사람들을 하나씩 해치고 있습니다! 칼로 두 번 베면 멈칫합니다.";
        if (hairpin) return boss.Weakened ? "지금입니다! 콩쥐 요괴 가까이에서 F로 은비녀를 쓰세요." : "콩쥐 요괴의 체력을 절반 아래로 깎은 뒤, 가까이에서 F로 은비녀를 쓰면 콩쥐를 되찾습니다.";
        return "콩쥐 요괴를 쓰러뜨리세요. 은비녀가 있으면 정화할 수 있습니다. 강을 건너면 쫓아오지 못합니다.";
    }
    // 아직 못 찾은 콩밭 마을 단서를 알려 준다.
    string Clue()
    {
        var k = combat.Kong;
        if (!k.HeardPatjwi) return "콩쥐가 뒷산으로 오라 합니다. 가기 전에 팥쥐에게 넌지시 물어보세요.";
        if (!k.SawCrocks) return "팥쥐가 말린 장독대를 직접 살펴보세요.";
        return "장독대 곁에 떨어진 종이를 읽어 보세요.";
    }

    // HUD 전체를 그린다(연출 중에는 숨는다).
    void OnGUI()
    {
        if (combat == null) return;
        KoreanUI.Apply();
        // 화면 가장자리: 맞을 때마다 번쩍, 죽을 때가 가까우면 느리게 맥박. 세상 위, 모든 판 아래.
        if (combat.PlayerHealth > 0 && !Hidden) DrawHurt();
        bool tray = !Hidden && !combat.Paused;
        if (Event.current.type == EventType.Repaint) JinaUI.TrayShown = tray;
        if (Hidden) { JinaUI.ToastTop = 14; return; }
        GUI.depth = 10;   // 모든 창은 HUD 위에 그려진다
        DrawStatus();
        if (!combat.InventoryPauseActive) DrawMorals();
        DrawBossBar();
        if (tray) DrawTray();
        DrawRoute();
    }

    // ===== 아래 가운데 상(床): 체력·기력, 무기 두 칸, 레벨 =====
    // 스킬창(Q·W·E)은 팀원의 Skills.cs가 그 자리에 그대로 그린다. 여기서는 그 둘레만: 게이지 두 줄은 스킬창 위로
    // 같은 폭에, 무기 두 칸과 레벨 칸은 스킬창 오른쪽(원래 배치: 화면 픽셀로 224·296에 64 칸, 368부터 끝까지 레벨).
    void DrawTray()
    {
        var row = JinaUI.SkillRow;
        float px = 1 / JinaUI.Scale;   // 화면 픽셀 하나의 레이아웃 크기(스킬창과 같은 자리)
        var plate = new Rect(row.x, row.y - JinaUI.TrayAbove, row.width, JinaUI.TrayAbove - 6);
        JinaUI.HudPanel(plate);
        float x = plate.x + 10, right = plate.xMax - 10, y = plate.y + 7;
        // 체력: 體 낙관과 주홍 막대(10칸 눈금). 4분의 1 아래로 떨어지면 맥박친다.
        float hp = Mathf.Clamp01((float)combat.PlayerHealth / combat.MaxHealth);
        var hpColour = hp > .25f ? JinaUI.Health : Color.Lerp(JinaUI.Health, new Color(1f, .5f, .36f), .5f + .5f * Mathf.Sin(Time.time * 7));
        JinaUI.Seal(new Rect(x, y, 24, 24), "體", 15);
        JinaUI.Gauge(new Rect(x + 32, y + 1, right - x - 32, 22), hp, hpColour, "체력", combat.PlayerHealth + " / " + combat.MaxHealth, JinaUI.Lag("player hp", hp), 10, 14);
        y += 28;
        // 기력(스테미너): 氣 낙관과 쪽빛 막대.
        float st = combat.player.MaxStamina > 0 ? Mathf.Clamp01((float)combat.player.Stamina / combat.player.MaxStamina) : 0;
        JinaUI.Seal(new Rect(x + 3, y, 18, 18), "氣", 12);
        JinaUI.Gauge(new Rect(x + 32, y + 1, right - x - 32, 16), st, JinaUI.Stamina, "기력", combat.player.Stamina + " / " + combat.player.MaxStamina, JinaUI.Lag("player stamina", st), 0, 12);
        for (int i = 0; i < 2; i++) WeaponSlot(new Rect(row.x + (224 + i * 72) * px, row.y, 64 * px, 64 * px), i);
        DrawLevel(new Rect(row.x + 368 * px, row.y, row.xMax - (row.x + 368 * px), row.height));
    }

    // 무기 칸 하나: 든 무기는 금빛 테두리, 다른 칸에는 바꾸는 키(S) 낙관. 활 칸 아래 구석에 남은 화살.
    void WeaponSlot(Rect r, int i)
    {
        string id = combat.EquippedWeapons[i];
        bool holding = id != null && id == combat.CurrentWeapon;
        JinaUI.Box(r, holding ? new Color(.32f, .23f, .14f) : new Color(.17f, .13f, .11f), holding ? JinaUI.Gold : JinaUI.WoodLight, 2);
        if (id == null) return;
        var weapon = Weapon.Get(id);
        var icon = combat.WeaponIcon(weapon);
        if (icon != null)
        {
            var saved = GUI.color; if (!holding) GUI.color = new Color(1, 1, 1, .55f);
            InventoryUI.DrawIcon(icon, new Rect(r.x + 6, r.y + 6, r.width - 12, r.height - 12));
            GUI.color = saved;
        }
        if (weapon.ranged) JinaUI.Outlined(new Rect(r.x, r.yMax - 19, r.width - 5, 16), combat.Arrows.ToString(), 12, combat.Arrows == 0 ? new Color(1, .5f, .4f) : JinaUI.Bright, TextAnchor.LowerRight);
        if (!holding && combat.EquippedWeapons[1 - i] != null) JinaUI.Seal(new Rect(r.x - 5, r.y - 5, 17, 17), "S", 11);
    }

    // 레벨과 다음 레벨까지의 경험(금빛 막대). 칸 높이는 스킬창을 따른다(화면 크기마다 다름).
    void DrawLevel(Rect r)
    {
        JinaUI.Box(r, new Color(.17f, .13f, .11f), JinaUI.WoodLight, 2);
        float mid = r.y + r.height / 2;
        JinaUI.Text(new Rect(r.x + 12, mid - 15, 30, 30), "Lv", 13, JinaUI.Faint, TextAnchor.MiddleLeft, true);
        JinaUI.Text(new Rect(r.x + 34, mid - 16, 40, 32), combat.Level.ToString(), 24, JinaUI.Gold, TextAnchor.MiddleLeft, true);
        float gx = r.x + 80, gw = r.xMax - 12 - gx;
        int need = Combat.ExpToNext(combat.Level);
        JinaUI.Text(new Rect(gx, mid - 19, gw, 16), combat.MaxedLevel ? "가장 높은 경지" : "경험  " + combat.Exp + " / " + need, 12, JinaUI.Bright, TextAnchor.MiddleLeft);
        JinaUI.Gauge(new Rect(gx + 2, mid + 2, gw - 4, 13), combat.MaxedLevel ? 1 : (float)combat.Exp / need, JinaUI.Exp, null, null, 0, 10);
    }

    // 맞았거나 죽을 때가 가까우면 화면 가장자리를 붉게 그린다.
    void DrawHurt()
    {
        float flash = combat.PlayerHurtAt < 0 ? 0 : Mathf.Clamp01(1 - (Time.time - combat.PlayerHurtAt) / .32f);
        float low = combat.PlayerHealth <= Combat.MaxPlayerHealth / 4 ? .3f + .16f * Mathf.Sin(Time.time * 4.4f) : 0;
        float a = Mathf.Max(flash * .85f, low);
        if (a <= 0) return;
        var v = JinaUI.Art("vignette");
        if (v == null) return;
        // 그림은 검은색이다. 붉은 바탕 위에 알파로 그려서 붉은 사본을 만든다.
        var saved = GUI.color;
        GUI.color = new Color(.78f, .06f, .05f, a);
        var mat = GUI.matrix;
        GUI.DrawTexture(new Rect(0, 0, JinaUI.W, JinaUI.H), Red(v));
        GUI.color = saved;
    }
    static Texture2D redVignette;
    // 검은 그림을 붉은 사본으로 만든다(캐시).
    static Texture2D Red(Texture2D source)
    {
        if (redVignette != null) return redVignette;
        redVignette = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = source.GetPixels32();
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, (byte)Mathf.Min(255, pixels[i].a * 2));
        redVignette.SetPixels32(pixels); redVignette.Apply();
        return redVignette;
    }

    // 왼쪽 위 선비 이름패(화살·엽전)와 할 일, 조작 안내를 그린다. 체력·기력·무기는 아래 상(DrawTray)에 있다.
    void DrawStatus()
    {
        float x = Margin, w = PanelWidth, inner = w - 24, y = Margin;
        bool full = !combat.Paused;
        // 이름패: 선비 사(士) 낙관, 이름과 연격, 화살과 엽전.
        const float height = 56;
        JinaUI.HudPanel(new Rect(x, y, w, height));
        JinaUI.Seal(new Rect(x + 10, y + 11, 34, 34), "士", 21);
        float left = x + 54, width = w - 54 - 12;
        JinaUI.Text(new Rect(left, y + 6, width, 22), "선비", 16, JinaUI.Gold, TextAnchor.MiddleLeft, true);
        // 칼이 다음 타격으로 이어질 수 있는 동안 연격 점 세 개(놋쇠 장식).
        if (!combat.UsingBow && combat.ComboStep >= 0)
            for (int i = 0; i < 3; i++)
            {
                var dot = new Rect(left + width - 38 + i * 13, y + 11, 10, 10);
                JinaUI.Fill(dot, JinaUI.Ink);
                JinaUI.Fill(new Rect(dot.x + 2, dot.y + 2, 6, 6), i <= combat.ComboStep ? JinaUI.BrassLight : new Color(.3f, .26f, .22f));
            }
        JinaUI.Icon("icon_arrow", left - 2, y + 31, 1.25f);
        JinaUI.Text(new Rect(left + 20, y + 31, 90, 20), combat.Arrows + " / " + ShopRules.MaxArrows, 14, combat.Arrows == 0 ? new Color(1, .5f, .4f) : JinaUI.Bright, TextAnchor.MiddleLeft, true);
        string coins = (combat.Tavern != null ? combat.Tavern.wallet.Coins : 0) + "문";
        JinaUI.Icon("icon_coin", left + width - JinaUI.Width(coins, 14, true) - 24, y + 31, 1.25f);
        JinaUI.Text(new Rect(left, y + 31, width, 20), coins, 14, JinaUI.Gold, TextAnchor.MiddleRight, true);
        y += height + 6;
        // 지금 중요한 소지품: 판 아래 작은 꼬리표.
        float tagX = x;
        if (combat.Quest.progress.HasCharm) tagX = Tag(tagX, y, "icon_charm", "정화 부적");
        if (combat.Warded) tagX = Tag(tagX, y, "icon_ward", "액막이");
        if (combat.player.Hasted) tagX = Tag(tagX, y, "icon_sandal", "짚신 " + Mathf.CeilToInt(combat.player.HasteUntil - Time.time) + "초");
        if (tagX > x) y += 30;
        if (!full) return;
        // 할 일.
        string goal = Goal();
        float goalHeight = JinaUI.Height(goal, 14, inner - 18);
        JinaUI.HudPanel(new Rect(x, y, w, goalHeight + 16));
        JinaUI.Icon("icon_goal", x + 7, y + 8, 1);
        JinaUI.Text(new Rect(x + 28, y + 8, inner - 18, goalHeight), goal, 14, JinaUI.Bright);
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
    // 판 아래 소지품 꼬리표 하나를 그리고 다음 x를 돌려준다.
    static float Tag(float x, float y, string icon, string label)
    {
        float w = 30 + JinaUI.Width(label, 12) + 8;
        var r = new Rect(x, y, w, 24);
        JinaUI.HudPanel(r);
        JinaUI.Icon(icon, x + 5, y + 4, 1);
        JinaUI.Text(new Rect(x + 24, y, w - 26, 24), label, 12, new Color(.8f, .95f, .82f), TextAnchor.MiddleLeft);
        return x + w + 4;
    }

    // 오른쪽 위 선악 게이지와 수를 그린다.
    void DrawMorals()
    {
        var q = combat.Quest.progress;
        float w = 236, x = JinaUI.W - w - Margin, inner = w - 24;
        string[] rows =
        {
            q.ElderDead ? "노인 희생" : null,
            combat.Camp != null && !combat.Camp.Cleared && combat.player.transform.position.x < WorldBuilder.ForestStart ? "들판 유령 " + combat.Camp.Kills + " / " + Camp.SealKills : null,
            combat.River != null && combat.RiverFelled < Combat.KeepsakeAt && (combat.RiverFelled > 0 || combat.River.Inside(combat.player.transform.position))
                ? "물귀신 " + combat.RiverFelled + " / " + Combat.KeepsakeAt : null,
            combat.Kong != null && combat.Kong.InVillage && !combat.Kong.Revealed ? "콩밭 단서 " + combat.Kong.Clues + " / 3" : null,
        };
        int shown = 0; foreach (var row in rows) if (row != null) shown++;
        JinaUI.HudPanel(new Rect(x, Margin, w, 52 + shown * 18));
        float y = Margin + 8;
        string state = q.Honor > 0 ? "선행" : q.Honor < 0 ? "악행" : "중립";
        JinaUI.Text(new Rect(x + 12, y, inner, 18), "선악", 13, JinaUI.Gold, TextAnchor.MiddleLeft, true);
        JinaUI.Text(new Rect(x + 12, y, inner, 18), state + " " + (q.Honor > 0 ? "+" : "") + q.Honor, 13, q.Honor > 0 ? new Color(.66f, .94f, .72f) : q.Honor < 0 ? new Color(1, .6f, .52f) : JinaUI.Bright, TextAnchor.MiddleRight);
        y += 21;
        // 게이지: 악은 빨강, 선은 초록, 가운데 표시와 지금 명예 위치의 흰 표시.
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

    // 보스 막대가 있으면 알림을 그 아래로 내린다.
    void DrawBossBar()
    {
        JinaUI.ToastTop = Margin + 2;
        // 막대 자체는 팀원 것(Combat.DrawBossHealthBar). 알림은 그 아래로 간다.
        bool kong = combat.KongBoss != null && combat.KongBoss.Fighting || combat.FinalBoss != null && combat.FinalBoss.Fighting;
        if ((!combat.BossAlerted || combat.EnemyDefeated) && !kong || combat.Paused) return;
        JinaUI.ToastTop = JinaUI.BossBarTop + 54;
    }

    // 지금 맵의 이웃만 보여서 늘 한 줄에 들어간다.
    void DrawRoute()
    {
        if (combat.Paused || combat.Maps == null) return;
        int i = combat.Maps.Current; var all = Maps.All;
        string before = i > 0 ? all[i - 1].name + " ← " : "", here = all[i].name, after = i + 1 < all.Length ? " → " + all[i + 1].name : "";
        float wb = JinaUI.Width(before, 12), wh = JinaUI.Width(here, 13, true), wa = JinaUI.Width(after, 12), w = wb + wh + wa + 26;
        // 왼쪽 아래(상 옆에 들어가지 않으면 상 위).
        var r = new Rect(16, JinaUI.LeftBottom(w) - 24, w, 24);
        JinaUI.HudPanel(r);
        float x = r.x + 13;
        JinaUI.Text(new Rect(x, r.y, wb + 2, r.height), before, 12, JinaUI.Faint, TextAnchor.MiddleLeft);
        JinaUI.Text(new Rect(x + wb, r.y, wh + 2, r.height), here, 13, JinaUI.Gold, TextAnchor.MiddleLeft, true);
        JinaUI.Text(new Rect(x + wb + wh, r.y, wa + 2, r.height), after, 12, JinaUI.Faint, TextAnchor.MiddleLeft);
    }
}
