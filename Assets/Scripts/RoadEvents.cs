using UnityEngine;
using System.Collections.Generic;

// ===== 가시덤불·뱃사공·나룻배 (Ferry) =====
// 숲 동쪽의 나루터(WorldBuilder.FerryStart..FerryEnd). 숲 끝의 가시덤불이 도깨비를 토벌하거나
// 정화할 때까지 길을 막아서, 늘 그 일을 끝낸 뒤에만 도착한다. 뱃사공이 말한다(E: 다음 줄,
// Esc: 떠남). 선비가 이 맵에 있는 동안 배가 강을 오가며 위에 선 사람을 태운다.
// 강에 빠지면 다치고 가까운 둑으로 돌아간다.
public class Ferry : MonoBehaviour
{
    public PrototypeCombat combat;
    // 배의 양 끝(중심), 속도, 둑마다 기다리는 시간, 강에 휩쓸리기 전 가라앉는 깊이, 대화 거리.
    public const float BoatLeft = WorldBuilder.RiverLeft + 2.1f, BoatRight = WorldBuilder.RiverRight - 2.1f;
    public const float BoatSpeed = 2.4f, BoatWait = 1.6f, SinkY = -1.6f, TalkRange = 1.8f;
    // 갑판 윗면(둑 높이)은 배 그림 아래에서 22줄 중 15줄 위.
    const float DeckHeight = 15f / 24f, Deck = -.5f;

    public bool IsOpen { get; private set; }
    public int Page { get; private set; }
    public float SplashAt { get; private set; } = -1;
    // 가시덤불: 숲이 조용해진 뒤 시드는 데 걸리는 시간, 그게 시작된 시각.
    public const float WitherLength = 1.2f, ClearedNotice = 4f;
    public float ClearedAt { get; private set; } = -1;
    public bool ThornsShut => thornWall != null && thornWall.enabled;
    private SpriteRenderer thorns;
    private BoxCollider2D thornWall;
    public FerryBoat Boat { get; private set; }
    private SpriteRenderer marker;
    private string[] lines;
    private bool talked;

    public bool PlayerHere => combat != null && combat.player.transform.position.x >= WorldBuilder.FerryStart && combat.player.transform.position.x < WorldBuilder.FerryEnd;
    bool AtThorns => ThornsShut && combat.player.transform.position.x > WorldBuilder.FerryStart - 3;
    public bool Near => combat != null && Vector2.Distance(combat.player.transform.position, WorldBuilder.Ferryman) < TalkRange;

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
        wall.transform.position = new Vector3(WorldBuilder.FerryStart - .5f, -.5f, 0);
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
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            else if (Input.GetKeyDown(KeyCode.E) && !JinaUI.FinishTyping(lines[Page])) { Next(); }
        }
        else if (Near && Input.GetKeyDown(KeyCode.E) && !combat.Paused) Open();
        Splash();
    }

    // 뱃사공 대화를 연다.
    public void Open()
    {
        lines = Lines(combat.Quest.progress, talked, JinaItems.Inventory != null && JinaItems.Inventory.GetQuantity(JinaItems.Keepsake) > 0, combat.Kong != null && (combat.Kong.Invited || combat.Kong.Stayed),
            combat.KongBoss != null && combat.KongBoss.Fighting);
        talked = true;
        Page = 0; IsOpen = true;
        combat.player.CanMove = false;
    }
    public void Next() { if (++Page >= lines.Length) Close(); }
    void Close() { IsOpen = false; combat.player.CanMove = combat.PlayerHealth > 0; }

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
        PrototypeKoreanUI.Apply();
        if (Near && !IsOpen && !combat.Paused) JinaUI.Prompt("E: 뱃사공과 이야기하기");
        if (AtThorns && !combat.Paused) JinaUI.Prompt("가시덤불이 길을 막았습니다. 숲의 도깨비를 해결해야 지나갈 수 있습니다.");
        if (ClearedAt >= 0 && Time.time - ClearedAt < ClearedNotice && !combat.Paused && combat.Encounter != null && !combat.Encounter.Playing)
            JinaUI.Toast("숲 끝을 막던 가시덤불이 시들었습니다. 나루터로 가는 길이 열렸습니다.");
        if (IsOpen && SpeechBubble.OnScreen(WorldBuilder.Ferryman))
        {
            SpeechBubble.Draw(new Vector3(WorldBuilder.Ferryman.x, 1.6f, 0), "뱃사공", lines[Page]);
            JinaUI.Prompt("E: 다음 (" + (Page + 1) + "/" + lines.Length + ")   ·   Esc: 그만 듣기");
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

// ===== 고개: 굴러오는 바위·서낭당 (MountainPass) =====
// 나루터 너머 고개(WorldBuilder.PassStart..PassEnd). 선비가 꼭대기 아래를 오르는 동안
// 몇 초마다 바위가 계단을 굴러 내려온다: 뛰어넘거나 백스텝(K)으로 통과한다.
// 고원의 서낭당에서 한 판에 한 번 돌탑에 돌을 얹으면(E) 체력이 다 찬다.
public class MountainPass : MonoBehaviour
{
    public PrototypeCombat combat;
    public const float RockEvery = 3.2f, RollSpeed = 4.2f, Radius = .48f, Fall = 25f, ShrineRange = 1.8f;
    public class Rock { public SpriteRenderer art; public float vy; }
    public readonly List<Rock> rocks = new List<Rock>();
    public bool ShrineUsed { get; private set; }
    public float ShrineAt { get; private set; } = -1;
    private float nextRock;
    private string shrineSaid = "";

    public bool OnPass => combat != null && combat.player.transform.position.x >= WorldBuilder.PassStart && combat.player.transform.position.x < WorldBuilder.PassEnd;
    public bool NearShrine => combat != null && Mathf.Abs(combat.player.transform.position.x - WorldBuilder.ShrineX) < ShrineRange
        && Mathf.Abs(combat.player.transform.position.y - WorldBuilder.Top(WorldBuilder.ShrineX)) < 1.5f;

    // 재시작: 바위와 서낭당을 처음으로.
    public void ResetPass()
    {
        ClearRocks();
        ShrineUsed = false; ShrineAt = -1; shrineSaid = "";
    }
    // 굴러가는 바위를 모두 지운다.
    public void ClearRocks()
    {
        foreach (var r in rocks) Destroy(r.art.gameObject);
        rocks.Clear();
        nextRock = 0;
    }

    // `x`에 바위 하나를 굴린다.
    public Rock Spawn(float x)
    {
        var art = new GameObject("굴러오는 바위").AddComponent<SpriteRenderer>();
        art.sprite = JinaArt.PropCentred("boulder"); art.sortingOrder = 4;
        art.transform.position = new Vector3(x, WorldBuilder.Top(x) + Radius, 0);
        var rock = new Rock { art = art };
        rocks.Add(rock);
        return rock;
    }

    // 남은 바위를 `dt`만큼 왼쪽으로 굴린다. 계단마다 중력으로 떨어지고, 선비에게 닿으면 타격.
    public void Step(float dt)
    {
        var body = combat.player.GetComponent<SpriteRenderer>().bounds;
        for (int i = rocks.Count - 1; i >= 0; i--)
        {
            var r = rocks[i]; var p = r.art.transform.position;
            p.x -= RollSpeed * dt;
            float floor = WorldBuilder.Top(p.x) + Radius;
            if (p.y > floor) { r.vy -= Fall * dt; p.y = Mathf.Max(floor, p.y + r.vy * dt); }
            else { p.y = floor; r.vy = 0; }
            r.art.transform.position = p;
            r.art.transform.Rotate(0, 0, RollSpeed * dt / Radius * Mathf.Rad2Deg);
            if (new Bounds(p, Vector3.one * Radius * 1.7f).Intersects(body)) combat.HurtPlayer();
            if (p.x < WorldBuilder.PassStart + 1) { Destroy(r.art.gameObject); rocks.RemoveAt(i); }
        }
    }

    // 돌탑에 돌 하나: 체력 가득, 한 판에 한 번.
    public bool UseShrine()
    {
        if (combat.PlayerHealth <= 0) return false;
        ShrineAt = Time.time;
        if (ShrineUsed) { shrineSaid = "이미 돌을 얹었습니다. 걱정은 한 번이면 족합니다."; return false; }
        ShrineUsed = true;
        combat.RestoreHealth(PrototypeCombat.MaxPlayerHealth);
        shrineSaid = "돌 하나를 얹었습니다. 지나간 이들의 걱정 위에 내 걱정도 하나.\n몸이 가벼워집니다. (체력 회복)";
        return true;
    }

    // 때가 되면 바위를 굴리고, 서낭당에서 E를 받는다.
    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused) return;
        if (NearShrine && Input.GetKeyDown(KeyCode.E)) UseShrine();
        // 선비가 고개에서 비탈 꼭대기 아래에 있을 때만 바위가 굴러온다.
        if (OnPass && combat.player.transform.position.x < WorldBuilder.RockStart - 1 && Time.time >= nextRock)
        {
            Spawn(WorldBuilder.RockStart);
            nextRock = Time.time + RockEvery;
        }
        Step(Time.deltaTime);
    }

    // 서낭당 안내를 그린다.
    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused) return;
        PrototypeKoreanUI.Apply();
        if (ShrineAt >= 0 && Time.time - ShrineAt < 3f)
            JinaUI.Toast(shrineSaid);
        if (NearShrine && !ShrineUsed) JinaUI.Prompt("E: 서낭당 돌무더기에 돌 얹기");
    }

    void OnDestroy() { foreach (var r in rocks) if (r.art != null) Destroy(r.art.gameObject); }
}

// ===== 길가 조사 지점 (RoadsideLore) =====
// 길가의 작은 것들이 E에 한두 줄로 답한다: 아무것도 여기에 달려 있지 않지만 어떤 곳인지
// 알려 주고, 콩밭 마을에서는 그곳 사람들이 믿는 것과 조용히 어긋난다.
public class RoadsideLore : MonoBehaviour
{
    public PrototypeCombat combat;
    public const float Range = 1.1f, ShowFor = 5f;
    public struct Spot
    {
        public float x; public string prompt, said, afterNight;
        public Spot(float x, string prompt, string said, string afterNight = null) { this.x = x; this.prompt = prompt; this.said = said; this.afterNight = afterNight; }
    }
    public static readonly Spot[] Spots =
    {
        // 비석 터.
        new Spot(-64f, "돌탑 살펴보기", "누군가 쌓다 만 돌탑이다. 망한 나라의 연호가 새겨진 돌이 맨 아래에 깔려 있다."),
        new Spot(-46f, "선돌 살펴보기", "글자는 닳아 없어졌는데, 사람 손이 닿은 자리만 반들반들하다. 지금도 찾아오는 이가 있는 모양이다."),
        // 망령의 들판.
        new Spot(-23.5f, "봉분 살펴보기", "이름 없는 봉분이다. 술 한 잔 올릴 사람도 없이 풀만 무성하다."),
        new Spot(-30f, "녹슨 창 살펴보기", "녹슨 창날이 땅에 박혀 있다. 이 창의 임자도 어쩌면 저 유령들 가운데 있을 것이다."),
        // 첫 마을.
        new Spot(4.5f, "기와집 문 두드리기", "대답이 없다. 문틈으로 곡식 찧는 소리만 들린다. 낯선 길손에게는 문을 열지 않는 모양이다."),
        new Spot(25f, "담장 너머 보기", "담 너머로 숲이 보인다. 해가 중천인데 숲 쪽만 저녁처럼 어둡다."),
        // 숲.
        new Spot(44f, "소나무 옹이 살펴보기", "옹이에 작은 그릇이 놓여 있다. 오래전 산의 주인에게 올리던 제물 그릇이다. 지금은 비어 있다."),
        new Spot(50f, "꺾인 가지 살펴보기", "굵은 가지가 맨손으로 꺾인 듯 찢겨 있다. 사람의 키보다 훨씬 높은 자리다."),
        // 나루터.
        new Spot(70f, "오두막 살펴보기", "낡은 노가 벽에 기대어 있다. 손잡이가 닳아 한 사람 손 모양으로 패었다."),
        new Spot(102.5f, "강가 돌 살펴보기", "젖은 돌 위에 여인의 신 한 짝이 가지런히 놓여 있다. 물에 떠밀려 온 것 치고는 너무 가지런하다."),
        // 콩밭 마을: 마을이 들려주는 이야기가 사람들 말과 다르다.
        new Spot(113f, "초가 살펴보기", "처마 밑에 말린 콩이 주렁주렁하다. 아이들 키를 잰 금이 기둥에 새겨져 있다. 잔치에 쓴다며 다들 곡식을 내놓는 중이다."),
        new Spot(119.5f, "우물 들여다보기", "두레박을 올리자 물에서 어렴풋이 짠내가 난다. 내륙 마을의 우물에서.", "두레박 끈에 낯익은 젓갈 냄새가 배어 있다. 간밤에 누가 여기서 무언가를 씻은 것 같다."),
        new Spot(128f, "빨랫줄 살펴보기", "괴롭힘 받는다는 콩쥐의 옷은 새것처럼 희고, 못됐다는 팥쥐의 옷은 소매마다 해져 기웠다."),
        new Spot(126f, "콩밭 살펴보기", "콩잎이 유난히 검푸르고 알이 굵다. 거름을 무얼 썼는지 물어보는 사람이 아무도 없다."),
        new Spot(138.5f, "평상 살펴보기", "두 사람 밥상이 차려져 있다. 한쪽 그릇은 비었고, 다른 쪽 그릇은 입도 대지 않은 채 식었다."),
        // 고개.
        new Spot(154f, "장승 살펴보기", "천하대장군, 지하여장군. 발치에 누가 숯으로 적어 놓았다. '이 고개는 해 지기 전에.'"),
        new Spot(176.5f, "서낭나무 살펴보기", "오색 천이 빽빽하다. 그중 하나에 삐뚤빼뚤한 글씨가 있다. '아버지 급제하게 해 주십시오.'"),
        new Spot(192.5f, "한양 쪽 바라보기", "구름 너머로 도성이 있을 터다. 길은 어둑한 뒷산 너머로 이어진다."),
        // 콩쥐를 만나기 전의 뒷산.
        new Spot(202.5f, "깨진 독 조각 살펴보기", "깨진 독 조각이 비탈에 흩어져 있다. 안쪽에 비늘 같은 것이 말라붙어 있다."),
        new Spot(211.5f, "발자국 살펴보기", "작은 맨발 자국이 비탈 위로 이어지다가… 어느 순간부터 물갈퀴 자국으로 바뀐다."),
    };

    // 가장 가까운 조사 지점 번호(없으면 -1).
    public int Near()
    {
        if (combat == null) return -1;
        var p = combat.player.transform.position;
        int best = -1; float bestD = Range;
        for (int i = 0; i < Spots.Length; i++)
        {
            float d = Mathf.Abs(p.x - Spots[i].x);
            if (d < bestD && p.y < WorldBuilder.Top(Spots[i].x) + 2.6f) { bestD = d; best = i; }
        }
        return best;
    }
    // 사람과 서낭당이 먼저: 근처에 다른 게 없을 때만 답한다.
    bool SomeoneElseHere()
    {
        var p = (Vector2)combat.player.transform.position;
        // 싸우는 중에도 안 나온다.
        if (combat.BossAlerted && !combat.EnemyDefeated) return true;
        if (combat.KongBoss != null && combat.KongBoss.Fighting) return true;
        foreach (var camp in combat.Camps)
            if (camp != null)
                foreach (var m in camp.mobs)
                    if (m.hp > 0 && m.art != null && Vector2.Distance(p, m.art.transform.position) < 6) return true;
        if (Vector2.Distance(p, WorldBuilder.Elder) < 1.9f || Vector2.Distance(p, WorldBuilder.Stone) < 1.9f) return true;
        if (Mathf.Abs(p.x - WorldBuilder.Tavern.x) < 2.2f || Mathf.Abs(p.x - WorldBuilder.Peddler.x) < 2.2f) return true;
        if (combat.Ferry != null && combat.Ferry.Near) return true;
        if (combat.Pass != null && combat.Pass.NearShrine) return true;
        if (combat.Kong != null && combat.Kong.InVillage && combat.Kong.HasSomethingHere) return true;
        if (combat.BossPurified && Vector2.Distance(p, combat.BossPosition) < 2.2f) return true;
        return false;
    }

    private float saidAt = -1;
    private string said = "";
    public void ResetLore() { saidAt = -1; said = ""; }

    // E로 조사 지점을 읽는다.
    void Update()
    {
        if (combat == null || combat.Paused || combat.PlayerHealth <= 0) return;
        if (!Input.GetKeyDown(KeyCode.E) || SomeoneElseHere()) return;
        int i = Near();
        if (i < 0) return;
        Read(i);
    }
    // 조사 지점 `i`의 글을 연다.
    public void Read(int i)
    {
        var s = Spots[i];
        said = s.afterNight != null && combat.Kong != null && combat.Kong.Stayed ? s.afterNight : s.said;
        saidAt = Time.time;
    }

    // 조사 글과 E 안내를 그린다.
    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused) return;
        PrototypeKoreanUI.Apply();
        if (saidAt >= 0 && Time.time - saidAt < ShowFor) JinaUI.Toast(said);
        int i = Near();
        if (i >= 0 && !SomeoneElseHere()) JinaUI.Prompt("E: " + Spots[i].prompt);
    }
}
