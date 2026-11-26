using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ===== 세계를 씬에 굽기 (WorldBake) =====
// 실행할 때 코드(WorldBuilder.Build)가 짓던 땅·하늘·배경·기물·발판을 지금 열린 씬에 진짜 오브젝트로 넣는다.
// 그 뒤로는 씬 창에서 기물·배경·발판을 골라 옮기고 복사할 수 있고, 실행하면 게임이 씬의 것을 그대로 쓴다
// (WorldBuilder.Ensure). 땅과 하늘 무리는 지형 높이와 맵 경계에 맞춰져 있어 씬 창에서 고르지 못하게 잠근다.
// 다시 구우면(코드에서 배치를 바꿨을 때) 씬에서 옮긴 기물은 같은 자리에, 씬에서 새로 넣은 오브젝트도 그대로 남고,
// 씬에서 손대지 않은 기물은 새 코드의 자리로 간다(구울 때 자리를 WorldArt에 기록해 두고 견준다).
// 그림은 코드가 만든 스프라이트라 씬 파일에 저장하지 않는다: 저장하기 직전에 비우고 저장한 뒤 다시 입힌다(WorldArt).
[InitializeOnLoad]
public static class WorldBake
{
    public const string RootName = "세계 (씬에 구운 것)";
    static readonly string[] Movable = { WorldBuilder.PropGroup, WorldBuilder.BackdropGroup, WorldBuilder.LedgeGroup };

    static WorldBake()
    {
        EditorSceneManager.sceneSaving += (scene, path) => { foreach (var art in ArtIn(scene)) art.Clear(); };
        EditorSceneManager.sceneSaved += scene => { foreach (var art in ArtIn(scene)) art.Apply(); };
    }
    static IEnumerable<WorldArt> ArtIn(Scene scene)
    {
        foreach (var art in Object.FindObjectsOfType<WorldArt>(true))
            if (art.gameObject.scene == scene) yield return art;
    }

    [MenuItem("지나가던 선비/세계를 씬에 굽기 (다시 굽기)", false, 1)]
    static void BakeMenu()
    {
        if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("세계 굽기", "플레이 중에는 구울 수 없습니다. 플레이를 멈추고 다시 고르세요.", "확인"); return; }
        var world = Bake();
        Selection.activeGameObject = world.gameObject;
        EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
        Debug.Log("세계를 씬에 구웠습니다. 기물·배경·발판은 씬 창에서 옮겨도 됩니다. 씬을 저장하세요.");
    }

    [MenuItem("지나가던 선비/구운 세계 지우기 (실행할 때 코드로 짓기)", false, 2)]
    static void ClearMenu()
    {
        var world = Find();
        if (world == null) return;
        var scene = world.gameObject.scene;
        Undo.DestroyObjectImmediate(world.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    // 배치 모드용: 게임 장면을 열어 굽고 저장한다(Unity -batchmode -executeMethod WorldBake.BakeGameScene).
    // -freshbake를 붙이면 옛 구운 세계를 버리고 처음부터 굽는다(자리 기록이 없던 옛 세계를 옮길 때).
    public static void BakeGameScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MovementTest.unity");
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-freshbake") >= 0 && Find() != null) Object.DestroyImmediate(Find().gameObject);
        Bake();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("WORLD_BAKED: " + scene.path);
    }

    // 지금 씬에 구운 세계(없으면 null).
    public static WorldBuilder Find()
    {
        foreach (var w in Object.FindObjectsOfType<WorldBuilder>(true)) if (w.Baked) return w;
        return null;
    }

    // 무리 안 오브젝트의 이름표: 무리/이름#같은 이름 가운데 몇 번째.
    static Dictionary<string, Transform> Keys(WorldBuilder world)
    {
        var keys = new Dictionary<string, Transform>();
        foreach (var group in Movable)
        {
            var g = world.transform.Find(group);
            if (g == null) continue;
            var seen = new Dictionary<string, int>();
            foreach (Transform child in g)
            {
                seen.TryGetValue(child.name, out int n); seen[child.name] = n + 1;
                keys[group + "/" + child.name + "#" + n] = child;
            }
        }
        return keys;
    }

    // 세계를 굽는다. 이미 구운 것이 있으면 새로 지은 뒤 씬에서 옮긴 것과 새로 넣은 것을 옮겨 담고 옛것을 지운다.
    public static WorldBuilder Bake(Sprite square = null)
    {
        var old = Find();
        if (square == null) square = Square();
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "세계 굽기");
        if (old != null) SceneManager.MoveGameObjectToScene(root, old.gameObject.scene);
        var world = root.AddComponent<WorldBuilder>();
        world.Build(square);
        world.Baked = true;
        world.BakedLayout = WorldBuilder.Layout;
        var fresh = Keys(world);
        foreach (var t in fresh.Values) Stamp(t);
        if (old != null)
        {
            var kept = Keys(old);
            foreach (var pair in kept)
            {
                var was = pair.Value;
                if (fresh.TryGetValue(pair.Key, out var now))
                {
                    // 씬에서 손대지 않았으면 새 코드의 자리 그대로.
                    if (!Touched(was)) continue;
                    // 씬에서 옮기거나 뒤집거나 끈 것을 그대로(다음에 다시 구울 때도 손댄 것으로 남는다).
                    now.localPosition = was.localPosition; now.localRotation = was.localRotation; now.localScale = was.localScale;
                    var a = was.GetComponent<SpriteRenderer>(); var b = now.GetComponent<SpriteRenderer>();
                    if (a != null && b != null) { b.flipX = a.flipX; b.sortingOrder = a.sortingOrder; b.color = a.color; }
                    now.gameObject.SetActive(was.gameObject.activeSelf);
                }
                else
                {
                    // 씬에서 새로 넣은 것(복사한 기물 등)은 같은 무리로 옮겨 담는다.
                    var group = pair.Key.Substring(0, pair.Key.IndexOf('/'));
                    Undo.SetTransformParent(was, world.Group(group), "세계 굽기");
                }
            }
            // 무리 밖, 세계 바로 아래에 넣은 것도 옮겨 담는다.
            var loose = new List<Transform>();
            foreach (Transform child in old.transform)
                if (child.name != WorldBuilder.GroundGroup && System.Array.IndexOf(Movable, child.name) < 0) loose.Add(child);
            foreach (var child in loose) Undo.SetTransformParent(child, world.transform, "세계 굽기");
            Undo.DestroyObjectImmediate(old.gameObject);
        }
        var ground = world.transform.Find(WorldBuilder.GroundGroup);
        if (ground != null) SceneVisibilityManager.instance.DisablePicking(ground.gameObject, true);
        return world;
    }

    // 코드가 놓은 모습을 기록한다.
    static void Stamp(Transform t)
    {
        var art = t.GetComponent<WorldArt>();
        if (art == null) return;
        var r = t.GetComponent<SpriteRenderer>();
        art.built = true; art.builtAt = t.localPosition;
        if (r != null) { art.builtFlip = r.flipX; art.builtOrder = r.sortingOrder; art.builtColor = r.color; }
    }
    // 씬에서 손댔는지: 구울 때 기록과 자리·돌림·크기·뒤집기·순서·색·켜짐 가운데 하나라도 다르면. 기록이 없으면 손댄 것으로 본다.
    static bool Touched(Transform t)
    {
        var art = t.GetComponent<WorldArt>();
        if (art == null || !art.built || !t.gameObject.activeSelf) return true;
        if ((t.localPosition - art.builtAt).sqrMagnitude > 1e-8f || t.localRotation != Quaternion.identity || t.localScale != Vector3.one) return true;
        var r = t.GetComponent<SpriteRenderer>();
        return r != null && (r.flipX != art.builtFlip || r.sortingOrder != art.builtOrder || r.color != art.builtColor);
    }

    // 땅 조각에 쓰는 흰 사각형: 장면의 선비 자리 표시와 같은 그림.
    static Sprite Square()
    {
        var player = GameObject.Find("Player");
        var sprite = player != null && player.GetComponent<SpriteRenderer>() != null ? player.GetComponent<SpriteRenderer>().sprite : null;
        return sprite != null ? sprite : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Square.png");
    }
}
