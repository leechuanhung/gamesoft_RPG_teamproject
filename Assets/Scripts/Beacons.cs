using UnityEngine;

// ===== 봉화 산성 (BeaconFort) =====
// 뒷산 너머 산성(WorldBuilder.FortStart~FortEnd)은 문이 닫혀 있다. 성문 앞 봉수 군령판이 봉화를 올리는 순서를
// 말해 준다: 마른 우물 곁 → 무너진 성가퀴 위 → 늙은 소나무 아래 → 가장 높은 곳 → 성문 앞.
// 다섯 봉수대는 산성 여기저기, 높이가 다른 곳에 있다. F·클릭으로 하나씩 불을 올리고, 순서가 틀리면 모두 꺼진다.
// 다섯이 바르게 오르면 성문이 열린다. 몬스터는 없다(퍼즐 맵).
public class BeaconFort : MonoBehaviour
{
    public Combat combat;
    public struct Beacon
    {
        public float x, top; public string place;
        public Beacon(float x, float top, string place) { this.x = x; this.top = top; this.place = place; }
    }
    // 왼쪽부터: 소나무 아래, 성가퀴 위(계단), 가장 높은 발판, 우물 곁, 성문 앞.
    public static readonly Beacon[] All =
    {
        new Beacon(250.5f, -.5f, "늙은 소나무 아래"), new Beacon(256f, .8f, "무너진 성가퀴 위"),
        new Beacon(WorldBuilder.FortPeak.x, WorldBuilder.FortPeak.y, "가장 높은 곳"),
        new Beacon(277.5f, -.5f, "마른 우물 곁"), new Beacon(285.5f, -.5f, "성문 앞"),
    };
    // 군령판이 말하는 순서(All 번호).
    public static readonly int[] Order = { 3, 1, 0, 2, 4 };
    public const float BoardX = 248.2f, Range = 1.3f, GateX = WorldBuilder.FortGateX;
    public const string Orders = "봉수 군령: 산성 문은 봉화가 바른 차례로 오를 때에만 열린다. 첫 불은 마른 우물 곁, 둘째는 무너진 성가퀴 위, 셋째는 늙은 소나무 아래, 넷째는 가장 높은 곳, 마지막은 성문 앞.";
    public const string Route = "봉화 순서: 마른 우물 → 성가퀴 위 → 늙은 소나무 → 가장 높은 곳 → 성문 앞";

    public int Step { get; private set; }
    public bool Open => Step >= Order.Length;
    public bool ReadOrders { get; private set; }
    public float WrongAt { get; private set; } = -1;
    public float OpenedAt { get; private set; } = -1;
    public float ReadAt { get; private set; } = -1;
    public float LitAt { get; private set; } = -1;
    private readonly bool[] lit = new bool[5];
    private SpriteRenderer[] towers;
    private GameObject gate;
    private SpriteRenderer gateArt;
    public bool IsLit(int i) => lit[i];

    private GameObject root;
    // 봉수대와 문은 따로 둔다: 이 컴포넌트가 붙은 전투 오브젝트는 카메라라서 그 밑에 두면 화면을 따라다닌다.
    void Start()
    {
        root = new GameObject("봉화 산성");
        towers = new SpriteRenderer[All.Length];
        for (int i = 0; i < All.Length; i++)
        {
            var art = new GameObject("봉수대 " + All[i].place).AddComponent<SpriteRenderer>();
            art.transform.SetParent(root.transform);
            art.transform.position = new Vector3(All[i].x, All[i].top, 0);
            art.sortingOrder = -4;
            towers[i] = art;
        }
        gateArt = new GameObject("산성 문").AddComponent<SpriteRenderer>();
        gateArt.transform.SetParent(root.transform);
        gateArt.transform.position = new Vector3(GateX, -.5f, 0);
        gateArt.sortingOrder = -8;
        gate = new GameObject("산성 문 빗장");
        gate.transform.SetParent(root.transform);
        gate.transform.position = new Vector3(GateX, 9.5f, 0);
        gate.layer = 8;
        gate.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 20);
        ResetFort();
    }

    // 재시작: 모든 봉화를 끄고 문을 닫는다.
    public void ResetFort()
    {
        Step = 0; ReadOrders = false; WrongAt = OpenedAt = ReadAt = LitAt = -1;
        for (int i = 0; i < lit.Length; i++) lit[i] = false;
        if (gate != null) gate.SetActive(true);
        Draw();
    }

    // 가장 가까운 봉수대 번호(없으면 -1). 같은 높이에 서 있어야 한다.
    public int Near()
    {
        if (combat == null) return -1;
        var p = combat.player.transform.position;
        for (int i = 0; i < All.Length; i++)
            if (Mathf.Abs(p.x - All[i].x) < Range && p.y > All[i].top - .4f && p.y < All[i].top + 1.6f) return i;
        return -1;
    }
    public bool NearBoard => combat != null && Mathf.Abs(combat.player.transform.position.x - BoardX) < Range && combat.player.transform.position.y < 1.5f;
    // 조사 지점(RoadsideLore)보다 먼저 답한다.
    public bool HasSomethingHere => NearBoard || Near() >= 0;

    void Update()
    {
        if (combat == null || combat.Paused || combat.PlayerHealth <= 0) return;
        if (Open && Time.frameCount % 20 == 0) Smoke();
        if (!JinaUI.Interact) return;
        if (NearBoard) { ReadOrders = true; ReadAt = Time.time; return; }
        int i = Near();
        if (i >= 0) Light(i);
    }

    // 봉수대 `i`에 불을 올린다. 차례가 맞으면 true. 틀리면 모두 꺼지고 처음부터.
    public bool Light(int i)
    {
        if (Open || lit[i]) return false;
        var fire = new Vector3(All[i].x, All[i].top + 1.8f, 0);
        if (Order[Step] != i)
        {
            for (int k = 0; k < lit.Length; k++)
                if (lit[k]) JinaFx.Rise(new Vector3(All[k].x, All[k].top + 1.8f, 0), 10, new Color(.5f, .5f, .52f, .8f), .5f, 1.6f, 1f);
            for (int k = 0; k < lit.Length; k++) lit[k] = false;
            Step = 0; WrongAt = Time.time;
            JinaFx.Rise(fire, 8, new Color(.45f, .45f, .48f, .8f), .4f, 1.2f, .8f);
            Draw();
            return false;
        }
        lit[i] = true; Step++; LitAt = Time.time;
        JinaFx.Dots(fire, 14, new Color(1, .7f, .3f), 3, .5f);
        JinaFx.Glint(fire, new Color(1, .86f, .5f));
        if (Open)
        {
            OpenedAt = Time.time;
            gate.SetActive(false);
            View.Shake(.06f, .5f);
            JinaFx.Dust(new Vector3(GateX, -.5f, 0), -1.4f, 1.6f);
            JinaFx.Dust(new Vector3(GateX, -.5f, 0), 1.4f, 1.6f);
        }
        Draw();
        return true;
    }
    void Draw()
    {
        if (towers == null) return;
        for (int i = 0; i < towers.Length; i++) towers[i].sprite = JinaArt.Prop(lit[i] ? "beacon_on" : "beacon_off");
        gateArt.sprite = JinaArt.Prop(Open ? "fort_gate_open" : "fort_gate_shut");
    }
    // 다 오른 봉화에서 연기가 한양 쪽으로 오른다.
    void Smoke()
    {
        for (int i = 0; i < All.Length; i++) JinaFx.Rise(new Vector3(All[i].x, All[i].top + 2.6f, 0), 1, new Color(.86f, .84f, .8f, .5f), .3f, 2.4f, 1.6f);
    }

    void OnDestroy() { if (root != null) Destroy(root); }

    void OnGUI()
    {
        if (combat == null || combat.Paused || combat.PlayerHealth <= 0) return;
        KoreanUI.Apply();
        if (ReadAt >= 0 && Time.time - ReadAt < 7f) JinaUI.Toast(Orders);
        if (WrongAt >= 0 && Time.time - WrongAt < 3f) JinaUI.Toast("봉화가 엉뚱한 차례로 올라 모두 꺼졌습니다. 군령판의 차례를 다시 떠올려 보세요.");
        if (LitAt >= 0 && Time.time - LitAt < 1.6f && !Open) JinaUI.Toast("봉화가 올랐습니다. (" + Step + "/" + Order.Length + ")");
        if (OpenedAt >= 0 && Time.time - OpenedAt < 4f) JinaUI.Toast("다섯 봉화가 한양 쪽으로 이어 올랐습니다. 무거운 산성 문이 열립니다.");
        if (NearBoard) JinaUI.Prompt("F·클릭: 봉수 군령판 읽기");
        else
        {
            int i = Near();
            if (i >= 0 && !lit[i] && !Open) JinaUI.Prompt("F·클릭: 봉화 올리기 (" + All[i].place + ")");
        }
    }
}
