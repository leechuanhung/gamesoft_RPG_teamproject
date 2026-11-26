using System;
using System.Collections.Generic;
using UnityEngine;

// 추가 퀘스트의 상태, 공통 표시와 진행 HUD를 담당한다.
// 기존 도깨비불 퀘스트는 RoadsideLore의 상태를 읽기만 한다.
public class QuestSystem : MonoBehaviour
{
    public Combat combat;
    public enum State { Available, Active, Completed, Failed }

    class Entry
    {
        public string title;
        public Func<string> objective;
        public Func<bool> available;
        public Func<Vector3> markerPosition;
        public State state;
        public SpriteRenderer marker;
    }

    readonly Dictionary<string, Entry> quests = new Dictionary<string, Entry>();
    bool expanded = true;
    Sprite markerSprite;

    // 한양 아이의 부탁: 기존 몬스터와 아이템 시스템의 상태를 이용한다.
    const string ChildQuest = "grave_child";
    const int ChildTarget = 10, ChildHonor = 10, ChildYakgwa = 1;
    int graveKillsAtAccept, roadAliveAtAccept;
    HanyangTown.Person child;
    const string ChildOffer = "선비님, 북망산 무덤길에 요괴가 너무 많아서 친구들이랑 놀러 가지도 못해요. 그곳의 요괴를 열 마리만 물리쳐 주실래요?";


    void Start()
    {
        combat.Restarted += ResetQuests;
        child = Array.Find(HanyangTown.People, p => p.who == HanyangTown.Who.Ai);
        Register(ChildQuest, "아이의 부탁", ChildObjective,
            () => new Vector3(child.x, WorldBuilder.Top(child.x) + 1.85f, 0),
            () => combat.Quest.progress.Honor >= 0);
    }

    // 신규 퀘스트 등록. markerPosition은 느낌표 중심의 월드 좌표(둥실거림 제외).
    // available은 수락 가능 조건, objective는 현재 목표 문구를 돌려준다.
    public void Register(string id, string title, Func<string> objective,
        Func<Vector3> markerPosition, Func<bool> available = null)
    {
        quests.Add(id, new Entry { title = title, objective = objective,
            markerPosition = markerPosition, available = available, state = State.Available });
    }

    public bool CanAccept(string id) => quests.TryGetValue(id, out var q)
        && q.state == State.Available && (q.available == null || q.available());

    public bool IsActive(string id) => quests.TryGetValue(id, out var q) && q.state == State.Active;

    public bool TryAccept(string id)
    {
        if (!CanAccept(id)) return false;
        quests[id].state = State.Active;
        return true;
    }

    // 실제 완료 조건 확인과 보상 지급이 성공한 뒤 호출한다.
    public bool Complete(string id) => Finish(id, State.Completed);
    public bool Fail(string id) => Finish(id, State.Failed);

    bool Finish(string id, State result)
    {
        if (!IsActive(id)) return false;
        quests[id].state = result;
        return true;
    }

    int GraveRoadAlive()
    {
        int count = 0;
        int map = Maps.Index(WorldBuilder.StationEnd);
        foreach (var mob in combat.Mobs.mobs)
            if (mob.map == map && !mob.soldier && mob.Alive) count++;
        return count;
    }

    int ChildKills => Mathf.Clamp(combat.GraveGhosts.Kills - graveKillsAtAccept
        + roadAliveAtAccept - GraveRoadAlive(), 0, ChildTarget);

    string ChildObjective() => ChildKills >= ChildTarget ? "한양의 아이에게 돌아가기"
        : "북망산 무덤길 요괴 처치 (" + ChildKills + "/" + ChildTarget + ")";

    // 진행 중인 부탁과 보상 대사만 처리한다. 수락 창은 별도로 그린다.
    public string[] ChildDialogue(string[] original)
    {
        if (IsActive(ChildQuest))
        {
            if (ChildKills < ChildTarget)
                return new[] { "선비님, 무덤길 요괴를 " + ChildKills + "마리나 물리쳐 주셨군요! 열 마리만 물리쳐 주세요." };
            if (!JinaItems.Grant("yakgwa", ChildYakgwa))
                return new[] { "선비님께 드릴 약과가 있어요! 보따리에 자리가 나면 다시 와 주세요." };
            Complete(ChildQuest);
            combat.Quest.progress.CheatHonor(ChildHonor);
            combat.PreserveQuestHonor(ChildHonor);
            return new[] { "와아! 이제 친구들이랑 놀러 갈 수 있겠어요. 고마워요, 선비님!",
                "엄마가 저 먹으라고 주신 약과인데, 선비님 드릴래요. 꼭 받아 주세요! (약과 " + ChildYakgwa + "개 획득 · 선행 +" + ChildHonor + ")" };
        }
        return original;
    }

    public bool CanOfferChild => CanAccept(ChildQuest);

    // 도깨비불과 같은 별도 퀘스트 수락 창. 선택 결과는 한양 NPC에 전달한다.
    public void DrawChildOffer(Action<bool> answer)
    {
        float width = Mathf.Min(720, JinaUI.W - 32);
        float height = 240;
        var box = new Rect((JinaUI.W - width) / 2, JinaUI.H - height - 24, width, height);
        var inside = JinaUI.Window(box, "아이의 부탁", "부탁을 들어주시겠습니까?");
        JinaUI.Text(new Rect(inside.x, inside.y, inside.width, 108), ChildOffer, 18, JinaUI.TextDark);
        float choiceY = inside.y + 112, half = (inside.width - 12) / 2;
        if (JinaUI.Button(new Rect(inside.x, choiceY, half, 40), "1 · 수락")) answer(true);
        else if (JinaUI.Button(new Rect(inside.x + half + 12, choiceY, half, 40), "2 · 거절")) answer(false);
    }

    public string AnswerChild(bool accept)
    {
        if (!accept) return "알겠어요. 나중에라도 도와주시면 좋겠어요.";
        graveKillsAtAccept = combat.GraveGhosts.Kills;
        roadAliveAtAccept = GraveRoadAlive();
        if (!TryAccept(ChildQuest)) return "다음에 다시 이야기해요.";
        return "정말요? 고마워요, 선비님! 조심해서 다녀오세요.";
    }

    // 기존 도깨비불 상태만 참조한다. 동행 및 흡수 로직은 변경하지 않는다.
    bool WispActive => combat.Lore != null && combat.Lore.WispFollowing
        && combat.Quest.progress.Outcome == EncounterOutcome.Unresolved
        && (combat.Encounter == null || !combat.Encounter.WispWeakened);

    // 신규 퀘스트 모두 같은 이미지와 움직임을 사용한다.
    void LateUpdate()
    {
        foreach (var pair in quests)
        {
            var q = pair.Value;
            bool show = q.markerPosition != null && CanAccept(pair.Key);
            if (show && q.marker == null) q.marker = CreateMarker(q.title);
            if (q.marker == null) continue;
            q.marker.enabled = show;
            if (show)
                q.marker.transform.position = q.markerPosition() + Vector3.up * (Mathf.Sin(Time.time * 2f) * .09f);
        }
    }

    SpriteRenderer CreateMarker(string title)
    {
        if (markerSprite == null)
        {
            var texture = Resources.Load<Texture2D>("JinaV4/14_Items/item_quest_marker");
            if (texture == null) return null;
            markerSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(.5f, .5f), texture.height / 1.5f);
        }
        var marker = new GameObject(title + " 퀘스트 표시").AddComponent<SpriteRenderer>();
        marker.sprite = markerSprite;
        marker.sortingOrder = 9;
        return marker;
    }

    // Hud.DrawMorals에서 실제 선악 판의 아래쪽 좌표를 받아 그린다.
    public void DrawPanel(float x, float y, float w)
    {
        if (combat.Paused) return;
        var rows = new List<(string title, string objective)>();
        if (WispActive) rows.Add(("도깨비불의 소망", "숲의 도깨비에게 데려가기"));
        foreach (var q in quests.Values)
            if (q.state == State.Active) rows.Add((q.title, q.objective()));
        if (rows.Count == 0) return;

        const float header = 28, pad = 10;
        float inner = w - pad * 2;
        var heading = new Rect(x, y, w, header);
        JinaUI.MarkButton(heading);   // 이 위의 클릭은 공격·상호작용이 아니다
        if (GUI.Button(heading, GUIContent.none, GUIStyle.none)) expanded = !expanded;
        float height = header;
        if (expanded)
        {
            foreach (var row in rows)
                height += JinaUI.Height(row.title, 12, inner) + JinaUI.Height(row.objective, 12, inner) + 8;
            height += 4;
        }
        JinaUI.HudPanel(new Rect(x, y, w, height));
        JinaUI.Text(new Rect(x + pad, y, inner - 22, header), "진행 중인 퀘스트 (" + rows.Count + ")",
            12, JinaUI.Gold, TextAnchor.MiddleLeft, true);
        JinaUI.Text(new Rect(x + pad, y, inner, header), expanded ? "▲" : "▼", 12, JinaUI.Faint, TextAnchor.MiddleRight);
        if (!expanded) return;
        y += header;
        foreach (var row in rows)
        {
            float titleHeight = JinaUI.Height(row.title, 12, inner);
            float goalHeight = JinaUI.Height(row.objective, 12, inner);
            JinaUI.Text(new Rect(x + pad, y, inner, titleHeight), row.title, 12, JinaUI.Gold);
            y += titleHeight;
            JinaUI.Text(new Rect(x + pad, y, inner, goalHeight), row.objective, 12, JinaUI.Bright);
            y += goalHeight + 8;
        }
    }

    void ResetQuests()
    {
        expanded = true;
        graveKillsAtAccept = roadAliveAtAccept = 0;
        foreach (var q in quests.Values) q.state = State.Available;
    }

    void OnDestroy()
    {
        if (combat != null) combat.Restarted -= ResetQuests;
        foreach (var q in quests.Values)
            if (q.marker != null) Destroy(q.marker.gameObject);
        if (markerSprite != null) Destroy(markerSprite);
    }
}
