using UnityEngine;

public static class PrototypeKoreanUI
{
    private static Font koreanFont;
    public static Font Font
    {
        get
        {
            if (koreanFont == null)
                koreanFont = UnityEngine.Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR" }, 16);
            return koreanFont;
        }
    }
    public static void Apply()
    {
        GUI.skin.font = Font;
        GUI.skin.label.fontSize = 15;
        GUI.skin.label.wordWrap = true;
        GUI.skin.box.fontSize = 15;
        GUI.skin.box.wordWrap = true;
    }
    public static string Outcome(EncounterOutcome outcome)
    {
        switch (outcome)
        {
            case EncounterOutcome.Slain: return "도깨비 토벌";
            case EncounterOutcome.Purified: return "도깨비 정화";
            default: return "해결되지 않은 사연";
        }
    }
}
