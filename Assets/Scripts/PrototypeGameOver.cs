using UnityEngine;

// 게임 오버: 세상이 검게 꺼지고 선비만 남아 자기만의 동작으로 무너진다(뒤로 튕기고,
// 주저앉고, 무릎 꿇고, 엎어짐). 그동안 무기가 뒤로 돌며 날아간다. 그다음 몇 마디와 R.
// 게임 안 쓰러짐(SeonbiVisual.Fall)은 아래에서 그대로 재생되고 검은 화면에 가려진다.
public class PrototypeGameOver : MonoBehaviour
{
    public PrototypeCombat combat;
    // PlayerDownAt 뒤 초: 검은 화면이 닫히고, 각 자세가 시작되고, 무기가 떨어지고, 글이 나온다.
    public const float FadeIn = .15f, Slump = .35f, Kneel = .8f, Topple = 1.45f, Down = 1.75f, WeaponLands = .9f, TextAt = 2.3f;
    // 타격에 뒤로 밀리는 거리와 타격의 흰 번쩍임이 이어지는 시간.
    public const float Knockback = .3f, FlashLength = .12f, VeilSize = 200;
    public const string Title = "선비가 쓰러졌습니다", Words = "길은 여기서 끊겼으나, 뜻은 아직 남았다.", Retry = "R: 다시 일어서기", RetryHill = "R: 뒷산 입구에서 다시 일어서기";
    private SpriteRenderer veil, actor, flash, weapon;
    private Sprite[] frames;
    private Sprite facedown;
    private float shownFor = -1;
    private bool landed;

    public bool Showing => combat != null && combat.PlayerHealth <= 0 && combat.PlayerDownAt >= 0;
    public float Elapsed => Showing ? Time.time - combat.PlayerDownAt : -1;

    // 검은 판, 선비, 무기 렌더러를 만든다.
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

    // 게임 오버 연출용 렌더러 하나를 만든다.
    SpriteRenderer Part(string name, int order)
    {
        var art = new GameObject(name).AddComponent<SpriteRenderer>(); art.sortingOrder = order; art.enabled = false; return art;
    }

    // `t`초의 자세: 어느 프레임인지, 얼마나 미끄러졌는지, 앞으로 얼마나 기울었는지(0..1).
    public Sprite Pose(float t, out float slide, out float tip)
    {
        slide = Knockback * (1 - Mathf.Pow(1 - Mathf.Clamp01(t / Slump), 3)); tip = 0;
        if (t < Slump) return frames[0];
        if (t < Kneel) return frames[1];
        if (t < Topple) return frames[2];
        if (t < Down) { tip = Mathf.Pow((t - Topple) / (Down - Topple), 2); return frames[2]; }
        tip = 1; return facedown;
    }

    // 쓰러진 뒤 시간에 따라 선비 자세와 무기를 움직인다.
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
        // 어떤 화면보다 훨씬 넓게 선비를 중심으로 깔아서, 카메라가 무엇을 보든 검다.
        veil.transform.position = feet;
        veil.transform.localScale = new Vector3(VeilSize, VeilSize, 1) / JinaArt.DotSize;
        veil.color = new Color(0, 0, 0, Mathf.Clamp01(t / FadeIn));
        actor.sprite = Pose(t, out float slide, out float tip);
        var at = feet - Vector3.right * facing * slide;
        if (t >= Kneel && t < Topple) at.x += Mathf.Sin(t * 70) * .015f;   // 몸을 떨며 버틴다
        if (actor.sprite == facedown) at.y += facedown.pivot.y / facedown.pixelsPerUnit;
        JinaArt.Place(actor, at, facing < 0);
        actor.transform.rotation = Quaternion.Euler(0, 0, actor.sprite == facedown ? 0 : -90 * tip * facing);
        // 처음엔 흰색, 가만히 누우면 빛이 빠진다.
        actor.color = Color.Lerp(Color.white, new Color(.55f, .55f, .6f), Mathf.Clamp01((t - Down) / 1.2f));
        if (!landed && t >= Down) { landed = true; PrototypeView.Shake(.12f, .2f); }

        if (t < FlashLength)
        {
            flash.enabled = true; flash.sprite = JinaArt.Silhouette(actor.sprite);
            JinaArt.Place(flash, at, facing < 0);
            flash.color = new Color(1, 1, 1, 1 - t / FlashLength);
        }

        // 무기가 손에서 튕겨 나간다: 뒤로 넘어가며 돌다가 바닥에 눕는다(게임 안 쓰러짐과 같게).
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

    // 게임 오버 문구와 R 안내를 그린다.
    void OnGUI()
    {
        float t = Elapsed;
        if (t < TextAt) return;
        PrototypeKoreanUI.Apply();
        const float size = 30;
        float a = Mathf.Clamp01((t - TextAt) / .6f), b = Mathf.Clamp01((t - TextAt - .8f) / .6f), c = Mathf.Clamp01((t - TextAt - 1.6f) / .6f);
        float y = JinaUI.H * .22f;
        JinaUI.Text(new Rect(40, y, JinaUI.W - 80, size * 2.2f), Title, 40, new Color(.8f, .25f, .22f, a), TextAnchor.MiddleCenter, true);
        JinaUI.Text(new Rect(40, y + size * 2.2f, JinaUI.W - 80, size * 1.6f), Words, 24, new Color(.92f, .9f, .85f, b), TextAnchor.MiddleCenter);
        JinaUI.Text(new Rect(40, JinaUI.H * .78f, JinaUI.W - 80, size * 1.6f), combat.CanRetryAtHill ? RetryHill : Retry, 24, new Color(.95f, .85f, .6f, c * (.65f + .35f * Mathf.Sin(t * 3))), TextAnchor.MiddleCenter);
    }

    // 연출용 오브젝트를 지운다.
    void OnDestroy()
    {
        foreach (var art in new[] { veil, actor, flash, weapon }) if (art != null) Destroy(art.gameObject);
    }
}
