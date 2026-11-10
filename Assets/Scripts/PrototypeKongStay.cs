using UnityEngine;
using Who = PrototypeKongVillage.Who;

// The night at Patjwi's house, played as a cutscene the first time he finishes talking to Kongjwi (no control; E moves
// the evening's lines on, Space skips the night and the morning). It is painted, not played: the user's GPT pictures
// (18_Night, cut by Tools/ArtGen/make_night.py) in a cinema frame of 2.4 : 1 between black bars, layered and moved here.
//   Evening: the house at sunset, three figures on the road. Kongjwi asks him to stay. Patjwi, who knows what she is and
//   dares not say it, tries to drive him off in the only voice she is allowed, shouting; the picture cuts to Kongjwi's eyes,
//   and Patjwi shrinks back and gives in.
//   Night: the house under the moon, the lit door going dark. The guest room: noises outside, a shape thrown on the paper,
//   him sitting up. The close shot of the door: the shape looms, a wet hole opens in the paper, an eye looks in, and it goes.
//   Morning: dawn from the road, a rooster on the wall. The room in sunlight; the hole is still there, with a beam through it.
//   Parting: Kongjwi at the door begs him to stay longer. He chooses how to refuse: politely, or to her face. Either way a
//   silence, and then she asks him up to the back hill.
public class PrototypeKongStay : MonoBehaviour
{
    public PrototypeCombat combat;
    public enum Phase { None, Evening, Night, Morning, Parting }
    public Phase Now { get; private set; }
    public bool Playing => Now != Phase.None;
    // Where the pictures are hung, out of sight of the world. A picture is 1280 x 533 pixels, 128 to the unit (10 units
    // wide); the camera moves in until a 16:9 screen of that width fits exactly, and the camera always looks at y = 2.
    // Black bars cover all round the picture, so a picture moved or zoomed never shows past its frame.
    public const float FrameW = 1280, FrameH = 533, PixelsPerUnit = 128;
    public const float RoomX = 400, RoomY = 2, PictureWidth = FrameW / PixelsPerUnit, RoomSize = 2.8125f;
    public const float FadeLength = .8f, NightLength = 37f, MorningLength = 9.8f;
    // Night's beats, in seconds.
    public const float RoomAt = 6f, LampOutAt = 3.4f, ShadowIn = 13f, ShadowStop = 16f, SitAt = 18.5f;
    public const float CloseAt = 23f, HoleAt = 25f, EyeAt = 26.6f, EyeGone = 29.2f, CloseEnd = 30.4f, LieAt = 31.5f;
    // Morning's: the road at dawn, then the room.
    public const float MorningRoomAt = 4.6f;

    public static readonly (Who who, string portrait, string line, float seconds)[] Evening =
    {
        (Who.Kongjwi, "kongjwi", "어머, 벌써 해가 저물었네요. 밤 산길은 위험해요. 오늘은 저희 집에서 묵고 가세요.", 4.2f),
        (Who.Patjwi, "patjwi_angry", "무슨 소리야! 어머니도 안 계신 집에 외간 남자를 들이려고 하다니! 넌 부끄럽지도 않아?!", 4.4f),
        (Who.Patjwi, "patjwi_angry", "당신도 당장 가요! 고개 너머 주막까지 금방이니까, 해 지기 전에 어서요!", 4.2f),
        (Who.Kongjwi, "kongjwi_stare", "……", 3.6f),
        (Who.Patjwi, "patjwi_down", "……", 2.2f),
        (Who.Patjwi, "patjwi_down", "…마, 마음대로 해. 내가 무슨 상관이야. …자고 가든지.", 4f),
        (Who.Kongjwi, "kongjwi", "동생이 손님을 어려워해서 그래요. 자, 이쪽이에요. 푹 쉬세요.", 3.8f),
    };
    // The morning's talk, line by line. No portrait means he is the one speaking; "stare" cuts to the picture of her eyes.
    public static readonly (string portrait, string line)[] PartingAsk =
    {
        ("kongjwi", "일어나셨어요? 간밤엔 쥐 소리에 시끄러우셨죠. 안색이 안 좋으세요."),
        ("kongjwi", "먼 길 가시는데 그리 서두르실 것 없어요. 며칠 더 푹 쉬다 가세요. 보름 잔치도 보시고요."),
    };
    public static readonly (string portrait, string line)[] PartingPolite =
    {
        (null, "말씀은 고맙소만, 과거 날이 촉박하여 오늘은 길을 나서야 하오."),
        ("kongjwi", "그러세요… 아쉬워라."),
        ("kongjwi_stare", "……"),
        ("kongjwi", "그럼 부탁 하나만 드려도 될까요? 떠나시기 전에, 이따가 뒷산에 잠시만 와 주세요."),
        ("kongjwi", "돌아가신 어머니 일로 선비님께만 여쭐 것이 있어서요. 네? 꼭이요."),
        (null, "뒷산이라… 떠나기 전에 집 둘레나 한번 돌아봐야겠군."),
    };
    public static readonly (string portrait, string line)[] PartingBlunt =
    {
        (null, "어젯밤 창호지에 구멍을 뚫고 들여다본 이가 누구요? 이 집은 아무래도 수상하오."),
        ("stare", ""),
        ("kongjwi", "…어머. 쥐가 갉았나 봐요. 이 집엔 쥐가 많거든요."),
        ("kongjwi_stare", "……"),
        ("kongjwi", "그런 오해를 사고는 못 보내 드리지요. 이따가 뒷산으로 잠시 와 주세요. 거기서 다 말씀드릴게요."),
        ("kongjwi", "혼자 오세요, 네? 사람들이 들으면 안 되는 이야기니까요."),
        (null, "…곱게 보내 줄 것 같지는 않군. 가기 전에 집 둘레부터 살펴봐야겠어."),
    };
    public const string PoliteChoice = "1   정중히 거절한다", BluntChoice = "2   대놓고 의심한다";
    const float StareCut = 3f;
    const int StareLine = 3;

    private float startedAt, wokeAt = -10, savedSize;
    private int line;
    private bool inRoom;
    private GameObject room;
    private SpriteRenderer[] layers;
    private int used;
    private SpriteRenderer[] motes;

    public int Line => line;
    public float Elapsed => Time.time - startedAt;
    // From Kongjwi's stare on, Patjwi has shrunk back a step.
    public float Flinch => Now != Phase.Evening ? 0 : line > StareLine ? 1 : line == StareLine ? Mathf.Clamp01((Elapsed - 1.4f) / .5f) : 0;
    public bool InRoom => inRoom;
    // What each named part of the picture is showing now (for checks).
    public string BedSprite { get; private set; }
    public float ShadowX { get; private set; }
    public bool HoleShown { get; private set; }
    public bool EyeShown { get; private set; }
    public bool ShadowShown { get; private set; }
    // The world position of a pixel on a picture (x right, y down from its top left).
    static Vector3 Dot(float x, float y) => new Vector3(RoomX + (x - FrameW / 2) / PixelsPerUnit, RoomY + (FrameH / 2 - y) / PixelsPerUnit, 0);
    // The slow push-in of a shot: every picture is scaled by `zoom` about the pixel `pivot`.
    private float zoom = 1;
    private Vector2 pivot = new Vector2(FrameW / 2, FrameH / 2);
    Vector3 At(float x, float y) => Dot(pivot.x + (x - pivot.x) * zoom, pivot.y + (y - pivot.y) * zoom);
    void View(float z, float px, float py) { zoom = z; pivot = new Vector2(px, py); }

    void Start()
    {
        room = new GameObject("하룻밤 그림");
        var black = new GameObject("어둠").AddComponent<SpriteRenderer>();
        black.transform.SetParent(room.transform);
        black.sprite = JinaArt.Dot(); black.color = Color.black; black.sortingOrder = 100;
        black.transform.position = new Vector3(RoomX, RoomY, 0);
        black.transform.localScale = Vector3.one * 200 / JinaArt.DotSize;
        // The bars: above, below, left and right of the frame, in front of everything in it.
        float hw = FrameW / 2 / PixelsPerUnit, hh = FrameH / 2 / PixelsPerUnit, deep = 40;
        foreach (var (x, y, w, h) in new[] { (0f, hh + deep / 2, deep * 2, deep), (0f, -hh - deep / 2, deep * 2, deep), (hw + deep / 2, 0f, deep, deep * 2), (-hw - deep / 2, 0f, deep, deep * 2) })
        {
            var bar = new GameObject("검은 띠").AddComponent<SpriteRenderer>();
            bar.transform.SetParent(room.transform);
            bar.sprite = JinaArt.Dot(); bar.color = Color.black; bar.sortingOrder = 130;
            bar.transform.position = new Vector3(RoomX + x, RoomY + y, 0);
            bar.transform.localScale = new Vector3(w, h, 1) / JinaArt.DotSize;
        }
        layers = new SpriteRenderer[16];
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i] = new GameObject("그림 " + i).AddComponent<SpriteRenderer>();
            layers[i].transform.SetParent(room.transform);
            layers[i].sortingOrder = 101 + i;
        }
        motes = new SpriteRenderer[24];
        for (int i = 0; i < motes.Length; i++)
        {
            motes[i] = new GameObject("먼지 " + i).AddComponent<SpriteRenderer>();
            motes[i].transform.SetParent(room.transform);
            motes[i].sprite = JinaArt.Dot(); motes[i].sortingOrder = 120;
            motes[i].transform.localScale = Vector3.one * (1 / 64f) / JinaArt.DotSize * (i % 3 == 0 ? 2 : 1);
        }
        room.SetActive(false);
    }

    public void ResetStay()
    {
        Now = Phase.None; line = 0; wokeAt = -10; Blunt = false;
        ShowRoom(false);
    }
    public void Begin()
    {
        Now = Phase.Evening; line = 0; startedAt = Time.time;
        combat.player.CanMove = false;
        ShowRoom(true);
        Pose();
    }
    // Jumps straight to a moment of the cutscene (for checks and screenshots).
    public void Show(Phase phase, float t)
    {
        if (phase == Phase.Parting) BeginParting();
        Now = phase; startedAt = Time.time - t;
        combat.player.CanMove = false;
        ShowRoom(phase != Phase.None);
        var cam = Camera.main;
        if (cam != null && phase != Phase.None) cam.orthographicSize = FitSize(cam);
        Pose();
    }
    // The size that shows one whole picture whatever the window's shape; the black behind fills any spare margin.
    static float FitSize(Camera cam) => Mathf.Max(RoomSize, PictureWidth / 2 / Mathf.Max(.1f, cam.aspect));
    void ShowRoom(bool show)
    {
        if (show == inRoom) return;
        inRoom = show;
        if (room != null) room.SetActive(show);
        var cam = Camera.main;
        PrototypeView.FixedX = show ? RoomX : (float?)null;
        if (cam == null) return;
        if (show) { savedSize = cam.orthographicSize; cam.orthographicSize = FitSize(cam); }
        else cam.orthographicSize = savedSize > 0 ? savedSize : cam.orthographicSize;
    }

    void Update()
    {
        if (!Playing) return;
        var cam = Camera.main;
        if (cam != null) cam.orthographicSize = FitSize(cam);
        if (Now == Phase.Parting) { UpdateParting(); return; }
        if (Now == Phase.Evening)
        {
            bool next = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
            // A press while the words are still appearing shows them whole; the next moves on.
            if (next && line < Evening.Length && !typedAll && JinaUI.Visible(Evening[line].line, Elapsed) < Evening[line].line.Length) { typedAll = true; next = false; }
            if (line < Evening.Length && (next || Elapsed >= Evening[line].seconds)) NextLine();
            else if (line >= Evening.Length && Elapsed >= FadeLength) { Now = Phase.Night; startedAt = Time.time; }
            Pose();
            return;
        }
        // Space jumps to where the scene fades out.
        float length = Now == Phase.Night ? NightLength : MorningLength;
        if (Input.GetKeyDown(KeyCode.Space) && Elapsed < length - FadeLength) startedAt = Time.time - (length - FadeLength);
        Pose();
        if (Elapsed < length) return;
        if (Now == Phase.Night) Now = Phase.Morning;
        else BeginParting();
        startedAt = Time.time; Pose();
    }

    // The morning's talk: her plea, his answer (the only choice in the cutscene), the silence, the hill.
    public bool Blunt { get; private set; }
    public (string portrait, string line)[] PartingLines { get; private set; }
    public int PartingPage { get; private set; }
    // She has asked; he has not yet answered.
    public bool Choosing => Now == Phase.Parting && PartingLines == PartingAsk && PartingPage >= PartingAsk.Length - 1 && !leaving;
    private bool leaving;
    private float leftAt;
    (string portrait, string line) CurrentPart => PartingLines[PartingPage];
    void BeginParting()
    {
        Now = Phase.Parting; PartingLines = PartingAsk; PartingPage = 0; leaving = false; typedAll = false; startedAt = Time.time;
    }
    public void Choose(bool blunt)
    {
        if (!Choosing) return;
        Blunt = blunt; PartingLines = blunt ? PartingBlunt : PartingPolite; PartingPage = 0; typedAll = false; startedAt = Time.time;
    }
    public void NextPart()
    {
        if (Choosing || leaving) return;
        if (PartingPage + 1 < PartingLines.Length) { PartingPage++; typedAll = false; startedAt = Time.time; }
        else { leaving = true; leftAt = Time.time; }
    }
    void UpdateParting()
    {
        Pose();
        if (leaving) { if (Time.time - leftAt >= FadeLength) Wake(); return; }
        if (Choosing)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Choose(false);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) Choose(true);
            return;
        }
        // The cut to her eyes holds by itself for a moment, and cannot be hurried past.
        if (CurrentPart.portrait == "stare") { if (Elapsed >= StareCut) NextPart(); return; }
        if (!Input.GetKeyDown(KeyCode.E) && !Input.GetKeyDown(KeyCode.Space)) return;
        if (!typedAll && JinaUI.Visible(CurrentPart.line, Elapsed) < CurrentPart.line.Length) typedAll = true;
        else NextPart();
    }
    private bool typedAll;
    public void NextLine() { line++; startedAt = Time.time; typedAll = false; }

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
        combat.Kong.WakeUp(Blunt);
    }

    // Lays the next picture on the stack; dx and dy move it, in pixels.
    SpriteRenderer Put(string name, float alpha = 1, float dx = 0, float dy = 0)
    {
        var r = layers[used++];
        r.enabled = true; r.sprite = JinaArt.Night(name);
        r.color = new Color(1, 1, 1, alpha);
        r.transform.position = At(FrameW / 2 + dx, FrameH / 2 + dy);
        r.transform.localScale = Vector3.one * zoom;
        return r;
    }
    // A shape thrown on paper (drawn white): centred on pixel (x, y), `size` times its own size, in the dark of the night.
    SpriteRenderer Shade(string name, float x, float y, float size, float alpha)
    {
        var r = Put(name);
        r.color = new Color(.07f, .08f, .2f, alpha);
        r.transform.position = At(x, y);
        r.transform.localScale = Vector3.one * zoom * size;
        return r;
    }

    // The picture at this moment, rebuilt from its layers.
    void Pose()
    {
        if (layers == null) return;
        foreach (var r in layers) r.enabled = false;
        foreach (var m in motes) m.enabled = false;
        used = 0; ShadowShown = HoleShown = EyeShown = false;
        View(1, FrameW / 2, FrameH / 2);
        float t = Elapsed;
        if (Now == Phase.Evening) PoseEvening(t);
        else if (Now == Phase.Night) { if (t < RoomAt) PoseMoonlitHouse(t); else if (t >= CloseAt && t < CloseEnd) PoseDoor(t); else PoseRoomAtNight(t); }
        else if (Now == Phase.Morning) { if (t < MorningRoomAt) PoseDawn(t); else PoseRoomAtMorning(t); }
        else if (Now == Phase.Parting) { if (!leaving && CurrentPart.portrait == "stare") Put("stare"); else PoseRoomAtMorning(Time.time); }
    }

    void PoseEvening(float t)
    {
        if (line == StareLine) { Put("stare"); return; }
        Put("ext_evening");
        // She shrinks back from her sister: the picture of her cowering fades over her.
        if (Flinch > 0) Put("ext_evening_flinch", Flinch);
    }

    void PoseMoonlitHouse(float t)
    {
        // Drawing slowly in on the lit door.
        View(1 + t * .007f, 560, 250);
        Put("ext_night");
        // The lamp gutters, then goes out: the same house with that door dark, laid over it.
        float dark = t < LampOutAt ? Mathf.Clamp01((Mathf.Sin(t * 9) + Mathf.Sin(t * 13.7f)) * .06f) : Mathf.Clamp01((t - LampOutAt) / .4f);
        if (dark > 0) Put("ext_night_dark", dark);
        // Fireflies over the yard.
        for (int i = 0; i < 8; i++)
        {
            float x = 90 + i * 150 + Mathf.Sin(t * .7f + i * 1.9f) * 30, y = 470 - i % 3 * 22 + Mathf.Sin(t * 1.1f + i) * 14;
            float glow = Mathf.Clamp01(Mathf.Sin(t * 1.7f + i * 2.4f) * 1.4f);
            Mote(i, x, y, new Color(.86f, 1, .5f, glow));
        }
    }

    // Where the shape stands on the paper of the guest-room door, and how far right it starts.
    public const float ShadowRestX = 1095, ShadowY = 225, ShadowFrom = 420;
    // Its arm while it eats: down, to its mouth, at its mouth, back.
    static readonly int[] Chew = { 0, 1, 2, 1 };
    void PoseRoomAtNight(float t)
    {
        bool sitting = t >= SitAt + .3f && t < LieAt;
        bool rising = t >= SitAt && t < SitAt + .3f, lowering = t >= LieAt && t < LieAt + .4f;
        string pose = sitting ? "sit" : rising || lowering ? "prop" : "lie";
        BedSprite = pose;
        Put("room_night");
        // The shape: in from the right, stopping square on the door; a hand to its mouth while it chews. It falls on
        // the paper only: the room less its panes is laid over it.
        bool gone = t >= CloseEnd;
        if (t >= ShadowIn && !gone)
        {
            ShadowShown = true;
            float x = t < ShadowStop ? Mathf.Lerp(ShadowFrom, 0, (t - ShadowIn) / (ShadowStop - ShadowIn)) : 0;
            ShadowX = x;
            int frame = t < ShadowStop ? 0 : (int)((t - ShadowStop) / .34f) % 4;
            // A slow heave as it breathes.
            float heave = t >= ShadowStop ? Mathf.Sin(t * 2.4f) * 3 : 0;
            Shade("shadow_" + (t >= SitAt ? 0 : Chew[frame]), ShadowRestX + x, ShadowY - heave, 1.05f, .82f);
        }
        Put("room_night_front");
        // The moonlight on the floor goes when the shape stands in its way.
        float blocked = t < ShadowIn ? 0 : t < ShadowStop ? (t - ShadowIn) / (ShadowStop - ShadowIn) : gone ? 0 : 1;
        if (blocked > 0) Put("room_night_floor", blocked * .7f);
        // Sitting up, staring at the door: the picture of it fades in over him asleep, and out again when he lies down.
        float up = sitting ? 1 : rising ? (t - SitAt) / .3f : lowering ? 1 - (t - LieAt) / .4f : 0;
        if (up > 0) Put("room_night_sit", up);
        // Dust turning in the moonbeam.
        for (int i = 0; i < 10; i++)
        {
            float life = (t * .06f + i * .137f) % 1;
            float x = 760 + (i * 61) % 360 + Mathf.Sin(t * .5f + i) * 14 - life * 80, y = 120 + life * 330;
            Mote(i, x, y, new Color(.74f, .82f, 1, .5f * Mathf.Sin(life * Mathf.PI) * (1 - blocked * .8f)));
        }
    }

    void PoseDoor(float t)
    {
        // Drawing in on the panes where the hole will open.
        View(1 + (t - CloseAt) * .012f, 590, 240);
        Put("close_paper");
        if (t >= HoleAt)
        {
            HoleShown = true;
            Put("close_hole_" + (t < HoleAt + .5f ? 0 : t < HoleAt + 1f ? 1 : 2));
        }
        // It looms, its head filling the panes, trembling a little; then it is gone, quickly, to the left.
        ShadowShown = true;
        float lean = Mathf.Clamp01((t - CloseAt) / 1.6f);
        float x = 640 + Mathf.Sin(t * 1.3f) * 4 - (t >= EyeGone ? (t - EyeGone) * (t - EyeGone) * 1150 : 0);
        float y = 470 + (1 - lean) * 330 + Mathf.Sin(t * 2.4f) * 3;
        Shade("close_shadow", x, y, 2f, .78f);   // drawn at twice the size of the others
        Put("close_front");
        if (t >= EyeAt && t < EyeGone)
        {
            EyeShown = true;
            float e = t - EyeAt;
            // It opens, looks, blinks once, slowly, and stays until it has seen enough.
            int frame = e < .12f ? 2 : e < .24f ? 1 : e > 1.45f && e < 1.55f ? 1 : e >= 1.55f && e < 1.7f ? 2 : e >= 1.7f && e < 1.8f ? 1 : 0;
            Put("close_eye_" + frame);
        }
    }

    void PoseDawn(float t)
    {
        // Easing back from the wall where the rooster stands.
        View(1.05f - t * .01f, 1000, 330);
        Put("ext_morning");
        if (t >= .9f && t < 3.2f) Put("ext_rooster");
    }

    void PoseRoomAtMorning(float t)
    {
        BedSprite = "sit";
        Put("room_morning");
        // Dust lit gold, drifting down through the sun from the door.
        for (int i = 0; i < motes.Length; i++)
        {
            float life = (t * .05f + i * .071f) % 1;
            float x = 700 + (i * 53) % 520 + Mathf.Sin(t * .4f + i) * 16 - life * 90, y = 80 + life * 400;
            Mote(i, x, y, new Color(1, .94f, .7f, .7f * Mathf.Sin(life * Mathf.PI)));
        }
    }

    void Mote(int i, float x, float y, Color colour)
    {
        var m = motes[i];
        m.enabled = true; m.color = colour;
        m.transform.position = At(Mathf.Round(x), Mathf.Round(y));
    }

    void Black(float a)
    {
        if (a > 0) JinaUI.Fill(new Rect(0, 0, JinaUI.W, JinaUI.H), new Color(0, 0, 0, Mathf.Clamp01(a)));
    }
    // Narration under the picture: a line of plain words on black, typed out.
    static void Caption(string s, float since)
    {
        float w = Mathf.Min(760, JinaUI.W - 40), h = JinaUI.Height(s, 19, w - 36) + 24;
        var r = new Rect((JinaUI.W - w) / 2, JinaUI.H - h - 26, w, h);
        JinaUI.Fill(r, new Color(.02f, .02f, .05f, .76f));
        JinaUI.Text(new Rect(r.x + 18, r.y + 12, w - 36, h - 24), s, 19, JinaUI.Bright, TextAnchor.UpperCenter, false, JinaUI.Visible(s, since));
    }
    // A noise from beyond the door, written large and shivering, as if heard rather than seen.
    static void Noise(string s, float t, float from, float to, float x = .3f, float y = .1f)
    {
        if (t < from || t >= to) return;
        float a = Mathf.Clamp01((t - from) / .3f) * Mathf.Clamp01((to - t) / .4f);
        int size = 34;
        float jitter = Mathf.Sin(t * 37) * 1.5f, jitterY = Mathf.Sin(t * 29) * 1f;
        JinaUI.Text(new Rect(JinaUI.W * x - 300 + jitter, JinaUI.H * y - size + jitterY, 600, size * 2), s, size, new Color(.92f, .9f, .98f, a), TextAnchor.MiddleCenter, true);
    }
    // Over his head in the picture, wherever the window's shape has put it.
    static Vector3 HeadAbove => Dot(385, 95);

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
            if (line < Evening.Length)
            {
                var (who, portrait, words, _) = Evening[line];
                // Her stare has no window: the eyes alone, and the silence.
                if (line != StareLine) PrototypeKongVillage.TalkWindow(who, portrait, words, "E 다음", false, typedAll ? 1000 : t, true);
                if (line == 0) Black(1 - t / FadeLength);
            }
            else Black(t / FadeLength);
            return;
        }
        if (Now == Phase.Parting) { DrawParting(t); return; }
        if (Now == Phase.Night)
        {
            if (t >= .9f && t < RoomAt - .8f) Caption("그날 밤, 콩쥐네 손님방.", t - .9f);
            Noise("쓱… 쓱…", t, 8f, 10.6f);
            Noise("달그락…", t, 10.8f, 13f);
            Noise("콰직… 우물우물…", t, 15.4f, 18.2f);
            if (t >= SitAt + .6f && t < CloseAt - .2f) SpeechBubble.Draw(HeadAbove, "선비", "…밖에 누구요?", null, t - SitAt - .6f);
            if (t >= LieAt + .6f && t < NightLength - FadeLength) Caption("대답은 없었다. 선비는 애써 눈을 감았다.", t - LieAt - .6f);
            Black(Mathf.Max(Mathf.Max(1 - t / FadeLength, (t - (NightLength - FadeLength)) / FadeLength), Dip(t, RoomAt, CloseAt, CloseEnd)));
        }
        else
        {
            Noise("꼬끼오—", t, .9f, 3.2f, .5f, .14f);
            if (t >= 1.4f && t < MorningRoomAt - .5f) Caption("아침이 밝았다.", t - 1.4f);
            if (t >= MorningRoomAt + 1 && t < MorningLength - .1f) SpeechBubble.Draw(HeadAbove, "선비", "…창호지에 구멍이? 어젯밤엔 분명 없었는데.", null, t - MorningRoomAt - 1);
            Black(Mathf.Max(1 - t / FadeLength, Dip(t, MorningRoomAt)));
        }
        JinaUI.Text(new Rect(0, 8, JinaUI.W - 14, 18), "스페이스: 건너뛰기", 12, JinaUI.Faint, TextAnchor.UpperRight);
    }
    void DrawParting(float t)
    {
        if (leaving) { Black((Time.time - leftAt) / FadeLength); return; }
        var (portrait, words) = CurrentPart;
        float since = typedAll ? 1000 : t;
        if (portrait == "stare") return;   // her eyes, and nothing said
        if (portrait == null) { SpeechBubble.Draw(HeadAbove, "선비", words, null, since); JinaUI.Prompt("E: 다음"); return; }
        var box = PrototypeKongVillage.TalkWindow(Who.Kongjwi, portrait, words, Choosing ? "" : "E 다음", false, since);
        if (!Choosing || since < 1000 && JinaUI.Visible(words, since) < words.Length) return;
        // His answer, once she has finished asking: two buttons over the window, to the right of her portrait.
        float bx = box.x + 280, bw = box.xMax - bx - 14, bh = 44;
        JinaUI.Fill(new Rect(bx - 10, box.y - bh * 2 - 54, bw + 20, bh * 2 + 46), new Color(.04f, .03f, .06f, .72f));
        JinaUI.Text(new Rect(bx, box.y - bh * 2 - 48, bw, 26), "어떻게 답할까?", 17, JinaUI.Bright, TextAnchor.MiddleLeft, true);
        if (JinaUI.Button(new Rect(bx, box.y - bh * 2 - 16, bw, bh), PoliteChoice)) Choose(false);
        if (JinaUI.Button(new Rect(bx, box.y - bh - 8, bw, bh), BluntChoice)) Choose(true);
    }
    // A breath of black at each cut between pictures.
    static float Dip(float t, params float[] cuts)
    {
        float dark = 0;
        foreach (float c in cuts) dark = Mathf.Max(dark, 1 - Mathf.Abs(t - c) / .35f);
        return dark;
    }

    void OnDestroy() { if (room != null) Destroy(room); }
}
