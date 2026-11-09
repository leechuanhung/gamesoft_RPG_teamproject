using UnityEngine;
using static JinaUI;   // 창 색과 그리기(공용)

[RequireComponent(typeof(InventoryManager))]
public class InventoryUI : MonoBehaviour
{
    [Header("Test Item")]
    [SerializeField] private ItemData testItem;

    private InventoryManager inventory;
    private PrototypeCombat combat;

    private bool isOpen;
    private bool previousCanMove;
    private float previousTimeScale;

    // 칸 배치(가로 5칸)와 고른 칸, 방금 쓴 결과
    public const int Columns = 5;
    private int selected;
    private string message = "";

    public bool IsOpen => isOpen;
    public int Selected => selected;

    void Awake()
    {
        inventory = GetComponent<InventoryManager>();
    }

    void Update()
    {
        // 전투 시스템이 나중에 생성되는 경우에도 연결
        if (combat == null)
        {
            ConnectCombat();
            return;
        }

        if (isOpen)
        {
            // 방향키·WASD로 칸을 고르고, 스페이스·엔터로 사용
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Select(selected + 1);
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Select(selected - 1);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Select(selected + Columns);
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Select(selected - Columns);
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) UseSelected();
        }

        if (!Input.GetKeyDown(KeyCode.I) && !(isOpen && Input.GetKeyDown(KeyCode.Escape)))
            return;

        if (isOpen)
        {
            CloseInventory();
        }
        else
        {
            // 기존 상점, 대화창, 이벤트와 중복 실행 방지
            if (combat.Paused || combat.PlayerHealth <= 0)
                return;

            OpenInventory();
        }
    }

    void Select(int index)
    {
        int count = inventory.Slots.Count;
        selected = count == 0 ? 0 : Mathf.Clamp(index, 0, count - 1);
    }

    // 고른 칸의 아이템을 쓴다. 다 쓰면 칸이 비고, 쓸 수 없으면 그대로 남는다.
    public void UseSelected()
    {
        Select(selected);
        if (combat == null || selected >= inventory.Slots.Count)
            return;

        var item = inventory.Slots[selected].Item as JinaItem;
        if (item == null)
        {
            message = "지금은 쓸 수 없는 물건입니다.";
            return;
        }

        if (item.Use(combat, out message))
            inventory.TryRemove(item);

        Select(selected);
    }

    void ConnectCombat()
    {
        var found = FindObjectOfType<PrototypeCombat>();

        if (found == null)
            return;

        combat = found;
        combat.Restarted += HandleRestart;
    }

    void OpenInventory()
    {
        if (combat == null || combat.player == null)
            return;

        previousCanMove = combat.player.CanMove;
        previousTimeScale = Time.timeScale;

        isOpen = true;
        message = "";
        Select(selected);
        combat.InventoryPauseActive = true;
        combat.player.CanMove = false;

        Time.timeScale = 0f;
    }

    void CloseInventory()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (combat != null)
        {
            combat.InventoryPauseActive = false;

            if (combat.player != null &&
                combat.PlayerHealth > 0)
            {
                combat.player.CanMove = previousCanMove;
            }
        }

        Time.timeScale = previousTimeScale;
    }

    void HandleRestart()
    {
        CloseInventory();
        inventory.Clear();
        selected = 0;
    }

    void OnDisable()
    {
        CloseInventory();
    }

    void OnDestroy()
    {
        if (combat != null)
            combat.Restarted -= HandleRestart;
    }

    void OnGUI()
    {
        if (!isOpen || inventory == null)
            return;

        GUI.depth = -100;
        PrototypeKoreanUI.Apply();

        // 오른쪽 위 구석. 열려 있는 동안 선악 창은 숨는다(PrototypeHud).
        const float cell = 54, gap = 6;
        int rows = Mathf.CeilToInt(inventory.MaxSlots / (float)Columns);
        float gridWidth = Columns * cell + (Columns - 1) * gap, gridHeight = rows * cell + (rows - 1) * gap;
        float width = gridWidth + WindowPad * 2, height = TitleHeight + 12 + gridHeight + 12 + 28 + WindowPad;
        var window = new Rect(Screen.width - width - 12, 12, width, height);
        var inside = Window(window, "보따리", "I / Esc 닫기");

        // 아이템 칸: 아이콘과 개수. 누르면 고르고, 고른 칸을 다시 누르면 쓴다. 마우스를 올리면 그 칸의 설명이 뜬다.
        int hovered = -1;
        for (int i = 0; i < inventory.MaxSlots; i++)
        {
            var r = new Rect(inside.x + (i % Columns) * (cell + gap), inside.y + (i / Columns) * (cell + gap), cell, cell);
            bool filled = i < inventory.Slots.Count, over = r.Contains(Event.current.mousePosition);
            if (over && filled) hovered = i;
            Slot(r, filled && i == selected, over && filled);
            if (GUI.Button(r, "", GUIStyle.none) && filled)
            {
                if (i == selected) UseSelected();
                else { selected = i; message = ""; }
            }
            if (!filled) continue;

            var slot = inventory.Slots[i];
            if (slot.Item.Icon != null) DrawIcon(slot.Item.Icon, new Rect(r.x + 7, r.y + 5, cell - 14, cell - 14));
            else Text(new Rect(r.x + 3, r.y + 3, cell - 6, cell - 6), slot.Item.ItemName, 12, TextDark, TextAnchor.MiddleCenter);
            if (slot.Quantity > 1)
            {
                string count = slot.Quantity.ToString();
                float tagWidth = Width(count, 13, true) + 8;
                var tag = new Rect(r.xMax - tagWidth - 2, r.yMax - 20, tagWidth, 18);
                Box(tag, Night, Ink, 0);
                Text(tag, count, 13, Bright, TextAnchor.MiddleCenter, true);
            }
        }

        // 아래 줄: 가진 엽전
        int coins = combat != null && combat.Tavern != null ? combat.Tavern.wallet.Coins : 0;
        var bottom = new Rect(inside.x, inside.y + gridHeight + 12, gridWidth, 28);
        Box(bottom, PaperDark, SlotEdge, 1);
        Text(new Rect(bottom.x + 12, bottom.y, bottom.width - 24, bottom.height), "엽전", 15, TextDark, TextAnchor.MiddleLeft);
        Text(new Rect(bottom.x + 12, bottom.y, bottom.width - 24, bottom.height), coins + "문", 15, Red, TextAnchor.MiddleRight, true);

        DrawDetails(window, hovered >= 0 ? hovered : selected);

        // 에디터에서만 보이는 시험용 버튼(팀원 테스트 아이템)
        if (Application.isEditor && testItem != null && Button(new Rect(window.x, window.yMax + 6, 170, 28), "테스트 아이템 +1"))
            inventory.TryAdd(testItem);
    }

    // 창 왼쪽의 설명 창. 글 높이를 재서 쌓으므로 설명이 길어도 잘리지 않는다.
    void DrawDetails(Rect window, int index)
    {
        const float boxWidth = 300, pad = 16, inner = boxWidth - pad * 2;
        float x = window.x - boxWidth - 8, top = window.y;
        if (index >= inventory.Slots.Count)
        {
            string empty = "보따리가 비었습니다.\n주막 옆 보부상에게서 길에 요긴한 것들을 살 수 있습니다.";
            float emptyHeight = Height(empty, 15, inner), said = Height(message, 14, inner);
            var plain = new Rect(x, top, boxWidth, pad * 2 + 6 + emptyHeight + (said > 0 ? said + 8 : 0));
            Box(plain, Paper, Wood, 3);
            Text(new Rect(x + pad, top + pad + 3, inner, emptyHeight), empty, 15, TextDark);
            Text(new Rect(x + pad, top + pad + 11 + emptyHeight, inner, said), message, 14, Red);
            return;
        }
        var item = inventory.Slots[index].Item;
        string description = item.Description ?? "", story = description, effect = null;
        int cut = description.LastIndexOf('\n');
        if (cut >= 0) { story = description.Substring(0, cut); effect = description.Substring(cut + 1); }
        const string hint = "스페이스 · 칸 한 번 더 누르기";
        float storyHeight = Height(story, 15, inner), effectHeight = Height(effect, 15, inner, true), messageHeight = Height(message, 14, inner);
        float hintHeight = Mathf.Max(34, Height(hint, 12, inner - 116));
        float total = pad + 3 + 56 + 12 + storyHeight + (effectHeight > 0 ? effectHeight + 8 : 0) + 12 + hintHeight + (messageHeight > 0 ? messageHeight + 10 : 0) + pad + 3;
        Box(new Rect(x, top, boxWidth, total), Paper, Wood, 3);

        float left = x + pad, y = top + pad + 3;
        var iconBox = new Rect(left, y, 56, 56);
        Box(iconBox, PaperDark, SlotEdge, 1);
        if (item.Icon != null) DrawIcon(item.Icon, new Rect(iconBox.x + 6, iconBox.y + 6, 44, 44));
        Text(new Rect(left + 68, y + 4, inner - 68, 26), item.ItemName, 18, TextDark, TextAnchor.MiddleLeft, true);
        Text(new Rect(left + 68, y + 32, inner - 68, 20), "가진 수 " + inventory.Slots[index].Quantity, 13, TextDim, TextAnchor.MiddleLeft);
        y += 56 + 12;
        Text(new Rect(left, y, inner, storyHeight), story, 15, TextDark);
        y += storyHeight + 8;
        if (effect != null) { Text(new Rect(left, y, inner, effectHeight), effect, 15, Green, TextAnchor.UpperLeft, true); y += effectHeight; }
        y += 12;
        if (Button(new Rect(left, y, 106, 34), "사용하기", item is JinaItem usable && usable.effect != JinaItem.Effect.Keepsake) && index < inventory.Slots.Count)
        {
            selected = index;
            UseSelected();
        }
        Text(new Rect(left + 116, y, inner - 116, hintHeight), hint, 12, TextDim, TextAnchor.MiddleLeft);
        y += hintHeight + 10;
        Text(new Rect(left, y, inner, messageHeight), message, 14, Red);
    }

    // 스프라이트 하나를 칸 안에 비율을 지켜 그린다(도트가 번지지 않게 텍스처는 Point 필터).
    public static void DrawIcon(Sprite icon, Rect area)
    {
        var t = icon.texture; var src = icon.textureRect;
        float scale = Mathf.Min(area.width / src.width, area.height / src.height);
        var size = new Vector2(src.width * scale, src.height * scale);
        var dst = new Rect(area.center.x - size.x / 2, area.center.y - size.y / 2, size.x, size.y);
        GUI.DrawTextureWithTexCoords(dst, t, new Rect(src.x / t.width, src.y / t.height, src.width / t.width, src.height / t.height));
    }
}
