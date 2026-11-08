using UnityEngine;

// Shared look for the game's windows (the inventory, the peddler's pack): solid dark panels with a gold edge, so
// the guide boxes behind never show through, and text drawn at whole font sizes over a one-pixel shadow.
public static class JinaUI
{
    public static readonly Color Back = new Color(.1f, .09f, .12f), Edge = new Color(.78f, .62f, .32f), Title = new Color(.32f, .21f, .12f);
    public static readonly Color SlotBack = new Color(.18f, .17f, .21f), SlotEdge = new Color(.36f, .34f, .42f), Hover = new Color(.26f, .25f, .31f);
    public static readonly Color Gold = new Color(1f, .86f, .5f), Ink = new Color(.95f, .93f, .88f), Effect = new Color(.62f, .9f, .62f), Note = new Color(1f, .7f, .45f);
    public static readonly Color Dim = new Color(.55f, .53f, .5f);

    static GUIStyle text;
    public static void Fill(Rect r, Color c)
    {
        var saved = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = saved;
    }
    public static void Frame(Rect r, Color fill, Color edge, float thick)
    {
        Fill(r, edge);
        Fill(new Rect(r.x + thick, r.y + thick, r.width - thick * 2, r.height - thick * 2), fill);
    }
    // A window with a title bar; returns the area under the bar.
    public static Rect Window(Rect r, string title, string right = null)
    {
        Frame(r, Back, Edge, 2);
        Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, 30), Title);
        Text(new Rect(r.x + 12, r.y + 4, r.width - 24, 26), title, 18, Gold, TextAnchor.MiddleLeft, true);
        if (right != null) Text(new Rect(r.x + 12, r.y + 4, r.width - 24, 26), right, 14, Ink, TextAnchor.MiddleRight);
        return new Rect(r.x + 14, r.y + 46, r.width - 28, r.height - 60);
    }
    public static void Text(Rect r, string s, int size, Color colour, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false)
    {
        if (string.IsNullOrEmpty(s)) return;
        if (text == null) text = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = false, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0) };
        text.font = PrototypeKoreanUI.Font; text.fontSize = size; text.alignment = anchor;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.normal.textColor = new Color(0, 0, 0, .85f * colour.a);
        GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), s, text);
        text.normal.textColor = colour;
        GUI.Label(r, s, text);
    }
}
