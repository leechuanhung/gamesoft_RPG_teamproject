using System;
using UnityEngine;
using UnityEngine.Events;

// 한 칸씩 걷고 비석을 미는 격자형 미니게임. 이미지가 연결되지 않은 동안은 색 도형으로 표시한다.
public class SeonbiGridPuzzle : MonoBehaviour
{
    public Combat combat;
    [Header("이미지 리소스 (선택 사항, 비워 두면 도형으로 표시)")]
    public Texture2D playerImage, steleImage, goalImage, wallImage;
    [Header("보상 연결")]
    public UnityEvent onTutorialSolved = new UnityEvent();
    public UnityEvent onMainPuzzleSolved = new UnityEvent();

    public bool IsOpen { get; private set; }
    public bool TutorialSolved { get; private set; }
    public bool MainPuzzleSolved { get; private set; }
    public bool IsMainPuzzle { get; private set; }

    const int Tutorial = 0, MainPuzzle = 1;
    const float Tile = 58f;
    static readonly string[] TutorialMap = { "#####", "#...#", "#...#", "#...#", "#####" };
    static readonly string[] MainMap = { "#######", "#.....#", "#..#..#", "#.....#", "#.....#", "#######" };

    bool[,] walls;
    Vector2Int player;
    Vector2Int[] stones, goals;
    int width, height;
    int activePuzzle;
    string notice = "";
    float noticeUntil = -1;
    GUIStyle button;

    public void OpenTutorial() => Open(Tutorial);
    public void OpenMainPuzzle() => Open(MainPuzzle);

    void Open(int puzzle)
    {
        if ((puzzle == Tutorial && TutorialSolved) || (puzzle == MainPuzzle && MainPuzzleSolved))
        {
            return;
        }
        activePuzzle = puzzle;
        IsMainPuzzle = puzzle == MainPuzzle;
        BuildBoard(puzzle);
        IsOpen = true;
        if (combat != null && combat.player != null) combat.player.CanMove = false;
    }

    void BuildBoard(int puzzle)
    {
        string[] map = puzzle == Tutorial ? TutorialMap : MainMap;
        height = map.Length; width = map[0].Length;
        walls = new bool[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++) walls[x, y] = map[y][x] == '#';

        if (puzzle == Tutorial)
        {
            player = new Vector2Int(1, 3);
            stones = new[] { new Vector2Int(2, 3) };
            goals = new[] { new Vector2Int(3, 3) };
        }
        else
        {
            player = new Vector2Int(1, 4);
            stones = new[] { new Vector2Int(2, 2), new Vector2Int(4, 2) };
            goals = new[] { new Vector2Int(5, 1), new Vector2Int(4, 4) };
        }
    }

    void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }

        Vector2Int dir = Vector2Int.zero;
        // 화면 행은 아래로 갈수록 커지므로 상하 입력을 행 좌표에 맞춰 뒤집는다.
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) dir = new Vector2Int(0, -1);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) dir = new Vector2Int(0, 1);
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) dir = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) dir = Vector2Int.right;
        if (dir != Vector2Int.zero) Move(dir);
    }

    void Move(Vector2Int dir)
    {
        Vector2Int next = player + dir;
        if (!Inside(next) || walls[next.x, next.y]) return;
        int stone = StoneAt(next);
        if (stone >= 0)
        {
            Vector2Int beyond = next + dir;
            if (!Inside(beyond) || walls[beyond.x, beyond.y] || StoneAt(beyond) >= 0) return;
            stones[stone] = beyond;
        }
        player = next;
        if (Solved())
        {
            if (activePuzzle == Tutorial)
            {
                TutorialSolved = true;
                onTutorialSolved.Invoke();
                notice = "연습 완료! 이제 두 비석을 문양 칸에 놓으세요.";
                noticeUntil = Time.unscaledTime + 2.5f;
                Open(MainPuzzle);
            }
            else
            {
                Close();
                MainPuzzleSolved = true;
                onMainPuzzleSolved.Invoke();
            }
        }
    }

    public void ResetPuzzles()
    {
        Close();
        TutorialSolved = MainPuzzleSolved = false;
        notice = "";
        noticeUntil = -1;
    }

    void RestartCurrentPuzzle()
    {
        BuildBoard(activePuzzle);
        notice = "";
        noticeUntil = -1;
    }

    void Close()
    {
        IsOpen = false;
        if (combat != null && combat.player != null)
            combat.player.CanMove = combat.PlayerHealth > 0 && !combat.Paused;
    }

    bool Inside(Vector2Int p) => p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;
    int StoneAt(Vector2Int p) { for (int i = 0; i < stones.Length; i++) if (stones[i] == p) return i; return -1; }
    bool IsGoal(Vector2Int p) { for (int i = 0; i < goals.Length; i++) if (goals[i] == p) return true; return false; }
    bool Solved() { foreach (var goal in goals) if (StoneAt(goal) < 0) return false; return true; }

    void OnGUI()
    {
        if (!IsOpen)
        {
            return;
        }
        KoreanUI.Apply();
        float boardW = width * Tile, boardH = height * Tile;
        float panelW = Mathf.Max(boardW + 48, 440), panelH = boardH + 154;
        Rect panel = new Rect((JinaUI.W - panelW) / 2, (JinaUI.H - panelH) / 2, panelW, panelH);
        JinaUI.Fill(new Rect(0, 0, JinaUI.W, JinaUI.H), new Color(.02f, .015f, .01f, .78f));
        JinaUI.HudPanel(panel);
        Text(new Rect(panel.x + 20, panel.y + 13, panelW - 40, 30), IsMainPuzzle ? "두 비석의 봉인" : "비석 퍼즐 · 연습", 20, JinaUI.Gold, TextAnchor.MiddleCenter, true);
        string subtitle = Time.unscaledTime < noticeUntil ? notice : IsMainPuzzle ? "두 비석을 문양 칸에 모두 올려놓으세요." : "비석을 밀어 문양 칸에 올려놓으세요.";
        Text(new Rect(panel.x + 18, panel.y + 45, panelW - 36, 25), subtitle, 13, Time.unscaledTime < noticeUntil ? JinaUI.Gold : JinaUI.Bright, TextAnchor.MiddleCenter);

        float startX = panel.x + (panelW - boardW) / 2;
        float startY = panel.y + 78;
        DrawBoard(startX, startY);
        if (GUI.Button(new Rect(panel.x + 18, panel.y + panelH - 48, 104, 30), "다시 시작", ButtonStyle())) RestartCurrentPuzzle();
        Text(new Rect(panel.x + 126, panel.y + panelH - 52, panelW - 142, 38), "이동: 방향키 / WASD     닫기: Esc", 13, JinaUI.Bright, TextAnchor.MiddleCenter);
        if (GUI.Button(new Rect(panel.x + panelW - 88, panel.y + 10, 72, 28), "닫기", ButtonStyle())) Close();
    }

    void DrawBoard(float x, float y)
    {
        for (int row = 0; row < height; row++)
        for (int col = 0; col < width; col++)
        {
            var r = new Rect(x + col * Tile, y + row * Tile, Tile - 2, Tile - 2);
            JinaUI.Fill(r, walls[col, row] ? new Color(.18f, .13f, .1f) : new Color(.77f, .68f, .52f));
            if (walls[col, row] && wallImage != null) DrawImage(wallImage, r);
            if (!walls[col, row] && IsGoal(new Vector2Int(col, row)))
            {
                if (goalImage != null) DrawImage(goalImage, Inset(r, 12));
                else
                {
                    JinaUI.Fill(Inset(r, 7), new Color(.82f, .62f, .2f));
                    JinaUI.Fill(Inset(r, 12), new Color(.77f, .68f, .52f));
                }
            }
            int box = StoneAt(new Vector2Int(col, row));
            if (box >= 0)
            {
                Rect shape = Inset(r, 8);
                if (steleImage != null) DrawImage(steleImage, shape);
                else
                {
                    JinaUI.Fill(shape, box == 0 ? new Color(.48f, .27f, .16f) : new Color(.35f, .33f, .39f));
                    JinaUI.Fill(Inset(shape, 7), new Color(.75f, .59f, .36f));
                }
            }
            if (player == new Vector2Int(col, row))
            {
                Rect shape = Inset(r, 11);
                if (playerImage != null) DrawImage(playerImage, shape);
                else
                {
                    JinaUI.Fill(shape, new Color(.14f, .31f, .48f));
                    JinaUI.Fill(new Rect(shape.x + shape.width * .34f, shape.y + shape.height * .2f, shape.width * .32f, shape.height * .32f), JinaUI.PaperLight);
                }
            }
        }
    }

    static Rect Inset(Rect r, float amount) => new Rect(r.x + amount, r.y + amount, r.width - amount * 2, r.height - amount * 2);
    static void DrawImage(Texture2D image, Rect r) { if (image != null) GUI.DrawTexture(r, image, ScaleMode.ScaleToFit, true); }
    void Text(Rect r, string value, int size, Color color, TextAnchor align, bool bold = false) => JinaUI.Text(r, value, size, color, align, bold);
    GUIStyle ButtonStyle()
    {
        if (button == null) button = new GUIStyle(GUI.skin.button);
        button.font = KoreanUI.Font; button.fontSize = 13;
        return button;
    }
}
