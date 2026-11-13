using UnityEngine;

// 세계는 길게 이어진 땅 하나지만 맵이 따로따로인 것처럼 플레이된다. 카메라는 선비가 있는 맵 가장자리
// 너머를 보여 주지 않고, 가장자리를 걸어 나가면 계속 걷는 동안 어둠을 지나 다음 맵으로 바뀐다:
// 누를 키가 없다. 몬스터는 알아서 경계를 넘는다(도깨비는 마을까지 쫓아올 수 있음).
public class PrototypeMaps : MonoBehaviour
{
    public struct Map
    {
        public string name, line;
        public float left, right;
        public Map(string name, string line, float left, float right) { this.name = name; this.line = line; this.left = left; this.right = right; }
    }
    // 왼쪽에서 오른쪽으로, 경계를 공유한다. 바깥 벽은 첫 맵과 마지막 맵 바로 밖에 있다.
    public static readonly Map[] All =
    {
        new Map("옛 비석 터", "망한 나라의 비석이 홀로 남은 곳", -70, WorldBuilder.GateX),
        new Map("망령의 들판", "무덤 없는 군졸들의 넋이 떠도는 곳", WorldBuilder.GateX, WorldBuilder.VillageLeft),
        new Map("주막 마을", "산 넘는 길손이 쉬어 가는 마을", WorldBuilder.VillageLeft, WorldBuilder.ForestStart),
        new Map("도깨비 숲", "산의 주인이 미혹되어 길을 막는 숲", WorldBuilder.ForestStart, WorldBuilder.FerryStart),
        new Map("나루터", "강 건너 한양으로 가는 길목", WorldBuilder.FerryStart, WorldBuilder.FerryEnd),
        new Map("콩밭 마을", "효녀 콩쥐와 못된 팥쥐가 산다는 부잣집 마을", WorldBuilder.KongStart, WorldBuilder.KongEnd),
        new Map("고개와 서낭당", "길손의 걱정이 돌무더기로 쌓인 고개", WorldBuilder.PassStart, WorldBuilder.PassEnd),
        new Map("뒷산", "소리를 질러도 아무도 듣지 못한다는 고개 너머 뒷산", WorldBuilder.HillStart, WorldBuilder.HillEnd),
    };
    // 맵이 바뀌기 전 경계를 얼마나 넘어 걷는지, 어둠이 닫히고 열리는 시간, 이름 카드.
    public const float Margin = .2f, FadeOut = .22f, FadeIn = .3f, CardLength = 2.4f;

    public PrototypeCombat combat;
    public int Current { get; private set; } = Index(WorldBuilder.Spawn.x);
    public float LeftAt { get; private set; } = -1;
    public float EnteredAt { get; private set; } = -1;
    public bool Travelling => LeftAt >= 0;
    public Map Here => All[Current];

    // `x`가 몇 번째 맵인지.
    public static int Index(float x)
    {
        for (int i = 0; i < All.Length; i++) if (x < All[i].right) return i;
        return All.Length - 1;
    }

    public void ResetMaps() { Current = Index(WorldBuilder.Spawn.x); LeftAt = EnteredAt = -1; }
    // 맵 전환 없이 지금 선 맵으로 바로 둔다(뒷산 체크포인트).
    public void Jump() { LeftAt = EnteredAt = -1; if (combat != null) Current = Index(combat.player.transform.position.x); }

    // 걷지 않고 다른 곳으로 옮김(R, 검사): 어둠 없이 그냥 그 맵이 된다. 가장자리를 걸어 나가는 건
    // Update가 (카메라의 LateUpdate보다) 먼저 보므로 그대로 어둠 전환이 된다.
    public void Sync()
    {
        if (combat == null || combat.player == null || Travelling) return;
        float x = combat.player.transform.position.x;
        if (x > Here.right + Margin || x < Here.left - Margin) Current = Index(x);
    }

    // `x`가 있는 맵이 화면에 완전히 보이는 맵인지(전환 중이 아님).
    public bool Shows(float x) { Sync(); return !Travelling && Current == Index(x); }

    // 이 맵만 보이게 할 수 있는 카메라 위치. 화면보다 좁은 맵은 가운데에 보여 준다.
    public float ClampView(float x, float halfWidth)
    {
        Sync();
        float lo = Here.left + halfWidth, hi = Here.right - halfWidth;
        return lo > hi ? (Here.left + Here.right) / 2 : Mathf.Clamp(x, lo, hi);
    }

    // 경계를 넘으면 어둠 전환을 시작한다.
    void Update()
    {
        if (combat == null || combat.player == null) return;
        float x = combat.player.transform.position.x;
        if (!Travelling)
        {
            if (x > Here.right + Margin || x < Here.left - Margin) LeftAt = Time.time;
            return;
        }
        // 어둠 뒤에서 카메라가 지금 맵으로 건너뛴다.
        if (Time.time - LeftAt >= FadeOut && EnteredAt < LeftAt)
        {
            Current = Index(x);
            EnteredAt = Time.time;
        }
        if (EnteredAt >= LeftAt && Time.time - EnteredAt >= FadeIn) LeftAt = -1;
    }

    // 화면이 얼마나 어두운지: 떠날 때 닫히고, 새 맵이 보이면 열린다.
    public float Cover => !Travelling ? 0 : EnteredAt < LeftAt ? Mathf.Clamp01((Time.time - LeftAt) / FadeOut)
        : 1 - Mathf.Clamp01((Time.time - EnteredAt) / FadeIn);

    // 어둠과 맵 이름을 그린다.
    void OnGUI()
    {
        GUI.depth = -50;
        PrototypeKoreanUI.Apply();
        if (Cover > 0)
        {
            JinaUI.Fill(new Rect(0, 0, JinaUI.W, JinaUI.H), new Color(0, 0, 0, Cover));
        }
        // 걸어 들어온 맵의 이름(시작할 때나 R 뒤에는 없음).
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
