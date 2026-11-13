using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WorldChecks
{
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var world = new GameObject("World Check").AddComponent<WorldBuilder>();
        world.Build(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/PrototypeSquare.png"));
        Physics2D.SyncTransforms();
        for (int x = -69; x < (int)WorldBuilder.HillEnd; x++)
        {
            if (x == (int)WorldBuilder.RiverLeft || x == (int)WorldBuilder.RiverRight) continue;   // the bank's very edge
            bool river = x > WorldBuilder.RiverLeft && x < WorldBuilder.RiverRight;
            // The ground under x, looking past the one-way ledges above it.
            var floor = new RaycastHit2D();
            foreach (var hit in Physics2D.RaycastAll(new Vector2(x + .5f, 6), Vector2.down, 10, 1 << 8))
                if (!hit.collider.usedByEffector) { floor = hit; break; }
            Require(river || floor.collider != null && Mathf.Abs(floor.point.y - WorldBuilder.Top(x + .5f)) < .01f, "Floor at the wrong height at " + x);
            Require((floor.collider != null) != river, (river ? "Floor over the river at " : "Missing floor at ") + x);
        }
        Require(!WorldBuilder.CanDetect(new Vector2(WorldBuilder.FerryStart + 2, .2f), new Vector2(WorldBuilder.EnemyMax, .7f)), "The boss never notices him at the ferry");
        Require(WorldBuilder.GoalX > WorldBuilder.HillStart && WorldBuilder.GoalX < WorldBuilder.HillEnd, "The road to Hanyang is past the back hill");
        Require(WorldBuilder.KongLair > WorldBuilder.HillStart + KongBoss.MeetRange && WorldBuilder.KongLair < WorldBuilder.GoalX, "Kongjwi waits on the hill, between its foot and the road");
        foreach (var w in new[] { WorldBuilder.Hunjang, WorldBuilder.Yeot, WorldBuilder.Halmi })
            Require(w.x > WorldBuilder.KongStart && w.x < WorldBuilder.KongEnd && Mathf.Abs(w.x - WorldBuilder.Kongjwi.x) > 2 && Mathf.Abs(w.x - WorldBuilder.Patjwi.x) > 2, "New villagers stand apart in Kongbat");
        Require(WorldBuilder.PatjwiRaidX < WorldBuilder.Anak.x && WorldBuilder.PatjwiRaidX > WorldBuilder.KongStart, "Patjwi's refuge is the far left of the village");
        Require(WorldBuilder.Spawn.x > WorldBuilder.VillageLeft && WorldBuilder.Spawn.x < WorldBuilder.ForestStart, "Spawn must be in village");
        Require(WorldBuilder.Stone.x < WorldBuilder.VillageLeft, "Puzzle must be in west");
        Require(WorldBuilder.Elder.x > WorldBuilder.Peddler.x && WorldBuilder.Elder.x < WorldBuilder.ForestStart - 6, "Elder stands east of the peddler, inside the village");
        Require(WorldBuilder.Elder.x - WorldBuilder.Peddler.x > 3.6f, "Elder and peddler talk ranges do not overlap");
        var enemy = new Vector2(WorldBuilder.EnemyHome, .7f);
        Require(WorldBuilder.CanDetect(new Vector2(WorldBuilder.ForestStart, .2f), enemy), "Detect at forest entrance");
        Require(!WorldBuilder.CanDetect(WorldBuilder.Spawn, enemy), "Spawn must be safe");
        Require(!WorldBuilder.CanDetect(new Vector2(11.9f, .2f), new Vector2(13.2f, .7f)), "Village boundary must remain safe even near enemy");
        Require(PrototypeCombat.EnemyMaxHealth == 200, "Health must be 200");
        Require(PrototypeCombat.ElderCounterDamage * 3 >= PrototypeCombat.EnemyMaxHealth && PrototypeCombat.ElderCounterDamage * 2 < PrototypeCombat.EnemyMaxHealth, "The elder's last blow takes a third of the boss");
        foreach (var prop in new[] { "뒷산 독", "뒷산 발판", "뒷산 땅", "주막", "주막 주모", "주막 평상", "마을 기와집", "거북 받침 비석", "숲 소나무", "뱃사공 오두막", "갈대", "강물", "장승", "서낭나무", "서낭당 돌무더기", "팥쥐네 장독대", "잔치 명단", "콩밭 마을 우물", "콩밭 마을 빨랫줄" })
            Require(GameObject.Find(prop) != null && GameObject.Find(prop).GetComponent<SpriteRenderer>().sprite != null, "Prop placed: " + prop);
        Require(GameObject.Find("주막 건물") == null && GameObject.Find("숲 수관") == null, "Placeholder blocks replaced");
        ArrowChecks.Run();
        Debug.Log("WORLD_CHECKS_PASSED: continuous ground, area placement, detection and village safety");
    }
    static void Require(bool result, string label)
    {
        if (!result) throw new Exception(label);
    }
}
