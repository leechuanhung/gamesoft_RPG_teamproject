using System.Collections.Generic;
using UnityEngine;

// The look of every window and notice in the game, "hanji and wood": fibred cream paper in a grained wooden frame with
// brass corners, dark lacquer panels for the always-on HUD, and text that is typed out as people speak.
// The UI is laid out on a screen 720 units tall whatever the window's size (W x H): PrototypeKoreanUI.Apply sets the
// matrix, and text is drawn at its real pixel size so it stays crisp. Text is measured (Height) so a window can grow to
// fit it instead of cutting it off. Toasts stack downward and only one Prompt shows, so notices from different scripts
// never land on top of each other.
public static class JinaUI
{
    public static readonly Color Ink = new Color(.04f, .035f, .04f), Wood = new Color(.33f, .21f, .12f), WoodLight = new Color(.5f, .34f, .2f);
    public static readonly Color Paper = new Color(.96f, .92f, .81f), PaperDark = new Color(.87f, .8f, .65f), PaperLight = new Color(.99f, .97f, .9f);
    public static readonly Color SlotEdge = new Color(.56f, .43f, .29f), Night = new Color(.1f, .085f, .08f, .95f), Gone = new Color(.6f, .55f, .47f);
    // Text on paper.
    public static readonly Color TextDark = new Color(.16f, .11f, .08f), TextDim = new Color(.42f, .34f, .26f), Red = new Color(.66f, .16f, .13f), Green = new Color(.08f, .4f, .18f);
    // Text on wood or night.
    public static readonly Color Bright = new Color(.97f, .94f, .86f), Gold = new Color(1f, .86f, .52f), Faint = new Color(.74f, .69f, .6f);

    // One layout unit in screen pixels, and the layout's size. Small windows stop shrinking before text is unreadable.
    public static float Scale => Mathf.Max(.75f, Screen.height / 720f);
    public static float W => Screen.width / Scale;
    public static float H => Screen.height / Scale;
    public static void Begin() { GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1)); }
    // Where a point in the world falls in layout units (top-left origin); z < 0 is behind the camera.
    public static Vector3 FromWorld(Camera cam, Vector3 world)
    {
        var p = cam.WorldToScreenPoint(world);
        return new Vector3(p.x / Scale, (Screen.height - p.y) / Scale, p.z);
    }

    static GUIStyle text;
    static GUIStyle Style(int size, TextAnchor anchor, bool bold)
    {
        if (text == null) text = new GUIStyle { wordWrap = false, richText = false, clipping = TextClipping.Overflow };
        text.font = PrototypeKoreanUI.Font; text.fontSize = size; text.alignment = anchor;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        return text;
    }
    static int Px(int size) => Mathf.Max(8, Mathf.RoundToInt(size * Scale));
    // Line breaks are worked out here, not left to IMGUI: its own wrapping can break a line that just fits differently
    // from one draw to the next, which split text from its shadow. Words stay whole unless one is wider than the line.
    // Sizes and widths here are real pixels, so a line that fits on one screen fits the same way on all of them.
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
        string.IsNullOrEmpty(s) ? 0 : Mathf.Ceil(Style(Px(size), TextAnchor.UpperLeft, bold).CalcHeight(new GUIContent(Wrap(s, Px(size), Mathf.Round(width * Scale), bold)), 10000) / Scale);
    public static float Width(string s, int size, bool bold = false) =>
        string.IsNullOrEmpty(s) ? 0 : Mathf.Ceil(Style(Px(size), TextAnchor.UpperLeft, bold).CalcSize(new GUIContent(s)).x / Scale);

    public static void Fill(Rect r, Color c)
    {
        var saved = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = saved;
    }

    // The tiling grains the windows are made of (19_UI, Tools/ArtGen/make_ui.py), multiplied by the fill colour.
    static readonly Dictionary<string, Texture2D> grains = new Dictionary<string, Texture2D>();
    public static Texture2D Art(string name)
    {
        if (grains.TryGetValue(name, out var t) && t != null) return t;
        t = Resources.Load<Texture2D>("JinaV4/19_UI/" + name);
        if (t != null) { t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Point; }
        return grains[name] = t;
    }
    static void Tile(Rect r, string grain, Color c)
    {
        var t = Art(grain);
        if (t == null) { Fill(r, c); return; }
        var saved = GUI.color; GUI.color = c;
        // Two layout units to a texel: the dots of the grain match the art's.
        GUI.DrawTextureWithTexCoords(r, t, new Rect(r.x / (t.width * 2), r.y / (t.height * 2), r.width / (t.width * 2), r.height / (t.height * 2)));
        GUI.color = saved;
    }
    // An icon from 19_UI at its dots' size (each dot `dot` layout units).
    public static void Icon(string name, float x, float y, float dot = 1.5f, float alpha = 1)
    {
        var t = Art(name);
        if (t == null) return;
        var saved = GUI.color; GUI.color = new Color(1, 1, 1, alpha);
        GUI.DrawTexture(new Rect(x, y, t.width * dot, t.height * dot), t);
        GUI.color = saved;
    }

    // Ink outline with notched corners, a wooden band, then the fill: paper for light colours, lacquer for dark ones.
    public static void Box(Rect r, Color fill, Color edge, float band = 2)
    {
        Fill(new Rect(r.x, r.y + 2, r.width, r.height - 4), Ink);
        Fill(new Rect(r.x + 2, r.y, r.width - 4, r.height), Ink);
        if (band > 0) Tile(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), "grain_wood", edge);
        var inside = new Rect(r.x + 2 + band, r.y + 2 + band, r.width - 4 - band * 2, r.height - 4 - band * 2);
        Tile(inside, fill.grayscale > .45f ? "grain_paper" : "grain_night", fill);
        if (band >= 3)
        {
            // A soft shadow under the frame's top edge, as if the paper sat inside it, and brass on the corners.
            Fill(new Rect(inside.x, inside.y, inside.width, 2), new Color(0, 0, 0, .14f));
            Corners(r, 2);
        }
    }
    static void Corners(Rect r, float dot)
    {
        var t = Art("corner");
        if (t == null) return;
        float s = t.width * dot;
        GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, s, s), t, new Rect(0, 0, 1, 1));
        GUI.DrawTextureWithTexCoords(new Rect(r.xMax - s, r.y, s, s), t, new Rect(1, 0, -1, 1));
        GUI.DrawTextureWithTexCoords(new Rect(r.x, r.yMax - s, s, s), t, new Rect(0, 1, 1, -1));
        GUI.DrawTextureWithTexCoords(new Rect(r.xMax - s, r.yMax - s, s, s), t, new Rect(1, 1, -1, -1));
    }
    public static void HudPanel(Rect r)
    {
        Box(r, Night, WoodLight, 1);
        Corners(r, 1);
    }

    public const float TitleHeight = 36, WindowPad = 16;
    // A paper window with a wooden title bar; returns the area under the bar, inset by WindowPad.
    public static Rect Window(Rect r, string title, string right = null)
    {
        Fill(new Rect(r.x + 5, r.y + 6, r.width, r.height), new Color(0, 0, 0, .28f));
        Box(r, Paper, Wood, 3);
        Tile(new Rect(r.x + 2, r.y + 2, r.width - 4, TitleHeight - 2), "grain_wood", Wood);
        Fill(new Rect(r.x + 2, r.y + TitleHeight - 2, r.width - 4, 2), new Color(0, 0, 0, .35f));
        Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, 1), new Color(1, .86f, .6f, .22f));
        Corners(r, 2);
        Text(new Rect(r.x + WindowPad, r.y + 3, r.width - WindowPad * 2, TitleHeight - 6), title, 18, Gold, TextAnchor.MiddleLeft, true);
        if (right != null) Text(new Rect(r.x + WindowPad, r.y + 3, r.width - WindowPad * 2, TitleHeight - 6), right, 13, Bright, TextAnchor.MiddleRight);
        return new Rect(r.x + WindowPad, r.y + TitleHeight + 12, r.width - WindowPad * 2, r.height - TitleHeight - 12 - WindowPad);
    }
    // Bright text gets a shadow so it stays crisp over anything; dark text on paper needs none. `visible` draws only
    // that many characters, with the lines already broken as for the whole text, so words do not jump as they appear.
    public static void Text(Rect r, string s, int size, Color colour, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false, int visible = -1)
    {
        if (string.IsNullOrEmpty(s)) return;
        float k = Scale; int px = Px(size);
        // Whole pixels: fractional positions can wrap a line differently from its shadow or from the measured height.
        r = new Rect(Mathf.Round(r.x * k), Mathf.Round(r.y * k), Mathf.Round(r.width * k), Mathf.Round(r.height * k));
        s = Wrap(s, px, Mathf.Round(r.width), bold);
        if (visible >= 0 && visible < s.Length) s = Cut(s, visible);
        var style = Style(px, anchor, bold);
        var matrix = GUI.matrix; GUI.matrix = Matrix4x4.identity;
        if (colour.grayscale > .5f)
        {
            float shadow = Mathf.Max(1, Mathf.Round(k));
            style.normal.textColor = new Color(0, 0, 0, .9f * colour.a);
            GUI.Label(new Rect(r.x + shadow, r.y + shadow, r.width, r.height), s, style);
        }
        style.normal.textColor = colour;
        GUI.Label(r, s, style);
        GUI.matrix = matrix;
    }
    // The first `count` characters of wrapped text, not counting its line breaks.
    static string Cut(string s, int count)
    {
        int i = 0, seen = 0;
        while (i < s.Length && seen < count) { if (s[i] != '\n') seen++; i++; }
        return s.Substring(0, i);
    }

    // Spoken words appear a few at a time. Visible says how many characters of `s` show `since` seconds after it began.
    public const float TypeSpeed = 34;
    public static int Visible(string s, float since) => string.IsNullOrEmpty(s) || since < 0 ? 0 : Mathf.Min(s.Length, Mathf.CeilToInt(since * TypeSpeed));
    // The same when nobody keeps the start time: a line starts over if it has not been drawn for a moment.
    static readonly Dictionary<string, (float first, float last)> typing = new Dictionary<string, (float, float)>();
    public static int VisibleAuto(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        float now = Time.unscaledTime;
        if (!typing.TryGetValue(s, out var seen) || now - seen.last > .4f) seen = (now, now);
        typing[s] = (seen.first, now);
        if (typing.Count > 64) typing.Clear();
        return Visible(s, now - seen.first);
    }

    // For E on a line still being typed: true if it was, and now it shows whole.
    public static bool FinishTyping(string s)
    {
        if (string.IsNullOrEmpty(s) || !typing.TryGetValue(s, out var seen)) return false;
        if (Visible(s, Time.unscaledTime - seen.first) >= s.Length) return false;
        typing[s] = (seen.first - 1000, seen.last);
        return true;
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
        string whole = speaker + ":  " + s;
        Text(new Rect(r.x + 12, r.y + 6, r.width - 24, r.height - 12), whole, 16, TextDark, TextAnchor.UpperLeft, false, VisibleAuto(whole));
    }
    // A gauge: dark trough, a pale stretch for what was just lost (`lag`, 0 for none), the colour with a lit top edge.
    public static void Bar(Rect r, float full, Color colour, string label, float lag = 0)
    {
        Box(r, new Color(.13f, .1f, .1f), Ink, 0);
        var inner = new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4);
        full = Mathf.Clamp01(full);
        if (lag > full) Fill(new Rect(inner.x, inner.y, inner.width * Mathf.Clamp01(lag), inner.height), new Color(.95f, .86f, .72f));
        var filled = new Rect(inner.x, inner.y, inner.width * full, inner.height);
        Fill(filled, colour);
        Fill(new Rect(filled.x, filled.y, filled.width, Mathf.Max(1, inner.height * .25f)), Color.Lerp(colour, Color.white, .34f));
        Fill(new Rect(filled.x, filled.yMax - Mathf.Max(1, inner.height * .2f), filled.width, Mathf.Max(1, inner.height * .2f)), Color.Lerp(colour, Color.black, .28f));
        Text(r, label, 13, Bright, TextAnchor.MiddleCenter, true);
    }

    // Where the toast column starts (the HUD moves it below the boss's bar), and how wide it may be between the panels.
    public static float ToastTop = 14;
    public static float CenterWidth => Mathf.Clamp(W - 640, 240, 560);
    static int toastFrame = -1, promptFrame = -1;
    static float toastY;
    // A short notice in the column at the top centre; call it every OnGUI while it should show.
    public static void Toast(string s)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (toastFrame != Time.frameCount) { toastFrame = Time.frameCount; toastY = ToastTop; }
        float w = CenterWidth, h = Height(s, 15, w - 28) + 18;
        var r = new Rect((W - w) / 2, toastY, w, h);
        HudPanel(r);
        Text(new Rect(r.x + 14, r.y + 9, w - 28, h - 18), s, 15, Bright, TextAnchor.UpperCenter);
        toastY += h + 6;
    }
    // What the player can do right here, bottom centre; if two scripts ask in one frame the first one wins.
    // "E: 말 걸기" draws its key as a little keycap in front of the words.
    public const float PromptBottom = 44;
    public static void Prompt(string s)
    {
        if (Event.current.type != EventType.Repaint || promptFrame == Time.frameCount) return;
        promptFrame = Time.frameCount;
        string key = null, words = s;
        int colon = s.IndexOf(": ");
        if (colon > 0 && colon <= 2) { key = s.Substring(0, colon); words = s.Substring(colon + 2); }
        float keyWidth = key == null ? 0 : Mathf.Max(24, Width(key, 14, true) + 12) + 10;
        float w = Mathf.Min(Width(words, 15) + keyWidth + 36, W - 40), h = Height(words, 15, w - 28 - keyWidth) + 16;
        h = Mathf.Max(h, 36);
        var r = new Rect((W - w) / 2, H - PromptBottom - h, w, h);
        Box(r, Wood, Gold, 1);
        float x = r.x + 14;
        if (key != null)
        {
            var cap = new Rect(x, r.y + (h - 24) / 2, keyWidth - 10, 24);
            Box(cap, PaperLight, Ink, 0);
            Fill(new Rect(cap.x + 2, cap.yMax - 5, cap.width - 4, 3), new Color(.62f, .55f, .44f));
            Text(new Rect(cap.x, cap.y - 1, cap.width, cap.height), key, 14, TextDark, TextAnchor.MiddleCenter, true);
            x += keyWidth;
        }
        Text(new Rect(x, r.y, r.xMax - x - 14, h), words, 15, Bright, TextAnchor.MiddleLeft, true);
    }
}
