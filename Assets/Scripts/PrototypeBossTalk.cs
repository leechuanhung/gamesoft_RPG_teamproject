using UnityEngine;

// Once purified, the dokkaebi can be spoken to (E): it tells how the ghosts of the fallen kingdom cursed it.
// If it had killed the elder it only asks to be left beside him. Pages advance with E; Esc closes.
public class PrototypeBossTalk : MonoBehaviour
{
    public PrototypeCombat combat;
    static readonly string[] Story =
    {
        "고맙소, 선비. 오래 꾼 악몽에서 막 깨어난 듯하오.",
        "나는 본디 이 산을 지키던 도깨비였소. 산짐승을 돌보고, 길 잃은 나그네를 마을까지 데려다주곤 했지.",
        "그런데 망한 나라의 망령들이 서쪽 비석 터에 모여들더니, 나라 잃은 원한을 내게 덧씌웠소.",
        "그 뒤로는 사람 냄새만 맡아도 분노가 끓었소. 내가 무엇을 지키던 자였는지조차 잊고 말았지.",
        "비석 터의 요괴들도 그 망령들의 졸개였소. 선비 덕에 이 산이 다시 숨을 쉬겠구려.",
        "한양 가는 길이라 하셨소? 이 산길만큼은 내가 지켜 드리리다. 부디 뜻을 이루시오.",
    };
    static readonly string[] Remorse = { "…나를 내버려 두시오. 노인장 곁에 조금만 더 있겠소." };

    public bool IsOpen { get; private set; }
    public int Page { get; private set; }
    string[] Pages => combat.Quest.progress.ElderDead ? Remorse : Story;
    // Purified, the purification (and any grieving words) over, and the seonbi standing close by.
    public bool Near => combat != null && combat.Quest != null && combat.BossPurified && combat.PlayerHealth > 0
        && Time.time >= combat.PurifiedAt + BossVisual.PurifyLength
        && (combat.Remorse == null || !combat.Remorse.Active || combat.Remorse.Elapsed > BossRemorse.Spoken)
        && Mathf.Abs(combat.player.transform.position.x - combat.BossPosition.x) < 2.4f;

    public void ResetTalk() { IsOpen = false; Page = 0; }
    // Opens the talk, or turns to the next page (closing after the last).
    public void Talk()
    {
        if (!IsOpen) { IsOpen = true; Page = 0; combat.player.CanMove = false; return; }
        if (++Page >= Pages.Length) Close();
    }
    void Close() { IsOpen = false; combat.player.CanMove = combat.PlayerHealth > 0; }

    void Update()
    {
        if (combat == null) return;
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            else if (Input.GetKeyDown(KeyCode.E)) Talk();
        }
        else if (Near && !combat.Paused && Input.GetKeyDown(KeyCode.E)) Talk();
    }

    void OnGUI()
    {
        if (combat == null) return;
        PrototypeKoreanUI.Apply();
        if (!IsOpen)
        {
            if (Near && !combat.Paused)
                GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height - 55, 440, 35), "E: 도깨비와 이야기하기");
            return;
        }
        var pages = Pages;
        string hint = (Page + 1) + " / " + pages.Length + "   ·   E: " + (Page + 1 < pages.Length ? "다음" : "닫기") + "   Esc: 닫기";
        SpeechBubble.Draw(new Vector3(combat.BossPosition.x, 2.3f, 0), "도깨비", pages[Page], hint);
    }
}
