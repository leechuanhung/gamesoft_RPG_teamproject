using System.Collections.Generic;
using UnityEngine;

// Static pack art with small procedural motion; combat keeps its existing bounds.
public class MonsterVisual : MonoBehaviour
{
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    private SpriteRenderer hitArea, picture;
    private Vector3 lastPosition;
    private float width;
    private float phase;

    public static Sprite Load(string name)
    {
        Sprite sprite;
        if (cache.TryGetValue(name, out sprite) && sprite != null) return sprite;
        var texture = Resources.Load<Texture2D>("MonsterArt/" + name);
        if (texture == null) return null;
        var pixels = texture.GetPixels32();
        int left = texture.width, bottom = texture.height, right = -1, top = -1;
        for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
                if (pixels[y * texture.width + x].a > 0)
                {
                    left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                    bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                }
        if (right < left) return null;
        sprite = Sprite.Create(texture, new Rect(left, bottom, right - left + 1, top - bottom + 1),
            new Vector2(.5f, 0), 32, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        cache[name] = sprite;
        return sprite;
    }

    public static void Attach(SpriteRenderer root, string name, float visualWidth)
    {
        var sprite = Load(name);
        if (sprite == null) { Debug.LogWarning("Monster art missing: " + name); return; }
        var component = root.gameObject.AddComponent<MonsterVisual>();
        component.hitArea = root;
        component.width = visualWidth;
        component.lastPosition = root.transform.position;
        var child = new GameObject("몬스터 외형");
        child.transform.SetParent(root.transform, false);
        component.picture = child.AddComponent<SpriteRenderer>();
        component.picture.sprite = sprite;
        component.picture.sortingOrder = 2;
        root.forceRenderingOff = true;
        component.LateUpdate();
    }

    void LateUpdate()
    {
        if (hitArea == null || picture == null) return;
        picture.enabled = hitArea.enabled;
        if (!picture.enabled) { lastPosition = transform.position; return; }
        float movement = transform.position.x - lastPosition.x;
        if (Mathf.Abs(movement) > .0001f && Mathf.Abs(movement) < 2f)
            picture.flipX = movement > 0; // Source animals face left.
        bool teleported = Vector3.Distance(transform.position, lastPosition) > 2f;
        if (teleported) { phase = 0; picture.flipX = false; }
        var combat = FindObjectOfType<PrototypeCombat>();
        bool paused = combat != null && (combat.PlayerHealth <= 0 || combat.Paused);
        if (!paused) phase += Time.deltaTime * (Mathf.Abs(movement) > .0001f ? 12f : 3f);
        float bounce = paused ? 0 : Mathf.Abs(Mathf.Sin(phase)) * (Mathf.Abs(movement) > .0001f ? .07f : .015f);
        float uniform = width / picture.sprite.bounds.size.x;
        picture.transform.localScale = new Vector3(uniform / transform.lossyScale.x, uniform / transform.lossyScale.y, 1);
        picture.transform.position = new Vector3(transform.position.x, hitArea.bounds.min.y + bounce, 0);
        var color = hitArea.color;
        bool warning = color.r > .9f && color.g > .4f && color.g < .8f;
        picture.color = warning ? new Color(1f,.6f,.25f) : Color.white;
        // A brief bright pulse preserves the existing boss hit feedback.
        if (color == Color.white) picture.color = new Color(1f,.65f,.65f);
        lastPosition = transform.position;
    }
}
