using UnityEngine;

// 일반 NPC: 마을 경보와 대피(VillageAlarm), 뱃사공과 나룻배(Ferry), 주모·보부상 잡담(ShopChatter).
// 대화 바탕은 NpcTalk(MainNpcs.cs), 그림은 ActorVisuals.cs.

// ===== 마을 경보와 대피 (VillageAlarm) =====
// 도깨비가 마을에 들어오면 노인이 외치며 칼을 뽑고, 주모와 보부상은
// 집으로 달아난다. 도깨비가 토벌되거나 정화되면 다시 나와 천천히 제자리로 걷는다.
// 노인이 쓰러졌으면 먼저 시체 곁에 잠시 서서 슬픔을 말한다.
// 여기서는 움직임만 맡고, 프레임은 KeeperVisual, PeddlerVisual, ElderVisual이 고른다.
public class VillageAlarm : MonoBehaviour
{
    public Combat combat;
    public const string AlarmLine = "기어코 저 도깨비가 마을에 왔군! 모두 대피하시오!";
    public const float AlarmBubbleLength = 3.5f, RunSpeed = 3.6f, WalkSpeed = 1.1f;
    // 기와집 문. 마을 사람은 문 앞 마지막 반 칸에서 사라진다.
    public const float Shelter = 4.5f, DoorFade = .5f;
    // 경보 뒤 초(노인 외침을 먼저 듣는다)와 도깨비가 쓰러진 뒤 초(쓰러짐이 먼저 재생).
    public const float FleeDelay = .6f, ReturnDelay = 2.5f;
    // 쓰러진 노인 곁에 각자 서 있는 시간과 도착 뒤 말이 떠 있는 시간.
    public const float MournLength = 8f, MournLineLength = 4f;

    // 경보와 해제는 한 판에 한 번. 시각이 동작을 이끈다.
    public bool Alarmed { get; private set; }
    public bool Cleared { get; private set; }
    public float AlarmAt { get; private set; }
    public float ClearAt { get; private set; }

    public enum State { Home, Fleeing, Hidden, Returning, Mourning }
    public class Villager
    {
        public SpriteRenderer art;
        public string name, grief;
        public float home, stagger, bubble;
        // 쓰러진 노인 곁에서 서는 자리, 도착한 시각과 말을 시작하는 시각
        // (그 전에는 음수). 한 사람씩 MournLineLength 동안 말한다.
        public float mourn, mournedAt = -1, speakAt = -1;
        public State state;
    }
    public Villager Keeper { get; private set; }
    public Villager Peddler { get; private set; }
    public bool KeeperHome => Keeper == null || Keeper.state == State.Home;
    public bool PeddlerHome => Peddler == null || Peddler.state == State.Home;
    private KeeperVisual keeperVisual;
    // 다음 사람이 말을 시작할 수 있는 시각(앞사람이 끝난 뒤).
    private float lineFree;

    // 주모는 WorldBuilder가, 보부상은 PeddlerShop.Start가 만드니 필요할 때 찾는다.
    void Bind()
    {
        if (Keeper == null)
        {
            var keeper = GameObject.Find("주막 주모");
            if (keeper != null)
            {
                Keeper = new Villager
                {
                    art = keeper.GetComponent<SpriteRenderer>(), home = WorldBuilder.Tavern.x, stagger = 0,
                    name = "주모", bubble = 1.4f, mourn = WorldBuilder.Elder.x - 2.4f,
                    grief = "노인장… 이리 허망하게 가시다니요.",
                };
                keeperVisual = KeeperVisual.Attach(Keeper.art, combat);
            }
        }
        if (Peddler == null && combat.Peddler != null && combat.Peddler.Marker != null)
            Peddler = new Villager
            {
                art = combat.Peddler.Marker, home = WorldBuilder.Peddler.x, stagger = .25f,
                name = "보부상", bubble = 1.55f, mourn = WorldBuilder.Elder.x - 3.3f,
                grief = "장터마다 노인장 덕담을 전했는데… 이제 누구에게 길을 묻겠소.",
            };
    }

    // 재시작: 주모·보부상을 제자리로.
    public void ResetVillage()
    {
        Alarmed = Cleared = false;
        foreach (var v in new[] { Keeper, Peddler })
        {
            if (v == null) continue;
            v.state = State.Home;
            v.mournedAt = v.speakAt = -1;
            Move(v, v.home, 1);
            v.art.enabled = true;
        }
        lineFree = 0;
        if (keeperVisual != null) keeperVisual.ResetPose();
    }

    // 경보, 대피, 귀가, 애도를 진행한다.
    void Update()
    {
        if (combat == null) return;
        Bind();
        if (combat.Paused) return;
        if (!Alarmed && combat.BossInVillage) { Alarmed = true; AlarmAt = Time.time; }
        if (Alarmed && !Cleared && combat.EnemyDefeated) { Cleared = true; ClearAt = Time.time; }
        foreach (var v in new[] { Keeper, Peddler })
            if (v != null) Step(v);
    }

    bool ElderDead => combat.Quest != null && combat.Quest.progress.ElderDead;
    // 다시 나오면 먼저 쓰러진 노인에게 (한 번) 갔다가 집으로.
    float Target(Villager v) => ElderDead && v.mournedAt < 0 ? v.mourn : v.home;
    // 쓰러진 노인 곁에 섬. 그림이 노인 쪽으로 돌린다.
    public bool IsMourning(SpriteRenderer art) =>
        (Keeper != null && Keeper.art == art && Keeper.state == State.Mourning) || (Peddler != null && Peddler.art == art && Peddler.state == State.Mourning);

    // 집 → (경보) 문으로 뜀 → 숨음 → (해제) 걸어 나옴 → (노인 쓰러짐: 애도) → 집.
    void Step(Villager v)
    {
        float x = v.art.transform.position.x;
        switch (v.state)
        {
            case State.Home:
                if (Alarmed && !Cleared && Time.time >= AlarmAt + FleeDelay + v.stagger) v.state = State.Fleeing;
                break;
            case State.Fleeing:
                x = Mathf.Max(Shelter, x - RunSpeed * Time.deltaTime);
                if (x <= Shelter) { v.state = State.Hidden; v.art.enabled = false; }
                break;
            case State.Hidden:
                if (Cleared && Time.time >= ClearAt + ReturnDelay + v.stagger * 4) { v.state = State.Returning; v.art.enabled = true; }
                break;
            case State.Returning:
                float target = Target(v);
                x = Mathf.MoveTowards(x, target, WalkSpeed * Time.deltaTime);
                if (x == target && target == v.home) v.state = State.Home;
                else if (x == target) Arrive(v);
                break;
            case State.Mourning:
                // 적어도 자기 말을 다 읽을 때까지는 머문다.
                if (Time.time >= Mathf.Max(v.mournedAt + MournLength, v.speakAt + MournLineLength + 1)) v.state = State.Returning;
                break;
        }
        Move(v, x, Mathf.Clamp01((x - Shelter) / DoorFade));
    }

    // 쓰러진 노인 곁에 도착: 애도를 시작하고 말할 차례를 잡는다.
    void Arrive(Villager v)
    {
        v.state = State.Mourning;
        v.mournedAt = Time.time;
        v.speakAt = Mathf.Max(Time.time, lineFree);
        lineFree = v.speakAt + MournLineLength;
    }

    // 마을 사람을 `x`로 옮기고 투명도를 정한다.
    void Move(Villager v, float x, float alpha)
    {
        var p = v.art.transform.position; p.x = x; v.art.transform.position = p;
        var c = v.art.color; c.a = alpha; v.art.color = c;
    }

    // 애도 말풍선을 그린다.
    void OnGUI()
    {
        if (combat == null || combat.Quest == null || combat.Paused) return;
        if (Alarmed && Time.time - AlarmAt < AlarmBubbleLength && !ElderDead)
            SpeechBubble.Draw(new Vector3(WorldBuilder.Elder.x, 1.45f, 0), "마을 노인", AlarmLine);
        // 한 번에 한 사람만 애도한다: 먼저 도착한 사람, 그 사람이 끝나면 다른 사람.
        foreach (var v in new[] { Keeper, Peddler })
            if (v != null && v.state == State.Mourning && Time.time >= v.speakAt && Time.time < v.speakAt + MournLineLength
                && SpeechBubble.OnScreen(v.art.transform.position))
                SpeechBubble.Draw(new Vector3(v.art.transform.position.x, v.bubble, 0), v.name, v.grief);
    }
}

// ===== 가시덤불·뱃사공·나룻배 (Ferry) =====
// 숲 동쪽의 나루터(WorldBuilder.FerryStart..FerryEnd). 숲 끝의 가시덤불이 도깨비를 토벌하거나
// 정화할 때까지 길을 막아서, 늘 그 일을 끝낸 뒤에만 도착한다. 뱃사공이 말한다(E: 다음 줄,
// Esc: 떠남). 선비가 이 맵에 있는 동안 배가 강을 오가며 위에 선 사람을 태운다.
// 강에 빠지면 다치고 가까운 둑으로 돌아간다.
public class Ferry : NpcTalk
{
    // 배의 양 끝(중심), 속도, 둑마다 기다리는 시간, 강에 휩쓸리기 전 가라앉는 깊이, 대화 거리.
    public const float BoatLeft = WorldBuilder.RiverLeft + 2.1f, BoatRight = WorldBuilder.RiverRight - 2.1f;
    public const float BoatSpeed = 2.4f, BoatWait = 1.6f, SinkY = -1.6f, TalkRange = 1.8f;
    // 갑판 윗면(둑 높이)은 배 그림 아래에서 22줄 중 15줄 위.
    const float DeckHeight = 15f / 24f, Deck = -.5f;

    public float SplashAt { get; private set; } = -1;
    // 가시덤불: 숲이 조용해진 뒤 시드는 데 걸리는 시간, 그게 시작된 시각.
    public const float WitherLength = 1.2f, ClearedNotice = 4f;
    public float ClearedAt { get; private set; } = -1;
    public bool ThornsShut => thornWall != null && thornWall.enabled;
    private SpriteRenderer thorns;
    private BoxCollider2D thornWall;
    public FerryBoat Boat { get; private set; }
    private SpriteRenderer marker;
    private bool talked;

    public bool PlayerHere => combat != null && combat.player.transform.position.x >= WorldBuilder.FerryStart && combat.player.transform.position.x < WorldBuilder.FerryEnd;
    bool AtThorns => ThornsShut && combat.player.transform.position.x > WorldBuilder.FerryStart - 3;
    public bool Near => combat != null && Within(WorldBuilder.Ferryman, TalkRange);

    // 가시덤불, 뱃사공, 나룻배를 만든다.
    void Start()
    {
        marker = new GameObject("뱃사공").AddComponent<SpriteRenderer>();
        marker.transform.position = WorldBuilder.Ferryman;
        marker.transform.localScale = new Vector3(.75f, 1.4f, 1);
        marker.sprite = combat.player.GetComponent<SpriteRenderer>().sprite;
        marker.color = new Color(.5f, .45f, .35f);   // 흰 사각형은 "방금 맞음"으로 읽혀 색이 입혀진다
        JinaActorVisual.Attach(marker, JinaArt.Ferryman, combat);

        var boat = new GameObject("나룻배");
        var art = boat.AddComponent<SpriteRenderer>(); art.sprite = JinaArt.Prop("ferry_boat"); art.sortingOrder = 5;
        boat.layer = 8;
        var box = boat.AddComponent<BoxCollider2D>();
        box.size = new Vector2(3.7f, .25f); box.offset = new Vector2(0, DeckHeight - .125f);
        box.usedByEffector = true;
        var effector = boat.AddComponent<PlatformEffector2D>(); effector.useOneWay = true; effector.surfaceArc = 160;
        var body = boat.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        Boat = boat.AddComponent<FerryBoat>();
        Boat.ferry = this;

        var wall = new GameObject("숲 끝 가시덤불");
        wall.transform.position = new Vector3(WorldBuilder.FerryStart - 1f, -.5f, 0);   // 그림 폭 2: 숲 맵 오른쪽 끝에 붙음
        wall.layer = 8;
        thorns = wall.AddComponent<SpriteRenderer>(); thorns.sprite = JinaArt.Prop("thorn_wall"); thorns.sortingOrder = 7;
        thornWall = wall.AddComponent<BoxCollider2D>();
        thornWall.size = new Vector2(1, 30); thornWall.offset = new Vector2(0, 15);
        ResetFerry();
    }

    // 재시작: 가시덤불과 배, 대화를 처음으로.
    public void ResetFerry()
    {
        IsOpen = false; Page = 0; SplashAt = ClearedAt = -1; talked = false;
        if (Boat != null) Boat.Moor(new Vector2(BoatLeft, Deck - DeckHeight));
        if (thornWall != null) { thornWall.enabled = true; thorns.enabled = true; thorns.color = Color.white; }
    }

    // 선비가 도착했을 때의 말(늘 숲이 조용해진 뒤): 노인이 죽었으면 그 이야기 먼저,
    // 그다음 숲 이야기, 그다음 건너는 법과 강에 대한 소문.
    // `fleeing`: 배까지 따라오지 못하는 콩쥐 요괴를 피해 강을 건너 돌아왔음.
    public static string[] Lines(QuestProgress q, bool again = false, bool keepsake = false, bool crossed = false, bool fleeing = false)
    {
        var said = new System.Collections.Generic.List<string>();
        if (fleeing)
        {
            said.Add("건너편에서 비명이 끊이질 않더니… 선비, 낯빛이 하얗구려. 무슨 일이오?");
            said.Add("독을 뒤집어쓴 두꺼비라… 그 무거운 몸으로는 내 배에 못 오를 게요. 여기선 숨을 돌려도 되오.");
            if (keepsake) said.Add("그 비녀는 강이 내어 준 것이오. 그 집 일은 그 비녀로 풀어야 할 게요.");
            said.Add("허나 저 마을 사람들은 누가 지키오? 채비를 마치면 배에 오르시오. 바로 건네 드리리다.");
            return said.ToArray();
        }
        if (q.KongOutcome == EncounterOutcome.Purified)
        {
            said.Add("강물 소리가 맑아졌소. 그 여인의 넋도 이제 편히 쉬겠구려.");
            said.Add("칼 대신 비녀로 끝내셨다지. 사공 노릇 삼십 년에 이런 길손은 처음이오.");
            return said.ToArray();
        }
        if (q.KongOutcome == EncounterOutcome.Slain)
        {
            said.Add("강 건너 괴물이 쓰러졌다는 소식이오. 수고하셨소.");
            said.Add("…허나 그 집 큰딸도 함께 갔다지. 칼로 끝낸 일은 강물처럼 다시 돌아오기도 하오.");
            return said.ToArray();
        }
        // 다시 왔을 때: 전부 다시 말하지 않는다.
        if (keepsake) { said.Add("그 비녀… 어디서 났소? 물귀신을 그리 베어 내더니, 강이 내어 준 것이로구려."); said.Add("부잣집 안주인이 꼭 그런 걸 꽂고 다녔소. 임자에게 돌려줄 날이 있을 게요."); return said.ToArray(); }
        if (again && crossed) { said.Add("강 건너는 어떻습디까? …말 안 해도 낯빛을 보니 알겠구려."); said.Add("콩밭 마을 사람들은 좋은 사람들이오. 그래서 더 걱정이오."); return said.ToArray(); }
        if (again) { said.Add("배는 쉬지 않고 오가니 편히 타시오. 강물 소리나 듣다 가시구려."); said.Add("밤마다 우는 그 소리, 물귀신을 열쯤 베어 내면 잦아든다는 말이 있소. 뱃사람들 말이니 믿거나 말거나."); return said.ToArray(); }
        if (q.ElderDead) said.Add("마을 노인장 소식은 바람결에 들었소… 그분은 젊어서 이 나루를 지키던 분이오.");
        if (q.Outcome == EncounterOutcome.Purified)
        {
            said.Add("숲에서 내려오신 길이오? 강물 소리가 한결 순해졌다 했더니, 도깨비를 달래 보내셨구려.");
            said.Add("삯은 됐소. 이런 길손을 건네는 게 사공의 복이지.");
        }
        else
        {
            said.Add("숲 쪽에서 오셨소? 그 가시덤불이 걷힌 걸 보니, 선비가 도깨비를 쓰러뜨렸구려.");
            said.Add("덕분에 다시 배를 띄우게 됐으니 삯은 받지 않겠소.");
            said.Add("다만… 칼로 끝낸 일은 강물처럼 다시 돌아오기도 하오. 조심해 건너시오.");
        }
        said.Add("배가 이쪽 기슭에 닿으면 올라타시오. 물에 빠지면 차갑소.");
        said.Add("강 건너 콩밭 마을 부잣집 안주인이 작년 이맘때 이 강에 빠졌소. 사람들은 천벌이라는데…");
        said.Add("내 눈엔 누가 민 것 같았소. 밤에 강에서 우는 소리는… 그 여인일지도 모르지.");
        return said.ToArray();
    }

    // 가시덤불, 강에 빠짐, 뱃사공 대화를 처리한다.
    void Update()
    {
        if (combat == null) return;
        // 도깨비를 해결하면 가시덤불은 바로 길을 막지 않고 시들어 간다.
        if (ClearedAt < 0 && combat.EnemyDefeated) { ClearedAt = Time.time; thornWall.enabled = false; }
        if (ClearedAt >= 0 && thorns.enabled)
        {
            float gone = (Time.time - ClearedAt) / WitherLength;
            thorns.color = new Color(1, 1, 1, 1 - Mathf.Clamp01(gone));
            if (gone >= 1) thorns.enabled = false;
        }
        if (combat.PlayerHealth <= 0) { IsOpen = false; return; }
        if (IsOpen) TalkKeys();
        else if (Near && JinaUI.Interact && !combat.Paused) Open();
        Splash();
    }

    // 뱃사공 대화를 연다.
    public void Open()
    {
        Begin(Lines(combat.Quest.progress, talked, JinaItems.Inventory != null && JinaItems.Inventory.GetQuantity(JinaItems.Keepsake) > 0, combat.Kong != null && (combat.Kong.Invited || combat.Kong.Stayed),
            combat.KongBoss != null && combat.KongBoss.Fighting));
        talked = true;
    }

    // 강에 빠짐: 다른 타격처럼 맞고, 가까운 둑으로 돌아간다.
    public void Splash()
    {
        var p = combat.player.transform.position;
        if (p.x <= WorldBuilder.RiverLeft || p.x >= WorldBuilder.RiverRight || p.y > SinkY) return;
        SplashAt = Time.time;
        JinaFx.Dots(new Vector3(p.x, WorldBuilder.WaterBottom + 1, 0), 22, new Color(.8f, .92f, 1), 6, .6f, 0, 16);
        combat.HurtPlayer();
        bool nearLeft = p.x - WorldBuilder.RiverLeft < WorldBuilder.RiverRight - p.x;
        var body = combat.player.GetComponent<Rigidbody2D>();
        combat.player.transform.position = new Vector3(nearLeft ? WorldBuilder.RiverLeft - 1.2f : WorldBuilder.RiverRight + 1.2f, .3f, 0);
        body.velocity = Vector2.zero;
    }

    // 뱃사공 말풍선과 안내를 그린다.
    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        KoreanUI.Apply();
        if (Near && !IsOpen && !combat.Paused) JinaUI.Prompt("Enter: 뱃사공과 이야기하기");
        if (AtThorns && !combat.Paused) JinaUI.Prompt("가시덤불이 길을 막았습니다. 숲의 도깨비를 해결해야 지나갈 수 있습니다.");
        if (ClearedAt >= 0 && Time.time - ClearedAt < ClearedNotice && !combat.Paused && combat.Encounter != null && !combat.Encounter.Playing)
            JinaUI.Toast("숲 끝을 막던 가시덤불이 시들었습니다. 나루터로 가는 길이 열렸습니다.");
        if (IsOpen && SpeechBubble.OnScreen(WorldBuilder.Ferryman))
        {
            SpeechBubble.Draw(new Vector3(WorldBuilder.Ferryman.x, 1.6f, 0), "뱃사공", lines[Page]);
            JinaUI.Prompt(PageHint);
        }
        if (SplashAt >= 0 && Time.time - SplashAt < 1.6f)
            JinaUI.Toast("풍덩! 차가운 강물에 빠졌다가 겨우 기슭으로 올라왔습니다.");
    }

    // 만든 표시를 지운다.
    void OnDestroy()
    {
        if (marker != null) Destroy(marker.gameObject);
        if (Boat != null) Destroy(Boat.gameObject);
        if (thorns != null) Destroy(thorns.gameObject);
    }
}

// 나룻배: 선비가 나루터 맵에 있는 동안 둑과 둑을 오가며 둑마다 기다리고, 갑판에 탄 선비를 태운다.
public class FerryBoat : MonoBehaviour
{
    public Ferry ferry;
    public Vector2 Velocity { get; private set; }
    private Rigidbody2D body;
    private int heading = 1;
    private float waitUntil;

    // 배를 `at`에 대 둔다.
    public void Moor(Vector2 at)
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        transform.position = at; body.position = at;
        heading = 1; waitUntil = 0; Velocity = Vector2.zero;
    }

    // 배를 둑 사이로 움직이고 탄 사람을 함께 옮긴다.
    void FixedUpdate()
    {
        Velocity = Vector2.zero;
        if (ferry == null || !ferry.PlayerHere || ferry.combat.Paused || Time.time < waitUntil) return;
        float x = body.position.x + heading * Ferry.BoatSpeed * Time.fixedDeltaTime;
        if (x >= Ferry.BoatRight || x <= Ferry.BoatLeft)
        {
            x = Mathf.Clamp(x, Ferry.BoatLeft, Ferry.BoatRight);
            heading = -heading; waitUntil = Time.time + Ferry.BoatWait;
        }
        Velocity = new Vector2((x - body.position.x) / Time.fixedDeltaTime, 0);
        body.MovePosition(new Vector2(x, body.position.y));
    }
}

// ===== 주모·보부상 잡담 (ShopChatter) =====
// 선비가 가까이 있으면 주모와 보부상이 잡담한다: 이따금 한 사람이 말하고
// 다른 사람이 답한다. 이야기는 진행을 따른다(도깨비가 날뜀, 토벌, 정화, 노인이 죽음).
public class ShopChatter : MonoBehaviour
{
    public Combat combat;
    // 한 줄의 초, 주고받기 사이, 첫 주고받기 전, 엿들으려면 선비가 얼마나 가까워야 하는지.
    public const float LineLength = 3.4f, Gap = 8f, FirstAt = 4f, Range = 8f;
    // 주고받기 하나는 주모 한 줄, 보부상 한 줄.
    static readonly string[][] Uneasy =
    {
        new[] { "요즘 숲 쪽에서 밤마다 도깨비불이 보인다지요?", "그래서 산 넘는 장꾼이 줄었소. 장사가 영 시원찮구려." },
        new[] { "노인장께서 오늘도 숲 쪽을 한참 바라보시더이다.", "젊어서는 칼 꽤나 쓰셨다는 말이 있지요. 지금도 칼을 품고 다니신다던가." },
        new[] { "저 선비님 행색을 보니 과거 보러 가시는 길인가 보오.", "한양까지는 아직 멀었소. 화살은 넉넉히 챙겨 가시구려." },
        new[] { "서쪽 비석 터에 요괴가 들끓는다니 해가 지면 문을 걸어 잠가야겠소.", "망한 나라 군졸 귀신들이라던데… 무덤도 없이 떠도는 것들이지." },
        new[] { "숲을 지나면 나루터요. 뱃사공 영감이 요새 통 배를 띄우지 않는다더이다.", "강에서 밤마다 누가 운다지 뭐요. 물에 빠진 이들의 넋이라던가." },
        new[] { "강 건너 콩밭 마을에 콩쥐라는 효녀가 있다지요? 의붓동생한테 그리 시달린다던데.", "팥쥐 그것 말이오? 제 어미가 강에 빠져 죽은 것도 다 그 딸 업보지, 쯧쯧." },
        new[] { "그 집 젓갈이 그리 맛있다지요? 한번 들여와 볼까.", "값이 싸서 나도 두어 번 받아 왔는데… 이상하게 팔고 나면 찜찜합디다. 왜 그런지는 모르겠소." },
        new[] { "손님, 낮술은 안 되오. 과거 보러 가는 분이 얼굴 벌게서 어쩌시려고.", "주모는 저래 놓고 밤이면 혼자 한 사발씩 하지요." },
        new[] { "우리 집 국밥은 뼈를 사흘 고아요. 아무 데서나 못 먹는 맛이지.", "그 소리를 삼 년째 듣는데, 뼈는 늘 그 뼈 같단 말이오." },
    };
    static readonly string[][] Slain =
    {
        new[] { "도깨비가 쓰러졌다니 이제 밤길도 걸을 만하겠소.", "산 너머 장도 다시 서겠구려. 내일은 짐을 더 꾸려 와야겠소." },
        new[] { "그런데 그 도깨비, 본디 산을 지키던 녀석이었다던데…", "쉿, 다 지난 일이오. 산이 조용해졌으면 그만이지." },
        new[] { "산은 조용해졌는데, 강 건너 고개에 도적 떼가 생겼다더이다.", "도적들 눈빛이 도깨비처럼 퀭하다던데… 같은 것에 홀린 게 아닌가 싶소." },
        new[] { "나무하러 가던 아이들이 그 자리에 돌을 던지더이다. 잘 죽었다고.", "…애들 눈이 무섭구려. 노인장은 아무 말 없이 숲만 보고 계십디다." },
    };
    static readonly string[][] Purified =
    {
        new[] { "도깨비가 순해졌다더니, 아까 숲 어귀에서 꾸벅 인사를 하더이다.", "허허, 산길 길동무로 삼아도 되겠소. 장꾼들이 좋아하겠구려." },
        new[] { "칼 대신 부적을 쓰셨다니, 보기 드문 선비시오.", "그러게 말이오. 저런 분이 급제해야 나라가 바로 서지." },
        new[] { "도깨비 말이, 망령들이 강과 고개에도 손을 뻗었다더이다.", "가는 길마다 홀린 것이 있겠구려. 선비님, 부적은 넉넉하시오?" },
        new[] { "도깨비가 나뭇짐을 져다 주길래 막걸리 한 사발 줬더니 울더이다.", "허허. 몇백 년 만에 얻어먹는 술이었겠구려." },
    };
    static readonly string[][] Mourning =
    {
        new[] { "노인장 계시던 자리가 텅 비었구려…", "장터마다 이 소식을 전해야 하니 발걸음이 무겁소." },
        new[] { "그 연세에 칼을 뽑아 우리를 먼저 피하게 하셨지요.", "…우리 목숨은 노인장께 빚진 것이오." },
        new[] { "아침마다 노인장 드시던 국 한 그릇을 저도 모르게 푸고 있구려.", "…내려놓지 마시오. 그 자리에 놓아 두시오." },
    };

    private float nextAt = FirstAt, startedAt = -1;
    private int count;
    private string[] current;

    // 진행 상태에 맞는 잡담 주제.
    public static string[][] Topics(QuestProgress q) => q.ElderDead ? Mourning
        : q.Outcome == EncounterOutcome.Purified ? Purified : q.Outcome == EncounterOutcome.Slain ? Slain : Uneasy;
    public bool Talking => startedAt >= 0;
    // 둘 다 제자리, 다른 일 없음, 선비가 주막과 좌판에서 들리는 거리에 있음.
    public bool CanChat => combat != null && combat.Quest != null && combat.Village != null && !combat.Paused
        && combat.PlayerHealth > 0 && combat.Village.KeeperHome && combat.Village.PeddlerHome
        && (!combat.Village.Alarmed || combat.Village.Cleared)
        && Mathf.Abs(combat.player.transform.position.x - (WorldBuilder.Tavern.x + WorldBuilder.Peddler.x) / 2) < Range;

    public void ResetChatter() { nextAt = Time.time + FirstAt; startedAt = -1; count = 0; }
    // 잡담을 시작한다.
    public void Begin()
    {
        var topics = Topics(combat.Quest.progress);
        current = topics[count++ % topics.Length];
        startedAt = Time.time;
    }

    // 잡담 줄을 넘기고, 끊기면 기다린다.
    void Update()
    {
        if (combat == null) return;
        if (Talking)
        {
            // 끊겼거나(가게가 열림, 경보가 울림) 끝났으면 다음까지 잠시 기다린다.
            if (!CanChat || Time.time - startedAt >= LineLength * 2) { startedAt = -1; nextAt = Time.time + Gap; }
        }
        else if (Time.time >= nextAt && CanChat) Begin();
    }

    // 잡담 말풍선을 그린다.
    void OnGUI()
    {
        if (!Talking || !CanChat) return;
        bool keeper = Time.time - startedAt < LineLength;
        // 가게 가까이에서는 가게 안내가 화면 아래를 쓰고, 말풍선은 말하는 사람 위에 뜬다.
        var at = keeper ? new Vector3(WorldBuilder.Tavern.x, 1.4f, 0) : new Vector3(WorldBuilder.Peddler.x, 1.55f, 0);
        if (SpeechBubble.OnScreen(at)) SpeechBubble.Draw(at, keeper ? "주모" : "보부상", current[keeper ? 0 : 1]);
    }
}
