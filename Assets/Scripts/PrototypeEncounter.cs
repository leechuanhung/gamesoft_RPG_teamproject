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
    private GUIStyle nameStyle, epithetStyle;

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
        float bar = Screen.height * .11f * bars;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, bar), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, Screen.height - bar, Screen.width, bar), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (nameStyle == null)
        {
            nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
            epithetStyle = new GUIStyle(nameStyle);
        }
        // Name card slides in from the left at the lower left, above the bottom bar.
        float card = Mathf.Clamp01((t - CardAt) / .5f) * Mathf.Clamp01((Length - .4f - t) / .4f);
        if (card > 0)
        {
            float size = Mathf.Clamp(Screen.height / 22f, 20, 44), x = 60 - (1 - card) * 80, y = Screen.height - bar - size * 3.4f;
            epithetStyle.fontSize = Mathf.RoundToInt(size * .55f);
            epithetStyle.normal.textColor = new Color(.92f, .9f, .85f, card);
            nameStyle.fontSize = Mathf.RoundToInt(size * 1.3f);
            nameStyle.normal.textColor = new Color(.95f, .45f, .35f, card);
            GUI.Label(new Rect(x, y, Screen.width, size), Epithet, epithetStyle);
            GUI.color = new Color(.95f, .45f, .35f, card);
            GUI.DrawTexture(new Rect(x, y + size * 1.05f, size * 7 * card, 3), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y + size * 1.15f, Screen.width, size * 1.8f), Name, nameStyle);
        }
        if (t >= LineAt && t < Length - .3f)
            SpeechBubble.Draw(new Vector3(combat.BossPosition.x, 2.3f, 0), "도깨비", Line);
    }
}
