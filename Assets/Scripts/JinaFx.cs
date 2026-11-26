using System.Collections.Generic;
using UnityEngine;

// 타격에서 튀어나오는 모든 것: 초승달 칼빛, 불티, 먼지, 충격파, 잔상, 떠다니는 점, 맞은 곳에서 튀는 숫자.
// 모두 풀링된 스프라이트로 수명이 짧고 게임에 영향이 없다. 타격이 들어간 순간 세상이 숨을 멈추는
// 히트스톱도 여기 있다.
public class JinaFx : MonoBehaviour
{
    class Bit
    {
        public SpriteRenderer art;
        public Sprite[] frames;
        public Vector3 velocity;
        public Vector2 stretch;
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
    // 효과 프레임을 한 번만 불러 캐시에 둔다.
    static Sprite[] Frames(ref Sprite[] cache, string name, int n)
    {
        if (cache != null && cache.Length > 0 && cache[0] != null) return cache;
        cache = new Sprite[n];
        for (int i = 0; i < n; i++) cache[i] = JinaArt.Fx(name + "_" + i);
        return cache;
    }

    // 풀에서 쉬고 있는 효과 하나를 꺼낸다(없으면 새로 만든다).
    Bit Take()
    {
        foreach (var b in bits) if (!b.active) { Prepare(b); return b; }
        var made = new Bit { art = new GameObject("조각").AddComponent<SpriteRenderer>() };
        made.art.transform.SetParent(transform);
        bits.Add(made); Prepare(made);
        return made;
    }
    // 꺼낸 효과를 기본값으로 되돌린다.
    static void Prepare(Bit b)
    {
        b.active = true; b.age = 0; b.velocity = Vector3.zero; b.gravity = 0; b.spin = 0; b.grow = 1; b.startScale = 1; b.frames = null;
        b.stretch = Vector2.one;
        b.art.enabled = true; b.art.flipX = false; b.art.sortingOrder = 6;
        b.art.transform.rotation = Quaternion.identity; b.art.transform.localScale = Vector3.one;
    }

    // 살아 있는 효과를 움직이고 수명이 다한 것을 거둔다.
    void LateUpdate()
    {
        // 멈춤은 게임 시계와 상관없이 실제 시간으로 끝난다.
        if (stopUntil > 0 && Time.unscaledTime >= stopUntil)
        {
            if (Mathf.Approximately(Time.timeScale, Stopped)) Time.timeScale = Normal;
            stopUntil = 0;
        }
        float dt = Time.deltaTime;
        foreach (var b in bits)
        {
            if (!b.active) continue;
            float previousAge = b.age;
            b.age += dt;
            if (b.age < 0) continue;
            if (b.age >= b.life) { b.active = false; b.art.enabled = false; continue; }
            if (previousAge < 0) b.art.enabled = true;
            float elapsed = previousAge < 0 ? b.age : dt;
            float k = b.age / b.life;
            b.velocity.y += b.gravity * elapsed;
            var t = b.art.transform;
            t.position += b.velocity * elapsed;
            if (b.spin != 0) t.Rotate(0, 0, b.spin * elapsed);
            float scale = b.startScale * Mathf.Lerp(1, b.grow, k);
            t.localScale = new Vector3(scale * b.stretch.x, scale * b.stretch.y, 1);
            if (b.frames != null) b.art.sprite = b.frames[Mathf.Min(b.frames.Length - 1, (int)(k * b.frames.Length))];
            b.art.color = Color.Lerp(b.from, b.to, k);
        }
    }

    // 전투 난수에 영향을 주지 않도록 정화 입자만의 난수를 사용한다.
    static readonly System.Random purificationRandom = new System.Random();

    public static void Purification(Vector3 feet)
    {
        float Range(float min, float max) => Mathf.Lerp(min, max, (float)purificationRandom.NextDouble());
        var smokeFrames = Frames(ref dust, "dust", 4);
        var emberFrames = Frames(ref spark, "spark", 4);
        for (int i = 0; i < 30; i++)
        {
            bool smoke = i < 12;
            var b = Self.Take();
            b.frames = smoke ? smokeFrames : emberFrames;
            b.art.sprite = b.frames[0];
            b.age = -i * .021f - (smoke ? 0 : .29f);
            b.life = smoke ? Range(.58f, .88f) : Range(.62f, .92f);
            b.startScale = smoke ? Range(.8f, 1.35f) : Range(.14f, .27f);
            b.grow = smoke ? 1.65f : .55f;
            float side = Range(-1, 1);
            b.art.transform.position = feet + new Vector3(side * .65f, Range(.65f, 1.95f), 0);
            b.velocity = new Vector3(side * .65f + Range(-.3f, .3f), Range(.65f, 1.65f), 0);
            b.gravity = smoke ? .45f : .75f;
            b.art.flipX = side < 0;
            b.art.sortingOrder = smoke ? 5 : 6;
            b.from = smoke
                ? Color.Lerp(new Color(.055f, .045f, .065f, .97f), new Color(.19f, .15f, .21f, .85f), Range(0, 1))
                : Color.Lerp(new Color(1, .62f, .15f), new Color(1, .96f, .65f), Range(0, 1));
            b.to = new Color(b.from.r, b.from.g, b.from.b, 0);
            b.art.color = b.from;
            b.art.transform.localScale = new Vector3(b.startScale, b.startScale, 1);
            b.art.enabled = b.age >= 0;
        }
    }

    // ---- 게임이 부르는 함수 ----

    // 세상이 잠깐 멈춘다. 시계가 거의 안 가는 배치 검사에서는 끈다.
    public static void Stop(float seconds)
    {
        if (Application.isBatchMode || Time.timeScale < .5f) return;
        Time.timeScale = Stopped;
        Self.stopUntil = Time.unscaledTime + seconds;
    }
    // 모든 효과를 지운다(재시작).
    public static void Clear()
    {
        if (self == null) return;
        foreach (var b in self.bits) { b.active = false; b.art.enabled = false; }
        self.popups.Clear();
        if (self.stopUntil > 0) { if (Mathf.Approximately(Time.timeScale, Stopped)) Time.timeScale = Normal; self.stopUntil = 0; }
    }

    // 칼의 초승달. `angle`: 0이면 앞쪽 아래로 휩쓸고, 양수면 위로 기운다. `size`는 크기 배율.
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
    // 타격이 꽂힌 곳의 별, 날아온 쪽으로 튀는 점 몇 개.
    public static void Hit(Vector3 at, int direction, Color colour, float size = 1)
    {
        GameAudio.Play(GameAudio.Sfx.Hit);   // 타격 별이 뜨는 곳이면 맞는 소리도 난다
        var b = Self.Take();
        b.frames = Frames(ref spark, "spark", 4); b.life = .2f;
        b.art.sprite = b.frames[0]; b.art.transform.position = at;
        b.startScale = size; b.from = colour; b.to = new Color(colour.r, colour.g, colour.b, .7f);
        b.art.sortingOrder = 8;
        Dots(at, 6, colour, 3.4f, .3f, direction, 9);
    }
    // 한 점에서 점들이 터진다. `direction`은 한쪽으로 치우침(0이면 사방), 중력으로 떨어진다.
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
    // 한 점에서 천천히 떠오르는 점들: 회복, 몸에서 빠져나가는 기운, 동전의 반짝임.
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
    // 땅의 먼지 한 줌. `drift`만큼 흘러간다.
    public static void Dust(Vector3 feet, float drift = 0, float size = 1, Color? tint = null)
    {
        DustPuff(feet, drift, size, tint ?? new Color(.86f, .8f, .68f, .9f));
    }

    static Bit DustPuff(Vector3 feet, float drift, float size, Color tint)
    {
        var b = Self.Take();
        b.frames = Frames(ref dust, "dust", 4); b.life = .34f;
        b.art.sprite = b.frames[0]; b.art.transform.position = feet + Vector3.up * .15f * size;
        b.velocity = new Vector3(drift, .25f, 0);
        b.startScale = size; b.grow = 1.25f; b.art.sortingOrder = 6;
        b.from = tint; b.to = new Color(tint.r, tint.g, tint.b, .15f);
        return b;
    }

    // 바위 뒤에 낮게 퍼지는 흙먼지. 위치에 따라 크기와 속도를 조금씩 바꾼다.
    public static void RollingDust(Vector3 contact, float behind, float radius)
    {
        float variation = Mathf.Repeat(contact.x * 2.73f, 1);
        float size = radius * Mathf.Lerp(1.05f, 1.55f, variation);
        var tint = Color.Lerp(new Color(.58f, .39f, .25f, .68f),
            new Color(.83f, .61f, .39f, .8f), variation);
        var puff = DustPuff(contact, behind * Mathf.Lerp(.55f, 1.3f, variation), size, tint);
        puff.velocity.y = Mathf.Lerp(.65f, 1.05f, variation);
        puff.gravity = -3.1f;
        puff.life = Mathf.Lerp(.43f, .59f, variation);
        puff.grow = 1.65f;
        puff.to.a = 0;
        puff.art.flipX = behind < 0;
        puff.art.color = tint;
        puff.art.transform.localScale = new Vector3(size, size, 1);
    }

    // 수면 위에서 양옆으로 퍼지다가 사라지는 작은 물결.
    public static void RiverRipple(float x, float y, float drift, float size, float opacity, float life,
        float length = 1, float tilt = 0)
    {
        const float growth = 1.55f;
        var frames = Frames(ref ring, "ring", 4);
        var extents = frames[0].bounds.extents;
        float halfWidth = (extents.x * length + extents.y * 1.5f * Mathf.Abs(Mathf.Sin(tilt * Mathf.Deg2Rad))) * size * growth;
        float endX = x + drift * life;
        if (Mathf.Min(x, endX) - halfWidth < WorldBuilder.RiverLeft ||
            Mathf.Max(x, endX) + halfWidth > WorldBuilder.RiverRight) return;

        var b = Self.Take();
        b.frames = frames;
        b.art.sprite = frames[0];
        b.art.sortingOrder = WorldBuilder.RiverFxOrder;
        b.art.transform.position = new Vector3(x, y, 0);
        b.stretch = new Vector2(length, 1.5f);
        b.art.transform.localScale = new Vector3(size * b.stretch.x, size * b.stretch.y, 1);
        b.art.transform.rotation = Quaternion.Euler(0, 0, tilt);
        b.velocity = Vector3.right * drift;
        b.life = life;
        b.startScale = size;
        b.grow = growth;
        b.from = new Color(.78f, .98f, 1, opacity);
        b.to = new Color(.39f, .9f, .94f, 0);
        b.art.color = b.from;
    }

    public static void RiverSpray(float x, float y, float direction, float strength)
    {
        if (x < WorldBuilder.RiverLeft + .45f || x > WorldBuilder.RiverRight - .45f) return;
        for (int i = 0; i < 2; i++)
        {
            var b = Self.Take();
            b.art.sprite = JinaArt.Dot();
            b.art.sortingOrder = WorldBuilder.RiverFxOrder;
            b.art.transform.position = new Vector3(x - direction * i * .14f, y, 0);
            b.velocity = new Vector3(direction * (.42f + i * .28f), (.95f - i * .24f) * strength, 0);
            b.gravity = -4.5f;
            b.life = .31f - i * .04f;
            b.startScale = (.85f - i * .22f) * strength;
            b.grow = .45f;
            b.from = new Color(.85f, .98f, 1, .85f * strength);
            b.to = new Color(.52f, .88f, 1, 0);
            b.art.color = b.from;
            b.art.transform.localScale = new Vector3(b.startScale, b.startScale, 1);
        }
    }
    // 무겁게 내려앉을 때 땅을 따라 퍼지는 고리와 함께 튀는 먼지·흙덩이.
    public static void Shock(Vector3 feet, float size = 1, bool clubImpact = false)
    {
        var b = Self.Take();
        b.frames = Frames(ref ring, "ring", 4); b.life = .3f;
        b.art.sprite = b.frames[0]; b.art.transform.position = feet + Vector3.up * .08f;
        b.startScale = size * 1.2f; b.grow = 1.5f;
        b.from = new Color(1, .96f, .84f, .95f); b.to = new Color(1, .9f, .7f, .2f);
        if (clubImpact)
        {
            b.life = .38f;
            b.grow = 1.9f;
            b.to.a = 0;
            b.art.color = b.from;
            b.art.transform.localScale = new Vector3(b.startScale, b.startScale, 1);
            ClubDust(feet, size);
            return;
        }
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 3; i++)
                Dust(feet + Vector3.right * side * (.4f + i * .5f) * size, side * (1.2f + i * .5f), 1.1f + i * .25f);
        Dots(feet + Vector3.up * .1f, 14, new Color(.5f, .38f, .28f), 5, .5f, 0, 16, 1.2f);
    }

    // 충격점 양쪽에서 튀는 흙먼지. 커지며 흐려지고 땅에 닿을 때 사라진다.
    static void ClubDust(Vector3 at, float size)
    {
        const float gravity = 12f;
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 4; i++)
            {
                float heightSpeed = (3.8f - i * .45f + side * .18f) * size;
                float width = (1.3f - i * .18f) * size;
                var tint = Color.Lerp(new Color(.68f, .48f, .31f, .94f),
                    new Color(.9f, .73f, .53f, .78f), i / 3f);
                var puff = DustPuff(at + Vector3.right * (side * (.22f + .19f * i) * size),
                    side * (1.5f + i * .7f) * size, width, tint);
                puff.velocity.y = heightSpeed;
                puff.gravity = -gravity;
                puff.life = 2 * heightSpeed / gravity;
                puff.grow = 1.6f;
                puff.to.a = 0;
                puff.art.flipX = side < 0;
                puff.art.color = tint;
                puff.art.transform.localScale = new Vector3(width, width, 1);
            }
    }
    // 타격 직전의 번쩍임.
    public static void Glint(Vector3 at, Color colour)
    {
        var b = Self.Take();
        b.art.sprite = JinaArt.Fx("glint"); b.life = .28f;
        b.art.transform.position = at; b.spin = 200;
        b.startScale = .4f; b.grow = 3f;
        b.from = colour; b.to = new Color(colour.r, colour.g, colour.b, 0);
        b.art.sortingOrder = 9;
    }
    // 몸이 있던 자리에 남는 사라지는 잔상: 백스텝, 돌진.
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
    // 맞은 곳에서 튀어나오는 숫자.
    public static void Number(Vector3 at, int amount, Color colour, int size = 20)
    {
        Self.popups.Add(new Popup { at = at, text = amount.ToString(), colour = colour, born = Time.time, size = size });
    }
    // 맞은 곳에서 튀어나오는 글자.
    public static void Word(Vector3 at, string text, Color colour, int size = 20)
    {
        Self.popups.Add(new Popup { at = at, text = text, colour = colour, born = Time.time, size = size });
    }

    const float PopupLength = .62f;
    // 튀어나온 숫자와 글자를 그린다.
    void OnGUI()
    {
        if (popups.Count == 0) return;
        var cam = Camera.main;
        if (cam == null) return;
        GUI.depth = 5;
        KoreanUI.Apply();
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            var p = popups[i];
            float t = (Time.time - p.born) / PopupLength;
            if (t >= 1) { if (Event.current.type == EventType.Repaint) popups.RemoveAt(i); continue; }
            var world = p.at + Vector3.up * (.5f * (1 - (1 - t) * (1 - t)));
            var at = JinaUI.FromWorld(cam, world);
            var c = p.colour; c.a = Mathf.Clamp01((1 - t) * 3);
            // 잠깐 커졌다가 가라앉는다.
            int size = Mathf.RoundToInt(p.size * (t < .15f ? 1 + (1 - t / .15f) * .35f : 1));
            // 긴 글(레벨 업과 오른 능력치)도 한 줄로.
            var r = new Rect(at.x - 220, at.y - 20, 440, 40);
            var ink = new Color(.04f, .03f, .04f, c.a);
            foreach (var o in Offsets) JinaUI.Text(new Rect(r.x + o.x, r.y + o.y, r.width, r.height), p.text, size, ink, TextAnchor.MiddleCenter, true);
            JinaUI.Text(r, p.text, size, c, TextAnchor.MiddleCenter, true);
        }
    }
    static readonly Vector2[] Offsets = { new Vector2(-2, 0), new Vector2(2, 0), new Vector2(0, -2), new Vector2(0, 2) };

    void OnDestroy() { if (self == this) self = null; }
}
