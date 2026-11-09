using System.Collections.Generic;
using UnityEngine;

// The look of every window and notice in the game, "hanji and wood": cream paper in a wooden frame with the speech
// bubbles' notched pixel corners, and dark night panels for the always-on HUD. Text is measured (Height) so a window
// can grow to fit it instead of cutting it off. Toasts stack downward and only one Prompt shows, so notices from
// different scripts never land on top of each other.
public static class JinaUI
{
    public static readonly Color Ink = new Color(.04f, .035f, .04f), Wood = new Color(.33f, .21f, .12f), WoodLight = new Color(.5f, .34f, .2f);
    public static readonly Color Paper = new Color(.96f, .92f, .81f), PaperDark = new Color(.87f, .8f, .65f), PaperLight = new Color(.99f, .97f, .9f);
    public static readonly Color SlotEdge = new Color(.56f, .43f, .29f), Night = new Color(.1f, .085f, .08f, .95f), Gone = new Color(.6f, .55f, .47f);
    // Text on paper.
    public static readonly Color TextDark = new Color(.16f, .11f, .08f), TextDim = new Color(.42f, .34f, .26f), Red = new Color(.66f, .16f, .13f), Green = new Color(.08f, .4f, .18f);
    // Text on wood or night.
    public static readonly Color Bright = new Color(.97f, .94f, .86f), Gold = new Color(1f, .86f, .52f), Faint = new Color(.74f, .69f, .6f);

    static GUIStyle text;
    static GUIStyle Style(int size, TextAnchor anchor, bool bold)
    {
        if (text == null) text = new GUIStyle { wordWrap = false, richText = false, clipping = TextClipping.Overflow };
        text.font = PrototypeKoreanUI.Font; text.fontSize = size; text.alignment = anchor;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        return text;
    }
    // Line breaks are worked out here, not left to IMGUI: its own wrapping can break a line that just fits differently
    // from one draw to the next, which split text from its shadow. Words stay whole unless one is wider than the line.
    static readonly Dictionary<(string, int, bool, int), string> wrapped = new Dictionary<(string, int, bool, int), string>();
    static string Wrap(string s, int size, float width, bool bold)
    {
        var key = (s, size, bold, Mathf.RoundToInt(width));
        if (wrapped.TryGetValue(key, out string done)) return done;
        var style = Style(size, TextAnchor.UpperLeft, bold);
        var result = new System.Text.StringBuilder();
        var parts = s.Split('\n');
        for (int p = 0; p < parts.Length; p++)
        {
            if (p > 0) result.Append('\n');
            string line = "";
            foreach (var word in parts[p].Split(' '))
            {
                string attempt = line == "" ? word : line + " " + word;
                if (style.CalcSize(new GUIContent(attempt)).x <= width) { line = attempt; continue; }
                if (line != "") { result.Append(line).Append('\n'); line = ""; }
                line = word;
                while (line.Length > 1 && style.CalcSize(new GUIContent(line)).x > width)
                {
                    int fit = 1;
                    while (fit < line.Length && style.CalcSize(new GUIContent(line.Substring(0, fit + 1))).x <= width) fit++;
                    result.Append(line.Substring(0, fit)).Append('\n');
                    line = line.Substring(fit);
                }
            }
            result.Append(line);
        }
        return wrapped[key] = result.ToString();
    }
    // How tall this text is when wrapped to `width`.
    public static float Height(string s, int size, float width, bool bold = false) =>
        string.IsNullOrEmpty(s) ? 0 : Mathf.Ceil(Style(size, TextAnchor.UpperLeft, bold).CalcHeight(new GUIContent(Wrap(s, size, Mathf.Round(width), bold)), 10000));
    public static float Width(string s, int size, bool bold = false) =>
        string.IsNullOrEmpty(s) ? 0 : Mathf.Ceil(Style(size, TextAnchor.UpperLeft, bold).CalcSize(new GUIContent(s)).x);

    public static void Fill(Rect r, Color c)
    {
        var saved = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = saved;
    }
    // Ink outline with notched corners, an edge band, then the fill.
    public static void Box(Rect r, Color fill, Color edge, float band = 2)
    {
        Fill(new Rect(r.x, r.y + 2, r.width, r.height - 4), Ink);
        Fill(new Rect(r.x + 2, r.y, r.width - 4, r.height), Ink);
        Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), edge);
        Fill(new Rect(r.x + 2 + band, r.y + 2 + band, r.width - 4 - band * 2, r.height - 4 - band * 2), fill);
    }
    public static void HudPanel(Rect r) => Box(r, Night, WoodLight, 1);

    public const float TitleHeight = 36, WindowPad = 16;
    // A paper window with a wooden title bar; returns the area under the bar, inset by WindowPad.
    public static Rect Window(Rect r, string title, string right = null)
    {
        Box(r, Paper, Wood, 3);
        Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, TitleHeight - 2), Wood);
        Text(new Rect(r.x + WindowPad, r.y + 3, r.width - WindowPad * 2, TitleHeight - 6), title, 18, Gold, TextAnchor.MiddleLeft, true);
        if (right != null) Text(new Rect(r.x + WindowPad, r.y + 3, r.width - WindowPad * 2, TitleHeight - 6), right, 13, Bright, TextAnchor.MiddleRight);
        return new Rect(r.x + WindowPad, r.y + TitleHeight + 12, r.width - WindowPad * 2, r.height - TitleHeight - 12 - WindowPad);
    }
    // Bright text gets a one-pixel shadow so it stays crisp over anything; dark text on paper needs none.
    public static void Text(Rect r, string s, int size, Color colour, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false)
    {
        if (string.IsNullOrEmpty(s)) return;
        // Whole pixels: fractional positions can wrap a line differently from its shadow or from the measured height.
        r = new Rect(Mathf.Round(r.x), Mathf.Round(r.y), Mathf.Round(r.width), Mathf.Round(r.height));
        s = Wrap(s, size, r.width, bold);
        var style = Style(size, anchor, bold);
        if (colour.grayscale > .5f)
        {
            style.normal.textColor = new Color(0, 0, 0, .9f * colour.a);
            GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), s, style);
        }
        style.normal.textColor = colour;
        GUI.Label(r, s, style);
    }
    public static bool Button(Rect r, string label, bool enabled = true)
    {
        bool over = enabled && r.Contains(Event.current.mousePosition);
        Box(r, !enabled ? Gone : over ? WoodLight : Wood, over ? Gold : WoodLight, 1);
        Text(r, label, 15, enabled ? Bright : PaperDark, TextAnchor.MiddleCenter, true);
        return GUI.Button(r, "", GUIStyle.none) && enabled;
    }
    public static void Slot(Rect r, bool selected, bool over) => Box(r, over ? PaperLight : PaperDark, selected ? Red : SlotEdge, selected ? 2 : 1);

    // One thing for sale: icon, name, gold-red price, green effect; true when bought. It lights up under the mouse.
    public const float GoodHeight = 58;
    public static bool Good(Rect r, Sprite icon, string name, string price, string effect, bool open)
    {
        bool over = open && r.Contains(Event.current.mousePosition);
        Box(r, over ? PaperLight : open ? PaperDark : Gone, over ? Red : SlotEdge, over ? 2 : 1);
        float x = r.x + 12;
        if (icon != null)
        {
            var saved = GUI.color; if (!open) GUI.color = new Color(1, 1, 1, .45f);
            InventoryUI.DrawIcon(icon, new Rect(r.x + 9, r.y + 9, 40, 40));
            GUI.color = saved; x = r.x + 58;
        }
        float w = r.xMax - x - 12, priceWidth = Width(price, 16, true);
        Text(new Rect(x, r.y + 8, w - priceWidth - 6, 22), name, 16, open ? TextDark : TextDim, TextAnchor.UpperLeft, true);
        Text(new Rect(r.xMax - 12 - priceWidth, r.y + 8, priceWidth, 22), price, 16, Red, TextAnchor.UpperRight, true);
        Text(new Rect(x, r.y + 33, w, 20), effect, 13, open ? Green : TextDim);
        return GUI.Button(r, "", GUIStyle.none) && open;
    }
    // A shopkeeper's words inside the window, in a strip at least two lines tall so the window does not jump.
    public static float SayHeight(string s, float width) => Mathf.Max(Height(s, 16, width - 24), 44) + 12;
    public static void Say(Rect r, string speaker, string s)
    {
        Box(r, PaperLight, SlotEdge, 1);
        Text(new Rect(r.x + 12, r.y + 6, r.width - 24, r.height - 12), speaker + ":  " + s, 16, TextDark);
    }
    public static void Bar(Rect r, float full, Color colour, string label)
    {
        Box(r, new Color(.2f, .16f, .14f), Ink, 0);
        var inner = new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4);
        Fill(new Rect(inner.x, inner.y, inner.width * Mathf.Clamp01(full), inner.height), colour);
        Text(r, label, 13, Bright, TextAnchor.MiddleCenter, true);
    }

    // Where the toast column starts (the HUD moves it below the boss's bar), and how wide it may be between the panels.
    public static float ToastTop = 14;
    public static float CenterWidth => Mathf.Clamp(Screen.width - 640, 240, 560);
    static int toastFrame = -1, promptFrame = -1;
    static float toastY;
    // A short notice in the column at the top centre; call it every OnGUI while it should show.
    public static void Toast(string s)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (toastFrame != Time.frameCount) { toastFrame = Time.frameCount; toastY = ToastTop; }
        float w = CenterWidth, h = Height(s, 15, w - 28) + 18;
        var r = new Rect((Screen.width - w) / 2, toastY, w, h);
        HudPanel(r);
        Text(new Rect(r.x + 14, r.y + 9, w - 28, h - 18), s, 15, Bright, TextAnchor.UpperCenter);
        toastY += h + 6;
    }
    // What the player can do right here, bottom centre; if two scripts ask in one frame the first one wins.
    public const float PromptBottom = 44;
    public static void Prompt(string s)
    {
        if (Event.current.type != EventType.Repaint || promptFrame == Time.frameCount) return;
        promptFrame = Time.frameCount;
        float w = Mathf.Min(Width(s, 15) + 36, Screen.width - 40), h = Height(s, 15, w - 28) + 16;
        var r = new Rect((Screen.width - w) / 2, Screen.height - PromptBottom - h, w, h);
        Box(r, Wood, Gold, 1);
        Text(new Rect(r.x + 14, r.y + 8, w - 28, h - 16), s, 15, Bright, TextAnchor.UpperCenter, true);
    }
}
