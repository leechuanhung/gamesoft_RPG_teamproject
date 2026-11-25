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
        if (Input.GetKeyDown(KeyCode.F1)) Save();
        if (Input.GetKeyDown(KeyCode.F2)) Load();
    }

    private void Save()
    {
        if (!CanAccessPlayer()) return;

        PlayerSaveData data = new PlayerSaveData
        {
            x = player.position.x,
            y = player.position.y,
            z = player.position.z,
            hp = combat.PlayerHealth,
            stamina = combat.player != null ? combat.player.Stamina : 0,
            hasStamina = combat.player != null,
            bosses = CaptureBosses(),
            skillCooldowns = CaptureSkillCooldowns()
        };

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log($"저장했습니다: {SavePath} (보스 {data.bosses.Length}마리)");
    }

    private void Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("저장 파일이 없습니다.");
            return;
        }
        if (!CanAccessPlayer()) return;

        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(File.ReadAllText(SavePath));
        if (data == null)
        {
            Debug.LogError("저장 파일을 읽을 수 없습니다.");
            return;
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
        int skillCount = RestoreSkillCooldowns(data.skillCooldowns);
        Debug.Log($"불러왔습니다. 보스 {bossCount}마리 위치/HP, 스킬 쿨타임 {skillCount}개와 스테미나 복원");
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