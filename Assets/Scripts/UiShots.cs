#if UI_SHOTS
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

// Screenshots of every window and notice, which the camera captures in JinaPlayChecks cannot see (IMGUI draws only in
// a real player). Compiled only into the build made by Tools/run_ui_shots.sh; it puts the game into each state, saves
// <width>x<height>_<name>.png into the folder after -uishots, at each screen size, and quits.
public class UiShots : MonoBehaviour
{
    PrototypeCombat combat;
    string folder;
    int width, height;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        var args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-uishots");
        if (at < 0 || at + 1 >= args.Length) return;
        var shots = new GameObject("UI shots").AddComponent<UiShots>();
        shots.folder = args[at + 1];
        DontDestroyOnLoad(shots.gameObject);
    }

    IEnumerator Start()
    {
        Application.runInBackground = true;
        Directory.CreateDirectory(folder);
        while ((combat = FindObjectOfType<PrototypeCombat>()) == null || combat.Ending == null || combat.Hud == null) yield return null;
        width = 1280; height = 720;
        yield return Size(width, height);
        yield return new WaitForSecondsRealtime(3.5f);
        yield return Shot("intro");
        combat.Intro.Skip();
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(960, 540), new Vector2Int(1920, 1080) })
        {
            width = size.x; height = size.y;
            yield return Size(width, height);
            yield return All();
        }
        Application.Quit();
    }

    IEnumerator Size(int w, int h)
    {
        Screen.SetResolution(w, h, FullScreenMode.Windowed);
        yield return new WaitForSecondsRealtime(.6f);
    }
    IEnumerator Shot(string name)
    {
        yield return new WaitForSecondsRealtime(.35f);
        // Read back after the frame, once IMGUI has drawn over the camera.
        yield return new WaitForEndOfFrame();
        var image = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        image.Apply();
        // This project's player has no PNG encoder module: raw pixels as a PPM, which run_ui_shots.sh converts.
        using (var file = File.Create(Path.Combine(folder, width + "x" + height + "_" + name + ".ppm")))
        {
            var header = System.Text.Encoding.ASCII.GetBytes("P6 " + image.width + " " + image.height + " 255 ");
            file.Write(header, 0, header.Length);
            var raw = image.GetRawTextureData();
            for (int row = image.height - 1; row >= 0; row--) file.Write(raw, row * image.width * 3, image.width * 3);
        }
        Destroy(image);
    }

    static FieldInfo Find(object o, string name)
    {
        for (var t = o.GetType(); t != null; t = t.BaseType)
        {
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ?? t.GetField("<" + name + ">k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) return f;
        }
        throw new System.MissingFieldException(o.GetType().Name, name);
    }
    static void Set(object o, string name, object value) => Find(o, name).SetValue(o, value);
    static void Call(object o, string name, params object[] args) =>
        o.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(o, args);

    void Fresh(float x, bool quiet = true)
    {
        combat.Restart();
        Time.timeScale = 1;
        if (quiet) Set(combat, "invulnerableUntil", Time.time + 10000);
        Set(combat.Encounter, "played", true); Set(combat.Encounter, "finished", true); Set(combat.Encounter, "startAt", -1000f);
        Move(x);
    }
    void Move(float x) { combat.player.transform.position = new Vector3(x, .3f, 0); combat.player.GetComponent<Rigidbody2D>().velocity = Vector2.zero; combat.Maps.Sync(); }
    void Solve() { combat.Quest.progress.Resolve(false); Set(combat, "EnemyHealth", 0); }

    IEnumerator All()
    {
        var q = combat.Quest;
        // The HUD
        Fresh(0);
        yield return Shot("hud_village");
        Set(combat.Hud, "GuideOpen", false);
        yield return Shot("hud_guide_closed");
        Set(combat.Hud, "GuideOpen", true);
        Fresh(PrototypeWorld.Elder.x - 1);
        q.progress.HearClue(); q.progress.ChooseSymbol(2); q.progress.ChooseSymbol(1); q.progress.ChooseSymbol(3);
        combat.GiveWard(); combat.player.HasteUntil = Time.time + 15;
        yield return Shot("hud_charms_prompt");

        // The inventory: the hairpin that was cut off, food, an arrow bundle, and empty
        Fresh(0);
        var bag = JinaItems.Inventory; var ui = bag.GetComponent<InventoryUI>();
        bag.TryAdd(JinaItems.Keepsake);
        foreach (var item in JinaItems.All) bag.TryAdd(item, 3);
        Call(ui, "OpenInventory");
        yield return Shot("inventory_hairpin");
        ui.UseSelected();
        yield return Shot("inventory_hairpin_used");
        for (int i = 1; i < bag.Slots.Count; i++)
        {
            Set(ui, "selected", i); ui.UseSelected();
            yield return Shot("inventory_item" + i);
        }
        Call(ui, "CloseInventory");
        bag.Clear();
        Call(ui, "OpenInventory");
        yield return Shot("inventory_empty");
        Call(ui, "CloseInventory");

        // The shops
        Fresh(PrototypeWorld.Peddler.x - 1);
        yield return Shot("peddler_prompt");
        Set(combat.Peddler, "IsOpen", true); Set(combat.Peddler, "message", "등짐 진 보부상이오. 화살도 있고, 먼 길에 요긴한 것들도 챙겨 왔소.");
        yield return Shot("peddler");
        Fresh(PrototypeWorld.Peddler.x - 1);
        q.progress.SacrificeElder();
        Set(combat.Peddler, "IsOpen", true); Set(combat.Peddler, "message", "노인장 일은 들었소. 흉한 일이 난 마을이라 짐을 다 풀 수가 없구려. 오늘은 낱개로만 파오.");
        yield return Shot("peddler_mourning");
        Fresh(PrototypeWorld.Tavern.x - 1);
        Set(combat.Tavern, "IsOpen", true); Set(combat.Tavern, "message", "어서 오시오, 길손. 따뜻한 국밥 한 그릇 드시고 가구려.");
        yield return Shot("tavern");
        Fresh(PrototypeWorld.Tavern.x - 1);
        q.progress.HearClue(); q.progress.ChooseSymbol(2); q.progress.ChooseSymbol(1); q.progress.ChooseSymbol(3); q.progress.Resolve(true);
        Set(combat.Tavern, "IsOpen", true); Set(combat.Tavern, "message", "도깨비를 달래셨다지요? 오늘은 귀한 삼계탕을 내어 드리리다.");
        yield return Shot("tavern_special");

        // Talk and reading
        Fresh(PrototypeWorld.Elder.x - 1);
        Set(q, "IsDialogOpen", true); Set(q, "shrineDialog", false);
        Set(q, "message", "숲의 도깨비는 본디 산을 지키던 주인이었소. 망한 나라의 원한이 씌어 저리 되었으니, 서쪽 비석 터의 옛 비석을 찾아가 보시오. 달이 먼저요, 그다음이 강, 마지막이 산이라오.");
        yield return Shot("elder_bubble");
        Fresh(PrototypeWorld.Stone.x + 1);
        q.progress.HearClue();
        Set(q, "IsDialogOpen", true); Set(q, "shrineDialog", true);
        Set(q, "message", "이끼 낀 비석에 세 개의 문양이 새겨져 있다. 달은 강을 비추고, 강은 산을 돌아 흐른다.\n노인의 말대로 차례를 맞추면 무언가 드러날 것 같다.\n비문의 나머지는 닳아 읽을 수 없다. 오래전 이 땅을 다스리던 왕의 이름이 있던 자리인 듯하다.");
        yield return Shot("stele_puzzle");
        Fresh(PrototypeWorld.Elder.x - 1);
        q.progress.SacrificeElder();
        Set(q, "IsDialogOpen", true); Set(q, "shrineDialog", false);
        Set(q, "message", "피 묻은 쪽지가 떨어져 있다.\n선비에게. 서쪽 비석에 달, 강, 산의 차례로 손을 얹으면 부적이 나올 것이오. 부디 그 아이를 베지 말고 풀어 주시오.");
        yield return Shot("elder_note");

        // The forest: the thorns' prompt; every notice at once under the boss's bar
        Fresh(PrototypeWorld.FerryStart - 1.5f);
        yield return Shot("thorns_prompt");
        Fresh(40);
        Set(combat, "alerted", true);
        Set(combat, "WardBrokeAt", Time.time + 2); Set(combat, "CoinsFoundAt", Time.time + 2); Set(combat, "CoinsFound", 4); Set(combat, "KeepsakeFreedAt", Time.time + 2);
        Set(combat, "noArrowsUntil", Time.time + 5);
        yield return Shot("toasts_bossbar");

        // Cinematics
        Fresh(30);
        combat.Encounter.Play();
        yield return new WaitForSecondsRealtime(2.2f);
        yield return Shot("encounter");
        Fresh(0);
        Set(combat.Maps, "EnteredAt", Time.time);
        yield return new WaitForSecondsRealtime(.4f);
        yield return Shot("map_card");
        Fresh(0, false);
        Set(combat, "PlayerHealth", 1);
        Set(combat, "invulnerableUntil", 0f);
        combat.HurtPlayer();
        yield return new WaitForSecondsRealtime(PrototypeGameOver.TextAt + 2.4f);
        yield return Shot("gameover");

        // Across the river
        Fresh(PrototypeWorld.Ferryman.x + 1); Solve();
        combat.Ferry.Open();
        yield return Shot("ferry_talk");
        Fresh(PrototypeWorld.Ferryman.x + 1); Solve();
        yield return Shot("ferry_prompt");
        Fresh(PrototypeWorld.Kongjwi.x - 3); Solve();
        Set(combat.Kong, "SceneAt", Time.time);
        yield return Shot("kong_scene");
        Fresh(PrototypeWorld.JangdokX); Solve();
        combat.Kong.WakeUp();
        combat.Kong.ExamineCrocks();
        yield return Shot("kong_note_prompt");
        Fresh(PrototypeWorld.Patjwi.x + 1); Solve();
        combat.Kong.Open(PrototypeKongVillage.Who.Patjwi);
        yield return Shot("kong_patjwi");
        Fresh(PrototypeWorld.Patjwi.x + 1); Solve();
        combat.Kong.WakeUp();
        combat.Kong.Open(PrototypeKongVillage.Who.Patjwi); combat.Kong.Next();
        yield return Shot("kong_patjwi_morning");
        Fresh(PrototypeWorld.Kongjwi.x - 1); Solve();
        Set(combat.Kong, "Stayed", true); Set(combat.Kong, "HeardPatjwi", true); Set(combat.Kong, "SawCrocks", true); Set(combat.Kong, "ReadList", true);
        combat.Kong.Open(PrototypeKongVillage.Who.Kongjwi); combat.Kong.Next();
        yield return Shot("kong_unmasked");
        // The night at the house
        Fresh(PrototypeWorld.Kongjwi.x - 2.5f); Solve();
        var stay = combat.Stay;
        stay.Begin();
        for (int i = 0; i < PrototypeKongStay.Evening.Length; i += 1)
        {
            if (i == 0 || i == 1 || i == 3 || i == 5) yield return Shot("stay_evening" + i);
            stay.NextLine();
        }
        foreach (float t in new[] { 2f, 6f, 9f, 11f, 13.5f, 18.5f })
        {
            stay.Show(PrototypeKongStay.Phase.Night, t);
            yield return Shot("stay_night" + t);
        }
        foreach (float t in new[] { 2f, 5f })
        {
            stay.Show(PrototypeKongStay.Phase.Morning, t);
            yield return Shot("stay_morning" + t);
        }
        stay.Wake();
        yield return new WaitForSecondsRealtime(1f);
        yield return Shot("stay_awake");
        Move(PrototypeWorld.FeastListX);
        yield return Shot("kong_paper_prompt");
        combat.Kong.ReadFeastList();
        yield return Shot("kong_paper");
        Fresh(PrototypeWorld.ShrineX); Solve();
        combat.player.transform.position = new Vector3(PrototypeWorld.ShrineX, PrototypeWorld.Top(PrototypeWorld.ShrineX) + .8f, 0);
        yield return Shot("shrine_prompt");
        combat.Pass.UseShrine();
        yield return Shot("shrine_toast");

        // The ending's title
        Fresh(0); Solve();
        combat.Ending.Play(PrototypeEnding.Kind.Slain);
        combat.Ending.Skip();
        yield return new WaitForSecondsRealtime(1.4f);
        yield return Shot("ending_title");
        Fresh(0);
    }
}
#endif
