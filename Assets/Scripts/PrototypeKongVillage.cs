using UnityEngine;

// Kongbat village across the river (PrototypeWorld.KongStart..KongEnd): the story of Kongjwi and Patjwi turned
// inside out (기획서.md, 1-1). Everyone pities sweet Kongjwi and curses Patjwi, who torments her; in truth Kongjwi is a
// demon, and it is she who makes Patjwi play the wicked stepsister, out loud and without honorifics, so the village will
// hate the only one who knows. Alone with him Patjwi can only hint. Talking to Kongjwi leads to a night in their guest
// room (PrototypeKongStay), and in the morning she asks him up to the back hill; only then can the crocks be opened and
// the paper beside them found. Three clues:
//   Patjwi's words (her mother never came back from the river), the crocks (bones among the salted fish),
//   and the feast's bill of fare (the villagers' names, and last of all "선비").
// Caught with all three, Kongjwi drops her mask when he next speaks to her. The hill and the fight there come later.
public class PrototypeKongVillage : MonoBehaviour
{
    public PrototypeCombat combat;
    public enum Who { Kongjwi, Patjwi, Anak, Namu }
    public const string Kongjwi = "16_Kong/kongjwi", Patjwi = "16_Kong/patjwi", Anak = "16_Kong/anak", Namu = "16_Kong/namu";
    // The scene of Patjwi tormenting Kongjwi: how near he must be, each line's length, the wait between scenes.
    public const float SceneRange = 10f, LineLength = 3.2f, SceneGap = 9f, TalkRange = 1.6f, LookRange = 1.6f;

    public bool HeardPatjwi { get; private set; }
    public bool SawCrocks { get; private set; }
    public bool ReadList { get; private set; }
    public bool Invited { get; private set; }
    // He has spent the night; the crocks and the paper can be looked at now.
    public bool Stayed { get; private set; }
    // That morning he told her to her face that the house is not right, rather than declining politely.
    public bool Blunt { get; private set; }
    public bool Revealed { get; private set; }
    public int Clues => (HeardPatjwi ? 1 : 0) + (SawCrocks ? 1 : 0) + (ReadList ? 1 : 0);

    public bool IsOpen { get; private set; }
    public Who Speaker { get; private set; }
    public int Page { get; private set; }
    public float SceneAt { get; private set; } = -1;
    public float NoteAt { get; private set; } = -1;
    private string[] lines;
    // This talk is Kongjwi dropping her mask: her portrait changes after its first line.
    private bool unmasking;
    private string note = "";
    private int scene;
    private float nextScene = 4;
    private SpriteRenderer[] people;
    private GameObject feastList;

    // One scene: who speaks each line. Patjwi shoves on her first line. She plays the old tale's cruel sister, loudly.
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

    // The morning after: Kongjwi goes about getting ready for the feast, everyone charmed. He has reason to listen now,
    // and can catch what the village does not: Patjwi saying her lines, and being told to say them louder.
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

    public static string Name(Who who) => who == Who.Kongjwi ? "콩쥐" : who == Who.Patjwi ? "팥쥐" : who == Who.Anak ? "아낙" : "나무꾼";
    public static Vector2 Spot(Who who) => who == Who.Kongjwi ? PrototypeWorld.Kongjwi : who == Who.Patjwi ? PrototypeWorld.Patjwi
        : who == Who.Anak ? PrototypeWorld.Anak : PrototypeWorld.Namu;
    static string Art(Who who) => who == Who.Kongjwi ? Kongjwi : who == Who.Patjwi ? Patjwi : who == Who.Anak ? Anak : Namu;

    void Start()
    {
        feastList = GameObject.Find("잔치 명단");
        if (feastList != null) feastList.SetActive(Stayed);
        people = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++)
        {
            var who = (Who)i;
            var marker = new GameObject(Name(who)).AddComponent<SpriteRenderer>();
            marker.transform.position = Spot(who);
            marker.transform.localScale = new Vector3(.75f, 1.4f, 1);   // the feet stand on its bottom edge, like the peddler's
            marker.sprite = combat.player.GetComponent<SpriteRenderer>().sprite;
            marker.color = new Color(.5f, .45f, .35f);   // a white placeholder would read as "just hit"
            JinaActorVisual.Attach(marker, Art(who), combat);
            people[i] = marker;
        }
    }

    public void ResetVillage()
    {
        HeardPatjwi = SawCrocks = ReadList = Invited = Revealed = Stayed = Blunt = false;
        if (feastList != null) feastList.SetActive(false);
        IsOpen = false; Page = 0; SceneAt = NoteAt = -1; scene = 0; nextScene = Time.time + 4; note = "";
    }

    float PlayerX => combat.player.transform.position.x;
    bool Near(float x, float range) => Mathf.Abs(PlayerX - x) < range && combat.player.transform.position.y < 2f;
    // The paper only lies there the morning after, nearer than the crocks when he stands by it.
    bool OnPaper => Stayed && Near(PrototypeWorld.FeastListX, LookRange)
        && Mathf.Abs(PlayerX - PrototypeWorld.FeastListX) <= Mathf.Abs(PlayerX - PrototypeWorld.JangdokX);
    public bool InVillage => PlayerX >= PrototypeWorld.KongStart && PlayerX < PrototypeWorld.KongEnd;
    bool HasKeepsake => JinaItems.Inventory != null && JinaItems.Inventory.GetQuantity(JinaItems.Keepsake) > 0;

    // Who he would speak to here, if anyone (the nearest within reach).
    public Who? Nearest()
    {
        Who? best = null; float bestD = TalkRange;
        for (int i = 0; i < 4; i++)
        {
            float d = Mathf.Abs(PlayerX - Spot((Who)i).x);
            if (d < bestD) { bestD = d; best = (Who)i; }
        }
        return best;
    }

    // Something here answers to E (PrototypeLore steps aside for it).
    public bool HasSomethingHere => Nearest().HasValue || OnPaper || Near(PrototypeWorld.JangdokX, LookRange);

    public void WakeUp(bool blunt = false)
    {
        Stayed = true; Blunt = blunt;
        if (feastList != null) feastList.SetActive(true);
    }

    public static string[] Lines(Who who, bool heard, bool saw, bool read, bool revealed, bool keepsake, bool stayed, bool blunt = false)
    {
        switch (who)
        {
            case Who.Kongjwi:
                // Back in front of everyone, she is sweet again; only he sees the eyes now.
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
                return new[] {
                    "선비님, 먼 길 오셨죠? 저는 콩쥐라고 해요.",
                    "동생이 좀 거칠어도 미워하지 마세요. 어머니를 잃고 그런 거니까요.",
                    "다음 보름에 우리 집에서 마을 잔치를 열어요. 꼭 오세요. 모두… 오셔야 해요." };
            case Who.Patjwi:
                if (revealed)
                {
                    // She has been seen through at last, and there is someone else who knows: now she can say a little more.
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
                    if (keepsake) return new[] {
                        "그, 그 비녀… 어디서 나셨어요?",
                        "…아니에요. 잘못 봤어요. 꼭 지니고 계세요. 저 애 눈에 띄지 않게요." };
                    return new[] {
                        "뭘 봐요? 당신도 콩쥐 구경 왔어요? 다들 그러던데.",
                        "볼일 없으면 가요. 이 마을엔 볼 것도 없으니까. 해 있을 때 고개를 넘으라고요.",
                        "…제가 이런 말 했다고 저 애한테는 하지 마시고요." };
                }
                // The morning after: she cannot ask outright what he heard, so she feels her way round it.
                var morning = new System.Collections.Generic.List<string>();
                if (keepsake) morning.Add("그 비녀는… 아니에요. 지니고 계세요. 저 애 눈에 띄지 않게요.");
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

    public void Open(Who who)
    {
        Speaker = who; Page = 0; IsOpen = true;
        unmasking = who == Who.Kongjwi && (Revealed || Clues == 3);
        lines = Lines(who, HeardPatjwi, SawCrocks, ReadList, Revealed, HasKeepsake, Stayed, Blunt);
        combat.player.CanMove = false;
        SceneAt = -1;
    }
    public void Next()
    {
        if (++Page < lines.Length) return;
        IsOpen = false; combat.player.CanMove = combat.PlayerHealth > 0;
        // She only lets anything slip the morning after, when she sees he came through the night.
        if (Speaker == Who.Patjwi && !Revealed && Stayed) HeardPatjwi = true;
        if (Speaker == Who.Kongjwi && !Revealed)
        {
            if (Clues == 3) Revealed = true;
            else
            {
                Invited = true;
                if (!Stayed && combat.Stay != null) combat.Stay.Begin();
            }
        }
    }
    public void ExamineCrocks()
    {
        NoteAt = Time.time;
        if (!Stayed) { note = "남의 집 장독을 함부로 열 수는 없다."; return; }
        SawCrocks = true;
        note = "독 뚜껑을 열자 짠 냄새가 확 끼친다. 젓갈 사이로… 사람의 손가락뼈가 보인다.";
    }
    public void ReadFeastList()
    {
        ReadList = true; NoteAt = Time.time;
        note = "항아리 곁에 떨어진 종이. 보름 잔칫상 차림표다. '젓갈 여섯 독' 아래로 마을 사람 이름이 하나씩 적혀 있다. 맨 끝에는… '선비'.";
    }

    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) { IsOpen = false; return; }
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
        // The scene plays over and over while he is nearby, until Kongjwi is unmasked.
        if (!Revealed && SceneAt < 0 && Time.time >= nextScene && Mathf.Abs(PlayerX - PrototypeWorld.Kongjwi.x) < SceneRange) SceneAt = Time.time;
        if (SceneAt >= 0 && Time.time - SceneAt >= LineLength * 4) { SceneAt = -1; scene = (scene + 1) % CurrentScenes.Length; nextScene = Time.time + SceneGap; }
    }

    // The scene's current line, or none.
    public int SceneLine => SceneAt < 0 ? -1 : Mathf.Min(3, (int)((Time.time - SceneAt) / LineLength));
    // Patjwi's shove: Kongjwi is pushed back a little and staggers during the first line.
    public float Shove => Stayed ? 0 : SceneLine == 0 ? Mathf.Clamp01((Time.time - SceneAt) / .25f) : SceneLine > 0 ? 1 : 0;

    void LateUpdate()
    {
        if (people == null) return;
        // Kongjwi pushed back toward the well during a scene; unmasked, she darkens.
        people[(int)Who.Kongjwi].transform.position = (Vector3)PrototypeWorld.Kongjwi + Vector3.left * .35f * Shove;
        people[(int)Who.Kongjwi].GetComponent<VillagerVisual>().dark = Revealed;
        var stay = combat.Stay;
        bool evening = stay != null && stay.Now == PrototypeKongStay.Phase.Evening;
        people[(int)Who.Patjwi].transform.position = (Vector3)PrototypeWorld.Patjwi + Vector3.right * .3f * (evening ? stay.Flinch : 0);
        for (int i = 0; i < 4; i++)
        {
            var v = people[i].GetComponent<VillagerVisual>();
            var who = (Who)i;
            // Facing: the one speaking with him turns to him; in a scene Patjwi and Kongjwi face each other.
            v.lookAt = IsOpen && Speaker == who ? (PlayerX >= Spot(who).x ? 1 : -1)
                : (SceneAt >= 0 || evening) && who == Who.Patjwi ? -1 : (SceneAt >= 0 || evening) && who == Who.Kongjwi ? 1 : 0;
        }
    }

    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        PrototypeKoreanUI.Apply();
        if (IsOpen && Portrait != null) { DrawTalk(); return; }
        if (IsOpen)
        {
            var at = new Vector3(Spot(Speaker).x, Speaker == Who.Kongjwi || Speaker == Who.Patjwi ? 1.1f : 1.55f, 0);
            if (SpeechBubble.OnScreen(at)) SpeechBubble.Draw(at, Name(Speaker), lines[Page]);
            JinaUI.Prompt("E: 다음 (" + (Page + 1) + "/" + lines.Length + ")   ·   Esc: 그만 듣기");
            return;
        }
        if (combat.Paused || !InVillage) return;
        if (SceneLine >= 0)
        {
            var (who, line) = CurrentScenes[scene % CurrentScenes.Length][SceneLine];
            var at = new Vector3(Spot(who).x, who == Who.Kongjwi || who == Who.Patjwi ? 1.1f : 1.55f, 0);
            if (SpeechBubble.OnScreen(at)) SpeechBubble.Draw(at, Name(who), line);
        }
        if (NoteAt >= 0 && Time.time - NoteAt < 4.5f)
            JinaUI.Toast(note);
        var near = Nearest();
        string prompt = near.HasValue ? "E: " + Name(near.Value) + "와(과) 이야기하기"
            : OnPaper ? "E: 항아리 곁의 종이 읽기"
            : Near(PrototypeWorld.JangdokX, LookRange) ? "E: 장독대 살펴보기" : null;
        if (prompt != null) JinaUI.Prompt(prompt);
    }

    // Kongjwi and Patjwi speak in a window with their portrait (17_Portraits); the others in speech bubbles.
    public string Portrait => !IsOpen ? null : Speaker == Who.Patjwi ? "patjwi"
        : Speaker == Who.Kongjwi ? (unmasking && Page >= 1 ? "kongjwi_true" : "kongjwi") : null;
    void DrawTalk() => TalkWindow(Speaker, Portrait, lines[Page], "E 다음 (" + (Page + 1) + "/" + lines.Length + ")   ·   Esc 그만 듣기", unmasking && Page >= 1);
    // The window, with the portrait inside its left end; returns the window's rectangle.
    public static Rect TalkWindow(Who who, string portrait, string line, string hint, bool menacing, float since = -1, bool toLeft = false)
    {
        const float h = 200;
        var sprite = JinaArt.Portrait(portrait);
        // The portrait sits inside the window at its full height, in a thin frame.
        float ph = h - 24, pw = ph * sprite.rect.width / sprite.rect.height;
        float w = Mathf.Min(toLeft ? Mathf.Max(640, JinaUI.W * .7f) : 880, JinaUI.W - 30);
        var box = new Rect(toLeft ? 16 : (JinaUI.W - w) / 2, JinaUI.H - h - 14, w, h);
        JinaUI.Fill(new Rect(box.x + 5, box.y + 6, box.width, box.height), new Color(0, 0, 0, .28f));
        JinaUI.Box(box, JinaUI.Paper, JinaUI.Wood, 3);
        var face = new Rect(box.x + 12, box.y + 12, pw, ph);
        JinaUI.Box(new Rect(face.x - 2, face.y - 2, face.width + 4, face.height + 4), JinaUI.Wood, JinaUI.Wood, 2);
        InventoryUI.DrawIcon(sprite, face);
        float x = face.xMax + 20, tw = box.xMax - x - 18;
        var nameplate = new Rect(x, box.y + 14, JinaUI.Width(Name(who), 18, true) + 28, 32);
        JinaUI.Box(nameplate, JinaUI.Wood, JinaUI.WoodLight, 1);
        JinaUI.Text(nameplate, Name(who), 18, JinaUI.Gold, TextAnchor.MiddleCenter, true);
        JinaUI.Text(new Rect(x, box.y + 58, tw, h - 58 - 34), line, 19, menacing ? JinaUI.Red : JinaUI.TextDark, TextAnchor.UpperLeft, false, since >= 0 ? JinaUI.Visible(line, since) : JinaUI.VisibleAuto(line));
        JinaUI.Text(new Rect(x, box.yMax - 32, tw, 20), hint, 13, JinaUI.TextDim, TextAnchor.MiddleRight);
        return box;
    }

    void OnDestroy() { if (people != null) foreach (var p in people) if (p != null) Destroy(p.gameObject); }
}
