using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PrototypeQuest : MonoBehaviour
{
    public PrototypeCombat combat;
    public readonly QuestProgress progress = new QuestProgress();
    public bool IsDialogOpen { get; private set; }
    public bool TalkingToElder => IsDialogOpen && !shrineDialog;
    public float ElderStruckAt { get; private set; } = -1;
    private bool shrineDialog;
    private string message = "";
    // The elder's talk, a page at a time (E: next); `message` is the page showing.
    private string[] pages;
    private int pageAt;
    private SpriteRenderer villager;
    private float charmReveal = -1;
    const float CharmRevealLength = 3.4f;
    public bool RevealingCharm => charmReveal >= 0;
    private PrototypePlayer Player => combat.player;

    void Start()
    {
        villager = Marker("Village Elder", PrototypeWorld.Elder, new Vector2(.75f, 1.4f), new Color(.35f,.75f,1f));
        JinaActorVisual.Attach(villager, JinaArt.Elder, combat);
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
        charmReveal = -1;
        message = "";
        ElderStruckAt = -1;
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
        ElderStruckAt = Time.time;
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
        if (combat != null && ((combat.Tavern != null && combat.Tavern.IsOpen) || (combat.Peddler != null && combat.Peddler.IsOpen))) return;
        if (combat == null || Player == null || combat.PlayerHealth <= 0) return;
        if (charmReveal >= 0)
        {
            // The dialog stays open (and combat paused) until the reveal finishes.
            charmReveal += Time.unscaledDeltaTime;
            if (charmReveal >= CharmRevealLength) charmReveal = -1;
            return;
        }
        if (IsDialogOpen)
        {
            if (Input.GetKeyDown(KeyCode.E) && !shrineDialog && !progress.ElderDead && JinaUI.FinishTyping(message)) return;
            if (Input.GetKeyDown(KeyCode.E) && pages != null && pageAt + 1 < pages.Length) { message = pages[++pageAt]; return; }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)) { Close(); return; }
            if (shrineDialog && progress.HeardClue && !progress.HasCharm && progress.Outcome == EncounterOutcome.Unresolved)
            {
                int symbol = Input.GetKeyDown(KeyCode.Alpha1) ? 1 : Input.GetKeyDown(KeyCode.Alpha2) ? 2 : Input.GetKeyDown(KeyCode.Alpha3) ? 3 : 0;
                if (symbol != 0)
                {
                    bool correct = progress.ChooseSymbol(symbol);
                    if (progress.HasCharm) charmReveal = 0;
                    message = progress.HasCharm ? "비석의 빛이 잦아들고 부적 한 장이 드러났다.\n[정화 부적 획득] 창을 닫고 도깨비 가까이에서 F를 누르세요."
                        : correct ? "문양에 희미한 빛이 깃들었다. 다음 문양을 고르세요." : "빛이 사그라들었다. 첫 문양부터 다시 고르세요.";
                }
            }
            return;
        }
        if (!Input.GetKeyDown(KeyCode.E) || (combat.BossTalk != null && combat.BossTalk.IsOpen)) return;
        if (Near(PrototypeWorld.Elder))
        {
            shrineDialog = false;
            progress.HearClue();
            pages = ElderPages(progress, combat.KongBoss != null && combat.KongBoss.Fighting,
                JinaItems.Inventory != null && JinaItems.Inventory.GetQuantity(JinaItems.Keepsake) > 0);
            pageAt = 0; message = pages[0];
        }
        else if (Near(PrototypeWorld.Stone))
        {
            if (combat.Camp != null && combat.Camp.Remaining > 0) return;
            shrineDialog = true;
            pages = null;
            message = progress.Outcome != EncounterOutcome.Unresolved ? "이곳의 일은 이미 끝났다.\n다른 선택을 해 보려면 R을 눌러 처음부터 시작하세요."
                : progress.HasCharm ? "정화 부적을 지니고 있다.\n창을 닫고 도깨비 가까이에서 F를 누르세요."
                : !progress.HeardClue ? "비석에 낯선 문양이 새겨져 있다.\n먼저 갈색 갓을 쓴 마을 노인에게 이야기를 들어 보세요."
                : "달과 강과 산의 문양이 새겨져 있다.\n노인에게 들은 차례대로 문양을 고르세요.";
        }
        else return;
        IsDialogOpen = true;
        Player.CanMove = false;
    }
    // What the elder has to say, as the story stands. Each entry is one speech bubble.
    // `fleeing`: he has come back across the river from the Kongjwi demon to rest and buy what he needs.
    public static string[] ElderPages(QuestProgress q, bool fleeing = false, bool keepsake = false)
    {
        if (fleeing && !q.ElderDead) return new[] {
            "자네, 얼굴이 사색일세. 강 건너에서 무슨 일이 있었나?",
            "독 속의 두꺼비라… 밑 빠진 독에 물을 붓던 처녀를 두꺼비가 도와준 옛이야기를 아는가? 이번엔 그 두꺼비가 처녀를 삼킨 게로군.",
            keepsake ? "그 비녀… 그 집 안주인의 것이로군. 원한은 원한으로 풀리지 않는 법이네. 그 아이를 베기 전에 그것부터 써 보게."
                : "그 아이를 칼로 베면 끝은 나겠지. 허나 강물이 품은 것이 있다면… 그것이 그 아이를 돌려줄지도 모르네.",
            "주모에게 요기라도 하고, 보부상에게 화살도 넉넉히 사 가게. 서두르게. 그 마을엔 피할 데가 없다네." };
        if (q.ElderDead) return new[] { "노인은 마지막 힘을 다해 도깨비에게 맞섰으나 끝내 쓰러졌다.\n품에 남은 쪽지에는 ‘달, 강, 산’이라 적혀 있다. 비석으로 가려면 서쪽의 몬스터를 모두 물리쳐야 한다." };
        if (q.Outcome == EncounterOutcome.Purified) return new[] {
            "그 도깨비를 살려 주셨구려. 어젯밤엔 숲에서 울음소리 대신 솔바람 소리가 내려오더이다.",
            "내 젊어서 저 아이와 씨름도 하고 술도 나눠 마셨다오. …이 늙은이 소원을 선비께서 들어주셨소. 부디 남은 길도 평안히 가시오." };
        if (q.Outcome == EncounterOutcome.Slain) return new[] {
            "도깨비를 물리치셨구려. 이제 마을 사람들도 한숨 놓겠소.",
            "다만… 어찌 그리 사나워졌는지, 그 사연은 끝내 알 길이 없구려. 내 젊은 날의 벗이었는데. 아니오, 선비 탓을 하는 게 아니오." };
        if (q.HasCharm) return new[] {
            "부적을 손에 넣으셨구려. 이제 저 도깨비의 저주를 풀어 줄 수도 있겠소.",
            "허나 칼을 들어 물리칠지, 부적으로 살려 줄지는 선비께서 정할 일이오. 살려 주면 더 위험하고, 베면 더 쉽지. 부디 깊이 헤아려 뜻을 정하시오." };
        return new[] {
            "한양 가는 길이시오? 그 길은 지금 막혔소. 동쪽 숲 끝에 가시덤불이 자라 나루터로 가는 길을 틀어막았다오.",
            "숲의 도깨비 짓이오. 본디 산을 지키던 착한 놈이었는데, 망한 나라의 원한이 씌어 저리 되었소.",
            "저 아이를 꼭 베어야만 한다면 어쩔 수 없겠지. 허나 풀어 줄 길도 있소. 서쪽 비석 터의 옛 비석에 달, 강, 산의 차례로 손을 얹으면 부적이 나온다 하오.",
            "비석 터로 가려면 들판의 망령들을 물리쳐야 하오. 그리고 부디, 도깨비를 마을로 끌어들이지는 마시오. 주민들이 다칠까 두렵소." };
    }

    void OnGUI()
    {
        if (combat == null || Player == null) return;
        PrototypeKoreanUI.Apply();
        if (!IsDialogOpen && combat.PlayerHealth > 0 && !combat.Paused)
        {
            if (Near(PrototypeWorld.Elder)) JinaUI.Prompt(progress.ElderDead ? "E: 노인이 남긴 쪽지 살펴보기" : "E: 마을 노인과 대화");
            else if (Near(PrototypeWorld.Stone)) JinaUI.Prompt("E: 비석 살펴보기");
        }
        if (!IsDialogOpen) return;
        if (!shrineDialog && !progress.ElderDead)
        {
            // The elder talks in a window with his portrait; the stone and his note stay as narration boxes.
            bool more = pages != null && pageAt + 1 < pages.Length;
            JinaUI.TalkWindow("마을 노인", "elder", message, more ? "E 다음 (" + (pageAt + 1) + "/" + pages.Length + ")   ·   Esc 닫기" : "E / Esc 닫기", false);
            return;
        }
        // The stone and the note: a paper window as tall as its words.
        float width = Mathf.Min(760, JinaUI.W - 30), inner = width - JinaUI.WindowPad * 2;
        float textHeight = JinaUI.Height(message, 17, inner);
        bool puzzle = shrineDialog && progress.HeardClue;
        float height = JinaUI.TitleHeight + 12 + textHeight + (puzzle ? 34 : 0) + 30 + JinaUI.WindowPad;
        var box = new Rect((JinaUI.W - width) / 2, JinaUI.H - height - 14, width, height);
        var inside = JinaUI.Window(box, shrineDialog ? "오래된 비석" : "노인이 남긴 흔적", "E / Esc 닫기");
        JinaUI.Text(new Rect(inside.x, inside.y, inner, textHeight), message, 17, JinaUI.TextDark);
        if (puzzle)
            JinaUI.Text(new Rect(inside.x, inside.y + textHeight + 8, inner, 24), "1: 강     2: 달     3: 산          맞춘 문양: " + progress.PuzzleStep + " / 3", 16, JinaUI.Red, TextAnchor.MiddleLeft, true);
        JinaUI.Text(new Rect(inside.x, box.yMax - JinaUI.WindowPad - 20, inner, 20), "대화 중에는 전투가 멈춥니다.", 13, JinaUI.TextDim);
        if (charmReveal >= 0) DrawCharmReveal(charmReveal);
    }

    // Veil, turning rays, the charm rising into place, rising sparks, then the caption.
    void DrawCharmReveal(float t)
    {
        var screen = new Rect(0, 0, JinaUI.W, JinaUI.H);
        float fadeOut = 1 - Mathf.Clamp01((t - (CharmRevealLength - .6f)) / .6f);
        GUI.color = new Color(0, 0, 0, .7f * Mathf.Clamp01(t / .3f) * fadeOut);
        GUI.DrawTexture(screen, Texture2D.whiteTexture);
        GUI.color = new Color(1, .97f, .85f, Mathf.Clamp01(1 - t / .35f) * .8f);
        GUI.DrawTexture(screen, Texture2D.whiteTexture);

        const float unit = 5;
        var center = new Vector2(JinaUI.W / 2f, JinaUI.H * .42f);
        var gold = new Color(1, .84f, .4f, 1);
        var matrix = GUI.matrix;
        for (int i = 0; i < 8; i++)
        {
            GUIUtility.RotateAroundPivot(t * 18 + i * 45, center * JinaUI.Scale);
            GUI.color = new Color(gold.r, gold.g, gold.b, .22f * Mathf.Clamp01(t / .5f) * fadeOut);
            GUI.DrawTexture(new Rect(center.x - unit, center.y - unit * 60, unit * 2, unit * 60), Texture2D.whiteTexture);
            GUI.matrix = matrix;
        }
        float pulse = .5f + .5f * Mathf.Sin(t * 6);
        for (int ring = 3; ring >= 1; ring--)
        {
            float half = unit * (14 + ring * 6 + pulse * 2);
            GUI.color = new Color(gold.r, gold.g, gold.b, .12f * fadeOut);
            GUI.DrawTexture(new Rect(center.x - half * .6f, center.y - half, half * 1.2f, half * 2), Texture2D.whiteTexture);
        }

        float rise = Mathf.Clamp01((t - .15f) / .7f);
        float scale = Mathf.Lerp(.2f, 1, 1 - Mathf.Pow(1 - rise, 3)) * (1 + .06f * Mathf.Sin(t * 3));
        var charm = JinaArt.Prop("charm").texture;
        float w = charm.width * unit * scale, h = charm.height * unit * scale;
        float bob = Mathf.Sin(t * 2.5f) * unit;
        GUI.color = new Color(1, 1, 1, Mathf.Clamp01(rise * 3) * fadeOut);
        GUI.DrawTexture(new Rect(center.x - w / 2, center.y - h / 2 + (1 - rise) * unit * 20 + bob, w, h), charm);

        for (int i = 0; i < 14; i++)
        {
            float life = (t * .6f + i * .137f) % 1;
            float x = center.x + Mathf.Sin(i * 2.3f) * unit * 22, y = center.y + unit * 24 - life * unit * 50;
            GUI.color = new Color(1, .9f, .55f, (1 - life) * Mathf.Clamp01(t / .5f) * fadeOut);
            GUI.DrawTexture(new Rect(x, y, unit, unit), Texture2D.whiteTexture);
        }

        float caption = Mathf.Clamp01((t - 1f) / .5f) * fadeOut;
        GUI.color = Color.white;
        float captionY = center.y + charm.height * unit / 2 + unit * 6;
        JinaUI.Text(new Rect(0, captionY, JinaUI.W, unit * 10), "정화 부적을 얻었다", 35, new Color(1, .88f, .55f, caption), TextAnchor.MiddleCenter, true);
        JinaUI.Text(new Rect(0, captionY + unit * 10, JinaUI.W, unit * 7), "도깨비 가까이에서 F를 누르면 저주를 풀 수 있다.", 20, new Color(.95f, .92f, .85f, caption), TextAnchor.MiddleCenter);
    }
    void OnDestroy()
    {
        if (villager != null) Destroy(villager.gameObject);
    }
}
