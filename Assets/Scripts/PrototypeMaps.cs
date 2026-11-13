using UnityEngine;

// The world is one long strip of ground, but it plays as separate maps. The camera never shows past the edges of
// the map the seonbi is in, and walking off an edge fades through black into the next map while he keeps walking:
// no key to press. Monsters still cross on their own (the boss can chase him into the village).
public class PrototypeMaps : MonoBehaviour
{
    public struct Map
    {
        public string name, line;
        public float left, right;
        public Map(string name, string line, float left, float right) { this.name = name; this.line = line; this.left = left; this.right = right; }
    }
    // Left to right, sharing edges. The outer walls stand just beyond the first and last.
    public static readonly Map[] All =
    {
        new Map("옛 비석 터", "망한 나라의 비석이 홀로 남은 곳", -70, PrototypeWorld.GateX),
        new Map("망령의 들판", "무덤 없는 군졸들의 넋이 떠도는 곳", PrototypeWorld.GateX, PrototypeWorld.VillageLeft),
        new Map("주막 마을", "산 넘는 길손이 쉬어 가는 마을", PrototypeWorld.VillageLeft, PrototypeWorld.ForestStart),
        new Map("도깨비 숲", "산의 주인이 미혹되어 길을 막는 숲", PrototypeWorld.ForestStart, PrototypeWorld.FerryStart),
        new Map("나루터", "강 건너 한양으로 가는 길목", PrototypeWorld.FerryStart, PrototypeWorld.FerryEnd),
        new Map("콩밭 마을", "효녀 콩쥐와 못된 팥쥐가 산다는 부잣집 마을", PrototypeWorld.KongStart, PrototypeWorld.KongEnd),
        new Map("고개와 서낭당", "길손의 걱정이 돌무더기로 쌓인 고개", PrototypeWorld.PassStart, PrototypeWorld.PassEnd),
        new Map("뒷산", "소리를 질러도 아무도 듣지 못한다는 고개 너머 뒷산", PrototypeWorld.HillStart, PrototypeWorld.HillEnd),
    };
    // How far past an edge he walks before the map changes; the black closing and opening; the name card.
    public const float Margin = .2f, FadeOut = .22f, FadeIn = .3f, CardLength = 2.4f;

    public PrototypeCombat combat;
    public int Current { get; private set; } = Index(PrototypeWorld.Spawn.x);
    public float LeftAt { get; private set; } = -1;
    public float EnteredAt { get; private set; } = -1;
    public bool Travelling => LeftAt >= 0;
    public Map Here => All[Current];

    public static int Index(float x)
    {
        for (int i = 0; i < All.Length; i++) if (x < All[i].right) return i;
        return All.Length - 1;
    }

    public void ResetMaps() { Current = Index(PrototypeWorld.Spawn.x); LeftAt = EnteredAt = -1; }
    // Put straight onto the map where he now stands, without a change of maps (the back hill checkpoint).
    public void Jump() { LeftAt = EnteredAt = -1; if (combat != null) Current = Index(combat.player.transform.position.x); }

    // Put somewhere else without walking there (R, a test): no black, the map is simply where he is. A walk off
    // the edge is seen first by Update (before the camera's LateUpdate), so it still fades.
    public void Sync()
    {
        if (combat == null || combat.player == null || Travelling) return;
        float x = combat.player.transform.position.x;
        if (x > Here.right + Margin || x < Here.left - Margin) Current = Index(x);
    }

    // Whether the map holding `x` is the one fully on screen (not mid-change).
    public bool Shows(float x) { Sync(); return !Travelling && Current == Index(x); }

    // Where the camera may sit to show only this map; a map narrower than the view is shown centred.
    public float ClampView(float x, float halfWidth)
    {
        Sync();
        float lo = Here.left + halfWidth, hi = Here.right - halfWidth;
        return lo > hi ? (Here.left + Here.right) / 2 : Mathf.Clamp(x, lo, hi);
    }

    void Update()
    {
        if (combat == null || combat.player == null) return;
        float x = combat.player.transform.position.x;
        if (!Travelling)
        {
            if (x > Here.right + Margin || x < Here.left - Margin) LeftAt = Time.time;
            return;
        }
        // Behind the black, the camera jumps to the map he is now in.
        if (Time.time - LeftAt >= FadeOut && EnteredAt < LeftAt)
        {
            Current = Index(x);
            EnteredAt = Time.time;
        }
        if (EnteredAt >= LeftAt && Time.time - EnteredAt >= FadeIn) LeftAt = -1;
    }

    // How dark the screen is: closing as he leaves, opening once the new map is in view.
    public float Cover => !Travelling ? 0 : EnteredAt < LeftAt ? Mathf.Clamp01((Time.time - LeftAt) / FadeOut)
        : 1 - Mathf.Clamp01((Time.time - EnteredAt) / FadeIn);

    void OnGUI()
    {
        GUI.depth = -50;
        PrototypeKoreanUI.Apply();
        if (Cover > 0)
        {
            JinaUI.Fill(new Rect(0, 0, JinaUI.W, JinaUI.H), new Color(0, 0, 0, Cover));
        }
        // The map's name, after walking into it (not at the start or after R).
        float t = EnteredAt < 0 ? -1 : Time.time - EnteredAt;
        if (t < 0 || t > CardLength || combat == null || combat.Paused) return;
        float fade = Mathf.Clamp01(t / .4f) * Mathf.Clamp01((CardLength - t) / .6f);
        float w = JinaUI.CenterWidth, lineHeight = JinaUI.Height(Here.line, 15, w - 24);
        var card = new Rect((JinaUI.W - w) / 2, JinaUI.H * .3f, w, 56 + lineHeight);
        JinaUI.Fill(card, new Color(.1f, .085f, .08f, .78f * fade));
        JinaUI.Text(new Rect(card.x, card.y + 8, w, 36), "― " + Here.name + " ―", 26, new Color(1f, .86f, .52f, fade), TextAnchor.MiddleCenter, true);
        JinaUI.Text(new Rect(card.x + 12, card.y + 46, w - 24, lineHeight), Here.line, 15, new Color(.97f, .94f, .86f, fade), TextAnchor.UpperCenter);
    }
}
