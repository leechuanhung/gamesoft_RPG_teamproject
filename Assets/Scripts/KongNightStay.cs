using UnityEngine;
using Who = KongVillage.Who;

// 팥쥐네 집의 하룻밤. 콩쥐와 대화를 처음 마쳤을 때 연출로 재생된다(조작 불가, E로 저녁 대사를
// 넘기고, 스페이스로 밤과 아침을 건너뜀). 플레이가 아니라 그림이다: 사용자의 GPT 그림
// (18_Night, Tools/ArtGen/make_night.py로 자름)을 검은 띠 사이 2.4:1 화면에 겹쳐 쌓아 움직인다.
//   저녁: 노을 속 집, 길 위의 세 사람. 콩쥐가 묵어 가라고 청한다. 콩쥐의 정체를 알지만 말 못 하는
//   팥쥐는 허락된 유일한 말투인 고함으로 선비를 쫓아내려 한다. 화면이 콩쥐의 눈으로 넘어가고,
//   팥쥐는 움츠러들어 물러선다.
//   밤: 달빛 아래 집, 불 켜진 문이 꺼진다. 손님방: 밖의 소리, 창호지에 비친 형체,
//   일어나 앉는 선비. 문 클로즈업: 형체가 다가오고, 창호지에 젖은 구멍이 뚫리고, 눈이 들여다보고, 사라진다.
//   아침: 길에서 본 새벽, 담 위의 수탉. 햇살 든 방에 구멍이 그대로 있고 빛이 새어 든다.
//   작별: 문 앞의 콩쥐가 더 있으라고 붙잡는다. 선비가 거절하는 방법을 고른다: 정중히, 또는 대놓고.
//   어느 쪽이든 침묵, 그리고 뒷산으로 와 달라고 한다.
public class KongNightStay : MonoBehaviour
{
    public PrototypeCombat combat;
    public enum Phase { None, Evening, Night, Morning, Parting }
    public Phase Now { get; private set; }
    public bool Playing => Now != Phase.None;
    // 그림을 거는 곳, 세계 밖. 그림은 1280 x 533픽셀, 1칸에 128픽셀(10칸 너비).
    // 카메라는 그 너비의 16:9 화면이 딱 맞을 때까지 당기고, 늘 y = 2를 본다.
    // 그림 사방을 검은 띠가 덮어서, 그림을 옮기거나 당겨도 틀 밖이 보이지 않는다.
    public const float FrameW = 1280, FrameH = 533, PixelsPerUnit = 128;
    public const float RoomX = 400, RoomY = 2, PictureWidth = FrameW / PixelsPerUnit, RoomSize = 2.8125f;
    public const float FadeLength = .8f, NightLength = 37f, MorningLength = 9.8f;
    // 밤의 박자, 초.
    public const float RoomAt = 6f, LampOutAt = 3.4f, ShadowIn = 13f, ShadowStop = 16f, SitAt = 18.5f;
    public const float CloseAt = 23f, HoleAt = 25f, EyeAt = 26.6f, EyeGone = 29.2f, CloseEnd = 30.4f, LieAt = 31.5f;
    // 아침의 박자: 새벽 길, 그다음 방.
    public const float MorningRoomAt = 4.6f;

    // 저녁 장면의 대사: 누가, 어느 초상화로, 무슨 말을, 몇 초.
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
    // 아침 대화, 한 줄씩. 초상화가 없으면 선비가 말하는 것, "stare"는 콩쥐 눈 그림으로 넘어감.
    public static readonly (string portrait, string line)[] PartingAsk =
    {
        ("kongjwi", "일어나셨어요? 간밤엔 쥐 소리에 시끄러우셨죠. 안색이 안 좋으세요."),
        ("kongjwi", "먼 길 가시는데 그리 서두르실 것 없어요. 며칠 더 푹 쉬다 가세요. 보름 잔치도 보시고요."),
    };
    // 정중히 거절했을 때의 대화.
    public static readonly (string portrait, string line)[] PartingPolite =
    {
        (null, "말씀은 고맙소만, 과거 날이 촉박하여 오늘은 길을 나서야 하오."),
        ("kongjwi", "그러세요… 아쉬워라."),
        ("kongjwi_stare", "……"),
        ("kongjwi", "그럼 부탁 하나만 드려도 될까요? 떠나시기 전에, 이따가 뒷산에 잠시만 와 주세요."),
        ("kongjwi", "돌아가신 어머니 일로 선비님께만 여쭐 것이 있어서요. 네? 꼭이요."),
        (null, "뒷산이라… 떠나기 전에 집 둘레나 한번 돌아봐야겠군."),
    };
    // 대놓고 의심했을 때의 대화.
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
    // 콩쥐가 노려본 뒤로 팥쥐는 한 걸음 움츠러들어 있다.
    public float Flinch => Now != Phase.Evening ? 0 : line > StareLine ? 1 : line == StareLine ? Mathf.Clamp01((Elapsed - 1.4f) / .5f) : 0;
    public bool InRoom => inRoom;
    // 그림의 이름 붙은 부분마다 지금 보여 주는 것(검사용).
    public string BedSprite { get; private set; }
    public float ShadowX { get; private set; }
    public bool HoleShown { get; private set; }
    public bool EyeShown { get; private set; }
    public bool ShadowShown { get; private set; }
    // 그림의 한 픽셀이 월드에서 어디인지(x는 오른쪽, y는 왼쪽 위에서 아래로).
    static Vector3 Dot(float x, float y) => new Vector3(RoomX + (x - FrameW / 2) / PixelsPerUnit, RoomY + (FrameH / 2 - y) / PixelsPerUnit, 0);
    // 컷마다 천천히 당기기: 모든 그림을 `pivot` 픽셀을 중심으로 `zoom`만큼 키운다.
    private float zoom = 1;
    private Vector2 pivot = new Vector2(FrameW / 2, FrameH / 2);
    Vector3 At(float x, float y) => Dot(pivot.x + (x - pivot.x) * zoom, pivot.y + (y - pivot.y) * zoom);
    void View(float z, float px, float py) { zoom = z; pivot = new Vector2(px, py); }

    // 그림 층과 띠, 카메라 자리를 준비한다.
    void Start()
    {
        room = new GameObject("하룻밤 그림");
        var black = new GameObject("어둠").AddComponent<SpriteRenderer>();
        black.transform.SetParent(room.transform);
        black.sprite = JinaArt.Dot(); black.color = Color.black; black.sortingOrder = 100;
        black.transform.position = new Vector3(RoomX, RoomY, 0);
        black.transform.localScale = Vector3.one * 200 / JinaArt.DotSize;
        // 띠: 틀의 위, 아래, 왼쪽, 오른쪽, 그 안 모든 것보다 앞.
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

    // 재시작: 연출을 처음 상태로.
    public void ResetStay()
    {
        Now = Phase.None; line = 0; wokeAt = -10; Blunt = false;
        ShowRoom(false);
    }
    // 하룻밤 연출을 시작한다.
    public void Begin()
    {
        Now = Phase.Evening; line = 0; startedAt = Time.time;
        combat.player.CanMove = false;
        ShowRoom(true);
        Pose();
    }
    // 연출의 한 순간으로 바로 건너뛴다(검사와 스크린샷용).
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
    // 창 모양과 상관없이 그림 하나가 다 보이는 크기. 남는 여백은 뒤의 검은색이 채운다.
    static float FitSize(Camera cam) => Mathf.Max(RoomSize, PictureWidth / 2 / Mathf.Max(.1f, cam.aspect));
    // 연출 방 그림을 보이거나 숨긴다.
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

    // 단계(저녁·밤·아침·작별)를 진행하고 키 입력을 받는다.
    void Update()
    {
        if (!Playing) return;
        var cam = Camera.main;
        if (cam != null) cam.orthographicSize = FitSize(cam);
        if (Now == Phase.Parting) { UpdateParting(); return; }
        if (Now == Phase.Evening)
        {
            bool next = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
            // 글자가 나타나는 중에 누르면 전체를 보여 주고, 다음에 누르면 넘어간다.
            if (next && line < Evening.Length && !typedAll && JinaUI.Visible(Evening[line].line, Elapsed) < Evening[line].line.Length) { typedAll = true; next = false; }
            if (line < Evening.Length && (next || Elapsed >= Evening[line].seconds)) NextLine();
            else if (line >= Evening.Length && Elapsed >= FadeLength) { Now = Phase.Night; startedAt = Time.time; }
            Pose();
            return;
        }
        // 스페이스는 장면이 어두워지는 곳으로 건너뛴다.
        float length = Now == Phase.Night ? NightLength : MorningLength;
        if (Input.GetKeyDown(KeyCode.Space) && Elapsed < length - FadeLength) startedAt = Time.time - (length - FadeLength);
        Pose();
        if (Elapsed < length) return;
        if (Now == Phase.Night) Now = Phase.Morning;
        else BeginParting();
        startedAt = Time.time; Pose();
    }

    // 아침 대화: 콩쥐의 부탁, 선비의 대답(연출의 유일한 선택지), 침묵, 뒷산.
    public bool Blunt { get; private set; }
    public (string portrait, string line)[] PartingLines { get; private set; }
    public int PartingPage { get; private set; }
    // 콩쥐가 부탁했고, 선비는 아직 대답하지 않았다.
    public bool Choosing => Now == Phase.Parting && PartingLines == PartingAsk && PartingPage >= PartingAsk.Length - 1 && !leaving;
    private bool leaving;
    private float leftAt;
    (string portrait, string line) CurrentPart => PartingLines[PartingPage];
    // 아침 작별 대화를 시작한다.
    void BeginParting()
    {
        Now = Phase.Parting; PartingLines = PartingAsk; PartingPage = 0; leaving = false; typedAll = false; startedAt = Time.time;
    }
    // 선비의 대답을 고른다(`blunt`: 대놓고 의심).
    public void Choose(bool blunt)
    {
        if (!Choosing) return;
        Blunt = blunt; PartingLines = blunt ? PartingBlunt : PartingPolite; PartingPage = 0; typedAll = false; startedAt = Time.time;
    }
    // 작별 대화를 다음 줄로 넘긴다.
    public void NextPart()
    {
        if (Choosing || leaving) return;
        if (PartingPage + 1 < PartingLines.Length) { PartingPage++; typedAll = false; startedAt = Time.time; }
        else { leaving = true; leftAt = Time.time; }
    }
    // 작별 대화를 진행하고, 끝나면 연출을 마친다.
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
        // 콩쥐 눈으로 넘어간 컷은 잠시 저절로 머물고, 서둘러 넘길 수 없다.
        if (CurrentPart.portrait == "stare") { if (Elapsed >= StareCut) NextPart(); return; }
        if (!Input.GetKeyDown(KeyCode.E) && !Input.GetKeyDown(KeyCode.Space)) return;
        if (!typedAll && JinaUI.Visible(CurrentPart.line, Elapsed) < CurrentPart.line.Length) typedAll = true;
        else NextPart();
    }
    private bool typedAll;
    public void NextLine() { line++; startedAt = Time.time; typedAll = false; }

    // 방에서 나와 집 문 앞, 다시 조작 가능. 아침의 발견들이 열린다.
    public void Wake()
    {
        Now = Phase.None; wokeAt = Time.time;
        ShowRoom(false);
        var player = combat.player;
        player.transform.position = new Vector3(WorldBuilder.KongHouseX, .3f, 0);
        player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        player.CanMove = true;
        if (combat.Maps != null) combat.Maps.Sync();
        combat.Kong.WakeUp(Blunt);
    }

    // 다음 그림을 쌓는다. dx, dy는 픽셀 단위 이동.
    SpriteRenderer Put(string name, float alpha = 1, float dx = 0, float dy = 0)
    {
        var r = layers[used++];
        r.enabled = true; r.sprite = JinaArt.Night(name);
        r.color = new Color(1, 1, 1, alpha);
        r.transform.position = At(FrameW / 2 + dx, FrameH / 2 + dy);
        r.transform.localScale = Vector3.one * zoom;
        return r;
    }
    // 창호지에 비친 형체(흰색으로 그림): 픽셀 (x, y) 중심, 자기 크기의 `size`배, 밤의 어둠 속.
    SpriteRenderer Shade(string name, float x, float y, float size, float alpha)
    {
        var r = Put(name);
        r.color = new Color(.07f, .08f, .2f, alpha);
        r.transform.position = At(x, y);
        r.transform.localScale = Vector3.one * zoom * size;
        return r;
    }

    // 지금 순간의 그림, 층을 다시 쌓아 만든다.
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

    // 저녁 외경 컷.
    void PoseEvening(float t)
    {
        if (line == StareLine) { Put("stare"); return; }
        Put("ext_evening");
        // 팥쥐가 언니에게서 움츠러든다: 움츠린 그림이 위로 서서히 겹친다.
        if (Flinch > 0) Put("ext_evening_flinch", Flinch);
    }

    // 달빛 외경 컷.
    void PoseMoonlitHouse(float t)
    {
        // 불 켜진 문으로 천천히 다가간다.
        View(1 + t * .007f, 560, 250);
        Put("ext_night");
        // 등불이 흔들리다 꺼진다: 그 문만 어두운 같은 집 그림을 위에 겹친다.
        float dark = t < LampOutAt ? Mathf.Clamp01((Mathf.Sin(t * 9) + Mathf.Sin(t * 13.7f)) * .06f) : Mathf.Clamp01((t - LampOutAt) / .4f);
        if (dark > 0) Put("ext_night_dark", dark);
        // 마당 위의 반딧불.
        for (int i = 0; i < 8; i++)
        {
            float x = 90 + i * 150 + Mathf.Sin(t * .7f + i * 1.9f) * 30, y = 470 - i % 3 * 22 + Mathf.Sin(t * 1.1f + i) * 14;
            float glow = Mathf.Clamp01(Mathf.Sin(t * 1.7f + i * 2.4f) * 1.4f);
            Mote(i, x, y, new Color(.86f, 1, .5f, glow));
        }
    }

    // 손님방 문 창호지에서 형체가 서는 곳, 오른쪽 어디서 시작하는지.
    public const float ShadowRestX = 1095, ShadowY = 225, ShadowFrom = 420;
    // 먹는 동안의 팔: 아래, 입으로, 입에, 다시 아래.
    static readonly int[] Chew = { 0, 1, 2, 1 };
    // 밤의 손님방 컷.
    void PoseRoomAtNight(float t)
    {
        bool sitting = t >= SitAt + .3f && t < LieAt;
        bool rising = t >= SitAt && t < SitAt + .3f, lowering = t >= LieAt && t < LieAt + .4f;
        string pose = sitting ? "sit" : rising || lowering ? "prop" : "lie";
        BedSprite = pose;
        Put("room_night");
        // 형체: 오른쪽에서 들어와 문 한가운데 멈추고, 씹는 동안 손을 입으로 가져간다.
        // 창호지에만 비친다: 창살을 뺀 방 그림을 위에 겹친다.
        bool gone = t >= CloseEnd;
        if (t >= ShadowIn && !gone)
        {
            ShadowShown = true;
            float x = t < ShadowStop ? Mathf.Lerp(ShadowFrom, 0, (t - ShadowIn) / (ShadowStop - ShadowIn)) : 0;
            ShadowX = x;
            int frame = t < ShadowStop ? 0 : (int)((t - ShadowStop) / .34f) % 4;
            // 숨 쉬며 천천히 들썩인다.
            float heave = t >= ShadowStop ? Mathf.Sin(t * 2.4f) * 3 : 0;
            Shade("shadow_" + (t >= SitAt ? 0 : Chew[frame]), ShadowRestX + x, ShadowY - heave, 1.05f, .82f);
        }
        Put("room_night_front");
        // 형체가 가로막으면 바닥의 달빛이 사라진다.
        float blocked = t < ShadowIn ? 0 : t < ShadowStop ? (t - ShadowIn) / (ShadowStop - ShadowIn) : gone ? 0 : 1;
        if (blocked > 0) Put("room_night_floor", blocked * .7f);
        // 일어나 앉아 문을 본다: 자는 그림 위로 그 그림이 서서히 나타나고, 누우면 다시 사라진다.
        float up = sitting ? 1 : rising ? (t - SitAt) / .3f : lowering ? 1 - (t - LieAt) / .4f : 0;
        if (up > 0) Put("room_night_sit", up);
        // 달빛 속에 도는 먼지.
        for (int i = 0; i < 10; i++)
        {
            float life = (t * .06f + i * .137f) % 1;
            float x = 760 + (i * 61) % 360 + Mathf.Sin(t * .5f + i) * 14 - life * 80, y = 120 + life * 330;
            Mote(i, x, y, new Color(.74f, .82f, 1, .5f * Mathf.Sin(life * Mathf.PI) * (1 - blocked * .8f)));
        }
    }

    // 문 클로즈업 컷(구멍과 눈).
    void PoseDoor(float t)
    {
        // 구멍이 뚫릴 창살로 다가간다.
        View(1 + (t - CloseAt) * .012f, 590, 240);
        Put("close_paper");
        if (t >= HoleAt)
        {
            HoleShown = true;
            Put("close_hole_" + (t < HoleAt + .5f ? 0 : t < HoleAt + 1f ? 1 : 2));
        }
        // 형체가 다가와 머리가 창살을 채우고 조금 떨다가, 왼쪽으로 빠르게 사라진다.
        ShadowShown = true;
        float lean = Mathf.Clamp01((t - CloseAt) / 1.6f);
        float x = 640 + Mathf.Sin(t * 1.3f) * 4 - (t >= EyeGone ? (t - EyeGone) * (t - EyeGone) * 1150 : 0);
        float y = 470 + (1 - lean) * 330 + Mathf.Sin(t * 2.4f) * 3;
        Shade("close_shadow", x, y, 2f, .78f);   // 다른 것의 두 배 크기로 그림
        Put("close_front");
        if (t >= EyeAt && t < EyeGone)
        {
            EyeShown = true;
            float e = t - EyeAt;
            // 구멍이 열리고, 들여다보고, 천천히 한 번 깜빡이고, 다 볼 때까지 머문다.
            int frame = e < .12f ? 2 : e < .24f ? 1 : e > 1.45f && e < 1.55f ? 1 : e >= 1.55f && e < 1.7f ? 2 : e >= 1.7f && e < 1.8f ? 1 : 0;
            Put("close_eye_" + frame);
        }
    }

    // 새벽 외경 컷.
    void PoseDawn(float t)
    {
        // 수탉이 선 담에서 천천히 물러난다.
        View(1.05f - t * .01f, 1000, 330);
        Put("ext_morning");
        if (t >= .9f && t < 3.2f) Put("ext_rooster");
    }

    // 아침 방 컷.
    void PoseRoomAtMorning(float t)
    {
        BedSprite = "sit";
        Put("room_morning");
        // 문에서 들어온 햇빛 속에 금빛으로 내려앉는 먼지.
        for (int i = 0; i < motes.Length; i++)
        {
            float life = (t * .05f + i * .071f) % 1;
            float x = 700 + (i * 53) % 520 + Mathf.Sin(t * .4f + i) * 16 - life * 90, y = 80 + life * 400;
            Mote(i, x, y, new Color(1, .94f, .7f, .7f * Mathf.Sin(life * Mathf.PI)));
        }
    }

    // 떠다니는 먼지·반딧불 하나를 놓는다.
    void Mote(int i, float x, float y, Color colour)
    {
        var m = motes[i];
        m.enabled = true; m.color = colour;
        m.transform.position = At(Mathf.Round(x), Mathf.Round(y));
    }

    // 화면 전체를 `a`만큼 검게 덮는다.
    void Black(float a)
    {
        if (a > 0) JinaUI.Fill(new Rect(0, 0, JinaUI.W, JinaUI.H), new Color(0, 0, 0, Mathf.Clamp01(a)));
    }
    // 그림 아래 내레이션: 검은 바탕에 담백한 한 줄, 한 자씩 찍힌다.
    static void Caption(string s, float since)
    {
        float w = Mathf.Min(760, JinaUI.W - 40), h = JinaUI.Height(s, 19, w - 36) + 24;
        var r = new Rect((JinaUI.W - w) / 2, JinaUI.H - h - 26, w, h);
        JinaUI.Fill(r, new Color(.02f, .02f, .05f, .76f));
        JinaUI.Text(new Rect(r.x + 18, r.y + 12, w - 36, h - 24), s, 19, JinaUI.Bright, TextAnchor.UpperCenter, false, JinaUI.Visible(s, since));
    }
    // 문 너머의 소리, 크게 떨리게 써서 보이는 게 아니라 들리는 것처럼.
    static void Noise(string s, float t, float from, float to, float x = .3f, float y = .1f)
    {
        if (t < from || t >= to) return;
        float a = Mathf.Clamp01((t - from) / .3f) * Mathf.Clamp01((to - t) / .4f);
        int size = 34;
        float jitter = Mathf.Sin(t * 37) * 1.5f, jitterY = Mathf.Sin(t * 29) * 1f;
        JinaUI.Text(new Rect(JinaUI.W * x - 300 + jitter, JinaUI.H * y - size + jitterY, 600, size * 2), s, size, new Color(.92f, .9f, .98f, a), TextAnchor.MiddleCenter, true);
    }
    // 그림 속 선비 머리 위, 창 모양이 어디로 두었든.
    static Vector3 HeadAbove => Dot(385, 95);

    // 내레이션, 소리 글자, 대화창, 선택지를 그린다.
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
                // 노려보는 컷에는 창이 없다: 눈과 침묵뿐.
                if (line != StareLine) KongVillage.TalkWindow(who, portrait, words, "E 다음", false, typedAll ? 1000 : t, true);
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
    // 작별 대화창과 선택지 버튼을 그린다.
    void DrawParting(float t)
    {
        if (leaving) { Black((Time.time - leftAt) / FadeLength); return; }
        var (portrait, words) = CurrentPart;
        float since = typedAll ? 1000 : t;
        if (portrait == "stare") return;   // 콩쥐의 눈, 아무 말 없이
        if (portrait == null) { SpeechBubble.Draw(HeadAbove, "선비", words, null, since); JinaUI.Prompt("E: 다음"); return; }
        var box = KongVillage.TalkWindow(Who.Kongjwi, portrait, words, Choosing ? "" : "E 다음", false, since);
        if (!Choosing || since < 1000 && JinaUI.Visible(words, since) < words.Length) return;
        // 콩쥐가 부탁을 마치면 선비의 대답: 창 위, 초상화 오른쪽의 버튼 두 개.
        float bx = box.x + 280, bw = box.xMax - bx - 14, bh = 44;
        JinaUI.Fill(new Rect(bx - 10, box.y - bh * 2 - 54, bw + 20, bh * 2 + 46), new Color(.04f, .03f, .06f, .72f));
        JinaUI.Text(new Rect(bx, box.y - bh * 2 - 48, bw, 26), "어떻게 답할까?", 17, JinaUI.Bright, TextAnchor.MiddleLeft, true);
        if (JinaUI.Button(new Rect(bx, box.y - bh * 2 - 16, bw, bh), PoliteChoice)) Choose(false);
        if (JinaUI.Button(new Rect(bx, box.y - bh - 8, bw, bh), BluntChoice)) Choose(true);
    }
    // 그림이 바뀌는 컷마다 잠깐 검은 화면.
    static float Dip(float t, params float[] cuts)
    {
        float dark = 0;
        foreach (float c in cuts) dark = Mathf.Max(dark, 1 - Mathf.Abs(t - c) / .35f);
        return dark;
    }

    void OnDestroy() { if (room != null) Destroy(room); }
}
