using System.Collections.Generic;
using UnityEngine;

// Flying ghosts that guard one stretch of the world. They drift toward the seonbi in any direction (over ledges
// too) once he is in their stretch, wind up and strike.
//   The camp's ghost guards (Initialize) block the stele road until all are felled, and stay felled.
//   The river's water ghosts (InitializeRiver) rise again each time he comes back to the ferry; every one felled
//   counts toward the drowned mother's keepsake (PrototypeCombat.RiverFelled).
public class PrototypeCamp : MonoBehaviour
{
    public PrototypeCombat combat;
    public const int GhostHealth = PrototypeCombat.MonsterMaxHealth, CoinsPerGhost = 4, CoinsPerWaterGhost = 1;
    // Where they fly, the colour of their placeholder and their art, whether they rise again, what each leaves.
    public float minX = -38, maxX = -13;
    public Color body = new Color(.65f,.35f,.22f), tint = Color.white;
    public bool river;
    public int coins = CoinsPerGhost;
    private bool wasInside;
    public const float FlySpeed = 2.2f, SightRange = 8f, StrikeRange = 1.3f, StrikeWindup = .55f, StrikeCooldown = .9f;
    // How high they hover over the ground at rest, and the highest they fly.
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

    public void Initialize(PrototypeCombat owner, Sprite sprite)
    {
        combat = owner;
        Spawn(sprite, "일반 몬스터", new[] { -18f, -26f, -34f });
        gate = new GameObject("비석 길 봉인");
        gate.transform.SetParent(transform);
        gate.transform.position = new Vector3(PrototypeWorld.GateX, 9.5f, 0);
        gate.transform.localScale = new Vector3(.8f, 20, 1);
        gate.layer = 8;
        var image = gate.AddComponent<SpriteRenderer>();
        image.sprite = sprite;
        image.color = new Color(.6f,.3f,.8f,.8f);
        gate.AddComponent<BoxCollider2D>();
        ResetCamp();
    }
    public void InitializeRiver(PrototypeCombat owner, Sprite sprite)
    {
        combat = owner; river = true; coins = CoinsPerWaterGhost;
        minX = PrototypeWorld.RiverLeft - 1; maxX = PrototypeWorld.RiverRight + 1;
        body = new Color(.25f,.4f,.55f); tint = new Color(.62f,.82f,1f);
        Spawn(sprite, "물귀신", new[] { 84f, 90f, 96f });
        ResetCamp();
    }
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
    // Their stretch: the camp west of the village, or the ferry map for the river's ghosts.
    public bool Inside(Vector3 p) => river ? p.x >= PrototypeWorld.FerryStart && p.x < PrototypeWorld.FerryEnd : p.x < PrototypeWorld.VillageLeft;
    // The river's felled ghosts rise again (from where they first hovered) whenever he comes back.
    public void Rise()
    {
        foreach (var m in mobs)
            if (m.hp <= 0) Revive(m);
    }
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
    public void ResetCamp()
    {
        foreach (var m in mobs) Revive(m);
        wasInside = false;
        if (gate != null) gate.SetActive(true);
    }
    public void Hit(Mob m, int damage = PrototypeCombat.ArrowDamage)
    {
        if (m == null || m.hp <= 0) return;
        m.hp = Mathf.Max(0, m.hp - PrototypeCombat.CalcDamage(damage, PrototypeCombat.MonsterDefense));
        if (m.hp == 0)
        {
            m.visual.Down(); m.windup = -1;
            m.vanishAt = Time.time + 1.5f;
            if (combat != null) { combat.FindCoins(coins); if (river) combat.FellWaterGhost(); }
        }
        if (Remaining == 0 && gate != null) gate.SetActive(false);
    }
    public void Slash(Bounds bounds)
    {
        foreach (var m in mobs) if (m.hp > 0 && bounds.Intersects(m.art.bounds)) Hit(m, PrototypeCombat.SwordDamage);
    }

        // 몬스터 머리 위 체력바
        void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused || Camera.main == null) return;
        var oldMatrix = GUI.matrix;
        var oldColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;
        foreach (var m in mobs)
        {
            // 쓰러졌거나 사라진 몬스터는 건너뜀
            if (m.hp <= 0 || !m.art.gameObject.activeSelf) continue;
            Vector3 head = new Vector3(m.art.transform.position.x, m.art.bounds.max.y + .3f, 0);
            Vector3 screen = Camera.main.WorldToScreenPoint(head);
            // 화면 밖에 있는 몬스터는 그리지 않음
            if (screen.z < 0 || screen.x < -50 || screen.x > Screen.width + 50) continue;
            float width = 40, height = 6;
            float x = screen.x - width / 2;
            float y = Screen.height - screen.y - height;
            float ratio = (float)m.hp / GhostHealth;
            // 검은 테두리
            GUI.color = new Color(0, 0, 0, .7f);
            GUI.DrawTexture(new Rect(x - 1, y - 1, width + 2, height + 2), Texture2D.whiteTexture);
            // 남은 체력만큼 빨간색으로 채우기
            GUI.color = new Color(.9f, .2f, .2f);
            GUI.DrawTexture(new Rect(x, y, width * ratio, height), Texture2D.whiteTexture);
            // 막대 위에 체력 숫자 (검은 그림자 + 흰 글씨)
            var numStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = 11, fontStyle = FontStyle.Bold };
            var numRect = new Rect(x - 20, y - 16, width + 40, 14);
            string numText = m.hp + " / " + GhostHealth;
            GUI.color = Color.black;
            GUI.Label(new Rect(numRect.x + 1, numRect.y + 1, numRect.width, numRect.height), numText, numStyle);
            GUI.color = Color.white;
            GUI.Label(numRect, numText, numStyle);
        }
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }

    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        if (combat.Paused)
        {
            foreach (var m in mobs) { m.windup = -1; m.ready = Time.time + .5f; }
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
            // They make for the seonbi's chest.
            var target = new Vector3(p.x, p.y + .2f, 0);
            var delta = target - pos;
            if (m.windup >= 0)
            {
                // The strike stays where it was aimed, so moving or jumping can evade it.
                if (Time.time >= m.windup)
                {
                    if (new Bounds(m.strike, new Vector3(1.5f, 1.5f, 1)).Intersects(combat.player.GetComponent<SpriteRenderer>().bounds)) combat.HurtPlayer(PrototypeCombat.MonsterAttack);
                    m.windup = -1; m.ready = Time.time + StrikeCooldown;
                }
            }
            else if (inCamp && delta.magnitude < StrikeRange && Time.time >= m.ready)
            {
                m.strike = (Vector2)pos + (Vector2)delta.normalized * .7f;
                m.windup = Time.time + StrikeWindup;
            }
            else
            {
                // Chase within sight, otherwise drift home; the camp's walls bound them.
                var goal = inCamp && delta.magnitude < SightRange ? target : new Vector3(m.home, HoverY, 0);
                var step = Vector3.MoveTowards(pos, goal, FlySpeed * Time.deltaTime * (goal == target ? 1 : .5f));
                if (goal == target && delta.magnitude < StrikeRange * .8f) step = pos;
                step.x = Mathf.Clamp(step.x, minX, maxX);
                // A gentle bob keeps them from hanging still.
                step.y = Mathf.Clamp(step.y + Mathf.Sin(Time.time * 2.4f + m.phase) * .25f * Time.deltaTime, HoverY, CeilingY);
                m.art.transform.position = step;
            }
            m.art.color = m.windup >= 0 ? new Color(1,.65f,.1f) : body;
        }
    }
}
