using UnityEngine;

// ===== 구운 세계의 그림 (WorldArt) =====
// 땅·하늘·배경·기물·발판의 그림은 Resources의 PNG에서 실행 중에 만든 스프라이트(JinaArt)라서 씬 파일에 저장되지
// 않는다. 씬에 구운 세계(WorldBuilder.Baked, 에디터 메뉴 "지나가던 선비 > 세계를 씬에 굽기")의 오브젝트마다 이것이
// 붙어 무슨 그림인지 기억하고, 켜질 때마다(에디터에서도) 그림을 다시 입힌다. 씬을 저장할 때는 그림을 잠깐 비워 두어
// 씬 파일에 스프라이트가 끼어들지 않게 한다(Editor/WorldBake.cs).
[ExecuteAlways]
public class WorldArt : MonoBehaviour
{
    public enum Kind { Prop, Strip, Centred }
    public Kind kind;
    public string art;
    // 옆으로 이어 그리는 그림(땅 띠, 깊은 강물)의 크기. 그림을 다시 입힐 때 되돌린다.
    public Vector2 tiled;

    // 렌더러에 그림 이름을 기억시킨다.
    public static void Mark(SpriteRenderer renderer, Kind kind, string art)
    {
        var mark = renderer.gameObject.AddComponent<WorldArt>();
        mark.kind = kind; mark.art = art;
        if (renderer.drawMode != SpriteDrawMode.Simple) mark.tiled = renderer.size;
    }

    void OnEnable() { Apply(); }

    // 그림을 다시 입힌다.
    public void Apply()
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer == null || string.IsNullOrEmpty(art)) return;
        renderer.sprite = kind == Kind.Strip ? JinaArt.Strip(art) : kind == Kind.Centred ? JinaArt.PropCentred(art) : JinaArt.Prop(art);
        if (renderer.drawMode != SpriteDrawMode.Simple && tiled != Vector2.zero) renderer.size = tiled;
    }
    // 씬을 저장하기 전에 그림을 비운다(저장 뒤에 Apply).
    public void Clear()
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.sprite = null;
    }
}
