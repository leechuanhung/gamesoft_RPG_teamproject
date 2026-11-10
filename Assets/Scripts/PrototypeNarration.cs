using UnityEngine;

// Narration on a black screen: lines fade in one by one, a page at a time; hold Space to skip.
// The intro plays it at launch and the endings when the seonbi reaches the road to Hanyang.
public abstract class PrototypeNarration : MonoBehaviour
{
    public PrototypeCombat combat;
    const float LineGap = 2.6f, FadeIn = 1.2f, Hold = 2.4f, FadeOut = 1f, PageGap = .6f, SkipHold = 1f;
    public bool IsPlaying { get; protected set; }
    protected string[][] pages;
    protected int page;
    protected float pageTime, skipTime;

    // How dark the screen is behind the lines (0 to 1).
    protected abstract float Cover { get; }
    // Called after the last page or when skipped.
    protected abstract void Finish();
    // Lines that land harder than the rest.
    protected virtual bool Emphasis(int page, int line) => false;
    // Seconds before the first page starts (a fade to black, for the endings).
    protected virtual float Lead => 0;

    float PageLength(int p) => (pages[p].Length - 1) * LineGap + FadeIn + Hold;
    protected void Begin(string[][] text)
    {
        pages = text; page = 0; pageTime = -Lead; skipTime = 0;
        IsPlaying = true;
    }
    public void Skip()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        Finish();
    }
    // Runs the pages; returns false when nothing is playing.
    protected bool Advance()
    {
        if (!IsPlaying) return false;
        if (combat != null) combat.player.CanMove = false;
        skipTime = Input.GetKey(KeyCode.Space) ? skipTime + Time.deltaTime : 0;
        if (skipTime >= SkipHold) { Skip(); return false; }
        pageTime += Time.deltaTime;
        if (pageTime >= PageLength(page) + FadeOut + PageGap)
        {
            pageTime = 0;
            if (++page >= pages.Length) Skip();
        }
        return true;
    }

    protected void DrawCover()
    {
        GUI.depth = -100;
        PrototypeKoreanUI.Apply();
        JinaUI.Fill(new Rect(0, 0, JinaUI.W, JinaUI.H), new Color(0, 0, 0, Cover));
    }
    protected void DrawPage()
    {
        const int size = 30;
        float lineHeight = size * 2.1f;
        var lines = pages[page];
        float top = JinaUI.H / 2f - lines.Length * lineHeight / 2f;
        float pageFade = 1 - Mathf.Clamp01((pageTime - PageLength(page)) / FadeOut);
        for (int i = 0; i < lines.Length; i++)
        {
            float alpha = Mathf.Clamp01((pageTime - i * LineGap) / FadeIn) * pageFade;
            if (alpha <= 0) continue;
            bool emphasis = Emphasis(page, i);
            // Each line drifts up a little as it fades in.
            float rise = (1 - Mathf.Clamp01((pageTime - i * LineGap) / FadeIn)) * 8;
            var colour = emphasis ? new Color(.95f, .85f, .6f, alpha) : new Color(.92f, .9f, .85f, alpha);
            JinaUI.Text(new Rect(40, top + i * lineHeight + rise, JinaUI.W - 80, lineHeight), lines[i], emphasis ? 41 : size, colour, TextAnchor.MiddleCenter, emphasis);
        }
        var hint = new Rect(JinaUI.W - 340, JinaUI.H - 50, 310, 24);
        JinaUI.Text(hint, "스페이스바를 길게 눌러 건너뛰기", 15, new Color(.6f, .6f, .6f, .8f), TextAnchor.LowerRight);
        if (skipTime > 0)
            JinaUI.Fill(new Rect(hint.xMax - 200 * skipTime / SkipHold, hint.yMax + 6, 200 * skipTime / SkipHold, 3), new Color(.95f, .85f, .6f, .9f));
    }
}
