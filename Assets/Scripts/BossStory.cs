using UnityEngine;

// ===== 도깨비 첫 조우 연출 (BossEncounter) =====
// 도깨비가 처음 선비를 알아챌 때: 검은 띠가 들어오고, 카메라가 도깨비로 돌고, 몽둥이를 들고
// 포효하고(화면 흔들림), 이름 카드가 나오고 한마디 한다. 그다음 띠가 걷히고 싸움이 시작된다.
// 그동안 게임은 멈추고, 한 판에 한 번만 나온다.
public class BossEncounter : MonoBehaviour
{
    public PrototypeCombat combat;
    public const string Name = "미혹된 도깨비", Epithet = "망국의 원한에 사로잡힌 산의 주인";
    public const string Line = "죽인다죽인다죽인다죽인다";
    // 초: 전체 장면, 카메라가 오가는 시간, 포효, 이름 카드, 대사.
    public const float Length = 4.6f, Pan = .5f, RoarAt = .7f, RoarLength = 1.4f, CardAt = 1f, LineAt = 2.3f;

    private bool played, finished, shaken;
    private float startAt;

    public bool Playing => played && Time.time - startAt < Length;
    public float Elapsed => Time.time - startAt;
    public bool Roaring => Playing && Elapsed >= RoarAt && Elapsed < RoarAt + RoarLength;

    public void ResetEncounter() { played = finished = shaken = false; PrototypeView.Focus(0, 0); }
    // 조우 연출을 시작한다(한 판에 한 번).
    public void Play()
    {
        played = true; finished = shaken = false; startAt = Time.time;
        combat.player.CanMove = false;
    }

    // 연출 시간을 재고, 카메라를 돌리고, 끝나면 게임을 다시 움직인다.
    void Update()
    {
        if (combat == null || finished) return;
        if (!played)
        {
            if (combat.BossAlerted && !combat.EnemyDefeated && combat.PlayerHealth > 0) Play();
            return;
        }
        if (!Playing)
        {
            finished = true;
            PrototypeView.Focus(0, 0);
            combat.player.CanMove = combat.PlayerHealth > 0;
            return;
        }
        combat.player.CanMove = false;
        if (!shaken && Elapsed >= RoarAt) { PrototypeView.Shake(.15f, .7f); shaken = true; }
        // 도깨비로 갔다가 돌아오는 부드러운 이동. 가중치는 카메라가 선비에게서 돌아간 정도.
        float weight = Mathf.SmoothStep(0, 1, Mathf.Clamp01(Elapsed / Pan) * Mathf.Clamp01((Length - Elapsed) / Pan));
        PrototypeView.Focus(combat.BossPosition.x, weight);
    }

    // 띠, 이름 카드, 대사를 그린다.
    void OnGUI()
    {
        if (!Playing) return;
        GUI.depth = -50;
        PrototypeKoreanUI.Apply();
        float t = Elapsed, bars = Mathf.Clamp01(t / .4f) * Mathf.Clamp01((Length - t) / .4f);
        float bar = JinaUI.H * .11f * bars;
        JinaUI.Fill(new Rect(0, 0, JinaUI.W, bar), Color.black);
        JinaUI.Fill(new Rect(0, JinaUI.H - bar, JinaUI.W, bar), Color.black);
        // 이름 카드는 왼쪽 아래, 아래 띠 위로 왼쪽에서 밀려 들어온다.
        float card = Mathf.Clamp01((t - CardAt) / .5f) * Mathf.Clamp01((Length - .4f - t) / .4f);
        if (card > 0)
        {
            const float size = 33;
            float x = 60 - (1 - card) * 80, y = JinaUI.H - bar - size * 3.4f;
            JinaUI.Text(new Rect(x, y, JinaUI.W, size), Epithet, 18, new Color(.92f, .9f, .85f, card), TextAnchor.MiddleLeft);
            JinaUI.Fill(new Rect(x, y + size * 1.05f, size * 7 * card, 3), new Color(.95f, .45f, .35f, card));
            JinaUI.Text(new Rect(x, y + size * 1.15f, JinaUI.W, size * 1.8f), Name, 43, new Color(.95f, .45f, .35f, card), TextAnchor.MiddleLeft, true);
        }
        if (t >= LineAt && t < Length - .3f)
            SpeechBubble.Draw(new Vector3(combat.BossPosition.x, 2.3f, 0), "도깨비", Line);
    }
}

// ===== 정화된 도깨비의 사연 (BossTalk) =====
// 정화된 도깨비에게 말을 걸 수 있다(E): 망한 나라의 망령들이 어떻게 저주했는지 이야기한다.
// 노인을 죽였다면 그 곁에 있게 해 달라고만 한다. E로 넘기고 Esc로 닫는다.
public class BossTalk : MonoBehaviour
{
    public PrototypeCombat combat;
    static readonly string[] Story =
    {
        "고맙소, 선비. 오래 꾼 악몽에서 막 깨어난 듯하오.",
        "나는 본디 이 산을 지키던 도깨비였소. 산짐승을 돌보고, 길 잃은 나그네를 마을까지 데려다주곤 했지.",
        "그런데 망한 나라의 망령들이 서쪽 비석 터에 모여들더니, 나라 잃은 원한을 내게 덧씌웠소.",
        "그 뒤로는 사람 냄새만 맡아도 분노가 끓었소. 내가 무엇을 지키던 자였는지조차 잊고 말았지.",
        "비석 터의 요괴들도 그 망령들의 졸개였소. 선비 덕에 이 산이 다시 숨을 쉬겠구려.",
        "한양 가는 길이라 하셨소? 이 산길만큼은 내가 지켜 드리리다. 부디 뜻을 이루시오.",
    };
    static readonly string[] Remorse = { "…나를 내버려 두시오. 노인장 곁에 조금만 더 있겠소." };

    public bool IsOpen { get; private set; }
    public int Page { get; private set; }
    string[] Pages => combat.Quest.progress.ElderDead ? Remorse : Story;
    // 정화됐고, 정화(와 슬픈 대사)가 끝났고, 선비가 가까이 서 있음.
    public bool Near => combat != null && combat.Quest != null && combat.BossPurified && combat.PlayerHealth > 0
        && Time.time >= combat.PurifiedAt + BossVisual.PurifyLength
        && (combat.Remorse == null || !combat.Remorse.Active || combat.Remorse.Elapsed > BossRemorse.Spoken)
        && Mathf.Abs(combat.player.transform.position.x - combat.BossPosition.x) < 2.4f;

    public void ResetTalk() { IsOpen = false; Page = 0; }
    // 대화를 열거나 다음 장으로 넘긴다(마지막 장 뒤에는 닫는다).
    public void Talk()
    {
        if (!IsOpen) { IsOpen = true; Page = 0; combat.player.CanMove = false; return; }
        if (++Page >= Pages.Length) Close();
    }
    void Close() { IsOpen = false; combat.player.CanMove = combat.PlayerHealth > 0; }

    // 정화된 도깨비 곁에서 E를 누르면 대화를 열거나 넘긴다.
    void Update()
    {
        if (combat == null) return;
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            else if (Input.GetKeyDown(KeyCode.E)) Talk();
        }
        else if (Near && !combat.Paused && Input.GetKeyDown(KeyCode.E)) Talk();
    }

    // 도깨비 사연 창과 E 안내를 그린다.
    void OnGUI()
    {
        if (combat == null) return;
        PrototypeKoreanUI.Apply();
        if (!IsOpen)
        {
            if (Near && !combat.Paused)
                JinaUI.Prompt("E: 도깨비와 이야기하기");
            return;
        }
        var pages = Pages;
        string hint = (Page + 1) + " / " + pages.Length + "   ·   E: " + (Page + 1 < pages.Length ? "다음" : "닫기") + "   Esc: 닫기";
        SpeechBubble.Draw(new Vector3(combat.BossPosition.x, 2.3f, 0), "도깨비", pages[Page], hint);
    }
}

// ===== 노인을 해친 도깨비의 죄책감 (BossRemorse) =====
// 노인을 죽인 도깨비가 정화되어 정신을 차림: 저주가 풀리면 시체 쪽으로 돌아서
// 무릎 꿇고 흐느끼며 자기가 한 일을 말한다. 여기서는 시간과 대사를 맡고, 그림은 BossVisual이 그린다.
public class BossRemorse : MonoBehaviour
{
    public PrototypeCombat combat;
    static readonly string[] Lines =
    {
        "…저 노인은… 내가… 내가 그리한 것이오?",
        "저주에 눈이 멀어, 마을을 지키던 이를 해치다니…",
        "노인장… 이 죄를 어찌 씻으리까…",
    };
    // 대사 한 줄의 초, 무릎 꿇기 전 깨닫는 시간, 무릎 꿇는 데 걸리는 시간.
    public const float LineLength = 3.5f, Realise = .6f, Kneel = .4f;
    // 마지막 대사가 끝나기까지의 초.
    public static float Spoken => Lines.Length * LineLength;

    public float Start => combat.PurifiedAt + BossVisual.PurifyLength;
    public bool Active => combat != null && combat.Quest != null && combat.BossPurified
        && combat.Quest.progress.ElderDead && Time.time >= Start;
    public float Elapsed => Time.time - Start;

    // 죄책감 장면의 대사를 말풍선으로 그린다.
    void OnGUI()
    {
        if (!Active || combat.Paused) return;
        int line = (int)(Elapsed / LineLength);
        var at = new Vector3(combat.BossPosition.x, 1.9f, 0);
        if (line < Lines.Length && SpeechBubble.OnScreen(at)) SpeechBubble.Draw(at, "도깨비", Lines[line]);
    }
}
