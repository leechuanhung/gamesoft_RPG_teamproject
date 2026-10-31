using System.Collections.Generic;
using UnityEngine;

public class PrototypeCamp : MonoBehaviour
{
    public PrototypeCombat combat;
    public class Mob
    {
        public SpriteRenderer art;
        public int hp = 3;
        public float home, ready, windup = -1;
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
            JinaActorVisual.Attach(art, JinaArt.Ghost, owner);
            mobs.Add(new Mob { art = art, home = x });
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
            m.hp = 3; m.windup = -1; m.ready = Time.time + 1;
            m.art.transform.position = new Vector3(m.home, .2f, 0);
            m.art.enabled = true;
            m.art.color = new Color(.65f,.35f,.22f);
        }
        if (gate != null) gate.SetActive(true);
    }
    public void Hit(Mob m)
    {
        if (m == null || m.hp <= 0) return;
        m.hp--;
        if (m.hp == 0) { m.art.enabled = false; m.windup = -1; }
        if (Remaining == 0) gate.SetActive(false);
    }
    public void Slash(Bounds bounds)
    {
        foreach (var m in mobs) if (m.hp > 0 && bounds.Intersects(m.art.bounds)) Hit(m);
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
        foreach (var m in mobs)
        {
            if (m.hp <= 0) continue;
            var delta = p - m.art.transform.position;
            if (m.windup >= 0)
            {
                if (Time.time >= m.windup)
                {
                    if (new Bounds(m.strike, new Vector3(1.5f, 1.5f, 1)).Intersects(combat.player.GetComponent<SpriteRenderer>().bounds)) combat.HurtPlayer();
                    m.windup = -1; m.ready = Time.time + 1.2f;
                }
            }
            else if (p.x < PrototypeWorld.VillageLeft && Mathf.Abs(delta.x) < 1.4f && Mathf.Abs(delta.y) < 1.5f && Time.time >= m.ready)
            {
                m.strike = (Vector2)m.art.transform.position + Vector2.right * Mathf.Sign(delta.x) * .7f;
                m.windup = Time.time + .7f;
            }
            else if (p.x < PrototypeWorld.VillageLeft && Mathf.Abs(delta.x) < 6 && Mathf.Abs(delta.x) > 1.2f)
            {
                var pos = m.art.transform.position;
                pos.x = Mathf.Clamp(pos.x + Mathf.Sign(delta.x) * 1.4f * Time.deltaTime, -38, -13);
                m.art.transform.position = pos;
            }
            m.art.color = m.windup >= 0 ? new Color(1,.65f,.1f) : new Color(.65f,.35f,.22f);
        }
    }
}
