using UnityEngine;

// Pixel-styled speech bubble pointing at a speaker in the world, with a name tag. The words are typed out as they are
// said; the bubble is measured for the whole line, so it does not grow while they appear.
public static class SpeechBubble
{
    const float Border = 3, Pad = 16, Step = 4;
    static readonly Color Ink = new Color(.04f, .035f, .04f), Paper = new Color(.99f, .97f, .9f);

    // Whether a speaker is within the camera's width; bubbles clamp to the screen, so off-screen speakers stay quiet.
    public static bool OnScreen(Vector3 head)
    {
        var cam = Camera.main;
        if (cam == null) return false;
        var v = cam.WorldToViewportPoint(head);
        return v.x > 0 && v.x < 1;
    }

    // `since`: seconds since this line began, or negative to have it worked out from when the line first showed.
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
        // Two overlapping border rects leave notched, pixel-style corners.
        JinaUI.Fill(new Rect(box.x - Border, box.y, box.width + Border * 2, box.height), Ink);
        JinaUI.Fill(new Rect(box.x, box.y - Border, box.width, box.height + Border * 2), Ink);
        JinaUI.Fill(box, Paper);
        // A faint fold of shade along the bottom edge.
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
