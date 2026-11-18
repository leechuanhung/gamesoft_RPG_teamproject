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
        world.Build(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Square.png"));
        Physics2D.SyncTransforms();
        // 같은 높이로 이어진 땅은 충돌체 하나여야 한다: 이음매가 있으면 걷던 선비가 옆 조각의 모서리에 걸린다.
        Collider2D lastFloor = null; float lastTop = 0;
        for (int x = -69; x < (int)WorldBuilder.WorldEnd; x++)
        {
            if (x == (int)WorldBuilder.RiverLeft || x == (int)WorldBuilder.RiverRight) continue;   // the bank's very edge
            bool river = x > WorldBuilder.RiverLeft && x < WorldBuilder.RiverRight;
            // The ground under x, looking past the one-way ledges above it.
            var floor = new RaycastHit2D();
            foreach (var hit in Physics2D.RaycastAll(new Vector2(x + .5f, 6), Vector2.down, 10, 1 << 8))
                if (!hit.collider.usedByEffector) { floor = hit; break; }
            Require(river || floor.collider != null && Mathf.Abs(floor.point.y - WorldBuilder.Top(x + .5f)) < .01f, "Floor at the wrong height at " + x);
            Require((floor.collider != null) != river, (river ? "Floor over the river at " : "Missing floor at ") + x);
            Require(floor.collider == null || lastFloor == null || Mathf.Abs(floor.point.y - lastTop) > .01f || floor.collider == lastFloor, "Seam in level ground at " + x);
            lastFloor = floor.collider; lastTop = floor.point.y;
        }
        Require(!WorldBuilder.CanDetect(new Vector2(WorldBuilder.FerryStart + 2, .2f), new Vector2(WorldBuilder.EnemyMax, .7f)), "The boss never notices him at the ferry");
        Require(WorldBuilder.GoalX > WorldBuilder.ManorStart && WorldBuilder.GoalX < WorldBuilder.WorldEnd, "The road to the examination is past the councillor's house");
        Require(WorldBuilder.OfficialX > WorldBuilder.ManorStart + FinalBoss.MeetRange && WorldBuilder.OfficialX < WorldBuilder.GoalX - 2, "The councillor waits between the gate of his house and the road");
        Require(WorldBuilder.FortGateX > BeaconFort.All[BeaconFort.All.Length - 1].x && WorldBuilder.FortGateX < WorldBuilder.FortEnd, "The fortress gate is past every beacon");
        Require(WorldBuilder.KongLair > WorldBuilder.HillStart + KongBoss.MeetRange && WorldBuilder.KongLair < WorldBuilder.HillEnd, "Kongjwi waits on the hill, between its foot and the fortress");
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
        Require(Combat.EnemyMaxHealth == 200, "Health must be 200");
        Require(Combat.ElderCounterDamage * 3 >= Combat.EnemyMaxHealth && Combat.ElderCounterDamage * 2 < Combat.EnemyMaxHealth, "The elder's last blow takes a third of the boss");
        foreach (var prop in new[] { "봉수 군령판", "산성 우물", "버려진 역참", "무덤", "한양 가게", "홍살문", "좌의정 댁 솟을대문", "석등", "좌의정 댁 누각", "뒷산 독", "뒷산 발판", "뒷산 땅", "주막", "주막 주모", "주막 평상", "마을 기와집", "거북 받침 비석", "숲 소나무", "뱃사공 오두막", "갈대", "강물", "장승", "서낭나무", "서낭당 돌무더기", "팥쥐네 장독대", "잔치 명단", "콩밭 마을 우물", "콩밭 마을 빨랫줄" })
            Require(GameObject.Find(prop) != null && GameObject.Find(prop).GetComponent<SpriteRenderer>().sprite != null, "Prop placed: " + prop);
        Require(GameObject.Find("주막 건물") == null && GameObject.Find("숲 수관") == null, "Placeholder blocks replaced");
        BakeChecks();
        ArrowChecks.Run();
        Debug.Log("WORLD_CHECKS_PASSED: continuous ground, area placement, detection and village safety, baking into the scene");
    }

    // 씬에 굽기(WorldBake): 무리별로 묶이고, 그림은 저장할 때 비웠다가 다시 입혀지고, 다시 구워도 옮긴 기물과
    // 새로 넣은 오브젝트가 남고, 실행할 때는 구운 세계를 쓴다(둘을 짓지 않는다).
    static void BakeChecks()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Square.png");
        var world = WorldBake.Bake(square);
        Require(world.Baked && world.BakedLayout == WorldBuilder.Layout && WorldBake.Find() == world, "The baked world is marked");
        foreach (var g in new[] { WorldBuilder.GroundGroup, WorldBuilder.BackdropGroup, WorldBuilder.PropGroup, WorldBuilder.LedgeGroup })
            Require(world.transform.Find(g) != null && world.transform.Find(g).childCount > 0, "Baked group " + g);
        // Every component saved with the world must live in a file of its own name, or a built game reads the scene wrongly.
        foreach (var mb in world.GetComponentsInChildren<MonoBehaviour>(true))
        {
            var script = MonoScript.FromMonoBehaviour(mb);
            Require(script != null && script.GetClass() == mb.GetType() && AssetDatabase.Contains(script), "Baked component in a file of its own name: " + mb.GetType().Name);
        }
        var tavern = GameObject.Find("주막");
        Require(tavern.transform.parent.name == WorldBuilder.PropGroup && tavern.GetComponent<WorldArt>() != null, "Props are grouped and know their art");
        Require(GameObject.Find("바위 발판").transform.parent.name == WorldBuilder.LedgeGroup && GameObject.Find("원경 village").transform.parent.name == WorldBuilder.BackdropGroup, "Ledges and backdrops have their own groups");
        var arts = UnityEngine.Object.FindObjectsOfType<WorldArt>();
        foreach (var a in arts) a.Clear();
        Require(tavern.GetComponent<SpriteRenderer>().sprite == null, "Saving empties the pictures");
        foreach (var a in arts) a.Apply();
        Require(Array.TrueForAll(arts, a => a.GetComponent<SpriteRenderer>().sprite != null), "…and every picture comes back");
        var land = GameObject.Find("지면 village").GetComponent<SpriteRenderer>();
        Require(land.drawMode == SpriteDrawMode.Tiled && Mathf.Abs(land.size.x - 40) < .01f, "Tiled ground keeps its width");
        // Moved and added things survive a rebake.
        tavern.transform.position += Vector3.right * 3;
        var added = UnityEngine.Object.Instantiate(GameObject.Find("마을 기와집"), world.transform.Find(WorldBuilder.PropGroup));
        added.name = "새 기와집";
        world = WorldBake.Bake(square);
        Require(Mathf.Abs(GameObject.Find("주막").transform.position.x - 14) < .01f, "Rebaking keeps a moved prop where it was moved");
        Require(GameObject.Find("새 기와집") != null && GameObject.Find("새 기와집").transform.parent.parent == world.transform, "Rebaking keeps a prop added in the scene");
        Require(UnityEngine.Object.FindObjectsOfType<WorldBuilder>().Length == 1 && Count("주막") == 1, "One world after rebaking");
        // Playing uses the baked world and builds no second one.
        Require(WorldBuilder.Ensure(square) == world && UnityEngine.Object.FindObjectsOfType<WorldBuilder>().Length == 1 && Count("마을 기와집") == 1, "Playing uses the baked world");
        Physics2D.SyncTransforms();
        Require(Physics2D.Raycast(new Vector2(.5f, 6), Vector2.down, 10, 1 << 8).collider != null, "The baked ground can be stood on");
    }
    static int Count(string name) { int n = 0; foreach (var t in UnityEngine.Object.FindObjectsOfType<Transform>()) if (t.name == name) n++; return n; }
    static void Require(bool result, string label)
    {
        if (!result) throw new Exception(label);
    }
}
