using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PrototypeQuest : MonoBehaviour
{
    public PrototypeCombat combat;
    public readonly QuestProgress progress = new QuestProgress();
    public bool IsDialogOpen { get; private set; }
    private bool shrineDialog;
    private string message = "";
    private SpriteRenderer villager, shrine;
    private PrototypePlayer Player => combat.player;

    void Start()
    {
        villager = Marker("Village Elder", PrototypeWorld.Elder, new Vector2(.75f, 1.4f), new Color(.35f,.75f,1f));
        JinaActorVisual.Attach(villager, JinaArt.Elder, combat);
        shrine = Marker("Purification Shrine", PrototypeWorld.Stone, new Vector2(1.1f, 2), new Color(.65f,.4f,.85f));
    }
    SpriteRenderer Marker(string label, Vector2 position, Vector2 scale, Color color)
    {
        var obj = new GameObject(label);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        var art = obj.AddComponent<SpriteRenderer>();
        art.sprite = Player.GetComponent<SpriteRenderer>().sprite;
        art.color = color;
        return art;
    }
    bool Near(Vector2 position) => Vector2.Distance(Player.transform.position, position) < 1.8f;

    public void ResetQuest()
    {
        progress.Reset();
        IsDialogOpen = false;
        message = "";
        if (Player != null) Player.CanMove = true;
        if (villager != null)
        {
            villager.transform.position = PrototypeWorld.Elder;
            villager.transform.localScale = new Vector3(.75f, 1.4f, 1);
            villager.color = new Color(.35f,.75f,1f);
        }
    }
    public void ShowElderDeath()
    {
        if (villager == null) return;
        villager.color = Color.gray;
        villager.transform.localScale = new Vector3(1.4f, .35f, 1);
        villager.transform.position = new Vector3(PrototypeWorld.Elder.x, -.3f, 0);
    }
    void Close()
    {
        IsDialogOpen = false;
        Player.CanMove = combat.PlayerHealth > 0;
    }
    void Update()
    {
        if (combat != null && combat.Tavern != null && combat.Tavern.IsOpen) return;
        if (combat == null || Player == null || combat.PlayerHealth <= 0) return;
        if (IsDialogOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)) { Close(); return; }
            if (shrineDialog && progress.HeardClue && !progress.HasCharm && progress.Outcome == EncounterOutcome.Unresolved)
            {
                int symbol = Input.GetKeyDown(KeyCode.Alpha1) ? 1 : Input.GetKeyDown(KeyCode.Alpha2) ? 2 : Input.GetKeyDown(KeyCode.Alpha3) ? 3 : 0;
                if (symbol != 0)
                {
                    bool correct = progress.ChooseSymbol(symbol);
                    message = progress.HasCharm ? "비석의 빛이 잦아들고 부적 한 장이 드러났다.\n[정화 부적 획득] 창을 닫고 도깨비 가까이에서 F를 누르세요."
                        : correct ? "문양에 희미한 빛이 깃들었다. 다음 문양을 고르세요." : "빛이 사그라들었다. 첫 문양부터 다시 고르세요.";
                }
            }
            return;
        }
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (Near(PrototypeWorld.Elder))
        {
            shrineDialog = false;
            progress.HearClue();
            message = progress.ElderDead
                ? "노인은 마지막 힘을 다해 도깨비에게 맞섰으나 끝내 쓰러졌다.\n품에 남은 쪽지에는 ‘달, 강, 산’이라 적혀 있다. 비석으로 가려면 서쪽의 몬스터를 모두 물리쳐야 한다."
                : progress.Outcome == EncounterOutcome.Purified
                ? "그 도깨비를 살려 주셨구려. 마을의 근심도 덜었으니 참으로 고맙소. 부디 남은 길도 평안히 가시오."
                : progress.Outcome == EncounterOutcome.Slain
                ? "도깨비를 물리치셨구려. 이제 마을 사람들도 한숨 놓겠소. 다만 어찌 그리 사나워졌는지, 그 사연은 끝내 알 길이 없구려."
                : progress.HasCharm
                ? "부적을 손에 넣으셨구려. 이제 저 도깨비의 저주를 풀어 줄 수도 있겠소.\n허나 칼을 들어 물리칠지, 부적으로 살려 줄지는 선비께서 정할 일이오. 부디 깊이 헤아려 뜻을 정하시오."
                : "서쪽 길의 요괴들을 모두 물리치면 비석 터의 봉인이 풀릴 것이오. 비석에서 달, 강, 산의 차례로 문양을 짚어 보시오.\n부적이면 동쪽 도깨비의 저주를 풀 수 있소. 허나 도깨비를 마을로 끌어들이지는 마시오. 주민들이 다칠까 두렵소.";
        }
        else if (Near(PrototypeWorld.Stone))
        {
            if (combat.Camp != null && combat.Camp.Remaining > 0) return;
            shrineDialog = true;
            message = progress.Outcome != EncounterOutcome.Unresolved ? "이곳의 일은 이미 끝났다.\n다른 선택을 해 보려면 R을 눌러 처음부터 시작하세요."
                : progress.HasCharm ? "정화 부적을 지니고 있다.\n창을 닫고 도깨비 가까이에서 F를 누르세요."
                : !progress.HeardClue ? "비석에 낯선 문양이 새겨져 있다.\n먼저 파란 옷을 입은 마을 노인에게 이야기를 들어 보세요."
                : "달과 강과 산의 문양이 새겨져 있다.\n노인에게 들은 차례대로 문양을 고르세요.";
        }
        else return;
        IsDialogOpen = true;
        Player.CanMove = false;
    }
    void OnGUI()
    {
        if (combat == null || Player == null) return;
        PrototypeKoreanUI.Apply();
        GUI.Box(new Rect(15, 185, 440, 85), "사건: " + PrototypeKoreanUI.Outcome(progress.Outcome));
        GUI.Label(new Rect(30, 211, 410, 50), "E: 대화·조사    F: 가까운 도깨비 정화\n부적: " + (progress.HasCharm ? "있음" : "없음") + "    단서: " + (progress.HeardClue ? "확인함" : "마을 노인에게 묻기"));
        if (!IsDialogOpen && combat.PlayerHealth > 0 && (combat.Tavern == null || (!combat.Tavern.IsOpen && !combat.Tavern.Near)))
        {
            string hint = Near(PrototypeWorld.Elder) ? (progress.ElderDead ? "E: 노인이 남긴 쪽지 살펴보기" : "E: 마을 노인과 대화") : Near(PrototypeWorld.Stone) ? "E: 비석 살펴보기" : "";
            if (hint == "" && progress.Outcome == EncounterOutcome.Unresolved)
                hint = Player.transform.position.x < PrototypeWorld.ForestStart ? "서쪽 몬스터를 모두 물리치면 비석 길이 열립니다." : "J: 공격    ·    부적이 있다면 가까이에서 F: 정화";
            GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height - 55, 440, 35), hint);
        }
        if (!IsDialogOpen) return;
        float width = Mathf.Min(760, Screen.width - 30);
        var box = new Rect((Screen.width - width) / 2, Screen.height - 255, width, 235);
        GUI.Box(box, shrineDialog ? "오래된 비석" : progress.ElderDead ? "노인이 남긴 흔적" : "마을 노인");
        var style = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 16 };
        GUI.Label(new Rect(box.x + 15, box.y + 30, width - 30, 130), message, style);
        if (shrineDialog && progress.HeardClue)
            GUI.Label(new Rect(box.x + 15, box.y + 167, width - 30, 30), "1: 강    2: 달    3: 산     맞춘 문양: " + progress.PuzzleStep + "/3");
        GUI.Label(new Rect(box.x + 15, box.y + 202, width - 30, 25), "E / Esc: 닫기    ·    대화 중에는 전투가 멈춥니다.");
    }
    void OnDestroy()
    {
        if (villager != null) Destroy(villager.gameObject);
        if (shrine != null) Destroy(shrine.gameObject);
    }
}
