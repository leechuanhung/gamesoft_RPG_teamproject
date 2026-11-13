using System;
using System.IO;
using UnityEngine;

public static class KoreanUIChecks
{
    public static void Run()
    {
        var font = PrototypeKoreanUI.Font;
        if (font == null) throw new Exception("Korean font could not be loaded.");
        int count = 0;
        foreach (var file in new[] { "PrototypeView.cs", "PrototypeCombat.cs", "ElderQuest.cs", "JinaUI.cs", "Shops.cs", "Narration.cs", "VillageAlarm.cs", "BossStory.cs", "GameOver.cs", "JinaItems.cs", "InventoryUI.cs", "PrototypeMaps.cs", "RoadEvents.cs", "KongVillage.cs", "PrototypeHud.cs", "KongNightStay.cs", "JinaFx.cs", "WorldBuilder.cs", "KongBoss.cs", "KongBoss.Fight.cs", "KongAftermath.cs", "DevCheats.cs", "ActorVisuals.cs" })
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
