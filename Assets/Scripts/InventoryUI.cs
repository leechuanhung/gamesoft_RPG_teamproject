
using UnityEngine;

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

    public bool IsOpen => isOpen;

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

        if (!Input.GetKeyDown(KeyCode.I))
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

        float width = Mathf.Min(430f, Screen.width - 20f);
        float height = Mathf.Min(430f, Screen.height - 20f);

        Rect panel = new Rect(
            (Screen.width - width) / 2f,
            (Screen.height - height) / 2f,
            width,
            height
        );

        GUI.Box(panel, "인벤토리");

        float x = panel.x + 20;
        float y = panel.y + 35;
        float contentWidth = width - 40;

        GUI.Label(
            new Rect(x, y, contentWidth, 25),
            "I키: 닫기 | 슬롯: " +
            inventory.Slots.Count + "/" + inventory.MaxSlots
        );

        y += 35;

        // 현재 보유 중인 아이템 표시
        if (inventory.Slots.Count == 0)
        {
            GUI.Label(
                new Rect(x, y, contentWidth, 30),
                "보유한 아이템이 없습니다."
            );
        }
        else
        {
            int count = Mathf.Min(inventory.Slots.Count, 8);

            for (int i = 0; i < count; i++)
            {
                InventorySlot slot = inventory.Slots[i];

                GUI.Label(
                    new Rect(x, y + i * 27, contentWidth, 25),
                    (i + 1) + ". " +
                    slot.Item.ItemName +
                    " x " + slot.Quantity
                );
            }

            if (inventory.Slots.Count > count)
            {
                GUI.Label(
                    new Rect(x, y + count * 27, contentWidth, 25),
                    "나머지 슬롯은 추후 UI에서 표시"
                );
            }
        }

        // 테스트 버튼
        float buttonY = panel.yMax - 95;

        GUI.enabled = testItem != null;

        if (GUI.Button(
            new Rect(x, buttonY, contentWidth, 30),
            "테스트: 회복약 1개 추가"
        ))
        {
            if (!inventory.TryAdd(testItem))
                Debug.Log("인벤토리에 공간이 없습니다.");
        }

        if (GUI.Button(
            new Rect(x, buttonY + 35, contentWidth, 30),
            "테스트: 회복약 1개 제거"
        ))
        {
            inventory.TryRemove(testItem);
        }

        GUI.enabled = true;
    }
}
