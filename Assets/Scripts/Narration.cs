using UnityEngine;

// ===== 낭독 공통: 검은 화면에 한 줄씩 (Narration) =====
// 검은 화면의 낭독: 줄이 하나씩 나타나고 한 장씩 넘어간다. 스페이스를 길게 누르면 건너뛴다.
// 인트로는 시작할 때, 결말은 선비가 한양 가는 길에 닿았을 때 재생한다.
public abstract class Narration : MonoBehaviour
{
    public Combat combat;
    const float LineGap = 2.6f, FadeIn = 1.2f, Hold = 2.4f, FadeOut = 1f, PageGap = .6f, SkipHold = 1f;
    public bool IsPlaying { get; protected set; }
    protected string[][] pages;
    protected int page;
    protected float pageTime, skipTime;

    // 글 뒤 화면이 얼마나 어두운지(0~1).
    protected abstract float Cover { get; }
    // 마지막 장 뒤나 건너뛸 때 부른다.
    protected abstract void Finish();
    // 다른 줄보다 무겁게 내려앉는 줄.
    protected virtual bool Emphasis(int page, int line) => false;
    // 첫 장이 시작하기 전 초(결말에서는 어둠으로 바뀌는 시간).
    protected virtual float Lead => 0;

    float PageLength(int p) => (pages[p].Length - 1) * LineGap + FadeIn + Hold;
    // 낭독을 시작한다.
    protected void Begin(string[][] text)
    {
        pages = text; page = 0; pageTime = -Lead; skipTime = 0;
        IsPlaying = true;
    }
    // 낭독을 건너뛴다.
    public void Skip()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        Finish();
    }
    // 장을 돌린다. 재생 중인 게 없으면 false.
    protected bool Advance()
    {
        if (!IsPlaying) return false;
        if (combat != null) combat.player.CanMove = false;
        skipTime = Input.GetKey(KeyCode.Space) ? skipTime + Time.deltaTime : 0;
        if (skipTime >= SkipHold) { Skip(); return false; }
        pageTime += Time.deltaTime;
        if (pageTime >= PageLength(page) + FadeOut + PageGap)
        {
            pageTime = 0;
            if (++page >= pages.Length) Skip();
        }
        return true;
    }

    // 검은 바탕을 그린다.
    protected void DrawCover()
    {
        GUI.depth = -100;
        KoreanUI.Apply();
        JinaUI.Fill(new Rect(0, 0, JinaUI.W, JinaUI.H), new Color(0, 0, 0, Cover));
    }
    // 지금 장의 줄들을 그린다.
    protected void DrawPage()
    {
        const int size = 30;
        float lineHeight = size * 2.1f;
        var lines = pages[page];
        float top = JinaUI.H / 2f - lines.Length * lineHeight / 2f;
        float pageFade = 1 - Mathf.Clamp01((pageTime - PageLength(page)) / FadeOut);
        for (int i = 0; i < lines.Length; i++)
        {
            float alpha = Mathf.Clamp01((pageTime - i * LineGap) / FadeIn) * pageFade;
            if (alpha <= 0) continue;
            bool emphasis = Emphasis(page, i);
            // 줄마다 나타나면서 조금 위로 떠오른다.
            float rise = (1 - Mathf.Clamp01((pageTime - i * LineGap) / FadeIn)) * 8;
            var colour = emphasis ? new Color(.95f, .85f, .6f, alpha) : new Color(.92f, .9f, .85f, alpha);
            JinaUI.Text(new Rect(40, top + i * lineHeight + rise, JinaUI.W - 80, lineHeight), lines[i], emphasis ? 41 : size, colour, TextAnchor.MiddleCenter, emphasis);
        }
        var hint = new Rect(JinaUI.W - 340, JinaUI.H - 50, 310, 24);
        JinaUI.Text(hint, "스페이스바를 길게 눌러 건너뛰기", 15, new Color(.6f, .6f, .6f, .8f), TextAnchor.LowerRight);
        if (skipTime > 0)
            JinaUI.Fill(new Rect(hint.xMax - 200 * skipTime / SkipHold, hint.yMax + 6, 200 * skipTime / SkipHold, 3), new Color(.95f, .85f, .6f, .9f));
    }
}

// ===== 인트로 (IntroNarration) =====
// 시작 낭독, 실행할 때 한 번. 끝나면 검은 화면이 게임으로 밝아진다.
public class IntroNarration : Narration
{
    static readonly string[][] Pages =
    {
        new[] { "유교가 백성(民)을 사람(人)으로 만드는 시대에", "옛 망국의 망령들이 떠돌아 다닌다" },
        new[] { "산군(山君)의 호환(虎患)이 호상(好喪)인 기이한 삶 안에서" },
        new[] { "큰 뜻을 품고 산을 넘으며", "비가 와도 갓 아래에서 품위를 지키고", "배운 도리를 세상에 펼치고자 하는 자들" },
        new[] { "우리는 그들을", "선비라고 부른다" },
        new[] { "그리고 한 선비가, 길을 지나가고 있었다." },
    };
    const float Reveal = 1.5f;
    private float revealTime = -1;

    void Awake() { Begin(Pages); }
    // 이름을 밝히는 줄은 다른 줄보다 무겁게 내려앉는다.
    protected override bool Emphasis(int page, int line) => page == 3 && line == 1;
    protected override float Cover => IsPlaying ? 1 : 1 - Mathf.Clamp01(revealTime / Reveal);
    // 인트로가 끝나면 선비가 움직일 수 있게 한다.
    protected override void Finish()
    {
        revealTime = 0;
        if (combat != null) combat.player.CanMove = combat.PlayerHealth > 0;
    }
    // 인트로 장을 넘기고, 화면이 다 밝아지면 꺼진다.
    void Update()
    {
        if (Advance()) return;
        if (revealTime >= 0) revealTime += Time.deltaTime;
        if (revealTime > Reveal) enabled = false;
    }
    // 인트로를 그린다.
    void OnGUI()
    {
        if (!IsPlaying && revealTime < 0) return;
        DrawCover();
        if (IsPlaying) DrawPage();
    }
}

// ===== 결말 (EndingNarration) =====
// 도깨비를 해결하고 한양 가는 길에 닿으면 화면이 어두워지고 선비의 선택에 맞는 결말을
// 낭독한 뒤, R로 다시 시작할 때까지 제목이 남는다.
public class EndingNarration : Narration
{
    public enum Kind { Slain, Purified, Sacrifice }
    static readonly string[][] SlainPages =
    {
        new[] { "도깨비는 쓰러졌고", "마을에는 다시 아궁이 연기가 피어올랐다" },
        new[] { "사람들은 오래도록 선비의 칼끝을 이야기했으나", "도깨비가 어찌 그리 사나워졌는지는 아무도 알지 못했다" },
        new[] { "선비는 다시 한양으로 발길을 옮겼다" },
    };
    static readonly string[][] PurifiedPages =
    {
        new[] { "저주가 풀리자", "도깨비는 선비에게 고개를 숙였다" },
        new[] { "칼 대신 부적을 든 선택은", "산에도 마을에도 평온을 남겼다" },
        new[] { "선비는 다시 한양으로 발길을 옮겼다" },
    };
    static readonly string[][] SacrificePages =
    {
        new[] { "도깨비의 위협은 사라졌으나", "노인은 끝내 돌아오지 않았다" },
        new[] { "마을 사람들은 노인이 쓰러진 자리에 돌을 쌓고", "오래도록 그 앞을 지나지 못했다" },
        new[] { "선비의 선택은 마을에 깊은 상처를 남겼고", "선비는 무거운 걸음으로 한양을 향했다" },
    };
    // 희생: 도깨비가 나중에 정화되어 노인 곁에서 정신을 차렸을 때.
    static readonly string[][] RemorsePages =
    {
        new[] { "저주가 풀리고 도깨비는 제정신을 찾았으나", "노인은 끝내 돌아오지 않았다" },
        new[] { "도깨비는 노인이 쓰러진 자리를 떠나지 못하고", "오래도록 그 곁을 지켰다" },
        new[] { "선비의 선택은 마을에 깊은 상처를 남겼고", "선비는 무거운 걸음으로 한양을 향했다" },
    };
    // 콩밭 마을에 남기고 가는 것. 한양 가는 길은 계속되고, 보름달은 아직이다.
    public static string[] Coda(bool unmasked, bool stayed, bool keepsake)
    {
        if (unmasked && keepsake) return new[] { "품속의 은비녀가 유난히 차가웠다", "뒷산에서 기다리는 이를 두고, 선비는 오래 걸음을 떼지 못했다" };
        if (unmasked) return new[] { "뒷산에서는 아직도 누군가 기다리고 있었다", "선비는 고갯마루에서 한 번, 오래 뒤를 돌아보았다" };
        if (stayed) return new[] { "그날 밤 문종이에 뚫린 구멍과", "뒷산으로 와 달라던 목소리를, 선비는 오래도록 잊지 못했다" };
        return new[] { "강 건너 콩밭 마을에서는 보름 잔치 준비가 한창이었다", "선비는 그 잔칫상이 무엇으로 차려지는지 알지 못했다" };
    }
    // 뒷산 요괴를 해결한 뒤 콩밭 마을에 남기고 가는 것: `patjwi` 팥쥐가 살았음, `lost` 죽은 마을 사람 수.
    public static string[] KongCoda(EncounterOutcome kong, bool patjwi, int lost)
    {
        if (kong == EncounterOutcome.Purified && patjwi && lost == 0) return new[] { "보름날, 잔치 대신 제사가 열렸다", "팥쥐는 어머니의 비녀를 꽂고, 처음으로 마을 사람들 앞에서 울었다" };
        if (kong == EncounterOutcome.Purified && patjwi) return new[] { "보름날, 잔치 대신 장례가 열렸다", "콩쥐와 팥쥐는 빈 집들을 하나씩 돌며 오래도록 절을 올렸다" };
        if (kong == EncounterOutcome.Purified) return new[] { "두꺼비는 사라졌으나 팥쥐는 돌아오지 않았다", "콩쥐는 동생의 무덤 곁을 끝내 떠나지 못했다" };
        if (lost > 0) return new[] { "사람들은 괴물이 죽었다고만 했다", "선비만이, 독 속에서 끝까지 울던 목소리와 비어 버린 집들을 기억했다" };
        return new[] { "사람들은 괴물이 죽었다고만 했다", "선비만이, 독 속에서 끝까지 울던 목소리를 기억했다" };
    }
    // 결말 이야기 뒤에 콩밭 마을 이야기를 덧붙인다.
    static string[][] WithCoda(string[][] story, string[] coda)
    {
        var pages = new string[story.Length + 1][];
        System.Array.Copy(story, pages, story.Length);
        pages[story.Length] = coda;
        return pages;
    }
    public static string Title(Kind kind) => kind == Kind.Sacrifice ? "희생 위의 승리" : kind == Kind.Purified ? "자비로운 해결사" : "토벌의 영웅";
    // 한양: 좌의정이 쓰러진 다음 날 아침의 과거장. 선악으로 갈린다(기획서.md 4장 결말).
    public enum Verdict { None, Loyal, Sword, Failed }
    public static Verdict VerdictFor(QuestProgress q) => q.FinalOutcome == EncounterOutcome.Unresolved ? Verdict.None
        : q.Honor >= 80 ? Verdict.Loyal : q.Honor >= 0 ? Verdict.Sword : Verdict.Failed;
    public static string VerdictTitle(Verdict v) => v == Verdict.Loyal ? "성군의 신하" : v == Verdict.Sword ? "칼을 든 선비" : "낙방한 선비";
    // `lostSomeone`: 노인이나 콩밭 마을 사람을 잃었다(희생 위의 길: 끝에 한 장이 붙는다).
    public static string[][] Finale(Verdict v, bool lostSomeone)
    {
        var pages = new System.Collections.Generic.List<string[]>();
        if (v == Verdict.Loyal)
        {
            pages.Add(new[] { "좌의정이 쓰러지자 한양 가는 길마다 걸린 저주가 풀렸고", "이튿날 아침, 과거장의 문이 열렸다" });
            pages.Add(new[] { "붓을 들자 길에서 만난 얼굴들이 하나씩 떠올랐다", "산의 주인이 된 도깨비, 강가의 은비녀, 다시 서로의 이름을 부른 자매" });
            pages.Add(new[] { "방이 붙던 날, 어린 임금은 장원의 답안을 오래 들여다보았다", "시관이 말했다. \"글보다 길이 먼저 답했습니다\"" });
        }
        else if (v == Verdict.Sword)
        {
            pages.Add(new[] { "좌의정이 쓰러지자 한양 가는 길마다 걸린 저주가 풀렸고", "이튿날 아침, 과거장의 문이 열렸다" });
            pages.Add(lostSomeone ? new[] { "선비는 급제했다", "그러나 돌아보면 길에 남은 것은 무덤뿐이었다" }
                : new[] { "선비는 급제했다", "그러나 칼끝에 밴 피 냄새는 오래도록 지워지지 않았다" });
            pages.Add(new[] { "어린 임금은 칼 잘 쓰는 신하를 얻었고", "선비는 그 칼을 어디에 쓸지 아직 알지 못했다" });
        }
        else
        {
            pages.Add(new[] { "좌의정이 쓰러지고 길의 저주도 풀렸으나", "관군에게 쫓기던 선비의 이름은 이미 수배 방에 올라 있었다" });
            pages.Add(new[] { "과거장에 들어섰으나 붓이 움직이지 않았다", "종이 위로 길에서 벤 얼굴들이 번졌다" });
            pages.Add(new[] { "흩어지던 그림자가 귓가에 속삭였다", "\"너도 우리와 같다\"" });
        }
        if (lostSomeone) pages.Add(new[] { "방이 붙던 날, 선비는 길에 두고 온 이름들을 하나씩 불렀다", "대답하는 이는 없었다" });
        return pages.ToArray();
    }
    // 화면에 남는 결말 제목: 한양까지 왔으면 과거장의 판정, 아니면 도깨비 결말.
    public string TitleText => Final != Verdict.None ? VerdictTitle(Final) : Title(Ending);
    public Verdict Final { get; private set; }
    const float FadeToBlack = 1f, TitleFade = 1.2f;

    // 시작한 순간부터 R까지 켜져 있다: 그 뒤로 게임은 멈춰 있다.
    public bool Active { get; private set; }
    public Kind Ending { get; private set; }
    private float time, titleTime = -1;

    // 진행 상태에 맞는 결말 종류.
    public static Kind For(QuestProgress progress) => progress.ElderDead ? Kind.Sacrifice
        : progress.Outcome == EncounterOutcome.Purified ? Kind.Purified : Kind.Slain;
    // 결말을 재생한다.
    public void Play(Kind kind)
    {
        Ending = kind; Active = true; time = 0; titleTime = -1;
        if (combat != null) combat.player.CanMove = false;
        bool remorse = combat != null && combat.Quest != null && combat.Quest.progress.Outcome == EncounterOutcome.Purified;
        var story = kind == Kind.Sacrifice ? (remorse ? RemorsePages : SacrificePages) : kind == Kind.Purified ? PurifiedPages : SlainPages;
        var q = combat != null && combat.Quest != null ? combat.Quest.progress : null;
        var coda = q != null && q.KongOutcome != EncounterOutcome.Unresolved
            ? KongCoda(q.KongOutcome, combat.Kong == null || combat.Kong.Alive(KongVillage.Who.Patjwi), q.VillagersLost)
            : Coda(combat != null && combat.Kong != null && combat.Kong.Revealed, combat != null && combat.Kong != null && combat.Kong.Stayed,
                JinaItems.Inventory != null && JinaItems.Inventory.GetQuantity(JinaItems.Keepsake) > 0);
        Final = q != null ? VerdictFor(q) : Verdict.None;
        var pages = WithCoda(story, coda);
        if (Final != Verdict.None)
        {
            // 도깨비 이야기의 마지막 장("한양으로 발길을 옮겼다")은 빼고 과거장의 아침을 잇는다.
            var list = new System.Collections.Generic.List<string[]>(story);
            list.RemoveAt(list.Count - 1);
            list.Add(coda);
            list.AddRange(Finale(Final, q.ElderDead || q.VillagersLost > 0));
            pages = list.ToArray();
        }
        Begin(pages);
    }
    public void ResetEnding() { Active = IsPlaying = false; titleTime = -1; }
    protected override float Lead => FadeToBlack;
    protected override float Cover => Active ? Mathf.Clamp01(time / FadeToBlack) : 0;
    protected override void Finish() { titleTime = 0; }

    // 선비가 한양 가는 길에 닿으면 결말을 시작한다.
    void Update()
    {
        if (combat == null || combat.Quest == null) return;
        if (!Active)
        {
            // 과거장 가는 길은 좌의정 댁 너머라서 콩쥐 요괴와 좌의정도 해결해야 한다.
            bool kongDone = combat.KongBoss == null || combat.KongBoss.Resolved;
            bool finalDone = combat.FinalBoss == null || combat.FinalBoss.Resolved;
            if (combat.EnemyDefeated && kongDone && finalDone && combat.PlayerHealth > 0 && combat.player.transform.position.x > WorldBuilder.GoalX - 1)
                Play(For(combat.Quest.progress));
            return;
        }
        time += Time.deltaTime;
        if (!Advance() && titleTime >= 0) titleTime += Time.deltaTime;
        combat.player.CanMove = false;
    }

    // 결말 낭독과 제목을 그린다.
    void OnGUI()
    {
        if (!Active) return;
        DrawCover();
        if (IsPlaying && pageTime >= 0) DrawPage();
        if (titleTime < 0) return;
        float alpha = Mathf.Clamp01(titleTime / TitleFade), y = JinaUI.H / 2f;
        var sub = new Color(.92f, .9f, .85f, alpha * .9f);
        JinaUI.Text(new Rect(0, y - 96, JinaUI.W, 42), "결말", 21, sub, TextAnchor.MiddleCenter);
        JinaUI.Text(new Rect(0, y - 48, JinaUI.W, 72), TitleText, 48, new Color(.95f, .85f, .6f, alpha), TextAnchor.MiddleCenter, true);
        JinaUI.Fill(new Rect(JinaUI.W / 2 - 120 * alpha, y + 30, 240 * alpha, 2), new Color(.95f, .85f, .6f, alpha * .7f));
        JinaUI.Text(new Rect(0, y + 48, JinaUI.W, 42), "R: 처음부터 다시 시작", 21, sub, TextAnchor.MiddleCenter);
    }
}
