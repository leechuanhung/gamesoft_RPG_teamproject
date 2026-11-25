using System.Collections.Generic;
using UnityEngine;

// 어둑시니 좌의정의 싸움(FinalBoss.cs의 나머지). 그림자 웅덩이를 타고 미끄러지며 선비와 거리를 둔다.
//   휩쓸기(Sweep): 마디 많은 팔을 땅을 따라 길게 뻗는다. 뛰어서 넘는다.
//   인장 찍기(Slam): 국새를 머리 위로 들고 선비 위로 미끄러져 와서 내려찍는다. 붉은 인장 자국과 충격파.
//   먹물 비(Ink): 붓을 휘둘러 하늘에서 먹물 덩이를 떨군다. 떨어질 자리에 그림자가 먼저 진다.
//   그림자 손(Hands): 웅덩이에서 뻗은 손이 선비 발밑에서 솟는다. 솟을 자리가 먼저 검게 번진다.
//   엽전 토하기(Coins): 갈라진 얼굴에서 엽전을 부채꼴로 토해 낸다.
// 체력이 절반 아래면 어둠이 짙어지고(Dark) 몸이 커지며 더 빨라진다. 선악이 음수면 관군이 한 무리 더 온다.
public partial class FinalBoss
{
    public enum Move { None, Sweep, Slam, Ink, Hands, Coins }
    public Move Doing { get; private set; }
    public float MoveAt { get; private set; } = -1;
    public bool Dark { get; private set; }
    public float DarkAt { get; private set; } = -1;
    public float Size => Dark ? 1.12f : 1f;
    public const int SweepAttack = 22, SlamAttack = 28, InkAttack = 16, HandAttack = 18, CoinAttack = 10;
    public const float MinX = WorldBuilder.ManorStart + 2.5f, MaxX = WorldBuilder.ManorEnd - 2.5f, Glide = 1.2f;
    // 동작마다 준비(앞)와 전체 길이.
    public static float Windup(Move m) => m == Move.Sweep ? .75f : m == Move.Slam ? .95f : m == Move.Ink ? .5f : m == Move.Hands ? .4f : .45f;
    public static float Length(Move m) => m == Move.Sweep ? 1.35f : m == Move.Slam ? 1.6f : m == Move.Ink ? 1.2f : m == Move.Hands ? 1.1f : 1f;
    float Cooldown => Dark ? .9f : 1.5f;
    float nextMoveAt;
    int moveCount;
    bool struck;

    class Shot { public SpriteRenderer art, mark; public Move kind; public Vector2 pos, vel; public float at, until; public bool hit; }
    readonly List<Shot> shots = new List<Shot>();
    SpriteRenderer sealMark;
    float sealMarkUntil;

    void BuildFight()
    {
        sealMark = Child("인장 자국", 3);
        sealMark.transform.SetParent(null);   // 땅에 남는다: 좌의정을 따라가지 않는다
        sealMark.sprite = JinaArt.Prop("final_mark");
    }
    void ResetFight()
    {
        Doing = Move.None; MoveAt = -1; Dark = false; DarkAt = -1; nextMoveAt = 0; moveCount = 0; struck = false;
        sealMarkUntil = 0;
        ClearShots();
    }
    void StartFight() { nextMoveAt = Time.time + 1.2f; Doing = Move.None; }
    void EndFight() { Doing = Move.None; ClearShots(); }
    void ClearShots()
    {
        foreach (var s in shots) { if (s.art != null) Destroy(s.art.gameObject); if (s.mark != null) Destroy(s.mark.gameObject); }
        shots.Clear();
    }
    // 체력이 절반 아래로: 어둠이 짙어지고 관군이 더 온다.
    void Enrage()
    {
        if (Dark) return;
        Dark = true; DarkAt = Time.time;
        View.Shake(.16f, .8f);
        JinaFx.Shock(Feet, 2.6f);
        var mobs = combat.Mobs;
        int rank = RoadMobs.Rank(combat.Quest != null ? combat.Quest.progress.Honor : 0);
        if (mobs != null && rank > 0) mobs.CallSquad(Maps.Index(Home), rank, WorldBuilder.ManorStart + .8f, WorldBuilder.ManorEnd - 1.5f, "대감마님을 지켜라! 역적의 목을 베라!", 1);
    }

    Vector3 Player => combat.player.transform.position;
    Bounds PlayerBox => combat.player.GetComponent<SpriteRenderer>().bounds;

    void Fight(float dt)
    {
        StepShots(dt);
        if (Doing == Move.None)
        {
            Facing = PlayerX >= Feet.x ? 1 : -1;
            // 선비와 4칸쯤 떨어져 미끄러진다.
            float want = PlayerX - Facing * 4.2f;
            float x = Mathf.Clamp(Mathf.MoveTowards(Feet.x, want, Glide * (Dark ? 1.4f : 1) * dt), MinX, MaxX);
            transform.position = new Vector3(x, WorldBuilder.Top(x), 0);
            if (Time.time >= nextMoveAt) Begin(Pick());
            return;
        }
        float t = Time.time - MoveAt, was = t - dt;
        float w = Windup(Doing);
        switch (Doing)
        {
            case Move.Sweep:
                if (t >= w && t < w + .45f && !struck)
                {
                    var reach = new Bounds(Feet + new Vector3(Facing * 3.4f * Size, .25f, 0), new Vector3(5.6f * Size, .9f, 1));
                    if (reach.Intersects(PlayerBox)) { combat.HurtPlayer(SweepAttack); struck = true; }
                }
                if (was < w && t >= w) { JinaFx.Dust(Feet + Vector3.right * Facing * 2, Facing * 2, 1.4f); JinaFx.Dust(Feet + Vector3.right * Facing * 4.5f, Facing * 2, 1.2f); View.Shake(.06f, .3f); }
                break;
            case Move.Slam:
                if (t < w)
                {
                    // 국새를 든 채 선비 위로 미끄러져 온다.
                    float x = Mathf.Clamp(Mathf.MoveTowards(Feet.x, PlayerX - Facing * 1.4f, 7f * dt), MinX, MaxX);
                    transform.position = new Vector3(x, WorldBuilder.Top(x), 0);
                }
                if (was < w && t >= w)
                {
                    var at = Feet + Vector3.right * Facing * 1.6f;
                    JinaFx.Shock(at, 2.4f); JinaFx.Dust(at, -1.6f, 1.6f); JinaFx.Dust(at, 1.6f, 1.6f);
                    View.Shake(.2f, .5f); JinaFx.Stop(.05f);
                    sealMark.transform.position = new Vector3(at.x, WorldBuilder.Top(at.x) + .02f, 0); sealMarkUntil = Time.time + 2f;
                    var p = Player;
                    if (Mathf.Abs(p.x - at.x) < 2.4f * Size && p.y < WorldBuilder.Top(at.x) + 1.3f) combat.HurtPlayer(SlamAttack);
                }
                break;
            case Move.Ink:
                if (was < w && t >= w)
                {
                    int n = Dark ? 8 : 5;
                    for (int i = 0; i < n; i++)
                    {
                        float x = i == 0 ? PlayerX : PlayerX + Random.Range(-4.5f, 4.5f);
                        Drop(Mathf.Clamp(x, WorldBuilder.ManorStart + .5f, WorldBuilder.ManorEnd - .5f), .7f + i * .12f);
                    }
                }
                break;
            case Move.Hands:
                if (was < w && t >= w)
                {
                    int n = Dark ? 5 : 3;
                    for (int i = 0; i < n; i++) Hand(Mathf.Clamp(PlayerX + (i - n / 2) * 1.8f, WorldBuilder.ManorStart + .5f, WorldBuilder.ManorEnd - .5f), .75f + Mathf.Abs(i - n / 2) * .1f);
                }
                break;
            case Move.Coins:
                if (was < w && t >= w)
                {
                    var mouth = Feet + new Vector3(Facing * 1.3f, 3.2f * Size, 0);
                    var aim = ((Vector2)(Player + Vector3.up * .4f - mouth)).normalized;
                    int n = Dark ? 11 : 7;
                    for (int i = 0; i < n; i++)
                    {
                        float spread = Mathf.Lerp(-32, 32, n == 1 ? .5f : i / (float)(n - 1)) * Mathf.Deg2Rad;
                        var d = new Vector2(aim.x * Mathf.Cos(spread) - aim.y * Mathf.Sin(spread), aim.x * Mathf.Sin(spread) + aim.y * Mathf.Cos(spread));
                        Coin(mouth, d * 7.5f);
                    }
                    JinaFx.Dots(mouth, 10, new Color(1, .85f, .4f), 3, .4f);
                }
                break;
        }
        if (t >= Length(Doing)) { Doing = Move.None; nextMoveAt = Time.time + Cooldown; }
    }
    // 다음 동작: 가까우면 휩쓸기와 인장, 멀면 먹물·손·엽전. 같은 것을 두 번 잇지 않는다.
    Move Pick()
    {
        float d = Mathf.Abs(PlayerX - Feet.x);
        Move[] near = { Move.Sweep, Move.Slam, Move.Hands, Move.Coins }, far = { Move.Ink, Move.Hands, Move.Coins, Move.Slam };
        var from = d < 6f ? near : far;
        var pick = from[(moveCount + Random.Range(0, from.Length)) % from.Length];
        if (pick == lastMove) pick = from[(System.Array.IndexOf(from, pick) + 1) % from.Length];
        return pick;
    }
    Move lastMove = Move.None;
    void Begin(Move m)
    {
        Doing = m; lastMove = m; MoveAt = Time.time; moveCount++; struck = false;
        Facing = PlayerX >= Feet.x ? 1 : -1;
    }

    // ── 던지는 것 ──
    SpriteRenderer Thing(string label, string art, int order)
    {
        var r = new GameObject(label).AddComponent<SpriteRenderer>();
        r.sprite = JinaArt.PropCentred(art); r.sortingOrder = order;
        return r;
    }
    SpriteRenderer Mark(float x)
    {
        var r = new GameObject("그림자 표시").AddComponent<SpriteRenderer>();
        r.sprite = JinaArt.Dot(); r.sortingOrder = 3;
        r.transform.position = new Vector3(x, WorldBuilder.Top(x) + .04f, 0);
        r.color = new Color(.08f, .04f, .1f, 0);
        return r;
    }
    void Drop(float x, float delay)
    {
        var s = new Shot { kind = Move.Ink, at = Time.time + delay, pos = new Vector2(x, 8f), vel = new Vector2(0, -12f), mark = Mark(x) };
        s.art = Thing("먹물", "final_ink", 6); s.art.enabled = false;
        shots.Add(s);
    }
    void Hand(float x, float delay)
    {
        var s = new Shot { kind = Move.Hands, at = Time.time + delay, until = Time.time + delay + .55f, pos = new Vector2(x, WorldBuilder.Top(x)), mark = Mark(x) };
        s.art = new GameObject("그림자 손").AddComponent<SpriteRenderer>();
        s.art.sprite = JinaArt.Prop("final_hand"); s.art.sortingOrder = 1; s.art.enabled = false;   // 땅 띠(2) 아래: 땅속에서 솟는다
        shots.Add(s);
    }
    void Coin(Vector3 from, Vector2 vel)
    {
        var s = new Shot { kind = Move.Coins, at = Time.time, until = Time.time + 2.2f, pos = from, vel = vel };
        s.art = Thing("엽전", "final_coin", 6);
        shots.Add(s);
    }
    void StepShots(float dt)
    {
        var box = PlayerBox;
        for (int i = shots.Count - 1; i >= 0; i--)
        {
            var s = shots[i];
            bool done = false;
            if (s.kind == Move.Ink)
            {
                float warn = Mathf.Clamp01(1 - (s.at - Time.time) / .7f);
                if (s.mark != null) { s.mark.color = new Color(.08f, .04f, .1f, .25f + .45f * warn); s.mark.transform.localScale = new Vector3(2.2f + warn, .45f, 1); }
                if (Time.time >= s.at)
                {
                    s.art.enabled = true;
                    s.pos += s.vel * dt;
                    if (!s.hit && Mathf.Abs(s.pos.x - box.center.x) < .55f && s.pos.y < box.max.y && s.pos.y > box.min.y) { combat.HurtPlayer(InkAttack); s.hit = true; done = true; }
                    float ground = WorldBuilder.Top(s.pos.x);
                    if (s.pos.y <= ground + .1f) { done = true; JinaFx.Dots(new Vector3(s.pos.x, ground + .1f, 0), 8, new Color(.14f, .1f, .18f), 2.4f, .35f); }
                }
            }
            else if (s.kind == Move.Hands)
            {
                float warn = Mathf.Clamp01(1 - (s.at - Time.time) / .75f);
                if (s.mark != null) { s.mark.color = new Color(.1f, .05f, .14f, .3f + .5f * warn); s.mark.transform.localScale = new Vector3(1.6f + warn * .8f, .5f, 1); }
                if (Time.time >= s.at)
                {
                    float rise = Mathf.Clamp01((Time.time - s.at) / .12f) * Mathf.Clamp01((s.until - Time.time) / .15f);
                    s.art.enabled = true;
                    s.art.transform.position = new Vector3(s.pos.x, s.pos.y - 1.2f * (1 - rise), 0);
                    var hand = new Bounds(new Vector3(s.pos.x, s.pos.y + .65f, 0), new Vector3(.7f, 1.3f, 1));
                    if (!s.hit && rise > .5f && hand.Intersects(box)) { combat.HurtPlayer(HandAttack); s.hit = true; }
                    if (Time.time >= s.until) done = true;
                }
            }
            else
            {
                s.vel.y -= 6f * dt;
                s.pos += s.vel * dt;
                if (box.Contains(new Vector3(s.pos.x, s.pos.y, box.center.z))) { combat.HurtPlayer(CoinAttack); done = true; }
                if (s.pos.y < WorldBuilder.Top(s.pos.x) || Time.time > s.until) done = true;
            }
            if (done)
            {
                if (s.art != null) Destroy(s.art.gameObject);
                if (s.mark != null) Destroy(s.mark.gameObject);
                shots.RemoveAt(i);
                continue;
            }
            if (s.kind != Move.Hands && s.art != null) s.art.transform.position = s.pos;
        }
    }

    // ── 모습 ──
    void DrawFight()
    {
        sealMark.enabled = Time.time < sealMarkUntil;
        if (sealMark.enabled) sealMark.color = new Color(1, 1, 1, Mathf.Clamp01((sealMarkUntil - Time.time) / .6f));
    }
    // 싸우는 동안의 자세: 동작 준비와 실행에 맞춰.
    public string FightPose()
    {
        if (Doing == Move.None) return (int)(Time.time * 2.2f) % 2 == 0 ? "idle_00" : "idle_01";
        float t = Time.time - MoveAt, w = Windup(Doing);
        switch (Doing)
        {
            case Move.Sweep: return t < w ? "idle_01" : t < w + .5f ? "reach" : "idle_00";
            case Move.Slam: return t < w ? "raise" : t < w + .35f ? "reach" : "idle_00";
            case Move.Ink:
            case Move.Hands: return t < Length(Doing) - .2f ? "cast" : "idle_00";
            default: return t < Length(Doing) - .2f ? "open" : "idle_00";
        }
    }
    void DrawFighter(bool flip)
    {
        Show(picture, JinaArt.Load(Pose(FightPose())), Feet, flip);
        picture.transform.localScale = Vector3.one * Size;
        float t = Time.time - MoveAt;
        // 준비 동작은 붉게 깜박인다.
        if (Doing != Move.None && t < Windup(Doing)) Flash(picture, Mathf.FloorToInt(Time.time * 14) % 2 == 0 ? .45f : 0, new Color(1, .3f, .3f));
        if (HitAt >= 0 && Time.time - HitAt < .1f) Flash(picture, .8f, Color.white);
        if (Dark && Time.frameCount % 4 == 0) JinaFx.Rise(Feet + Vector3.up * .3f, 1, new Color(.1f, .05f, .14f, .8f), 2.2f, 1.6f, 1f);
    }
    // 체력 막대와 어둠.
    void DrawFightGUI()
    {
        if (!Fighting) return;
        if (Dark)
        {
            float a = Mathf.Clamp01((Time.time - DarkAt) / 1.2f) * .28f;
            JinaUI.Fill(new Rect(0, 0, JinaUI.W, JinaUI.H), new Color(.05f, 0, .06f, a));
        }
        JinaUI.BossBar(Name, (float)Health / MaxHealth, new Color(.56f, .13f, .32f), Mathf.Max(0, Health) + " / " + MaxHealth);
        if (Dark && Time.time - DarkAt < 3f) JinaUI.Toast("어둠이 짙어집니다. 좌의정이 더 커지고 빨라졌습니다!");
    }
}
