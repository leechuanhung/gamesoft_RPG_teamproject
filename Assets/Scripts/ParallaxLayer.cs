using UnityEngine;

// ===== 원경 패럴랙스 (ParallaxLayer) =====
// 씬에 구운 세계(WorldBake)의 원경에 붙어 장면에 저장되므로, 파일 이름과 클래스 이름이 같아야 한다(빌드된 게임이
// 다른 파일 안의 클래스를 찾지 못해 장면 데이터가 깨진다).
// 먼 배경 층이 깊이감을 위해 카메라를 따라 조금 흐른다(최대 2칸). 층은 구역 폭 그대로이고 카메라가 그 구역을 볼
// 때만 흐른다: 흐르면 카메라 쪽으로 밀리므로 화면은 늘 덮이고, 밀려 나간 끝은 카메라가 그 구역에 갇혀 있어 보이지
// 않는다. 옆 구역을 볼 때는 제자리라 그 화면 가장자리에 끼어들지 않는다.
public class ParallaxLayer : MonoBehaviour
{
    public float home, factor = .15f;
    public const float MaxShift = 2f;
    private SpriteRenderer art;
    // 카메라 위치에 따라 배경 층을 조금 움직인다.
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (art == null) art = GetComponent<SpriteRenderer>();
        float half = art != null && art.sprite != null ? art.sprite.bounds.extents.x : 0;
        float offset = cam.transform.position.x - home;
        var p = transform.position;
        p.x = home + (Mathf.Abs(offset) <= half ? Mathf.Clamp(offset * factor, -MaxShift, MaxShift) : 0);
        transform.position = p;
    }
}
