using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrototypeSetup
{
    [MenuItem("Seonbi/Create Movement Test Scene")]
    public static void Build()
    {
        const string scenePath = "Assets/Scenes/MovementTest.unity";
        if (File.Exists(scenePath)) return;
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Art");
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        File.WriteAllBytes("Assets/Art/PrototypeSquare.png", texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.Refresh();
        var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/PrototypeSquare.png");
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 1;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/PrototypeSquare.png");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Block("Ground", new Vector2(0, -1), new Vector2(18, 1), new Color(.25f,.39f,.31f), sprite, true);
        Block("Step", new Vector2(6, .3f), new Vector2(3, .5f), new Color(.4f,.55f,.4f), sprite, true);
        Block("Island", new Vector2(13, -.4f), new Vector2(4, 1), new Color(.25f,.39f,.31f), sprite, true);
        Block("Finish Ground", new Vector2(22, -1), new Vector2(10, 1), new Color(.25f,.39f,.31f), sprite, true);
        Block("Goal", new Vector2(24, 1), new Vector2(.4f, 3), new Color(1,.75f,.2f), sprite, false);
        var player = Block("Player", new Vector2(-5, 1), new Vector2(.8f, 1.4f), new Color(.85f,.9f,1f), sprite, false);
        player.AddComponent<BoxCollider2D>();
        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 3;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var material = new PhysicsMaterial2D("PlayerFriction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(material, "Assets/Art/PlayerFriction.physicsMaterial2D");
        player.GetComponent<BoxCollider2D>().sharedMaterial = material;
        player.AddComponent<PrototypePlayer>();
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.1f,.15f,.23f);
        cameraObject.transform.position = new Vector3(0, 2, -10);
        cameraObject.AddComponent<PrototypeView>().target = player.transform;
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("SEONBI_SETUP_OK");
    }

    static GameObject Block(string name, Vector2 position, Vector2 size, Color color, Sprite sprite, bool solid)
    {
        var obj = new GameObject(name);
        obj.transform.position = position;
        obj.transform.localScale = size;
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        if (solid) { obj.layer = 8; obj.AddComponent<BoxCollider2D>(); }
        return obj;
    }
}
