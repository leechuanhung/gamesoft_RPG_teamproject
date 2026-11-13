using UnityEngine;

// What makes each map feel lived in, none of it touching play: clouds drifting behind the mountains, motes in the air
// (fireflies at the stele, ash over the field, leaves in the forest, mist and dragonflies by the river, seeds in the bean
// fields, wind on the pass), smoke from the tavern, the river's shimmer, and a soft vignette over everything.
public class PrototypeAmbience : MonoBehaviour
{
    public PrototypeCombat combat;
    enum Kind { Firefly, Ash, Petal, Leaf, Mist, Seed, Wind }
    struct Air { public Kind kind; public Color a, b; public int count; public Color cloud; public int clouds; }
    // In the order of PrototypeMaps.All.
    static readonly Air[] Maps =
    {
        new Air { kind = Kind.Firefly, a = new Color(.72f, 1, .78f), b = new Color(.5f, .8f, 1), count = 16, cloud = new Color(.32f, .34f, .5f, .55f), clouds = 3 },
        new Air { kind = Kind.Ash, a = new Color(1, .62f, .3f), b = new Color(.45f, .42f, .42f), count = 26, cloud = new Color(.42f, .2f, .22f, .75f), clouds = 4 },
        new Air { kind = Kind.Petal, a = new Color(1, .86f, .9f), b = new Color(1, .96f, .9f), count = 12, cloud = new Color(1, 1, 1, .9f), clouds = 5 },
        new Air { kind = Kind.Leaf, a = new Color(.56f, .72f, .36f), b = new Color(.8f, .72f, .36f), count = 22, cloud = new Color(.2f, .36f, .34f, .4f), clouds = 2 },
        new Air { kind = Kind.Mist, a = new Color(.9f, .94f, .96f), b = new Color(.76f, .86f, .9f), count = 18, cloud = new Color(.94f, .96f, .98f, .85f), clouds = 5 },
        new Air { kind = Kind.Seed, a = new Color(1, .96f, .78f), b = new Color(1, .86f, .56f), count = 18, cloud = new Color(1, .96f, .86f, .92f), clouds = 5 },
        new Air { kind = Kind.Wind, a = new Color(1, .84f, .74f), b = new Color(.9f, .66f, .62f), count = 16, cloud = new Color(.9f, .56f, .5f, .75f), clouds = 5 },
        new Air { kind = Kind.Ash, a = new Color(.62f, .5f, .7f), b = new Color(.36f, .3f, .4f), count = 22, cloud = new Color(.5f, .36f, .46f, .8f), clouds = 4 },
    };
    const float ViewHalfW = 11, ViewHalfH = 5.6f;
    const int MaxMotes = 30, MaxClouds = 5, SmokeBits = 10;

    class Mote { public SpriteRenderer art; public float x, y, phase, speed; }
    class Cloud { public SpriteRenderer art; public float offset, height, speed; }
    readonly Mote[] motes = new Mote[MaxMotes];
    readonly Cloud[] clouds = new Cloud[MaxClouds];
    readonly SpriteRenderer[] smoke = new SpriteRenderer[SmokeBits];
    readonly SpriteRenderer[] shimmer = new SpriteRenderer[14];
    int shownMap = -1;

    void Start()
    {
        for (int i = 0; i < MaxMotes; i++)
        {
            motes[i] = new Mote { art = Dot("공기 " + i, 2), phase = Random.value * 100, x = Random.value, y = Random.value, speed = Random.Range(.7f, 1.3f) };
            motes[i].art.enabled = false;
        }
        for (int i = 0; i < MaxClouds; i++)
        {
            var art = new GameObject("구름 " + i).AddComponent<SpriteRenderer>();
            art.transform.SetParent(transform);
            art.sprite = JinaArt.Prop("cloud_" + i % 3); art.sortingOrder = -20;
            clouds[i] = new Cloud { art = art, offset = Random.value * 60, height = Random.Range(3.6f, 6.6f), speed = Random.Range(.12f, .3f) };
            art.enabled = false;
        }
        for (int i = 0; i < SmokeBits; i++) { smoke[i] = Dot("주막 연기 " + i, -7); smoke[i].enabled = false; }
        for (int i = 0; i < shimmer.Length; i++) { shimmer[i] = Dot("강 물빛 " + i, 3); shimmer[i].enabled = false; }
    }
    SpriteRenderer Dot(string label, int order)
    {
        var art = new GameObject(label).AddComponent<SpriteRenderer>();
        art.transform.SetParent(transform);
        art.sprite = JinaArt.Dot(); art.sortingOrder = order;
        art.transform.localScale = Vector3.one * .5f;
        return art;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || combat == null || combat.Maps == null) return;
        bool away = combat.Stay != null && combat.Stay.InRoom;
        int map = combat.Maps.Current;
        var air = Maps[Mathf.Clamp(map, 0, Maps.Length - 1)];
        float cx = cam.transform.position.x, t = Time.time;

        for (int i = 0; i < MaxMotes; i++)
        {
            var m = motes[i];
            m.art.enabled = !away && i < air.count;
            if (!m.art.enabled) continue;
            StepMote(m, air, i, t, cx);
        }
        var bounds = PrototypeMaps.All[map];
        float span = bounds.right - bounds.left + 20;
        for (int i = 0; i < MaxClouds; i++)
        {
            var c = clouds[i];
            c.art.enabled = !away && i < air.clouds;
            if (!c.art.enabled) continue;
            // Across the map and round again, slower than anything else on screen.
            float x = bounds.left - 10 + Mathf.Repeat(c.offset * 13 + i * span / MaxClouds + t * c.speed, span);
            c.art.transform.position = new Vector3(x, c.height, 0);
            c.art.color = air.cloud;
        }
        StepSmoke(t, !away && map == 2);
        StepRiver(t, !away && map == 4, cx);
    }

    void StepMote(Mote m, Air air, int i, float t, float cx)
    {
        float phase = m.phase;
        float wrapX(float v) => cx - ViewHalfW + Mathf.Repeat(v, ViewHalfW * 2);
        float wrapY(float v) => -.2f + Mathf.Repeat(v, ViewHalfH * 1.6f);
        Vector3 p; float alpha = 1; float size = .5f;
        switch (air.kind)
        {
            case Kind.Firefly:
                p = new Vector3(wrapX(phase * 31 + Mathf.Sin(t * .4f + phase) * 1.5f), .2f + Mathf.Repeat(phase * 7, 4.5f) + Mathf.Sin(t * .7f + phase * 3) * .6f, 0);
                alpha = Mathf.Clamp01(Mathf.Sin(t * 1.3f * m.speed + phase) * 1.5f);
                break;
            case Kind.Ash:
                p = new Vector3(wrapX(phase * 31 + t * .25f * m.speed + Mathf.Sin(t + phase)), wrapY(phase * 17 + t * .55f * m.speed), 0);
                alpha = i % 3 == 0 ? .95f : .6f; size = i % 3 == 0 ? .5f : .4f;
                break;
            case Kind.Petal:
                p = new Vector3(wrapX(phase * 31 + t * .35f * m.speed + Mathf.Sin(t * .8f + phase) * .8f), wrapY(phase * 17 - t * .4f * m.speed), 0);
                alpha = .85f;
                break;
            case Kind.Leaf:
                p = new Vector3(wrapX(phase * 31 - t * .3f * m.speed + Mathf.Sin(t * 1.1f + phase) * 1.1f), wrapY(phase * 17 - t * .6f * m.speed), 0);
                alpha = .9f; size = .6f;
                break;
            case Kind.Mist:
                p = new Vector3(wrapX(phase * 31 + t * .18f * m.speed), -.1f + Mathf.Repeat(phase * 7, 1.6f) + Mathf.Sin(t * .3f + phase) * .15f, 0);
                alpha = .18f + .1f * Mathf.Sin(t * .5f + phase); size = 1.2f + i % 3 * .6f;
                break;
            case Kind.Seed:
                p = new Vector3(wrapX(phase * 31 + t * .3f * m.speed + Mathf.Sin(t * .6f + phase)), .2f + Mathf.Repeat(phase * 11 + t * .1f, 5f) + Mathf.Sin(t * .9f + phase) * .3f, 0);
                alpha = .55f + .3f * Mathf.Sin(t * 2 + phase);
                break;
            default:
                p = new Vector3(wrapX(phase * 31 - t * (2.4f + m.speed)), wrapY(phase * 17) + Mathf.Sin(t * 1.3f + phase) * .2f, 0);
                alpha = .45f; size = .4f;
                break;
        }
        m.art.transform.position = p;
        m.art.transform.localScale = air.kind == Kind.Wind ? new Vector3(size * 4, size * .6f, 1) : air.kind == Kind.Mist ? new Vector3(size * 4, size, 1) : Vector3.one * size;
        var c = i % 2 == 0 ? air.a : air.b; c.a = alpha;
        m.art.color = c;
    }

    // A thin line of smoke from the tavern's roof.
    void StepSmoke(float t, bool show)
    {
        for (int i = 0; i < SmokeBits; i++)
        {
            smoke[i].enabled = show;
            if (!show) continue;
            float life = Mathf.Repeat(t * .22f + i * .1f, 1);
            smoke[i].transform.position = new Vector3(PrototypeWorld.Tavern.x + 2.4f + Mathf.Sin(life * 6 + t) * .1f + life * .5f, 3.0f + life * 2.6f, 0);
            smoke[i].transform.localScale = Vector3.one * (.5f + life * 1.3f);
            smoke[i].color = new Color(.92f, .92f, .9f, .42f * (1 - life));
        }
    }

    // Glints moving on the river's surface.
    void StepRiver(float t, bool show, float cx)
    {
        for (int i = 0; i < shimmer.Length; i++)
        {
            shimmer[i].enabled = show;
            if (!show) continue;
            float x = PrototypeWorld.RiverLeft + Mathf.Repeat(i * 1.9f + t * .24f * (i % 2 == 0 ? 1 : -1), PrototypeWorld.RiverRight - PrototypeWorld.RiverLeft);
            float y = -1.1f - (i % 4) * .5f;
            float pulse = Mathf.Clamp01(Mathf.Sin(t * 1.4f + i * 1.7f) * 1.5f);
            shimmer[i].transform.position = new Vector3(x, y, 0);
            shimmer[i].transform.localScale = new Vector3(1.8f + i % 3, .5f, 1);
            shimmer[i].color = new Color(.84f, .94f, 1, .6f * pulse);
        }
    }

    void OnGUI()
    {
        if (combat == null || (combat.Stay != null && combat.Stay.InRoom)) return;
        GUI.depth = 20;
        PrototypeKoreanUI.Apply();
        var v = JinaUI.Art("vignette");
        if (v == null) return;
        v.wrapMode = TextureWrapMode.Clamp; v.filterMode = FilterMode.Bilinear;
        var saved = GUI.color; GUI.color = new Color(1, 1, 1, .62f);
        GUI.DrawTexture(new Rect(0, 0, JinaUI.W, JinaUI.H), v);
        GUI.color = saved;
    }
}
