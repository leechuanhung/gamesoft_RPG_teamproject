using System.Collections.Generic;
using UnityEngine;

// The mountain pass beyond the ferry (PrototypeWorld.PassStart..PassEnd). While the seonbi climbs below the top,
// boulders roll down the steps toward him every few seconds: jump them or backstep (K) through them. At the
// shrine on the plateau he can lay a stone on the cairn (E) once a run, which restores him fully.
public class PrototypePass : MonoBehaviour
{
    public PrototypeCombat combat;
    public const float RockEvery = 3.2f, RollSpeed = 4.2f, Radius = .48f, Fall = 25f, ShrineRange = 1.8f;
    public class Rock { public SpriteRenderer art; public float vy; }
    public readonly List<Rock> rocks = new List<Rock>();
    public bool ShrineUsed { get; private set; }
    public float ShrineAt { get; private set; } = -1;
    private float nextRock;
    private string shrineSaid = "";

    public bool OnPass => combat != null && combat.player.transform.position.x >= PrototypeWorld.PassStart && combat.player.transform.position.x < PrototypeWorld.PassEnd;
    public bool NearShrine => combat != null && Mathf.Abs(combat.player.transform.position.x - PrototypeWorld.ShrineX) < ShrineRange
        && Mathf.Abs(combat.player.transform.position.y - PrototypeWorld.Top(PrototypeWorld.ShrineX)) < 1.5f;

    public void ResetPass()
    {
        foreach (var r in rocks) Destroy(r.art.gameObject);
        rocks.Clear();
        ShrineUsed = false; ShrineAt = -1; nextRock = 0; shrineSaid = "";
    }

    public Rock Spawn(float x)
    {
        var art = new GameObject("굴러오는 바위").AddComponent<SpriteRenderer>();
        art.sprite = JinaArt.PropCentred("boulder"); art.sortingOrder = 4;
        art.transform.position = new Vector3(x, PrototypeWorld.Top(x) + Radius, 0);
        var rock = new Rock { art = art };
        rocks.Add(rock);
        return rock;
    }

    // Rolls every boulder left by `dt`: down each step it drops under gravity; touching him is a blow.
    public void Step(float dt)
    {
        var body = combat.player.GetComponent<SpriteRenderer>().bounds;
        for (int i = rocks.Count - 1; i >= 0; i--)
        {
            var r = rocks[i]; var p = r.art.transform.position;
            p.x -= RollSpeed * dt;
            float floor = PrototypeWorld.Top(p.x) + Radius;
            if (p.y > floor) { r.vy -= Fall * dt; p.y = Mathf.Max(floor, p.y + r.vy * dt); }
            else { p.y = floor; r.vy = 0; }
            r.art.transform.position = p;
            r.art.transform.Rotate(0, 0, RollSpeed * dt / Radius * Mathf.Rad2Deg);
            if (new Bounds(p, Vector3.one * Radius * 1.7f).Intersects(body)) combat.HurtPlayer();
            if (p.x < PrototypeWorld.PassStart + 1) { Destroy(r.art.gameObject); rocks.RemoveAt(i); }
        }
    }

    // A stone on the cairn: full health, once a run.
    public bool UseShrine()
    {
        if (combat.PlayerHealth <= 0) return false;
        ShrineAt = Time.time;
        if (ShrineUsed) { shrineSaid = "이미 돌을 얹었습니다. 걱정은 한 번이면 족합니다."; return false; }
        ShrineUsed = true;
        combat.RestoreHealth(PrototypeCombat.MaxPlayerHealth);
        shrineSaid = "돌 하나를 얹었습니다. 지나간 이들의 걱정 위에 내 걱정도 하나.\n몸이 가벼워집니다. (체력 회복)";
        return true;
    }

    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused) return;
        if (NearShrine && Input.GetKeyDown(KeyCode.E)) UseShrine();
        // Boulders only while he is on the pass below the top of the slope.
        if (OnPass && combat.player.transform.position.x < PrototypeWorld.RockStart - 1 && Time.time >= nextRock)
        {
            Spawn(PrototypeWorld.RockStart);
            nextRock = Time.time + RockEvery;
        }
        Step(Time.deltaTime);
    }

    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused) return;
        PrototypeKoreanUI.Apply();
        if (ShrineAt >= 0 && Time.time - ShrineAt < 3f)
            JinaUI.Toast(shrineSaid);
        if (NearShrine && !ShrineUsed) JinaUI.Prompt("E: 서낭당 돌무더기에 돌 얹기");
    }

    void OnDestroy() { foreach (var r in rocks) if (r.art != null) Destroy(r.art.gameObject); }
}
