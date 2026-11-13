using System.Collections.Generic;
using UnityEngine;

// 세상의 한 구간을 지키는 날아다니는 유령. 선비가 구간에 들어오면 어느 방향으로든(발판 위로도)
// 다가와 준비 동작 뒤 공격한다.
//   들판의 수호 유령(Initialize)은 전부 쓰러질 때까지 비석 길을 막고, 쓰러지면 그대로 남는다.
//   강의 물귀신(InitializeRiver)은 나루터에 올 때마다 되살아나고, 쓰러뜨린 수는 모두
//   어머니의 은비녀에 쌓인다(PrototypeCombat.RiverFelled).
public class PrototypeCamp : MonoBehaviour
{
    public PrototypeCombat combat;
    public const int GhostHealth = PrototypeCombat.MonsterMaxHealth, CoinsPerGhost = 4, CoinsPerWaterGhost = 1;
    // 날아다니는 구역, 사각형과 그림의 색, 되살아나는지, 무엇을 남기는지.
    public float minX = -38, maxX = -13;
    public Color body = new Color(.65f,.35f,.22f), tint = Color.white;
    public bool river;
    public int coins = CoinsPerGhost;
    private bool wasInside;
    public const float FlySpeed = 2.2f, SightRange = 8f, StrikeRange = 1.3f, StrikeWindup = .55f, StrikeCooldown = .9f;
    // 쉴 때 땅 위로 떠 있는 높이와 가장 높이 나는 높이.
    public const float HoverY = .2f, CeilingY = 4.5f;
    public class Mob
    {
        public SpriteRenderer art;
        public GhostVisual visual;
        public int hp = GhostHealth;
        public float home, ready, windup = -1, phase;
        public Vector2 strike;
        public float vanishAt = -1;
    }
    public readonly List<Mob> mobs = new List<Mob>();
    private GameObject gate;
    public int Remaining { get { int n = 0; foreach (var m in mobs) if (m.hp > 0) n++; return n; } }

    // 들판의 수호 유령들을 만든다.
    public void Initialize(PrototypeCombat owner, Sprite sprite)
    {
        combat = owner;
        Spawn(sprite, "일반 몬스터", new[] { -18f, -26f, -34f });
        gate = new GameObject("비석 길 봉인");
        gate.transform.SetParent(transform);
        gate.transform.position = new Vector3(WorldBuilder.GateX, 9.5f, 0);
        gate.transform.localScale = new Vector3(.8f, 20, 1);
        gate.layer = 8;
        var image = gate.AddComponent<SpriteRenderer>();
        image.sprite = sprite;
        image.color = new Color(.6f,.3f,.8f,.8f);
        image.enabled = false;
        gate.AddComponent<BoxCollider2D>();
        // 보이는 것: 줄에 매단 부적과 빛의 장막, 벽 높이만큼.
        var veil = new GameObject("봉인 장막").AddComponent<SpriteRenderer>();
        veil.transform.SetParent(gate.transform, false);
        veil.transform.localScale = new Vector3(1 / gate.transform.localScale.x, 1 / gate.transform.localScale.y, 1);
        veil.sprite = JinaArt.Fx("seal");
        veil.drawMode = SpriteDrawMode.Tiled;
        veil.size = new Vector2(1.67f, 20);
        veil.color = new Color(1, 1, 1, .9f);
        veil.sortingOrder = 3;
        ResetCamp();
    }
    // 나루터의 물귀신들을 만든다.
    public void InitializeRiver(PrototypeCombat owner, Sprite sprite)
    {
        combat = owner; river = true; coins = CoinsPerWaterGhost;
        minX = WorldBuilder.RiverLeft - 1; maxX = WorldBuilder.RiverRight + 1;
        body = new Color(.25f,.4f,.55f); tint = new Color(.62f,.82f,1f);
        Spawn(sprite, "물귀신", new[] { 84f, 90f, 96f });
        ResetCamp();
    }
    // `homes` 자리마다 유령을 하나씩 만든다.
    void Spawn(Sprite sprite, string label, float[] homes)
    {
        foreach (float x in homes)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(transform);
            obj.transform.localScale = new Vector3(1, 1.4f, 1);
            var art = obj.AddComponent<SpriteRenderer>();
            art.sprite = sprite;
            var visual = (GhostVisual)JinaActorVisual.Attach(art, JinaArt.Ghost, combat);
            visual.tint = tint;
            mobs.Add(new Mob { art = art, visual = visual, home = x, phase = mobs.Count * 2.1f });
        }
    }
    // 구역: 마을 서쪽 들판, 또는 물귀신이면 나루터 맵.
    public bool Inside(Vector3 p) => river ? p.x >= WorldBuilder.FerryStart && p.x < WorldBuilder.FerryEnd : p.x < WorldBuilder.VillageLeft;
    // 쓰러진 물귀신은 선비가 돌아올 때마다(처음 떠 있던 자리에서) 되살아난다.
    public void Rise()
    {
        foreach (var m in mobs)
            if (m.hp <= 0) Revive(m);
    }
    // 쓰러진 유령을 처음 자리에서 되살린다.
    void Revive(Mob m)
    {
        m.art.gameObject.SetActive(true);
        m.vanishAt = -1;
        m.hp = GhostHealth; m.windup = -1; m.ready = Time.time + 1;
        m.art.transform.position = new Vector3(m.home, HoverY, 0);
        m.art.enabled = true;
        m.visual.Revive();
        m.art.color = body;
    }
    // 재시작: 모든 유령을 처음 상태로.
    public void ResetCamp()
    {
        foreach (var m in mobs) Revive(m);
        wasInside = false;
        if (gate != null) gate.SetActive(true);
    }
    // 유령 하나에 피해를 준다. 체력이 다하면 쓰러지고 동전·넋을 남긴다.
    public void Hit(Mob m, int damage = PrototypeCombat.ArrowDamage)
    {
        if (m == null || m.hp <= 0) return;
        int before = m.hp;
        m.hp -= PrototypeCombat.CalcDamage(damage, PrototypeCombat.MonsterDefense);
        var at = m.art.transform.position;
        int away = combat != null && combat.player.transform.position.x < at.x ? 1 : -1;
        JinaFx.Hit(at + Vector3.up * .1f, away, river ? new Color(.74f, .92f, 1) : new Color(1, .92f, .72f));
        JinaFx.Number(at + new Vector3(Random.Range(-.2f, .2f), .7f, 0), before - m.hp, Color.white);
        if (m.hp > 0)
        {
            // 움찔 물러나 한 박자 늦어진다.
            m.visual.Hurt();
            m.art.transform.position = at + Vector3.right * away * .24f;
            m.windup = -1; m.visual.Winding = false; m.ready = Mathf.Max(m.ready, Time.time + .35f);
        }
        if (m.hp <= 0)
        {
            m.visual.Down(); m.windup = -1; m.visual.Winding = false;
            m.vanishAt = Time.time + 1.5f;
            // 넋이 빠져나오고, 갖고 있던 동전이 반짝인다.
            JinaFx.Rise(at, 12, river ? new Color(.7f, .9f, 1) : new Color(.86f, .72f, 1), .4f, 2.2f, 1.1f);
            JinaFx.Dots(at, 7, new Color(1, .84f, .4f), 2.6f, .55f, 0, 6);
            JinaFx.Stop(.08f);
            if (combat != null) { combat.FindCoins(coins); if (river) combat.FellWaterGhost(); }
        }
        if (Remaining == 0 && gate != null) gate.SetActive(false);
    }
    // Whether the blow landed on anything.
    // damage를 넘기지 않으면 환도 데미지(10), 스킬은 Slash(area, 20)처럼 원하는 데미지를 넘김
    public bool Slash(Bounds bounds, int damage = PrototypeCombat.SwordDamage)
    {
        bool landed = false;
        foreach (var m in mobs) if (m.hp > 0 && bounds.Intersects(m.art.bounds)) { Hit(m, damage); landed = true; }
        return landed;
    }

    // 몬스터 머리 위 체력바 (숫자는 막대 안쪽)
    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused || Camera.main == null) return;
        var oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        foreach (var m in mobs)
        {
            // 쓰러졌거나 사라진 몬스터는 건너뜀
            if (m.hp <= 0 || !m.art.gameObject.activeSelf) continue;
            Vector3 head = new Vector3(m.art.transform.position.x, m.art.bounds.max.y + .3f, 0);
            Vector3 screen = Camera.main.WorldToScreenPoint(head);
            // 화면 밖에 있는 몬스터는 그리지 않음
            if (screen.z < 0 || screen.x < -80 || screen.x > Screen.width + 80) continue;
            float width = 100, height = 20;
            var r = new Rect(screen.x - width / 2, Screen.height - screen.y - height, width, height);
            PrototypeCombat.DrawBar(r, (float)m.hp / GhostHealth, new Color(.9f, .2f, .2f), m.hp + " / " + GhostHealth, 17);
        }
        GUI.matrix = oldMatrix;
    }

    // 유령마다 추격, 준비 동작, 공격을 돌린다.
    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        if (combat.Paused)
        {
            foreach (var m in mobs) { m.windup = -1; m.ready = Time.time + .5f; m.visual.Winding = false; }
            return;
        }
        var p = combat.player.transform.position;
        bool inCamp = Inside(p);
        if (river && inCamp && !wasInside) Rise();
        wasInside = inCamp;
        foreach (var m in mobs)
        {
            if (m.hp <= 0)
            {
                if (m.vanishAt >= 0 && Time.time >= m.vanishAt)
                {
                    m.art.gameObject.SetActive(false);
                    m.vanishAt = -1;
                }
                continue;
            }
            var pos = m.art.transform.position;
            // 선비의 가슴을 노린다.
            var target = new Vector3(p.x, p.y + .2f, 0);
            var delta = target - pos;
            if (m.windup >= 0)
            {
                // 공격은 노린 자리에 그대로 떨어진다. 움직이거나 뛰면 피할 수 있다.
                if (Time.time >= m.windup)
                {
                    if (new Bounds(m.strike, new Vector3(1.5f, 1.5f, 1)).Intersects(combat.player.GetComponent<SpriteRenderer>().bounds)) combat.HurtPlayer(PrototypeCombat.MonsterAttack);
                    m.windup = -1; m.ready = Time.time + StrikeCooldown;
                    m.visual.Strike();
                }
            }
            else if (inCamp && delta.magnitude < StrikeRange && Time.time >= m.ready)
            {
                m.strike = (Vector2)pos + (Vector2)delta.normalized * .7f;
                m.windup = Time.time + StrikeWindup;
                m.visual.Winding = true; m.visual.AimX = p.x;
            }
            else
            {
                // 보이면 쫓고, 아니면 제자리로 떠간다. 들판의 벽이 경계다.
                var goal = inCamp && delta.magnitude < SightRange ? target : new Vector3(m.home, HoverY, 0);
                var step = Vector3.MoveTowards(pos, goal, FlySpeed * Time.deltaTime * (goal == target ? 1 : .5f));
                if (goal == target && delta.magnitude < StrikeRange * .8f) step = pos;
                step.x = Mathf.Clamp(step.x, minX, maxX);
                // 살짝 오르내려 멈춰 있지 않게 한다.
                step.y = Mathf.Clamp(step.y + Mathf.Sin(Time.time * 2.4f + m.phase) * .25f * Time.deltaTime, HoverY, CeilingY);
                m.art.transform.position = step;
            }
            m.art.color = m.windup >= 0 ? new Color(1,.65f,.1f) : body;
        }
    }
}