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
        foreach (var file in new[] { "PrototypeView.cs", "PrototypeCombat.cs", "PrototypeQuest.cs", "JinaUI.cs", "PrototypeShops.cs", "PrototypeNarration.cs", "PrototypeVillage.cs", "PrototypeBossStory.cs", "PrototypeGameOver.cs", "JinaItems.cs", "InventoryUI.cs", "PrototypeMaps.cs", "PrototypeRoad.cs", "PrototypeKongVillage.cs", "PrototypeHud.cs", "PrototypeKongStay.cs", "JinaFx.cs", "PrototypeWorld.cs", "PrototypeKongBoss.cs", "PrototypeKongBoss.Fight.cs", "PrototypeKongAftermath.cs", "PrototypeCheats.cs", "ActorVisuals.cs" })
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
