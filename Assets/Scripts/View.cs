using UnityEngine;

public class View : MonoBehaviour
{
    public Transform target;
    static float shakeUntil, shakeLength, shakeAmount;
    public static void Shake(float amount, float length) { shakeAmount = amount; shakeLength = length; shakeUntil = Time.time + length; }
    // 연출이 카메라를 한 점으로 돌릴 수 있다: 0이면 선비를 따라가고, 1이면 `x`를 본다.
    static float focusX, focusWeight;
    public static void Focus(float x, float weight) { focusX = x; focusWeight = weight; }
    // 세계 밖 연출 방: 카메라가 그냥 거기 머문다.
    public static float? FixedX;

    // 카메라 줌 연출 및 기본 배율 복구
    Camera cam;
    float defaultSize;
    static float zoomScale = 1f;
    static float zoomUntil;

    public static void Zoom(float scale, float duration)
    {
        zoomScale = Mathf.Clamp(scale, .5f, 1.5f);
        zoomUntil = Time.time + duration;
    }

    // 카메라를 준비하고 세계와 전투를 만든다.
    void Start()
    {
        // 기본 카메라 배율 저장
        cam = GetComponent<Camera>();
        if (cam != null)
            defaultSize = cam.orthographicSize;

        if (target != null && FindObjectOfType<Combat>() == null)
        {
            // 씬에 구운 세계가 있으면 그것을, 없으면 코드로 새로 짓는다.
            WorldBuilder.Ensure(target.GetComponent<SpriteRenderer>().sprite);
            target.GetComponent<Player>().SetSpawn(WorldBuilder.Spawn);
            var combat = gameObject.AddComponent<Combat>();
            combat.player = target.GetComponent<Player>();
        }
    }
    // 카메라가 선비(또는 연출 지점)를 따라간다.
    void LateUpdate()
    {
        // 부드러운 줌인 및 줌아웃
        if (cam != null)
        {
            float desiredSize = Time.time < zoomUntil
                ? defaultSize * zoomScale
                : defaultSize;

            cam.orthographicSize = Mathf.Lerp(
                cam.orthographicSize,
                desiredSize,
                1f - Mathf.Exp(-5f * Time.unscaledDeltaTime)
            );
        }

        if (target != null)
        {
            float shake = Time.time < shakeUntil ? Mathf.Sin(Time.time * 80) * shakeAmount * (shakeUntil - Time.time) / shakeLength : 0;

            // 상하 방향 카메라 흔들림 추가
            float shakeY = Time.time < shakeUntil ? Mathf.Cos(Time.time * 65) * shakeAmount * .65f * (shakeUntil - Time.time) / shakeLength : 0;

            transform.position = new Vector3(ViewX(Mathf.Lerp(target.position.x, focusX, focusWeight)) + shake, 2f + shakeY, -10f);
        }
    }
    // `x`의 관심 지점을 볼 때 카메라 위치: 세계 안, 그리고 지금 맵 안.
    public float ViewX(float x)
    {
        if (FixedX.HasValue) return FixedX.Value;
        var maps = GetComponent<Maps>();
        var cam = GetComponent<Camera>();
        return maps != null && cam != null ? maps.ClampView(x, cam.orthographicSize * cam.aspect) : Mathf.Clamp(x, -60f, 54f);
    }
}