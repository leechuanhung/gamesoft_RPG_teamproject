using UnityEngine;

// ===== 한양 거리와 과거장 앞 사람들 (HanyangTown) =====
// 기획서.md 1-3. 어린 임금의 도성은 좌의정 엄탁의 세상이다. 사람들은 엔터로 말을 걸면 말풍선으로 몇 마디 한다.
// 선악이 음수면 사람들이 선비를 꺼리고 포졸이 노려본다(이 맵에서는 덤비지 않는다).
// 무당은 몇 해 전 좌의정 댁 굿에서 내린 공수("업 많은 선비가 네 목을 치리라")를 들려준다.
// 거리 끝의 청지기가 과거 전날 밤 좌의정 댁 술자리에 선비를 청한다(Invited).
// 상인(잡화전)과 국밥집 주모는 가게다(Shops.cs의 PeddlerShop, TavernShop). 상인의 말은 여기 Lines에 있다.
public class HanyangTown : NpcTalk
{
    public enum Who { Sangin, Ai, Guard1, Mudang, Guard2, Cheongji, Yusaeng1, Yusaeng2 }
    public struct Person
    {
        public Who who; public string name, art; public float x; public int native;
        public Person(Who who, string name, string art, float x, int native) { this.who = who; this.name = name; this.art = art; this.x = x; this.native = native; }
    }
    public static readonly Person[] People =
    {
        new Person(Who.Ai, "아이", "24_Hanyang/ai", 399.5f, -1),
        new Person(Who.Guard1, "포졸", "22_Soldier/blade_1_idle", 405.5f, 1),
        new Person(Who.Mudang, "무당", "24_Hanyang/mudang", 410.5f, -1),
        new Person(Who.Guard2, "포졸", "22_Soldier/bow_1_idle", 419.5f, 1),
        new Person(Who.Cheongji, "좌의정 댁 청지기", "24_Hanyang/cheongji", 426f, -1),
        new Person(Who.Yusaeng1, "유생", "24_Hanyang/yusaeng", 443.5f, -1),
        new Person(Who.Yusaeng2, "유생", "24_Hanyang/yusaeng", 453f, -1),
    };
    public const float TalkRange = 1.6f;
    // 청지기에게서 좌의정의 초대를 들었는지(FinalBoss의 첫 마디가 달라진다).
    public bool Invited { get; private set; }
    // 무당의 공수를 들었는지.
    public bool HeardProphecy { get; private set; }
    public Who Speaking { get; private set; }
    private SpriteRenderer[] arts;
    private bool childOfferOpen;

    bool Wicked => combat != null && combat.Quest != null && combat.Quest.progress.Honor < 0;

    // `who`의 말. `wicked`: 선악이 음수.
    public static string[] Lines(Who who, bool wicked, bool invited)
    {
        switch (who)
        {
            case Who.Sangin: return wicked
                ? new[] { "(선비의 얼굴을 보더니 좌판을 슬금슬금 당긴다) 사, 살 것 없으면 그냥 지나가시오.", "…임금님은 아직 열두 살이시라오. 나랏일은 죄다 좌의정 대감 손에 있지. 당신 같은 사람이 대감 눈에 들면 큰일 나오." }
                : new[] { "어서 오시오! 과거 보러 오셨소? 붓이며 먹이며 다 있소이다. 다만 요즘 세금이 하도 올라 값이 좀 비싸오.", "임금님이 아직 열두 살이시라, 나랏일은 죄다 좌의정 대감 손에 있다오. 대감 댁 곳간만 날로 불어난다지." };
            case Who.Ai: return wicked
                ? new[] { "(아이가 선비의 칼을 보고 얼어붙었다가 골목으로 달아난다)" }
                : new[] { "선비님, 좌의정 대감 댁엔 밤마다 굿 소리가 나요. 엄마가 그 앞으로는 다니지 말래요.", "그 집 담장 밑을 지나면 등에 누가 업히는 것 같대요. 얼굴 없는 누가요." };
            case Who.Guard1:
            case Who.Guard2: return wicked
                ? new[] { "(창을 고쳐 쥐며) …수상한 놈이로군. 대감께서 너 같은 놈을 눈여겨보라 하셨다.", "도성 안에서 칼을 뽑으면 관군이 벌 떼처럼 몰려올 줄 알아라." }
                : new[] { "과거 보러 온 선비시오? 지나가시오. 요즘 좌의정 대감 명으로 선비들 검문이 심하오.", "대감께서 한양에 드는 선비마다 어디서 왔는지 적어 올리라 하셨소. 무얼 그리 찾으시는지." };
            case Who.Mudang: return wicked
                ? new[] { "…선비 양반, 살기가 짙구려. 길 먼지보다 업이 더 두껍게 앉았어.", "몇 해 전 좌의정 댁에 굿을 해 준 게 나요. 그날 신이 내린 공수가 이랬지. '업 많은 선비가 네 목을 치리라.'", "대감은 그 뒤로 한양에 드는 선비마다 사람을 붙여 본다오. 당신이 그 선비라면, 대감이 먼저 칼을 뽑을 거요." }
                : new[] { "…선비 양반, 길 먼지보다 업이 더 두껍게 앉았구려. 나쁜 업은 아니오. 많이 구하고 많이 거둔 얼굴이야.", "몇 해 전 좌의정 댁에 굿을 해 준 게 나요. 그날 신이 내린 공수가 이랬지. '업 많은 선비가 네 목을 치리라.'", "그 뒤로 대감은 얼굴을 팔아 무언가를 샀다는 소문이오. 그 집 술은 마시지 마시오. 잔에 비친 걸 보시오." };
            case Who.Cheongji: return invited
                ? new[] { "대감께서 기다리십니다. 과거장 앞을 지나 대감 댁 누각으로 오십시오." }
                : new[] { "선비님이시지요? 좌의정 대감께서 먼 길 오신 선비님을 모셔 오라 하셨습니다.", "과거 전날 밤, 대감 댁 누각에서 술 한잔 대접하시겠답니다. 과거장 앞을 지나 대감 댁으로 오십시오.",
                    "…대감께선 요즘 집 안의 거울을 모두 치우라 하십니다. 아, 아무것도 아닙니다. 잊어 주십시오." };
            case Who.Yusaeng1: return new[] { "내일이 과거날이오. 시제가 무엇일지 밤잠이 안 오는구려.",
                "그런데 올해는 지방 선비가 통 안 보이오. 오는 길마다 요괴가 들끓는다더니, 다들 길에서 돌아선 모양이오.",
                "좌의정 대감께 줄을 댄 자들은 벌써 시제를 안다는 소문도 있소. …그 먼 길을 뚫고 오셨다니, 선비께선 운이 좋으시구려." };
            default: return wicked
                ? new[] { "…저 선비 소문 들었소? 오는 길마다 피를 묻혔다더군. 가까이 가지 마시오." }
                : new[] { "선비께서 오는 길에 도깨비를 달래고 콩밭 마을을 구했다는 이야기가 벌써 한양에 퍼졌소!", "그런 분이 급제하신다면 좋겠소. 다만… 좌의정 대감이 그런 소문을 반길지는 모르겠구려." };
        }
    }

    private GameObject root;
    // 사람들은 따로 둔다: 이 컴포넌트가 붙은 전투 오브젝트는 카메라라서 그 밑에 두면 화면을 따라다닌다.
    void Start()
    {
        root = new GameObject("한양 사람들");
        arts = new SpriteRenderer[People.Length];
        for (int i = 0; i < People.Length; i++)
        {
            var art = new GameObject(People[i].name).AddComponent<SpriteRenderer>();
            art.transform.SetParent(root.transform);
            art.sprite = JinaArt.Load(People[i].art);
            art.transform.position = new Vector3(People[i].x, -.5f, 0);
            art.sortingOrder = 2;
            arts[i] = art;
        }
        ResetTown();
    }
    void OnDestroy() { if (root != null) Destroy(root); }
    public void ResetTown() { childOfferOpen = false; IsOpen = false; Invited = HeardProphecy = false; Page = 0; lines = null; }

    // 가장 가까운 사람 번호(없으면 -1).
    public int Near()
    {
        if (combat == null) return -1;
        var p = combat.player.transform.position;
        int best = -1; float bestD = TalkRange;
        for (int i = 0; i < People.Length; i++)
        {
            float d = Mathf.Abs(p.x - People[i].x);
            if (d < bestD && p.y < 1.8f) { bestD = d; best = i; }
        }
        return best;
    }
    public bool HasSomeoneHere => Near() >= 0;

    // 사람 `i`에게 말을 건다.
    public void Talk(int i)
    {
        Speaking = People[i].who;
        var say = Lines(Speaking, Wicked, Invited);
        var quests = GetComponent<QuestSystem>();
        childOfferOpen = Speaking == Who.Ai && quests != null && quests.CanOfferChild;
        if (Speaking == Who.Ai && quests != null && !childOfferOpen) say = quests.ChildDialogue(say);
        Begin(say);
        if (Speaking == Who.Mudang) HeardProphecy = true;
    }
    // 수락/거절 뒤에는 기존 아이 대사를 이어서 보여 준다.
    void AnswerChildOffer(bool accept)
    {
        if (!childOfferOpen) return;
        childOfferOpen = false;
        var quests = GetComponent<QuestSystem>();
        var original = Lines(Speaking, Wicked, Invited);
        var say = new string[original.Length + 1];
        say[0] = quests.AnswerChild(accept);
        System.Array.Copy(original, 0, say, 1, original.Length);
        Begin(say);
    }

    protected override void Close()
    {
        childOfferOpen = false;
        if (Speaking == Who.Cheongji) Invited = true;
        base.Close();
    }

    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        if (IsOpen)
        {
            if (childOfferOpen)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) AnswerChildOffer(true);
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Escape)) AnswerChildOffer(false);
                return;
            }
            TalkKeys(); return;
        }
        if (combat.Paused || !JinaUI.Interact) return;
        int i = Near();
        if (i >= 0) Talk(i);
    }

    // 말하는 사람은 선비 쪽을 본다.
    void LateUpdate()
    {
        if (arts == null || combat == null) return;
        float px = combat.player.transform.position.x;
        for (int i = 0; i < arts.Length; i++)
        {
            bool near = Mathf.Abs(px - People[i].x) < 5;
            int look = near ? (px >= People[i].x ? 1 : -1) : People[i].native;
            arts[i].flipX = look != People[i].native;
            if (People[i].who == Who.Yusaeng2 && !near) arts[i].flipX = true;
        }
    }

    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        KoreanUI.Apply();
        if (IsOpen && lines != null)
        {
            int i = System.Array.FindIndex(People, p => p.who == Speaking);
            var head = new Vector3(People[i].x, 1.9f, 0);
            if (childOfferOpen)
            {
                GetComponent<QuestSystem>().DrawChildOffer(AnswerChildOffer);
                return;
            }
            SpeechBubble.Draw(head, People[i].name, lines[Page], PageHint);
            return;
        }
        if (combat.Paused) return;
        int n = Near();
        if (n >= 0) JinaUI.Prompt("Enter: 말 걸기 (" + People[n].name + ")");
    }
}
