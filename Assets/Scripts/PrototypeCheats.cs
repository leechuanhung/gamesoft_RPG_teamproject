using UnityEngine;

// Developer shortcuts, not shown to players.
//   F9: the dokkaebi's purification route, finished as if played: every field ghost laid (their coins found, the
//       stele's seal lifted), the elder heard, the stele's signs read (moon, river, mountain), the charm used on the
//       dokkaebi (+30, the thorns wither), then the seonbi is put at the ferry beside the ferryman.
//       If the dokkaebi is already dealt with, it only moves him to the ferry.
public class PrototypeCheats : MonoBehaviour
{
    public PrototypeCombat combat;
    public const KeyCode PurifyAndFerry = KeyCode.F9;
    public static readonly Vector2 FerrySpot = new Vector2(PrototypeWorld.Ferryman.x - 2, .3f);
    public float UsedAt { get; private set; } = -1;
    private string said = "";

    void Update()
    {
        if (combat != null && Input.GetKeyDown(PurifyAndFerry)) SkipToFerry();
    }

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
            // Moon, river, mountain, from wherever he had got to on the stele.
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

    void OnGUI()
    {
        if (UsedAt < 0 || Time.time - UsedAt > 3f || combat == null || combat.Paused) return;
        PrototypeKoreanUI.Apply();
        JinaUI.Toast(said);
    }
}
