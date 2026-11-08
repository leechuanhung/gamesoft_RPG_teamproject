using UnityEngine;

// The ferry crossing east of the forest (PrototypeWorld.FerryStart..FerryEnd). The ferryman talks (E: next line,
// Esc: leave); the boat stays tied up while the forest's dokkaebi still rages, then shuttles across the river by
// itself and carries whoever stands on it. Falling into the river hurts and puts him back on the nearer bank.
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
    // He met the ferryman while the boat was still tied up, so the ferryman promised to take him across.
    public bool Promised { get; private set; }
    public PrototypeBoat Boat { get; private set; }
    private SpriteRenderer marker;
    private string[] lines;

    public bool Sailing => combat != null && combat.EnemyDefeated;
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
        ResetFerry();
    }

    public void ResetFerry()
    {
        IsOpen = false; Page = 0; SplashAt = -1; Promised = false;
        if (Boat != null) Boat.Moor(new Vector2(BoatLeft, Deck - DeckHeight));
    }

    // What he says now: the elder's death first if it happened, then the forest's state. Once the forest is quiet
    // he keeps his promise if he made one; meeting the seonbi only then, he works out what happened in the forest.
    public static string[] Lines(QuestProgress q, bool sailing, bool promised)
    {
        var said = new System.Collections.Generic.List<string>();
        if (q.ElderDead) said.Add("마을 노인장 소식은 바람결에 들었소… 그분은 젊어서 이 나루를 지키던 분이오.");
        if (!sailing)
        {
            said.Add("이 나루는 한양 가는 길목이오. 헌데 요새는 배를 못 띄우오.");
            said.Add("숲의 도깨비가 날뛴 뒤로 강까지 밤마다 울어 대니, 사공 노릇도 목숨 걸고 해야 하오.");
            said.Add("숲을 조용히 만들어 주시면, 그때는 삯도 안 받고 건네 드리리다.");
            return said.ToArray();
        }
        if (q.Outcome == EncounterOutcome.Purified)
        {
            said.Add(promised ? "도깨비를 달래 보내셨다고? 허허, 약속대로 배를 띄우리다. 강물 소리도 한결 순해졌구려."
                : "숲에서 내려오신 길이오? 강물 소리가 한결 순해졌다 했더니, 도깨비를 달래 보내셨구려.");
            said.Add("삯은 됐소. 이런 길손을 건네는 게 사공의 복이지.");
        }
        else
        {
            if (promised) said.Add("숲이 조용해졌소. 약속대로 배를 띄우리다.");
            else
            {
                said.Add("숲 쪽에서 오셨소? 요 며칠 도깨비 때문에 배를 못 띄웠는데, 숲이 조용해졌구려.");
                said.Add("선비가 그 도깨비를 쓰러뜨렸다고? …그렇다면 삯은 받지 않겠소.");
            }
            said.Add("다만… 칼로 끝낸 일은 강물처럼 다시 돌아오기도 하오. 조심해 건너시오.");
        }
        said.Add("배가 이쪽 기슭에 닿으면 올라타시오. 물에 빠지면 차갑소.");
        said.Add("강 건너 콩밭 마을 부잣집 안주인이 작년 이맘때 이 강에 빠졌소. 사람들은 천벌이라는데…");
        said.Add("내 눈엔 누가 민 것 같았소. 밤에 강에서 우는 소리는… 그 여인일지도 모르지.");
        return said.ToArray();
    }

    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) { IsOpen = false; return; }
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
        lines = Lines(combat.Quest.progress, Sailing, Promised); Page = 0; IsOpen = true;
        if (!Sailing) Promised = true;
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
        if (Near && !IsOpen && !combat.Paused)
            GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height - 55, 440, 35), "E: 뱃사공과 이야기하기");
        if (IsOpen && SpeechBubble.OnScreen(PrototypeWorld.Ferryman))
        {
            SpeechBubble.Draw(new Vector3(PrototypeWorld.Ferryman.x, 1.6f, 0), "뱃사공", lines[Page]);
            GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height - 55, 440, 35), "E: 다음 (" + (Page + 1) + "/" + lines.Length + ")   ·   Esc: 그만 듣기");
        }
        if (SplashAt >= 0 && Time.time - SplashAt < 1.6f)
            GUI.Box(new Rect(Screen.width / 2f - 220, 240, 440, 35), "풍덩! 차가운 강물에 빠졌다가 겨우 기슭으로 올라왔습니다.");
    }

    void OnDestroy()
    {
        if (marker != null) Destroy(marker.gameObject);
        if (Boat != null) Destroy(Boat.gameObject);
    }
}

// The ferry boat: moves bank to bank (once the ferry sails), waiting at each; the seonbi rides along on its deck.
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
        if (ferry == null || !ferry.Sailing || ferry.combat.Paused || Time.time < waitUntil) return;
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
