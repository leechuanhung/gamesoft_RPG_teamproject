using System.Collections.Generic;
using UnityEngine;

// The ghost guards of the western camp. They fly: once the seonbi is in the camp they drift toward him in any
// direction (over ledges too), wind up and strike; they block the stele road until all are felled.
public class PrototypeCamp : MonoBehaviour
{
    public PrototypeCombat combat;
    public const int GhostHealth = 4;
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
    }
    public readonly List<Mob> mobs = new List<Mob>();
    private GameObject gate;
    public int Remaining { get { int n = 0; foreach (var m in mobs) if (m.hp > 0) n++; return n; } }

    public void Initialize(PrototypeCombat owner, Sprite sprite)
    {
        combat = owner;
        foreach (float x in new[] { -18f, -26f, -34f })
        {
            var obj = new GameObject("일반 몬스터");
            obj.transform.SetParent(transform);
            obj.transform.localScale = new Vector3(1, 1.4f, 1);
            var art = obj.AddComponent<SpriteRenderer>();
            art.sprite = sprite;
            var visual = (GhostVisual)JinaActorVisual.Attach(art, JinaArt.Ghost, owner);
            mobs.Add(new Mob { art = art, visual = visual, home = x, phase = mobs.Count * 2.1f });
        }
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
    public void ResetCamp()
    {
        foreach (var m in mobs)
        {
            m.hp = GhostHealth; m.windup = -1; m.ready = Time.time + 1;
            m.art.transform.position = new Vector3(m.home, HoverY, 0);
            m.art.enabled = true;
            m.visual.Revive();
            m.art.color = new Color(.65f,.35f,.22f);
        }
        if (gate != null) gate.SetActive(true);
    }
    public void Hit(Mob m, int damage = 1)
    {
        if (m == null || m.hp <= 0) return;
        m.hp = Mathf.Max(0, m.hp - damage);
        // The placeholder stays on while GhostVisual plays the death motion; a felled ghost no longer acts or gets hit.
        if (m.hp == 0) { m.visual.Down(); m.windup = -1; }
        if (Remaining == 0) gate.SetActive(false);
    }
    public void Slash(Bounds bounds)
    {
        foreach (var m in mobs) if (m.hp > 0 && bounds.Intersects(m.art.bounds)) Hit(m, PrototypeCombat.SwordDamage);
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
        bool inCamp = p.x < PrototypeWorld.VillageLeft;
        foreach (var m in mobs)
        {
            if (m.hp <= 0) continue;
            var pos = m.art.transform.position;
            // They make for the seonbi's chest.
            var target = new Vector3(p.x, p.y + .2f, 0);
            var delta = target - pos;
            if (m.windup >= 0)
            {
                // The strike stays where it was aimed, so moving or jumping can evade it.
                if (Time.time >= m.windup)
                {
                    if (new Bounds(m.strike, new Vector3(1.5f, 1.5f, 1)).Intersects(combat.player.GetComponent<SpriteRenderer>().bounds)) combat.HurtPlayer();
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
                step.x = Mathf.Clamp(step.x, -38, -13);
                // A gentle bob keeps them from hanging still.
                step.y = Mathf.Clamp(step.y + Mathf.Sin(Time.time * 2.4f + m.phase) * .25f * Time.deltaTime, HoverY, CeilingY);
                m.art.transform.position = step;
            }
            m.art.color = m.windup >= 0 ? new Color(1,.65f,.1f) : new Color(.65f,.35f,.22f);
        }
    }
}
