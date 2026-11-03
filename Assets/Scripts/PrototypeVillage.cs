using UnityEngine;

// When the boss walks into the village the elder calls out and draws his sword, and the keeper and the peddler
// run into the house. Once the boss is slain or purified they come back out and walk slowly to their places.
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
    private Sprite[] keeperRun;
    private float keeperWalk;

    // The keeper is built by PrototypeWorld and the peddler by PrototypePeddler.Start, so find them lazily.
    void Bind()
    {
        if (Keeper == null)
        {
            var keeper = GameObject.Find("주막 주모");
            if (keeper != null)
            {
                Keeper = new Villager { art = keeper.GetComponent<SpriteRenderer>(), home = PrototypeWorld.Tavern.x, stagger = 0 };
                keeperRun = new Sprite[4];
                for (int i = 0; i < 4; i++) keeperRun[i] = JinaArt.Prop("keeper_run_" + i.ToString("00"));
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
        if (Keeper != null) { Keeper.art.sprite = JinaArt.Prop("keeper"); Keeper.art.flipX = false; }
    }

    void Update()
    {
        if (combat == null) return;
        Bind();
        if (combat.Paused) { DrawKeeper(0); return; }
        if (!Alarmed && combat.BossInVillage) { Alarmed = true; AlarmAt = Time.time; }
        if (Alarmed && !Cleared && combat.EnemyDefeated) { Cleared = true; ClearAt = Time.time; }
        float keeperMoved = 0;
        foreach (var v in new[] { Keeper, Peddler })
        {
            if (v == null) continue;
            float x = v.art.transform.position.x, before = x;
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
            if (v == Keeper) keeperMoved = x - before;
        }
        DrawKeeper(keeperMoved);
    }

    void Move(Villager v, float x, float alpha)
    {
        var p = v.art.transform.position; p.x = x; v.art.transform.position = p;
        var c = v.art.color; c.a = alpha; v.art.color = c;
    }

    // The keeper is a single prop sprite: run frames while moving (slower steps on the way home),
    // otherwise she stands as drawn (facing left) or turns to the customer she is serving.
    void DrawKeeper(float moved)
    {
        if (Keeper == null) return;
        var art = Keeper.art;
        if (Mathf.Abs(moved) > .0001f)
        {
            keeperWalk += Time.deltaTime;
            art.sprite = keeperRun[(int)(keeperWalk / (Keeper.state == State.Fleeing ? .09f : .2f)) % 4];
            art.flipX = moved > 0;
            return;
        }
        keeperWalk = 0;
        art.sprite = JinaArt.Prop("keeper");
        art.flipX = combat.Tavern != null && combat.Tavern.IsOpen && combat.player.transform.position.x > art.transform.position.x;
    }

    void OnGUI()
    {
        if (combat == null || combat.Quest == null || !Alarmed || Time.time - AlarmAt >= AlarmBubbleLength) return;
        if (combat.Quest.progress.ElderDead || combat.Quest.IsDialogOpen) return;
        SpeechBubble.Draw(new Vector3(PrototypeWorld.Elder.x, 1.45f, 0), "마을 노인", AlarmLine);
    }
}
