using UnityEngine;

public class PrototypeView : MonoBehaviour
{
    public Transform target;
    static float shakeUntil, shakeLength, shakeAmount;
    public static void Shake(float amount, float length) { shakeAmount = amount; shakeLength = length; shakeUntil = Time.time + length; }
    // A cutscene can turn the camera toward a point: 0 follows the seonbi, 1 looks at `x`.
    static float focusX, focusWeight;
    public static void Focus(float x, float weight) { focusX = x; focusWeight = weight; }
    void Start()
    {
        if (target != null && FindObjectOfType<PrototypeCombat>() == null)
        {
            var world = new GameObject("네 구역 지도").AddComponent<PrototypeWorld>();
            world.Build(target.GetComponent<SpriteRenderer>().sprite);
            target.GetComponent<PrototypePlayer>().SetSpawn(PrototypeWorld.Spawn);
            var combat = gameObject.AddComponent<PrototypeCombat>();
            combat.player = target.GetComponent<PrototypePlayer>();
        }
    }
    void LateUpdate()
    {
        if (target != null)
        {
            float shake = Time.time < shakeUntil ? Mathf.Sin(Time.time * 80) * shakeAmount * (shakeUntil - Time.time) / shakeLength : 0;
            transform.position = new Vector3(ViewX(Mathf.Lerp(target.position.x, focusX, focusWeight)) + shake, 2f, -10f);
        }
    }
    // Where the camera looks for a point of interest at `x`: inside the world, and inside the current map.
    public float ViewX(float x)
    {
        var maps = GetComponent<PrototypeMaps>();
        var cam = GetComponent<Camera>();
        return maps != null && cam != null ? maps.ClampView(x, cam.orthographicSize * cam.aspect) : Mathf.Clamp(x, -60f, 54f);
    }

    void OnGUI()
    {
        PrototypeKoreanUI.Apply();
        var combat = GetComponent<PrototypeCombat>();
        GUI.Box(new Rect(15, 15, 520, 85), "지나가던 선비 · " + (combat != null && combat.UsingBow ? "활" : "환도")
            + (combat != null ? " · 화살 " + combat.Arrows + "/" + ShopRules.MaxArrows : ""));
        GUI.Label(new Rect(30, 43, 495, 25), "이동: A·D / 방향키    도약: 스페이스바    무기: Q    물러서기: K    보따리: I");
        GUI.Label(new Rect(30, 66, 495, 25), "달리기: Shift+이동    공격: J    대화: E    정화: F    다시 시작: R");
        // The map strip steps aside while a shop, a talk or the inventory is open.
        if (target != null && (combat == null || !combat.Paused))
        {
            float mapWidth = Mathf.Min(560, Screen.width - 30);
            var maps = GetComponent<PrototypeMaps>();
            string route = "";
            for (int i = 0; i < PrototypeMaps.All.Length; i++)
                route += (i > 0 ? "  ·  " : "") + (maps != null && i == maps.Current ? "[" + PrototypeMaps.All[i].name + "]" : PrototypeMaps.All[i].name);
            GUI.Box(new Rect((Screen.width - mapWidth) / 2, Screen.height - 103, mapWidth, 40), route);
        }
        if (combat != null && combat.Quest != null)
        {
            var progress = combat.Quest.progress;
            float x = Mathf.Max(15, Screen.width - 355);
            GUI.Box(new Rect(x, 15, 340, 125), "선악 · " + (progress.Honor > 0 ? "선행" : progress.Honor < 0 ? "악행" : "중립") + "  " + progress.Honor);
            var savedColor = GUI.color;
            GUI.color = new Color(.65f,.25f,.25f);
            GUI.DrawTexture(new Rect(x + 20, 47, 150, 12), Texture2D.whiteTexture);
            GUI.color = new Color(.3f,.65f,.45f);
            GUI.DrawTexture(new Rect(x + 170, 47, 150, 12), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(x + 20 + (progress.Honor + 100) * 1.5f - 2, 43, 4, 20), Texture2D.whiteTexture);
            GUI.color = savedColor;
            GUI.Label(new Rect(x + 15, 68, 315, 50), "일반 몬스터: " + (combat.Camp == null ? 3 : combat.Camp.Remaining) + "마리 남음"
                + (combat.River != null && combat.RiverFelled < PrototypeCombat.KeepsakeAt && (combat.RiverFelled > 0 || combat.River.Inside(target.position))  ? " · 물귀신 " + combat.RiverFelled + "/" + PrototypeCombat.KeepsakeAt : "")
                + (combat.Kong != null && combat.Kong.InVillage && !combat.Kong.Revealed ? " · 콩밭 단서 " + combat.Kong.Clues + "/3" : "") + "\n" +(progress.ElderDead ? "노인 희생 −40 · 보스에게 마지막 반격 10" : "노인 생존 · 토벌 +10 / 정화 +30"));
        }
    }
}
