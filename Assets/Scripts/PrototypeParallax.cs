using UnityEngine;

// A far background layer drifts a little with the camera, for depth. It never shifts more than the 2 units each
// layer is drawn wider than its area, so it cannot slide into the neighbouring area.
public class PrototypeParallax : MonoBehaviour
{
    public float home, factor = .15f;
    public const float MaxShift = 2f;
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var p = transform.position;
        p.x = home + Mathf.Clamp((cam.transform.position.x - home) * factor, -MaxShift, MaxShift);
        transform.position = p;
    }
}
