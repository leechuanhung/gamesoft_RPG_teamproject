using UnityEngine;
using System.Collections.Generic;

// ===== 고개: 굴러오는 바위·서낭당 (MountainPass) =====
// 나루터 너머 고개(WorldBuilder.PassStart..PassEnd). 선비가 꼭대기 아래를 오르는 동안
// 몇 초마다 바위가 계단을 굴러 내려온다: 뛰어넘거나 백스텝(K)으로 통과한다.
// 고원의 서낭당에서 한 판에 한 번 돌탑에 돌을 얹으면(엔터) 체력이 다 찬다.
public class MountainPass : MonoBehaviour
{
    public Combat combat;
    public const float RockEvery = 3.2f, RollSpeed = 4.2f, Radius = .48f, Fall = 25f, ShrineRange = 1.8f;
    public class Rock { public SpriteRenderer art; public float vy; }
    public readonly List<Rock> rocks = new List<Rock>();
    public bool ShrineUsed { get; private set; }
    public float ShrineAt { get; private set; } = -1;
    private float nextRock;
    private string shrineSaid = "";

    public bool OnPass => combat != null && combat.player.transform.position.x >= WorldBuilder.PassStart && combat.player.transform.position.x < WorldBuilder.PassEnd;
    public bool NearShrine => combat != null && Mathf.Abs(combat.player.transform.position.x - WorldBuilder.ShrineX) < ShrineRange
        && Mathf.Abs(combat.player.transform.position.y - WorldBuilder.Top(WorldBuilder.ShrineX)) < 1.5f;

    // 재시작: 바위와 서낭당을 처음으로.
    public void ResetPass()
    {
        ClearRocks();
        ShrineUsed = false; ShrineAt = -1; shrineSaid = "";
    }
    // 굴러가는 바위를 모두 지운다.
    public void ClearRocks()
    {
        foreach (var r in rocks) Destroy(r.art.gameObject);
        rocks.Clear();
        nextRock = 0;
    }

    // `x`에 바위 하나를 굴린다.
    public Rock Spawn(float x)
    {
        var art = new GameObject("굴러오는 바위").AddComponent<SpriteRenderer>();
        art.sprite = JinaArt.PropCentred("boulder"); art.sortingOrder = 4;
        art.transform.position = new Vector3(x, WorldBuilder.Top(x) + Radius, 0);
        var rock = new Rock { art = art };
        rocks.Add(rock);
        return rock;
    }

    // 남은 바위를 `dt`만큼 왼쪽으로 굴린다. 계단마다 중력으로 떨어지고, 선비에게 닿으면 타격.
    public void Step(float dt)
    {
        var body = combat.player.GetComponent<SpriteRenderer>().bounds;
        for (int i = rocks.Count - 1; i >= 0; i--)
        {
            var r = rocks[i]; var p = r.art.transform.position;
            p.x -= RollSpeed * dt;
            float floor = WorldBuilder.Top(p.x) + Radius;
            if (p.y > floor) { r.vy -= Fall * dt; p.y = Mathf.Max(floor, p.y + r.vy * dt); }
            else { p.y = floor; r.vy = 0; }
            r.art.transform.position = p;
            r.art.transform.Rotate(0, 0, RollSpeed * dt / Radius * Mathf.Rad2Deg);
            if (new Bounds(p, Vector3.one * Radius * 1.7f).Intersects(body)) combat.HurtPlayer();
            if (p.x < WorldBuilder.PassStart + 1) { Destroy(r.art.gameObject); rocks.RemoveAt(i); }
        }
    }

    // 돌탑에 돌 하나: 체력 가득, 한 판에 한 번.
    public bool UseShrine()
    {
        if (combat.PlayerHealth <= 0) return false;
        ShrineAt = Time.time;
        if (ShrineUsed) { shrineSaid = "이미 돌을 얹었습니다. 걱정은 한 번이면 족합니다."; return false; }
        ShrineUsed = true;
        combat.RestoreHealth(Combat.MaxPlayerHealth);
        shrineSaid = "돌 하나를 얹었습니다. 지나간 이들의 걱정 위에 내 걱정도 하나.\n몸이 가벼워집니다. (체력 회복)";
        return true;
    }

    // 때가 되면 바위를 굴리고, 서낭당에서 엔터를 받는다.
    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused) return;
        if (NearShrine && JinaUI.Interact) UseShrine();
        // 선비가 고개에서 비탈 꼭대기 아래에 있을 때만 바위가 굴러온다.
        if (OnPass && combat.player.transform.position.x < WorldBuilder.RockStart - 1 && Time.time >= nextRock)
        {
            Spawn(WorldBuilder.RockStart);
            nextRock = Time.time + RockEvery;
        }
        Step(Time.deltaTime);
    }

    // 서낭당 안내를 그린다.
    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused) return;
        KoreanUI.Apply();
        if (ShrineAt >= 0 && Time.time - ShrineAt < 3f)
            JinaUI.Toast(shrineSaid);
        if (NearShrine && !ShrineUsed) JinaUI.Prompt("Enter: 서낭당 돌무더기에 돌 얹기");
    }

    void OnDestroy() { foreach (var r in rocks) if (r.art != null) Destroy(r.art.gameObject); }
}

// ===== 길가 조사 지점 (RoadsideLore) =====
// 길가의 작은 것들이 E에 한두 줄로 답한다: 아무것도 여기에 달려 있지 않지만 어떤 곳인지
// 알려 주고, 콩밭 마을에서는 그곳 사람들이 믿는 것과 조용히 어긋난다.
public class RoadsideLore : MonoBehaviour
{
    public Combat combat;
    public const float Range = 1.1f, ShowFor = 5f;
    public struct Spot
    {
        public float x; public string prompt, said, afterNight;
        public Spot(float x, string prompt, string said, string afterNight = null) { this.x = x; this.prompt = prompt; this.said = said; this.afterNight = afterNight; }
    }
    public static readonly Spot[] Spots =
    {
        // 비석 터.
        new Spot(-64f, "돌탑 살펴보기", "누군가 쌓다 만 돌탑이다. 망한 나라의 연호가 새겨진 돌이 맨 아래에 깔려 있다."),
        new Spot(-46f, "선돌 살펴보기", "글자는 닳아 없어졌는데, 사람 손이 닿은 자리만 반들반들하다. 지금도 찾아오는 이가 있는 모양이다."),
        // 망령의 들판.
        new Spot(-23.5f, "봉분 살펴보기", "이름 없는 봉분이다. 술 한 잔 올릴 사람도 없이 풀만 무성하다."),
        new Spot(-30f, "녹슨 창 살펴보기", "녹슨 창날이 땅에 박혀 있다. 이 창의 임자도 어쩌면 저 유령들 가운데 있을 것이다."),
        // 첫 마을.
        new Spot(4.5f, "기와집 문 두드리기", "대답이 없다. 문틈으로 곡식 찧는 소리만 들린다. 낯선 길손에게는 문을 열지 않는 모양이다."),
        new Spot(25f, "담장 너머 보기", "담 너머로 숲이 보인다. 해가 중천인데 숲 쪽만 저녁처럼 어둡다."),
        // 숲.
        new Spot(44f, "소나무 옹이 살펴보기", "옹이에 작은 그릇이 놓여 있다. 오래전 산의 주인에게 올리던 제물 그릇이다. 지금은 비어 있다."),
        new Spot(50f, "꺾인 가지 살펴보기", "굵은 가지가 맨손으로 꺾인 듯 찢겨 있다. 사람의 키보다 훨씬 높은 자리다."),
        // 나루터.
        new Spot(70f, "오두막 살펴보기", "낡은 노가 벽에 기대어 있다. 손잡이가 닳아 한 사람 손 모양으로 패었다."),
        new Spot(102.5f, "강가 돌 살펴보기", "젖은 돌 위에 여인의 신 한 짝이 가지런히 놓여 있다. 물에 떠밀려 온 것 치고는 너무 가지런하다."),
        // 콩밭 마을: 마을이 들려주는 이야기가 사람들 말과 다르다.
        new Spot(113f, "초가 살펴보기", "처마 밑에 말린 콩이 주렁주렁하다. 아이들 키를 잰 금이 기둥에 새겨져 있다. 잔치에 쓴다며 다들 곡식을 내놓는 중이다."),
        new Spot(119.5f, "우물 들여다보기", "두레박을 올리자 물에서 어렴풋이 짠내가 난다. 내륙 마을의 우물에서.", "두레박 끈에 낯익은 젓갈 냄새가 배어 있다. 간밤에 누가 여기서 무언가를 씻은 것 같다."),
        new Spot(128f, "빨랫줄 살펴보기", "괴롭힘 받는다는 콩쥐의 옷은 새것처럼 희고, 못됐다는 팥쥐의 옷은 소매마다 해져 기웠다."),
        new Spot(126f, "콩밭 살펴보기", "콩잎이 유난히 검푸르고 알이 굵다. 거름을 무얼 썼는지 물어보는 사람이 아무도 없다."),
        new Spot(138.5f, "평상 살펴보기", "두 사람 밥상이 차려져 있다. 한쪽 그릇은 비었고, 다른 쪽 그릇은 입도 대지 않은 채 식었다."),
        // 고개.
        new Spot(154f, "장승 살펴보기", "천하대장군, 지하여장군. 발치에 누가 숯으로 적어 놓았다. '이 고개는 해 지기 전에.'"),
        new Spot(176.5f, "서낭나무 살펴보기", "오색 천이 빽빽하다. 그중 하나에 삐뚤빼뚤한 글씨가 있다. '아버지 급제하게 해 주십시오.'"),
        new Spot(192.5f, "한양 쪽 바라보기", "구름 너머로 도성이 있을 터다. 길은 어둑한 뒷산 너머로 이어진다."),
        // 콩쥐를 만나기 전의 뒷산.
        new Spot(202.5f, "깨진 독 조각 살펴보기", "깨진 독 조각이 비탈에 흩어져 있다. 안쪽에 비늘 같은 것이 말라붙어 있다."),
        new Spot(211.5f, "발자국 살펴보기", "작은 맨발 자국이 비탈 위로 이어지다가… 어느 순간부터 물갈퀴 자국으로 바뀐다."),
    };

    // 가장 가까운 조사 지점 번호(없으면 -1).
    public int Near()
    {
        if (combat == null) return -1;
        var p = combat.player.transform.position;
        int best = -1; float bestD = Range;
        for (int i = 0; i < Spots.Length; i++)
        {
            float d = Mathf.Abs(p.x - Spots[i].x);
            if (d < bestD && p.y < WorldBuilder.Top(Spots[i].x) + 2.6f) { bestD = d; best = i; }
        }
        return best;
    }
    // 사람과 서낭당이 먼저: 근처에 다른 게 없을 때만 답한다.
    bool SomeoneElseHere()
    {
        var p = (Vector2)combat.player.transform.position;
        // 싸우는 중에도 안 나온다.
        if (combat.BossAlerted && !combat.EnemyDefeated) return true;
        if (combat.KongBoss != null && combat.KongBoss.Fighting) return true;
        foreach (var camp in combat.Camps)
            if (camp != null)
                foreach (var m in camp.mobs)
                    if (m.hp > 0 && m.art != null && Vector2.Distance(p, m.art.transform.position) < 6) return true;
        if (Vector2.Distance(p, WorldBuilder.Elder) < 1.9f || Vector2.Distance(p, WorldBuilder.Stone) < 1.9f) return true;
        if (Mathf.Abs(p.x - WorldBuilder.Tavern.x) < 2.2f || Mathf.Abs(p.x - WorldBuilder.Peddler.x) < 2.2f) return true;
        if (combat.Ferry != null && combat.Ferry.Near) return true;
        if (combat.Pass != null && combat.Pass.NearShrine) return true;
        if (combat.Kong != null && combat.Kong.InVillage && combat.Kong.HasSomethingHere) return true;
        if (combat.BossPurified && Vector2.Distance(p, combat.BossPosition) < 2.2f) return true;
        return false;
    }

    private float saidAt = -1;
    private string said = "";
    public void ResetLore() { saidAt = -1; said = ""; }

    // 엔터로 조사 지점을 읽는다.
    void Update()
    {
        if (combat == null || combat.Paused || combat.PlayerHealth <= 0) return;
        if (!JinaUI.Interact || SomeoneElseHere()) return;
        int i = Near();
        if (i < 0) return;
        Read(i);
    }
    // 조사 지점 `i`의 글을 연다.
    public void Read(int i)
    {
        var s = Spots[i];
        said = s.afterNight != null && combat.Kong != null && combat.Kong.Stayed ? s.afterNight : s.said;
        saidAt = Time.time;
    }

    // 조사 글과 E 안내를 그린다.
    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused) return;
        KoreanUI.Apply();
        if (saidAt >= 0 && Time.time - saidAt < ShowFor) JinaUI.Toast(said);
        int i = Near();
        if (i >= 0 && !SomeoneElseHere()) JinaUI.Prompt("Enter: " + Spots[i].prompt);
    }
}
