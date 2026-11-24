using System.Collections.Generic;
using UnityEngine;
using static JinaUI;

// 기존 보따리를 4개 인덱스 탭으로 확장한다. 기존 InventorySystem 오브젝트에서 그대로 사용.
[RequireComponent(typeof(InventoryManager))]
public class InventoryUI : MonoBehaviour
{
    [Header("Test Item")]
    [SerializeField] private ItemData testItem;

    public enum PageTab { Items, KeyItems, Skills, Weapons }
    private PageTab tab;
    private InventoryManager inventory;
    private Combat combat;
    private bool isOpen, previousCanMove;
    private float previousTimeScale;
    public const int Columns = 5, PageSize = 20, PageCount = 3;
    private int selected, itemPage, collectionPage;
    private int selectedWeapon;
    private string message = "";
    public bool IsOpen => isOpen;
    public int Selected => selected;

    // 스킬 획득/장착은 보관함이 관리한다. Skills는 스킬 사용/쿨타임/UI에 집중한다.
    public const int MaxOwnedSkills = 20;
    public class SkillEntry
    {
        public string id, name, description;
        public float cooldown;
        public bool implemented;
    }
    private readonly List<SkillEntry> ownedSkills = new List<SkillEntry>();
    private readonly Dictionary<string, float> skillReadyAt = new Dictionary<string, float>();
    public IReadOnlyList<SkillEntry> OwnedSkills => ownedSkills;
    public bool UnlockSkill(string id, string name, string description, float cooldown, bool implemented = false)
    {
        if (string.IsNullOrEmpty(id) || ownedSkills.Count >= MaxOwnedSkills || ownedSkills.Exists(s => s.id == id)) return false;
        ownedSkills.Add(new SkillEntry { id = id, name = name, description = description,
            cooldown = Mathf.Max(.01f, cooldown), implemented = implemented });
        return true;
    }

    public string EquippedSkillId(int index)
    {
        var slots = combat != null && combat.Skills != null ? combat.Skills.slots : null;
        return slots != null && index >= 0 && index < slots.Length ? slots[index].skillId : null;
    }
    public string EquippedSkillName(int index)
    {
        string id = EquippedSkillId(index);
        var skill = ownedSkills.Find(s => s.id == id);
        return skill != null ? skill.name : "없음";
    }
    public bool EquipSkill(string id, int index)
    {
        var slots = combat != null && combat.Skills != null ? combat.Skills.slots : null;
        if (slots == null || index < 0 || index >= slots.Length) return false;
        var skill = ownedSkills.Find(s => s.id == id);
        if (skill == null || !skill.implemented) return false;
        // 각 스킬 쿨타임은 Q/W/E 위치를 바꿔도 계속 유지한다.
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (!string.IsNullOrEmpty(slot.skillId)) skillReadyAt[slot.skillId] = slot.readyAt;
        }
        for (int i = 0; i < slots.Length; i++)
        {
            if (i == index || slots[i].skillId != id) continue;
            slots[i].skillId = null;
            slots[i].name = "";
            slots[i].empty = true;
            slots[i].readyAt = 0;
        }
        var target = slots[index];
        target.skillId = skill.id;
        target.name = skill.name;
        target.cooldown = skill.cooldown;
        target.readyAt = skillReadyAt.TryGetValue(id, out float ready) ? ready : 0;
        target.empty = false;
        return true;
    }

    // 장착된 스킬을 Q/W/E에서 비운다. 쿨타임은 보관해 다시 장착해도 유지한다.
    public bool UnequipSkill(int index)
    {
        var slots = combat != null && combat.Skills != null ? combat.Skills.slots : null;
        if (slots == null || index < 0 || index >= slots.Length || slots[index].empty) return false;
        var slot = slots[index];
        if (!string.IsNullOrEmpty(slot.skillId)) skillReadyAt[slot.skillId] = slot.readyAt;
        slot.skillId = null;
        slot.name = "";
        slot.readyAt = 0;
        slot.empty = true;
        return true;
    }

    // 후속 무기 제작 때 UnlockWeapon으로 등록한다. 실제 공격 연동은 전투 쪽 구현이 필요.
    public class WeaponEntry
    {
        public string id, name, description, icon;
        public bool combatReady;
    }
    private readonly List<WeaponEntry> weapons = new List<WeaponEntry>();
    static readonly string[] defaultWeapons = { "dagger", "bow" };
    private string[] equippedWeapons => combat != null ? combat.EquippedWeapons : defaultWeapons;
    public IReadOnlyList<WeaponEntry> OwnedWeapons => weapons;
    public string EquippedWeapon(int index) => index >= 0 && index < 2 ? equippedWeapons[index] : null;

    public bool UnlockWeapon(string id, string name, string description, string icon = "icon_sword", bool combatReady = false)
    {
        if (string.IsNullOrEmpty(id) || weapons.Count >= 20 || weapons.Exists(w => w.id == id)) return false;
        weapons.Add(new WeaponEntry { id = id, name = name, description = description, icon = icon, combatReady = combatReady });
        return true;
    }

    public bool HasWeapon(string id) => weapons.Exists(w => w.id == id);

    // 땅에서 주운 무기를 보따리에 넣고, 빈 무기 칸이 있으면 바로 장착
    public bool GiveWeapon(string id)
    {
        bool added = id == "hwando" ? UnlockWeapon("hwando", "환도", "기본 근접 무기", "icon_sword", true)
                   : id == "bow" ? UnlockWeapon("bow", "활", "화살을 소비하는 원거리 무기", "icon_bow", true)
                   : id == "myeonggeom" ? UnlockWeapon("myeonggeom", "명검", "환도보다 강하고 긴 전설의 검 (3연격)", "icon_sword", true)
                   : id == "saingeom" ? UnlockWeapon("saingeom", "사인검", "사악한 것을 베는 검. 환도보다 조금 강하다 (악행 20 이상이면 장착 불가)", "icon_sword", true)
                   : id == "byeokjogung" ? UnlockWeapon("byeokjogung", "벽조궁", "번개 맞은 대추나무로 만든 활 (활보다 데미지 50% 높음)", "icon_bow", true)
                   : false;
        if (!added) return false;
        for (int i = 0; i < 2; i++) if (equippedWeapons[i] == null) { EquipWeapon(id, i); break; }
        return true;
    }

    public bool EquipWeapon(string id, int index)
    {
        if (index < 0 || index >= 2) return false;
        var weapon = weapons.Find(w => w.id == id);
        if (weapon == null || !weapon.combatReady) return false;
        if (combat != null && !combat.CanEquip(Weapon.Get(id))) return false;   // 장착 조건 (사인검: 악행)
        for (int i = 0; i < 2; i++) if (i != index && equippedWeapons[i] == id) equippedWeapons[i] = null;
        equippedWeapons[index] = id;
        return true;
    }

    // 이미 장착된 칸을 다시 누르면 해제하되, 최소 한 개의 무기는 유지한다.
    public bool UnequipWeapon(int index)
    {
        if (index < 0 || index >= 2 || equippedWeapons[index] == null) return false;
        if (equippedWeapons[1 - index] == null) return false;
        equippedWeapons[index] = null;
        return true;
    }

    void Awake()
    {
        inventory = GetComponent<InventoryManager>();
        UnlockSkill("retreat", "물러서기", "바라보는 방향 반대로 이동하며 잠시 무적", Skills.RetreatCooldown, true);
        UnlockSkill("rush_slash", "돌진베기", "전방으로 돌진하며 적에게 피해", Skills.RushCooldown, true);
        UnlockSkill("power_shot", "강궁", "다음 화살 한 발의 공격력이 2배 (활을 들었을 때만)", Skills.PowerShotCooldown, true);
        // 처음엔 단검과 활. 나머지 무기는 들판 몹이 떨어뜨림 (WeaponPickup.cs)
        UnlockWeapon("dagger", "단검", "짧고 빠른 근접 무기 (콤보 없음)", "icon_sword", true);
        UnlockWeapon("bow", "활", "화살을 소비하는 원거리 무기", "icon_bow", true);
    }

    void Update()
    {
        if (combat == null) { ConnectCombat(); return; }
        if (isOpen)
        {
            // 탭은 마우스로, 일반 아이템 슬롯은 방향키로 조작한다.
            if (tab == PageTab.Items)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Select(selected + 1);
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Select(selected - 1);
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Select(selected + Columns);
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Select(selected - Columns);
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) UseSelected();
            }
        }
        if (!Input.GetKeyDown(KeyCode.I) && !(isOpen && Input.GetKeyDown(KeyCode.Escape))) return;
        if (isOpen) CloseInventory();
        else if (!combat.Paused && combat.PlayerHealth > 0) OpenInventory();
    }

    // 은비녀는 중요 아이템 탭에만 보인다. 일반 슬롯은 최대 60개.
    List<InventorySlot> Ordinary()
    {
        var result = new List<InventorySlot>();
        if (inventory == null) return result;
        foreach (var slot in inventory.Slots)
            if (!(slot.Item is JinaItem item && (item.effect == JinaItem.Effect.Keepsake || item.effect == JinaItem.Effect.QuestWisp))) result.Add(slot);
        return result;
    }

    void Select(int index)
    {
        var slots = Ordinary();
        selected = Mathf.Clamp(index, itemPage * PageSize, itemPage * PageSize + PageSize - 1);
        if (slots.Count == 0) selected = itemPage * PageSize;
    }

    void ChangePage(int page)
    {
        itemPage = Mathf.Clamp(page, 0, PageCount - 1);
        selected = itemPage * PageSize;
        message = "";
    }

    public void UseSelected()
    {
        var slots = Ordinary();
        if (combat == null || selected < 0 || selected >= slots.Count) return;
        var item = slots[selected].Item as JinaItem;
        if (item == null) { message = "지금은 쓸 수 없는 물건입니다."; return; }
        if (item.Use(combat, out message)) inventory.TryRemove(item);
        Select(selected);
    }

    void ConnectCombat()
    {
        var found = FindObjectOfType<Combat>();
        if (found == null) return;
        combat = found;
        combat.Restarted += HandleRestart;
    }

    void OpenInventory()
    {
        if (combat == null || combat.player == null) return;
        previousCanMove = combat.player.CanMove;
        previousTimeScale = Time.timeScale < .5f ? 1f : Time.timeScale;
        isOpen = true;
        message = "";
        Select(selected);
        combat.InventoryPauseActive = true;
        combat.player.CanMove = false;
        Time.timeScale = 0f;
    }

    void CloseInventory()
    {
        if (!isOpen) return;
        isOpen = false;
        if (combat != null)
        {
            combat.InventoryPauseActive = false;
            if (combat.player != null && combat.PlayerHealth > 0) combat.player.CanMove = previousCanMove;
        }
        Time.timeScale = previousTimeScale;
    }

    void HandleRestart()
    {
        CloseInventory();
        inventory.Clear();
        itemPage = selected = 0;
        tab = PageTab.Items;
        collectionPage = 0;
        selectedWeapon = 0;
        skillReadyAt.Clear();
        // 재시작하면 주운 무기를 잃고 단검과 활만 남음
        weapons.RemoveAll(w => w.id != "dagger" && w.id != "bow");
    }

    void OnDisable() { CloseInventory(); }
    void OnDestroy() { if (combat != null) combat.Restarted -= HandleRestart; }

    void OnGUI()
    {
        if (!isOpen || inventory == null) return;
        GUI.depth = -100;
        KoreanUI.Apply();

        const float cell = 54, gap = 6;
        float gridWidth = Columns * cell + (Columns - 1) * gap;
        float gridHeight = 4 * cell + 3 * gap;
        float width = gridWidth + WindowPad * 2;
        float height = TitleHeight + 12 + gridHeight + 12 + 88 + WindowPad;
        var window = new Rect(JinaUI.W - width - 12, 51, width, height);

        string[] tabNames = { "보따리", "귀중품", "스킬", "무기" };
        float tabWidth = (width - 6) / 4f;
        for (int i = 0; i < tabNames.Length; i++)
        {
            var r = new Rect(window.x + i * tabWidth, window.y - 38, tabWidth - 3, 34);
            if (Button(r, tabNames[i])) { tab = (PageTab)i; collectionPage = 0; message = ""; }
        }
        var inside = Window(window, tabNames[(int)tab], "I / Esc 닫기");
        Rect content = new Rect(inside.x, inside.y, gridWidth, gridHeight);

        switch (tab)
        {
            case PageTab.Items: DrawOrdinary(window, content, cell, gap); break;
            case PageTab.KeyItems: DrawKeyItems(content); break;
            case PageTab.Skills: DrawSkills(content); break;
            case PageTab.Weapons: DrawWeapons(window, content); break;
        }
        if (Application.isEditor && tab == PageTab.Items && testItem != null && Button(new Rect(window.x, window.yMax + 6, 170, 28), "테스트 아이템 +1"))
            inventory.TryAdd(testItem);
    }

    void DrawOrdinary(Rect window, Rect content, float cell, float gap)
    {
        var slots = Ordinary();
        int hovered = -1;
        for (int i = 0; i < PageSize; i++)
        {
            int absolute = itemPage * PageSize + i;
            var r = new Rect(content.x + (i % Columns) * (cell + gap), content.y + (i / Columns) * (cell + gap), cell, cell);
            bool filled = absolute < slots.Count, over = r.Contains(Event.current.mousePosition);
            if (over && filled) hovered = absolute;
            Slot(r, filled && absolute == selected, over && filled);
            if (GUI.Button(r, "", GUIStyle.none) && filled)
            {
                if (absolute == selected) UseSelected();
                else { selected = absolute; message = ""; }
            }
            if (!filled) continue;
            var slot = slots[absolute];
            if (slot.Item.Icon != null) DrawIcon(slot.Item.Icon, new Rect(r.x + 7, r.y + 5, cell - 14, cell - 14));
            else Text(new Rect(r.x + 3, r.y + 3, cell - 6, cell - 6), slot.Item.ItemName, 12, TextDark, TextAnchor.MiddleCenter);
            if (slot.Quantity > 1)
            {
                var tag = new Rect(r.xMax - 26, r.yMax - 20, 24, 18);
                Box(tag, Night, Ink, 0);
                Text(tag, slot.Quantity.ToString(), 13, Bright, TextAnchor.MiddleCenter, true);
            }
        }
        var pages = new Rect(content.x, content.yMax + 8, content.width, 28);
        if (Button(new Rect(pages.x, pages.y, 42, 28), "◀") && itemPage > 0) ChangePage(itemPage - 1);
        Text(new Rect(pages.x + 45, pages.y, pages.width - 90, 28), (itemPage + 1) + " / " + PageCount + "   (" + slots.Count + "/60)", 14, TextDark, TextAnchor.MiddleCenter, true);
        if (Button(new Rect(pages.xMax - 42, pages.y, 42, 28), "▶") && itemPage < PageCount - 1) ChangePage(itemPage + 1);
        int coins = combat != null && combat.Tavern != null ? combat.Tavern.wallet.Coins : 0;
        var bottom = new Rect(content.x, content.yMax + 40, content.width, 28);
        Box(bottom, PaperDark, SlotEdge, 1);
        Text(new Rect(bottom.x + 12, bottom.y, bottom.width - 24, bottom.height), "엽전", 15, TextDark, TextAnchor.MiddleLeft);
        Text(new Rect(bottom.x + 12, bottom.y, bottom.width - 24, bottom.height), coins + "문", 15, Red, TextAnchor.MiddleRight, true);
        DrawDetails(window, slots, hovered >= 0 ? hovered : selected);
    }

    void DrawKeyItems(Rect content)
    {
        bool hasHairpin = inventory.GetQuantity(JinaItems.Keepsake) > 0;
        bool hasWisp = inventory.GetQuantity(JinaItems.Wisp) > 0;
        bool hasCharm = combat != null && combat.Quest != null && combat.Quest.progress.HasCharm;
        float y = content.y;
        if (hasHairpin)
        {
            string description = JinaItems.Keepsake.Description;
            y += DrawKeyItemRecord(new Rect(content.x, y, content.width, 0), "은비녀", description, JinaItems.Keepsake.Icon);
        }
        if (hasWisp)
            y += DrawKeyItemRecord(new Rect(content.x, y, content.width, 0), JinaItems.Wisp.ItemName, JinaItems.Wisp.Description, JinaItems.Wisp.Icon);
        if (hasCharm)
        {
            string description = "옛 비석에서 얻은 정화 부적. 숲의 도깨비에게 씌인 저주를 풀 수 있다.\n도깨비 가까이에서 F를 눌러 정화한다.";
            y += DrawKeyItemRecord(new Rect(content.x, y, content.width, 0), "정화 부적", description, JinaArt.Prop("charm"));
        }
    }

    float DrawKeyItemRecord(Rect place, string name, string description, Sprite sprite)
    {
        const float pad = 8;
        float textWidth = place.width - 62;
        float bodyHeight = Height(description, 12, textWidth);
        float rowHeight = Mathf.Max(68, bodyHeight + 34);
        var r = new Rect(place.x, place.y, place.width, rowHeight);
        Box(r, PaperLight, SlotEdge, 1);
        if (sprite != null) DrawIcon(sprite, new Rect(r.x + 6, r.y + 7, 43, 43));
        Text(new Rect(r.x + 55, r.y + 5, textWidth, 22), name, 15, TextDark, TextAnchor.MiddleLeft, true);
        Text(new Rect(r.x + 55, r.y + 29, textWidth, bodyHeight), description, 12, TextDark);
        return rowHeight + pad;
    }

    // 스킬은 보관함에 보유 ID로 저장되고, Q/W/E 슬롯 데이터만 Skills에 반영된다.
    void DrawSkills(Rect content)
    {
        if (combat == null || combat.Skills == null || combat.Skills.slots == null)
        { Text(content, "스킬 정보를 연결하는 중입니다.", 14, TextDark); return; }
        Text(new Rect(content.x, content.y, content.width, 21), "획득한 스킬 " + ownedSkills.Count + " / " + MaxOwnedSkills, 14, TextDark, TextAnchor.MiddleLeft, true);
        const int perPage = 7;
        int maxPage = Mathf.Max(0, (ownedSkills.Count - 1) / perPage);
        collectionPage = Mathf.Clamp(collectionPage, 0, maxPage);
        for (int i = collectionPage * perPage; i < Mathf.Min(ownedSkills.Count, (collectionPage + 1) * perPage); i++)
        {
            var skill = ownedSkills[i];
            var r = new Rect(content.x, content.y + 24 + (i % perPage) * 30, content.width, 28);
            Box(r, PaperDark, SlotEdge, 1);
            Text(new Rect(r.x + 7, r.y, 123, r.height), skill.name, 13, TextDark, TextAnchor.MiddleLeft);
            for (int slot = 0; slot < 3; slot++)
            {
                int index = slot;
                bool chosen = EquippedSkillId(slot) == skill.id;
                if (Button(new Rect(r.x + 125 + slot * 48, r.y + 2, 46, 24), chosen ? "●" + "QWE"[slot] : "QWE"[slot].ToString()))
                {
                    if (chosen) { UnequipSkill(index); message = skill.name + " 장착 해제"; }
                    else if (EquipSkill(skill.id, index)) message = skill.name + " → " + "QWE"[slot];
                    else message = "스킬 사용 동작이 아직 구현되지 않았습니다.";
                }
            }
        }
        DrawCollectionPages(content, maxPage);
        Text(new Rect(content.x, content.yMax + 40, content.width, 22), "장착: Q " + EquippedSkillName(0) + " / W " + EquippedSkillName(1) + " / E " + EquippedSkillName(2), 12, TextDark);
        Text(new Rect(content.x, content.yMax + 65, content.width, 20), message, 12, Red);
        Text(new Rect(content.x, content.yMax + 85, content.width, 20), "장착된 Q/W/E 버튼을 다시 누르면 해제", 11, TextDim);
    }

    // 새 무기 목록을 저장한다. 실제 무기 동작은 별도의 전투/애니메이션 연동이 필요.
    void DrawWeapons(Rect window, Rect content)
    {
        Text(new Rect(content.x, content.y, content.width, 22), "획득 무기 " + weapons.Count + " / 20", 14, TextDark, TextAnchor.MiddleLeft, true);
        const int perPage = 6;
        int maxPage = Mathf.Max(0, (weapons.Count - 1) / perPage);
        collectionPage = Mathf.Clamp(collectionPage, 0, maxPage);
        for (int i = collectionPage * perPage; i < Mathf.Min(weapons.Count, (collectionPage + 1) * perPage); i++)
        {
            var weapon = weapons[i];
            var r = new Rect(content.x, content.y + 28 + (i % perPage) * 38, content.width, 36);
            Box(r, PaperDark, SlotEdge, 1);
            Icon(weapon.icon, r.x + 4, r.y + 4, 1f);
            if (GUI.Button(new Rect(r.x + 4, r.y + 2, 158, r.height - 4), "", GUIStyle.none)) { selectedWeapon = i; message = ""; }
            Text(new Rect(r.x + 37, r.y, 125, r.height), weapon.name, 14, TextDark, TextAnchor.MiddleLeft);
            for (int slot = 0; slot < 2; slot++)
            {
                int index = slot;
                if (Button(new Rect(r.x + 168 + slot * 52, r.y + 5, 48, 26), equippedWeapons[slot] == weapon.id ? "●" + (slot + 1) : (slot + 1).ToString()))
                {
                    if (equippedWeapons[index] == weapon.id) message = UnequipWeapon(index) ? weapon.name + " 장착 해제" : "최소 한 개의 무기는 장착해야 합니다.";
                    else if (EquipWeapon(weapon.id, index)) message = weapon.name + " → 무기 " + (slot + 1);
                    else message = "새 무기의 전투 동작을 연결해야 합니다.";
                }
            }
        }
        DrawCollectionPages(content, maxPage);
        Text(new Rect(content.x, content.yMax + 40, content.width, 22), "장착: 1 " + WeaponName(0) + " / 2 " + WeaponName(1), 13, TextDark);
        Text(new Rect(content.x, content.yMax + 65, content.width, 20), message, 12, Red);
        Text(new Rect(content.x, content.yMax + 85, content.width, 20), "장착된 1/2 버튼을 다시 누르면 해제", 11, TextDim);
        if (weapons.Count > 0) DrawWeaponDetails(window, weapons[Mathf.Clamp(selectedWeapon, 0, weapons.Count - 1)]);
    }

    void DrawWeaponDetails(Rect window, WeaponEntry weapon)
    {
        const float boxWidth = 300, pad = 16, inner = boxWidth - pad * 2;
        float x = window.x - boxWidth - 8, top = window.y;
        var stats = Weapon.Get(weapon.id);
        string description = weapon.description ?? "";
        float descHeight = Height(description, 15, inner);
        float total = pad + 80 + 12 + descHeight + 12 + 28 + pad;
        Box(new Rect(x, top, boxWidth, total), Paper, Wood, 3);
        float left = x + pad, y = top + pad;
        var iconBox = new Rect(left, y, 80, 80);
        Box(iconBox, PaperDark, SlotEdge, 1);
        // 실제 무기 스프라이트를 왼쪽 창에 크게 그린다.
        string art = stats.id == "bow" ? "04_Weapons/bow" : stats.art ?? "04_Weapons/hwando";
        DrawIcon(JinaArt.Load(art), new Rect(iconBox.x + 6, iconBox.y + 6, 68, 68));
        Text(new Rect(left + 92, y + 5, inner - 92, 30), weapon.name, 19, TextDark, TextAnchor.MiddleLeft, true);
        Text(new Rect(left + 92, y + 42, inner - 92, 26), "보유 중", 13, TextDim, TextAnchor.MiddleLeft);
        y += 92;
        Text(new Rect(left, y, inner, descHeight), description, 15, TextDark);
        y += descHeight + 12;
        string damage = stats.comboDamage != null ? string.Join(" / ", System.Array.ConvertAll(stats.comboDamage, v => v.ToString())) : stats.damage.ToString();
        Text(new Rect(left, y, inner, 28), "공격력 " + damage + (stats.comboDamage != null ? " (연격)" : ""), 15, Green, TextAnchor.MiddleLeft, true);
    }

    string WeaponName(int i) => weapons.Find(w => w.id == equippedWeapons[i])?.name ?? "없음";

    void DrawCollectionPages(Rect content, int maxPage)
    {
        var y = content.yMax + 8;
        if (Button(new Rect(content.x, y, 42, 28), "◀") && collectionPage > 0) { collectionPage--; message = ""; }
        Text(new Rect(content.x + 45, y, content.width - 90, 28), (collectionPage + 1) + " / " + (maxPage + 1), 13, TextDark, TextAnchor.MiddleCenter, true);
        if (Button(new Rect(content.xMax - 42, y, 42, 28), "▶") && collectionPage < maxPage) { collectionPage++; message = ""; }
    }

    void DrawRecord(Rect r, string name, string detail, bool owned, string image, Sprite sprite = null)
    {
        Box(r, owned ? PaperLight : PaperDark, SlotEdge, 1);
        if (sprite != null)
        {
            var oldColor = GUI.color;
            GUI.color = owned ? Color.white : new Color(1f, 1f, 1f, .35f);
            DrawIcon(sprite, new Rect(r.x + 5, r.y + 5, 43, 43));
            GUI.color = oldColor;
        }
        else Icon(image, r.x + 7, r.y + 8, 1.3f, owned ? 1f : .35f);
        Text(new Rect(r.x + 48, r.y + 5, r.width - 52, 23), name, 16, TextDark, TextAnchor.MiddleLeft, true);
        Text(new Rect(r.x + 48, r.y + 28, r.width - 52, 21), detail, 12, owned ? Green : TextDim);
    }

    // 기존 설명 창을 최대한 유지한다. 현재 페이지에 속한 보따리 아이템만 보여준다.
    void DrawDetails(Rect window, List<InventorySlot> slots, int index)
    {
        const float boxWidth = 300, pad = 16, inner = boxWidth - pad * 2;
        float x = window.x - boxWidth - 8, top = window.y;
        if (index < 0 || index >= slots.Count)
        {
            string empty = "이 칸은 비어 있습니다.\n주막 옆 보부상에게서 물건을 살 수 있습니다.";
            float emptyHeight = Height(empty, 15, inner), said = Height(message, 14, inner);
            var plain = new Rect(x, top, boxWidth, pad * 2 + 6 + emptyHeight + (said > 0 ? said + 8 : 0));
            Box(plain, Paper, Wood, 3);
            Text(new Rect(x + pad, top + pad + 3, inner, emptyHeight), empty, 15, TextDark);
            Text(new Rect(x + pad, top + pad + 11 + emptyHeight, inner, said), message, 14, Red);
            return;
        }
        var item = slots[index].Item;
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
        Text(new Rect(left + 68, y + 32, inner - 68, 20), "가진 수 " + slots[index].Quantity, 13, TextDim, TextAnchor.MiddleLeft);
        y += 68;
        Text(new Rect(left, y, inner, storyHeight), story, 15, TextDark);
        y += storyHeight + 8;
        if (effect != null) { Text(new Rect(left, y, inner, effectHeight), effect, 15, Green, TextAnchor.UpperLeft, true); y += effectHeight; }
        y += 12;
        if (Button(new Rect(left, y, 106, 34), "사용하기", item is JinaItem) && index < slots.Count)
        {
            selected = index;
            UseSelected();
        }
        Text(new Rect(left + 116, y, inner - 116, hintHeight), hint, 12, TextDim, TextAnchor.MiddleLeft);
        y += hintHeight + 10;
        Text(new Rect(left, y, inner, messageHeight), message, 14, Red);
    }

    public static void DrawIcon(Sprite icon, Rect area)
    {
        var t = icon.texture; var src = icon.textureRect;
        float scale = Mathf.Min(area.width / src.width, area.height / src.height);
        var size = new Vector2(src.width * scale, src.height * scale);
        var dst = new Rect(area.center.x - size.x / 2, area.center.y - size.y / 2, size.x, size.y);
        GUI.DrawTextureWithTexCoords(dst, t, new Rect(src.x / t.width, src.y / t.height, src.width / t.width, src.height / t.height));
    }
}
