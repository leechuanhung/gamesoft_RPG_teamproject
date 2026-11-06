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

        // 오른쪽 위, 선악 창 아래. 화면이 낮으면 위로 올린다.
        const float cell = 54, gap = 6, pad = 14, titleHeight = 32;
        int rows = Mathf.CeilToInt(inventory.MaxSlots / (float)Columns);
        float gridWidth = Columns * cell + (Columns - 1) * gap, gridHeight = rows * cell + (rows - 1) * gap;
        float width = gridWidth + pad * 2, height = titleHeight + pad + gridHeight + 44;
        var window = new Rect(Screen.width - width - 15, Mathf.Min(150, Mathf.Max(10, Screen.height - height - 10)), width, height);

        Frame(window, Back, Edge, 2);
        Fill(new Rect(window.x + 2, window.y + 2, window.width - 4, titleHeight - 2), Title);
        Text(new Rect(window.x + 12, window.y + 4, window.width - 24, titleHeight - 6), "보따리", 18, Gold, TextAnchor.MiddleLeft, true);
        Text(new Rect(window.x + 12, window.y + 4, window.width - 24, titleHeight - 6), "I / Esc 닫기", 13, Ink, TextAnchor.MiddleRight);

        // 아이템 칸: 아이콘과 개수. 누르면 고르고, 고른 칸을 다시 누르면 쓴다. 마우스를 올리면 그 칸의 설명이 뜬다.
        float x = window.x + pad, y = window.y + titleHeight + pad;
        int hovered = -1;
        for (int i = 0; i < inventory.MaxSlots; i++)
        {
            var r = new Rect(x + (i % Columns) * (cell + gap), y + (i / Columns) * (cell + gap), cell, cell);
            bool filled = i < inventory.Slots.Count, over = r.Contains(Event.current.mousePosition);
            if (over && filled) hovered = i;
            Frame(r, over ? Hover : SlotBack, filled && i == selected ? Gold : SlotEdge, filled && i == selected ? 2 : 1);
            if (GUI.Button(r, "", GUIStyle.none) && filled)
            {
                if (i == selected) UseSelected();
                else { selected = i; message = ""; }
            }
            if (!filled) continue;

            var slot = inventory.Slots[i];
            if (slot.Item.Icon != null) DrawIcon(slot.Item.Icon, new Rect(r.x + 7, r.y + 5, cell - 14, cell - 14));
            else Text(new Rect(r.x + 3, r.y + 3, cell - 6, cell - 6), slot.Item.ItemName, 12, Ink, TextAnchor.MiddleCenter);
            if (slot.Quantity > 1) Text(new Rect(r.x + 3, r.y, cell - 6, cell - 2), slot.Quantity.ToString(), 14, Ink, TextAnchor.LowerRight, true);
        }

        // 아래 줄: 가진 엽전
        int coins = combat != null && combat.Tavern != null ? combat.Tavern.wallet.Coins : 0;
        var bottom = new Rect(x, window.yMax - 36, gridWidth, 26);
        Frame(bottom, SlotBack, SlotEdge, 1);
        Text(new Rect(bottom.x + 10, bottom.y, bottom.width - 20, bottom.height), "엽전", 15, Ink, TextAnchor.MiddleLeft);
        Text(new Rect(bottom.x + 10, bottom.y, bottom.width - 20, bottom.height), coins + " 문", 15, Gold, TextAnchor.MiddleRight, true);

        DrawDetails(window, hovered >= 0 ? hovered : selected);

        // 에디터에서만 보이는 시험용 버튼(팀원 테스트 아이템)
        if (Application.isEditor && testItem != null && GUI.Button(new Rect(window.x, window.yMax + 4, 120, 22), "테스트 아이템 +1"))
            inventory.TryAdd(testItem);
    }

    // 창 왼쪽의 설명 창: 큰 아이콘, 이름, 설명, 효과(마지막 줄), 사용 버튼, 방금 쓴 결과.
    void DrawDetails(Rect window, int index)
    {
        var box = new Rect(window.x - 290, window.y, 280, 250);
        Frame(box, Back, Edge, 2);
        float x = box.x + 14, w = box.width - 28;
        if (index >= inventory.Slots.Count)
        {
            Text(new Rect(x, box.y + 14, w, 80), "보따리가 비었습니다.\n주막 옆 보부상에게서 길에 요긴한 것들을 살 수 있습니다.", 15, Ink);
            Text(new Rect(x, box.yMax - 30, w, 22), message, 14, Note);
            return;
        }
        var item = inventory.Slots[index].Item;
        var iconBox = new Rect(x, box.y + 14, 56, 56);
        Frame(iconBox, SlotBack, SlotEdge, 1);
        if (item.Icon != null) DrawIcon(item.Icon, new Rect(iconBox.x + 6, iconBox.y + 6, 44, 44));
        Text(new Rect(x + 68, box.y + 14, w - 68, 26), item.ItemName, 18, Gold, TextAnchor.MiddleLeft, true);
        Text(new Rect(x + 68, box.y + 42, w - 68, 22), "x" + inventory.Slots[index].Quantity, 14, Ink, TextAnchor.MiddleLeft);

        // 설명의 마지막 줄은 효과라서 따로 초록색으로
        string text = item.Description ?? "";
        int cut = text.LastIndexOf('\n');
        Text(new Rect(x, box.y + 80, w, 70), cut >= 0 ? text.Substring(0, cut) : text, 15, Ink);
        if (cut >= 0) Text(new Rect(x, box.y + 150, w, 24), text.Substring(cut + 1), 15, Effect, TextAnchor.UpperLeft, true);

        GUI.enabled = item is JinaItem;
        if (GUI.Button(new Rect(x, box.y + 180, 110, 30), "사용하기") && index < inventory.Slots.Count)
        {
            selected = index;
            UseSelected();
        }
        GUI.enabled = true;
        Text(new Rect(x + 120, box.y + 180, w - 120, 30), "스페이스 · 한 번 더 누르기", 12, Ink, TextAnchor.MiddleLeft);
        Text(new Rect(x, box.y + 214, w, 30), message, 14, Note);
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
