using UnityEngine;

// 도깨비가 마을에 들어오면 노인이 외치며 칼을 뽑고, 주모와 보부상은
// 집으로 달아난다. 도깨비가 토벌되거나 정화되면 다시 나와 천천히 제자리로 걷는다.
// 노인이 쓰러졌으면 먼저 시체 곁에 잠시 서서 슬픔을 말한다.
// 여기서는 움직임만 맡고, 프레임은 KeeperVisual, PeddlerVisual, ElderVisual이 고른다.
public class VillageAlarm : MonoBehaviour
{
    public PrototypeCombat combat;
    public const string AlarmLine = "기어코 저 도깨비가 마을에 왔군! 모두 대피하시오!";
    public const float AlarmBubbleLength = 3.5f, RunSpeed = 3.6f, WalkSpeed = 1.1f;
    // 기와집 문. 마을 사람은 문 앞 마지막 반 칸에서 사라진다.
    public const float Shelter = 4.5f, DoorFade = .5f;
    // 경보 뒤 초(노인 외침을 먼저 듣는다)와 도깨비가 쓰러진 뒤 초(쓰러짐이 먼저 재생).
    public const float FleeDelay = .6f, ReturnDelay = 2.5f;
    // 쓰러진 노인 곁에 각자 서 있는 시간과 도착 뒤 말이 떠 있는 시간.
    public const float MournLength = 8f, MournLineLength = 4f;

    // 경보와 해제는 한 판에 한 번. 시각이 동작을 이끈다.
    public bool Alarmed { get; private set; }
    public bool Cleared { get; private set; }
    public float AlarmAt { get; private set; }
    public float ClearAt { get; private set; }

    public enum State { Home, Fleeing, Hidden, Returning, Mourning }
    public class Villager
    {
        public SpriteRenderer art;
        public string name, grief;
        public float home, stagger, bubble;
        // 쓰러진 노인 곁에서 서는 자리, 도착한 시각과 말을 시작하는 시각
        // (그 전에는 음수). 한 사람씩 MournLineLength 동안 말한다.
        public float mourn, mournedAt = -1, speakAt = -1;
        public State state;
    }
    public Villager Keeper { get; private set; }
    public Villager Peddler { get; private set; }
    public bool KeeperHome => Keeper == null || Keeper.state == State.Home;
    public bool PeddlerHome => Peddler == null || Peddler.state == State.Home;
    private KeeperVisual keeperVisual;
    // 다음 사람이 말을 시작할 수 있는 시각(앞사람이 끝난 뒤).
    private float lineFree;

    // 주모는 PrototypeWorld가, 보부상은 PeddlerShop.Start가 만드니 필요할 때 찾는다.
    void Bind()
    {
        if (Keeper == null)
        {
            var keeper = GameObject.Find("주막 주모");
            if (keeper != null)
            {
                Keeper = new Villager
                {
                    art = keeper.GetComponent<SpriteRenderer>(), home = WorldBuilder.Tavern.x, stagger = 0,
                    name = "주모", bubble = 1.4f, mourn = WorldBuilder.Elder.x - 2.4f,
                    grief = "노인장… 이리 허망하게 가시다니요.",
                };
                keeperVisual = KeeperVisual.Attach(Keeper.art, combat);
            }
        }
        if (Peddler == null && combat.Peddler != null && combat.Peddler.Marker != null)
            Peddler = new Villager
            {
                art = combat.Peddler.Marker, home = WorldBuilder.Peddler.x, stagger = .25f,
                name = "보부상", bubble = 1.55f, mourn = WorldBuilder.Elder.x - 3.3f,
                grief = "장터마다 노인장 덕담을 전했는데… 이제 누구에게 길을 묻겠소.",
            };
    }

    // 재시작: 주모·보부상을 제자리로.
    public void ResetVillage()
    {
        Alarmed = Cleared = false;
        foreach (var v in new[] { Keeper, Peddler })
        {
            if (v == null) continue;
            v.state = State.Home;
            v.mournedAt = v.speakAt = -1;
            Move(v, v.home, 1);
            v.art.enabled = true;
        }
        lineFree = 0;
        if (keeperVisual != null) keeperVisual.ResetPose();
    }

    // 경보, 대피, 귀가, 애도를 진행한다.
    void Update()
    {
        if (combat == null) return;
        Bind();
        if (combat.Paused) return;
        if (!Alarmed && combat.BossInVillage) { Alarmed = true; AlarmAt = Time.time; }
        if (Alarmed && !Cleared && combat.EnemyDefeated) { Cleared = true; ClearAt = Time.time; }
        foreach (var v in new[] { Keeper, Peddler })
            if (v != null) Step(v);
    }

    bool ElderDead => combat.Quest != null && combat.Quest.progress.ElderDead;
    // 다시 나오면 먼저 쓰러진 노인에게 (한 번) 갔다가 집으로.
    float Target(Villager v) => ElderDead && v.mournedAt < 0 ? v.mourn : v.home;
    // 쓰러진 노인 곁에 섬. 그림이 노인 쪽으로 돌린다.
    public bool IsMourning(SpriteRenderer art) =>
        (Keeper != null && Keeper.art == art && Keeper.state == State.Mourning) || (Peddler != null && Peddler.art == art && Peddler.state == State.Mourning);

    // 집 → (경보) 문으로 뜀 → 숨음 → (해제) 걸어 나옴 → (노인 쓰러짐: 애도) → 집.
    void Step(Villager v)
    {
        float x = v.art.transform.position.x;
        switch (v.state)
        {
            case State.Home:
                if (Alarmed && !Cleared && Time.time >= AlarmAt + FleeDelay + v.stagger) v.state = State.Fleeing;
                break;
            case State.Fleeing:
                x = Mathf.Max(Shelter, x - RunSpeed * Time.deltaTime);
                if (x <= Shelter) { v.state = State.Hidden; v.art.enabled = false; }
                break;
            case State.Hidden:
                if (Cleared && Time.time >= ClearAt + ReturnDelay + v.stagger * 4) { v.state = State.Returning; v.art.enabled = true; }
                break;
            case State.Returning:
                float target = Target(v);
                x = Mathf.MoveTowards(x, target, WalkSpeed * Time.deltaTime);
                if (x == target && target == v.home) v.state = State.Home;
                else if (x == target) Arrive(v);
                break;
            case State.Mourning:
                // 적어도 자기 말을 다 읽을 때까지는 머문다.
                if (Time.time >= Mathf.Max(v.mournedAt + MournLength, v.speakAt + MournLineLength + 1)) v.state = State.Returning;
                break;
        }
        Move(v, x, Mathf.Clamp01((x - Shelter) / DoorFade));
    }

    // 쓰러진 노인 곁에 도착: 애도를 시작하고 말할 차례를 잡는다.
    void Arrive(Villager v)
    {
        v.state = State.Mourning;
        v.mournedAt = Time.time;
        v.speakAt = Mathf.Max(Time.time, lineFree);
        lineFree = v.speakAt + MournLineLength;
    }

    // 마을 사람을 `x`로 옮기고 투명도를 정한다.
    void Move(Villager v, float x, float alpha)
    {
        var p = v.art.transform.position; p.x = x; v.art.transform.position = p;
        var c = v.art.color; c.a = alpha; v.art.color = c;
    }

    // 애도 말풍선을 그린다.
    void OnGUI()
    {
        if (combat == null || combat.Quest == null || combat.Paused) return;
        if (Alarmed && Time.time - AlarmAt < AlarmBubbleLength && !ElderDead)
            SpeechBubble.Draw(new Vector3(WorldBuilder.Elder.x, 1.45f, 0), "마을 노인", AlarmLine);
        // 한 번에 한 사람만 애도한다: 먼저 도착한 사람, 그 사람이 끝나면 다른 사람.
        foreach (var v in new[] { Keeper, Peddler })
            if (v != null && v.state == State.Mourning && Time.time >= v.speakAt && Time.time < v.speakAt + MournLineLength
                && SpeechBubble.OnScreen(v.art.transform.position))
                SpeechBubble.Draw(new Vector3(v.art.transform.position.x, v.bubble, 0), v.name, v.grief);
    }
}
