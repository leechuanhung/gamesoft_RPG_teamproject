using UnityEngine;

// Pixel-styled speech bubble pointing at a speaker in the world, with a name tag.
public static class SpeechBubble
{
    const float Border = 3, Pad = 16, Step = 4;
    static readonly Color Ink = new Color(.04f, .035f, .04f), Paper = new Color(.99f, .97f, .9f), Tag = new Color(.25f, .17f, .13f);
    static GUIStyle text, tag, footer;

    static void Fill(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); }

    // Whether a speaker is within the camera's width; bubbles clamp to the screen, so off-screen speakers stay quiet.
    public static bool OnScreen(Vector3 head)
    {
        var cam = Camera.main;
        if (cam == null) return false;
        var v = cam.WorldToViewportPoint(head);
        return v.x > 0 && v.x < 1;
    }

    public static void Draw(Vector3 head, string speaker, string message, string hint = null)
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 point = cam.WorldToScreenPoint(head);
        if (point.z < 0) return;
        PrototypeKoreanUI.Apply();
        if (text == null)
        {
            text = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 17 };
            text.normal.textColor = new Color(.12f, .09f, .08f);
            tag = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            tag.normal.textColor = Paper;
            footer = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.LowerRight };
            footer.normal.textColor = new Color(.45f, .4f, .36f);
        }
        float width = Mathf.Min(500, Screen.width - 40);
        float body = text.CalcHeight(new GUIContent(message), width - Pad * 2);
        float height = body + Pad * 2 + (hint == null ? 0 : 20);
        float tipX = point.x, tipY = Screen.height - point.y, tail = Step * 5;
        var box = new Rect(Mathf.Clamp(tipX - width / 2, 20, Screen.width - width - 20), Mathf.Max(30, tipY - tail - height), width, height);
        tipX = Mathf.Clamp(tipX, box.x + 24, box.xMax - 24);

        Fill(new Rect(box.x + 6, box.y + 6, box.width, box.height), new Color(0, 0, 0, .3f));
        // Two overlapping border rects leave notched, pixel-style corners.
        Fill(new Rect(box.x - Border, box.y, box.width + Border * 2, box.height), Ink);
        Fill(new Rect(box.x, box.y - Border, box.width, box.height + Border * 2), Ink);
        Fill(box, Paper);
        for (int i = 0; i < 5; i++)
        {
            float w = 22 - i * 4, y = box.yMax + i * Step;
            Fill(new Rect(tipX - w / 2 - Border, y, w + Border * 2, Step), Ink);
            Fill(new Rect(tipX - w / 2, y, w, Step), Paper);
        }
        Fill(new Rect(tipX - Border / 2 - 1, box.yMax + tail, Border + 2, Step), Ink);

        float tagWidth = tag.CalcSize(new GUIContent(speaker)).x + 24;
        var nameTag = new Rect(box.x + 14, box.y - 15, tagWidth, 26);
        Fill(new Rect(nameTag.x - Border, nameTag.y - Border, nameTag.width + Border * 2, nameTag.height + Border * 2), Ink);
        Fill(nameTag, Tag);
        GUI.color = Color.white;
        GUI.Label(nameTag, speaker, tag);
        GUI.Label(new Rect(box.x + Pad, box.y + Pad + 4, width - Pad * 2, body), message, text);
        if (hint != null) GUI.Label(new Rect(box.x + Pad, box.yMax - 24, width - Pad * 2, 20), hint, footer);
    }
}
