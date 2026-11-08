using UnityEngine;

// When the boss walks into the village the elder calls out and draws his sword, and the keeper and the peddler
// run into the house. Once the boss is slain or purified they come back out and walk slowly to their places.
// If the elder fell, they first stand by his body for a while and speak their grief.
// This moves them; KeeperVisual, PeddlerVisual and ElderVisual pick their frames.
public class PrototypeVillage : MonoBehaviour
{
    public PrototypeCombat combat;
    public const string AlarmLine = "기어코 저 도깨비가 마을에 왔군! 모두 대피하시오!";
    public const float AlarmBubbleLength = 3.5f, RunSpeed = 3.6f, WalkSpeed = 1.1f;
    // The tiled house's door; villagers fade out over the last half unit before it.
    public const float Shelter = 4.5f, DoorFade = .5f;
    // Seconds after the alarm (they hear the elder first) and after the boss falls (its motion plays first).
    public const float FleeDelay = .6f, ReturnDelay = 2.5f;
    // How long each stands by the fallen elder, and how long their words stay up after they arrive.
    public const float MournLength = 8f, MournLineLength = 4f;

    // The alarm and the all-clear happen once per run; the times drive the motions.
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
        // Where they stand by the fallen elder, when they arrived there and when their words start
        // (negative until then); they speak one after the other, each for MournLineLength.
        public float mourn, mournedAt = -1, speakAt = -1;
        public State state;
    }
    public Villager Keeper { get; private set; }
    public Villager Peddler { get; private set; }
    public bool KeeperHome => Keeper == null || Keeper.state == State.Home;
    public bool PeddlerHome => Peddler == null || Peddler.state == State.Home;
    private KeeperVisual keeperVisual;
    // When the next mourner may start speaking (after the one before has finished).
    private float lineFree;

    // The keeper is built by PrototypeWorld and the peddler by PrototypePeddler.Start, so find them lazily.
    void Bind()
    {
        if (Keeper == null)
        {
            var keeper = GameObject.Find("주막 주모");
            if (keeper != null)
            {
                Keeper = new Villager
                {
                    art = keeper.GetComponent<SpriteRenderer>(), home = PrototypeWorld.Tavern.x, stagger = 0,
                    name = "주모", bubble = 1.4f, mourn = PrototypeWorld.Elder.x - 2.4f,
                    grief = "노인장… 이리 허망하게 가시다니요.",
                };
                keeperVisual = KeeperVisual.Attach(Keeper.art, combat);
            }
        }
        if (Peddler == null && combat.Peddler != null && combat.Peddler.Marker != null)
            Peddler = new Villager
            {
                art = combat.Peddler.Marker, home = PrototypeWorld.Peddler.x, stagger = .25f,
                name = "보부상", bubble = 1.55f, mourn = PrototypeWorld.Elder.x - 3.3f,
                grief = "장터마다 노인장 덕담을 전했는데… 이제 누구에게 길을 묻겠소.",
            };
    }

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
    // Walking back out, they go to the fallen elder first (once), then home.
    float Target(Villager v) => ElderDead && v.mournedAt < 0 ? v.mourn : v.home;
    // Standing by the fallen elder; their visuals turn them toward him.
    public bool IsMourning(SpriteRenderer art) =>
        (Keeper != null && Keeper.art == art && Keeper.state == State.Mourning) || (Peddler != null && Peddler.art == art && Peddler.state == State.Mourning);

    // Home → (alarm) run to the door → hidden → (all clear) walk back out → (elder fallen: mourn) → home.
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
                // They stay at least until their own words have been read.
                if (Time.time >= Mathf.Max(v.mournedAt + MournLength, v.speakAt + MournLineLength + 1)) v.state = State.Returning;
                break;
        }
        Move(v, x, Mathf.Clamp01((x - Shelter) / DoorFade));
    }

    void Arrive(Villager v)
    {
        v.state = State.Mourning;
        v.mournedAt = Time.time;
        v.speakAt = Mathf.Max(Time.time, lineFree);
        lineFree = v.speakAt + MournLineLength;
    }

    void Move(Villager v, float x, float alpha)
    {
        var p = v.art.transform.position; p.x = x; v.art.transform.position = p;
        var c = v.art.color; c.a = alpha; v.art.color = c;
    }

    void OnGUI()
    {
        if (combat == null || combat.Quest == null || combat.Paused) return;
        if (Alarmed && Time.time - AlarmAt < AlarmBubbleLength && !ElderDead)
            SpeechBubble.Draw(new Vector3(PrototypeWorld.Elder.x, 1.45f, 0), "마을 노인", AlarmLine);
        // One mourner speaks at a time: the first to arrive, then the other once the first has finished.
        foreach (var v in new[] { Keeper, Peddler })
            if (v != null && v.state == State.Mourning && Time.time >= v.speakAt && Time.time < v.speakAt + MournLineLength
                && SpeechBubble.OnScreen(v.art.transform.position))
                SpeechBubble.Draw(new Vector3(v.art.transform.position.x, v.bubble, 0), v.name, v.grief);
    }
}
