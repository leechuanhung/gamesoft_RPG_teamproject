using UnityEngine;

public class PrototypeView : MonoBehaviour
{
    public Transform target;
    static float shakeUntil, shakeLength, shakeAmount;
    public static void Shake(float amount, float length) { shakeAmount = amount; shakeLength = length; shakeUntil = Time.time + length; }
    // A cutscene can turn the camera toward a point: 0 follows the seonbi, 1 looks at `x`.
    static float focusX, focusWeight;
    public static void Focus(float x, float weight) { focusX = x; focusWeight = weight; }
    // A cutscene room outside the world: the camera simply stays on it.
    public static float? FixedX;
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
        if (FixedX.HasValue) return FixedX.Value;
        var maps = GetComponent<PrototypeMaps>();
        var cam = GetComponent<Camera>();
        return maps != null && cam != null ? maps.ClampView(x, cam.orthographicSize * cam.aspect) : Mathf.Clamp(x, -60f, 54f);
    }
}
