using UnityEngine;

// 개발자 단축키, 플레이어에게는 안 보인다.
//   F9: 도깨비 정화 루트를 끝낸 상태로 만든다. 들판 유령 전멸(동전 획득,
//       비석 봉인 해제), 노인 이야기 듣기, 비석 표식(달·강·산) 읽기, 도깨비에게 부적 사용
//       (+30, 가시덤불이 시듦), 그다음 선비를 나루터 뱃사공 옆에 둔다.
//       도깨비를 이미 해결했으면 나루터로 옮기기만 한다.
public class PrototypeCheats : MonoBehaviour
{
    public PrototypeCombat combat;
    public const KeyCode PurifyAndFerry = KeyCode.F9;
    public static readonly Vector2 FerrySpot = new Vector2(PrototypeWorld.Ferryman.x - 2, .3f);
    public float UsedAt { get; private set; } = -1;
    private string said = "";

    // F9를 누르면 나루터로 건너뛴다.
    void Update()
    {
        if (combat != null && Input.GetKeyDown(PurifyAndFerry)) SkipToFerry();
    }

    // 도깨비 정화 루트를 끝낸 상태로 만들고 나루터로 옮긴다.
    public bool SkipToFerry()
    {
        if (combat == null || combat.Quest == null) return false;
        if (combat.Intro != null && combat.Intro.IsPlaying) combat.Intro.Skip();
        if (combat.PlayerHealth <= 0 || combat.Paused) return false;
        var q = combat.Quest.progress;
        bool purified = false;
        if (q.Outcome == EncounterOutcome.Unresolved)
        {
            if (combat.Camp != null) foreach (var m in combat.Camp.mobs) combat.Camp.Hit(m, PrototypeCamp.GhostHealth);
            q.HearClue();
            // 달, 강, 산. 비석에서 어디까지 했든 거기서부터.
            int[] signs = { 2, 1, 3 };
            for (int i = 0; i < 3 && !q.HasCharm; i++) q.ChooseSymbol(signs[q.PuzzleStep]);
            purified = combat.TryPurifyBoss();
        }
        var player = combat.player;
        player.transform.position = FerrySpot;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) { body.velocity = Vector2.zero; body.position = FerrySpot; }
        if (combat.Maps != null) combat.Maps.Jump();
        PrototypeView.Focus(0, 0);
        UsedAt = Time.time;
        said = purified ? "[개발자] 도깨비 정화 루트를 마치고 나루터로 왔습니다. (선악 +30)" : "[개발자] 나루터로 왔습니다.";
        return true;
    }

    // 치트를 쓴 뒤 잠깐 알림을 띄운다.
    void OnGUI()
    {
        if (UsedAt < 0 || Time.time - UsedAt > 3f || combat == null || combat.Paused) return;
        PrototypeKoreanUI.Apply();
        JinaUI.Toast(said);
    }
}
