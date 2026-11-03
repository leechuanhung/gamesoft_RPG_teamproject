using UnityEngine;

// A purified boss that had killed the elder comes to itself: once the curse has lifted it turns toward his body,
// sinks to its knees and sobs, saying what it has done. This times the scene and speaks; BossVisual draws it.
public class BossRemorse : MonoBehaviour
{
    public PrototypeCombat combat;
    static readonly string[] Lines =
    {
        "…저 노인은… 내가… 내가 그리한 것이오?",
        "저주에 눈이 멀어, 마을을 지키던 이를 해치다니…",
        "노인장… 이 죄를 어찌 씻으리까…",
    };
    // Seconds per line; the turn of realisation before he kneels, and how long the kneel takes.
    public const float LineLength = 3.5f, Realise = .6f, Kneel = .4f;
    // Seconds until his last line is done.
    public static float Spoken => Lines.Length * LineLength;

    public float Start => combat.PurifiedAt + BossVisual.PurifyLength;
    public bool Active => combat != null && combat.Quest != null && combat.BossPurified
        && combat.Quest.progress.ElderDead && Time.time >= Start;
    public float Elapsed => Time.time - Start;

    void OnGUI()
    {
        if (!Active || combat.Paused) return;
        int line = (int)(Elapsed / LineLength);
        var at = new Vector3(combat.BossPosition.x, 1.9f, 0);
        if (line < Lines.Length && SpeechBubble.OnScreen(at)) SpeechBubble.Draw(at, "도깨비", Lines[line]);
    }
}
