using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WorldChecks
{
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var world = new GameObject("World Check").AddComponent<PrototypeWorld>();
        world.Build(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/PrototypeSquare.png"));
        Physics2D.SyncTransforms();
        for (int x = -69; x <= 63; x++)
            Require(Physics2D.Raycast(new Vector2(x, 0), Vector2.down, 1, 1 << 8).collider != null, "Missing floor at " + x);
        Require(PrototypeWorld.Spawn.x > PrototypeWorld.VillageLeft && PrototypeWorld.Spawn.x < PrototypeWorld.ForestStart, "Spawn must be in village");
        Require(PrototypeWorld.Stone.x < PrototypeWorld.VillageLeft, "Puzzle must be in west");
        var enemy = new Vector2(PrototypeWorld.EnemyHome, .7f);
        Require(PrototypeWorld.CanDetect(new Vector2(PrototypeWorld.ForestStart, .2f), enemy), "Detect at forest entrance");
        Require(!PrototypeWorld.CanDetect(PrototypeWorld.Spawn, enemy), "Spawn must be safe");
        Require(!PrototypeWorld.CanDetect(new Vector2(11.9f, .2f), new Vector2(13.2f, .7f)), "Village boundary must remain safe even near enemy");
        Require(PrototypeCombat.EnemyMaxHealth == 30, "Health must be 30");
        ArrowChecks.Run();
        Debug.Log("WORLD_CHECKS_PASSED: continuous ground, area placement, detection and village safety");
    }
    static void Require(bool result, string label)
    {
        if (!result) throw new Exception(label);
    }
}
