using UnityEngine;
using Who = PrototypeKongVillage.Who;

// The night at Patjwi's house, played as a cutscene the first time he finishes talking to Kongjwi (no control; E moves
// the evening's lines on, Space skips the room scenes).
//   Evening: Kongjwi asks him to stay. Patjwi, who knows what she is and dares not say it, tries a polite excuse to get
//   him away; Kongjwi only looks at her, and she shrinks back and gives in.
//   Night: the guest room, a small map of its own. Noises outside, and a shape thrown on the paper door.
//   Morning: he wakes and goes out; now the crocks can be searched and a paper lies beside them (PrototypeKongVillage).
public class PrototypeKongStay : MonoBehaviour
{
    public PrototypeCombat combat;
    public enum Phase { None, Evening, Night, Morning }
    public Phase Now { get; private set; }
    public bool Playing => Now != Phase.None;
    // Where the room is built, out of sight of the world; the camera moves in so its 10 x 5.5 units fill the screen.
    public const float RoomX = 400, RoomY = -.75f, RoomSize = 2.75f;
    public const float FadeLength = .8f, NightLength = 20f, MorningLength = 8f, DoorX = RoomX + 3.19f, DoorBaseY = RoomY + 1.62f;
    // Night's beats, in seconds.
    public const float ShadowIn = 8f, ShadowStop = 11f, SitAt = 11.5f, ShadowOut = 16f, ShadowGone = 17f, LieAt = 17.5f;

    public static readonly (Who who, string portrait, string line, float seconds)[] Evening =
    {
        (Who.Kongjwi, "kongjwi", "어머, 벌써 해가 저물었네요. 밤 산길은 위험해요. 오늘은 저희 집에서 묵고 가세요.", 4.2f),
        (Who.Patjwi, "patjwi", "…언니, 어머니도 안 계신 집에 외간 남자를 들이는 건 좀 그래요. 마을 사람들 보는 눈도 있고요.", 4.4f),
        (Who.Patjwi, "patjwi", "선비님도 갈 길이 바쁘실 텐데요. 고개 너머 주막까지 그리 멀지도 않고… 그렇죠?", 4.2f),
        (Who.Kongjwi, "kongjwi_stare", "……", 3.6f),
        (Who.Patjwi, "patjwi_down", "……", 2.2f),
        (Who.Patjwi, "patjwi_down", "…알았어요. 생각해 보니… 아니에요. …그냥, 자고 가시지요.", 4f),
        (Who.Kongjwi, "kongjwi", "동생이 손님을 어려워해서 그래요. 자, 이쪽이에요. 푹 쉬세요.", 3.8f),
    };

    private float startedAt, wokeAt = -10, savedSize;
    private int line;
    private bool inRoom;
    private GameObject room;
    private SpriteRenderer roomBack, roomDoor, doorShadow, bedUnder, body, quilt;
    // The bedding's left end and the mattress's top, from which he and the quilt are laid.
    const float BedLeft = RoomX - 2.08f, MattressTop = RoomY + .875f;

    public int Line => line;
    public float Elapsed => Time.time - startedAt;
    // From Kongjwi's stare on, Patjwi has shrunk back a step.
    public float Flinch => Now != Phase.Evening ? 0 : line > 3 ? 1 : line == 3 ? Mathf.Clamp01((Elapsed - 1.4f) / .5f) : 0;
    public bool InRoom => inRoom;
    public string BedSprite => body == null ? null : body.sprite.name;
    public SpriteRenderer BedBody => body;
    public SpriteRenderer BedQuilt => quilt;
    public Vector3 ShadowPosition => doorShadow.transform.position;

    void Start()
    {
        room = new GameObject("손님방");
        var black = Part("어둠", JinaArt.Dot(), -30, 0);
        black.color = Color.black;
        black.transform.position = new Vector3(RoomX, 2, 0);
        black.transform.localScale = Vector3.one * 200 / JinaArt.DotSize;
        roomDoor = Part("창호지", JinaArt.Prop("room_door"), -3, 0);
        doorShadow = Part("창호지 그림자", JinaArt.Prop("door_shadow"), -2, 0);
        roomBack = Part("방", JinaArt.Prop("room_bg"), -1, 0);
        bedUnder = Part("요와 베개", JinaArt.Prop("bed_under"), 0, -.67f);
        bedUnder.transform.position = new Vector3(RoomX - .67f, RoomY + .58f, 0);
        body = Part("잠자리 선비", JinaArt.Load("01_Player/player_sleep"), 1, 0);
        quilt = Part("이불", JinaArt.Prop("quilt_sleep"), 2, 0);
        room.SetActive(false);
    }
    SpriteRenderer Part(string label, Sprite sprite, int order, float dx)
    {
        var art = new GameObject(label).AddComponent<SpriteRenderer>();
        art.transform.SetParent(room.transform);
        art.transform.position = new Vector3(RoomX + dx, RoomY, 0);
        art.sprite = sprite; art.sortingOrder = order;
        return art;
    }

    public void ResetStay()
    {
        Now = Phase.None; line = 0; wokeAt = -10;
        ShowRoom(false);
    }
    public void Begin()
    {
        Now = Phase.Evening; line = 0; startedAt = Time.time;
        combat.player.CanMove = false;
    }
    // Jumps straight to a moment of the cutscene (for checks and screenshots).
    public void Show(Phase phase, float t)
    {
        Now = phase; startedAt = Time.time - t;
        combat.player.CanMove = false;
        ShowRoom(phase == Phase.Night || phase == Phase.Morning);
        Pose();
    }
    void ShowRoom(bool show)
    {
        if (show == inRoom) return;
        inRoom = show;
        if (room != null) room.SetActive(show);
        var cam = Camera.main;
        PrototypeView.FixedX = show ? RoomX : (float?)null;
        if (cam == null) return;
        if (show) { savedSize = cam.orthographicSize; cam.orthographicSize = RoomSize; }
        else cam.orthographicSize = savedSize > 0 ? savedSize : cam.orthographicSize;
    }

    void Update()
    {
        if (!Playing) return;
        if (Now == Phase.Evening)
        {
            bool next = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
            if (line < Evening.Length && (next || Elapsed >= Evening[line].seconds)) NextLine();
            else if (line >= Evening.Length && Elapsed >= FadeLength) { Now = Phase.Night; startedAt = Time.time; ShowRoom(true); }
            return;
        }
        // Space jumps to where the scene fades out.
        float length = Now == Phase.Night ? NightLength : MorningLength;
        if (Input.GetKeyDown(KeyCode.Space) && Elapsed < length - FadeLength) startedAt = Time.time - (length - FadeLength);
        Pose();
        if (Elapsed < length) return;
        if (Now == Phase.Night) { Now = Phase.Morning; startedAt = Time.time; Pose(); }
        else Wake();
    }
    public void NextLine() { line++; startedAt = Time.time; }

    // Out of the room at the house's door, in control again; the morning's discoveries open up.
    public void Wake()
    {
        Now = Phase.None; wokeAt = Time.time;
        ShowRoom(false);
        var player = combat.player;
        player.transform.position = new Vector3(PrototypeWorld.KongHouseX, .3f, 0);
        player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        player.CanMove = true;
        if (combat.Maps != null) combat.Maps.Sync();
        combat.Kong.WakeUp();
    }

    // The room at this moment: moonlit and blue at night, warm by morning; he lies down or sits up; the shape slides.
    void Pose()
    {
        if (roomBack == null) return;
        float t = Elapsed;
        bool night = Now == Phase.Night;
        var tint = night ? new Color(.42f, .46f, .66f) : Color.white;
        roomBack.color = tint; bedUnder.color = tint; body.color = tint; quilt.color = tint;
        roomDoor.color = night ? new Color(.62f, .68f, .9f) : new Color(1f, .95f, .8f);
        bool sitting = !night || (t >= SitAt && t < LieAt);
        body.sprite = JinaArt.Load(sitting ? "01_Player/player_sit" : "01_Player/player_sleep");
        quilt.sprite = JinaArt.Prop(sitting ? "quilt_sit" : "quilt_sleep");
        // Sleeping: his back on the mattress, head on the pillow, the quilt from below the collar to the mattress's end.
        // Sitting: his in-game feet line is set so the quilt hides the cut at his lap.
        if (sitting)
        {
            float top = MattressTop + 11f / 24;
            body.transform.position = new Vector3(BedLeft + 1.05f, top - (535f - 305f) / body.sprite.pixelsPerUnit, 0);
            quilt.transform.position = new Vector3(BedLeft + 1.6f, MattressTop - 2f / 24, 0);
        }
        else
        {
            var at = new Vector3(BedLeft + .2f - (body.sprite.bounds.min.x), MattressTop - 3f / 24 - body.sprite.bounds.min.y, 0);   // his head reaches the pillow; the robe's sleeves lie on the mattress
            body.transform.position = at;
            quilt.transform.position = new Vector3(BedLeft + .2f + (205f - 40f) / body.sprite.pixelsPerUnit + 25f / 24, MattressTop - 2f / 24, 0);
        }
        doorShadow.enabled = night && t >= ShadowIn && t < ShadowGone;
        if (doorShadow.enabled)
        {
            // In from the right, stopping square on the door; then gone to the left, quickly.
            float x = t < ShadowStop ? Mathf.Lerp(DoorX + 3.6f, DoorX, (t - ShadowIn) / (ShadowStop - ShadowIn))
                : t < ShadowOut ? DoorX : Mathf.Lerp(DoorX, DoorX - 3.6f, (t - ShadowOut) / (ShadowGone - ShadowOut));
            doorShadow.transform.position = new Vector3(x, DoorBaseY, 0);
            doorShadow.color = new Color(.12f, .13f, .24f);
        }
    }

    void Black(float a)
    {
        if (a > 0) JinaUI.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, Mathf.Clamp01(a)));
    }
    static void Caption(string s)
    {
        float w = Mathf.Min(760, Screen.width - 40), h = JinaUI.Height(s, 18, w - 32) + 22;
        var r = new Rect((Screen.width - w) / 2, Screen.height - h - 20, w, h);
        JinaUI.Fill(r, new Color(0, 0, 0, .72f));
        JinaUI.Text(new Rect(r.x + 16, r.y + 11, w - 32, h - 22), s, 18, JinaUI.Bright, TextAnchor.UpperCenter);
    }
    // A noise from beyond the door, written large and shivering, as if heard rather than seen.
    static void Noise(string s, float t, float from, float to)
    {
        if (t < from || t >= to) return;
        float a = Mathf.Clamp01((t - from) / .3f) * Mathf.Clamp01((to - t) / .4f);
        int size = Mathf.Clamp(Screen.height / 20, 20, 44);
        float jitter = Mathf.Sin(t * 37) * 1.5f;
        var at = Camera.main.WorldToScreenPoint(new Vector3(DoorX - 2.1f, RoomY + 4.4f, 0));
        JinaUI.Text(new Rect(at.x - 250 + jitter, Screen.height - at.y - size, 500, size * 2), s, size, new Color(.92f, .9f, .98f, a), TextAnchor.MiddleCenter, true);
    }

    void OnGUI()
    {
        GUI.depth = -60;
        PrototypeKoreanUI.Apply();
        float sinceWake = Time.time - wokeAt;
        if (!Playing)
        {
            if (sinceWake < FadeLength) Black(1 - sinceWake / FadeLength);
            return;
        }
        float t = Elapsed;
        if (Now == Phase.Evening)
        {
            JinaUI.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(.75f, .32f, .12f, .22f));
            if (line < Evening.Length)
            {
                var (who, portrait, words, _) = Evening[line];
                PrototypeKongVillage.TalkWindow(who, portrait, words, "E 다음", false);
            }
            else Black(t / FadeLength);
            return;
        }
        var head = new Vector3(BedLeft + 1.1f, MattressTop + 1.95f, 0);
        if (Now == Phase.Night)
        {
            if (t >= .6f && t < 4.4f) Caption("그날 밤, 콩쥐네 손님방.");
            Noise("쓱… 쓱…", t, 5f, 7.6f);
            Noise("달그락…", t, 7.8f, 10f);
            Noise("콰직… 우물우물…", t, 10f, 12.4f);
            if (t >= 12.6f && t < 15.2f) SpeechBubble.Draw(head, "선비", "…밖에 누구요?");
            if (t >= LieAt && t < NightLength - FadeLength) Caption("대답은 없었다. 선비는 애써 눈을 감았다.");
            Black(Mathf.Max(1 - t / FadeLength, (t - (NightLength - FadeLength)) / FadeLength));
        }
        else
        {
            Noise("꼬끼오—", t, .8f, 3.2f);
            if (t >= 1.2f && t < 3.4f) Caption("아침이 밝았다.");
            if (t >= 3.5f && t < 6.9f) SpeechBubble.Draw(head, "선비", "밤새 들리던 그 소리는 무엇이었을까… 집 둘레나 한번 돌아봐야겠군.");
            Black(Mathf.Max(1 - t / FadeLength, (t - (MorningLength - FadeLength)) / FadeLength));
        }
        JinaUI.Text(new Rect(0, 8, Screen.width - 14, 18), "스페이스: 건너뛰기", 12, JinaUI.Faint, TextAnchor.UpperRight);
    }

    void OnDestroy() { if (room != null) Destroy(room); }
}
