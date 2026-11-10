using UnityEngine;

// When the seonbi reaches the road to Hanyang with the boss dealt with, the screen fades to black and the ending
// for his choices is narrated, then its title stays up until R starts over.
public class PrototypeEnding : PrototypeNarration
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
    // The sacrifice, when the boss was purified afterwards and came to itself beside the elder.
    static readonly string[][] RemorsePages =
    {
        new[] { "저주가 풀리고 도깨비는 제정신을 찾았으나", "노인은 끝내 돌아오지 않았다" },
        new[] { "도깨비는 노인이 쓰러진 자리를 떠나지 못하고", "오래도록 그 곁을 지켰다" },
        new[] { "선비의 선택은 마을에 깊은 상처를 남겼고", "선비는 무거운 걸음으로 한양을 향했다" },
    };
    // What he leaves behind him in Kongbat. The road to Hanyang goes on; the full moon has not come yet.
    public static string[] Coda(bool unmasked, bool stayed, bool keepsake)
    {
        if (unmasked && keepsake) return new[] { "품속의 은비녀가 유난히 차가웠다", "뒷산에서 기다리는 이를 두고, 선비는 오래 걸음을 떼지 못했다" };
        if (unmasked) return new[] { "뒷산에서는 아직도 누군가 기다리고 있었다", "선비는 고갯마루에서 한 번, 오래 뒤를 돌아보았다" };
        if (stayed) return new[] { "그날 밤 문종이에 뚫린 구멍과", "뒷산으로 와 달라던 목소리를, 선비는 오래도록 잊지 못했다" };
        return new[] { "강 건너 콩밭 마을에서는 보름 잔치 준비가 한창이었다", "선비는 그 잔칫상이 무엇으로 차려지는지 알지 못했다" };
    }
    static string[][] WithCoda(string[][] story, string[] coda)
    {
        var pages = new string[story.Length + 1][];
        System.Array.Copy(story, pages, story.Length);
        pages[story.Length] = coda;
        return pages;
    }
    public static string Title(Kind kind) => kind == Kind.Sacrifice ? "희생 위의 승리" : kind == Kind.Purified ? "자비로운 해결사" : "토벌의 영웅";
    const float FadeToBlack = 1f, TitleFade = 1.2f;

    // Active from the moment it starts until R: the game stays paused behind it.
    public bool Active { get; private set; }
    public Kind Ending { get; private set; }
    private float time, titleTime = -1;

    public static Kind For(QuestProgress progress) => progress.ElderDead ? Kind.Sacrifice
        : progress.Outcome == EncounterOutcome.Purified ? Kind.Purified : Kind.Slain;
    public void Play(Kind kind)
    {
        Ending = kind; Active = true; time = 0; titleTime = -1;
        if (combat != null) combat.player.CanMove = false;
        bool remorse = combat != null && combat.Quest != null && combat.Quest.progress.Outcome == EncounterOutcome.Purified;
        var story = kind == Kind.Sacrifice ? (remorse ? RemorsePages : SacrificePages) : kind == Kind.Purified ? PurifiedPages : SlainPages;
        Begin(WithCoda(story, Coda(combat != null && combat.Kong != null && combat.Kong.Revealed, combat != null && combat.Kong != null && combat.Kong.Stayed,
            JinaItems.Inventory != null && JinaItems.Inventory.GetQuantity(JinaItems.Keepsake) > 0)));
    }
    public void ResetEnding() { Active = IsPlaying = false; titleTime = -1; }
    protected override float Lead => FadeToBlack;
    protected override float Cover => Active ? Mathf.Clamp01(time / FadeToBlack) : 0;
    protected override void Finish() { titleTime = 0; }

    void Update()
    {
        if (combat == null || combat.Quest == null) return;
        if (!Active)
        {
            if (combat.EnemyDefeated && combat.PlayerHealth > 0 && combat.player.transform.position.x > PrototypeWorld.GoalX - 1)
                Play(For(combat.Quest.progress));
            return;
        }
        time += Time.deltaTime;
        if (!Advance() && titleTime >= 0) titleTime += Time.deltaTime;
        combat.player.CanMove = false;
    }

    void OnGUI()
    {
        if (!Active) return;
        DrawCover();
        if (IsPlaying && pageTime >= 0) DrawPage();
        if (titleTime < 0) return;
        float alpha = Mathf.Clamp01(titleTime / TitleFade), y = JinaUI.H / 2f;
        var sub = new Color(.92f, .9f, .85f, alpha * .9f);
        JinaUI.Text(new Rect(0, y - 96, JinaUI.W, 42), "결말", 21, sub, TextAnchor.MiddleCenter);
        JinaUI.Text(new Rect(0, y - 48, JinaUI.W, 72), Title(Ending), 48, new Color(.95f, .85f, .6f, alpha), TextAnchor.MiddleCenter, true);
        JinaUI.Fill(new Rect(JinaUI.W / 2 - 120 * alpha, y + 30, 240 * alpha, 2), new Color(.95f, .85f, .6f, alpha * .7f));
        JinaUI.Text(new Rect(0, y + 48, JinaUI.W, 42), "R: 처음부터 다시 시작", 21, sub, TextAnchor.MiddleCenter);
    }
}
