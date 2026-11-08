using UnityEngine;

// Kongbat village across the river (PrototypeWorld.KongStart..KongEnd): the story of Kongjwi and Patjwi turned
// inside out (기획서.md, 1-1). Everyone pities sweet Kongjwi and curses Patjwi, who keeps tormenting her; in truth
// Kongjwi is a demon and Patjwi the one who knows. Three clues expose her:
//   Patjwi's words (her mother never came back from the river), the crocks (bones among the salted fish),
//   and the feast's bill of fare (the villagers' names, and last of all "선비").
// With all three, Kongjwi drops her mask when he next speaks to her. The boss fight comes later.
public class PrototypeKongVillage : MonoBehaviour
{
    public PrototypeCombat combat;
    public enum Who { Kongjwi, Patjwi, Anak, Namu }
    public const string Kongjwi = "16_Kong/kongjwi", Patjwi = "16_Kong/patjwi", Anak = "16_Kong/anak", Namu = "16_Kong/namu";
    // The scene of Patjwi tormenting Kongjwi: how near he must be, each line's length, the wait between scenes.
    public const float SceneRange = 10f, LineLength = 2.8f, SceneGap = 9f, TalkRange = 1.6f, LookRange = 1.6f;

    public bool HeardPatjwi { get; private set; }
    public bool SawCrocks { get; private set; }
    public bool ReadList { get; private set; }
    public bool Invited { get; private set; }
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

    // One scene: who speaks each line. Patjwi shoves on her first line.
    static readonly (Who who, string line)[][] Scenes =
    {
        new[] { (Who.Patjwi, "또 그 웃음이야? 우리 어머니 앞에서도 그렇게 웃었지!"), (Who.Kongjwi, "아야… 괜찮아요. 다 제가 잘못했어요…"),
                (Who.Anak, "딸이 저 모양이니 계모가 천벌을 받고 죽었지, 쯧쯧."), (Who.Namu, "콩쥐 저 아이는 매를 맞으면서도 웃더라니까. 저런 효녀가 또 있을까.") },
        new[] { (Who.Patjwi, "물동이 이리 내! 네가 길어 온 물은 안 마셔!"), (Who.Kongjwi, "동생아, 물이 엎질러졌잖니… 다시 길어 올게."),
                (Who.Namu, "저것 봐, 또 엎는구먼. 저 집 재산이 콩쥐 몫이 될 만하지."), (Who.Anak, "팥쥐 저것, 언젠가 제 어미 꼴 날 게야.") },
        new[] { (Who.Patjwi, "장독대 근처에 얼씬도 하지 마. 거기서 뭘 하는지 다 알아!"), (Who.Kongjwi, "젓갈 담그는 걸 거들었을 뿐인걸… 왜 그러니."),
                (Who.Anak, "보름에 콩쥐가 마을 잔치를 연다던데, 팥쥐는 상에도 못 앉게 해야 해."), (Who.Namu, "온 마을이 다 모인다니, 그날은 나무도 쉬어야겠네.") },
    };

    public static string Name(Who who) => who == Who.Kongjwi ? "콩쥐" : who == Who.Patjwi ? "팥쥐" : who == Who.Anak ? "아낙" : "나무꾼";
    public static Vector2 Spot(Who who) => who == Who.Kongjwi ? PrototypeWorld.Kongjwi : who == Who.Patjwi ? PrototypeWorld.Patjwi
        : who == Who.Anak ? PrototypeWorld.Anak : PrototypeWorld.Namu;
    static string Art(Who who) => who == Who.Kongjwi ? Kongjwi : who == Who.Patjwi ? Patjwi : who == Who.Anak ? Anak : Namu;

    void Start()
    {
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
        HeardPatjwi = SawCrocks = ReadList = Invited = Revealed = false;
        IsOpen = false; Page = 0; SceneAt = NoteAt = -1; scene = 0; nextScene = Time.time + 4; note = "";
    }

    float PlayerX => combat.player.transform.position.x;
    bool Near(float x, float range) => Mathf.Abs(PlayerX - x) < range && combat.player.transform.position.y < 2f;
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

    public static string[] Lines(Who who, bool heard, bool saw, bool read, bool revealed, bool keepsake)
    {
        switch (who)
        {
            case Who.Kongjwi:
                if (revealed) return new[] { "……." };
                if (heard && saw && read) return new[] {
                    "…장독대를 열어 보셨군요. 명단도 읽으셨고.",
                    "착한 아이인 척하는 것도 이제 지겨웠는데, 잘됐네요.",
                    "잔치는 조금 일찍 열죠. 선비님부터 젓갈로 담가 드릴게요." };
                return new[] {
                    "선비님, 먼 길 오셨죠? 저는 콩쥐라고 해요.",
                    "동생이 좀 거칠어도 미워하지 마세요. 어머니를 잃고 그런 거니까요.",
                    "다음 보름에 우리 집에서 마을 잔치를 열어요. 꼭 오세요. 모두… 오셔야 해요." };
            case Who.Patjwi:
                if (revealed) return new[] { "봤죠? 저게 콩쥐예요. 아무도 내 말을 안 믿었어요." };
                var said = new System.Collections.Generic.List<string>();
                if (keepsake) { said.Add("그, 그 비녀… 우리 어머니 거예요! 어디서 났어요?"); said.Add("…강에서요? 역시 어머니는 그냥 빠진 게 아니었어."); }
                else said.Add("…뭘 봐요. 당신도 저 애가 불쌍해 보여요?");
                said.Add("우리 어머니도 처음엔 그랬어요. 그러다 강에 빨래하러 가서는 돌아오지 못했죠.");
                said.Add("어머니가 마지막으로 그랬어요. 장독대엔 가지 말라고. 저 애가 밤마다 거기 있다고.");
                return said.ToArray();
            case Who.Anak:
                return saw ? new[] { "장독대? 그 집 젓갈 맛이 기막히다고 소문났지. 뭐가 들었길래 그리 맛있누." }
                    : new[] { "선비님도 콩쥐 잔치에 가실 거유? 그 착한 것이 손수 다 차린다니.", "팥쥐 그것은 제 어미 닮아 독해. 딸이 저 모양이니 계모가 천벌을 받고 죽었지, 쯧쯧." };
            default:
                return read ? new[] { "잔칫상에 내 이름이 적혀 있다고? 허허, 귀한 손님 대접이구먼." }
                    : new[] { "나무하러 산에 오르면 밤마다 장독대 쪽에서 뭘 씹는 소리가 들려.", "…설마 콩쥐가 밤참을 먹는 거겠지." };
        }
    }

    public void Open(Who who)
    {
        Speaker = who; Page = 0; IsOpen = true;
        unmasking = who == Who.Kongjwi && (Revealed || Clues == 3);
        lines = Lines(who, HeardPatjwi, SawCrocks, ReadList, Revealed, HasKeepsake);
        combat.player.CanMove = false;
        SceneAt = -1;
    }
    public void Next()
    {
        if (++Page < lines.Length) return;
        IsOpen = false; combat.player.CanMove = combat.PlayerHealth > 0;
        if (Speaker == Who.Patjwi && !Revealed) HeardPatjwi = true;
        if (Speaker == Who.Kongjwi && !Revealed) { if (Clues == 3) Revealed = true; else Invited = true; }
    }
    public void ExamineCrocks()
    {
        SawCrocks = true; NoteAt = Time.time;
        note = "독 뚜껑을 열자 짠 냄새가 확 끼친다. 젓갈 사이로… 사람의 손가락뼈가 보인다.";
    }
    public void ReadFeastList()
    {
        ReadList = true; NoteAt = Time.time;
        note = "보름 잔칫상 차림표. '젓갈 여섯 독' 아래로 마을 사람 이름이 하나씩 적혀 있다. 맨 끝에는… '선비'.";
    }

    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) { IsOpen = false; return; }
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { Page = lines.Length - 1; Next(); }
            else if (Input.GetKeyDown(KeyCode.E)) Next();
            return;
        }
        if (combat.Paused || !InVillage) return;
        if (Input.GetKeyDown(KeyCode.E))
        {
            var who = Nearest();
            if (who.HasValue) Open(who.Value);
            else if (Near(PrototypeWorld.JangdokX, LookRange)) ExamineCrocks();
            else if (Near(PrototypeWorld.FeastListX, LookRange)) ReadFeastList();
        }
        // The scene plays over and over while he is nearby, until Kongjwi is unmasked.
        if (!Revealed && SceneAt < 0 && Time.time >= nextScene && Mathf.Abs(PlayerX - PrototypeWorld.Kongjwi.x) < SceneRange) SceneAt = Time.time;
        if (SceneAt >= 0 && Time.time - SceneAt >= LineLength * 4) { SceneAt = -1; scene = (scene + 1) % Scenes.Length; nextScene = Time.time + SceneGap; }
    }

    // The scene's current line, or none.
    public int SceneLine => SceneAt < 0 ? -1 : Mathf.Min(3, (int)((Time.time - SceneAt) / LineLength));
    // Patjwi's shove: Kongjwi is pushed back a little and staggers during the first line.
    public float Shove => SceneLine == 0 ? Mathf.Clamp01((Time.time - SceneAt) / .25f) : SceneLine > 0 ? 1 : 0;

    void LateUpdate()
    {
        if (people == null) return;
        // Kongjwi pushed back toward the well during a scene; unmasked, she darkens.
        people[(int)Who.Kongjwi].transform.position = (Vector3)PrototypeWorld.Kongjwi + Vector3.left * .35f * Shove;
        people[(int)Who.Kongjwi].GetComponent<VillagerVisual>().dark = Revealed;
        for (int i = 0; i < 4; i++)
        {
            var v = people[i].GetComponent<VillagerVisual>();
            var who = (Who)i;
            // Facing: the one speaking with him turns to him; in a scene Patjwi and Kongjwi face each other.
            v.lookAt = IsOpen && Speaker == who ? (PlayerX >= Spot(who).x ? 1 : -1)
                : SceneAt >= 0 && who == Who.Patjwi ? -1 : SceneAt >= 0 && who == Who.Kongjwi ? 1 : 0;
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
            GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height - 55, 440, 35), "E: 다음 (" + (Page + 1) + "/" + lines.Length + ")   ·   Esc: 그만 듣기");
            return;
        }
        if (combat.Paused || !InVillage) return;
        if (SceneLine >= 0)
        {
            var (who, line) = Scenes[scene][SceneLine];
            var at = new Vector3(Spot(who).x, who == Who.Kongjwi || who == Who.Patjwi ? 1.1f : 1.55f, 0);
            if (SpeechBubble.OnScreen(at)) SpeechBubble.Draw(at, Name(who), line);
        }
        if (NoteAt >= 0 && Time.time - NoteAt < 4.5f)
            GUI.Box(new Rect(Screen.width / 2f - 300, 240, 600, 55), note);
        var near = Nearest();
        string prompt = near.HasValue ? "E: " + Name(near.Value) + "와(과) 이야기하기"
            : Near(PrototypeWorld.JangdokX, LookRange) ? "E: 장독대 살펴보기"
            : Near(PrototypeWorld.FeastListX, LookRange) ? "E: 평상 위 종이 읽기" : null;
        if (prompt != null) GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height - 55, 440, 35), prompt);
    }

    // Kongjwi and Patjwi speak in a window with their portrait (17_Portraits); the others in speech bubbles.
    public string Portrait => !IsOpen ? null : Speaker == Who.Patjwi ? "patjwi"
        : Speaker == Who.Kongjwi ? (unmasking && Page >= 1 ? "kongjwi_true" : "kongjwi") : null;
    void DrawTalk()
    {
        const float scale = 3;
        var sprite = JinaArt.Portrait(Portrait);
        float pw = sprite.rect.width * scale, ph = sprite.rect.height * scale;
        float w = Mathf.Min(820, Screen.width - 30), h = ph + 24;
        var box = new Rect((Screen.width - w) / 2, Screen.height - h - 14, w, h);
        JinaUI.Frame(box, JinaUI.Back, JinaUI.Edge, 2);
        var face = new Rect(box.x + 12, box.y + 12, pw, ph);
        JinaUI.Frame(new Rect(face.x - 2, face.y - 2, face.width + 4, face.height + 4), new Color(.22f, .2f, .26f), JinaUI.SlotEdge, 2);
        InventoryUI.DrawIcon(sprite, face);
        float x = face.xMax + 20, tw = box.xMax - x - 16;
        var nameplate = new Rect(x, box.y + 14, 120, 30);
        JinaUI.Frame(nameplate, JinaUI.Title, JinaUI.Edge, 1);
        JinaUI.Text(nameplate, Name(Speaker), 18, JinaUI.Gold, TextAnchor.MiddleCenter, true);
        JinaUI.Text(new Rect(x, box.y + 58, tw, ph - 70), lines[Page], 19, unmasking && Page >= 1 ? new Color(1f, .78f, .74f) : JinaUI.Ink);
        JinaUI.Text(new Rect(x, box.yMax - 34, tw, 22), "E 다음 (" + (Page + 1) + "/" + lines.Length + ")   ·   Esc 그만 듣기", 13, JinaUI.Dim, TextAnchor.MiddleRight);
    }

    void OnDestroy() { if (people != null) foreach (var p in people) if (p != null) Destroy(p.gameObject); }
}
