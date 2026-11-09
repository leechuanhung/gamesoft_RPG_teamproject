using UnityEngine;

// The ferry crossing east of the forest (PrototypeWorld.FerryStart..FerryEnd). Thorns at the forest's end bar the way
// until the dokkaebi is slain or purified, so he only ever arrives with that done. The ferryman talks (E: next line,
// Esc: leave); the boat shuttles across the river while he is on this map and carries whoever stands on it.
// Falling into the river hurts and puts him back on the nearer bank.
public class PrototypeFerry : MonoBehaviour
{
    public PrototypeCombat combat;
    // Boat ends (its centre), speed, wait at each bank; how deep he sinks before the river takes him; talk range.
    public const float BoatLeft = PrototypeWorld.RiverLeft + 2.1f, BoatRight = PrototypeWorld.RiverRight - 2.1f;
    public const float BoatSpeed = 2.4f, BoatWait = 1.6f, SinkY = -1.6f, TalkRange = 1.8f;
    // The deck's top (level with the banks) above the boat picture's bottom: 15 of its 22 rows.
    const float DeckHeight = 15f / 24f, Deck = -.5f;

    public bool IsOpen { get; private set; }
    public int Page { get; private set; }
    public float SplashAt { get; private set; } = -1;
    // The thorns: how long they take to wither once the forest is quiet, and when that began.
    public const float WitherLength = 1.2f, ClearedNotice = 4f;
    public float ClearedAt { get; private set; } = -1;
    public bool ThornsShut => thornWall != null && thornWall.enabled;
    private SpriteRenderer thorns;
    private BoxCollider2D thornWall;
    public PrototypeBoat Boat { get; private set; }
    private SpriteRenderer marker;
    private string[] lines;

    public bool PlayerHere => combat != null && combat.player.transform.position.x >= PrototypeWorld.FerryStart && combat.player.transform.position.x < PrototypeWorld.FerryEnd;
    bool AtThorns => ThornsShut && combat.player.transform.position.x > PrototypeWorld.FerryStart - 3;
    public bool Near => combat != null && Vector2.Distance(combat.player.transform.position, PrototypeWorld.Ferryman) < TalkRange;

    void Start()
    {
        marker = new GameObject("뱃사공").AddComponent<SpriteRenderer>();
        marker.transform.position = PrototypeWorld.Ferryman;
        marker.transform.localScale = new Vector3(.75f, 1.4f, 1);
        marker.sprite = combat.player.GetComponent<SpriteRenderer>().sprite;
        marker.color = new Color(.5f, .45f, .35f);   // a white placeholder would read as "just hit" and tint him
        JinaActorVisual.Attach(marker, JinaArt.Ferryman, combat);

        var boat = new GameObject("나룻배");
        var art = boat.AddComponent<SpriteRenderer>(); art.sprite = JinaArt.Prop("ferry_boat"); art.sortingOrder = 5;
        boat.layer = 8;
        var box = boat.AddComponent<BoxCollider2D>();
        box.size = new Vector2(3.7f, .25f); box.offset = new Vector2(0, DeckHeight - .125f);
        box.usedByEffector = true;
        var effector = boat.AddComponent<PlatformEffector2D>(); effector.useOneWay = true; effector.surfaceArc = 160;
        var body = boat.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        Boat = boat.AddComponent<PrototypeBoat>();
        Boat.ferry = this;

        var wall = new GameObject("숲 끝 가시덤불");
        wall.transform.position = new Vector3(PrototypeWorld.FerryStart - .5f, -.5f, 0);
        wall.layer = 8;
        thorns = wall.AddComponent<SpriteRenderer>(); thorns.sprite = JinaArt.Prop("thorn_wall"); thorns.sortingOrder = 7;
        thornWall = wall.AddComponent<BoxCollider2D>();
        thornWall.size = new Vector2(1, 30); thornWall.offset = new Vector2(0, 15);
        ResetFerry();
    }

    public void ResetFerry()
    {
        IsOpen = false; Page = 0; SplashAt = ClearedAt = -1;
        if (Boat != null) Boat.Moor(new Vector2(BoatLeft, Deck - DeckHeight));
        if (thornWall != null) { thornWall.enabled = true; thorns.enabled = true; thorns.color = Color.white; }
    }

    // What he says when the seonbi arrives, always with the forest already quiet: the elder's death first if it
    // happened, then what he makes of the forest, then the way across and the rumour about the river.
    public static string[] Lines(QuestProgress q)
    {
        var said = new System.Collections.Generic.List<string>();
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

    void Update()
    {
        if (combat == null) return;
        // The dokkaebi dealt with, the thorns stop blocking at once and wither away.
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
            else if (Input.GetKeyDown(KeyCode.E)) Next();
        }
        else if (Near && Input.GetKeyDown(KeyCode.E) && !combat.Paused) Open();
        Splash();
    }

    public void Open()
    {
        lines = Lines(combat.Quest.progress); Page = 0; IsOpen = true;
        combat.player.CanMove = false;
    }
    public void Next() { if (++Page >= lines.Length) Close(); }
    void Close() { IsOpen = false; combat.player.CanMove = combat.PlayerHealth > 0; }

    // In the river: a hit like any other blow, then back on the nearer bank.
    public void Splash()
    {
        var p = combat.player.transform.position;
        if (p.x <= PrototypeWorld.RiverLeft || p.x >= PrototypeWorld.RiverRight || p.y > SinkY) return;
        SplashAt = Time.time;
        combat.HurtPlayer();
        bool nearLeft = p.x - PrototypeWorld.RiverLeft < PrototypeWorld.RiverRight - p.x;
        var body = combat.player.GetComponent<Rigidbody2D>();
        combat.player.transform.position = new Vector3(nearLeft ? PrototypeWorld.RiverLeft - 1.2f : PrototypeWorld.RiverRight + 1.2f, .3f, 0);
        body.velocity = Vector2.zero;
    }

    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        PrototypeKoreanUI.Apply();
        if (Near && !IsOpen && !combat.Paused) JinaUI.Prompt("E: 뱃사공과 이야기하기");
        if (AtThorns && !combat.Paused) JinaUI.Prompt("가시덤불이 길을 막았습니다. 숲의 도깨비를 해결해야 지나갈 수 있습니다.");
        if (ClearedAt >= 0 && Time.time - ClearedAt < ClearedNotice && !combat.Paused && combat.Encounter != null && !combat.Encounter.Playing)
            JinaUI.Toast("숲 끝을 막던 가시덤불이 시들었습니다. 나루터로 가는 길이 열렸습니다.");
        if (IsOpen && SpeechBubble.OnScreen(PrototypeWorld.Ferryman))
        {
            SpeechBubble.Draw(new Vector3(PrototypeWorld.Ferryman.x, 1.6f, 0), "뱃사공", lines[Page]);
            JinaUI.Prompt("E: 다음 (" + (Page + 1) + "/" + lines.Length + ")   ·   Esc: 그만 듣기");
        }
        if (SplashAt >= 0 && Time.time - SplashAt < 1.6f)
            JinaUI.Toast("풍덩! 차가운 강물에 빠졌다가 겨우 기슭으로 올라왔습니다.");
    }

    void OnDestroy()
    {
        if (marker != null) Destroy(marker.gameObject);
        if (Boat != null) Destroy(Boat.gameObject);
        if (thorns != null) Destroy(thorns.gameObject);
    }
}

// The ferry boat: moves bank to bank while he is on the ferry map, waiting at each; he rides along on its deck.
public class PrototypeBoat : MonoBehaviour
{
    public PrototypeFerry ferry;
    public Vector2 Velocity { get; private set; }
    private Rigidbody2D body;
    private int heading = 1;
    private float waitUntil;

    public void Moor(Vector2 at)
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        transform.position = at; body.position = at;
        heading = 1; waitUntil = 0; Velocity = Vector2.zero;
    }

    void FixedUpdate()
    {
        Velocity = Vector2.zero;
        if (ferry == null || !ferry.PlayerHere || ferry.combat.Paused || Time.time < waitUntil) return;
        float x = body.position.x + heading * PrototypeFerry.BoatSpeed * Time.fixedDeltaTime;
        if (x >= PrototypeFerry.BoatRight || x <= PrototypeFerry.BoatLeft)
        {
            x = Mathf.Clamp(x, PrototypeFerry.BoatLeft, PrototypeFerry.BoatRight);
            heading = -heading; waitUntil = Time.time + PrototypeFerry.BoatWait;
        }
        Velocity = new Vector2((x - body.position.x) / Time.fixedDeltaTime, 0);
        body.MovePosition(new Vector2(x, body.position.y));
    }
}
