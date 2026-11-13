using System.Collections.Generic;
using UnityEngine;

// 콩쥐 요괴(기획서.md 1-1): 밑 빠진 독에 낀 두꺼비가 콩쥐를 앞다리처럼 걸치고 있다.
// 고개 너머 뒷산에서 기다린다(PrototypeWorld.KongLair). 선비가 가까이 오면 게임이 멈추고
// 초상화 창으로 대화한다. 마지막 대사 "젓갈로 담가 주마!"에 요괴로 변한다(띠, 요괴를 보는
// 카메라, 부풀어 오르는 검은 형체, 번쩍임, 포효, 이름 카드). 그다음 멀리서 싸우고, 선비처럼 지형을 뛰어넘고
// (벽에 막히고 바위에 다치지 않음), 선비를 따라 콩밭 마을까지 내려가 마을 사람들을 묶고
// 오른쪽부터 죽인다(PrototypeKongVillage). 강은 건너지 못한다.
// 토벌: +10. 체력이 절반 아래면 어머니의 은비녀(F, 또는 보따리에서 사용)가 두꺼비를 태워 없애고
// 콩쥐만 남긴다: +50(그 뒤 장면은 PrototypeKongAftermath).
// 싸움 자체(이동, 공격, 던지는 것)는 PrototypeKongBoss.Fight.cs에 있다.
public partial class PrototypeKongBoss : MonoBehaviour
{
    public PrototypeCombat combat;
    public enum Phase { Waiting, Meeting, Changing, Fighting, Slain, Purified }
    public Phase Now { get; private set; }
    public const int MaxHealth = 260;
    public int Health { get; private set; } = MaxHealth;
    // 절반 아래면 은비녀가 안의 콩쥐에게 닿는다.
    public bool Weakened => Health <= MaxHealth / 2;
    public bool Fighting => Now == Phase.Fighting;
    public bool Resolved => Now == Phase.Slain || Now == Phase.Purified;
    public bool Started => Now >= Phase.Fighting;
    // 대화와 변신 동안 게임이 멈춘다(PrototypeCombat.Paused).
    public bool Cutscene => Now == Phase.Meeting || Now == Phase.Changing;
    public const string Name = "콩쥐 요괴", Epithet = "밑 빠진 독에 깃든 두꺼비", LastLine = "젓갈로 담가 주마!", Roar = "꾸르륵… 꾸에에엑!";
    // 말을 걸기 전에 선비가 얼마나 가까이 와야 하는지, 변신과 그 단계들의 초.
    public const float MeetRange = 11f, ChangeLength = 3.8f, SwellAt = .9f, FlashAt = 1.75f, CardAt = 2f, RoarUntil = 3f;
    public const float DeathLength = 1.8f, PurifyLength = 3.4f, PurifyRange = 3f;
    // 발 위치, 방향 +1 오른쪽 -1 왼쪽(그림은 왼쪽을 보고 그려짐).
    public Vector3 Feet => transform.position;
    public int Facing { get; private set; } = -1;
    // 맞히기 쉽게: 타격 판정 상자는 걸어 다니는 몸보다 넓고 높다.
    public Bounds HurtBox => new Bounds(Feet + new Vector3(0, 1.35f, 0), new Vector3(2.3f, 2.6f, 1));
    public float MetAt { get; private set; } = -1;
    public float ChangedAt { get; private set; } = -1;
    public float DownAt { get; private set; } = -1;
    public float PurifiedAt { get; private set; } = -1;
    public float HitAt { get; private set; } = -1;
    // 마을 밖에서 정화되면 선비가 그 맵을 떠날 때까지 무릎 꿇고 있다가 집으로 돌아가고
    // 마을(PrototypeKongVillage)이 그린다.
    public bool HandedBack { get; private set; }
    // 두꺼비가 타 없어질 때 있던 자리.
    public float PurifiedX { get; private set; }
    public float NoticeAt { get; private set; } = -1;
    private string notice = "";

    private string[] lines;
    private int page;
    private Rigidbody2D body;
    private BoxCollider2D shape;
    private SpriteRenderer picture, glow, human, pin;
    private SpriteRenderer[] sparks;

    // 뒷산에서 만났을 때의 대사. 마지막 줄은 늘 변신하는 대사다.
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
    // 요괴에게 쓰러진 뒤 뒷산 입구에서 다시 올라왔을 때.
    public static string[] RetryLines() => new[] { "또 오셨네요, 선비님. 이번엔 도망치지 마세요.", LastLine };
    // 먼저 상냥한 얼굴(이미 알면 빼고), 그다음 진짜 얼굴.
    public static string MeetingPortrait(int page, bool revealed) => page == 0 && !revealed ? "kongjwi" : "kongjwi_true";

    // 요괴 렌더러를 만들고 뒷산에 세운다.
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
    // 요괴 그림의 자식 렌더러를 만든다.
    SpriteRenderer Child(string label, int order)
    {
        var art = new GameObject(label).AddComponent<SpriteRenderer>();
        art.transform.SetParent(transform, false); art.sortingOrder = order; art.enabled = false;
        return art;
    }

    // 마지막 완전 재시작 뒤 체크포인트에서 몇 번 다시 왔는지.
    public int Retries { get; private set; }
    public void Retry() { int was = Retries; ResetBoss(); Retries = was + 1; }
    // 재시작: 요괴를 뒷산에서 기다리는 상태로.
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
    // 콩쥐가 (뒷산이 아니라) 마을에 있음: 싸우기 전 선비가 뒷산을 볼 수 없을 때,
    // 그리고 싸운 뒤 집에 돌아갔을 때.
    public bool AtHome => (Now == Phase.Waiting && PlayerX < PrototypeWorld.PassStart) || (Now == Phase.Purified && HandedBack);

    // 만남, 대화, 변신, 싸움 단계를 진행한다.
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

    // 뒷산에서 보이는 곳에 오면 대화가 시작된다.
    void WaitForHim()
    {
        if (combat.Paused || combat.Maps == null || !combat.Maps.Shows(PrototypeWorld.HillStart)) return;
        if (Mathf.Abs(PlayerX - Feet.x) < MeetRange) Meet(true);
    }

    // 뒷산 대화를 연다(`begin`: 바로 시작).
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
    // 대화를 다음 줄로 넘기고, 마지막 줄 뒤에 변신을 시작한다.
    public void NextLine()
    {
        if (Now != Phase.Meeting) return;
        if (++page < lines.Length) return;
        Now = Phase.Changing; ChangedAt = Time.time;
    }
    public string Line => Now == Phase.Meeting && lines != null ? lines[Mathf.Min(page, lines.Length - 1)] : null;
    public int Page => page;
    // 대화와 변신 동안 카메라가 요괴 쪽으로 절반 돈다.
    void Look(float weight) => PrototypeView.Focus((PlayerX + Feet.x) / 2, Mathf.SmoothStep(0, 1, weight));

    // 변신: 어두운 안개 속에서 떨고, 검은 형체가 부풀고, 번쩍, 요괴가 포효한다.
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

    // 선비의 타격. `sword`: 칼질이면 마을에서 죽이기를 끊는 데에도 센다.
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
    // 은비녀: F로(`take`: 여기서 보따리에서 뺀다) 또는 보따리에서 사용(보따리가 직접 뺀다).
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

    // 두꺼비가 타 없어진 뒤: 마을이면 바로 마을에 넘긴다. 뒷산이나 고개면 선비가 그 맵을 떠날 때까지
    // 그 자리에 무릎 꿇고 있다가 집으로 간다.
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
        // 마을에서 무릎 꿇은 자리, 아니면 자기 집 문 앞.
        kong.Place(PrototypeKongVillage.Who.Kongjwi, inVillage ? Feet.x : PrototypeWorld.Kongjwi.x);
        kong.SetLook(PrototypeKongVillage.Who.Kongjwi, inVillage ? VillagerVisual.Look.Kneel : VillagerVisual.Look.Sad);
        kong.Face(PrototypeKongVillage.Who.Kongjwi, inVillage ? Facing : 0);
    }

    // ---- 모습 ----
    static string Pose(string pose) => "21_KongBoss/kong_boss_" + pose;
    // 단계에 맞는 요괴 모습을 그린다(대기, 변신, 싸움, 토벌, 정화).
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
    // 그림 하나를 발 위치에 세운다.
    static void Show(SpriteRenderer art, Sprite sprite, Vector3 feet, bool flip, float alpha = 1)
    {
        art.enabled = true; art.sprite = sprite; art.color = new Color(1, 1, 1, alpha);
        JinaArt.Place(art, Snap(feet), flip);
        art.transform.localScale = Vector3.one; art.transform.rotation = Quaternion.identity;
    }
    const float ArtPixel = 12f / (546f / 1.9f);
    static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x / ArtPixel) * ArtPixel, Mathf.Round(p.y / ArtPixel) * ArtPixel, p.z);
    // 그림 위에 흰색 번쩍임을 덮는다.
    void Flash(SpriteRenderer over, float amount, Color colour)
    {
        if (amount <= 0) return;
        glow.enabled = true; glow.sprite = JinaArt.Silhouette(over.sprite); glow.flipX = over.flipX;
        glow.transform.position = over.transform.position; glow.transform.localScale = over.transform.localScale;
        colour.a = amount; glow.color = colour;
    }
    float Shudder(float t, float every = .05f) => ((int)(t / every) % 2 == 0 ? 1 : -1) * ArtPixel;

    // 변신 연출을 그린다.
    void DrawChange(float t, bool flip)
    {
        if (t < SwellAt)
        {
            Show(human, JinaArt.Load(PrototypeKongVillage.Kongjwi), Feet + Vector3.right * Shudder(t) * (t > .4f ? 2 : 1), flip);
            human.color = Color.Lerp(Color.white, new Color(.45f, .4f, .55f), t / SwellAt);
            return;
        }
        // 부풀어 오르는 형체: 요괴 윤곽을 검게, 콩쥐 크기에서 출렁이며 커지다가 번쩍.
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

    // 토벌: 흰 충격, 두꺼비가 눈을 감고 늘어지고, 독과 함께 부서져 사라진다.
    void DrawDeath(float t, bool flip)
    {
        if (t >= DeathLength) return;
        Show(picture, JinaArt.Load(Pose(t < .9f ? "death_00" : t < 1.3f ? "death_01" : "death_02")), Feet, flip, t > 1.3f ? 1 - (t - 1.3f) / (DeathLength - 1.3f) : 1);
        if (t < .5f) picture.transform.position += Vector3.right * Shudder(t);
        Flash(picture, t < .2f ? .9f * (1 - t / .2f) : 0, Color.white);
    }

    // 정화: 은비녀가 떠올라 빛나고, 빛이 독을 뚫고, 두꺼비는 검은 기운으로 타 없어지고
    // 독은 조각나고, 콩쥐가 무릎 꿇은 채 남는다. 금빛 불티가 떠오른다.
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

    // 대화창, 띠, 이름 카드, 체력 막대를 그린다.
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
    // 위아래 검은 띠를 그린다.
    void DrawBars(float amount)
    {
        float bar = JinaUI.H * .11f * amount;
        JinaUI.Fill(new Rect(0, 0, JinaUI.W, bar), Color.black);
        JinaUI.Fill(new Rect(0, JinaUI.H - bar, JinaUI.W, bar), Color.black);
    }
    // 변신 내내 띠가 남고, 포효 아래로 이름 카드가 밀려 들어온다.
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
