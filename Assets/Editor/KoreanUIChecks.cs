using System;
using System.IO;
using UnityEngine;

public static class KoreanUIChecks
{
    public static void Run()
    {
        var font = KoreanUI.Font;
        if (font == null) throw new Exception("Korean font could not be loaded.");
        int count = 0;
        foreach (var file in new[] { "View.cs", "Combat.cs", "MainNpcs.cs", "JinaUI.cs", "Shops.cs", "Narration.cs", "Townsfolk.cs", "BossStory.cs", "GameOver.cs", "JinaItems.cs", "InventoryUI.cs", "Maps.cs", "RoadEvents.cs", "Hud.cs", "KongNightStay.cs", "JinaFx.cs", "WorldBuilder.cs", "KongBoss.cs", "KongBoss.Fight.cs", "KongAftermath.cs", "DevCheats.cs", "ActorVisuals.cs", "RoadMobs.cs", "Beacons.cs", "Hanyang.cs", "FinalBoss.cs", "FinalBoss.Fight.cs" })
            foreach (char c in File.ReadAllText("Assets/Scripts/" + file))
                // Hangul syllables plus the Hanja used in the intro narration.
                if ((c >= '\uac00' && c <= '\ud7a3') || (c >= '\u4e00' && c <= '\u9fff'))
                {
                    if (!font.HasCharacter(c)) throw new Exception("Missing Korean glyph: " + c);
                    count++;
                }
        Debug.Log("KOREAN_UI_CHECK_PASSED: " + count + " Korean characters; font=" + font.name);
    }
}
