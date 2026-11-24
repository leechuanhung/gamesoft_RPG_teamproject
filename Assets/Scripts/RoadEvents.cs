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
        // 봉화 산성.
        new Spot(271.5f, "성벽 글씨 살펴보기", "성벽에 칼끝으로 새긴 글이 있다. '봉화가 오르지 않으니 한양은 아무것도 모른다.' 몇 해 전 날짜다."),
        // 버려진 역참.
        new Spot(300.5f, "부서진 수레 살펴보기", "쌀가마가 모두 찢겨 낟알 하나 남지 않았다. 수레에 '좌의정 댁 세곡'이라는 표찰이 붙어 있다."),
        new Spot(311f, "역참 현판 살펴보기", "빛바랜 현판에 '역'자만 남았다. 말 먹이던 구유에 녹슨 엽전이 몇 닢 붙어 있다. 세금을 못 내고 달아난 역졸의 것일까."),
        // 북망산 무덤길.
        new Spot(356f, "새 무덤 살펴보기", "흙이 아직 마르지 않은 무덤이다. 비석 대신 꽂힌 나무패에 '세금을 못 내 매 맞아 죽은 자'라고만 적혀 있다."),
        new Spot(381f, "무덤 앞 술잔 살펴보기", "누가 올린 술잔이 엎어져 있다. 잔에 남은 술이 먹물처럼 검다."),
        // 한양.
        new Spot(403f, "기와집 대문 살펴보기", "대문마다 '좌의정 대감 은혜'라고 적힌 부적이 붙어 있다. 붙이지 않은 집은 하나같이 비어 있다."),
        new Spot(437f, "홍살문 살펴보기", "붉은 살 사이로 과거장이 보인다. 내일이면 이 문을 지나 붓을 든다."),
        new Spot(447.5f, "과거 방 읽기", "방이 붙어 있다. '내일 진시, 과거를 연다. 시관 좌의정 엄탁.' 그 아래 누가 작게 덧붙였다. '시제는 이미 팔렸다.'"),
        new Spot(476f, "담장 밑 살펴보기", "담장 밑에 무당의 방울 하나가 떨어져 있다. 흔들어 보니 방울 속에서 사람 이빨 같은 것이 달그락거린다."),
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
        if (combat.FinalBoss != null && (combat.FinalBoss.Fighting || combat.FinalBoss.Cutscene)) return true;
        if (combat.Fort != null && combat.Fort.HasSomethingHere) return true;
        if (combat.Town != null && combat.Town.HasSomeoneHere) return true;
        if (combat.Mobs != null)
            foreach (var m in combat.Mobs.mobs)
                if (m.Alive && !m.witness && Vector2.Distance(p, m.pos) < 6) return true;
        foreach (var camp in combat.Camps)
            if (camp != null)
                foreach (var m in camp.mobs)
                    if (m.hp > 0 && m.art != null && Vector2.Distance(p, m.art.transform.position) < 6) return true;
        if (Vector2.Distance(p, WorldBuilder.Elder) < 1.9f || Vector2.Distance(p, WorldBuilder.Stone) < 1.9f) return true;
        if (Mathf.Abs(p.x - WorldBuilder.Tavern.x) < 2.2f || Mathf.Abs(p.x - WorldBuilder.Peddler.x) < 2.2f) return true;
        if ((combat.Tavern != null && combat.Tavern.Near) || (combat.Peddler != null && combat.Peddler.Near)) return true;
        if (combat.Ferry != null && combat.Ferry.Near) return true;
        if (combat.Pass != null && combat.Pass.NearShrine) return true;
        if (combat.Kong != null && combat.Kong.InVillage && combat.Kong.HasSomethingHere) return true;
        if (combat.BossPurified && Vector2.Distance(p, combat.BossPosition) < 2.2f) return true;
        return false;
    }

    // 선돌(-46)에서 만나는 도깨비불: 기존 길가 조사 시스템 안에서 퀘스트와 동행을 관리한다.
    public const int WispStoneIndex = 1;
    public const float WispFollowDistance = 1.35f, WispSpeed = 5.5f;
    public bool WispAsking { get; private set; }
    public bool WispFollowing { get; private set; }
    private SpriteRenderer wisp;
    private float wispBob;
    private float wispBobOffset;
    private float wispGlimmer;
    private bool wispAbsorbed;
    private bool wispApproachingBoss;
    private Vector3 wispApproachStart;
    private const float WispAbsorbAfter = 1.5f; // 추가 연출(2.2초) 중 도깨비에게 닿는 시점
    private string wispLine = "";

    // 별도 프리팹 없이 아이템 도트(20x23)를 월드용 작은 도깨비불로 만든다.
    // Assets/Resources/JinaV4/14_Items/item_dokkabibol.png 필요. Texture2D 기본 Import 설정으로도 로딩 가능.
    void EnsureWisp()
    {
        if (wisp != null) return;
        var texture = Resources.Load<Texture2D>("JinaV4/14_Items/item_dokkabibol");
        if (texture == null) { Debug.LogWarning("도깨비불 그림 누락: Assets/Resources/JinaV4/14_Items/item_dokkabibol.png"); return; }
        var obj = new GameObject("동행 도깨비불");
        wisp = obj.AddComponent<SpriteRenderer>();
        wisp.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), texture.height / .65f);
        wisp.sortingOrder = 8;
        wisp.transform.position = new Vector3(Spots[WispStoneIndex].x, 1.15f, 0);
        wispBob = wispBobOffset = 0f;
    }

    void AskWisp()
    {
        EnsureWisp();
        WispAsking = true;
        wispLine = "…저기, 선비님. 나를 이 쓸쓸한 곳에서 데려가 주실래요?\n함께 길을 가고 싶어요. 숲의 주인도… 원래는 그런 분이 아니었어요.";
        combat.player.CanMove = false;
        saidAt = -1;
    }

    // 거절해도 선돌에서 다시 말을 걸 수 있다.
    void AnswerWisp(bool accept)
    {
        WispAsking = false;
        WispFollowing = accept;
        combat.player.CanMove = combat.PlayerHealth > 0;
        said = accept ? "도깨비불이 조심스레 선비의 뒤를 따르기 시작했다." : "도깨비불이 잠시 고개를 떨구었다. 다시 말을 걸 수 있을 것 같다.";
        saidAt = Time.time;
        if (!accept && wisp != null) { Destroy(wisp.gameObject); wisp = null; }
    }

    // 플레이어보다 조금 늦게 따라온다. 조우 연출 후반에는 도깨비 쪽으로 다가간다.
    void FollowWisp()
    {
        // 도깨비에게 흡수된 뒤에는 다시 생성하거나 따라오지 않는다.
        if (!WispFollowing || wispAbsorbed || combat == null || combat.player == null) return;
        EnsureWisp();
        if (wisp == null) return;
        var encounter = combat.Encounter;
        if (encounter != null && encounter.WispResonating)
        {
            // 연출이 시작되는 프레임의 도깨비불 위치를 기억해, 보스가 멀어도
            // 1.5초 동안 실제로 날아가는 모습을 보여 준다(순간이동 없음).
            if (!wispApproachingBoss)
            {
                wispApproachingBoss = true;
                wispApproachStart = wisp.transform.position - Vector3.up * wispBobOffset;
            }
            float t = Mathf.Clamp01((encounter.Elapsed - BossEncounter.Length) / WispAbsorbAfter);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 destination = combat.BossPosition + new Vector3(-.45f, 1.2f, 0);
            Vector3 basePosition = Vector3.Lerp(wispApproachStart, destination, eased);
            wispBob += Time.deltaTime * 2.5f;
            wispBobOffset = Mathf.Sin(wispBob) * .08f * (1f - t);
            wisp.transform.position = basePosition + Vector3.up * wispBobOffset;
            // 마지막에 작아지면서 사라져, 도깨비의 몸에 스며드는 느낌을 준다.
            wisp.transform.localScale = Vector3.one * Mathf.Lerp(1f, .15f, Mathf.Clamp01((t - .7f) / .3f));
            var color = wisp.color;
            color.a = 1f - Mathf.Clamp01((t - .75f) / .25f);
            wisp.color = color;
            if (t >= 1f)
            {
                JinaFx.Glint(destination, new Color(.3f, .9f, 1f));
                JinaFx.Rise(destination, 8, new Color(.3f, .9f, 1f), .7f, 1.2f, .6f);
                Destroy(wisp.gameObject);
                wisp = null;
                wispAbsorbed = true; // 재시작 전까지 영구 소멸, 방어 약화는 그대로 유지
            }
            return;
        }

        var playerPos = combat.player.transform.position;
        float side = combat.player.Facing > 0 ? -1 : 1;
        Vector3 target = new Vector3(playerPos.x + side * WispFollowDistance, playerPos.y + 1.0f, 0);
        // 떠다니는 높이를 빼고 실제 추적 위치를 계산한다.
        var normalBasePosition = wisp.transform.position - Vector3.up * wispBobOffset;
        if (Vector2.Distance(normalBasePosition, target) > 24f)
            normalBasePosition = target;
        else if (!combat.Paused)
            normalBasePosition = Vector3.MoveTowards(normalBasePosition, target, WispSpeed * Time.deltaTime);
        wispBob += Time.deltaTime * 2.5f;
        wispBobOffset = Mathf.Sin(wispBob) * .08f;
        wisp.transform.position = normalBasePosition + Vector3.up * wispBobOffset;
        if (Time.time >= wispGlimmer && !combat.Paused)
        {
            wispGlimmer = Time.time + .7f;
            JinaFx.Glint(wisp.transform.position, new Color(.3f, .9f, 1f));
        }
    }

    private float saidAt = -1;
    private string said = "";
    public void ResetLore()
    {
        saidAt = -1; said = "";
        WispAsking = WispFollowing = wispAbsorbed = wispApproachingBoss = false;
        wispBob = wispBobOffset = 0f;
        if (wisp != null) { Destroy(wisp.gameObject); wisp = null; }
    }

    // 엔터로 조사 지점을 읽는다.
    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        FollowWisp();
        if (WispAsking)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) AnswerWisp(true);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Escape)) AnswerWisp(false);
            return;
        }
        if (combat.Paused || !JinaUI.Interact || SomeoneElseHere()) return;
        int i = Near();
        if (i < 0) return;
        Read(i);
    }
    // 조사 지점 `i`의 글을 연다.
    public void Read(int i)
    {
        if (i == WispStoneIndex && !WispFollowing) { AskWisp(); return; }
        var s = Spots[i];
        said = s.afterNight != null && combat.Kong != null && combat.Kong.Stayed ? s.afterNight : s.said;
        saidAt = Time.time;
    }

    // 조사 글과 E 안내를 그린다.
    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        KoreanUI.Apply();
        if (WispAsking)
        {
            float width = Mathf.Min(720, JinaUI.W - 32);
            float height = 240;
            var box = new Rect((JinaUI.W - width) / 2, JinaUI.H - height - 24, width, height);
            var inside = JinaUI.Window(box, "선돌의 도깨비불", "함께 길을 떠나겠습니까?");
            JinaUI.Text(new Rect(inside.x, inside.y, inside.width, 108), wispLine, 18, JinaUI.TextDark);
            float choiceY = inside.y + 112, half = (inside.width - 12) / 2;
            if (JinaUI.Button(new Rect(inside.x, choiceY, half, 40), "1 · 수락")) AnswerWisp(true);
            if (JinaUI.Button(new Rect(inside.x + half + 12, choiceY, half, 40), "2 · 거절")) AnswerWisp(false);
            return;
        }
        if (combat.Paused) return;
        if (saidAt >= 0 && Time.time - saidAt < ShowFor) JinaUI.Toast(said);
        int i = Near();
        if (i >= 0 && !SomeoneElseHere())
            JinaUI.Prompt(i == WispStoneIndex && WispFollowing ? (wispAbsorbed ? "도깨비불의 빛은 숲의 주인에게 스며들었다." : "도깨비불이 선비와 함께하고 있다.") : "Enter: " + Spots[i].prompt);
    }

    void OnDestroy() { if (wisp != null) Destroy(wisp.gameObject); }
}
