using UnityEngine;

// Game over: the world drops to black and only the seonbi is left, collapsing in a motion of its own (thrown back,
// slumping, onto his knees, toppling face down) while his weapon spins away behind him. Then a few words and R.
// The in-game fall (SeonbiVisual.Fall) still plays underneath, hidden by the black.
public class PrototypeGameOver : MonoBehaviour
{
    public PrototypeCombat combat;
    // Seconds after PlayerDownAt: the black closes in, each pose begins, the weapon lands, the words appear.
    public const float FadeIn = .15f, Slump = .35f, Kneel = .8f, Topple = 1.45f, Down = 1.75f, WeaponLands = .9f, TextAt = 2.3f;
    // How far the blow throws him back, and how long the white flash of the blow lasts.
    public const float Knockback = .3f, FlashLength = .12f, VeilSize = 200;
    public const string Title = "선비가 쓰러졌습니다", Words = "길은 여기서 끊겼으나, 뜻은 아직 남았다.", Retry = "R: 다시 일어서기";
    private SpriteRenderer veil, actor, flash, weapon;
    private Sprite[] frames;
    private Sprite facedown;
    private float shownFor = -1;
    private bool landed;
    private GUIStyle titleStyle, lineStyle;

    public bool Showing => combat != null && combat.PlayerHealth <= 0 && combat.PlayerDownAt >= 0;
    public float Elapsed => Showing ? Time.time - combat.PlayerDownAt : -1;

    void Start()
    {
        veil = Part("게임 오버 어둠", 40);
        veil.sprite = JinaArt.Dot(); veil.color = Color.black;
        actor = Part("게임 오버 선비", 41);
        weapon = Part("게임 오버 무기", 41);
        flash = Part("게임 오버 섬광", 42);
        frames = JinaArt.Frames("01_Player/player_ko_", 3);
        facedown = JinaArt.Load("01_Player/player_facedown");
        LateUpdate();
    }

    SpriteRenderer Part(string name, int order)
    {
        var art = new GameObject(name).AddComponent<SpriteRenderer>(); art.sortingOrder = order; art.enabled = false; return art;
    }

    // The pose at `t` seconds: which frame, how far it slides, and how far it has tipped forward (0..1).
    public Sprite Pose(float t, out float slide, out float tip)
    {
        slide = Knockback * (1 - Mathf.Pow(1 - Mathf.Clamp01(t / Slump), 3)); tip = 0;
        if (t < Slump) return frames[0];
        if (t < Kneel) return frames[1];
        if (t < Topple) return frames[2];
        if (t < Down) { tip = Mathf.Pow((t - Topple) / (Down - Topple), 2); return frames[2]; }
        tip = 1; return facedown;
    }

    void LateUpdate()
    {
        if (veil == null) return;
        bool on = Showing;
        veil.enabled = actor.enabled = weapon.enabled = on;
        flash.enabled = false;
        if (!on) { shownFor = -1; landed = false; return; }
        float t = Elapsed;
        if (shownFor != combat.PlayerDownAt) { shownFor = combat.PlayerDownAt; landed = false; PrototypeView.Shake(.25f, .35f); }

        int facing = combat.player.Facing;
        var source = combat.player.GetComponent<SpriteRenderer>();
        var feet = new Vector3(combat.player.transform.position.x, source.bounds.min.y, 0);
        // Far wider than any view, centred on him, so whatever the camera shows is black.
        veil.transform.position = feet;
        veil.transform.localScale = new Vector3(VeilSize, VeilSize, 1) / JinaArt.DotSize;
        veil.color = new Color(0, 0, 0, Mathf.Clamp01(t / FadeIn));
        actor.sprite = Pose(t, out float slide, out float tip);
        var at = feet - Vector3.right * facing * slide;
        if (t >= Kneel && t < Topple) at.x += Mathf.Sin(t * 70) * .015f;   // he shakes, holding himself up
        if (actor.sprite == facedown) at.y += facedown.pivot.y / facedown.pixelsPerUnit;
        JinaArt.Place(actor, at, facing < 0);
        actor.transform.rotation = Quaternion.Euler(0, 0, actor.sprite == facedown ? 0 : -90 * tip * facing);
        // White at first; once he lies still the light leaves him.
        actor.color = Color.Lerp(Color.white, new Color(.55f, .55f, .6f), Mathf.Clamp01((t - Down) / 1.2f));
        if (!landed && t >= Down) { landed = true; PrototypeView.Shake(.12f, .2f); }

        if (t < FlashLength)
        {
            flash.enabled = true; flash.sprite = JinaArt.Silhouette(actor.sprite);
            JinaArt.Place(flash, at, facing < 0);
            flash.color = new Color(1, 1, 1, 1 - t / FlashLength);
        }

        // The weapon is knocked from his hand: up and over behind him, spinning, then lies flat (as in the in-game fall).
        bool bow = combat.UsingBow;
        weapon.sprite = JinaArt.Load(bow ? "04_Weapons/bow" : "04_Weapons/hwando");
        float k = Mathf.Clamp01(t / WeaponLands);
        var hand = feet + new Vector3(facing * (bow ? .14f : .27f), bow ? .86f : .75f, 0);
        var ground = feet + new Vector3(-facing * 1.3f, .05f, 0);
        JinaArt.Place(weapon, Vector3.Lerp(hand, ground, k) + Vector3.up * 1.4f * 4 * k * (1 - k), facing < 0);
        float rest = bow ? -90 : -40;
        weapon.transform.rotation = Quaternion.Euler(0, 0, (k < 1 ? 720 * k : 0) * facing + rest * k * facing);
        weapon.color = actor.color;
    }

    void OnGUI()
    {
        float t = Elapsed;
        if (t < TextAt) return;
        PrototypeKoreanUI.Apply();
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            lineStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        }
        float size = Mathf.Clamp(Screen.height / 24f, 18, 40);
        float a = Mathf.Clamp01((t - TextAt) / .6f), b = Mathf.Clamp01((t - TextAt - .8f) / .6f), c = Mathf.Clamp01((t - TextAt - 1.6f) / .6f);
        titleStyle.fontSize = Mathf.RoundToInt(size * 1.35f);
        titleStyle.normal.textColor = new Color(.8f, .25f, .22f, a);
        lineStyle.fontSize = Mathf.RoundToInt(size * .8f);
        float y = Screen.height * .22f;
        GUI.Label(new Rect(40, y, Screen.width - 80, size * 2.2f), Title, titleStyle);
        lineStyle.normal.textColor = new Color(.92f, .9f, .85f, b);
        GUI.Label(new Rect(40, y + size * 2.2f, Screen.width - 80, size * 1.6f), Words, lineStyle);
        lineStyle.normal.textColor = new Color(.95f, .85f, .6f, c * (.65f + .35f * Mathf.Sin(t * 3)));
        GUI.Label(new Rect(40, Screen.height * .78f, Screen.width - 80, size * 1.6f), Retry, lineStyle);
    }

    void OnDestroy()
    {
        foreach (var art in new[] { veil, actor, flash, weapon }) if (art != null) Destroy(art.gameObject);
    }
}
