using System;
using System.IO;
using System.Reflection;
using UnityEngine;


[Serializable]
public class PlayerSaveData
{
    public float x;
    public float y;
    public float z;
    public float hp;
}

public class PlayerSave : MonoBehaviour
{
    [Header("저장할 플레이어")]
    [SerializeField] private Transform player;

    [Header("HP를 가진 컴포넌트와 필드명")]
    [SerializeField] private MonoBehaviour hpComponent;
    [SerializeField] private string hpFieldName = "hp";

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, "player-save.json");

    private FieldInfo HpField =>
        hpComponent == null
            ? null
            : hpComponent.GetType().GetField(
                hpFieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            Save();

        if (Input.GetKeyDown(KeyCode.F2))
            Load();
    }

    private void Save()
    {
        if (!CanAccessHp())
            return;

        PlayerSaveData data = new PlayerSaveData
        {
            x = player.position.x,
            y = player.position.y,
            z = player.position.z,
            hp = Convert.ToSingle(HpField.GetValue(hpComponent))
        };

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log($"저장했습니다: {SavePath}");
    }

    private void Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("저장 파일이 없습니다.");
            return;
        }

        if (!CanAccessHp())
            return;

        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(File.ReadAllText(SavePath));

        player.position = new Vector3(data.x, data.y, data.z);

        object hpValue = HpField.FieldType == typeof(int)
            ? (object)Mathf.RoundToInt(data.hp)
            : data.hp;

        if (HpField.FieldType != typeof(int) &&
            HpField.FieldType != typeof(float))
        {
            Debug.LogError("HP 필드는 int 또는 float이어야 합니다.");
            return;
        }

        HpField.SetValue(hpComponent, hpValue);
        Debug.Log($"불러왔습니다. HP: {data.hp}");
    }

    private bool CanAccessHp()
    {
        if (player == null || hpComponent == null || HpField == null)
        {
            Debug.LogError(
                "Player와 HP 컴포넌트를 연결하고 HP 필드명을 확인하세요."
            );
            return false;
        }

        if (HpField.FieldType != typeof(int) &&
            HpField.FieldType != typeof(float))
        {
            Debug.LogError("HP 필드는 int 또는 float이어야 합니다.");
            return false;
        }

        return true;
    }
}