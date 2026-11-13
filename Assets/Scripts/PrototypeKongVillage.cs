using UnityEngine;

// 강 건너 콩밭 마을(PrototypeWorld.KongStart..KongEnd): 콩쥐팥쥐 이야기를 뒤집었다
// (기획서.md, 1-1). 모두 착한 콩쥐를 불쌍히 여기고 괴롭히는 팥쥐를 욕한다. 사실 콩쥐가
// 요괴이고, 팥쥐에게 남들 앞에서 반말로 못된 동생 역할을 시켜 유일하게 아는 팥쥐를
// 마을이 미워하게 만든다. 단둘일 때 팥쥐는 넌지시 알릴 뿐이다. 넷을 다 만나고 콩쥐에게 돌아가면
// 콩쥐가 손님방에서 하룻밤 묵게 하고(PrototypeKongStay), 아침에 뒷산으로 와 달라고 한다.
// 그제서야 독을 열고 그 옆 종이를 찾을 수 있다. 단서 세 개:
//   팥쥐의 말(어머니가 강에서 돌아오지 않음), 독(젓갈 속 뼈),
//   잔치 차림표(마을 사람들 이름, 맨 끝에 "선비").
// 셋 다 알고 다음에 말을 걸면 콩쥐가 가면을 벗는다. 고개 너머 뒷산에서 기다린다
// (PrototypeKongBoss). 다른 마을 사람 셋(훈장, 엿장수, 할멈)은 말만 한다.
// 요괴가 선비를 따라 여기까지 오면 저주로 모두 그 자리에 묶이고(팥쥐는 먼저 맨 왼쪽으로 달아남)
// 오른쪽부터 한 명씩 죽인다(Raided, Kill: 한 명당 −10). 여기서는 아무도 대피하지 않는다.
// 싸운 뒤 PrototypeKongAftermath가 WalkTo, SetLook, Face로 사람들을 움직이고 자세를 고른다.
public class PrototypeKongVillage : MonoBehaviour
{
    public PrototypeCombat combat;
    public enum Who { Kongjwi, Patjwi, Anak, Namu, Hunjang, Yeot, Halmi }
    public const int People = 7;
    public const string Kongjwi = "16_Kong/kongjwi", Patjwi = "16_Kong/patjwi", Anak = "16_Kong/anak", Namu = "16_Kong/namu",
        Hunjang = "16_Kong/hunjang", Yeot = "16_Kong/yeot", Halmi = "16_Kong/halmi";
    // 팥쥐가 콩쥐를 괴롭히는 장면: 선비가 얼마나 가까워야 하는지, 대사 길이, 장면 사이 간격.
    public const float SceneRange = 10f, LineLength = 3.2f, SceneGap = 9f, TalkRange = 1.6f, LookRange = 1.6f;

    public bool HeardPatjwi { get; private set; }
    public bool SawCrocks { get; private set; }
    public bool ReadList { get; private set; }
    public bool Invited { get; private set; }
    // 하룻밤을 묵었다. 이제 독과 종이를 볼 수 있다.
    public bool Stayed { get; private set; }
    // 그날 아침 정중히 거절하지 않고 이 집이 이상하다고 대놓고 말했다.
    public bool Blunt { get; private set; }
    public bool Revealed { get; private set; }
    // 여기서 말을 건 사람(Who마다 비트 하나)과 팥쥐가 은비녀 이야기를 했는지.
    private int met;
    public bool Met(Who who) => (met & 1 << (int)who) != 0;
    public bool KeepsakeTold { get; private set; }
    // 이야기 속 넷을 다 만났다: 다음에 콩쥐에게 말을 걸면 하룻밤 묵어 가라고 한다.
    public bool ReadyForNight => (met & 15) == 15 && !Stayed;
    public int Clues => (HeardPatjwi ? 1 : 0) + (SawCrocks ? 1 : 0) + (ReadList ? 1 : 0);

    public bool IsOpen { get; private set; }
    public Who Speaker { get; private set; }
    public int Page { get; private set; }
    public float SceneAt { get; private set; } = -1;
    public float NoteAt { get; private set; } = -1;
    private string[] lines;
    // 콩쥐가 가면을 벗는 대화: 첫 줄 뒤에 초상화가 바뀐다.
    private bool unmasking;
    // 하룻밤 묵어 가라는 청으로 끝나는 대화.
    private bool inviting;
    // 뒷산 싸움 뒤의 대화.
    private bool afterTalk;
    private string note = "";
    private int scene;
    private float nextScene = 4;
    private SpriteRenderer[] people;
    private GameObject feastList;

    // 습격과 그 뒤: 누가 묶였거나 죽었는지, 지금 어디 서 있고 어디로 걷는지, 어떤 모습인지.
    public enum Fate { Free, Bound, Dead }
    private readonly Fate[] fates = new Fate[People];
    private readonly float[] xs = new float[People], goals = new float[People], speeds = new float[People], deaths = new float[People];
    private readonly VillagerVisual.Look[] looks = new VillagerVisual.Look[People];
    private readonly bool[] crying = new bool[People];
    private readonly int[] facings = new int[People];
    public bool Raided { get; private set; }
    public float RaidAt { get; private set; } = -1;
    public float ReleasedAt { get; private set; } = -1;
    // 지금 조이고 있는 사람(비명을 지름)과 마지막으로 죽은 사람(알림).
    public Who Threatened { get; private set; }
    public float ThreatAt { get; private set; } = -1;
    public Who LastLost { get; private set; }
    public float LostAt { get; private set; } = -1;
    public Fate FateOf(Who who) => fates[(int)who];
    public bool Alive(Who who) => fates[(int)who] != Fate.Dead;
    public float DiedAt(Who who) => deaths[(int)who];
    public int Lost { get { int n = 0; for (int i = 1; i < People; i++) if (fates[i] == Fate.Dead) n++; return n; } }
    // 요괴가 데려갈 사람이 남았는지(콩쥐 자신은 빼고).
    public bool AnyoneToTake { get { for (int i = 1; i < People; i++) if (fates[i] != Fate.Dead) return true; return false; } }
    public float PersonX(Who who) => xs[(int)who];
    // 요괴가 조일 때 각자 외치는 말.
    public static string Cry(Who who) => who == Who.Hunjang ? "이, 이놈! 물러가라! 썩 물러가!" : who == Who.Yeot ? "사, 살려 줘! 엿 다 줄게, 엿 다!"
        : who == Who.Namu ? "콩쥐야, 이게 무슨 짓이냐! 놔라, 놔!" : who == Who.Halmi ? "아이고, 아이고… 사람 살려…"
        : who == Who.Anak ? "선비님! 선비님, 살려 줘요!" : "…콩쥐 언니. 언니, 제발… 정신 차려…";
    // 이름 뒤의 이/가, 마지막 글자로 정한다.
    public static string Subject(string name) { char c = name[name.Length - 1]; return c >= '\uac00' && c <= '\ud7a3' && (c - 0xac00) % 28 != 0 ? "이" : "가"; }

    // 장면 하나: 줄마다 누가 말하는지. 팥쥐는 첫 줄에 밀친다. 옛이야기의 못된 동생을 큰 소리로 연기한다.
    static readonly (Who who, string line)[][] Scenes =
    {
        new[] { (Who.Patjwi, "야, 콩쥐! 그 웃음 좀 치워. 보기만 해도 밥맛 떨어지니까!"), (Who.Kongjwi, "아야… 미안해, 동생아. 다 내가 잘못했어…"),
                (Who.Anak, "딸이 저 모양이니 계모가 천벌을 받고 죽었지, 쯧쯧."), (Who.Namu, "콩쥐 저 아이는 매를 맞으면서도 웃더라니까. 저런 효녀가 또 있을까.") },
        new[] { (Who.Patjwi, "물동이 이리 내! 네가 길어 온 물은 더러워서 못 마셔!"), (Who.Kongjwi, "동생아, 물이 엎질러졌잖니… 내가 다시 길어 올게."),
                (Who.Namu, "저것 봐, 또 엎는구먼. 저 집 재산이 콩쥐 몫이 될 만하지."), (Who.Anak, "팥쥐 저것, 언젠가 제 어미 꼴 날 게야.") },
        new[] { (Who.Patjwi, "베는 다 짰어? 안 짰으면 오늘 저녁은 굶어! 알았어?!"), (Who.Kongjwi, "응… 다 짜 놓을게. 화내지 마, 동생아."),
                (Who.Anak, "보름에 콩쥐가 마을 잔치를 연다던데, 팥쥐는 상에도 못 앉게 해야 해."), (Who.Namu, "온 마을이 다 모인다니, 그날은 나무도 쉬어야겠네.") },
        new[] { (Who.Patjwi, "왜 또 장독대 쪽에 있었어? 거긴 네가 얼씬거릴 데가 아니잖아!"), (Who.Kongjwi, "젓갈 담그는 걸 거들었을 뿐인걸… 왜 그러니, 동생아."),
                (Who.Anak, "저 착한 걸 또 잡는구먼. 쯧쯧."), (Who.Namu, "그 집 젓갈이 또 기막히단 말이지.") },
    };

    // 다음 날 아침: 콩쥐가 잔치 준비를 하고 모두 홀려 있다. 이제 선비는 귀 기울일 이유가 있어서
    // 마을이 못 듣는 것을 듣는다: 팥쥐가 시킨 대사를 하고, 더 크게 하라는 말을 듣는 것.
    static readonly (Who who, string line)[][] MorningScenes =
    {
        new[] { (Who.Kongjwi, "아주머니, 댁 식구가 모두 몇이시랬죠? 상이 모자라면 안 되니까요."), (Who.Anak, "다섯이지, 다섯! 아이고, 저 착한 것이 그걸 다 챙기네."),
                (Who.Patjwi, "…야, 콩쥐. 쓸데없는 소리 말고 물이나 떠 와."), (Who.Kongjwi, "응, 동생아. …목소리가 작네?") },
        new[] { (Who.Patjwi, "무, 물 떠 오라고! 귀가 먹었어?!"), (Who.Kongjwi, "아야… 미안해. 금방 떠 올게."),
                (Who.Namu, "팥쥐야, 넌 좀 보고 배워라. 쯧쯧."), (Who.Patjwi, "……") },
        new[] { (Who.Kongjwi, "나무꾼 아저씨, 요즘 살이 좀 오르셨네요. 보기 좋아요."), (Who.Namu, "허허, 네가 갖다 준 젓갈 덕이지. 밥도둑이 따로 없어."),
                (Who.Patjwi, "…그 젓갈, 너무 많이는…"), (Who.Kongjwi, "응? 동생아, 뭐라고 했니?") },
        new[] { (Who.Anak, "손님까지 드셨으니 콩쥐가 더 바빠졌겠네."), (Who.Kongjwi, "보름이 코앞인걸요. 독을 미리 비워 둬야 해서요."),
                (Who.Namu, "독을 비워? 그 큰 독에 뭘 담으려고?"), (Who.Kongjwi, "…맛있는 거요.") },
    };
    (Who who, string line)[][] CurrentScenes => Stayed ? MorningScenes : Scenes;

    // 인물의 한글 이름.
    public static string Name(Who who) => who == Who.Kongjwi ? "콩쥐" : who == Who.Patjwi ? "팥쥐" : who == Who.Anak ? "아낙" : who == Who.Namu ? "나무꾼"
        : who == Who.Hunjang ? "훈장" : who == Who.Yeot ? "엿장수" : "할멈";
    // 인물이 처음 서 있는 자리.
    public static Vector2 Spot(Who who) => who == Who.Kongjwi ? PrototypeWorld.Kongjwi : who == Who.Patjwi ? PrototypeWorld.Patjwi
        : who == Who.Anak ? PrototypeWorld.Anak : who == Who.Namu ? PrototypeWorld.Namu
        : who == Who.Hunjang ? PrototypeWorld.Hunjang : who == Who.Yeot ? PrototypeWorld.Yeot : PrototypeWorld.Halmi;
    // 인물의 그림 경로.
    public static string Art(Who who) => who == Who.Kongjwi ? Kongjwi : who == Who.Patjwi ? Patjwi : who == Who.Anak ? Anak : who == Who.Namu ? Namu
        : who == Who.Hunjang ? Hunjang : who == Who.Yeot ? Yeot : Halmi;

    // 마을 사람과 잔치 명단 오브젝트를 만든다.
    void Start()
    {
        feastList = GameObject.Find("잔치 명단");
        if (feastList != null) feastList.SetActive(Stayed);
        people = new SpriteRenderer[People];
        for (int i = 0; i < People; i++)
        {
            var who = (Who)i;
            var marker = new GameObject(Name(who)).AddComponent<SpriteRenderer>();
            marker.transform.position = Spot(who);
            marker.transform.localScale = new Vector3(.75f, 1.4f, 1);   // 보부상처럼 발이 아래 가장자리에 선다
            marker.sprite = combat.player.GetComponent<SpriteRenderer>().sprite;
            marker.color = new Color(.5f, .45f, .35f);   // 흰 사각형은 "방금 맞음"으로 읽힌다
            JinaActorVisual.Attach(marker, Art(who), combat);
            people[i] = marker;
        }
        ResetPeople();
    }

    // 재시작: 마을 진행 상태를 모두 처음으로.
    public void ResetVillage()
    {
        HeardPatjwi = SawCrocks = ReadList = Invited = Revealed = Stayed = Blunt = KeepsakeTold = false; met = 0;
        if (feastList != null) feastList.SetActive(false);
        IsOpen = false; Page = 0; SceneAt = NoteAt = -1; scene = 0; nextScene = Time.time + 4; note = "";
        ResetPeople();
    }
    // 모두 싸우기 전 자리로(체크포인트). 여기서 알아낸 것은 남는다.
    public void ResetRaid() { IsOpen = false; ResetPeople(); }
    // 사람들의 묶임·죽음·위치를 처음으로 되돌린다.
    void ResetPeople()
    {
        Raided = false; RaidAt = ReleasedAt = ThreatAt = LostAt = -1;
        for (int i = 0; i < People; i++)
        {
            fates[i] = Fate.Free; xs[i] = goals[i] = Spot((Who)i).x; speeds[i] = 0; deaths[i] = -1;
            looks[i] = VillagerVisual.Look.Stand; crying[i] = false; facings[i] = 0;
        }
    }

    // ---- 습격 ----
    // 저주: 남은 사람은 모두 그 자리에 묶이고, 무슨 일이 올지 아는 팥쥐는 맨 왼쪽으로 달아난다.
    public void StartRaid()
    {
        if (Raided) return;
        Raided = true; RaidAt = Time.time; IsOpen = false; SceneAt = -1;
        for (int i = 1; i < People; i++) if (fates[i] != Fate.Dead) fates[i] = Fate.Bound;
        if (Alive(Who.Patjwi)) WalkTo(Who.Patjwi, PrototypeWorld.PatjwiRaidX, 3.4f);
    }
    // 아직 묶인 사람 중 맨 오른쪽. 맨 왼쪽의 팥쥐가 마지막이다.
    public Who? NextVictim()
    {
        Who? best = null;
        for (int i = 1; i < People; i++)
            if (fates[i] == Fate.Bound && (best == null || xs[i] > xs[(int)best.Value])) best = (Who)i;
        return best;
    }
    public void Threaten(Who who) { Threatened = who; ThreatAt = Time.time; }
    public void Spare(Who who) { if (Threatened == who) ThreatAt = -1; }
    // 요괴가 마을 사람 하나를 죽인다.
    public void Kill(Who who)
    {
        int i = (int)who;
        if (i == 0 || fates[i] == Fate.Dead) return;
        fates[i] = Fate.Dead; deaths[i] = Time.time; speeds[i] = 0; goals[i] = xs[i];
        LastLost = who; LostAt = Time.time; ThreatAt = -1;
        if (combat.Quest != null) combat.Quest.progress.LoseVillager();
        var chest = new Vector3(xs[i], .5f, 0);
        JinaFx.Dots(chest, 14, new Color(.42f, .1f, .12f), 3, .5f, 0, 12);
        JinaFx.Rise(chest, 8, new Color(.2f, .12f, .24f, .9f), .4f, 1.2f, .7f);
    }
    // 요괴가 사라지면 저주가 살아 있는 사람들을 놓아준다.
    public void Release()
    {
        ReleasedAt = Time.time; ThreatAt = -1;
        for (int i = 1; i < People; i++) if (fates[i] == Fate.Bound) fates[i] = Fate.Free;
    }
    // 싸움 뒤 장면용(PrototypeKongAftermath).
    public void WalkTo(Who who, float x, float speed) { goals[(int)who] = x; speeds[(int)who] = speed; }
    public void Place(Who who, float x) { xs[(int)who] = goals[(int)who] = x; speeds[(int)who] = 0; }
    public bool Arrived(Who who) => Mathf.Abs(xs[(int)who] - goals[(int)who]) < .01f;
    public void SetLook(Who who, VillagerVisual.Look look, bool tears = false) { looks[(int)who] = look; crying[(int)who] = tears; }
    public void Face(Who who, int facing) { facings[(int)who] = facing; }
    // 걸어가는 사람들을 목적지로 움직인다.
    void Walk(float dt)
    {
        for (int i = 0; i < People; i++)
            if (fates[i] != Fate.Dead && speeds[i] > 0) xs[i] = Mathf.MoveTowards(xs[i], goals[i], speeds[i] * dt);
    }
    // 마을의 콩쥐: 싸우기 전 선비가 뒷산을 볼 수 없을 때, 그리고 정화된 뒤 집에 돌아왔을 때.
    bool KongjwiHere => combat.KongBoss == null || combat.KongBoss.AtHome;
    // 지금 이 사람에게 말을 걸 수 있는지.
    bool CanTalkTo(Who who) => fates[(int)who] == Fate.Free && (who != Who.Kongjwi || KongjwiHere)
        && (combat.Aftermath == null || !combat.Aftermath.Playing);
    bool FightOver => combat.KongBoss != null && combat.KongBoss.Resolved;

    float PlayerX => combat.player.transform.position.x;
    bool Near(float x, float range) => Mathf.Abs(PlayerX - x) < range && combat.player.transform.position.y < 2f;
    // 종이는 다음 날 아침에만 놓여 있고, 그 옆에 서면 독보다 가깝다.
    bool OnPaper => Stayed && Near(PrototypeWorld.FeastListX, LookRange)
        && Mathf.Abs(PlayerX - PrototypeWorld.FeastListX) <= Mathf.Abs(PlayerX - PrototypeWorld.JangdokX);
    public bool InVillage => PlayerX >= PrototypeWorld.KongStart && PlayerX < PrototypeWorld.KongEnd;
    bool HasKeepsake => JinaItems.Inventory != null && JinaItems.Inventory.GetQuantity(JinaItems.Keepsake) > 0;

    // 여기서 말을 걸 사람(손 닿는 거리에서 가장 가까운 사람), 없으면 없음.
    public Who? Nearest()
    {
        Who? best = null; float bestD = TalkRange;
        for (int i = 0; i < People; i++)
        {
            float d = Mathf.Abs(PlayerX - xs[i]);
            if (d < bestD && CanTalkTo((Who)i)) { bestD = d; best = (Who)i; }
        }
        return best;
    }

    // 여기 무언가가 E에 반응한다(PrototypeLore는 비켜 준다).
    public bool HasSomethingHere => Nearest().HasValue || OnPaper || Near(PrototypeWorld.JangdokX, LookRange);

    // 하룻밤이 끝나고 아침에 깨어난다(`blunt`: 대놓고 의심했음).
    public void WakeUp(bool blunt = false)
    {
        Stayed = true; Blunt = blunt;
        if (feastList != null) feastList.SetActive(true);
    }

    // `met`: 전에 이 사람과 말했음. `keepsakeTold`: 팥쥐가 은비녀에 대해 할 수 있는 말을 이미 했음.
    // `ready`: 모두 만났으니 콩쥐가 묵어 가라고 함.
    public static string[] Lines(Who who, bool heard, bool saw, bool read, bool revealed, bool keepsake, bool stayed, bool blunt = false,
        bool met = false, bool keepsakeTold = false, bool ready = false)
    {
        switch (who)
        {
            case Who.Kongjwi:
                // 다시 모두 앞에서는 상냥하다. 이제 그 눈은 선비만 본다.
                if (revealed) return new[] { "쉿. 사람들이 보잖아요.", "뒷산이에요, 선비님. 저를 너무 오래 기다리게 하지 마세요." };
                if (heard && saw && read) return new[] {
                    "…장독대를 열어 보셨군요. 그 종이도 읽으셨고.",
                    "보름까지만 착한 아이로 있으면 됐는데. 들켰으니 어쩔 수 없네요.",
                    "뒷산에서 기다릴게요. 오시면 선비님을 젓갈로 담가 드리고, 안 오시면… 이 마을 사람들부터 담가야겠지요." };
                if (stayed && saw) return new[] {
                    "어머, 손끝에서 짠내가 나네요. 장독대에 다녀오셨어요?",
                    "…농담이에요. 저희 집 젓갈은 냄새가 오래가거든요. 뒷산에 오실 땐 손 씻고 오세요." };
                if (stayed && blunt) return new[] {
                    "아직도 그런 눈으로 보시네요. …뒷산에서 다 말씀드린다니까요.",
                    "혼자 오세요, 네? 그리고 장독대엔 가지 마세요. 젓갈이 아직 덜 익었거든요." };
                if (stayed) return new[] {
                    "이따 뒷산에서 봬요. 고개 너머라 조금 머니까 든든히 드시고요.",
                    "…장독대엔 가지 마세요. 젓갈이 아직 덜 익었거든요." };
                // 마을을 다 둘러봤다: 날이 저물고, 콩쥐는 선비를 보내지 않는다.
                if (met && ready) return new[] {
                    "마을은 다 둘러보셨어요? 다들 좋은 분들이죠.",
                    "…어머, 이야기하다 보니 하늘이 벌써 붉네요." };
                if (met) return new[] {
                    "마을 구경은 좀 하셨어요? 아주머니도, 나무꾼 아저씨도 다 좋은 분들이에요.",
                    "천천히 둘러보세요. 저는 여기 있을게요." };
                return new[] {
                    "선비님, 먼 길 오셨죠? 저는 콩쥐라고 해요.",
                    "동생이 좀 거칠어도 미워하지 마세요. 어머니를 잃고 그런 거니까요.",
                    "다음 보름에 우리 집에서 마을 잔치를 열어요. 꼭 오세요. 모두… 오셔야 해요." };
            case Who.Patjwi:
                if (revealed)
                {
                    // 드디어 정체가 들켰고 아는 사람이 또 있다: 이제 조금 더 말할 수 있다.
                    if (keepsake) return new[] {
                        "…이제 아시겠죠. 왜 아무 말도 못 했는지.",
                        "그 비녀, 우리 어머니 거예요. 저 애는 그것만은 곁에 못 둬요. 손을 대지도 못해요.",
                        "뒷산에 가실 거면 꼭 지니고 가세요. 저는… 여기서 어떻게든 버틸게요." };
                    return new[] {
                        "…이제 아시겠죠. 왜 아무 말도 못 했는지.",
                        "어머니는 저 애를 알아보셨어요. 그래서 강에서 돌아오지 못하셨고요. 은비녀를 꽂고 나가셨는데…",
                        "그 비녀만은 저 애가 곁에 못 둬요. 아직 강 어딘가에 있을 거예요. 물귀신들 사이 어딘가에요.",
                        "뒷산에 가시면 저 애가 본색을 드러낼 거예요. 쫓기거든 강으로 도망치세요. 저 애는 몸이 무거워서 배를 못 타요." };
                }
                if (saw && read) return new[] { "…보셨군요. 저는 아무 말도 안 했어요. 아무 말도요." };
                if (heard) return new[] { "저리 가요! …저 애가 보고 있어요. 저한테 말 걸지 마세요." };
                if (!stayed)
                {
                    // 처음엔 낯선 사람: 다음에 다가올 때에야 은비녀를 알아본다.
                    if (met && keepsake && !keepsakeTold) return new[] {
                        "그, 그 비녀… 어디서 나셨어요?",
                        "…아니에요. 잘못 봤어요. 꼭 지니고 계세요. 저 애 눈에 띄지 않게요." };
                    if (met) return new[] {
                        "또 왜요? 볼일 없으면 가라니까요.",
                        "…해 지기 전에 고개를 넘어요. 그게 다예요." };
                    return new[] {
                        "뭘 봐요? 당신도 콩쥐 구경 왔어요? 다들 그러던데.",
                        "볼일 없으면 가요. 이 마을엔 볼 것도 없으니까. 해 있을 때 고개를 넘으라고요.",
                        "…제가 이런 말 했다고 저 애한테는 하지 마시고요." };
                }
                // 다음 날 아침: 무엇을 들었는지 대놓고 묻지 못해 넌지시 떠본다.
                var morning = new System.Collections.Generic.List<string>();
                if (keepsake && !keepsakeTold) morning.Add("그 비녀는… 아니에요. 지니고 계세요. 저 애 눈에 띄지 않게요.");
                morning.Add("…아직도 안 갔어요? 용케 살아서 일어났네요.");
                morning.Add("밤에… 쥐 소리 같은 거 안 나던가요? 오래된 집이라서요. …아무것도 아니에요.");
                morning.Add("우리 어머니도 밤잠이 얕으셨어요. 작년 이맘때 빨래하러 강에 가셔서는…");
                morning.Add("뒷산이요? 가든지 말든지요. …거긴 소리를 질러도 아무도 못 듣는 데예요.");
                morning.Add("…장독대 쪽은 가지 말아요. 젓갈 냄새가 독하거든요. 저는, 그 말밖에 못 해요.");
                return morning.ToArray();
            case Who.Anak:
                if (revealed) return new[] { "콩쥐가 오늘따라 더 곱지 뭐유. 잔치에 쓸 젓갈을 여섯 독이나 담갔다네.", "보름날 빠지면 안 돼요, 선비님. 그 애가 얼마나 서운해하겠어." };
                if (saw) return new[] { "장독대? 그 집 젓갈 맛이 기막히다고 소문났지. 뭐가 들었길래 그리 맛있누.", "우리 집 양반은 그걸 먹은 뒤로 밥을 두 그릇씩 비운다우. 살도 뽀얗게 오르고." };
                if (stayed) return new[] { "콩쥐네서 주무셨다고? 복 받으셨네. 그 애가 어제 우리 집에 와서 식구 수를 묻더라니까.", "팥쥐 그것은 어젯밤에도 뒤꼍에서 훌쩍이던데. 제 어미 귀신이라도 본 모양이지." };
                return new[] { "선비님도 콩쥐 잔치에 가실 거유? 그 착한 것이 손수 다 차린다니.", "팥쥐 그것은 제 어미 닮아 독해. 딸이 저 모양이니 계모가 천벌을 받고 죽었지, 쯧쯧." };
            default:
                if (revealed) return new[] { "표정이 왜 그러시오? 보름 잔치가 코앞인데. 그날은 산에도 안 갈 참이오." };
                if (read) return new[] { "잔칫상에 내 이름이 적혀 있다고? 허허, 귀한 손님 대접이구먼.", "…그런데 이름 옆에 '한 독'이라 적힌 건 무슨 소리요? 술을 한 독 준다는 건가." };
                if (stayed) return new[] { "간밤에 그 소리 들으셨소? 콰직, 콰직… 장작 패는 소리는 아닌데 말이오.", "그 집 독은 밤마다 손을 타는지, 아침이면 뚜껑이 다 열려 있더이다." };
                return new[] { "나무하러 산에 오르면 밤마다 장독대 쪽에서 뭘 씹는 소리가 들려.", "…설마 콩쥐가 밤참을 먹는 거겠지." };
        }
    }

    // 말만 하는 세 사람: 하룻밤 전, 다음 날 아침, 선비가 다 알았을 때.
    public static string[] TownLines(Who who, bool stayed, bool revealed)
    {
        switch (who)
        {
            case Who.Hunjang:
                if (revealed) return new[] { "자네 낯빛이 왜 그런가? 보름 잔치 날 시라도 한 수 읊어 주게." };
                if (stayed) return new[] { "간밤에 콩쥐네서 묵었다지? 그 집 젓갈 냄새가 서당까지 넘어오더군.", "보름 잔치에 서당 아이들까지 다 부르겠다니, 통도 크지." };
                return new[] { "허허, 길손이로군. 과거 보러 가는 길인가?", "콩쥐 그 아이는 글은 몰라도 사람 도리는 아는 아이일세. 팥쥐는… 쯧." };
            case Who.Yeot:
                if (revealed) return new[] { "표정이 왜 그러슈? 엿이나 하나 물고 가슈. 단 게 들어가면 좀 낫지." };
                if (stayed) return new[] { "잔치에 엿도 한 판 대 달라더라고요. 사람 수만큼, 딱 맞춰서.", "…근데 젓갈 독은 왜 그리 많이 비워 두는지 몰라." };
                return new[] { "엿 사시오, 엿! 선비님, 과거 붙으려면 찹쌀엿이 최고요!", "콩쥐 아가씨는 엿을 사도 꼭 팥쥐 몫까지 사 가요. 근데 팥쥐는 입에도 안 댄다니까." };
            default:
                if (revealed) return new[] { "…눈빛을 보니 알았구먼. 그 애 잘못이 아니야.", "독 속의 그것이 그 애를 먹은 게지. 칼보다 먼저 그 애를 불러 줘요." };
                if (stayed) return new[] { "간밤에 독 깨지는 소리 못 들었수? 이 늙은이는 잠귀가 밝아서…", "그 애 두꺼비가 많이 컸겠지. 독이 비좁을 만큼." };
                return new[] { "아이고, 젊은 양반. 이 늙은 것 말동무나 좀 해 주구려.",
                    "콩쥐가 어릴 적에 밑 빠진 독에 물을 붓다가 울고 있었지. 그때 두꺼비 한 마리가 독 밑을 막아 줬다더군.",
                    "그 뒤로 그 애가 그 두꺼비를 독에 넣어 키운다던데… 요새는 통 안 보여." };
        }
    }

    // 뒷산 싸움 뒤: `purified`(콩쥐가 돌아옴) 또는 토벌, `patjwi`: 팥쥐가 살았음.
    public static string[] AfterLines(Who who, bool purified, bool patjwi)
    {
        if (purified) switch (who)
        {
            case Who.Kongjwi: return patjwi ? new[] { "…다 기억나요. 제가 한 일, 전부요.", "팥쥐한테도, 마을 분들한테도… 평생 갚을게요. 선비님, 고맙습니다." }
                : new[] { "……", "팥쥐 곁에 있을게요. 이번엔… 제가 지킬 차례예요." };
            case Who.Patjwi: return new[] { "…언니가 돌아왔어요. 어머니 비녀 덕분이에요.", "고마워요, 선비님. 이제… 큰 소리로 말해도 되겠죠." };
            case Who.Anak: return new[] { "콩쥐 속에 그런 게 있었다니… 우리가 팥쥐를 얼마나 욕했는지.", "팥쥐야, 미안하다. 이 아줌마가 미안해." };
            case Who.Namu: return new[] { "그 집 젓갈을 제일 많이 얻어먹은 게 나요. …한동안 산에도 못 가겠구먼." };
            case Who.Hunjang: return new[] { "글 읽는 사람이 눈 뜬 장님이었구먼. 팥쥐에게 사과부터 하겠네." };
            case Who.Yeot: return new[] { "보름에 잔치 대신 제사를 지낸다더군. 엿은… 제사상에 올려야지." };
            default: return new[] { "두꺼비가 떠났으니 그 애도 이제 사람으로 살겠지. 고맙수, 젊은 양반." };
        }
        switch (who)
        {
            case Who.Patjwi: return new[] { "…고마워요. 그래도… 저 안에는 언니가 있었어요.", "어머니도, 언니도… 이제 저 혼자네요." };
            case Who.Anak: return new[] { "그게 콩쥐였다니… 아니, 콩쥐 안에 있던 무언가였다니." };
            case Who.Namu: return new[] { "괴물은 죽었소. 허나 콩쥐도 함께 갔으니, 이걸 잘됐다고 해야 할지." };
            case Who.Hunjang: return new[] { "칼로 끝낼 수밖에 없었겠지. 자네를 탓하진 않겠네." };
            case Who.Yeot: return new[] { "엿판도 다 엎어졌소. …그래도 살았으니 됐지." };
            default: return new[] { "불쌍한 것. 독 속에서 얼마나 울었을꼬." };
        }
    }

    // 이 사람과의 대화를 연다.
    public void Open(Who who)
    {
        Speaker = who; Page = 0; IsOpen = true;
        afterTalk = FightOver;
        unmasking = !afterTalk && who == Who.Kongjwi && (Revealed || Clues == 3);
        lines = afterTalk ? AfterLines(who, combat.KongBoss.Now == PrototypeKongBoss.Phase.Purified, Alive(Who.Patjwi))
            : who >= Who.Hunjang ? TownLines(who, Stayed, Revealed)
            : Lines(who, HeardPatjwi, SawCrocks, ReadList, Revealed, HasKeepsake, Stayed, Blunt, Met(who), KeepsakeTold, ReadyForNight);
        // 선비가 다 알기 전에 은비녀에 대해 한 말은 두 번 하지 않는다.
        if (who == Who.Patjwi && !Revealed && System.Array.Exists(lines, l => l.Contains("비녀"))) KeepsakeTold = true;
        // 모두 만났고 전에 콩쥐와 말한 적이 있으면 묵어 가라는 청을 받는다(처음 본 사람에게 바로 청하지 않음).
        inviting = !afterTalk && who == Who.Kongjwi && !Revealed && Clues < 3 && !Stayed && Met(who) && ReadyForNight;
        met |= 1 << (int)who;
        combat.player.CanMove = false;
        SceneAt = -1;
    }
    // 대화를 다음 줄로 넘기고, 끝나면 단서·하룻밤을 처리한다.
    public void Next()
    {
        if (++Page < lines.Length) return;
        IsOpen = false; combat.player.CanMove = combat.PlayerHealth > 0;
        // 선비가 밤을 무사히 넘긴 걸 본 다음 날 아침에만 무언가를 흘린다.
        if (afterTalk) return;
        if (Speaker == Who.Patjwi && !Revealed && Stayed) HeardPatjwi = true;
        if (Speaker == Who.Kongjwi && !Revealed && Clues == 3) Revealed = true;
        else if (inviting)
        {
            Invited = true; inviting = false;
            if (combat.Stay != null) combat.Stay.Begin();
        }
    }
    // 장독대를 살펴본다(단서).
    public void ExamineCrocks()
    {
        NoteAt = Time.time;
        if (!Stayed) { note = "남의 집 장독을 함부로 열 수는 없다."; return; }
        SawCrocks = true;
        note = "독 뚜껑을 열자 짠 냄새가 확 끼친다. 젓갈 사이로… 사람의 손가락뼈가 보인다.";
    }
    // 잔치 명단을 읽는다(단서).
    public void ReadFeastList()
    {
        ReadList = true; NoteAt = Time.time;
        note = "항아리 곁에 떨어진 종이. 보름 잔칫상 차림표다. '젓갈 여섯 독' 아래로 마을 사람 이름이 하나씩 적혀 있다. 맨 끝에는… '선비'.";
    }

    // E 입력, 괴롭힘 장면, 걷기를 처리한다.
    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) { IsOpen = false; return; }
        if (!combat.Paused) Walk(Time.deltaTime);
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { Page = lines.Length - 1; Next(); }
            else if (Input.GetKeyDown(KeyCode.E) && !JinaUI.FinishTyping(lines[Page])) Next();
            return;
        }
        if (combat.Paused || !InVillage) return;
        if (Input.GetKeyDown(KeyCode.E))
        {
            var who = Nearest();
            if (who.HasValue) Open(who.Value);
            else if (OnPaper) ReadFeastList();
            else if (Near(PrototypeWorld.JangdokX, LookRange)) ExamineCrocks();
        }
        // 선비가 가까이 있는 동안 장면이 되풀이된다. 콩쥐 가면이 벗겨지거나 뒷산에 올라가면 끝.
        bool quiet = Revealed || (combat.KongBoss != null && (combat.KongBoss.Started || !combat.KongBoss.AtHome));
        if (!quiet && SceneAt < 0 && Time.time >= nextScene && Mathf.Abs(PlayerX - PrototypeWorld.Kongjwi.x) < SceneRange) SceneAt = Time.time;
        if (SceneAt >= 0 && Time.time - SceneAt >= LineLength * 4) { SceneAt = -1; scene = (scene + 1) % CurrentScenes.Length; nextScene = Time.time + SceneGap; }
    }

    // 장면의 지금 대사, 없으면 없음.
    public int SceneLine => SceneAt < 0 ? -1 : Mathf.Min(3, (int)((Time.time - SceneAt) / LineLength));
    // 팥쥐가 밀침: 첫 대사 동안 콩쥐가 조금 밀려나 비틀거린다.
    public float Shove => Stayed ? 0 : SceneLine == 0 ? Mathf.Clamp01((Time.time - SceneAt) / .25f) : SceneLine > 0 ? 1 : 0;

    // 사람마다 방향과 자세를 정한다.
    void LateUpdate()
    {
        if (people == null) return;
        var stay = combat.Stay;
        bool evening = stay != null && stay.Now == PrototypeKongStay.Phase.Evening;
        var boss = combat.KongBoss;
        for (int i = 0; i < People; i++)
        {
            var who = (Who)i;
            // 장면 중 우물 쪽으로 밀려난 콩쥐, 저녁에 콩쥐의 눈길에 움츠러든 팥쥐.
            float x = xs[i] + (who == Who.Kongjwi ? -.35f * Shove : who == Who.Patjwi && evening ? .3f * stay.Flinch : 0);
            people[i].transform.position = new Vector3(x, Spot(who).y, 0);
            var v = people[i].GetComponent<VillagerVisual>();
            // 방향: 싸움 뒤 장면이 정한 대로. 선비와 말하는 사람은 선비를 보고, 묶인 사람은 요괴를 보고,
            // 장면에서는 팥쥐와 콩쥐가 서로 본다.
            v.lookAt = facings[i] != 0 ? facings[i] : IsOpen && Speaker == who ? (PlayerX >= x ? 1 : -1)
                : fates[i] == Fate.Bound && boss != null && boss.Fighting ? (boss.Feet.x >= x ? 1 : -1)
                : (SceneAt >= 0 || evening) && who == Who.Patjwi ? -1 : (SceneAt >= 0 || evening) && who == Who.Kongjwi ? 1 : 0;
            v.look = fates[i] == Fate.Dead ? VillagerVisual.Look.Dead : fates[i] == Fate.Bound ? VillagerVisual.Look.Bound : looks[i];
            v.tears = crying[i];
            v.trembling = fates[i] == Fate.Bound && ThreatAt >= 0 && Threatened == who;
            v.running = speeds[i] > 0 && !Arrived(who) && fates[i] != Fate.Dead;
        }
        // 가면을 벗으면 어두워진다. 뒷산에서 기다리거나 싸우는 동안은 마을에 없다.
        people[0].GetComponent<VillagerVisual>().dark = Revealed && !FightOver;
        people[0].enabled = KongjwiHere;
    }

    // 대화창, 말풍선, 알림을 그린다.
    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        PrototypeKoreanUI.Apply();
        if (IsOpen && Portrait != null) { DrawTalk(); return; }
        if (IsOpen)
        {
            var at = Head(Speaker);
            if (SpeechBubble.OnScreen(at)) SpeechBubble.Draw(at, Name(Speaker), lines[Page]);
            JinaUI.Prompt("E: 다음 (" + (Page + 1) + "/" + lines.Length + ")   ·   Esc: 그만 듣기");
            return;
        }
        if (combat.Paused) return;
        // 죽음은 선비가 어디 있든 알린다: 그들을 요괴에게 두고 떠났을 수도 있으니.
        if (LostAt >= 0 && Time.time - LostAt < 4f) JinaUI.Toast(Name(LastLost) + Subject(Name(LastLost)) + " 콩쥐 요괴에게 목숨을 잃었습니다. (선악 −10)");
        if (!InVillage) return;
        if (RaidAt >= 0 && Time.time - RaidAt > 1.4f && Time.time - RaidAt < 4.6f && Alive(Who.Patjwi))
            Say(Who.Patjwi, "다들 도망쳐요! …안 돼, 발이… 발이 안 움직여…!");
        if (ThreatAt >= 0 && Time.time - ThreatAt < PrototypeKongBoss.KillWindup && Alive(Threatened)) Say(Threatened, Cry(Threatened));
        if (SceneLine >= 0)
        {
            var (who, line) = CurrentScenes[scene % CurrentScenes.Length][SceneLine];
            Say(who, line);
        }
        if (NoteAt >= 0 && Time.time - NoteAt < 4.5f)
            JinaUI.Toast(note);
        var near = Nearest();
        string prompt = near.HasValue ? "E: " + Name(near.Value) + "와(과) 이야기하기"
            : OnPaper ? "E: 항아리 곁의 종이 읽기"
            : Near(PrototypeWorld.JangdokX, LookRange) ? "E: 장독대 살펴보기" : null;
        if (prompt != null) JinaUI.Prompt(prompt);
    }

    // 누군가의 머리 위, 지금 선 자리에서(소녀들은 머리 하나 작다).
    public Vector3 Head(Who who) => new Vector3(xs[(int)who], who == Who.Kongjwi || who == Who.Patjwi ? 1.1f : who == Who.Halmi ? 1.2f : 1.55f, 0);
    public void Say(Who who, string line) { var at = Head(who); if (SpeechBubble.OnScreen(at)) SpeechBubble.Draw(at, Name(who), line); }
    // 콩쥐와 팥쥐는 초상화(17_Portraits)가 있는 창에서 말하고, 다른 사람은 말풍선으로 말한다.
    // 본래 모습으로 돌아온 콩쥐는 제 얼굴을 되찾는다(상냥한 얼굴: 슬픈 초상화는 아직 없음).
    public string Portrait => !IsOpen ? null : Speaker == Who.Patjwi ? "patjwi"
        : Speaker == Who.Kongjwi ? (!afterTalk && unmasking && Page >= 1 ? "kongjwi_true" : "kongjwi") : null;
    void DrawTalk() => TalkWindow(Speaker, Portrait, lines[Page], "E 다음 (" + (Page + 1) + "/" + lines.Length + ")   ·   Esc 그만 듣기", unmasking && Page >= 1);
    // 왼쪽 끝 안에 초상화가 들어간 창. 창의 사각형을 돌려준다.
    public static Rect TalkWindow(Who who, string portrait, string line, string hint, bool menacing, float since = -1, bool toLeft = false)
        => JinaUI.TalkWindow(Name(who), portrait, line, hint, menacing, since, toLeft);
    void OnDestroy() { if (people != null) foreach (var p in people) if (p != null) Destroy(p.gameObject); }
}
