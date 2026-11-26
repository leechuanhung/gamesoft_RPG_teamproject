using System;
using System.Collections.Generic;
using UnityEngine;

// 추가 퀘스트의 상태, 공통 표시와 진행 HUD를 담당한다.
// 기존 도깨비불 퀘스트는 RoadsideLore의 상태를 읽기만 한다.
[DefaultExecutionOrder(-120)]
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


    // 초가의 의뢰: 수락 → 구슬 획득 → 초가에 반납.
    const string OrbQuest = "ferry_orb";
    const float HutX = 113f, FerryHutX = 70f, HutRange = .75f;
    const int OrbHonor = -20;
    const string OrbOffer = "왼편 나루터에 뱃사공의 오두막이 있소. 그 집에 숨겨 둔 구슬 하나를 훔쳐다 주시오. 값은 톡톡히 치르리다.";
    bool hutOpen, hutAsking, hutClosing, ferrymanTold;
    string[] hutLines;
    int hutPage;
    public bool IsDialogOpen => hutOpen;
    bool HasOrb => JinaItems.Inventory != null && JinaItems.Inventory.GetQuantity(JinaItems.SuspiciousOrb) > 0;
    string OrbObjective() => HasOrb ? "콩밭 마을 초가로 구슬 가져가기" : "나루터 뱃사공의 오두막에서 구슬 가져오기";

    void Start()
    {
        combat.Restarted += ResetQuests;
        combat.SideQuests = this;
        child = Array.Find(HanyangTown.People, p => p.who == HanyangTown.Who.Ai);
        Register(ChildQuest, "아이의 부탁", ChildObjective,
            () => new Vector3(child.x, WorldBuilder.Top(child.x) + 1.85f, 0),
            () => combat.Quest.progress.Honor >= 0);
        Register(OrbQuest, "수상한 의뢰", OrbObjective,
            () => new Vector3(HutX, WorldBuilder.Top(HutX) + 1.85f, 0));
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
                "엄마가 저 먹으라고 주신 약과인데, 선비님 드릴래요. 꼭 받아 주세요!" };
        }
        return original;
    }

    public bool CanOfferChild => CanAccept(ChildQuest);

    // 도깨비불과 같은 별도 퀘스트 수락 창. 선택 결과는 한양 NPC에 전달한다.
    public void DrawChildOffer(Action<bool> answer)
    {
        DrawOffer("아이의 부탁", "부탁을 들어주시겠습니까?", ChildOffer, answer);
    }

    void DrawOffer(string title, string hint, string text, Action<bool> answer)
    {
        float width = Mathf.Min(720, JinaUI.W - 32);
        float height = 240;
        var box = new Rect((JinaUI.W - width) / 2, JinaUI.H - height - 24, width, height);
        var inside = JinaUI.Window(box, title, hint);
        JinaUI.Text(new Rect(inside.x, inside.y, inside.width, 108), text, 18, JinaUI.TextDark);
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

    // 기존 배경 오두막을 조사 지점으로 사용한다. 다른 NPC나 상점은 만들지 않는다.
    bool NearHut(float x) => Mathf.Abs(combat.player.transform.position.x - x) < HutRange
        && Mathf.Abs(combat.player.transform.position.y - WorldBuilder.Top(x)) < 2.1f;

    bool AtClient => NearHut(HutX) && (CanAccept(OrbQuest) || IsActive(OrbQuest));
    bool AtOrb => NearHut(FerryHutX) && IsActive(OrbQuest) && !HasOrb;

    void Update()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        if (hutOpen)
        {
            if (hutClosing) return;
            if (hutAsking)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) AnswerOrb(true);
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Escape)) AnswerOrb(false);
            }
            else if (Input.GetKeyDown(KeyCode.Escape)) hutClosing = true;
            else if (JinaUI.Interact && ++hutPage >= hutLines.Length) hutClosing = true;
            return;
        }
        if (combat.Paused || !JinaUI.Interact) return;
        if (combat.KongBoss != null && combat.KongBoss.Fighting) return;
        if (AtOrb)
        {
            if (JinaItems.Inventory == null || !JinaItems.Inventory.TryAdd(JinaItems.SuspiciousOrb)) return;
            combat.Quest.progress.CheatHonor(OrbHonor);
            combat.PreserveQuestHonor(OrbHonor);
            OpenHut("오두막 안쪽에 놓인 구슬을 챙겼다.");
        }
        else if (AtClient)
        {
            if (CanAccept(OrbQuest)) { OpenHut(); hutAsking = true; }
            else if (!HasOrb) OpenHut("나루터의 오두막이라 했소. 구슬을 가져오면 약속대로 값을 치르리다.");
            else ReturnOrb();
        }
    }

    void OpenHut(params string[] say)
    {
        hutOpen = true; hutAsking = hutClosing = false;
        hutLines = say; hutPage = 0;
        combat.player.CanMove = false;
    }

    void AnswerOrb(bool accept)
    {
        if (!hutAsking) return;
        if (accept && TryAccept(OrbQuest))
            OpenHut("흐흐, 말이 통하는 선비로군. 기다리고 있겠소.");
        else OpenHut("마음이 바뀌거든 다시 오시오.");
    }

    void ReturnOrb()
    {
        var bag = JinaItems.Inventory;
        var ui = bag != null ? bag.GetComponent<InventoryUI>() : null;
        if (ui == null || (!ui.HasWeapon("saingeom") && !ui.GiveWeapon("saingeom")))
        {
            OpenHut("칼을 담을 자리를 마련하고 다시 오시오.");
            return;
        }
        if (!bag.TryRemove(JinaItems.SuspiciousOrb)) return;
        Complete(OrbQuest);
        OpenHut("허허, 드디어 돌아왔군. 예전에 내가 죄를 지어 이 구슬을 빼앗겼소.",
            "신비로운 검을 얻어 되찾으려 했으나, 내 마음이 삿되어 그 검이 나를 받아들이지 않더군.",
            "이제 이 사인검은 선비에게 주겠소. 부디 나처럼 그 칼조차 쥘 수 없는 몸이 되지는 마시오. 흐흐.");
    }

    // 뱃사공의 기존 대화 앞에 한 번만 덧붙인다. 대화 여부는 반납 조건이 아니다.
    public string[] OrbFerryLines(string[] original)
    {
        if (!IsActive(OrbQuest) || !HasOrb || ferrymanTold) return original;
        ferrymanTold = true;
        var result = new List<string> {
            "내 오두막의 구슬을 가져갔구려. 내가 맡아 보관하던 물건이오.",
            "그대가 가져가는 것도 하늘의 뜻이겠지. 굳이 막지는 않겠소. 마음대로 하시오."
        };
        result.AddRange(original);
        return result.ToArray();
    }

    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0) return;
        KoreanUI.Apply();
        if (hutOpen)
        {
            if (hutClosing) return;
            if (hutAsking) { DrawOffer("수상한 의뢰", "부탁을 들어주시겠습니까?", OrbOffer, AnswerOrb); return; }
            float width = Mathf.Min(720, JinaUI.W - 32), height = 180;
            var inside = JinaUI.Window(new Rect((JinaUI.W - width) / 2, JinaUI.H - height - 24, width, height),
                NearHut(FerryHutX) ? "뱃사공의 오두막" : "수상한 사람", "Enter: 다음 · Esc: 닫기");
            JinaUI.Text(new Rect(inside.x, inside.y, inside.width, 108), hutLines[hutPage], 18, JinaUI.TextDark);
            return;
        }
        if (combat.Paused || (combat.KongBoss != null && combat.KongBoss.Fighting)) return;
        if (AtClient) JinaUI.Prompt("Enter: 초가집의 수상한 사람과 이야기하기");
        else if (AtOrb) JinaUI.Prompt("Enter: 뱃사공의 오두막 조사하기");
    }

    // 기존 도깨비불 상태만 참조한다. 동행 및 흡수 로직은 변경하지 않는다.
    bool WispActive => combat.Lore != null && combat.Lore.WispFollowing
        && combat.Quest.progress.Outcome == EncounterOutcome.Unresolved
        && (combat.Encounter == null || !combat.Encounter.WispWeakened);

    // 신규 퀘스트 모두 같은 이미지와 움직임을 사용한다.
    void LateUpdate()
    {
        // 같은 Enter 입력이 다른 NPC에게 전달되지 않도록 프레임 끝에 닫는다.
        if (hutClosing)
        {
            hutOpen = hutClosing = false;
            combat.player.CanMove = combat.PlayerHealth > 0;
        }
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
        hutOpen = hutAsking = hutClosing = ferrymanTold = false;
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
