using UnityEngine;

// When the boss walks into the village the elder calls out and draws his sword, and the keeper and the peddler
// run into the house. Once the boss is slain or purified they come back out and walk slowly to their places.
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

    // The alarm and the all-clear happen once per run; the times drive the motions.
    public bool Alarmed { get; private set; }
    public bool Cleared { get; private set; }
    public float AlarmAt { get; private set; }
    public float ClearAt { get; private set; }

    public enum State { Home, Fleeing, Hidden, Returning }
    public class Villager
    {
        public SpriteRenderer art;
        public float home, stagger;
        public State state;
    }
    public Villager Keeper { get; private set; }
    public Villager Peddler { get; private set; }
    public bool KeeperHome => Keeper == null || Keeper.state == State.Home;
    public bool PeddlerHome => Peddler == null || Peddler.state == State.Home;
    private KeeperVisual keeperVisual;

    // The keeper is built by PrototypeWorld and the peddler by PrototypePeddler.Start, so find them lazily.
    void Bind()
    {
        if (Keeper == null)
        {
            var keeper = GameObject.Find("주막 주모");
            if (keeper != null)
            {
                Keeper = new Villager { art = keeper.GetComponent<SpriteRenderer>(), home = PrototypeWorld.Tavern.x, stagger = 0 };
                keeperVisual = KeeperVisual.Attach(Keeper.art, combat);
            }
        }
        if (Peddler == null && combat.Peddler != null && combat.Peddler.Marker != null)
            Peddler = new Villager { art = combat.Peddler.Marker, home = PrototypeWorld.Peddler.x, stagger = .25f };
    }

    public void ResetVillage()
    {
        Alarmed = Cleared = false;
        foreach (var v in new[] { Keeper, Peddler })
        {
            if (v == null) continue;
            v.state = State.Home;
            Move(v, v.home, 1);
            v.art.enabled = true;
        }
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

    // Home → (alarm) run to the door → hidden → (all clear) walk back out → home.
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
                x = Mathf.Min(v.home, x + WalkSpeed * Time.deltaTime);
                if (x >= v.home) v.state = State.Home;
                break;
        }
        Move(v, x, Mathf.Clamp01((x - Shelter) / DoorFade));
    }

    void Move(Villager v, float x, float alpha)
    {
        var p = v.art.transform.position; p.x = x; v.art.transform.position = p;
        var c = v.art.color; c.a = alpha; v.art.color = c;
    }

    void OnGUI()
    {
        if (combat == null || combat.Quest == null || !Alarmed || Time.time - AlarmAt >= AlarmBubbleLength) return;
        if (combat.Quest.progress.ElderDead || combat.Quest.IsDialogOpen) return;
        SpeechBubble.Draw(new Vector3(PrototypeWorld.Elder.x, 1.45f, 0), "마을 노인", AlarmLine);
    }
}
