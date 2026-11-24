using UnityEngine;

// ===== 원경 패럴랙스 (ParallaxLayer) =====
// 씬에 구운 세계(WorldBake)의 원경에 붙어 장면에 저장되므로, 파일 이름과 클래스 이름이 같아야 한다(빌드된 게임이
// 다른 파일 안의 클래스를 찾지 못해 장면 데이터가 깨진다).
// 먼 배경 층이 깊이감을 위해 카메라를 따라 조금 흐른다. 각 층은 구역보다 2칸 넓게 그려져 있고
// 그 이상 밀리지 않으므로 옆 구역으로 넘어가지 않는다.
public class ParallaxLayer : MonoBehaviour
{
    public float home, factor = .15f;
    public const float MaxShift = 2f;
    // 카메라 위치에 따라 배경 층을 조금 움직인다.
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var p = transform.position;
        p.x = home + Mathf.Clamp((cam.transform.position.x - home) * factor, -MaxShift, MaxShift);
        transform.position = p;
    }
}
