using System.Collections.Generic;
using UnityEngine;

// The great crocks standing about the back hill. They stop what the Kongjwi demon throws; three of her shots break
// one, and so do two blows of his (sword or arrow). A broken crock spills a dish of honey that hops out onto the
// ground; touching it heals him at once (it is not an inventory item, and it waits if he is already whole).
public partial class PrototypeKongBoss
{
    public const int CrockShots = 3, CrockBlows = 2, HoneyHeal = 20;
    class Crock
    {
        public SpriteRenderer art; public int shots, blows;
        public bool Broken => shots >= CrockShots || blows >= CrockBlows;
    }
    class Honey { public SpriteRenderer art; public float vy, floor; }
    readonly List<Crock> crocks = new List<Crock>();
    readonly List<Honey> honey = new List<Honey>();
    public int CrocksStanding => crocks.FindAll(c => !c.Broken).Count;
    public int HoneyCount => honey.Count;
    public Vector3 HoneyAt(int i) => honey[i].art.transform.position;

    void BuildCrocks()
    {
        foreach (var art in FindObjectsOfType<SpriteRenderer>())
            if (art.gameObject.name == "뒷산 독") crocks.Add(new Crock { art = art });
        crocks.Sort((a, b) => a.art.transform.position.x.CompareTo(b.art.transform.position.x));
    }
    void ResetCrocks()
    {
        foreach (var c in crocks) { c.shots = c.blows = 0; c.art.sprite = JinaArt.Prop("crock"); }
        foreach (var h in honey) if (h.art != null) Destroy(h.art.gameObject);
        honey.Clear();
    }

    // One of her shots meeting a crock: it takes the shot.
    bool ShotCrock(Bounds shot)
    {
        foreach (var c in crocks)
        {
            if (c.Broken || !c.art.bounds.Intersects(shot)) continue;
            c.shots++; Struck(c, false);
            return true;
        }
        return false;
    }
    // His sword (PrototypeCombat): every standing crock in the cut takes a blow. True if one did.
    public bool BlowCrocks(Bounds blow)
    {
        bool any = false;
        foreach (var c in crocks)
            if (!c.Broken && c.art.bounds.Intersects(blow)) { c.blows++; Struck(c, true); any = true; }
        return any;
    }
    public void BlowCrock(int index) { var c = crocks[index]; if (!c.Broken) { c.blows++; Struck(c, true); } }
    // How far an arrow flies before it strikes a standing crock (index of that crock), or infinity.
    public float CrockArrowDistance(Vector2 origin, Vector2 direction, float step, out int index)
    {
        index = -1; float best = float.PositiveInfinity;
        for (int i = 0; i < crocks.Count; i++)
        {
            if (crocks[i].Broken) continue;
            float d = PrototypeCombat.ArrowHitDistance(crocks[i].art.bounds, origin, direction, step);
            if (d < best) { best = d; index = i; }
        }
        return best;
    }
    // Used by the checks: a crock taking `times` of her shots.
    public void StrikeCrock(int index, int times) { for (int i = 0; i < times && !crocks[index].Broken; i++) { crocks[index].shots++; Struck(crocks[index], false); } }

    // Chips fly; cracked after the first hit, in pieces once broken, and then the honey hops out toward him.
    void Struck(Crock c, bool his)
    {
        var at = c.art.bounds.center;
        JinaFx.Dots(at, 6, new Color(.55f, .34f, .22f), 2.5f, .4f, 0, 12);
        if (his) PrototypeView.Shake(.03f, .1f);
        if (!c.Broken) { c.art.sprite = JinaArt.Prop("crock_cracked"); return; }
        c.art.sprite = JinaArt.Prop("crock_broken");
        JinaFx.Dots(at, 14, new Color(.47f, .27f, .18f), 4, .6f, 0, 14);
        var dish = new GameObject("꿀 종지").AddComponent<SpriteRenderer>();
        dish.sprite = JinaArt.Prop("kong_honey"); dish.sortingOrder = 5;
        float side = PlayerX >= at.x ? 1 : -1, floor = c.art.transform.position.y;
        dish.transform.position = new Vector3(at.x + side * .75f, floor + .5f, 0);
        honey.Add(new Honey { art = dish, vy = 5, floor = floor });
        NoticeAt = Time.time; notice = "깨진 독에서 꿀 종지가 굴러 나왔습니다. 닿으면 기운이 납니다.";
    }

    // The honey hops and settles; touching it heals him, unless he is whole (then it waits for him).
    void StepHoney(float dt)
    {
        if (honey.Count == 0) return;
        var him = PlayerBody;
        for (int i = honey.Count - 1; i >= 0; i--)
        {
            var h = honey[i]; var p = h.art.transform.position;
            if (h.vy != 0 || p.y > h.floor) { h.vy -= 20 * dt; p.y = Mathf.Max(h.floor, p.y + h.vy * dt); if (p.y <= h.floor) h.vy = 0; }
            h.art.transform.position = p;
            if (!h.art.bounds.Intersects(him) || combat.PlayerHealth >= PrototypeCombat.MaxPlayerHealth) continue;
            combat.RestoreHealth(HoneyHeal);
            NoticeAt = Time.time; notice = "꿀 한 종지를 들이켰습니다. 체력 +" + HoneyHeal;
            Destroy(h.art.gameObject); honey.RemoveAt(i);
        }
    }
}
