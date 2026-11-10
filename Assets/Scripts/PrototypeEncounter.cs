using UnityEngine;

// The first time the boss notices the seonbi: black bars slide in, the camera turns to the boss, it raises its club
// and roars (the screen shakes), its name card appears and it speaks; then the bars lift and the fight begins.
// The game is paused throughout; it plays once per run.
public class PrototypeEncounter : MonoBehaviour
{
    public PrototypeCombat combat;
    public const string Name = "미혹된 도깨비", Epithet = "망국의 원한에 사로잡힌 산의 주인";
    public const string Line = "죽인다죽인다죽인다죽인다";
    // Seconds: the whole scene, the camera pan each way, the roar, the name card, the line.
    public const float Length = 4.6f, Pan = .5f, RoarAt = .7f, RoarLength = 1.4f, CardAt = 1f, LineAt = 2.3f;

    private bool played, finished, shaken;
    private float startAt;

    public bool Playing => played && Time.time - startAt < Length;
    public float Elapsed => Time.time - startAt;
    public bool Roaring => Playing && Elapsed >= RoarAt && Elapsed < RoarAt + RoarLength;

    public void ResetEncounter() { played = finished = shaken = false; PrototypeView.Focus(0, 0); }
    public void Play()
    {
        played = true; finished = shaken = false; startAt = Time.time;
        combat.player.CanMove = false;
    }

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
        // Ease to the boss and back; the weight is how far the camera has turned from the seonbi.
        float weight = Mathf.SmoothStep(0, 1, Mathf.Clamp01(Elapsed / Pan) * Mathf.Clamp01((Length - Elapsed) / Pan));
        PrototypeView.Focus(combat.BossPosition.x, weight);
    }

    void OnGUI()
    {
        if (!Playing) return;
        GUI.depth = -50;
        PrototypeKoreanUI.Apply();
        float t = Elapsed, bars = Mathf.Clamp01(t / .4f) * Mathf.Clamp01((Length - t) / .4f);
        float bar = JinaUI.H * .11f * bars;
        JinaUI.Fill(new Rect(0, 0, JinaUI.W, bar), Color.black);
        JinaUI.Fill(new Rect(0, JinaUI.H - bar, JinaUI.W, bar), Color.black);
        // Name card slides in from the left at the lower left, above the bottom bar.
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
