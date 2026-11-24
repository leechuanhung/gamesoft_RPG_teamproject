using System.Collections.Generic;
using UnityEngine;

// ===== 최종 보스: 좌의정 엄탁 (FinalBoss) =====
// 기획서.md 1-3. 어린 임금을 등에 업고 권세를 쥔 좌의정은 몇 해 전 무당의 공수("업 많은 선비가 네 목을 치리라")를
// 들은 뒤로 한양에 드는 선비를 경계해 왔다. 선악이 0 이상이면 선비의 선한 기운과 선업을, 음수면 살의와 탐욕을 보고
// 자기 밥그릇을 빼앗길까 두려워한다. 과거 전날 밤 자기 집 누각(WorldBuilder.ManorStart~ManorEnd)에서 술로 달래려
// 하지만, 선이든 악이든 길에서 요괴와 숱하게 부딪혀 온 선비는 술잔에 비친 눈코입 없는 얼굴을 본능으로 알아챈다.
// 좌의정은 망한 나라의 망령들에게 얼굴을 팔았다. 그 대가로 망령들은 한양 가는 길마다 저주를 걸어(미혹된 도깨비, 물귀신)
// 지방 선비들이 과거장에 닿지 못하게 했고, 벼슬은 그에게 값을 치른 자들의 몫이 되었다. 술자리 끝에 그가 털어놓는다.
// 대화가 끝나면 어둑시니 좌의정으로 변한다(띠, 카메라, 얼굴이 지워지고 검은 형체가 부풀고 번쩍, 포효, 이름 카드).
// 선악이 음수면 관군이 "내 그럴 줄 알았다"며 몰려와 선비를 치고(RoadMobs.CallSquad), 0 이상이면 들어왔다가
// "저런 선비가 역모일 리 없다"며 물러난다. 쓰러뜨리면 +10. 싸움(이동, 공격, 던지는 것)은 FinalBoss.Fight.cs.
public partial class FinalBoss : MonoBehaviour
{
    public Combat combat;
    public enum Phase { Waiting, Banquet, Changing, Fighting, Slain }
    public Phase Now { get; private set; }
    public const int MaxHealth = 420;
    public int Health { get; private set; } = MaxHealth;
    public bool Fighting => Now == Phase.Fighting;
    public bool Resolved => Now == Phase.Slain;
    public bool Cutscene => Now == Phase.Banquet || Now == Phase.Changing;
    public const string Name = "어둑시니 좌의정", Epithet = "얼굴을 팔아 권세를 산 자", Official = "좌의정 엄탁", Seonbi = "선비";
    public const string LastLine = "그렇다면 그 목, 내가 먼저 치마!", Roar = "흐흐흐… 이 나라는 내 것이다!";
    public const float MeetRange = 3.4f, ChangeLength = 4.2f, BlankAt = .6f, SwellAt = 1.3f, FlashAt = 2.2f, CardAt = 2.4f, RoarUntil = 3.4f;
    public const float DeathLength = 2.4f;
    public static readonly float Home = WorldBuilder.OfficialX;
    public Vector3 Feet => transform.position;
    public int Facing { get; private set; } = -1;
    public Bounds HurtBox => new Bounds(Feet + new Vector3(0, 1.9f * Size, 0), new Vector3(2.3f, 3.6f, 1) * Size);
    public float MetAt { get; private set; } = -1;
    public float ChangedAt { get; private set; } = -1;
    public float DownAt { get; private set; } = -1;
    public float HitAt { get; private set; } = -1;
    public struct Line
    {
        public string who, said; public bool thought;
        public Line(string who, string said, bool thought = false) { this.who = who; this.said = said; this.thought = thought; }
    }
    private Line[] lines;
    private int page;
    private SpriteRenderer picture, glow, human, table;
    public int Page => page;
    public Line? Current => Now == Phase.Banquet && lines != null ? lines[Mathf.Min(page, lines.Length - 1)] : (Line?)null;

    // 누각 술자리의 대사. `wicked`: 선악이 음수, `invited`: 청지기의 초대를 받고 왔다, `warned`: 무당의 공수를 들었다.
    public static Line[] BanquetLines(bool wicked, bool invited, bool warned)
    {
        var said = new List<Line>();
        said.Add(new Line(Official, invited ? "왔는가. 먼 길 오느라 고생이 많았네. 앉게, 한잔 받게. 내일이면 급제할 몸이 아닌가."
            : "불청객이로군. 그래도 과거 보러 온 선비를 박대할 수야 있나. 앉게, 한잔 받게."));
        said.Add(new Line(Official, "전하께서는 아직 열두 살이시네. 누군가는 이 나라를 대신 짊어져야 하지. 그게 나일세."));
        if (wicked)
        {
            said.Add(new Line(Official, "(…살기. 그리고 탐욕. 저 눈빛은 내 밥그릇을 노리는 눈이다. 공수가 말한 선비가 바로 이놈이로구나.)", true));
            said.Add(new Line(Official, "자네 같은 사람은 내가 잘 알지. 원하는 걸 말하게. 금이든 벼슬이든 주지. 대신 내일 시험만 보고 조용히 떠나게."));
        }
        else
        {
            said.Add(new Line(Official, "(…맑구나. 너무 맑아. 저 선한 기운, 길에 쌓고 온 선업… 공수가 말한 선비가 틀림없다.)", true));
            said.Add(new Line(Official, "이 나라엔 자네 같은 인재가 필요하네. 내 사람이 되게. 벼슬도 재물도 다 내 손에 있으니."));
        }
        said.Add(new Line(Seonbi, "(술잔을 드는 순간 등줄기가 서늘해졌다. 도깨비 숲에서, 강물 위에서, 뒷산에서 맡았던 바로 그 냄새.)", true));
        said.Add(new Line(Seonbi, warned ? "…무당의 말이 맞았군요. 대감, 잔에 비친 얼굴에 눈코입이 없습니다." : "…대감, 잔에 비친 얼굴에 눈코입이 없습니다."));
        said.Add(new Line(Official, "눈치가 빠르군. 그 무당 년의 공수가 틀리지 않았어. '업 많은 선비가 네 목을 치리라'…"));
        said.Add(new Line(Official, "얼굴 따위는 진작에 팔았네. 망한 나라의 망령들에게 말일세. 이 나라의 권세와 바꾸기엔 싼값이었지."));
        said.Add(new Line(Official, "그들은 한양 가는 길마다 저주를 걸어 주었네. 산에는 미혹된 도깨비를, 강에는 물귀신을."));
        said.Add(new Line(Official, "길이 막히니 시골 선비들은 과거장에 닿지 못하고, 벼슬은 내게 값을 치른 자들의 몫이 되었지. …자네만 빼고."));
        said.Add(new Line(Official, LastLine));
        return said.ToArray();
    }
    // 관군이 들이닥치며 하는 말.
    public const string SoldiersCome = "저 선비 놈, 내 그럴 줄 알았다! 대감을 해치려는 역적이다, 쳐라!";
    public const string SoldiersStay = "…저런 선비가 역모일 리가 없다. 우린 아무것도 못 본 걸로 하세.";

    void Start()
    {
        picture = Child("그림", 6); glow = Child("섬광", 7); human = Child("사람 모습", 5); table = Child("술상", 4);
        BuildFight();
        ResetBoss();
    }
    SpriteRenderer Child(string label, int order)
    {
        var art = new GameObject(label).AddComponent<SpriteRenderer>();
        art.transform.SetParent(transform, false); art.sortingOrder = order; art.enabled = false;
        return art;
    }

    // 재시작: 좌의정이 누각에서 기다리는 상태로.
    public void ResetBoss()
    {
        if (picture == null) return;
        Now = Phase.Waiting; Health = MaxHealth; Facing = -1;
        MetAt = ChangedAt = DownAt = HitAt = -1;
        lines = null; page = 0;
        transform.position = new Vector3(Home, WorldBuilder.Top(Home), 0);
        ResetFight();
        View.Focus(0, 0);
        // 기본 카메라 배율 복구
        View.Zoom(1f, 0f);
    }

    float PlayerX => combat.player.transform.position.x;
    bool Wicked => combat.Quest != null && combat.Quest.progress.Honor < 0;

    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        switch (Now)
        {
            case Phase.Waiting: WaitForHim(); break;
            case Phase.Banquet: Talk(); break;
            case Phase.Changing: Change(); break;
            case Phase.Fighting: if (!combat.Paused) Fight(Time.deltaTime); break;
            case Phase.Slain: StepAfter(); break;
        }
    }

    void WaitForHim()
    {
        if (combat.Paused || combat.Maps == null || !combat.Maps.Shows(Home)) return;
        if (Mathf.Abs(PlayerX - Feet.x) < MeetRange) Meet();
    }
    // 술자리 대화를 연다.
    public void Meet()
    {
        var town = combat.Town;
        lines = BanquetLines(Wicked, town != null && town.Invited, town != null && town.HeardProphecy);
        page = 0; Now = Phase.Banquet; MetAt = Time.time;
        combat.player.CanMove = false;
        combat.SaveOnRoad(new Vector2(WorldBuilder.ManorStart + 1.5f, .3f));
    }
    void Talk()
    {
        combat.player.CanMove = false;
        Facing = PlayerX >= Feet.x ? 1 : -1;
        Look(Mathf.Clamp01((Time.time - MetAt) / .5f));
        if (Input.GetKeyDown(KeyCode.Escape)) page = lines.Length - 1;
        else if (JinaUI.Interact && !JinaUI.FinishTyping(lines[page].said)) NextLine();
    }
    public void NextLine()
    {
        if (Now != Phase.Banquet) return;
        if (++page < lines.Length) return;
        Now = Phase.Changing; ChangedAt = Time.time;
        // 최종 보스 변신 시 카메라 줌인
        View.Zoom(.80f, ChangeLength - .5f);
    }
    void Look(float weight) => View.Focus((PlayerX + Feet.x) / 2, Mathf.SmoothStep(0, 1, weight));

    // 변신: 얼굴이 달걀처럼 지워지고, 검은 형체가 부풀고, 번쩍, 포효.
    void Change()
    {
        float t = Time.time - ChangedAt, was = t - Time.deltaTime;
        combat.player.CanMove = false;
        Look(Mathf.Clamp01((ChangeLength - t) / .5f));
        var chest = Feet + Vector3.up * 1.2f;
        if (t > BlankAt && t < FlashAt && Time.frameCount % 2 == 0) JinaFx.Rise(chest, 3, new Color(.1f, .06f, .14f, .9f), 1.2f, 2.4f, .8f);
        if (was < BlankAt && t >= BlankAt) View.Shake(.04f, .5f);
        if (was < SwellAt && t >= SwellAt) { JinaFx.Dust(Feet, -1.8f, 1.6f); JinaFx.Dust(Feet, 1.8f, 1.6f); View.Shake(.09f, .9f); }
        if (was < FlashAt && t >= FlashAt) { JinaFx.Shock(Feet, 2.2f); View.Shake(.22f, .8f); JinaFx.Dots(chest + Vector3.up * 2, 26, new Color(.5f, .2f, .3f), 7, .8f); }
        if (t < ChangeLength) return;
        Now = Phase.Fighting;
        View.Focus(0, 0);
        // 기본 카메라 배율 복구
        View.Zoom(1f, 0f);
        combat.player.CanMove = true;
        StartFight();
        // 관군: 선악이 음수면 선비를 치러, 아니면 들어왔다가 물러난다.
        var mobs = combat.Mobs;
        if (mobs == null) return;
        int rank = RoadMobs.Rank(combat.Quest != null ? combat.Quest.progress.Honor : 0);
        if (rank > 0) mobs.CallSquad(Maps.Index(Home), rank, WorldBuilder.ManorStart + .8f, WorldBuilder.ManorEnd - 1.5f, SoldiersCome);
        else mobs.CallWitnesses(Maps.Index(Home), WorldBuilder.ManorStart + .8f, SoldiersStay);
    }

    // 선비의 타격.
    public bool Damage(int amount)
    {
        if (!Fighting) return false;
        if (combat.Cheats != null) amount = combat.Cheats.Power(amount);   // 개발자 치트: 한 방
        int before = Health;
        Health = Mathf.Max(0, Health - Combat.CalcDamage(amount + JinaItems.AttackBonus(combat), 0));
        HitAt = Time.time;
        var at = Feet + new Vector3(0, 2.2f * Size, 0);
        int away = PlayerX < Feet.x ? 1 : -1;
        JinaFx.Hit(at, away, new Color(1, .9f, .9f), 1.4f);
        JinaFx.Number(at + Vector3.up * .9f + Vector3.right * Random.Range(-.3f, .3f), before - Health, Color.white, 22);
        if (before > MaxHealth / 2 && Health <= MaxHealth / 2) Enrage();
        if (Health > 0) return true;
        Now = Phase.Slain; DownAt = Time.time;
        JinaFx.Stop(.2f); View.Shake(.26f, .7f);
        EndFight();
        if (combat.Quest != null) combat.Quest.progress.ResolveFinal();
        if (combat.Mobs != null) combat.Mobs.Disband(Maps.Index(Home));
        return true;
    }

    // ---- 모습 ----
    static string Pose(string pose) => "25_Final/final_" + pose;
    void LateUpdate()
    {
        if (picture == null) return;
        picture.enabled = glow.enabled = human.enabled = false;
        table.enabled = Now <= Phase.Changing; table.sprite = JinaArt.Prop("drink_table");   // 싸움이 나면 술상은 엎어진다
        table.transform.position = new Vector3(Home - 1.6f, WorldBuilder.Top(Home), 0); table.transform.localScale = Vector3.one;
        DrawFight();
        bool flip = Facing > 0;
        switch (Now)
        {
            case Phase.Waiting: Show(human, JinaArt.Load("25_Final/official_idle"), Feet, flip); break;
            case Phase.Banquet: Show(human, JinaArt.Load(page == 0 ? "25_Final/official_cup" : "25_Final/official_idle"), Feet, flip); break;
            case Phase.Changing: DrawChange(Time.time - ChangedAt, flip); break;
            case Phase.Fighting: DrawFighter(flip); break;
            case Phase.Slain: DrawDeath(Time.time - DownAt, flip); break;
        }
    }
    static void Show(SpriteRenderer art, Sprite sprite, Vector3 feet, bool flip, float alpha = 1)
    {
        art.enabled = true; art.sprite = sprite; art.color = new Color(1, 1, 1, alpha);
        JinaArt.Place(art, Snap(feet), flip);
        art.transform.localScale = Vector3.one; art.transform.rotation = Quaternion.identity;
    }
    const float ArtPixel = 12f / (546f / 1.9f);
    static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x / ArtPixel) * ArtPixel, Mathf.Round(p.y / ArtPixel) * ArtPixel, p.z);
    void Flash(SpriteRenderer over, float amount, Color colour)
    {
        if (amount <= 0) return;
        glow.enabled = true; glow.sprite = JinaArt.Silhouette(over.sprite); glow.flipX = over.flipX;
        glow.transform.position = over.transform.position; glow.transform.localScale = over.transform.localScale;
        colour.a = amount; glow.color = colour;
    }
    float Shudder(float t, float every = .05f) => ((int)(t / every) % 2 == 0 ? 1 : -1) * ArtPixel;

    void DrawChange(float t, bool flip)
    {
        if (t < SwellAt)
        {
            var face = JinaArt.Load(t < BlankAt ? "25_Final/official_idle" : "25_Final/official_blank");
            Show(human, face, Feet + Vector3.right * Shudder(t) * (t > BlankAt ? 2 : 1), flip);
            human.color = Color.Lerp(Color.white, new Color(.5f, .42f, .55f), t / SwellAt);
            return;
        }
        var sprite = JinaArt.Load(Pose(t < RoarUntil && t >= FlashAt ? "open" : "idle_00"));
        Show(picture, sprite, Feet, flip);
        if (t < FlashAt)
        {
            float k = Mathf.SmoothStep(0, 1, (t - SwellAt) / (FlashAt - SwellAt));
            float wobble = 1 + .07f * Mathf.Sin(t * 38) * (1 - k);
            picture.transform.localScale = new Vector3(Mathf.Lerp(.3f, 1, k) * wobble, Mathf.Lerp(.42f, 1, k) / wobble, 1);
            picture.sprite = JinaArt.Silhouette(sprite); picture.color = new Color(.06f, .04f, .08f, 1);
            return;
        }
        Flash(picture, Mathf.Clamp01(1 - (t - FlashAt) / .45f), new Color(1, .92f, .9f));
        if (t < RoarUntil) picture.transform.position += Vector3.right * Shudder(t, .04f);
    }
    // 어둠이 흩어진 자리에는 얼굴 없는 좌의정이 쓰러져 남는다.
    public const float CorpseAt = 1.5f;
    void DrawDeath(float t, bool flip)
    {
        if (t >= CorpseAt) Show(human, JinaArt.Load("25_Final/official_down"), Feet, flip, Mathf.Clamp01((t - CorpseAt) / .8f));
        if (t >= DeathLength) return;
        Show(picture, JinaArt.Load(Pose(t < 1.1f ? "death_00" : t < 1.7f ? "death_01" : "death_02")), Feet, flip, t > 1.7f ? 1 - (t - 1.7f) / (DeathLength - 1.7f) : 1);
        picture.transform.localScale = Vector3.one * Size;
        if (t < .6f) picture.transform.position += Vector3.right * Shudder(t);
        Flash(picture, t < .25f ? .9f * (1 - t / .25f) : 0, Color.white);
        if (t > .9f && Time.frameCount % 3 == 0) JinaFx.Rise(Feet + Vector3.up * 1.6f, 2, new Color(.12f, .08f, .16f, .9f), 1.4f, 2.6f, 1f);
    }

    // 쓰러진 뒤 선비의 한마디.
    public const string AfterWord = "…얼굴 없는 자가 쥐고 있던 나라. 내일 아침, 어린 임금은 누구의 얼굴을 보게 될까.";
    void StepAfter() { }

    // 대화창, 띠, 이름 카드, 체력 막대.
    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        KoreanUI.Apply();
        if (Now == Phase.Banquet && lines != null)
        {
            DrawBars(Mathf.Clamp01((Time.time - MetAt) / .4f));
            var line = lines[page];
            JinaUI.TalkWindow(line.who, null, line.said, "Enter 다음 (" + (page + 1) + "/" + lines.Length + ")   ·   Esc 넘기기", line.thought && line.who == Official);
            return;
        }
        if (Now == Phase.Changing) { DrawChangeCard(Time.time - ChangedAt); return; }
        if (combat.Paused) return;
        if (Now == Phase.Slain)
        {
            float t = Time.time - DownAt;
            if (t > DeathLength && t < DeathLength + 5f)
            {
                var head = combat.player.transform.position + Vector3.up * 2.1f;
                SpeechBubble.Draw(head, Seonbi, AfterWord, null, t - DeathLength);
            }
            if (t > DeathLength + 1f && t < DeathLength + 6f) JinaUI.Toast("좌의정의 그림자가 흩어졌습니다. 오른쪽 끝 금빛 표식을 지나면 과거장입니다.");
            return;
        }
        DrawFightGUI();
    }
    void DrawBars(float amount)
    {
        float bar = JinaUI.H * .11f * amount;
        JinaUI.Fill(new Rect(0, 0, JinaUI.W, bar), Color.black);
        JinaUI.Fill(new Rect(0, JinaUI.H - bar, JinaUI.W, bar), Color.black);
    }
    void DrawChangeCard(float t)
    {
        GUI.depth = -50;
        DrawBars(Mathf.Clamp01((ChangeLength - t) / .4f));
        float bar = JinaUI.H * .11f * Mathf.Clamp01((ChangeLength - t) / .4f);
        if (t >= FlashAt && t < RoarUntil + .4f) SpeechBubble.Draw(Feet + Vector3.up * 4.4f, Official, Roar);
        float card = Mathf.Clamp01((t - CardAt) / .5f) * Mathf.Clamp01((ChangeLength - .3f - t) / .4f);
        if (card <= 0) return;
        const float size = 33;
        float x = 60 - (1 - card) * 80, y = JinaUI.H - bar - size * 3.4f;
        JinaUI.Text(new Rect(x, y, JinaUI.W, size), Epithet, 18, new Color(.92f, .9f, .85f, card), TextAnchor.MiddleLeft);
        JinaUI.Fill(new Rect(x, y + size * 1.05f, size * 7 * card, 3), new Color(.8f, .2f, .24f, card));
        JinaUI.Text(new Rect(x, y + size * 1.15f, JinaUI.W, size * 1.8f), Name, 43, new Color(.92f, .36f, .38f, card), TextAnchor.MiddleLeft, true);
    }

    void OnDestroy() { ClearShots(); if (sealMark != null) Destroy(sealMark.gameObject); }
}
