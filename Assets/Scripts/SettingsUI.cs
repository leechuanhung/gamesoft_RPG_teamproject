using UnityEngine;

// 빌드용 설정 메뉴: Esc로 열고 닫으며 저장·불러오기·종료를 제공한다.
public class SettingsUI : MonoBehaviour
{
    private Combat combat;
    private PlayerSave save;
    private bool isOpen;
    private float previousTimeScale = 1f;
    private string message = "";

    public void Initialize(Combat owner, PlayerSave playerSave)
    {
        combat = owner;
        save = playerSave;
    }

    private void Update()
    {
        if (combat == null) return;

        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            return;
        }

        // 대화·상점·보따리 같은 창을 닫는 Esc는 메뉴를 열지 않는다. 그 창이 이 스크립트보다 먼저 닫혔을 수 있어서
        // (상점은 실행 순서가 앞선다) 지난 프레임에 멈춰 있었는지도 본다.
        bool pausedNow = combat.Paused;
        if (Input.GetKeyDown(KeyCode.Escape) && !pausedNow && !wasPaused && combat.PlayerHealth > 0)
            Open();
        wasPaused = pausedNow;
    }
    private bool wasPaused;

    private void Open()
    {
        previousTimeScale = Time.timeScale;
        if (previousTimeScale <= 0f) previousTimeScale = 1f;
        isOpen = true;
        message = "";
        combat.SettingsPauseActive = true;
        Time.timeScale = 0f;
    }

    private void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        combat.SettingsPauseActive = false;
        Time.timeScale = previousTimeScale;
    }

    private void OnDisable() => Close();

    private void OnGUI()
    {
        if (!isOpen) return;
        GUI.depth = -90;
        KoreanUI.Apply();

        const float width = 380f, height = 300f, buttonHeight = 46f, gap = 12f;
        float w = Mathf.Min(width, JinaUI.W - 32f);
        var rect = new Rect((JinaUI.W - w) * .5f, (JinaUI.H - height) * .5f, w, height);
        Rect inside = JinaUI.Window(rect, "설정", "Esc 닫기");

        float y = inside.y + 4f;
        float x = inside.x + 8f;
        float buttonWidth = inside.width - 16f;
        if (JinaUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "저장"))
            message = save != null && save.SaveGame() ? "저장했습니다." : "저장하지 못했습니다.";

        y += buttonHeight + gap;
        if (JinaUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "불러오기"))
            message = save != null && save.LoadGame() ? "불러왔습니다." : "불러올 저장 파일이 없습니다.";

        y += buttonHeight + gap;
        if (JinaUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "게임 종료"))
        {
            Debug.Log("게임을 종료합니다.");
            Application.Quit();
        }

        JinaUI.Text(new Rect(inside.x, inside.yMax - 26f, inside.width, 24f), message,
            14, string.IsNullOrEmpty(message) ? JinaUI.TextDim : JinaUI.Green, TextAnchor.MiddleCenter);
    }
}
