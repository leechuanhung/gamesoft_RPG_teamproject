using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

[Serializable]
public class PlayerSaveData
{
    public float x, y, z, hp;
    public int stamina;
    public bool hasStamina;
    public BossSaveData[] bosses;
    public SkillCooldownSaveData[] skillCooldowns;
    public InventoryItemSaveData[] inventoryItems;
    public string[] ownedWeaponIds;
    public string[] equippedWeaponIds;
    public int activeWeaponSlot;
    public int coins;
    public int arrows;
    public SkillEntrySaveData[] ownedSkills;
    public string[] equippedSkillIds;
    public int keepsakeCount, wispCount;
    public StorySaveData story;
    public SideQuestSaveData sideQuests;
}

// 서브 퀘스트(QuestSystem): 퀘스트 id와 상태(State), 아이의 부탁에서 잡은 수, 뱃사공에게 이미 들었는지.
[Serializable]
public class SideQuestSaveData
{
    public bool saved;
    public string[] ids;
    public int[] states;
    public int childKills;
    public bool ferrymanTold;
}

// 이야기 진행(QuestProgress): 도깨비·콩쥐 요괴·좌의정을 어떻게 해결했는지, 선악, 노인, 부적, 잃은 마을 사람.
//   이게 없으면 불러온 뒤 앞의 보스가 "해결 안 됨"이 되어 좌의정을 쓰러뜨려도 결말이 열리지 않았다.
//   JsonUtility는 없는 칸도 빈 객체로 채우므로, 옛 저장 파일인지는 `saved`로 가린다.
[Serializable]
public class StorySaveData
{
    public bool saved;
    public bool heardClue, hasCharm, elderDead;
    public int puzzleStep, honor, villagersLost;
    public int outcome, kongOutcome, finalOutcome;   // EncounterOutcome
}

[Serializable]
public class SkillEntrySaveData
{
    public string id, name, description;
    public float cooldown;
    public bool implemented;
}

[Serializable]
public class InventoryItemSaveData
{
    public string itemId;
    public int quantity;
}

[Serializable]
public class BossSaveData
{
    public string id;
    public int hp;
    public bool hasPosition;
    public float x, y, z;
}

[Serializable]
public class SkillCooldownSaveData
{
    public string skillId;
    public float remainingSeconds;
}

public class PlayerSave : MonoBehaviour
{
    [Header("저장할 플레이어")]
    [SerializeField] private Transform player;
    [SerializeField] private Combat combat;

    private string SavePath => Path.Combine(Application.persistentDataPath, "player-save.json");

    private void Awake()
    {
        if (combat == null) combat = FindObjectOfType<Combat>();
        if (player == null && combat != null && combat.player != null)
            player = combat.player.transform;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) SaveGame();
        if (Input.GetKeyDown(KeyCode.F2)) LoadGame();
    }

    public bool SaveGame()
    {
        if (!CanAccessPlayer()) return false;

        PlayerSaveData data = new PlayerSaveData
        {
            x = player.position.x,
            y = player.position.y,
            z = player.position.z,
            hp = combat.PlayerHealth,
            stamina = combat.player != null ? combat.player.Stamina : 0,
            hasStamina = combat.player != null,
            bosses = CaptureBosses(),
            skillCooldowns = CaptureSkillCooldowns(),
            ownedSkills = CaptureOwnedSkills(),
            equippedSkillIds = CaptureEquippedSkills(),
            keepsakeCount = CaptureSpecialItemCount(JinaItems.Keepsake),
            wispCount = CaptureSpecialItemCount(JinaItems.Wisp),
            inventoryItems = CaptureInventory(),
            ownedWeaponIds = CaptureOwnedWeapons(),
            equippedWeaponIds = combat.EquippedWeapons,
            activeWeaponSlot = combat.ActiveWeaponSlot,
            coins = CaptureCoins(),
            arrows = combat.Arrows,
            story = CaptureStory(),
            sideQuests = combat.SideQuests != null ? combat.SideQuests.Capture() : null
        };

        try { File.WriteAllText(SavePath, JsonUtility.ToJson(data, true)); }
        catch (Exception e) { Debug.LogError($"저장하지 못했습니다: {e.Message}"); return false; }
        Debug.Log($"저장했습니다: {SavePath} (보스 {data.bosses.Length}마리)");
        return true;
    }

    public bool LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("저장 파일이 없습니다.");
            return false;
        }
        if (!CanAccessPlayer()) return false;

        PlayerSaveData data;
        try { data = JsonUtility.FromJson<PlayerSaveData>(File.ReadAllText(SavePath)); }
        catch (Exception e) { Debug.LogError($"저장 파일을 읽지 못했습니다: {e.Message}"); return false; }
        if (data == null)
        {
            Debug.LogError("저장 파일을 읽을 수 없습니다.");
            return false;
        }

        player.position = new Vector3(data.x, data.y, data.z);
        combat.SetPlayerHealthFromSave(Mathf.RoundToInt(data.hp));
        if (data.hasStamina && combat.player != null)
        {
            int savedStamina = Mathf.Clamp(data.stamina, 0, combat.player.MaxStamina);
            combat.player.UseStamina(combat.player.Stamina);
            combat.player.RestoreStamina(savedStamina);
        }
        int bossCount = RestoreBosses(data.bosses);
        RestoreStory(data.story);   // 보스 체력 뒤에: 진행에 맞춰 보스 상태를 다시 맞춘다
        if (combat.SideQuests != null) combat.SideQuests.Restore(data.sideQuests);
        RestoreSkills(data.ownedSkills, data.equippedSkillIds);
        int skillCount = RestoreSkillCooldowns(data.skillCooldowns);
        int itemCount = RestoreInventory(data.inventoryItems);
        EnsureSpecialItem(JinaItems.Keepsake, data.keepsakeCount);
        EnsureSpecialItem(JinaItems.Wisp, data.wispCount);
        RestoreWeapons(data.ownedWeaponIds, data.equippedWeaponIds, data.activeWeaponSlot);
        RestoreCoins(data.coins);
        combat.SetArrowsFromSave(data.arrows);
        Debug.Log($"불러왔습니다. 보스 {bossCount}마리, 스킬 쿨타임 {skillCount}개, 아이템 종류 {itemCount}개와 무기를 복원했습니다.");
        return true;
    }

    private StorySaveData CaptureStory()
    {
        if (combat == null || combat.Quest == null) return null;
        var q = combat.Quest.progress;
        return new StorySaveData
        {
            saved = true,
            heardClue = q.HeardClue, hasCharm = q.HasCharm, elderDead = q.ElderDead,
            puzzleStep = q.PuzzleStep, honor = q.Honor, villagersLost = q.VillagersLost,
            outcome = (int)q.Outcome, kongOutcome = (int)q.KongOutcome, finalOutcome = (int)q.FinalOutcome
        };
    }

    // 진행을 되살리고, 세 보스(도깨비·콩쥐 요괴·좌의정)를 그 진행에 맞춘다.
    private void RestoreStory(StorySaveData story)
    {
        if (story == null || !story.saved || combat == null || combat.Quest == null) return;
        var q = combat.Quest.progress;
        q.Restore(story.heardClue, story.hasCharm, story.puzzleStep, (EncounterOutcome)story.outcome, story.honor, story.elderDead,
            (EncounterOutcome)story.kongOutcome, story.villagersLost, (EncounterOutcome)story.finalOutcome);
        combat.MatchBossToProgress();
        if (combat.KongBoss != null) combat.KongBoss.MatchProgress(q.KongOutcome);
        if (combat.FinalBoss != null) combat.FinalBoss.MatchProgress(q.FinalOutcome);
    }

    private InventoryItemSaveData[] CaptureInventory()
    {
        InventoryManager inventory = FindObjectOfType<InventoryManager>();
        var totals = new Dictionary<string, int>();
        if (inventory != null)
        {
            foreach (InventorySlot slot in inventory.Slots)
            {
                JinaItem item = slot.Item as JinaItem;
                if (item == null || string.IsNullOrEmpty(item.id)) continue;
                totals[item.id] = totals.TryGetValue(item.id, out int count) ? count + slot.Quantity : slot.Quantity;
            }
        }
        var result = new List<InventoryItemSaveData>();
        foreach (var pair in totals)
            result.Add(new InventoryItemSaveData { itemId = pair.Key, quantity = pair.Value });
        return result.ToArray();
    }

    private int RestoreInventory(InventoryItemSaveData[] saved)
    {
        InventoryManager inventory = FindObjectOfType<InventoryManager>();
        if (inventory == null) return 0;
        inventory.Clear();
        if (saved == null) return 0;
        int restored = 0;
        foreach (InventoryItemSaveData entry in saved)
        {
            if (entry == null || entry.quantity <= 0) continue;
            JinaItem item = entry.itemId == JinaItems.Keepsake.id ? JinaItems.Keepsake
                          : entry.itemId == JinaItems.Wisp.id ? JinaItems.Wisp
                          : JinaItems.Get(entry.itemId);
            if (item != null && inventory.TryAdd(item, entry.quantity)) restored++;
        }
        return restored;
    }

    private int CaptureSpecialItemCount(JinaItem item)
    {
        InventoryManager inventory = FindObjectOfType<InventoryManager>();
        return inventory != null ? inventory.GetQuantity(item) : 0;
    }

    private void EnsureSpecialItem(JinaItem item, int savedCount)
    {
        InventoryManager inventory = FindObjectOfType<InventoryManager>();
        if (inventory == null || savedCount <= inventory.GetQuantity(item)) return;
        inventory.TryAdd(item, savedCount - inventory.GetQuantity(item));
    }

    private string[] CaptureOwnedWeapons()
    {
        InventoryUI ui = FindObjectOfType<InventoryUI>();
        if (ui == null) return Array.Empty<string>();
        var ids = new List<string>();
        foreach (InventoryUI.WeaponEntry weapon in ui.OwnedWeapons) ids.Add(weapon.id);
        return ids.ToArray();
    }

    private void RestoreWeapons(string[] owned, string[] equipped, int activeSlot)
    {
        InventoryUI ui = FindObjectOfType<InventoryUI>();
        if (ui != null) ui.RestoreWeapons(owned, equipped);
        if (combat != null) combat.RestoreActiveWeaponSlot(activeSlot);
    }

    private SkillEntrySaveData[] CaptureOwnedSkills()
    {
        InventoryUI ui = FindObjectOfType<InventoryUI>();
        if (ui == null) return Array.Empty<SkillEntrySaveData>();
        var result = new List<SkillEntrySaveData>();
        foreach (InventoryUI.SkillEntry skill in ui.OwnedSkills)
            result.Add(new SkillEntrySaveData { id = skill.id, name = skill.name, description = skill.description,
                cooldown = skill.cooldown, implemented = skill.implemented });
        return result.ToArray();
    }

    private string[] CaptureEquippedSkills()
    {
        InventoryUI ui = FindObjectOfType<InventoryUI>();
        if (ui == null) return Array.Empty<string>();
        return new[] { ui.EquippedSkillId(0), ui.EquippedSkillId(1), ui.EquippedSkillId(2) };
    }

    private void RestoreSkills(SkillEntrySaveData[] owned, string[] equipped)
    {
        InventoryUI ui = FindObjectOfType<InventoryUI>();
        if (ui != null) ui.RestoreSkills(owned, equipped);
    }

    private int CaptureCoins()
    {
        TavernShop tavern = combat != null && combat.Tavern != null ? combat.Tavern : FindObjectOfType<TavernShop>();
        return tavern != null ? tavern.wallet.Coins : 0;
    }

    private void RestoreCoins(int coins)
    {
        TavernShop tavern = combat != null && combat.Tavern != null ? combat.Tavern : FindObjectOfType<TavernShop>();
        if (tavern != null) tavern.wallet.SetCoins(coins);
    }

    private BossSaveData[] CaptureBosses()
    {
        var result = new List<BossSaveData>();
        foreach (MonoBehaviour component in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (!TryGetBossHealth(component, out PropertyInfo hpProperty)) continue;
            if (!(hpProperty.GetValue(component, null) is int hp)) continue;

            Vector3 position = GetBossTransform(component).position;
            result.Add(new BossSaveData
            {
                id = GetStableId(component),
                hp = hp,
                hasPosition = true,
                x = position.x,
                y = position.y,
                z = position.z
            });
        }

        result.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        return result.ToArray();
    }

    private int RestoreBosses(BossSaveData[] saved)
    {
        if (saved == null) return 0;

        var current = new Dictionary<string, MonoBehaviour>();
        foreach (MonoBehaviour component in FindObjectsOfType<MonoBehaviour>(true))
            if (TryGetBossHealth(component, out _))
                current[GetStableId(component)] = component;

        int restored = 0;
        foreach (BossSaveData entry in saved)
        {
            if (entry == null || string.IsNullOrEmpty(entry.id) || !current.TryGetValue(entry.id, out MonoBehaviour component)) continue;

            if (TryGetBossHealth(component, out PropertyInfo hpProperty))
            {
                MethodInfo setter = hpProperty.GetSetMethod(true);
                if (setter != null)
                    setter.Invoke(component, new object[] { Mathf.Max(0, entry.hp) });
            }

            if (entry.hasPosition)
            {
                Vector3 position = new Vector3(entry.x, entry.y, entry.z);
                Transform bossTransform = GetBossTransform(component);
                bossTransform.position = position;

                Rigidbody2D body = bossTransform.GetComponent<Rigidbody2D>();
                if (body != null) body.position = position;
            }
            restored++;
        }
        return restored;
    }

    private SkillCooldownSaveData[] CaptureSkillCooldowns()
    {
        var result = new List<SkillCooldownSaveData>();
        Skills skills = combat != null ? combat.Skills : null;
        if (skills == null || skills.slots == null) return result.ToArray();

        foreach (Skills.Slot slot in skills.slots)
        {
            if (slot == null || string.IsNullOrEmpty(slot.skillId)) continue;
            result.Add(new SkillCooldownSaveData
            {
                skillId = slot.skillId,
                remainingSeconds = Mathf.Max(0f, slot.readyAt - Time.time)
            });
        }
        return result.ToArray();
    }

    private int RestoreSkillCooldowns(SkillCooldownSaveData[] saved)
    {
        Skills skills = combat != null ? combat.Skills : null;
        if (saved == null || skills == null || skills.slots == null) return 0;

        int restored = 0;
        foreach (SkillCooldownSaveData entry in saved)
        {
            if (entry == null || string.IsNullOrEmpty(entry.skillId)) continue;
            foreach (Skills.Slot slot in skills.slots)
            {
                if (slot == null || slot.skillId != entry.skillId) continue;
                slot.readyAt = Time.time + Mathf.Max(0f, entry.remainingSeconds);
                restored++;
                break;
            }
        }
        return restored;
    }

    private static bool TryGetBossHealth(MonoBehaviour component, out PropertyInfo property)
    {
        property = null;
        if (component == null) return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = component.GetType();
        PropertyInfo candidate = type.GetProperty("EnemyHealth", flags)
                              ?? type.GetProperty("Health", flags);
        if (candidate == null || candidate.PropertyType != typeof(int) ||
            candidate.GetGetMethod(true) == null || candidate.GetSetMethod(true) == null)
            return false;

        property = candidate;
        return true;
    }

    private static Transform GetBossTransform(MonoBehaviour component)
    {
        // Combat은 매니저 오브젝트에 붙고, 도깨비 스프라이트는 별도 오브젝트로 생성됩니다.
        if (component is Combat)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo enemyField = typeof(Combat).GetField("enemy", flags);
            SpriteRenderer enemy = enemyField != null ? enemyField.GetValue(component) as SpriteRenderer : null;
            if (enemy != null) return enemy.transform;
        }
        return component.transform;
    }

    private static string GetStableId(Component component)
    {
        string path = component.transform.name;
        for (Transform parent = component.transform.parent; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;

        return component.gameObject.scene.name + "|" + path + "|" + component.GetType().FullName;
    }

    private bool CanAccessPlayer()
    {
        if (combat == null) combat = FindObjectOfType<Combat>();
        if (player == null && combat != null && combat.player != null)
            player = combat.player.transform;

        if (player == null || combat == null)
        {
            Debug.LogError("PlayerSave에 Combat와 Player가 필요합니다.");
            return false;
        }
        return true;
    }
}
