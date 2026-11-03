using UnityEngine;

// Opening narration on a black screen. Lines fade in one by one, a page at a time; hold Space to skip.
public class PrototypeIntro : MonoBehaviour
{
    public PrototypeCombat combat;
    static readonly string[][] Pages =
    {
        new[] { "유교가 백성(民)을 사람(人)으로 만드는 시대에", "옛 망국의 망령들이 떠돌아 다닌다" },
        new[] { "산군(山君)의 호환(虎患)이 호상(好喪)인 기이한 삶 안에서" },
        new[] { "큰 뜻을 품고 산을 넘으며", "비가 와도 갓 아래에서 품위를 지키고", "배운 도리를 세상에 펼치고자 하는 자들" },
        new[] { "우리는 그들을", "선비라고 부른다" },
        new[] { "그리고 한 선비가, 길을 지나가고 있었다." },
    };
    const float LineGap = 2.6f, FadeIn = 1.2f, Hold = 2.4f, FadeOut = 1f, PageGap = .6f, Reveal = 1.5f, SkipHold = 1f;
    public bool IsPlaying { get; private set; } = true;
    private int page;
    private float pageTime, skipTime, revealTime = -1;
    private GUIStyle lineStyle, hintStyle;

    float PageLength(int p) => (Pages[p].Length - 1) * LineGap + FadeIn + Hold;
    public void Skip()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        revealTime = 0;
        if (combat != null) combat.player.CanMove = combat.PlayerHealth > 0;
    }
    void Update()
    {
        if (!IsPlaying)
        {
            if (revealTime >= 0) revealTime += Time.deltaTime;
            if (revealTime > Reveal) enabled = false;
            return;
        }
        if (combat != null) combat.player.CanMove = false;
        skipTime = Input.GetKey(KeyCode.Space) ? skipTime + Time.deltaTime : 0;
        if (skipTime >= SkipHold) { Skip(); return; }
        pageTime += Time.deltaTime;
        if (pageTime >= PageLength(page) + FadeOut + PageGap)
        {
            pageTime = 0;
            if (++page >= Pages.Length) Skip();
        }
    }
    void OnGUI()
    {
        if (!IsPlaying && revealTime < 0) return;
        GUI.depth = -100;
        PrototypeKoreanUI.Apply();
        var screen = new Rect(0, 0, Screen.width, Screen.height);
        float cover = IsPlaying ? 1 : 1 - Mathf.Clamp01(revealTime / Reveal);
        GUI.color = new Color(0, 0, 0, cover);
        GUI.DrawTexture(screen, Texture2D.whiteTexture);
        if (!IsPlaying) { GUI.color = Color.white; return; }
        if (lineStyle == null)
        {
            lineStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerRight, fontSize = 15 };
        }
        float size = Mathf.Clamp(Screen.height / 24f, 18, 40), lineHeight = size * 2.1f;
        var lines = Pages[page];
        float top = Screen.height / 2f - lines.Length * lineHeight / 2f;
        float pageFade = 1 - Mathf.Clamp01((pageTime - PageLength(page)) / FadeOut);
        for (int i = 0; i < lines.Length; i++)
        {
            float alpha = Mathf.Clamp01((pageTime - i * LineGap) / FadeIn) * pageFade;
            if (alpha <= 0) continue;
            // The naming line lands harder than the rest.
            bool emphasis = page == 3 && i == 1;
            lineStyle.fontSize = Mathf.RoundToInt(emphasis ? size * 1.35f : size);
            lineStyle.normal.textColor = emphasis ? new Color(.95f, .85f, .6f, alpha) : new Color(.92f, .9f, .85f, alpha);
            GUI.color = Color.white;
            GUI.Label(new Rect(40, top + i * lineHeight, Screen.width - 80, lineHeight), lines[i], lineStyle);
        }
        GUI.color = Color.white;
        hintStyle.normal.textColor = new Color(.6f, .6f, .6f, .8f);
        var hint = new Rect(Screen.width - 340, Screen.height - 50, 310, 24);
        GUI.Label(hint, "스페이스바를 길게 눌러 건너뛰기", hintStyle);
        if (skipTime > 0)
        {
            GUI.color = new Color(.95f, .85f, .6f, .9f);
            GUI.DrawTexture(new Rect(hint.xMax - 200 * skipTime / SkipHold, hint.yMax + 6, 200 * skipTime / SkipHold, 3), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
