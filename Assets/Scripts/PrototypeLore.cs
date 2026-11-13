using UnityEngine;

// Small things along the road that answer to E with a line or two: nothing depends on them, but they say what kind of
// place this is, and in Kongbat they keep quietly disagreeing with what everyone there believes.
public class PrototypeLore : MonoBehaviour
{
    public PrototypeCombat combat;
    public const float Range = 1.1f, ShowFor = 5f;
    public struct Spot
    {
        public float x; public string prompt, said, afterNight;
        public Spot(float x, string prompt, string said, string afterNight = null) { this.x = x; this.prompt = prompt; this.said = said; this.afterNight = afterNight; }
    }
    public static readonly Spot[] Spots =
    {
        // The stele field.
        new Spot(-64f, "돌탑 살펴보기", "누군가 쌓다 만 돌탑이다. 망한 나라의 연호가 새겨진 돌이 맨 아래에 깔려 있다."),
        new Spot(-46f, "선돌 살펴보기", "글자는 닳아 없어졌는데, 사람 손이 닿은 자리만 반들반들하다. 지금도 찾아오는 이가 있는 모양이다."),
        // The field of ghosts.
        new Spot(-23.5f, "봉분 살펴보기", "이름 없는 봉분이다. 술 한 잔 올릴 사람도 없이 풀만 무성하다."),
        new Spot(-30f, "녹슨 창 살펴보기", "녹슨 창날이 땅에 박혀 있다. 이 창의 임자도 어쩌면 저 유령들 가운데 있을 것이다."),
        // The first village.
        new Spot(4.5f, "기와집 문 두드리기", "대답이 없다. 문틈으로 곡식 찧는 소리만 들린다. 낯선 길손에게는 문을 열지 않는 모양이다."),
        new Spot(25f, "담장 너머 보기", "담 너머로 숲이 보인다. 해가 중천인데 숲 쪽만 저녁처럼 어둡다."),
        // The forest.
        new Spot(44f, "소나무 옹이 살펴보기", "옹이에 작은 그릇이 놓여 있다. 오래전 산의 주인에게 올리던 제물 그릇이다. 지금은 비어 있다."),
        new Spot(50f, "꺾인 가지 살펴보기", "굵은 가지가 맨손으로 꺾인 듯 찢겨 있다. 사람의 키보다 훨씬 높은 자리다."),
        // The ferry.
        new Spot(70f, "오두막 살펴보기", "낡은 노가 벽에 기대어 있다. 손잡이가 닳아 한 사람 손 모양으로 패었다."),
        new Spot(102.5f, "강가 돌 살펴보기", "젖은 돌 위에 여인의 신 한 짝이 가지런히 놓여 있다. 물에 떠밀려 온 것 치고는 너무 가지런하다."),
        // Kongbat: the village tells a different story from its people.
        new Spot(113f, "초가 살펴보기", "처마 밑에 말린 콩이 주렁주렁하다. 아이들 키를 잰 금이 기둥에 새겨져 있다. 잔치에 쓴다며 다들 곡식을 내놓는 중이다."),
        new Spot(119.5f, "우물 들여다보기", "두레박을 올리자 물에서 어렴풋이 짠내가 난다. 내륙 마을의 우물에서.", "두레박 끈에 낯익은 젓갈 냄새가 배어 있다. 간밤에 누가 여기서 무언가를 씻은 것 같다."),
        new Spot(128f, "빨랫줄 살펴보기", "괴롭힘 받는다는 콩쥐의 옷은 새것처럼 희고, 못됐다는 팥쥐의 옷은 소매마다 해져 기웠다."),
        new Spot(126f, "콩밭 살펴보기", "콩잎이 유난히 검푸르고 알이 굵다. 거름을 무얼 썼는지 물어보는 사람이 아무도 없다."),
        new Spot(138.5f, "평상 살펴보기", "두 사람 밥상이 차려져 있다. 한쪽 그릇은 비었고, 다른 쪽 그릇은 입도 대지 않은 채 식었다."),
        // The pass.
        new Spot(154f, "장승 살펴보기", "천하대장군, 지하여장군. 발치에 누가 숯으로 적어 놓았다. '이 고개는 해 지기 전에.'"),
        new Spot(176.5f, "서낭나무 살펴보기", "오색 천이 빽빽하다. 그중 하나에 삐뚤빼뚤한 글씨가 있다. '아버지 급제하게 해 주십시오.'"),
        new Spot(192.5f, "한양 쪽 바라보기", "구름 너머로 도성이 있을 터다. 길은 어둑한 뒷산 너머로 이어진다."),
        // The back hill, before he meets her.
        new Spot(202.5f, "깨진 독 조각 살펴보기", "깨진 독 조각이 비탈에 흩어져 있다. 안쪽에 비늘 같은 것이 말라붙어 있다."),
        new Spot(211.5f, "발자국 살펴보기", "작은 맨발 자국이 비탈 위로 이어지다가… 어느 순간부터 물갈퀴 자국으로 바뀐다."),
    };

    public int Near()
    {
        if (combat == null) return -1;
        var p = combat.player.transform.position;
        int best = -1; float bestD = Range;
        for (int i = 0; i < Spots.Length; i++)
        {
            float d = Mathf.Abs(p.x - Spots[i].x);
            if (d < bestD && p.y < PrototypeWorld.Top(Spots[i].x) + 2.6f) { bestD = d; best = i; }
        }
        return best;
    }
    // People and shrines come first: this only answers when nothing else nearby would.
    bool SomeoneElseHere()
    {
        var p = (Vector2)combat.player.transform.position;
        // Nor in the middle of a fight.
        if (combat.BossAlerted && !combat.EnemyDefeated) return true;
        if (combat.KongBoss != null && combat.KongBoss.Fighting) return true;
        foreach (var camp in combat.Camps)
            if (camp != null)
                foreach (var m in camp.mobs)
                    if (m.hp > 0 && m.art != null && Vector2.Distance(p, m.art.transform.position) < 6) return true;
        if (Vector2.Distance(p, PrototypeWorld.Elder) < 1.9f || Vector2.Distance(p, PrototypeWorld.Stone) < 1.9f) return true;
        if (Mathf.Abs(p.x - PrototypeWorld.Tavern.x) < 2.2f || Mathf.Abs(p.x - PrototypeWorld.Peddler.x) < 2.2f) return true;
        if (combat.Ferry != null && combat.Ferry.Near) return true;
        if (combat.Pass != null && combat.Pass.NearShrine) return true;
        if (combat.Kong != null && combat.Kong.InVillage && combat.Kong.HasSomethingHere) return true;
        if (combat.BossPurified && Vector2.Distance(p, combat.BossPosition) < 2.2f) return true;
        return false;
    }

    private float saidAt = -1;
    private string said = "";
    public void ResetLore() { saidAt = -1; said = ""; }

    void Update()
    {
        if (combat == null || combat.Paused || combat.PlayerHealth <= 0) return;
        if (!Input.GetKeyDown(KeyCode.E) || SomeoneElseHere()) return;
        int i = Near();
        if (i < 0) return;
        Read(i);
    }
    public void Read(int i)
    {
        var s = Spots[i];
        said = s.afterNight != null && combat.Kong != null && combat.Kong.Stayed ? s.afterNight : s.said;
        saidAt = Time.time;
    }

    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused) return;
        PrototypeKoreanUI.Apply();
        if (saidAt >= 0 && Time.time - saidAt < ShowFor) JinaUI.Toast(said);
        int i = Near();
        if (i >= 0 && !SomeoneElseHere()) JinaUI.Prompt("E: " + Spots[i].prompt);
    }
}
