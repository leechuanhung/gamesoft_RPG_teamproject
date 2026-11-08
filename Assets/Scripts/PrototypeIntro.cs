using UnityEngine;

// Opening narration, played once per launch; afterwards the black screen fades back to the game.
public class PrototypeIntro : PrototypeNarration
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
    // The naming line lands harder than the rest.
    protected override bool Emphasis(int page, int line) => page == 3 && line == 1;
    protected override float Cover => IsPlaying ? 1 : 1 - Mathf.Clamp01(revealTime / Reveal);
    protected override void Finish()
    {
        revealTime = 0;
        if (combat != null) combat.player.CanMove = combat.PlayerHealth > 0;
    }
    void Update()
    {
        if (Advance()) return;
        if (revealTime >= 0) revealTime += Time.deltaTime;
        if (revealTime > Reveal) enabled = false;
    }
    void OnGUI()
    {
        if (!IsPlaying && revealTime < 0) return;
        DrawCover();
        if (IsPlaying) DrawPage();
    }
}
