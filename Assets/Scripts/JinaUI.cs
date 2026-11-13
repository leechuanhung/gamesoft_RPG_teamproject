using System.Collections.Generic;
using UnityEngine;

// ===== UI 공통 모양 (JinaUI) =====
// 게임 속 모든 창과 알림의 모양, "한지와 나무": 나뭇결 테두리와 놋쇠 모서리 안의 섬유질 한지,
// 상시 HUD는 어두운 옻칠 판, 글자는 말하듯 한 자씩 찍힌다.
// UI는 창 크기와 상관없이 높이 720 단위 화면(W x H)에 배치한다. PrototypeKoreanUI.Apply가 행렬을
// 맞추고, 글자는 실제 픽셀 크기로 그려 또렷하다. 글자 높이를 재서(Height) 창이 글자를 자르지 않고
// 늘어나게 한다. 알림(Toast)은 아래로 쌓이고 행동 안내(Prompt)는 하나만 보여서, 여러 스크립트의 알림이
// 서로 겹치지 않는다.
public static class JinaUI
{
    public static readonly Color Ink = new Color(.04f, .035f, .04f), Wood = new Color(.33f, .21f, .12f), WoodLight = new Color(.5f, .34f, .2f);
    public static readonly Color Paper = new Color(.96f, .92f, .81f), PaperDark = new Color(.87f, .8f, .65f), PaperLight = new Color(.99f, .97f, .9f);
    public static readonly Color SlotEdge = new Color(.56f, .43f, .29f), Night = new Color(.1f, .085f, .08f, .95f), Gone = new Color(.6f, .55f, .47f);
    // 한지 위 글자색.
    public static readonly Color TextDark = new Color(.16f, .11f, .08f), TextDim = new Color(.42f, .34f, .26f), Red = new Color(.66f, .16f, .13f), Green = new Color(.08f, .4f, .18f);
    // 나무나 밤 배경 위 글자색.
    public static readonly Color Bright = new Color(.97f, .94f, .86f), Gold = new Color(1f, .86f, .52f), Faint = new Color(.74f, .69f, .6f);

    // 레이아웃 1단위의 화면 픽셀 크기와 레이아웃 크기. 작은 창에서도 글자가 읽힐 만큼만 줄어든다.
    public static float Scale => Mathf.Max(.75f, Screen.height / 720f);
    public static float W => Screen.width / Scale;
    public static float H => Screen.height / Scale;
    public static void Begin() { GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1)); }
    // 월드의 한 점이 레이아웃 좌표(왼쪽 위 원점)로 어디인지. z < 0이면 카메라 뒤.
    public static Vector3 FromWorld(Camera cam, Vector3 world)
    {
        var p = cam.WorldToScreenPoint(world);
        return new Vector3(p.x / Scale, (Screen.height - p.y) / Scale, p.z);
    }

    static GUIStyle text;
    // 글자 크기·정렬·굵기에 맞는 GUI 스타일(캐시).
    static GUIStyle Style(int size, TextAnchor anchor, bool bold)
    {
        if (text == null) text = new GUIStyle { wordWrap = false, richText = false, clipping = TextClipping.Overflow };
        text.font = PrototypeKoreanUI.Font; text.fontSize = size; text.alignment = anchor;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        return text;
    }
    static int Px(int size) => Mathf.Max(8, Mathf.RoundToInt(size * Scale));
    // 줄바꿈은 IMGUI에 맡기지 않고 여기서 계산한다. IMGUI는 딱 맞는 줄을 그릴 때마다 다르게 끊어서
    // 글자와 그림자가 어긋났다. 한 단어가 줄보다 넓지 않으면 단어는 쪼개지 않는다.
    // 크기와 너비는 실제 픽셀이라, 한 화면에서 맞는 줄은 모든 화면에서 똑같이 맞는다.
    static readonly Dictionary<(string, int, bool, int), string> wrapped = new Dictionary<(string, int, bool, int), string>();
    // 글을 `width`에 맞게 줄바꿈한다.
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
    // `width`로 줄바꿈했을 때 글자 높이.
    public static float Height(string s, int size, float width, bool bold = false) =>
        string.IsNullOrEmpty(s) ? 0 : Mathf.Ceil(Style(Px(size), TextAnchor.UpperLeft, bold).CalcHeight(new GUIContent(Wrap(s, Px(size), Mathf.Round(width * Scale), bold)), 10000) / Scale);
    // 글의 너비(레이아웃 단위).
    public static float Width(string s, int size, bool bold = false) =>
        string.IsNullOrEmpty(s) ? 0 : Mathf.Ceil(Style(Px(size), TextAnchor.UpperLeft, bold).CalcSize(new GUIContent(s)).x / Scale);

    // 사각형을 한 색으로 채운다.
    public static void Fill(Rect r, Color c)
    {
        var saved = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = saved;
    }

    // 창을 이루는 반복 무늬(19_UI, Tools/ArtGen/make_ui.py)에 채움 색을 곱한다.
    static readonly Dictionary<string, Texture2D> grains = new Dictionary<string, Texture2D>();
    // 19_UI 그림을 불러온다(캐시).
    public static Texture2D Art(string name)
    {
        if (grains.TryGetValue(name, out var t) && t != null) return t;
        t = Resources.Load<Texture2D>("JinaV4/19_UI/" + name);
        if (t != null) { t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Point; }
        return grains[name] = t;
    }
    // 무늬 그림을 사각형에 반복해 깐다.
    static void Tile(Rect r, string grain, Color c)
    {
        var t = Art(grain);
        if (t == null) { Fill(r, c); return; }
        var saved = GUI.color; GUI.color = c;
        // 텍셀 하나가 레이아웃 2단위: 무늬의 도트가 그림 도트와 맞는다.
        GUI.DrawTextureWithTexCoords(r, t, new Rect(r.x / (t.width * 2), r.y / (t.height * 2), r.width / (t.width * 2), r.height / (t.height * 2)));
        GUI.color = saved;
    }
    // 19_UI의 아이콘을 도트 크기대로(도트 하나가 `dot` 단위).
    public static void Icon(string name, float x, float y, float dot = 1.5f, float alpha = 1)
    {
        var t = Art(name);
        if (t == null) return;
        var saved = GUI.color; GUI.color = new Color(1, 1, 1, alpha);
        GUI.DrawTexture(new Rect(x, y, t.width * dot, t.height * dot), t);
        GUI.color = saved;
    }

    // 모서리를 깎은 먹 외곽선, 나무 띠, 그리고 채움: 밝은 색이면 한지, 어두운 색이면 옻칠.
    public static void Box(Rect r, Color fill, Color edge, float band = 2)
    {
        Fill(new Rect(r.x, r.y + 2, r.width, r.height - 4), Ink);
        Fill(new Rect(r.x + 2, r.y, r.width - 4, r.height), Ink);
        if (band > 0) Tile(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), "grain_wood", edge);
        var inside = new Rect(r.x + 2 + band, r.y + 2 + band, r.width - 4 - band * 2, r.height - 4 - band * 2);
        Tile(inside, fill.grayscale > .45f ? "grain_paper" : "grain_night", fill);
        if (band >= 3)
        {
            // 테두리 윗변 아래 옅은 그림자(한지가 틀 안에 들어간 듯)와 모서리의 놋쇠.
            Fill(new Rect(inside.x, inside.y, inside.width, 2), new Color(0, 0, 0, .14f));
            Corners(r, 2);
        }
    }
    // 창 네 모서리에 놋쇠 장식을 그린다.
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
    // 상시 HUD용 어두운 옻칠 판.
    public static void HudPanel(Rect r)
    {
        Box(r, Night, WoodLight, 1);
        Corners(r, 1);
    }

    public const float TitleHeight = 36, WindowPad = 16;
    // 나무 제목줄이 있는 한지 창. 제목줄 아래 영역을 WindowPad만큼 안쪽으로 돌려준다.
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
    // 밝은 글자는 그림자를 넣어 어디서나 또렷하게, 한지 위 어두운 글자는 그림자 없이. `visible`은
    // 그만큼의 글자만 그린다. 줄은 전체 글 기준으로 이미 끊어 두어 글자가 나타나면서 단어가 튀지 않는다.
    public static void Text(Rect r, string s, int size, Color colour, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false, int visible = -1)
    {
        if (string.IsNullOrEmpty(s)) return;
        float k = Scale; int px = Px(size);
        // 정수 픽셀: 소수 위치면 그림자나 잰 높이와 줄바꿈이 달라질 수 있다.
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
    // 줄바꿈한 글의 앞 `count`글자(줄바꿈 문자는 세지 않음).
    static string Cut(string s, int count)
    {
        int i = 0, seen = 0;
        while (i < s.Length && seen < count) { if (s[i] != '\n') seen++; i++; }
        return s.Substring(0, i);
    }

    // 대사는 몇 글자씩 나타난다. Visible은 시작 `since`초 뒤 `s`가 몇 글자 보이는지 알려 준다.
    public const float TypeSpeed = 34;
    public static int Visible(string s, float since) => string.IsNullOrEmpty(s) || since < 0 ? 0 : Mathf.Min(s.Length, Mathf.CeilToInt(since * TypeSpeed));
    // 시작 시각을 따로 갖고 있지 않을 때: 잠깐 안 그려지면 처음부터 다시 찍는다.
    static readonly Dictionary<string, (float first, float last)> typing = new Dictionary<string, (float, float)>();
    // 시작 시각 없이 쓰는 타자 효과: 지금 몇 글자 보이는지.
    public static int VisibleAuto(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        float now = Time.unscaledTime;
        if (!typing.TryGetValue(s, out var seen) || now - seen.last > .4f) seen = (now, now);
        typing[s] = (seen.first, now);
        if (typing.Count > 64) typing.Clear();
        return Visible(s, now - seen.first);
    }

    // 타자 중인 줄에서 E를 누를 때: 타자 중이었으면 true를 돌려주고 전체를 보여 준다.
    public static bool FinishTyping(string s)
    {
        if (string.IsNullOrEmpty(s) || !typing.TryGetValue(s, out var seen)) return false;
        if (Visible(s, Time.unscaledTime - seen.first) >= s.Length) return false;
        typing[s] = (seen.first - 1000, seen.last);
        return true;
    }

    // 나무 버튼. 누르면 true.
    public static bool Button(Rect r, string label, bool enabled = true)
    {
        bool over = enabled && r.Contains(Event.current.mousePosition);
        Box(r, !enabled ? Gone : over ? WoodLight : Wood, over ? Gold : WoodLight, 1);
        Text(r, label, 15, enabled ? Bright : PaperDark, TextAnchor.MiddleCenter, true);
        return GUI.Button(r, "", GUIStyle.none) && enabled;
    }
    public static void Slot(Rect r, bool selected, bool over) => Box(r, over ? PaperLight : PaperDark, selected ? Red : SlotEdge, selected ? 2 : 1);

    // 판매 물건 한 칸: 아이콘, 이름, 금적색 가격, 초록 효과. 사면 true. 마우스를 올리면 밝아진다.
    public const float GoodHeight = 58;
    // 판매 물건 한 칸을 그린다. 사면 true.
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
    // 창 안의 상인 대사. 창이 들썩이지 않게 최소 두 줄 높이의 띠.
    public static float SayHeight(string s, float width) => Mathf.Max(Height(s, 16, width - 24), 44) + 12;
    // 창 안의 상인 대사 띠를 그린다.
    public static void Say(Rect r, string speaker, string s)
    {
        Box(r, PaperLight, SlotEdge, 1);
        string whole = speaker + ":  " + s;
        Text(new Rect(r.x + 12, r.y + 6, r.width - 24, r.height - 12), whole, 16, TextDark, TextAnchor.UpperLeft, false, VisibleAuto(whole));
    }
    // 게이지: 어두운 홈, 방금 잃은 만큼의 옅은 구간(`lag`, 0이면 없음), 윗변이 밝은 색 막대.
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

    // 알림 칸이 시작하는 높이(HUD가 보스 막대 아래로 옮김)와 양쪽 판 사이에서 쓸 수 있는 너비.
    public static float ToastTop = 14;
    public static float CenterWidth => Mathf.Clamp(W - 640, 240, 560);
    static int toastFrame = -1, promptFrame = -1;
    static float toastY;
    // 위 가운데 칸의 짧은 알림. 보이는 동안 OnGUI마다 부른다.
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
    // 지금 여기서 할 수 있는 행동, 아래 가운데. 한 프레임에 두 스크립트가 부르면 먼저 부른 쪽이 이긴다.
    // "E: 말 걸기"는 키를 작은 키캡으로 글 앞에 그린다.
    public const float PromptBottom = 44;
    // 화면 아래 가운데에 행동 안내를 띄운다.
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

    // 말하는 사람의 초상화(17_Portraits)가 왼쪽 끝 안에 들어가는 대화창. 이름표와 한 자씩 찍히는 대사.
    // `menacing`이면 글자가 붉고, `toLeft`면 화면 왼쪽에 둔다. 창 영역을 돌려준다.
    public static Rect TalkWindow(string name, string portrait, string line, string hint, bool menacing, float since = -1, bool toLeft = false)
    {
        const float h = 200;
        var sprite = JinaArt.Portrait(portrait);
        // 초상화는 창 높이 그대로 얇은 테두리 안에 들어간다.
        float ph = h - 24, pw = ph * sprite.rect.width / sprite.rect.height;
        float w = Mathf.Min(toLeft ? Mathf.Max(640, W * .7f) : 880, W - 30);
        var box = new Rect(toLeft ? 16 : (W - w) / 2, H - h - 14, w, h);
        Fill(new Rect(box.x + 5, box.y + 6, box.width, box.height), new Color(0, 0, 0, .28f));
        Box(box, Paper, Wood, 3);
        var face = new Rect(box.x + 12, box.y + 12, pw, ph);
        Box(new Rect(face.x - 2, face.y - 2, face.width + 4, face.height + 4), Wood, Wood, 2);
        InventoryUI.DrawIcon(sprite, face);
        float x = face.xMax + 20, tw = box.xMax - x - 18;
        var nameplate = new Rect(x, box.y + 14, Width(name, 18, true) + 28, 32);
        Box(nameplate, Wood, WoodLight, 1);
        Text(nameplate, name, 18, Gold, TextAnchor.MiddleCenter, true);
        Text(new Rect(x, box.y + 58, tw, h - 58 - 34), line, 19, menacing ? Red : TextDark, TextAnchor.UpperLeft, false, since >= 0 ? Visible(line, since) : VisibleAuto(line));
        Text(new Rect(x, box.yMax - 32, tw, 20), hint, 13, TextDim, TextAnchor.MiddleRight);
        return box;
    }
}

// ===== 말풍선 (SpeechBubble) =====
// 월드의 말하는 사람을 가리키는 도트풍 말풍선과 이름표. 대사는 말하듯 한 자씩 찍힌다.
// 말풍선 크기는 전체 대사로 재 두어 글자가 나타나는 동안 커지지 않는다.
public static class SpeechBubble
{
    const float Border = 3, Pad = 16, Step = 4;
    static readonly Color Ink = new Color(.04f, .035f, .04f), Paper = new Color(.99f, .97f, .9f);

    // 말하는 사람이 카메라 폭 안에 있는지. 말풍선은 화면 안으로 붙으므로 화면 밖 인물은 말하지 않는다.
    public static bool OnScreen(Vector3 head)
    {
        var cam = Camera.main;
        if (cam == null) return false;
        var v = cam.WorldToViewportPoint(head);
        return v.x > 0 && v.x < 1;
    }

    // `since`: 이 대사가 시작된 뒤 초. 음수면 처음 보인 때부터 계산한다.
    public static void Draw(Vector3 head, string speaker, string message, string hint = null, float since = -1)
    {
        var cam = Camera.main;
        if (cam == null) return;
        PrototypeKoreanUI.Apply();
        Vector3 point = JinaUI.FromWorld(cam, head);
        if (point.z < 0) return;
        float width = Mathf.Min(Mathf.Max(260, JinaUI.Width(message, 17) + Pad * 2 + 6), Mathf.Min(500, JinaUI.W - 40));
        float body = JinaUI.Height(message, 17, width - Pad * 2);
        float height = body + Pad * 2 + (hint == null ? 0 : 20);
        float tipX = point.x, tipY = point.y, tail = Step * 5;
        var box = new Rect(Mathf.Clamp(tipX - width / 2, 20, JinaUI.W - width - 20), Mathf.Max(30, tipY - tail - height), width, height);
        tipX = Mathf.Clamp(tipX, box.x + 24, box.xMax - 24);

        JinaUI.Fill(new Rect(box.x + 6, box.y + 6, box.width, box.height), new Color(0, 0, 0, .3f));
        // 두 테두리 사각형을 겹쳐 도트풍으로 깎인 모서리를 만든다.
        JinaUI.Fill(new Rect(box.x - Border, box.y, box.width + Border * 2, box.height), Ink);
        JinaUI.Fill(new Rect(box.x, box.y - Border, box.width, box.height + Border * 2), Ink);
        JinaUI.Fill(box, Paper);
        // 아래쪽 가장자리의 옅은 접힌 그림자.
        JinaUI.Fill(new Rect(box.x, box.yMax - 3, box.width, 3), new Color(.8f, .72f, .58f));
        for (int i = 0; i < 5; i++)
        {
            float w = 22 - i * 4, y = box.yMax + i * Step;
            JinaUI.Fill(new Rect(tipX - w / 2 - Border, y, w + Border * 2, Step), Ink);
            JinaUI.Fill(new Rect(tipX - w / 2, y, w, Step), Paper);
        }
        JinaUI.Fill(new Rect(tipX - Border / 2 - 1, box.yMax + tail, Border + 2, Step), Ink);

        float tagWidth = JinaUI.Width(speaker, 14, true) + 24;
        var nameTag = new Rect(box.x + 14, box.y - 15, tagWidth, 26);
        JinaUI.Box(new Rect(nameTag.x - Border, nameTag.y - Border, nameTag.width + Border * 2, nameTag.height + Border * 2), JinaUI.Wood, JinaUI.WoodLight, 1);
        JinaUI.Text(nameTag, speaker, 14, JinaUI.Gold, TextAnchor.MiddleCenter, true);
        int visible = since >= 0 ? JinaUI.Visible(message, since) : JinaUI.VisibleAuto(message);
        JinaUI.Text(new Rect(box.x + Pad, box.y + Pad + 4, width - Pad * 2, body), message, 17, new Color(.12f, .09f, .08f), TextAnchor.UpperLeft, false, visible);
        if (hint != null) JinaUI.Text(new Rect(box.x + Pad, box.yMax - 24, width - Pad * 2, 20), hint, 13, new Color(.45f, .4f, .36f), TextAnchor.LowerRight);
    }
}

// ===== UI 배율과 한글 글꼴 (PrototypeKoreanUI) =====
public static class PrototypeKoreanUI
{
    private static Font koreanFont;
    public static Font Font
    {
        get
        {
            if (koreanFont == null)
                koreanFont = UnityEngine.Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR" }, 16);
            return koreanFont;
        }
    }
    // OnGUI 처음에 부른다: 화면 배율과 한글 글꼴을 맞춘다.
    public static void Apply()
    {
        JinaUI.Begin();
        GUI.skin.font = Font;
        GUI.skin.label.fontSize = 15;
        GUI.skin.label.wordWrap = true;
        GUI.skin.box.fontSize = 15;
        GUI.skin.box.wordWrap = true;
    }
    // 보스 처리 결과(토벌·정화)를 한글로.
    public static string Outcome(EncounterOutcome outcome)
    {
        switch (outcome)
        {
            case EncounterOutcome.Slain: return "도깨비 토벌";
            case EncounterOutcome.Purified: return "도깨비 정화";
            default: return "해결되지 않은 사연";
        }
    }
}
