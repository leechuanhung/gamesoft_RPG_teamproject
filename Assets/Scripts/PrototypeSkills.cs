using UnityEngine;

// 스킬 3칸 (Q·W·E). 쿨타임을 관리하고 화면 왼쪽 아래에 스킬창을 그린다.
//   Q 물러서기: 보는 방향 반대로 빠르게 물러나며 1.5초 무적
//   W 돌진베기: 보는 방향으로 돌진하고, 지나간 구간의 적에게 20 피해
//   E 비어 있음 (나중에 추가)
// PrototypeCombat.Start에서 자동으로 붙는다.
public class PrototypeSkills : MonoBehaviour
{
    public PrototypeCombat combat;

    // ── Q 물러서기 ──
    public const float RetreatCooldown = 4f, RetreatInvulnerable = 1.5f, RetreatLength = .25f, RetreatSpeed = 10f;
    // ── W 돌진베기 ──
    public const float RushCooldown = 5f, RushLength = .3f, RushSpeed = 14f;
    public const int RushDamage = 20;

    public class Slot
    {
        public string key, name;
        public float cooldown, readyAt;
        public bool empty;
    }
    public Slot[] slots;

    void Awake()
    {
        slots = new[]
        {
            new Slot { key = "Q", name = "물러서기", cooldown = RetreatCooldown },
            new Slot { key = "W", name = "돌진베기", cooldown = RushCooldown },
            new Slot { key = "E", name = "", empty = true },
        };
    }

    // R키 재시작 때 쿨타임 초기화
    public void ResetSkills()
    {
        if (slots != null) foreach (var s in slots) s.readyAt = 0;
    }

    bool CanUse => combat.PlayerHealth > 0 && !combat.Paused && combat.player.CanMove && !combat.player.Dodging;

    void Update()
    {
        if (combat == null || combat.player == null) return;
        if (!CanUse) return;
        if (Input.GetKeyDown(KeyCode.Q)) TryUse(0);
        else if (Input.GetKeyDown(KeyCode.W)) TryUse(1);
        else if (Input.GetKeyDown(KeyCode.E)) TryUse(2);
    }

    void TryUse(int index)
    {
        var s = slots[index];
        if (s.empty || Time.time < s.readyAt) return;
        var p = combat.player;
        switch (index)
        {
            case 0:   // Q 물러서기: 베지 않는다
                p.StartDash(-p.Facing, RetreatLength, RetreatSpeed, false);
                combat.GrantSkillInvulnerable(RetreatInvulnerable);
                break;
            case 1:   // W 돌진베기: 피해는 돌진하는 동안 DashSlashAttack이 준다(적마다 한 번)
                p.StartDash(p.Facing, RushLength, RushSpeed, true);
                break;
        }
        s.readyAt = Time.time + s.cooldown;
    }

    // ── 화면 왼쪽 아래 스킬창 ──
    void OnGUI()
    {
        if (combat == null || combat.PlayerHealth <= 0 || slots == null) return;
        var oldMatrix = GUI.matrix;
        var oldColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;

        float size = 64, gap = 8, marginX = 20, marginY = 20;
        float y = Screen.height - marginY - size;
        var keyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = 13, fontStyle = FontStyle.Bold };
        var nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = 11 };
        var cdStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            var r = new Rect(marginX + i * (size + gap), y, size, size);

            // 금빛 테두리와 바탕
            GUI.color = new Color(.8f, .65f, .35f, .9f);
            GUI.DrawTexture(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), Texture2D.whiteTexture);
            GUI.color = s.empty ? new Color(.12f, .12f, .12f, .9f) : new Color(.18f, .16f, .22f, .95f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);

            // 쿨타임: 남은 비율만큼 위에서부터 어둡게 덮고 남은 초 표시
            float remain = s.readyAt - Time.time;
            if (!s.empty && remain > 0)
            {
                float ratio = Mathf.Clamp01(remain / s.cooldown);
                GUI.color = new Color(0, 0, 0, .65f);
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height * ratio), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(r, Mathf.CeilToInt(remain).ToString(), cdStyle);
            }

            // 키 글자(왼쪽 위)와 스킬 이름(아래)
            GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 5, r.y + 3, r.width, 18), s.key, keyStyle);
            GUI.Label(new Rect(r.x, r.y, r.width, r.height - 4), s.empty ? "비어 있음" : s.name, nameStyle);
        }

        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }
}