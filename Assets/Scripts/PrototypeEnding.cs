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
    public static string Title(Kind kind) => kind == Kind.Sacrifice ? "희생 위의 승리" : kind == Kind.Purified ? "자비로운 해결사" : "토벌의 영웅";
    const float FadeToBlack = 1f, TitleFade = 1.2f;

    // Active from the moment it starts until R: the game stays paused behind it.
    public bool Active { get; private set; }
    public Kind Ending { get; private set; }
    private float time, titleTime = -1;
    private GUIStyle titleStyle, subStyle;

    public static Kind For(QuestProgress progress) => progress.ElderDead ? Kind.Sacrifice
        : progress.Outcome == EncounterOutcome.Purified ? Kind.Purified : Kind.Slain;
    public void Play(Kind kind)
    {
        Ending = kind; Active = true; time = 0; titleTime = -1;
        if (combat != null) combat.player.CanMove = false;
        bool remorse = combat != null && combat.Quest != null && combat.Quest.progress.Outcome == EncounterOutcome.Purified;
        Begin(kind == Kind.Sacrifice ? (remorse ? RemorsePages : SacrificePages) : kind == Kind.Purified ? PurifiedPages : SlainPages);
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
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            subStyle = new GUIStyle(titleStyle);
        }
        float size = Mathf.Clamp(Screen.height / 24f, 18, 40), alpha = Mathf.Clamp01(titleTime / TitleFade);
        titleStyle.fontSize = Mathf.RoundToInt(size * 1.6f);
        titleStyle.normal.textColor = new Color(.95f, .85f, .6f, alpha);
        subStyle.fontSize = Mathf.RoundToInt(size * .7f);
        subStyle.normal.textColor = new Color(.92f, .9f, .85f, alpha * .9f);
        float y = Screen.height / 2f;
        GUI.Label(new Rect(0, y - size * 3.2f, Screen.width, size * 1.4f), "결말", subStyle);
        GUI.Label(new Rect(0, y - size * 1.6f, Screen.width, size * 2.4f), Title(Ending), titleStyle);
        GUI.Label(new Rect(0, y + size * 1.6f, Screen.width, size * 1.4f), "R: 처음부터 다시 시작", subStyle);
    }
}
