using System.Collections.Generic;
using UnityEngine;

// The Kongjwi demon (기획서.md 1-1): a toad wedged in a bottomless crock, wearing Kongjwi as its front legs.
// She waits on the back hill beyond the pass (PrototypeWorld.KongLair). When the seonbi comes near, the game stops
// for a talk in her portrait window; her last line, "젓갈로 담가 주마!", turns her into the demon (bars, the camera on
// her, a black swelling shape, a flash, a roar and her name card). Then she fights from range, hops over the terrain
// like he does (walls stop her, boulders do not hurt her), follows him down into Kongbat village, where she binds the
// villagers and kills them from the right (PrototypeKongVillage), and cannot cross the river.
// Slain: +10. With half her health gone, the drowned mother's hairpin (F, or used from the bag) burns the toad away
// and leaves Kongjwi herself: +50 (PrototypeKongAftermath plays what follows).
// The fight itself (moves, attacks, what she throws) is in PrototypeKongBoss.Fight.cs.
public partial class PrototypeKongBoss : MonoBehaviour
{
    public PrototypeCombat combat;
    public enum Phase { Waiting, Meeting, Changing, Fighting, Slain, Purified }
    public Phase Now { get; private set; }
    public const int MaxHealth = 260;
    public int Health { get; private set; } = MaxHealth;
    // Below half, the hairpin can reach Kongjwi inside.
    public bool Weakened => Health <= MaxHealth / 2;
    public bool Fighting => Now == Phase.Fighting;
    public bool Resolved => Now == Phase.Slain || Now == Phase.Purified;
    public bool Started => Now >= Phase.Fighting;
    // The talk and the change hold the game (PrototypeCombat.Paused).
    public bool Cutscene => Now == Phase.Meeting || Now == Phase.Changing;
    public const string Name = "콩쥐 요괴", Epithet = "밑 빠진 독에 깃든 두꺼비", LastLine = "젓갈로 담가 주마!", Roar = "꾸르륵… 꾸에에엑!";
    // How near he comes before she speaks; seconds of the change and of its parts.
    public const float MeetRange = 11f, ChangeLength = 3.8f, SwellAt = .9f, FlashAt = 1.75f, CardAt = 2f, RoarUntil = 3f;
    public const float DeathLength = 1.8f, PurifyLength = 3.4f, PurifyRange = 3f;
    // Her feet; facing +1 right, -1 left (the art is drawn facing left).
    public Vector3 Feet => transform.position;
    public int Facing { get; private set; } = -1;
    // Easy to hit: the box blows land in is wider and taller than the body that walks.
    public Bounds HurtBox => new Bounds(Feet + new Vector3(0, 1.35f, 0), new Vector3(2.3f, 2.6f, 1));
    public float MetAt { get; private set; } = -1;
    public float ChangedAt { get; private set; } = -1;
    public float DownAt { get; private set; } = -1;
    public float PurifiedAt { get; private set; } = -1;
    public float HitAt { get; private set; } = -1;
    // Purified away from the village, she stays kneeling there until he has left her map; then she is home again
    // and the village draws her (PrototypeKongVillage).
    public bool HandedBack { get; private set; }
    // Where she was when the toad burned away.
    public float PurifiedX { get; private set; }
    public float NoticeAt { get; private set; } = -1;
    private string notice = "";

    private string[] lines;
    private int page;
    private Rigidbody2D body;
    private BoxCollider2D shape;
    private SpriteRenderer picture, glow, human, pin;
    private SpriteRenderer[] sparks;

    // What she says when he finds her on the hill. Her last line is always the one that changes her.
    public static string[] MeetingLines(bool stayed, bool revealed, bool keepsake)
    {
        var said = new List<string>();
        if (revealed)
        {
            said.Add("늦으셨네요, 선비님. 너무 오래 기다리게 하지 말라고 했잖아요.");
            said.Add("장독대도, 그 종이도 다 보셨으니… 이제 선비님 차례예요.");
        }
        else if (stayed)
        {
            said.Add("오셨네요, 선비님. 혼자 오셨죠? …잘하셨어요.");
            said.Add("여긴 소리를 질러도 아무도 못 들어요. 동생이 그러던가요?");
            said.Add("보름까지만 착한 아이로 있으려 했는데… 선비님은 냄새를 너무 많이 맡으셨어요.");
        }
        else
        {
            said.Add("어머, 선비님. 저희 마을은 그냥 지나치셨네요?");
            said.Add("괜찮아요. 어차피 이 길로 오실 줄 알았거든요. 고개 너머엔 저밖에 없으니까.");
        }
        if (keepsake) said.Add("…그 냄새. 품에 강물 냄새 나는 걸 지니셨네요. 기분 나빠.");
        said.Add(LastLine);
        return said.ToArray();
    }
    // When he comes back from the foot of the hill after she struck him down.
    public static string[] RetryLines() => new[] { "또 오셨네요, 선비님. 이번엔 도망치지 마세요.", LastLine };
    // Her sweet face first (unless he already knows), the true one after.
    public static string MeetingPortrait(int page, bool revealed) => page == 0 && !revealed ? "kongjwi" : "kongjwi_true";

    void Start()
    {
        body = gameObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 3; body.freezeRotation = true; body.mass = 20;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        shape = gameObject.AddComponent<BoxCollider2D>();
        shape.size = new Vector2(1.5f, 1.9f); shape.offset = new Vector2(0, 1.0f); shape.edgeRadius = .05f;
        shape.sharedMaterial = new PhysicsMaterial2D("콩쥐 요괴 발") { friction = 0, bounciness = 0 };
        var playerBox = combat.player.GetComponent<Collider2D>();
        if (playerBox != null) Physics2D.IgnoreCollision(shape, playerBox);
        picture = Child("그림", 6); glow = Child("섬광", 7); human = Child("사람 모습", 6); pin = Child("은비녀", 8);
        pin.sprite = JinaArt.Item("item_binyeo");
        sparks = new SpriteRenderer[18];
        for (int i = 0; i < sparks.Length; i++) { sparks[i] = Child("빛 " + i, 8); sparks[i].sprite = JinaArt.Dot(); }
        BuildFight();
        ResetBoss();
    }
    SpriteRenderer Child(string label, int order)
    {
        var art = new GameObject(label).AddComponent<SpriteRenderer>();
        art.transform.SetParent(transform, false); art.sortingOrder = order; art.enabled = false;
        return art;
    }

    // How many times he has come back from the checkpoint since the last full restart.
    public int Retries { get; private set; }
    public void Retry() { int was = Retries; ResetBoss(); Retries = was + 1; }
    public void ResetBoss()
    {
        Retries = 0;
        if (body == null) return;
        Now = Phase.Waiting; Health = MaxHealth; Facing = -1;
        MetAt = ChangedAt = DownAt = PurifiedAt = HitAt = NoticeAt = -1; HandedBack = false; PurifiedX = 0;
        lines = null; page = 0;
        body.simulated = false; body.velocity = Vector2.zero;
        transform.position = new Vector3(PrototypeWorld.KongLair, PrototypeWorld.Top(PrototypeWorld.KongLair), 0);
        body.position = transform.position;
        ResetFight();
        PrototypeView.Focus(0, 0);
    }

    float PlayerX => combat.player.transform.position.x;
    bool HasKeepsake => JinaItems.Inventory != null && JinaItems.Inventory.GetQuantity(JinaItems.Keepsake) > 0;
    // Kongjwi in the village (rather than out on the hill): before the fight while he cannot see the hill, and
    // after it once she has gone home.
    public bool AtHome => (Now == Phase.Waiting && PlayerX < PrototypeWorld.PassStart) || (Now == Phase.Purified && HandedBack);

    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) { if (body != null) body.simulated = false; return; }
        if (!combat.Paused) StepHoney(Time.deltaTime);
        if (Now == Phase.Waiting) { WaitForHim(); return; }
        if (Now == Phase.Meeting) { Meet(); return; }
        if (Now == Phase.Changing) { Change(); return; }
        if (Now == Phase.Purified) { GoHome(); return; }
        if (Now != Phase.Fighting) return;
        body.simulated = !combat.Paused;
        if (combat.Paused) return;
        if (Input.GetKeyDown(KeyCode.F) && Near) TryPurify(true, out _);
        Fight(Time.deltaTime);
    }

    // On the hill, in sight: the talk begins.
    void WaitForHim()
    {
        if (combat.Paused || combat.Maps == null || !combat.Maps.Shows(PrototypeWorld.HillStart)) return;
        if (Mathf.Abs(PlayerX - Feet.x) < MeetRange) Meet(true);
    }

    public void Meet(bool begin = false)
    {
        if (begin)
        {
            var kong = combat.Kong;
            lines = Retries > 0 ? RetryLines() : MeetingLines(kong != null && kong.Stayed, kong != null && kong.Revealed, HasKeepsake);
            combat.SaveAtHill();
            page = 0; Now = Phase.Meeting; MetAt = Time.time;
            combat.player.CanMove = false;
        }
        combat.player.CanMove = false;
        Facing = PlayerX >= Feet.x ? 1 : -1;
        Look(Mathf.Clamp01((Time.time - MetAt) / .5f));
        if (Input.GetKeyDown(KeyCode.Escape)) page = lines.Length - 1;
        else if (Input.GetKeyDown(KeyCode.E) && !JinaUI.FinishTyping(lines[page])) NextLine();
    }
    public void NextLine()
    {
        if (Now != Phase.Meeting) return;
        if (++page < lines.Length) return;
        Now = Phase.Changing; ChangedAt = Time.time;
    }
    public string Line => Now == Phase.Meeting && lines != null ? lines[Mathf.Min(page, lines.Length - 1)] : null;
    public int Page => page;
    // The camera turns halfway to her while they talk and through the change.
    void Look(float weight) => PrototypeView.Focus((PlayerX + Feet.x) / 2, Mathf.SmoothStep(0, 1, weight));

    // The change: she trembles in a dark haze, a black shape swells round her, a flash, the demon roars.
    void Change()
    {
        float t = Time.time - ChangedAt, was = t - Time.deltaTime;
        combat.player.CanMove = false;
        Look(Mathf.Clamp01((ChangeLength - t) / .5f));
        var chest = Feet + Vector3.up * .8f;
        if (t < SwellAt && Time.frameCount % 3 == 0) JinaFx.Rise(chest, 2, new Color(.16f, .1f, .2f, .9f), .9f, 1.6f, .7f);
        if (was < .3f && t >= .3f) PrototypeView.Shake(.04f, .6f);
        if (was < SwellAt && t >= SwellAt) { JinaFx.Dust(Feet, -1.5f, 1.4f); JinaFx.Dust(Feet, 1.5f, 1.4f); PrototypeView.Shake(.08f, .8f); }
        if (was < FlashAt && t >= FlashAt) { JinaFx.Shock(Feet, 1.6f); PrototypeView.Shake(.18f, .7f); JinaFx.Dots(chest + Vector3.up, 20, new Color(.4f, .3f, .5f), 6, .7f); }
        if (t < ChangeLength) return;
        Now = Phase.Fighting;
        PrototypeView.Focus(0, 0);
        combat.player.CanMove = true;
        body.simulated = true; body.position = transform.position; body.velocity = Vector2.zero;
        StartFight();
    }

    // Blows from the seonbi. `sword`: a sword cut, which also counts toward breaking off a kill in the village.
    public bool Damage(int amount, bool sword)
    {
        if (!Fighting) return false;
        int before = Health;
        Health = Mathf.Max(0, Health - PrototypeCombat.CalcDamage(amount, 0));
        HitAt = Time.time;
        if (sword) Stagger(amount);
        var at = Feet + new Vector3(0, 1.4f, 0);
        int away = PlayerX < Feet.x ? 1 : -1;
        JinaFx.Hit(at, away, new Color(1, .94f, .78f), 1.3f);
        JinaFx.Number(at + Vector3.up * .9f + Vector3.right * Random.Range(-.3f, .3f), before - Health, Color.white, 22);
        if (before > MaxHealth / 2 && Weakened) { NoticeAt = Time.time; notice = HasKeepsake ? "독에 금이 갔습니다! 지금 가까이에서 F로 은비녀를 쓰면 콩쥐를 되찾을 수 있습니다." : "독에 금이 갔습니다. 은비녀가 있었다면…"; }
        if (Health > 0) return true;
        Now = Phase.Slain; DownAt = Time.time;
        body.simulated = false;
        JinaFx.Stop(.16f); PrototypeView.Shake(.22f, .5f);
        EndFight();
        if (combat.Quest != null) combat.Quest.progress.ResolveKong(false);
        if (combat.Kong != null) combat.Kong.Release();
        return true;
    }

    public bool Near => Fighting && Mathf.Abs(PlayerX - Feet.x) < PurifyRange && Mathf.Abs(combat.player.transform.position.y - (Feet.y + 1)) < 2.6f;
    // The hairpin, from F (`take`: it leaves the bag here) or from the bag (which takes it itself).
    public bool TryPurify(bool take, out string why)
    {
        why = null;
        if (!Fighting) { why = "누군가 아끼던 물건입니다. 지금은 품에 간직할 뿐입니다."; return false; }
        if (!HasKeepsake) { why = "독 속의 콩쥐에게 닿을 무언가가 필요합니다. 강의 물귀신들 사이에 무언가 있다고 했습니다."; NoticeAt = Time.time; notice = why; return false; }
        if (!Near) { why = "콩쥐 요괴 가까이에서 써야 합니다."; return false; }
        if (!Weakened) { why = "요괴의 기운이 아직 너무 셉니다. 체력을 절반 이하로 깎으세요."; NoticeAt = Time.time; notice = why; return false; }
        if (take) JinaItems.Inventory.TryRemove(JinaItems.Keepsake);
        Now = Phase.Purified; PurifiedAt = Time.time; PurifiedX = Feet.x;
        body.simulated = false;
        PrototypeView.Shake(.12f, .5f);
        EndFight();
        if (combat.Quest != null) combat.Quest.progress.ResolveKong(true);
        if (combat.Kong != null) combat.Kong.Release();
        return true;
    }

    // After the toad has burned away: in the village she is handed to it at once; out on the hill or the pass she
    // kneels where she is until he has gone from her map, then she is home.
    void GoHome()
    {
        if (HandedBack || Time.time - PurifiedAt < PurifyLength) return;
        bool inVillage = Feet.x >= PrototypeWorld.KongStart && Feet.x < PrototypeWorld.KongEnd;
        bool away = combat.Maps != null && combat.Maps.Current != PrototypeMaps.Index(Feet.x) && !combat.Maps.Travelling;
        var after = combat.Aftermath;
        if (!inVillage && !(away && (after == null || after.FieldDone))) return;
        HandedBack = true;
        var kong = combat.Kong;
        if (kong == null) return;
        // Where she knelt in the village, or home by her own door.
        kong.Place(PrototypeKongVillage.Who.Kongjwi, inVillage ? Feet.x : PrototypeWorld.Kongjwi.x);
        kong.SetLook(PrototypeKongVillage.Who.Kongjwi, inVillage ? VillagerVisual.Look.Kneel : VillagerVisual.Look.Sad);
        kong.Face(PrototypeKongVillage.Who.Kongjwi, inVillage ? Facing : 0);
    }

    // ---- what she looks like ----
    static string Pose(string pose) => "21_KongBoss/kong_boss_" + pose;
    void LateUpdate()
    {
        if (picture == null) return;
        picture.enabled = glow.enabled = human.enabled = pin.enabled = false;
        foreach (var s in sparks) s.enabled = false;
        DrawFight();
        if (combat == null) return;
        bool flip = Facing > 0;
        switch (Now)
        {
            case Phase.Waiting:
                if (!AtHome) Show(human, JinaArt.Load(PrototypeKongVillage.Kongjwi), Feet, flip);
                break;
            case Phase.Meeting:
                Show(human, JinaArt.Load(PrototypeKongVillage.Kongjwi), Feet, flip);
                break;
            case Phase.Changing: DrawChange(Time.time - ChangedAt, flip); break;
            case Phase.Fighting: DrawFighter(flip); break;
            case Phase.Slain: DrawDeath(Time.time - DownAt, flip); break;
            case Phase.Purified: if (!HandedBack) DrawPurify(Time.time - PurifiedAt, flip); break;
        }
    }
    static void Show(SpriteRenderer art, Sprite sprite, Vector3 feet, bool flip, float alpha = 1)
    {
        art.enabled = true; art.sprite = sprite; art.color = new Color(1, 1, 1, alpha);
        JinaArt.Place(art, Snap(feet), flip);
        art.transform.localScale = Vector3.one; art.transform.rotation = Quaternion.identity;
    }
    const float ArtPixel = 12f / (546f / 1.9f);
    static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x / ArtPixel) * ArtPixel, Mathf.Round(p.y / ArtPixel) * ArtPixel, p.z);
    void Flash(SpriteRenderer over, float amount, Color colour)
    {
        if (amount <= 0) return;
        glow.enabled = true; glow.sprite = JinaArt.Silhouette(over.sprite); glow.flipX = over.flipX;
        glow.transform.position = over.transform.position; glow.transform.localScale = over.transform.localScale;
        colour.a = amount; glow.color = colour;
    }
    float Shudder(float t, float every = .05f) => ((int)(t / every) % 2 == 0 ? 1 : -1) * ArtPixel;

    void DrawChange(float t, bool flip)
    {
        if (t < SwellAt)
        {
            Show(human, JinaArt.Load(PrototypeKongVillage.Kongjwi), Feet + Vector3.right * Shudder(t) * (t > .4f ? 2 : 1), flip);
            human.color = Color.Lerp(Color.white, new Color(.45f, .4f, .55f), t / SwellAt);
            return;
        }
        // The swelling shape: the demon's outline in black, growing from her size with a wobble, then the flash.
        var sprite = JinaArt.Load(Pose(t < RoarUntil && t >= FlashAt ? "open" : "idle_00"));
        Show(picture, sprite, Feet, flip);
        if (t < FlashAt)
        {
            float k = Mathf.SmoothStep(0, 1, (t - SwellAt) / (FlashAt - SwellAt));
            float wobble = 1 + .06f * Mathf.Sin(t * 40) * (1 - k);
            picture.transform.localScale = new Vector3(Mathf.Lerp(.42f, 1, k) * wobble, Mathf.Lerp(.5f, 1, k) / wobble, 1);
            picture.sprite = JinaArt.Silhouette(sprite); picture.color = new Color(.07f, .05f, .09f, 1);
            return;
        }
        Flash(picture, Mathf.Clamp01(1 - (t - FlashAt) / .45f), new Color(1, .95f, .85f));
        if (t < RoarUntil) picture.transform.position += Vector3.right * Shudder(t, .04f);
    }

    // The death: a white jolt, the toad slumps with its eyes shut, then the crock and all crumble away.
    void DrawDeath(float t, bool flip)
    {
        if (t >= DeathLength) return;
        Show(picture, JinaArt.Load(Pose(t < .9f ? "death_00" : t < 1.3f ? "death_01" : "death_02")), Feet, flip, t > 1.3f ? 1 - (t - 1.3f) / (DeathLength - 1.3f) : 1);
        if (t < .5f) picture.transform.position += Vector3.right * Shudder(t);
        Flash(picture, t < .2f ? .9f * (1 - t / .2f) : 0, Color.white);
    }

    // The purification: the hairpin rises and shines, light breaks through the crock, the toad burns away into black
    // wisps and the crock into shards, and Kongjwi is left kneeling; gold sparks drift up.
    void DrawPurify(float t, bool flip)
    {
        var kneel = JinaArt.Load("16_Kong/kongjwi_kneel");
        if (t < 2.4f)
        {
            Show(picture, JinaArt.Load(Pose(t < .6f ? "idle_00" : "crack")), Feet + Vector3.right * Shudder(t) * (t > .6f ? 2 : 1), flip, t > 1.5f ? 1 - (t - 1.5f) / .9f : 1);
            Flash(picture, t < .6f ? .2f + t : t < 1.5f ? .9f : Mathf.Max(0, .9f - (t - 1.5f)), new Color(1, .96f, .84f));
        }
        if (t >= 1.5f) Show(human, kneel, Feet, flip, Mathf.Clamp01((t - 1.5f) / .7f));
        if (t < 1.6f)
        {
            pin.enabled = true;
            float rise = 1 - Mathf.Pow(1 - Mathf.Clamp01(t / .7f), 3);
            pin.transform.position = Snap(Feet + new Vector3(Facing * .2f, 1.2f + 1.4f * rise, 0));
            pin.transform.localScale = Vector3.one * .8f; pin.transform.rotation = Quaternion.identity;
            pin.color = new Color(1, 1, 1, Mathf.Clamp01(t / .2f) * (t > 1.2f ? 1 - (t - 1.2f) / .4f : 1));
        }
        for (int i = 0; i < sparks.Length; i++)
        {
            bool wisp = i < 10;
            float start = wisp ? .9f : 1.6f, end = wisp ? 2.4f : PurifyLength;
            if (t < start || t >= end) continue;
            float life = ((t - start) * (wisp ? 1.3f : .9f) + i * .137f) % 1;
            var d = sparks[i]; d.enabled = true;
            d.transform.position = Snap(Feet + new Vector3(Mathf.Sin(i * 2.3f) * 1.1f, .4f + life * (wisp ? 2.8f : 3.2f), 0));
            d.transform.localScale = Vector3.one * (wisp ? 1.5f : 1);
            float fade = (1 - life) * Mathf.Clamp01((end - t) / .4f);
            d.color = wisp ? new Color(.1f, .07f, .12f, fade) : new Color(1, .86f, .45f, fade);
        }
    }

    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        PrototypeKoreanUI.Apply();
        if (Now == Phase.Meeting && lines != null)
        {
            DrawBars(Mathf.Clamp01((Time.time - MetAt) / .4f));
            bool revealed = combat.Kong != null && combat.Kong.Revealed;
            PrototypeKongVillage.TalkWindow(PrototypeKongVillage.Who.Kongjwi, MeetingPortrait(page, revealed || Retries > 0), lines[page],
                "E 다음 (" + (page + 1) + "/" + lines.Length + ")   ·   Esc 넘기기", page > 0 || revealed);
            return;
        }
        if (Now == Phase.Changing) { DrawChangeCard(Time.time - ChangedAt); return; }
        if (combat.Paused) return;
        if (NoticeAt >= 0 && Time.time - NoticeAt < 3.5f && !string.IsNullOrEmpty(notice)) JinaUI.Toast(notice);
        if (Fighting && Near && HasKeepsake) JinaUI.Prompt(Weakened ? "F: 은비녀로 콩쥐 되찾기" : "F: 은비녀 (요괴의 체력을 절반 이하로 깎아야 합니다)");
        DrawFightGUI();
    }
    void DrawBars(float amount)
    {
        float bar = JinaUI.H * .11f * amount;
        JinaUI.Fill(new Rect(0, 0, JinaUI.W, bar), Color.black);
        JinaUI.Fill(new Rect(0, JinaUI.H - bar, JinaUI.W, bar), Color.black);
    }
    // The bars stay through the change; her name card slides in under the roar.
    void DrawChangeCard(float t)
    {
        GUI.depth = -50;
        DrawBars(Mathf.Clamp01((ChangeLength - t) / .4f));
        float bar = JinaUI.H * .11f * Mathf.Clamp01((ChangeLength - t) / .4f);
        if (t >= FlashAt && t < RoarUntil + .4f) SpeechBubble.Draw(Feet + Vector3.up * 3.2f, "콩쥐", Roar);
        float card = Mathf.Clamp01((t - CardAt) / .5f) * Mathf.Clamp01((ChangeLength - .3f - t) / .4f);
        if (card <= 0) return;
        const float size = 33;
        float x = 60 - (1 - card) * 80, y = JinaUI.H - bar - size * 3.4f;
        JinaUI.Text(new Rect(x, y, JinaUI.W, size), Epithet, 18, new Color(.92f, .9f, .85f, card), TextAnchor.MiddleLeft);
        JinaUI.Fill(new Rect(x, y + size * 1.05f, size * 7 * card, 3), new Color(.62f, .8f, .45f, card));
        JinaUI.Text(new Rect(x, y + size * 1.15f, JinaUI.W, size * 1.8f), Name, 43, new Color(.7f, .86f, .5f, card), TextAnchor.MiddleLeft, true);
    }

    void OnDestroy() { ClearShots(); foreach (var h in honey) if (h.art != null) Destroy(h.art.gameObject); }
}
