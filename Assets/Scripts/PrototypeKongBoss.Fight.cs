using System.Collections.Generic;
using UnityEngine;

// 콩쥐 요괴의 싸움. 거리를 두고 던진다: 선비에게 포물선으로 던지는 젓갈 덩어리(떨어진 곳에
// 소금물 웅덩이), 부채꼴 콩알, 체력 절반 아래에서는 머리 위 발판이 막아 주는 소금 비. 선비가
// 가까이 오면 가슴 높이로 혀를 한 번 휘두르고(뛰거나 물러나 피함), 혀를 내민 채 잠시 앉아 있다:
// 그때가 벨 기회다. 모든 공격은 먼저 주황색으로 번쩍인다. 뛰어서 움직이므로 계단과 벽에
// 선비처럼 막히고, 발판 위로 따라 뛰어오른다. 고개의 바위는 해 없이 통과한다.
// 뒷산의 큰 독이 던진 것을 막는다(PrototypeKongBoss.Crocks.cs).
// 앞에 벽이 있거나, 선비가 더 높이 있거나, 뛰어도 제자리였으면 다음은 높이 뛰어서
// 뒷산의 움푹한 곳과 고개의 계단을 올라온다.
// 콩밭 마을에서는 묶인 사람을 오른쪽부터 노린다. 죽이려고 준비하는 동안 칼로 두 번 베면
// 움찔하며 그 사람을 잠시 살려 둔다. 강은 건너지 못한다: 강 너머로 가면 마을로 돌아간다.
public partial class PrototypeKongBoss
{
    public enum Attack { None, Spit, Beans, Tongue, Salt }
    public Attack Doing { get; private set; }
    public const int SpitDamage = 15, BrineDamage = 8, BeanDamage = 8, TongueDamage = 18, SaltDamage = 10;
    public const float MinX = PrototypeWorld.RiverRight + 1.4f, MaxX = PrototypeWorld.HillEnd - 1.5f, VillageCentre = 130f;
    // 뛰기: 보통 뛰기와 높이 뛰기의 높이(약 2.45, 3.6칸), 추격 속도와 걸음 속도.
    public const float Gravity = 9.81f * 3, HopRise = 12f, HighRise = 14.6f, ChaseHop = 6.6f, StepHop = 4.2f, CrouchLength = .2f, HopRest = .28f;
    // 선호하는 거리: Close보다 가까우면 혀를 휘두르고, Far보다 멀면 뛰어서 쫓는다.
    public const float Close = 2.8f, Far = 8.5f, Keep = 6f, AttackRange = 13f;
    public const float TongueReach = 3.1f, TongueOut = .12f, TongueHold = .18f, TongueBack = .4f;
    public const float SpitGravity = 22f, BeanSpeed = 9f, SaltFall = 13f, SaltWarn = .7f;
    // 마을에서 죽이기: 얼마나 가까이 서는지, 혀 전에 얼마나 조이는지, 다음까지의 간격.
    public const float KillReach = 1.7f, KillWindup = 1.8f, KillGap = 1.6f, StaggerAt = 20f;
    public static float Windup(Attack a) => a == Attack.Tongue ? .55f : a == Attack.Salt ? .8f : a == Attack.Beans ? .45f : .5f;
    public static float Recovery(Attack a) => a == Attack.Tongue ? TongueOut + TongueHold + TongueBack + .6f : a == Attack.Salt ? .6f : .4f;
    // 입: 벌린 입(침, 콩)과 혀를 위해 낮춘 입, 왼쪽을 볼 때 발 기준(21_KongBoss 프레임).
    static readonly Vector2 MouthOpen = new Vector2(-.85f, 2.1f), MouthLow = new Vector2(-1.25f, 1.2f);
    Vector3 Mouth(Vector2 m) => Feet + new Vector3(m.x * -Facing, m.y, 0);

    class Shot { public SpriteRenderer art; public Vector2 v; public Attack kind; public float life; public SpriteRenderer mark; }
    class Puddle { public SpriteRenderer art; public float x, y, until; }
    readonly List<Shot> shots = new List<Shot>();
    readonly List<Puddle> puddles = new List<Puddle>();
    public int ShotCount => shots.Count;
    public int PuddleCount => puddles.Count;
    private SpriteRenderer tongue, tip;

    private float attackAt, releaseAt, nextAttackAt, hopReadyAt, crouchUntil, leftGroundAt, landedAt = -1;
    private bool released, tongueLanded, airborne, grounded;
    private int attackCount, hopDir = -1, lastHopDir, stuckHops;
    private float lastHopX = -999;
    private float hopSpeed, hopRise;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    // 습격.
    public PrototypeKongVillage.Who? Victim { get; private set; }
    public float KillAt { get; private set; } = -1;
    public float CastAt { get; private set; } = -1;
    private PrototypeKongVillage.Who killing;
    private bool killed;
    private float stagger, stunnedUntil, nextKillAt, castUntil;

    // 싸움에 쓰는 렌더러(혀, 탄)를 준비한다.
    void BuildFight()
    {
        tongue = Loose("혀", "kong_tongue", 6); tongue.drawMode = SpriteDrawMode.Tiled;
        tip = Loose("혀끝", "kong_tongue_tip", 6);
        BuildCrocks();
    }
    // 부모 없이 따로 움직이는 렌더러를 만든다.
    SpriteRenderer Loose(string label, string art, int order)
    {
        var r = new GameObject(label).AddComponent<SpriteRenderer>();
        r.sprite = JinaArt.PropCentred(art); r.sortingOrder = order; r.enabled = false;
        return r;
    }

    // 재시작: 싸움 상태를 처음으로.
    void ResetFight()
    {
        ClearShots();
        Doing = Attack.None; released = tongueLanded = airborne = false; grounded = true;
        attackAt = releaseAt = nextAttackAt = hopReadyAt = crouchUntil = leftGroundAt = 0; landedAt = -1;
        attackCount = 0; hopDir = -1; lastHopDir = 0; stuckHops = 0; lastHopX = -999;
        Victim = null; KillAt = CastAt = -1; killed = false; stagger = stunnedUntil = nextKillAt = castUntil = 0;
        ResetCrocks();
    }
    void StartFight() { nextAttackAt = Time.time + 1.2f; hopReadyAt = Time.time + .6f; }
    // 토벌이나 정화: 하던 일이 멈추고, 던진 것은 사라진다.
    void EndFight()
    {
        ClearShots();
        Doing = Attack.None; KillAt = -1; castUntil = 0; crouchUntil = 0; airborne = false;
        body.velocity = Vector2.zero;
    }
    // 날고 있는 탄과 소금물을 모두 지운다.
    void ClearShots()
    {
        foreach (var s in shots) { if (s.art != null) Destroy(s.art.gameObject); if (s.mark != null) Destroy(s.mark.gameObject); }
        foreach (var p in puddles) if (p.art != null) Destroy(p.art.gameObject);
        shots.Clear(); puddles.Clear();
    }

    float PlayerY => combat.player.transform.position.y;
    // 선비의 발이 요괴 발보다 얼마나 높은지(계단, 둔덕, 발판).
    float Above => PlayerBody.min.y - Feet.y;
    Bounds PlayerBody => combat.player.GetComponent<SpriteRenderer>().bounds;
    // 요괴가 따라갈 수 있는 곳에 선비가 있다: 강 건너편 둑을 넘지 않음.
    bool Reachable => PlayerX >= MinX - .5f;

    // 싸움 한 프레임: 습격 시작, 어디로 갈지, 공격 고르기.
    void Fight(float dt)
    {
        StepShots(dt);
        var kong = combat.Kong;
        // 처음 마을에 들어옴: 소매를 들어 올리자 저주가 거기 있는 모두를 묶는다.
        if (kong != null && !kong.Raided && Feet.x < PrototypeWorld.KongEnd - 2 && kong.AnyoneToTake)
        {
            kong.StartRaid();
            CastAt = Time.time; castUntil = Time.time + 1.3f; Doing = Attack.None; KillAt = -1;
            JinaFx.Shock(Feet, 2.2f); PrototypeView.Shake(.08f, .5f);
        }
        Victim = kong != null && kong.Raided ? kong.NextVictim() : null;
        if (Time.time < stunnedUntil) return;
        if (castUntil > 0) { if (Time.time < castUntil) return; castUntil = 0; }
        if (KillAt >= 0) { StepKill(kong); return; }
        if (Doing != Attack.None) { StepAttack(); return; }
        if (!grounded || airborne || crouchUntil > 0) return;
        float x = Feet.x, d = Mathf.Abs(PlayerX - x);
        if (Victim.HasValue && Time.time >= nextKillAt && Mathf.Abs(kong.PersonX(Victim.Value) - x) <= KillReach + .5f)
        {
            StartKill(kong, Victim.Value);
            return;
        }
        if (Time.time >= nextAttackAt && d < AttackRange && Mathf.Abs(PlayerY - Feet.y) < 6.5f && combat.Maps != null && combat.Maps.Current == PrototypeMaps.Index(x)
            && (Reachable || d < 10))
        {
            StartAttack(d);
            return;
        }
        if (Time.time < hopReadyAt) { Facing = (Victim.HasValue ? kong.PersonX(Victim.Value) : PlayerX) >= x ? 1 : -1; return; }
        float? goal = Goal(kong);
        if (goal.HasValue && Mathf.Abs(goal.Value - x) > .6f) Hop(goal.Value);
        else Facing = PlayerX >= x ? 1 : -1;
    }

    // 가고 싶은 곳: 다음 희생자 옆, 아니면 선비와 던지기 좋은 거리, 선비가 강을 건넜으면
    // 건너편 둑(마을에 남은 사람이 없을 때) 또는 마을로 돌아감.
    public float? Goal(PrototypeKongVillage kong)
    {
        float x = Feet.x;
        var victim = kong != null && kong.Raided ? kong.NextVictim() : null;
        if (victim.HasValue) { float vx = kong.PersonX(victim.Value); return Mathf.Clamp(vx + (x >= vx ? KillReach : -KillReach), MinX, MaxX); }
        if (!Reachable)
        {
            if (kong != null && (kong.AnyoneToTake || kong.Raided)) return kong.AnyoneToTake ? PrototypeWorld.KongEnd - 4 : VillageCentre;
            return MinX;
        }
        float dx = PlayerX - x, d = Mathf.Abs(dx);
        if (d > Far) return Mathf.Clamp(PlayerX - Mathf.Sign(dx) * Keep, MinX, MaxX);
        if (d < Close && Time.time < nextAttackAt) return Mathf.Clamp(x - Mathf.Sign(dx) * 3.5f, MinX, MaxX);
        // 선비가 더 높이 있다(요괴가 있는 움푹한 곳 밖, 계단 위, 발판 위): 따라 올라간다.
        if (Above > .8f) return PlayerX;
        return null;
    }

    // `goal` 쪽으로 뛴다(필요하면 높이 뛰기).
    public void Hop(float goal)
    {
        hopDir = goal > Feet.x ? 1 : -1; Facing = hopDir;
        float d = Mathf.Abs(goal - Feet.x);
        // 같은 방향으로 뛴 지난번이 제자리였다(못 넘은 벽 앞): 이번엔 높이 뛴다.
        stuckHops = Mathf.Abs(Feet.x - lastHopX) < .4f && hopDir == lastHopDir ? stuckHops + 1 : 0;
        lastHopX = Feet.x; lastHopDir = hopDir;
        HighHop = Above > 1.2f || WallAhead(hopDir) || stuckHops > 0;
        hopRise = HighHop ? HighRise : HopRise;
        float air = 2 * hopRise / Gravity;
        hopSpeed = Mathf.Clamp(d / air + .4f, 1.6f, d > 6 ? ChaseHop : StepHop);
        crouchUntil = Time.time + CrouchLength;
    }
    public bool HighHop { get; private set; }
    // 앞에 닿을 만한 거리에서 솟은 단단한 땅(계단, 움푹한 곳의 옆면). 발판은 치지 않는다.
    bool WallAhead(int dir)
    {
        foreach (var hit in Physics2D.RaycastAll(Feet + new Vector3(0, .5f, 0), Vector2.right * dir, 1.7f, 1 << 8))
            if (!hit.collider.usedByEffector) return true;
        return false;
    }

    // 물리 프레임마다 뛰기, 공격, 탄, 마을 죽이기를 진행한다.
    void FixedUpdate()
    {
        if (body == null || !body.simulated || !Fighting) return;
        var filter = new ContactFilter2D(); filter.SetLayerMask(1 << 8); filter.useTriggers = false;
        int count = shape.Cast(Vector2.down, filter, groundHits, .08f);
        grounded = false;
        for (int i = 0; i < count; i++) if (groundHits[i].normal.y > .65f) grounded = true;
        var v = body.velocity;
        if (crouchUntil > 0 && Time.time >= crouchUntil)
        {
            crouchUntil = 0; airborne = true; leftGroundAt = Time.time;
            v = new Vector2(hopDir * hopSpeed, hopRise);
            JinaFx.Dust(Feet, -hopDir * 1.2f, 1.2f);
        }
        else if (airborne)
        {
            v.x = hopDir * hopSpeed;
            if (grounded && v.y <= .05f && Time.time > leftGroundAt + .12f)
            {
                airborne = false; v.x = 0; landedAt = Time.time; hopReadyAt = Time.time + HopRest;
                JinaFx.Dust(Feet, -1, 1.3f); JinaFx.Dust(Feet, 1, 1.3f);
                if (combat.Maps != null && combat.Maps.Current == PrototypeMaps.Index(Feet.x)) PrototypeView.Shake(.05f, .15f);
            }
        }
        else v.x = 0;
        if ((body.position.x <= MinX && v.x < 0) || (body.position.x >= MaxX && v.x > 0)) v.x = 0;
        body.velocity = v;
        if (body.position.x < MinX - .05f) body.position = new Vector2(MinX, body.position.y);
    }

    // 거리 `d`에 맞는 공격을 고르고 준비 동작을 시작한다.
    void StartAttack(float d)
    {
        Facing = PlayerX >= Feet.x ? 1 : -1;
        bool level = Mathf.Abs(PlayerY - (Feet.y + 1)) < 1.6f;
        Doing = d < Close && level ? Attack.Tongue : Weakened && attackCount % 3 == 2 ? Attack.Salt : attackCount % 2 == 0 ? Attack.Spit : Attack.Beans;
        attackCount++;
        attackAt = Time.time; releaseAt = Time.time + Windup(Doing);
        released = tongueLanded = false;
        JinaFx.Glint(Doing == Attack.Tongue ? Mouth(MouthLow) : Mouth(MouthOpen), new Color(1, .7f, .3f));
    }

    // 공격을 한 프레임 진행한다(준비 → 발사 → 회복).
    void StepAttack()
    {
        float t = Time.time;
        if (!released && t >= releaseAt) { released = true; Release(Doing); }
        if (Doing == Attack.Tongue && released) Lash(t - releaseAt, PlayerBody, true);
        if (released && t >= releaseAt + Recovery(Doing))
        {
            Doing = Attack.None;
            nextAttackAt = t + (Weakened ? 1f : 1.5f) + (attackCount % 3) * .2f;
            hopReadyAt = t + .1f;
        }
    }

    // 공격을 내보낸다: 젓갈, 콩알, 소금 비, 혀.
    void Release(Attack a)
    {
        var mouth = Mouth(MouthOpen);
        var target = new Vector2(PlayerX, PlayerBody.center.y - .2f);
        if (a == Attack.Spit)
        {
            float dx = target.x - mouth.x, dy = target.y - mouth.y;
            float time = Mathf.Clamp(Mathf.Abs(dx) / 8f, .55f, 1.1f);
            Throw(a, "kong_spit", mouth, new Vector2(dx / time, (dy + .5f * SpitGravity * time * time) / time), 4);
            JinaFx.Dots(mouth, 6, new Color(.86f, .7f, .7f), 2.5f, .3f, Facing);
        }
        else if (a == Attack.Beans)
        {
            var aim = (target - (Vector2)mouth).normalized;
            int n = Weakened ? 7 : 5; float spread = Weakened ? 24 : 18;
            for (int i = 0; i < n; i++)
                Throw(a, "kong_bean", mouth, (Vector2)(Quaternion.Euler(0, 0, Mathf.Lerp(-spread, spread, i / (n - 1f))) * aim) * BeanSpeed, 1.6f);
            JinaFx.Dots(mouth, 5, new Color(.9f, .8f, .5f), 3, .25f, Facing);
        }
        else if (a == Attack.Salt)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = PlayerX + (i - 2) * 1.7f + ((attackCount + i) % 3 - 1) * .35f;
                var s = Throw(a, "kong_salt", new Vector3(x, 8.4f + (i % 2) * .6f, 0), Vector2.down * SaltFall, 2);
                s.mark = new GameObject("소금 그림자").AddComponent<SpriteRenderer>();
                s.mark.sprite = JinaArt.PropCentred("kong_salt"); s.mark.sortingOrder = 3;
                s.mark.transform.position = new Vector3(x, Landing(x) + .1f, 0);
            }
            JinaFx.Rise(Feet + Vector3.up * 2.4f, 12, new Color(.96f, .96f, .92f), 1.2f, 2.5f, .6f);
        }
        else JinaFx.Dust(Feet, Facing * 1.4f, 1.2f);
    }
    // 한 점 아래 첫 번째 바닥(발판 포함): 위에서 떨어지는 소금이 닿는 곳.
    static float Landing(float x)
    {
        var hit = Physics2D.Raycast(new Vector2(x, 8), Vector2.down, 12, 1 << 8);
        return hit.collider != null ? hit.point.y : PrototypeWorld.Top(x);
    }
    // 탄 하나를 던진다.
    Shot Throw(Attack kind, string art, Vector3 at, Vector2 v, float life)
    {
        var s = new Shot { kind = kind, v = v, life = life, art = new GameObject("콩쥐 요괴가 던진 것").AddComponent<SpriteRenderer>() };
        s.art.sprite = JinaArt.PropCentred(art); s.art.sortingOrder = 5;
        s.art.transform.position = at;
        shots.Add(s);
        return s;
    }

    // 던진 것을 움직인다. 무엇에 닿는지: 선비, 독, 땅, 벽(콩은 화살처럼 발판을 통과하고,
    // 떨어지는 것은 발판에 내려앉는다). 젓갈 덩어리는 평평한 곳에 떨어지면 소금물을 남긴다.
    public void StepShots(float dt)
    {
        var him = PlayerBody;
        for (int i = shots.Count - 1; i >= 0; i--)
        {
            var s = shots[i];
            Vector2 from = s.art.transform.position;
            if (s.kind == Attack.Spit) s.v.y -= SpitGravity * dt;
            Vector2 step = s.v * dt, to = from + step;
            s.life -= dt;
            bool gone = false;
            var me = new Bounds(to, Vector3.one * (s.kind == Attack.Spit ? .45f : .25f));
            if (me.Intersects(him))
            {
                combat.HurtPlayer(s.kind == Attack.Spit ? SpitDamage : s.kind == Attack.Salt ? SaltDamage : BeanDamage);
                Burst(s, to); gone = true;
            }
            else if (ShotCrock(me)) { Burst(s, to); gone = true; }
            else if (step.sqrMagnitude > 0)
            {
                var hit = Surface(from, step, s.kind != Attack.Beans);
                if (hit.collider != null)
                {
                    Burst(s, hit.point);
                    if (s.kind == Attack.Spit && hit.normal.y > .5f) Spill(hit.point);
                    gone = true;
                }
            }
            if (gone || s.life <= 0)
            {
                Destroy(s.art.gameObject); if (s.mark != null) Destroy(s.mark.gameObject);
                shots.RemoveAt(i);
                continue;
            }
            s.art.transform.position = to;
            s.art.transform.rotation = Quaternion.Euler(0, 0, s.kind == Attack.Salt ? 0 : Time.time * 540 % 360);
            if (s.mark != null) s.mark.color = new Color(1, 1, 1, .35f + .35f * Mathf.Sin(Time.time * 18));
        }
        var feet = new Vector2(PlayerX, him.min.y);
        for (int i = puddles.Count - 1; i >= 0; i--)
        {
            var p = puddles[i];
            float left = p.until - Time.time;
            if (left <= 0) { Destroy(p.art.gameObject); puddles.RemoveAt(i); continue; }
            p.art.color = new Color(1, 1, 1, .8f * Mathf.Clamp01(left / .6f));
            if (Mathf.Abs(feet.x - p.x) < .9f && Mathf.Abs(feet.y - p.y) < .3f) combat.HurtPlayer(BrineDamage);
        }
    }
    // `from`에서 `step`만큼 가는 동안 처음 닿는 바닥이나 벽.
    static RaycastHit2D Surface(Vector2 from, Vector2 step, bool landsOnLedges)
    {
        foreach (var hit in Physics2D.RaycastAll(from, step.normalized, step.magnitude, 1 << 8))
        {
            if (!hit.collider.usedByEffector) return hit;
            if (landsOnLedges && step.y < 0 && hit.normal.y > .5f) return hit;
        }
        return default;
    }
    // 탄이 터지며 파편을 튀긴다.
    void Burst(Shot s, Vector2 at)
    {
        var colour = s.kind == Attack.Spit ? new Color(.86f, .66f, .66f) : s.kind == Attack.Salt ? new Color(.97f, .97f, .94f) : new Color(.9f, .8f, .5f);
        JinaFx.Dots(at, s.kind == Attack.Spit ? 10 : 4, colour, 3, .35f, 0, 12);
    }
    // 젓갈이 떨어진 곳에 소금물 웅덩이를 남긴다.
    void Spill(Vector2 at)
    {
        var art = new GameObject("짠물 웅덩이").AddComponent<SpriteRenderer>();
        art.sprite = JinaArt.Prop("kong_brine"); art.sortingOrder = 3;
        art.transform.position = new Vector3(at.x, at.y - .05f, 0);
        puddles.Add(new Puddle { art = art, x = at.x, y = at.y, until = Time.time + 3f });
    }

    // 혀: 끝까지 뻗고, 멈추고, 거둔다. 닿은 것을 한 번 다치게 한다. 얼마나 뻗었는지 돌려준다.
    float TongueLength(float t) => t < TongueOut ? TongueReach * t / TongueOut : t < TongueOut + TongueHold ? TongueReach
        : t < TongueOut + TongueHold + TongueBack ? TongueReach * (1 - (t - TongueOut - TongueHold) / TongueBack) : 0;
    // 혀가 `length`만큼 뻗었을 때의 판정 상자.
    public Bounds TongueBox(float length)
    {
        var from = Mouth(MouthLow);
        return new Bounds(from + Vector3.right * Facing * length / 2, new Vector3(Mathf.Max(.01f, length), .45f, 1));
    }
    // 혀를 휘두른다(선비 또는 묶인 마을 사람에게).
    void Lash(float t, Bounds target, bool atHim)
    {
        float length = TongueLength(t);
        if (tongueLanded || length <= 0 || !TongueBox(length).Intersects(target)) return;
        tongueLanded = true;
        if (atHim) combat.HurtPlayer(TongueDamage);
    }

    // ---- 마을 ----
    void StartKill(PrototypeKongVillage kong, PrototypeKongVillage.Who who)
    {
        killing = who; KillAt = Time.time; killed = false; stagger = 0;
        Facing = kong.PersonX(who) >= Feet.x ? 1 : -1;
        kong.Threaten(who);
    }
    // 마을에서 묶인 사람을 조이고 죽이는 과정을 진행한다.
    void StepKill(PrototypeKongVillage kong)
    {
        float t = Time.time - KillAt;
        if (t < KillWindup) return;
        if (!killed) { killed = true; kong.Kill(killing); PrototypeView.Shake(.06f, .2f); }
        if (t < KillWindup + TongueOut + TongueHold + TongueBack) return;
        KillAt = -1; nextKillAt = Time.time + KillGap; hopReadyAt = Time.time + .2f;
    }
    // 죽이려고 준비하는 동안의 칼질. 충분하면 움찔하며 그 사람을 잠시 살려 둔다.
    void Stagger(int amount)
    {
        if (KillAt < 0 || killed) return;
        stagger += amount;
        if (stagger < StaggerAt) return;
        if (combat.Kong != null) combat.Kong.Spare(killing);
        KillAt = -1; stunnedUntil = Time.time + .9f; nextKillAt = Time.time + 2.6f;
        JinaFx.Word(Feet + Vector3.up * 3f, "!", new Color(1, .9f, .5f), 30);
    }

    // ---- 싸움 그리기 ----
    void DrawFight()
    {
        tongue.enabled = tip.enabled = false;
        if (!Fighting) return;
        float length = 0;
        if (Doing == Attack.Tongue && released) length = TongueLength(Time.time - releaseAt);
        if (KillAt >= 0 && killed && combat.Kong != null)
            length = Mathf.Min(TongueLength(Time.time - KillAt - KillWindup) / TongueReach * (KillReach + .3f), Mathf.Abs(combat.Kong.PersonX(killing) - Mouth(MouthLow).x) + .2f);
        if (length <= .05f) return;
        var from = Mouth(MouthLow);
        tongue.enabled = tip.enabled = true;
        tongue.size = new Vector2(length, tongue.sprite.bounds.size.y);
        tongue.transform.position = Snap(from + Vector3.right * Facing * length / 2);
        tip.transform.position = Snap(from + Vector3.right * Facing * length);
        tip.flipX = Facing < 0;
    }
    // 싸우는 요괴 모습(몸, 입, 혀)을 그린다.
    void DrawFighter(bool flip)
    {
        float now = Time.time;
        string pose = (int)(now / .55f + Feet.x) % 2 == 0 ? "idle_00" : "idle_01";
        bool windup = false;
        if (KillAt >= 0) { pose = killed ? "tongue" : "cast"; windup = !killed; }
        else if (castUntil > 0 || (CastAt >= 0 && now - CastAt < 1.3f)) pose = "cast";
        else if (now < stunnedUntil) pose = "crouch";
        else if (Doing == Attack.Tongue) { pose = !released && now - attackAt < .25f ? "crouch" : "tongue"; windup = !released; }
        else if (Doing == Attack.Salt) { pose = "cast"; windup = !released; }
        else if (Doing != Attack.None) { pose = !released && now < releaseAt - .15f ? "idle_01" : now < releaseAt + .3f ? "open" : "idle_00"; windup = !released; }
        else if (crouchUntil > 0 || (landedAt >= 0 && now - landedAt < .12f)) pose = "crouch";
        else if (airborne) pose = "hop";
        var feet = Feet;
        if (KillAt >= 0 && !killed || now < stunnedUntil) feet += Vector3.right * Shudder(now);
        Show(picture, JinaArt.Load(Pose(pose)), feet, flip);
        if (windup) picture.color = new Color(1, .72f, .45f);
        float since = HitAt < 0 ? 1 : now - HitAt;
        if (since < .12f) Flash(picture, .85f * (1 - since / .12f), Color.white);
    }

    // 위 가운데 체력 막대(팀원 방식), 절반에 표시: 그 아래면 은비녀가 닿는다.
    void DrawFightGUI()
    {
        if (CastAt >= 0 && Time.time - CastAt < 3.2f && SpeechBubble.OnScreen(Feet))
            SpeechBubble.Draw(Feet + Vector3.up * 3.2f, "콩쥐 요괴", "보름까지 기다릴 것 없지. 잔치는 지금이다!");
        if (!Fighting) return;
        var oldMatrix = GUI.matrix; var oldColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;
        float width = Screen.width * .4f, height = 18, x = (Screen.width - width) / 2, y = 32;
        float ratio = (float)Health / MaxHealth;
        GUI.color = new Color(0, 0, 0, .75f); GUI.DrawTexture(new Rect(x - 2, y - 2, width + 4, height + 4), Texture2D.whiteTexture);
        GUI.color = new Color(.12f, .2f, .08f); GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = new Color(.5f, .7f, .25f); GUI.DrawTexture(new Rect(x, y, width * ratio, height), Texture2D.whiteTexture);
        GUI.color = new Color(1, .95f, .8f, .9f); GUI.DrawTexture(new Rect(x + width / 2 - 1, y - 4, 2, height + 8), Texture2D.whiteTexture);
        GUI.color = Color.white;
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(x, y - 26, width, 24), Name + "   " + Health + " / " + MaxHealth, style);
        GUI.color = oldColor; GUI.matrix = oldMatrix;
    }
}
