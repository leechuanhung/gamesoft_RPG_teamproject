using UnityEngine;

public class PrototypeView : MonoBehaviour
{
    public Transform target;
    static float shakeUntil, shakeLength, shakeAmount;
    public static void Shake(float amount, float length) { shakeAmount = amount; shakeLength = length; shakeUntil = Time.time + length; }
    // 연출이 카메라를 한 점으로 돌릴 수 있다: 0이면 선비를 따라가고, 1이면 `x`를 본다.
    static float focusX, focusWeight;
    public static void Focus(float x, float weight) { focusX = x; focusWeight = weight; }
    // 세계 밖 연출 방: 카메라가 그냥 거기 머문다.
    public static float? FixedX;
    // 카메라를 준비하고 세계와 전투를 만든다.
    void Start()
    {
        if (target != null && FindObjectOfType<PrototypeCombat>() == null)
        {
            var world = new GameObject("네 구역 지도").AddComponent<WorldBuilder>();
            world.Build(target.GetComponent<SpriteRenderer>().sprite);
            target.GetComponent<PrototypePlayer>().SetSpawn(WorldBuilder.Spawn);
            var combat = gameObject.AddComponent<PrototypeCombat>();
            combat.player = target.GetComponent<PrototypePlayer>();
        }
    }
    // 카메라가 선비(또는 연출 지점)를 따라간다.
    void LateUpdate()
    {
        if (target != null)
        {
            float shake = Time.time < shakeUntil ? Mathf.Sin(Time.time * 80) * shakeAmount * (shakeUntil - Time.time) / shakeLength : 0;
            transform.position = new Vector3(ViewX(Mathf.Lerp(target.position.x, focusX, focusWeight)) + shake, 2f, -10f);
        }
    }
    // `x`의 관심 지점을 볼 때 카메라 위치: 세계 안, 그리고 지금 맵 안.
    public float ViewX(float x)
    {
        if (FixedX.HasValue) return FixedX.Value;
        var maps = GetComponent<PrototypeMaps>();
        var cam = GetComponent<Camera>();
        return maps != null && cam != null ? maps.ClampView(x, cam.orthographicSize * cam.aspect) : Mathf.Clamp(x, -60f, 54f);
    }
}
