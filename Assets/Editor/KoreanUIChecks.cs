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
        foreach (var file in new[] { "PrototypeView.cs", "PrototypeCombat.cs", "PrototypeQuest.cs", "PrototypeKoreanUI.cs", "PrototypeTavern.cs" })
            foreach (char c in File.ReadAllText("Assets/Scripts/" + file))
                if (c >= '\uac00' && c <= '\ud7a3')
                {
                    if (!font.HasCharacter(c)) throw new Exception("Missing Korean glyph: " + c);
                    count++;
                }
        Debug.Log("KOREAN_UI_CHECK_PASSED: " + count + " Korean characters; font=" + font.name);
    }
}
