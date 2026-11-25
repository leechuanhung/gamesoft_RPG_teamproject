using System.Collections.Generic;
using UnityEngine;

// ===== 한양 가는 길의 몬스터와 관군 (RoadMobs) =====
// 기획서.md 1-2·1-3. 버려진 역참과 북망산 무덤길에는 유령(Camp) 말고도 새 일반 몬스터가 섞여 나온다:
//   도깨비불(Wisp): 떠다니다가 빛을 모은 뒤 선비에게 곧장 날아든다.
//   두억시니(Brute): 걸어와서 두 주먹을 들어 올렸다가 내려찍는다.
// 선악이 음수면 일반 몬스터가 나오는 맵(망령의 들판, 나루터, 역참, 무덤길)에 들어설 때 관군이 한 무리 나타나 선비를
// 공격한다. 음수가 클수록 무장이 높아지고 수가 는다(Rank 1~3). 칼병(Blade)은 다가와 칼을 뒤로 당겼다가 찌르듯 베고,
// 활병(Bow)은 거리를 두고 활을 당겼다가 쏜다. 좌의정 댁 싸움에도 같은 규칙으로 들이닥친다(FinalBoss).
// 모두 물리 없이 땅 높이(WorldBuilder.Top)를 따라 걷고, 자기 구역 밖으로는 나가지 않는다.
public class RoadMobs : MonoBehaviour
{
    public Combat combat;
    public enum Kind { Wisp, Brute, Blade, Bow }

    // ── 능력치 ──
    // 역참·무덤길(후반)의 몬스터라 들판 유령(30·10·2)보다 세다. 도깨비불은 약하지만 빠르고, 두억시니는 단단하고 아프다.
    public const int WispHealth = 30, WispAttack = 12, WispDefense = 3;
    public const int BruteHealth = 70, BruteAttack = 18, BruteDefense = 5;
    // 관군 무장 단계별 체력·공격력·방어력(1~3). 선비 공격력에서 방어력만큼 덜 들어간다.
    public static readonly int[] SoldierHealth = { 0, 35, 45, 60 }, SoldierAttack = { 0, 8, 13, 18 }, SoldierDefense = { 0, 0, 2, 4 };
    // 선악에 따른 관군 무장 단계: 0 이상이면 0(오지 않음), −1~−29 → 1, −30~−59 → 2, −60 이하 → 3.
    public static int Rank(int honor) => honor >= 0 ? 0 : honor > -30 ? 1 : honor > -60 ? 2 : 3;
    // 무리 구성(칼병, 활병): 단계가 높을수록 많다.
    public static (int blades, int bows) Squad(int rank) => rank <= 0 ? (0, 0) : rank == 1 ? (1, 1) : rank == 2 ? (2, 1) : (2, 2);
    public const float WispSpeed = 2.4f, WispDash = 9f, WispDashLength = .45f, WispWindup = .55f, WispCooldown = 1.5f;
    public const float BruteSpeed = 1.3f, BruteWindup = .7f, BruteCooldown = 1.7f, BruteReach = 1.9f;
    public const float SoldierSpeed = 2.0f, BladeReach = 1.6f, BowNear = 3.5f, BowFar = 8.5f, ArrowSpeed = 10f;
    public const float Sight = 10f, DownLength = 1.4f;
    public static float BladeWindup(int rank) => rank >= 3 ? .4f : rank == 2 ? .48f : .55f;
    public static float BowDraw(int rank) => rank >= 3 ? .6f : rank == 2 ? .72f : .85f;
    // 관군이 나오는 맵(Maps.All 번호)과 그 안에서 걸을 수 있는 범위.
    public static readonly (int map, float left, float right)[] SoldierZones =
    {
        (1, WorldBuilder.GateX + 1, WorldBuilder.VillageLeft - 1),
        (4, WorldBuilder.FerryStart + 1, WorldBuilder.RiverLeft - .6f),
        (9, WorldBuilder.FortEnd + 1, WorldBuilder.StationEnd - 1),
        (10, WorldBuilder.StationEnd + 1, WorldBuilder.GraveEnd - 1),
    };

    // 머리 위와 쓰러뜨렸을 때 알림에 쓰는 이름.
    public static string Name(Mob m) => m.soldier ? (m.kind == Kind.Bow ? "관군 활병" : "관군 칼병") : m.kind == Kind.Wisp ? "도깨비불" : "두억시니";
    // 쓰러뜨렸을 때 알림. 관군은 사람이라 "죽었다"고 한다. {0} 자리에 엽전 수.
    public static string FelledNote(Mob m) => m.soldier ? Name(m) + "이 죽었습니다. 품에서 엽전 {0}문이 나왔습니다."
        : m.kind == Kind.Wisp ? "도깨비불이 꺼지며 엽전 {0}문을 떨어뜨렸습니다." : "두억시니가 쓰러지며 엽전 {0}문을 떨어뜨렸습니다.";
    public class Mob
    {
        public Kind kind; public int rank, hp, max, attack, defense, map;
        public SpriteRenderer art;
        public Vector3 pos;
        public float home, left, right, ready, windup = -1, strikeUntil = -1, hurtAt = -1, downAt = -1, walk, phase;
        public int facing = -1;
        public Vector2 dash;
        public bool soldier, leaving, struck, walkingNow, witness;
        public float lastWalk;
        public bool Alive => hp > 0;
        public bool Flying => kind == Kind.Wisp;
        // 맞히기 쉬운 몸통 상자.
        public Bounds Body => Flying ? new Bounds(pos, new Vector3(.8f, .9f, 1))
            : kind == Kind.Brute ? new Bounds(pos + Vector3.up * .85f, new Vector3(1.4f, 1.7f, 1)) : new Bounds(pos + Vector3.up * .9f, new Vector3(.9f, 1.8f, 1));
    }
    public readonly List<Mob> mobs = new List<Mob>();
    class Arrow { public SpriteRenderer art; public Vector2 pos, dir; public float left; public int attack; }
    readonly List<Arrow> arrows = new List<Arrow>();
    readonly HashSet<int> squads = new HashSet<int>();
    // 관군이 나타나며 외친 말과 그 시각.
    public float ShoutAt { get; private set; } = -1;
    public string Shout { get; private set; } = "";
    Mob shouter;
    int lastMap = -1;

    // 맵에 사는 일반 몬스터: (종류, x).
    static readonly (Kind kind, float x)[] Wilds =
    {
        (Kind.Wisp, 300f), (Kind.Wisp, 314f), (Kind.Wisp, 325f), (Kind.Brute, 316f), (Kind.Brute, 333f),
        (Kind.Wisp, 345f), (Kind.Wisp, 361f), (Kind.Wisp, 376f), (Kind.Brute, 357f), (Kind.Brute, 373f),
    };

    void Start() { ResetMobs(); }

    // 재시작: 몬스터를 처음 자리로, 관군과 화살은 모두 지운다.
    public void ResetMobs()
    {
        foreach (var m in mobs) if (m.art != null) Destroy(m.art.gameObject);
        mobs.Clear(); squads.Clear(); ClearArrows();
        ShoutAt = -1; shouter = null; lastMap = -1;
        foreach (var (kind, x) in Wilds)
        {
            int map = Maps.Index(x);
            Add(kind, 0, x, map, Maps.All[map].left + .8f, Maps.All[map].right - .8f, false);
        }
    }
    // 길에서 다시 일어날 때(Combat.RestartOnRoad): 관군 무리는 지우고 다시 나올 수 있게, 몬스터는 그대로.
    public void ClearSoldiers()
    {
        for (int i = mobs.Count - 1; i >= 0; i--)
            if (mobs[i].soldier) { if (mobs[i].art != null) Destroy(mobs[i].art.gameObject); mobs.RemoveAt(i); }
        squads.Clear(); ClearArrows(); ShoutAt = -1; shouter = null; lastMap = -1;
    }

    Mob Add(Kind kind, int rank, float x, int map, float left, float right, bool soldier)
    {
        var art = new GameObject(soldier ? "관군" : kind == Kind.Wisp ? "도깨비불" : "두억시니").AddComponent<SpriteRenderer>();
        art.transform.SetParent(transform);
        art.sortingOrder = soldier ? 4 : 3;
        var m = new Mob { kind = kind, rank = rank, art = art, home = x, left = left, right = right, map = map, soldier = soldier, phase = mobs.Count * 1.7f };
        m.max = m.hp = soldier ? SoldierHealth[rank] : kind == Kind.Wisp ? WispHealth : BruteHealth;
        m.attack = soldier ? SoldierAttack[rank] : kind == Kind.Brute ? BruteAttack : WispAttack;
        m.defense = soldier ? SoldierDefense[rank] : kind == Kind.Brute ? BruteDefense : WispDefense;
        m.pos = new Vector3(x, kind == Kind.Wisp ? 1.6f : WorldBuilder.Top(x), 0);
        m.ready = Time.time + 1;
        mobs.Add(m);
        Draw(m);
        return m;
    }

    // `map`에 관군 무리를 부른다(이미 왔으면 그대로, `wave`로 같은 맵에 한 무리 더). 선비가 들어온 쪽 반대편 끝에서 걸어 들어온다.
    public bool CallSquad(int map, int rank, float left, float right, string shout = null, int wave = 0)
    {
        if (rank <= 0 || squads.Contains(map * 10 + wave)) return false;
        squads.Add(map * 10 + wave);
        var (blades, bows) = Squad(rank);
        float px = combat.player.transform.position.x;
        bool fromRight = px < (left + right) / 2;
        float edge = fromRight ? right - .5f : left + .5f, step = fromRight ? -1.1f : 1.1f;
        int n = 0;
        Mob first = null;
        for (int i = 0; i < blades; i++, n++) { var m = Add(Kind.Blade, rank, edge + step * n, map, left, right, true); first = first ?? m; }
        for (int i = 0; i < bows; i++, n++) Add(Kind.Bow, rank, edge + step * (n + 1.5f), map, left, right, true);
        shouter = first; ShoutAt = Time.time;
        Shout = shout ?? SquadShout(rank);
        return true;
    }
    // 관군이 나타나며 외치는 말: 무장이 높을수록 확신에 차 있다.
    // 선악이 음수인 것은 선비가 요괴를 끌고 다니는 동안 사람들이 죽었기 때문이다. 소문은 그것을 선비 탓으로 돌린다.
    public static string SquadShout(int rank) => rank >= 3 ? "좌의정 대감의 명이다! 저 선비는 산 채로 잡든 죽이든 상관없다, 쳐라!"
        : rank == 2 ? "요괴를 끌고 다니며 마을을 피로 물들였다는 선비가 저놈이다! 포위하라!"
        : "거기 선비, 멈춰라! 네가 지나는 마을마다 사람이 죽어 나간다는 소문이 파다하다!";
    // 선악이 0 이상일 때 좌의정 댁에 들어왔다가 물러나는 관군 둘: 다치지도 덤비지도 않는다.
    public void CallWitnesses(int map, float left, string shout)
    {
        if (squads.Contains(map * 10 + 9)) return;
        squads.Add(map * 10 + 9);
        Mob first = null;
        for (int i = 0; i < 2; i++)
        {
            var m = Add(i == 0 ? Kind.Blade : Kind.Bow, 1, left + i * 1.1f, map, left, left + 4f, true);
            m.witness = true; m.facing = 1; m.ready = Time.time + 3.6f;
            first = first ?? m;
        }
        shouter = first; ShoutAt = Time.time + .8f; Shout = shout;
    }
    // 싸움이 끝나 관군이 물러난다(좌의정이 쓰러졌을 때).
    public void Disband(int map)
    {
        foreach (var m in mobs) if (m.soldier && m.map == map && m.Alive) { m.leaving = true; m.windup = -1; }
    }
    public int SoldiersAlive(int map) { int n = 0; foreach (var m in mobs) if (m.soldier && m.map == map && m.Alive && !m.leaving && !m.witness) n++; return n; }
    public int Alive(int map) { int n = 0; foreach (var m in mobs) if (m.map == map && m.Alive) n++; return n; }

    // ── 맞기 ──
    public bool Slash(Bounds area, int damage = Combat.SwordDamage)
    {
        bool landed = false;
        foreach (var m in mobs) if (m.Alive && !m.leaving && !m.witness && area.Intersects(m.Body)) { Hit(m, damage); landed = true; }
        return landed;
    }
    public void Hit(Mob m, int damage)
    {
        if (m == null || !m.Alive) return;
        if (combat != null && combat.Cheats != null) damage = combat.Cheats.Power(damage);   // 개발자 치트: 한 방
        int before = m.hp;
        m.hp = Mathf.Max(0, m.hp - Combat.CalcDamage(damage + (combat != null ? combat.BonusAttack : 0), JinaItems.RopeDefense(m, m.defense, combat)));
        m.hurtAt = Time.time;
        var at = m.Body.center;
        int away = combat.player.transform.position.x < m.pos.x ? 1 : -1;
        JinaFx.Hit(at, away, m.kind == Kind.Wisp ? new Color(.7f, .9f, 1) : new Color(1, .92f, .72f));
        JinaFx.Number(at + new Vector3(Random.Range(-.2f, .2f), .9f, 0), before - m.hp, Color.white);
        if (m.Alive)
        {
            // 움찔 밀려나 한 박자 늦어진다. 두억시니는 덜 밀린다.
            if (m.kind != Kind.Brute) m.pos.x = Mathf.Clamp(m.pos.x + away * .22f, m.left, m.right);
            if (m.windup >= 0 && m.kind != Kind.Brute) { m.windup = -1; }
            m.ready = Mathf.Max(m.ready, Time.time + .3f);
            return;
        }
        m.downAt = Time.time; m.windup = -1; m.strikeUntil = -1;
        GameAudio.Play(m.kind == Kind.Wisp ? GameAudio.Sfx.GhostDie : GameAudio.Sfx.MobDie);
        JinaItems.ForgetMob(m);
        if (!m.soldier) JinaItems.RollDrops(m.pos);
        JinaFx.Stop(.07f);
        if (m.kind == Kind.Wisp) JinaFx.Dots(at, 14, new Color(.6f, .85f, 1), 3, .5f);
        else JinaFx.Dust(m.pos, 0, m.kind == Kind.Brute ? 1.4f : 1);
        if (combat != null) combat.FindCoins(m.soldier ? 3 : 2, FelledNote(m));
        if (combat != null) combat.GainExp(m.soldier ? Combat.SoldierExp[m.rank] : m.kind == Kind.Brute ? Combat.BruteExp : Combat.WispExp);
    }
    // 화살이 이 프레임에 지나는 길에서 가장 먼저 맞는 몬스터(없으면 null)와 그 거리.
    public Mob ArrowTarget(Vector2 origin, Vector2 direction, float step, out float distance)
    {
        Mob best = null; distance = float.PositiveInfinity;
        foreach (var m in mobs)
        {
            if (!m.Alive || m.leaving || m.witness) continue;
            float d = Combat.ArrowHitDistance(m.Body, origin, direction, step);
            if (d < distance) { distance = d; best = m; }
        }
        return best;
    }

    // ── 움직임 ──
    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) { foreach (var m in mobs) Draw(m); return; }
        int map = combat.Maps != null ? combat.Maps.Current : -1;
        if (map != lastMap && combat.Maps != null && !combat.Maps.Travelling) { lastMap = map; Entered(map); }
        if (combat.Paused)
        {
            foreach (var m in mobs) { if (m.windup >= 0) m.windup = -1; m.ready = Time.time + .5f; Draw(m); }
            return;
        }
        var p = combat.player.transform.position;
        float dt = Time.deltaTime;
        for (int i = mobs.Count - 1; i >= 0; i--)
        {
            var m = mobs[i];
            if (!m.Alive)
            {
                if (Time.time - m.downAt > DownLength + .6f) { Destroy(m.art.gameObject); mobs.RemoveAt(i); continue; }
                Draw(m); continue;
            }
            if (m.leaving) { Leave(m, dt); if (m.art == null) { mobs.RemoveAt(i); continue; } Draw(m); continue; }
            if (m.witness) { Witness(m, p, dt); Draw(m); continue; }
            bool here = combat.Maps == null || (Maps.Index(p.x) == m.map && !combat.Maps.Travelling);
            switch (m.kind)
            {
                case Kind.Wisp: Wisp(m, p, here, dt); break;
                case Kind.Brute: Brute(m, p, here, dt); break;
                case Kind.Blade: Blade(m, p, here, dt); break;
                case Kind.Bow: Bow(m, p, here, dt); break;
            }
            Draw(m);
        }
        StepArrows(dt);
    }

    // 맵에 들어서면 선악에 따라 관군을 부른다. 콩쥐 요괴를 해결한 뒤 산성과 한양 거리에 들어서면 체크포인트.
    void Entered(int map)
    {
        if (combat.Quest == null) return;
        bool kongDone = combat.KongBoss == null || combat.KongBoss.Resolved;
        if (kongDone && (map == Maps.Index(WorldBuilder.FortStart) || map == Maps.Index(WorldBuilder.GraveEnd)))
            combat.SaveOnRoad(new Vector2(Maps.All[map].left + 1.5f, .3f));
        int rank = Rank(combat.Quest.progress.Honor);
        foreach (var (zone, left, right) in SoldierZones)
            if (zone == map) CallSquad(map, rank, left, right);
    }

    float Ground(float x) => WorldBuilder.Top(x);
    // 무리 안에서의 자리(0~2): 칼병은 선비와 서로 다른 거리에 서고, 활병도 서로 다른 거리에서 쏜다. 겹쳐 서지 않게.
    static int Slot(Mob m) => (int)(m.phase / 1.7f + .5f) % 3;
    void Walk(Mob m, float toward, float speed, float dt)
    {
        float before = m.pos.x;
        m.pos.x = Mathf.Clamp(Mathf.MoveTowards(m.pos.x, toward, speed * dt), m.left, m.right);
        m.pos.y = Ground(m.pos.x);
        if (Mathf.Abs(m.pos.x - before) > .0001f) { m.walk += Mathf.Abs(m.pos.x - before); m.facing = m.pos.x > before ? 1 : -1; }
    }
    void Face(Mob m, float x) { m.facing = x >= m.pos.x ? 1 : -1; }
    Bounds PlayerBox => combat.player.GetComponent<SpriteRenderer>().bounds;

    void Wisp(Mob m, Vector3 p, bool here, float dt)
    {
        var target = new Vector3(p.x, p.y + .5f, 0);
        if (m.strikeUntil >= 0)
        {
            // 날아드는 중: 몸에 닿으면 다친다.
            m.pos += (Vector3)m.dash * dt;
            m.pos.x = Mathf.Clamp(m.pos.x, m.left, m.right);
            m.pos.y = Mathf.Clamp(m.pos.y, Ground(m.pos.x) + .3f, 5f);
            if (!m.struck && m.Body.Intersects(PlayerBox)) { combat.HurtPlayer(m.attack); m.struck = true; }
            if (Time.time >= m.strikeUntil) { m.strikeUntil = -1; m.ready = Time.time + WispCooldown; }
            return;
        }
        if (m.windup >= 0)
        {
            if (Time.time < m.windup) return;
            m.windup = -1; m.struck = false;
            m.dash = ((Vector2)(target - m.pos)).normalized * WispDash;
            m.strikeUntil = Time.time + WispDashLength;
            Face(m, p.x);
            return;
        }
        var delta = target - m.pos;
        if (here && delta.magnitude < 4f && Time.time >= m.ready) { m.windup = Time.time + WispWindup; Face(m, p.x); return; }
        // 보이면 선비 머리 위 앞쪽으로, 아니면 제자리로 떠간다.
        var goal = here && delta.magnitude < Sight ? target + new Vector3(-Mathf.Sign(delta.x) * 2.4f, 1.4f, 0) : new Vector3(m.home, 1.6f, 0);
        var step = Vector3.MoveTowards(m.pos, goal, WispSpeed * dt);
        step.x = Mathf.Clamp(step.x, m.left, m.right);
        step.y = Mathf.Clamp(step.y + Mathf.Sin(Time.time * 2.2f + m.phase) * .3f * dt, Ground(step.x) + .4f, 4.6f);
        if (Mathf.Abs(step.x - m.pos.x) > .0001f) m.facing = step.x > m.pos.x ? 1 : -1;
        m.pos = step;
    }

    void Brute(Mob m, Vector3 p, bool here, float dt)
    {
        if (m.strikeUntil >= 0) { if (Time.time >= m.strikeUntil) { m.strikeUntil = -1; m.ready = Time.time + BruteCooldown; } return; }
        if (m.windup >= 0)
        {
            if (Time.time < m.windup) return;
            m.windup = -1; m.strikeUntil = Time.time + .45f;
            var hit = new Bounds(m.pos + new Vector3(m.facing * 1.1f, .6f, 0), new Vector3(2f, 1.4f, 1));
            if (hit.Intersects(PlayerBox)) combat.HurtPlayer(m.attack);
            JinaFx.Shock(m.pos + Vector3.right * m.facing * 1.2f, 1.1f);
            JinaFx.Dust(m.pos + Vector3.right * m.facing * 1.2f, m.facing, 1.3f);
            View.Shake(.08f, .2f);
            return;
        }
        float dx = p.x - m.pos.x;
        if (here && Mathf.Abs(dx) < BruteReach && Mathf.Abs(p.y - m.pos.y) < 2.2f && Time.time >= m.ready) { Face(m, p.x); m.windup = Time.time + BruteWindup; return; }
        if (here && Mathf.Abs(dx) < Sight) Walk(m, p.x - Mathf.Sign(dx) * 1.2f, BruteSpeed, dt);
        else Walk(m, m.home, BruteSpeed * .6f, dt);
    }

    void Blade(Mob m, Vector3 p, bool here, float dt)
    {
        if (m.strikeUntil >= 0)
        {
            // 찌르듯 앞으로 내딛는다.
            if (!m.struck && new Bounds(m.pos + new Vector3(m.facing * .95f, .9f, 0), new Vector3(1.5f, 1.2f, 1)).Intersects(PlayerBox)) { combat.HurtPlayer(m.attack); m.struck = true; }
            if (Time.time >= m.strikeUntil) { m.strikeUntil = -1; m.ready = Time.time + 1.0f; }
            return;
        }
        if (m.windup >= 0)
        {
            if (Time.time < m.windup) return;
            m.windup = -1; m.struck = false; m.strikeUntil = Time.time + .3f;
            m.pos.x = Mathf.Clamp(m.pos.x + m.facing * .45f, m.left, m.right); m.pos.y = Ground(m.pos.x);
            JinaFx.Arc(m.pos + new Vector3(m.facing * .9f, .95f, 0), m.facing, -8, .8f, new Color(.86f, .9f, 1));
            return;
        }
        float dx = p.x - m.pos.x;
        if (here && Mathf.Abs(dx) < BladeReach && Mathf.Abs(p.y - m.pos.y) < 1.8f && Time.time >= m.ready) { Face(m, p.x); m.windup = Time.time + BladeWindup(m.rank); return; }
        if (here && Mathf.Abs(dx) < Sight + 4) Walk(m, p.x - Mathf.Sign(dx) * (.9f + Slot(m) * .3f), SoldierSpeed + m.rank * .15f, dt);
        else Walk(m, m.home, SoldierSpeed * .5f, dt);
        if (here) Face(m, p.x);
    }

    void Bow(Mob m, Vector3 p, bool here, float dt)
    {
        if (m.strikeUntil >= 0) { if (Time.time >= m.strikeUntil) { m.strikeUntil = -1; m.ready = Time.time + 1.5f; } return; }
        if (m.windup >= 0)
        {
            Face(m, p.x);
            if (Time.time < m.windup) return;
            m.windup = -1; m.strikeUntil = Time.time + .35f;
            Loose(m, p);
            return;
        }
        float dx = p.x - m.pos.x, d = Mathf.Abs(dx);
        if (!here) { Walk(m, m.home, SoldierSpeed * .5f, dt); return; }
        if (d < BowNear) Walk(m, m.pos.x - Mathf.Sign(dx) * 2, SoldierSpeed * .9f, dt);
        else if (d > BowFar) Walk(m, p.x - Mathf.Sign(dx) * (BowFar - 1 - Slot(m) * 1.2f), SoldierSpeed, dt);
        Face(m, p.x);
        if (d <= BowFar + .5f && Time.time >= m.ready) m.windup = Time.time + BowDraw(m.rank);
    }

    // 관군의 화살: 선비 가슴을 향해 곧게 난다. 땅과 벽에 꽂히고 한 방향 발판은 지난다.
    void Loose(Mob m, Vector3 p)
    {
        var from = (Vector2)(m.pos + new Vector3(m.facing * .55f, 1.15f, 0));
        var to = (Vector2)(p + Vector3.up * .3f);
        var art = new GameObject("관군 화살").AddComponent<SpriteRenderer>();
        art.sprite = JinaArt.PropCentred("soldier_arrow");
        art.sortingOrder = 6;
        var dir = (to - from).normalized;
        arrows.Add(new Arrow { art = art, pos = from, dir = dir, left = 13, attack = m.attack });
        art.transform.position = from;
        art.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }
    void StepArrows(float dt)
    {
        for (int i = arrows.Count - 1; i >= 0; i--)
        {
            var a = arrows[i];
            float step = Mathf.Min(ArrowSpeed * dt, a.left);
            bool done = false;
            foreach (var hit in Physics2D.RaycastAll(a.pos, a.dir, step, 1 << 8))
                if (!hit.collider.usedByEffector) { done = true; JinaFx.Dots(hit.point, 4, new Color(.7f, .6f, .46f), 2, .25f); break; }
            if (!done && Combat.ArrowHitDistance(PlayerBox, a.pos, a.dir, step) <= step) { combat.HurtPlayer(a.attack); done = true; }
            a.pos += a.dir * step; a.left -= step;
            if (done || a.left <= 0) { Destroy(a.art.gameObject); arrows.RemoveAt(i); continue; }
            a.art.transform.position = a.pos;
        }
    }
    void ClearArrows() { foreach (var a in arrows) if (a.art != null) Destroy(a.art.gameObject); arrows.Clear(); }

    // 지켜보는 관군: 들어와 멈춰 서서 선비 쪽을 보다가 물러난다.
    void Witness(Mob m, Vector3 p, float dt)
    {
        if (Time.time >= m.ready) { m.leaving = true; m.right = m.left + 4f; return; }
        Walk(m, m.home + 2.5f, SoldierSpeed * .7f, dt);
        if (Mathf.Abs(m.pos.x - (m.home + 2.5f)) < .05f) Face(m, p.x);
    }
    // 물러나는 관군: 가까운 맵 끝으로 걸어가 사라진다.
    void Leave(Mob m, float dt)
    {
        float edge = m.pos.x - m.left < m.right - m.pos.x ? m.left : m.right;
        Walk(m, edge, SoldierSpeed, dt);
        if (Mathf.Abs(m.pos.x - edge) < .05f) { Destroy(m.art.gameObject); m.art = null; }
    }

    // ── 모습 ──
    static string Prefix(Mob m) => m.soldier ? "22_Soldier/" + (m.kind == Kind.Blade ? "blade_" : "bow_") + m.rank + "_"
        : m.kind == Kind.Wisp ? "23_Mobs/wisp_" : "23_Mobs/brute_";
    public static string Pose(Mob m)
    {
        if (!m.Alive) return "down";
        if (m.kind == Kind.Wisp)
        {
            if (m.strikeUntil >= 0) return "dash";
            return "idle_0" + ((int)(Time.time * 8 + m.phase) % 3);
        }
        if (m.windup >= 0) return "ready";
        if (m.strikeUntil >= 0) return m.kind == Kind.Brute ? "slam" : "strike";
        if (m.soldier && m.hurtAt >= 0 && Time.time - m.hurtAt < .22f) return "hurt";
        if (Time.time - m.hurtAt < .1f) return "idle";
        return m.walkingNow ? "walk_0" + ((int)(m.walk / .32f) % 4) : "idle";
    }
    void Draw(Mob m)
    {
        if (m.art == null) return;
        m.walkingNow = Mathf.Abs(m.walk - m.lastWalk) > .0001f; m.lastWalk = m.walk;
        m.art.sprite = JinaArt.Load(Prefix(m) + Pose(m));
        m.art.flipX = m.facing < 0;
        float bob = m.Flying && m.Alive ? Mathf.Sin(Time.time * 3 + m.phase) * .05f : 0;
        m.art.transform.position = m.pos + Vector3.up * bob;
        float fade = m.Alive ? 1 : Mathf.Clamp01(1 - (Time.time - m.downAt - DownLength * .6f) / .6f);
        var tint = Color.white;
        if (m.Alive && m.windup >= 0) tint = Mathf.FloorToInt(Time.time * 16) % 2 == 0 ? new Color(1, .78f, .5f) : Color.white;
        if (m.Alive && m.hurtAt >= 0 && Time.time - m.hurtAt < .12f) tint = new Color(1, .55f, .55f);
        tint.a = fade;
        m.art.color = tint;
        m.art.enabled = true;
    }

    // 머리 위 체력 막대와 관군의 외침.
    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused || Camera.main == null) return;
        KoreanUI.Apply();
        if (shouter != null && shouter.art != null && shouter.Alive && Time.time >= ShoutAt && Time.time - ShoutAt < 3f)
        {
            // 관군은 맵 반대편 끝에서 들어오므로, 외친 사람이 화면 밖이면 알림으로 들린다.
            var head = shouter.pos + Vector3.up * 2.3f;
            if (SpeechBubble.OnScreen(head)) SpeechBubble.Draw(head, "관군", Shout, null, Time.time - ShoutAt);
            else JinaUI.Toast("관군: " + Shout);
        }
        var oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        foreach (var m in mobs)
        {
            if (!m.Alive || m.leaving || m.witness || m.art == null) continue;
            var screen = Camera.main.WorldToScreenPoint(new Vector3(m.pos.x, m.Body.max.y + .3f, 0));
            if (screen.z < 0 || screen.x < -80 || screen.x > Screen.width + 80) continue;
            float width = 100, height = 18;
            var r = new Rect(screen.x - width / 2, Screen.height - screen.y - height, width, height);
            Combat.DrawBar(r, (float)m.hp / m.max, m.soldier ? new Color(.85f, .55f, .2f) : new Color(.9f, .2f, .2f), m.hp + " / " + m.max, 14);
            Combat.DrawName(r, Name(m) + (m.soldier ? " · " + m.rank + "단계" : ""), m.soldier ? new Color(1, .82f, .6f) : m.kind == Kind.Wisp ? new Color(.7f, .9f, 1) : new Color(.86f, .84f, .92f));
        }
        GUI.matrix = oldMatrix;
    }

    void OnDestroy()
    {
        foreach (var m in mobs) if (m.art != null) Destroy(m.art.gameObject);
        ClearArrows();
    }
}
