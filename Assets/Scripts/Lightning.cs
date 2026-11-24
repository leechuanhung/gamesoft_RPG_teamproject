using UnityEngine;

// 번개 줄기 이펙트 (벽조궁).
// from에서 to까지 지그재그 선이 번쩍이며 모양을 바꾸다가 잠깐 뒤 사라진다.
//   Lightning.Bolt(시작점, 끝점, 굵기, 지속시간, 꺾임 정도)
public class Lightning : MonoBehaviour
{
    static Material material;
    private LineRenderer core, glow;
    private Vector3 from, to;
    private int segments;
    private float bornAt, life, jag, nextShape;

    public static void Bolt(Vector3 from, Vector3 to, float width = .07f, float life = .2f, float jag = .2f)
    {
        if (material == null) material = new Material(Shader.Find("Sprites/Default"));
        var b = new GameObject("번개").AddComponent<Lightning>();
        b.from = from; b.to = to; b.life = life; b.jag = jag;
        b.bornAt = Time.unscaledTime;
        b.segments = Mathf.Max(4, Mathf.RoundToInt(Vector3.Distance(from, to) / .22f));
        b.glow = b.MakeLine(width * 3.2f, new Color(.45f, .75f, 1f, .5f), 6);   // 바깥 하늘색 빛
        b.core = b.MakeLine(width, new Color(1f, .97f, .78f), 7);               // 안쪽 흰노랑 줄기
        b.Shape();
    }

    LineRenderer MakeLine(float width, Color color, int order)
    {
        var obj = new GameObject("줄기");
        obj.transform.SetParent(transform, false);
        var line = obj.AddComponent<LineRenderer>();
        line.material = material;
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.startColor = line.endColor = color;
        line.sortingOrder = order;
        line.positionCount = segments + 1;
        return line;
    }

    // 시작~끝 사이 점들을 옆으로 무작위로 밀어 지그재그를 만든다 (양 끝은 고정)
    void Shape()
    {
        Vector3 side = Vector3.Cross(to - from, Vector3.forward).normalized;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float push = i == 0 || i == segments ? 0 : Random.Range(-jag, jag);
            var p = Vector3.Lerp(from, to, t) + side * push;
            p.z = 0;
            core.SetPosition(i, p);
            glow.SetPosition(i, p);
        }
        nextShape = Time.unscaledTime + .04f;
    }

    void Update()
    {
        // 맞을 때 화면이 잠깐 멈춰도(JinaFx.Stop) 번개는 계속 번쩍이도록 실제 시간 사용
        float t = (Time.unscaledTime - bornAt) / life;
        if (t >= 1) { Destroy(gameObject); return; }
        if (Time.unscaledTime >= nextShape) Shape();
        float alpha = 1 - t * t;
        core.startColor = core.endColor = new Color(1f, .97f, .78f, alpha);
        glow.startColor = glow.endColor = new Color(.45f, .75f, 1f, .5f * alpha);
    }
}