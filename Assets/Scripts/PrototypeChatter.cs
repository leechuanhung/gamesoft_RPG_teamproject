using UnityEngine;

// The keeper and the peddler pass the time while the seonbi is nearby: every so often one says something and the
// other answers. What they talk about follows the story (the boss loose, slain, purified, or the elder gone).
public class PrototypeChatter : MonoBehaviour
{
    public PrototypeCombat combat;
    // Seconds per line, between exchanges, and before the first; how near the seonbi must be to overhear.
    public const float LineLength = 3.4f, Gap = 8f, FirstAt = 4f, Range = 8f;
    // Each exchange is the keeper's line, then the peddler's.
    static readonly string[][] Uneasy =
    {
        new[] { "요즘 숲 쪽에서 밤마다 도깨비불이 보인다지요?", "그래서 산 넘는 장꾼이 줄었소. 장사가 영 시원찮구려." },
        new[] { "노인장께서 오늘도 숲 쪽을 한참 바라보시더이다.", "젊어서는 칼 꽤나 쓰셨다는 말이 있지요. 지금도 칼을 품고 다니신다던가." },
        new[] { "저 선비님 행색을 보니 과거 보러 가시는 길인가 보오.", "한양까지는 아직 멀었소. 화살은 넉넉히 챙겨 가시구려." },
        new[] { "서쪽 비석 터에 요괴가 들끓는다니 해가 지면 문을 걸어 잠가야겠소.", "망한 나라 군졸 귀신들이라던데… 무덤도 없이 떠도는 것들이지." },
        new[] { "숲을 지나면 나루터요. 뱃사공 영감이 요새 통 배를 띄우지 않는다더이다.", "강에서 밤마다 누가 운다지 뭐요. 물에 빠진 이들의 넋이라던가." },
        new[] { "강 건너 콩밭 마을에 콩쥐라는 효녀가 있다지요? 의붓동생한테 그리 시달린다던데.", "팥쥐 그것 말이오? 제 어미가 강에 빠져 죽은 것도 다 그 딸 업보지, 쯧쯧." },
        new[] { "그 집 젓갈이 그리 맛있다지요? 한번 들여와 볼까.", "값이 싸서 나도 두어 번 받아 왔는데… 이상하게 팔고 나면 찜찜합디다. 왜 그런지는 모르겠소." },
        new[] { "손님, 낮술은 안 되오. 과거 보러 가는 분이 얼굴 벌게서 어쩌시려고.", "주모는 저래 놓고 밤이면 혼자 한 사발씩 하지요." },
        new[] { "우리 집 국밥은 뼈를 사흘 고아요. 아무 데서나 못 먹는 맛이지.", "그 소리를 삼 년째 듣는데, 뼈는 늘 그 뼈 같단 말이오." },
    };
    static readonly string[][] Slain =
    {
        new[] { "도깨비가 쓰러졌다니 이제 밤길도 걸을 만하겠소.", "산 너머 장도 다시 서겠구려. 내일은 짐을 더 꾸려 와야겠소." },
        new[] { "그런데 그 도깨비, 본디 산을 지키던 녀석이었다던데…", "쉿, 다 지난 일이오. 산이 조용해졌으면 그만이지." },
        new[] { "산은 조용해졌는데, 강 건너 고개에 도적 떼가 생겼다더이다.", "도적들 눈빛이 도깨비처럼 퀭하다던데… 같은 것에 홀린 게 아닌가 싶소." },
        new[] { "나무하러 가던 아이들이 그 자리에 돌을 던지더이다. 잘 죽었다고.", "…애들 눈이 무섭구려. 노인장은 아무 말 없이 숲만 보고 계십디다." },
    };
    static readonly string[][] Purified =
    {
        new[] { "도깨비가 순해졌다더니, 아까 숲 어귀에서 꾸벅 인사를 하더이다.", "허허, 산길 길동무로 삼아도 되겠소. 장꾼들이 좋아하겠구려." },
        new[] { "칼 대신 부적을 쓰셨다니, 보기 드문 선비시오.", "그러게 말이오. 저런 분이 급제해야 나라가 바로 서지." },
        new[] { "도깨비 말이, 망령들이 강과 고개에도 손을 뻗었다더이다.", "가는 길마다 홀린 것이 있겠구려. 선비님, 부적은 넉넉하시오?" },
        new[] { "도깨비가 나뭇짐을 져다 주길래 막걸리 한 사발 줬더니 울더이다.", "허허. 몇백 년 만에 얻어먹는 술이었겠구려." },
    };
    static readonly string[][] Mourning =
    {
        new[] { "노인장 계시던 자리가 텅 비었구려…", "장터마다 이 소식을 전해야 하니 발걸음이 무겁소." },
        new[] { "그 연세에 칼을 뽑아 우리를 먼저 피하게 하셨지요.", "…우리 목숨은 노인장께 빚진 것이오." },
        new[] { "아침마다 노인장 드시던 국 한 그릇을 저도 모르게 푸고 있구려.", "…내려놓지 마시오. 그 자리에 놓아 두시오." },
    };

    private float nextAt = FirstAt, startedAt = -1;
    private int count;
    private string[] current;

    public static string[][] Topics(QuestProgress q) => q.ElderDead ? Mourning
        : q.Outcome == EncounterOutcome.Purified ? Purified : q.Outcome == EncounterOutcome.Slain ? Slain : Uneasy;
    public bool Talking => startedAt >= 0;
    // Both at their places, nothing else going on, and the seonbi within earshot of the tavern and the stall.
    public bool CanChat => combat != null && combat.Quest != null && combat.Village != null && !combat.Paused
        && combat.PlayerHealth > 0 && combat.Village.KeeperHome && combat.Village.PeddlerHome
        && (!combat.Village.Alarmed || combat.Village.Cleared)
        && Mathf.Abs(combat.player.transform.position.x - (PrototypeWorld.Tavern.x + PrototypeWorld.Peddler.x) / 2) < Range;

    public void ResetChatter() { nextAt = Time.time + FirstAt; startedAt = -1; count = 0; }
    public void Begin()
    {
        var topics = Topics(combat.Quest.progress);
        current = topics[count++ % topics.Length];
        startedAt = Time.time;
    }

    void Update()
    {
        if (combat == null) return;
        if (Talking)
        {
            // Interrupted (a shop opened, the alarm went up) or finished: wait a while before the next.
            if (!CanChat || Time.time - startedAt >= LineLength * 2) { startedAt = -1; nextAt = Time.time + Gap; }
        }
        else if (Time.time >= nextAt && CanChat) Begin();
    }

    void OnGUI()
    {
        if (!Talking || !CanChat) return;
        bool keeper = Time.time - startedAt < LineLength;
        // Near a shop its own prompt takes the bottom of the screen; the bubbles sit above the speakers.
        var at = keeper ? new Vector3(PrototypeWorld.Tavern.x, 1.4f, 0) : new Vector3(PrototypeWorld.Peddler.x, 1.55f, 0);
        if (SpeechBubble.OnScreen(at)) SpeechBubble.Draw(at, keeper ? "주모" : "보부상", current[keeper ? 0 : 1]);
    }
}
