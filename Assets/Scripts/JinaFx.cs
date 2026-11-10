using System.Collections.Generic;
using UnityEngine;

// Everything that flies off a blow: crescents, sparks, dust, shock rings, after-images, drifting dots and the numbers
// that pop out of what was hit. Each is a pooled sprite with a short life and no effect on the game. Hit-stop, the
// instant the world holds its breath on a landed blow, lives here too.
public class JinaFx : MonoBehaviour
{
    class Bit
    {
        public SpriteRenderer art;
        public Sprite[] frames;
        public Vector3 velocity;
        public float age, life, gravity, spin, grow = 1, startScale = 1;
        public Color from, to;
        public bool active;
    }
    struct Popup { public Vector3 at; public string text; public Color colour; public float born; public int size; }

    static JinaFx self;
    static JinaFx Self
    {
        get
        {
            if (self == null) self = new GameObject("효과").AddComponent<JinaFx>();
            return self;
        }
    }
    readonly List<Bit> bits = new List<Bit>();
    readonly List<Popup> popups = new List<Popup>();
    const float Normal = 1, Stopped = .02f;
    float stopUntil;

    static Sprite[] arc, spark, dust, ring;
    static Sprite[] Frames(ref Sprite[] cache, string name, int n)
    {
        if (cache != null && cache.Length > 0 && cache[0] != null) return cache;
        cache = new Sprite[n];
        for (int i = 0; i < n; i++) cache[i] = JinaArt.Fx(name + "_" + i);
        return cache;
    }

    Bit Take()
    {
        foreach (var b in bits) if (!b.active) { Prepare(b); return b; }
        var made = new Bit { art = new GameObject("조각").AddComponent<SpriteRenderer>() };
        made.art.transform.SetParent(transform);
        bits.Add(made); Prepare(made);
        return made;
    }
    static void Prepare(Bit b)
    {
        b.active = true; b.age = 0; b.velocity = Vector3.zero; b.gravity = 0; b.spin = 0; b.grow = 1; b.startScale = 1; b.frames = null;
        b.art.enabled = true; b.art.flipX = false; b.art.sortingOrder = 6;
        b.art.transform.rotation = Quaternion.identity; b.art.transform.localScale = Vector3.one;
    }

    void LateUpdate()
    {
        // The hold ends on real time, whatever the game's clock is doing.
        if (stopUntil > 0 && Time.unscaledTime >= stopUntil)
        {
            if (Mathf.Approximately(Time.timeScale, Stopped)) Time.timeScale = Normal;
            stopUntil = 0;
        }
        float dt = Time.deltaTime;
        foreach (var b in bits)
        {
            if (!b.active) continue;
            b.age += dt;
            if (b.age >= b.life) { b.active = false; b.art.enabled = false; continue; }
            float k = b.age / b.life;
            b.velocity.y += b.gravity * dt;
            var t = b.art.transform;
            t.position += b.velocity * dt;
            if (b.spin != 0) t.Rotate(0, 0, b.spin * dt);
            float scale = b.startScale * Mathf.Lerp(1, b.grow, k);
            t.localScale = new Vector3(scale, scale, 1);
            if (b.frames != null) b.art.sprite = b.frames[Mathf.Min(b.frames.Length - 1, (int)(k * b.frames.Length))];
            b.art.color = Color.Lerp(b.from, b.to, k);
        }
    }

    // ---- what the game calls ----

    // The world holds still for a moment. Off in batch checks, whose clocks barely move.
    public static void Stop(float seconds)
    {
        if (Application.isBatchMode || Time.timeScale < .5f) return;
        Time.timeScale = Stopped;
        Self.stopUntil = Time.unscaledTime + seconds;
    }
    public static void Clear()
    {
        if (self == null) return;
        foreach (var b in self.bits) { b.active = false; b.art.enabled = false; }
        self.popups.Clear();
        if (self.stopUntil > 0) { if (Mathf.Approximately(Time.timeScale, Stopped)) Time.timeScale = Normal; self.stopUntil = 0; }
    }

    // The sword's crescent. `angle`: 0 sweeps down in front, positive tips it upward. `size` scales it.
    public static void Arc(Vector3 at, int facing, float angle, float size, Color colour, float life = .16f)
    {
        var b = Self.Take();
        b.frames = Frames(ref arc, "arc", 3); b.life = life;
        b.art.sprite = b.frames[0]; b.art.flipX = facing < 0;
        b.art.transform.position = at;
        b.art.transform.rotation = Quaternion.Euler(0, 0, angle * facing);
        b.startScale = size; b.grow = 1.12f;
        b.from = colour; b.to = new Color(colour.r, colour.g, colour.b, .55f);
        b.art.sortingOrder = 7;
    }
    // The star where a blow lands, with a few dots thrown back the way it came.
    public static void Hit(Vector3 at, int direction, Color colour, float size = 1)
    {
        var b = Self.Take();
        b.frames = Frames(ref spark, "spark", 4); b.life = .2f;
        b.art.sprite = b.frames[0]; b.art.transform.position = at;
        b.startScale = size; b.from = colour; b.to = new Color(colour.r, colour.g, colour.b, .7f);
        b.art.sortingOrder = 8;
        Dots(at, 6, colour, 3.4f, .3f, direction, 9);
    }
    // Dots burst from a point. `direction` biases them to one side (0 for all round); gravity pulls them down.
    public static void Dots(Vector3 at, int count, Color colour, float speed, float life, int direction = 0, float gravity = 0, float dotSize = 1)
    {
        for (int i = 0; i < count; i++)
        {
            var b = Self.Take();
            b.art.sprite = JinaArt.Dot(); b.life = life * Random.Range(.7f, 1.1f);
            b.art.transform.position = at;
            var dir = Random.insideUnitCircle.normalized;
            if (direction != 0) dir.x = Mathf.Abs(dir.x) * direction;
            b.velocity = dir * speed * Random.Range(.5f, 1.1f);
            b.gravity = -gravity;
            b.startScale = dotSize * .5f; b.grow = .5f;
            b.from = colour; b.to = new Color(colour.r, colour.g, colour.b, 0);
            b.art.sortingOrder = 8;
        }
    }
    // Dots rising gently from a point: a heal, a wisp leaving a body, a coin's glitter.
    public static void Rise(Vector3 at, int count, Color colour, float spread, float height, float life)
    {
        for (int i = 0; i < count; i++)
        {
            var b = Self.Take();
            b.art.sprite = JinaArt.Dot(); b.life = life * Random.Range(.7f, 1.2f);
            b.art.transform.position = at + new Vector3(Random.Range(-spread, spread), Random.Range(-.1f, .3f), 0);
            b.velocity = new Vector3(Random.Range(-.1f, .1f), height / life * Random.Range(.7f, 1.2f), 0);
            b.startScale = .5f; b.grow = .6f;
            b.from = colour; b.to = new Color(colour.r, colour.g, colour.b, 0);
        }
    }
    // A puff of dust on the ground; `drift` moves it.
    public static void Dust(Vector3 feet, float drift = 0, float size = 1, Color? tint = null)
    {
        var b = Self.Take();
        b.frames = Frames(ref dust, "dust", 4); b.life = .34f;
        b.art.sprite = b.frames[0]; b.art.transform.position = feet + Vector3.up * .15f * size;
        b.velocity = new Vector3(drift, .25f, 0);
        b.startScale = size; b.grow = 1.25f; b.art.sortingOrder = 6;
        var c = tint ?? new Color(.86f, .8f, .68f, .9f);
        b.from = c; b.to = new Color(c.r, c.g, c.b, .15f);
    }
    // The ring a heavy landing sends along the ground, and the dust and clods with it.
    public static void Shock(Vector3 feet, float size = 1)
    {
        var b = Self.Take();
        b.frames = Frames(ref ring, "ring", 4); b.life = .3f;
        b.art.sprite = b.frames[0]; b.art.transform.position = feet + Vector3.up * .08f;
        b.startScale = size * 1.2f; b.grow = 1.5f;
        b.from = new Color(1, .96f, .84f, .95f); b.to = new Color(1, .9f, .7f, .2f);
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 3; i++)
                Dust(feet + Vector3.right * side * (.4f + i * .5f) * size, side * (1.2f + i * .5f), 1.1f + i * .25f);
        Dots(feet + Vector3.up * .1f, 14, new Color(.5f, .38f, .28f), 5, .5f, 0, 16, 1.2f);
    }
    // The flash before a blow.
    public static void Glint(Vector3 at, Color colour)
    {
        var b = Self.Take();
        b.art.sprite = JinaArt.Fx("glint"); b.life = .28f;
        b.art.transform.position = at; b.spin = 200;
        b.startScale = .4f; b.grow = 3f;
        b.from = colour; b.to = new Color(colour.r, colour.g, colour.b, 0);
        b.art.sortingOrder = 9;
    }
    // A fading copy of a body left where it was: the backstep, the charge.
    public static void AfterImage(SpriteRenderer body, Color colour, float life = .22f)
    {
        if (body == null || body.sprite == null) return;
        var b = Self.Take();
        b.art.sprite = body.sprite; b.art.flipX = body.flipX; b.life = life;
        b.art.transform.position = body.transform.position;
        b.art.transform.rotation = body.transform.rotation;
        b.from = colour; b.to = new Color(colour.r, colour.g, colour.b, 0);
        b.art.sortingOrder = body.sortingOrder - 1;
    }
    // A number jumping out of what was hit.
    public static void Number(Vector3 at, int amount, Color colour, int size = 20)
    {
        Self.popups.Add(new Popup { at = at, text = amount.ToString(), colour = colour, born = Time.time, size = size });
    }
    public static void Word(Vector3 at, string text, Color colour, int size = 20)
    {
        Self.popups.Add(new Popup { at = at, text = text, colour = colour, born = Time.time, size = size });
    }

    const float PopupLength = .62f;
    void OnGUI()
    {
        if (popups.Count == 0) return;
        var cam = Camera.main;
        if (cam == null) return;
        GUI.depth = 5;
        PrototypeKoreanUI.Apply();
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            var p = popups[i];
            float t = (Time.time - p.born) / PopupLength;
            if (t >= 1) { if (Event.current.type == EventType.Repaint) popups.RemoveAt(i); continue; }
            var world = p.at + Vector3.up * (.5f * (1 - (1 - t) * (1 - t)));
            var at = JinaUI.FromWorld(cam, world);
            var c = p.colour; c.a = Mathf.Clamp01((1 - t) * 3);
            // Bigger for an instant, then settling.
            int size = Mathf.RoundToInt(p.size * (t < .15f ? 1 + (1 - t / .15f) * .35f : 1));
            var r = new Rect(at.x - 60, at.y - 20, 120, 40);
            var ink = new Color(.04f, .03f, .04f, c.a);
            foreach (var o in Offsets) JinaUI.Text(new Rect(r.x + o.x, r.y + o.y, r.width, r.height), p.text, size, ink, TextAnchor.MiddleCenter, true);
            JinaUI.Text(r, p.text, size, c, TextAnchor.MiddleCenter, true);
        }
    }
    static readonly Vector2[] Offsets = { new Vector2(-2, 0), new Vector2(2, 0), new Vector2(0, -2), new Vector2(0, 2) };

    void OnDestroy() { if (self == this) self = null; }
}
