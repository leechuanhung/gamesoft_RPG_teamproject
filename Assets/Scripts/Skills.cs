using UnityEngine;

// 스킬 3칸 (Q·W·E). 쿨타임을 관리하고 화면 왼쪽 아래에 스킬창을 그린다.
//   Q 물러서기: 보는 방향 반대로 빠르게 물러나며 1.5초 무적
//   W 돌진베기: 보는 방향으로 돌진하고, 지나간 구간의 적에게 20 피해
//   E 비어 있음 (나중에 추가)
// Combat.Start에서 자동으로 붙는다.
public class Skills : MonoBehaviour
{
    public Combat combat;

    // 스킬 아이콘: 칸이 아니라 스킬 ID로 찾음 → Q/W/E를 바꿔 끼워도 아이콘이 스킬을 따라감
    //   그림 위치: Resources/JinaV4/26_Skills/skill_스킬ID.png (예: skill_rush_slash)
    static readonly System.Collections.Generic.Dictionary<string, Sprite> icons = new System.Collections.Generic.Dictionary<string, Sprite>();
    public static Sprite IconOf(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;
        if (!icons.TryGetValue(skillId, out var icon))
        {
            // 그림이 없는 스킬은 아이콘 없이 이름만 표시
            try { icon = JinaArt.Load("26_Skills/skill_" + skillId); } catch { icon = null; }
            icons[skillId] = icon;
        }
        return icon;
    }

    // ── Q 물러서기 ──
    public const float RetreatCooldown = 4f, RetreatInvulnerable = 1.5f, RetreatLength = .25f, RetreatSpeed = 10f;
    // ── W 돌진베기 ──
    public const float RushCooldown = 5f, RushLength = .3f, RushSpeed = 18f;
    public const int RushDamage = 20;
    // ── E 강궁: 다음 화살 한 발 공격력 2배 (활을 들고 있을 때만) ──
    public const float PowerShotCooldown = 8f;
    public const int PowerShotMultiplier = 2;

// ── 스테미너 소모량 ──
    public const int AttackStamina = 10;    // 기본 공격 (Combat에서 사용)
    public const int RetreatStamina = 20;   // 물러서기
    public const int RushStamina = 30;      // 돌진베기
    public const int PowerShotStamina = 25; // 강궁
    
    public class Slot
    {
        public string key, name;
        public string skillId; // 장착된 스킬의 고유 ID (보관함에서 설정)
        public float cooldown, readyAt;
        public int stamina;
        public bool empty;
    }
    public Slot[] slots;

    // 돌진이 끝나는 시각과 시작 위치 (끝나는 순간 지나온 구간에 한 번 피해)
    private float rushEndsAt = -1, rushStartX;

    void Awake()
    {
        slots = new[]
        {
            new Slot { key = "Q", name = "물러서기", skillId = "retreat", cooldown = RetreatCooldown },
            new Slot { key = "W", name = "돌진베기", skillId = "rush_slash", cooldown = RushCooldown },
            new Slot { key = "E", name = "강궁", skillId = "power_shot", cooldown = PowerShotCooldown },
        };
    }

    // R키 재시작 때 쿨타임 초기화
    public void ResetSkills()
    {
        if (slots != null) foreach (var s in slots) s.readyAt = 0;
        if (combat != null) combat.PowerShotReady = false;
    }

    bool CanUse => combat.PlayerHealth > 0 && !combat.Paused && combat.player.CanMove && !combat.player.Dodging;

    void Update()
    {
        if (combat == null || combat.player == null) return;
        // 강궁 준비 중: 선비 몸에서 금빛이 피어오름
        if (combat.PowerShotReady && !combat.Paused && Time.frameCount % 5 == 0)
            JinaFx.Rise(combat.player.transform.position + Vector3.up * .3f, 2, new Color(1f, .82f, .3f), .35f, 1.2f, .5f);
        if (!CanUse) return;
        if (Input.GetKeyDown(KeyCode.Q)) TryUse(0);
        else if (Input.GetKeyDown(KeyCode.W)) TryUse(1);
        else if (Input.GetKeyDown(KeyCode.E)) TryUse(2);
    }

    void TryUse(int index)
    {
        var s = slots[index];
        if (s.empty || Time.time < s.readyAt) return;
        // 활을 들고 있으면 돌진베기는 못 씀 (어느 칸에 있든)
        if (s.skillId == "rush_slash" && combat.UsingBow) return;
        // 강궁은 활을 들고 있을 때만, 이미 준비된 상태면 다시 못 씀
        if (s.skillId == "power_shot" && (!combat.UsingBow || combat.PowerShotReady)) return;
        var p = combat.player;
        // 스킬 종류에 따라 스테미너 소모. 모자라면 쓰지 않음 (쿨타임도 안 돎)
        int cost = s.skillId == "retreat" ? RetreatStamina : s.skillId == "rush_slash" ? RushStamina
                 : s.skillId == "power_shot" ? PowerShotStamina : 0;
        if (!p.UseStamina(cost)) return;
        switch (s.skillId)
        {
            case "retreat":   // 물러서기 (Q/W/E 어느 칸에서도 사용)
                p.StartDash(-p.Facing, RetreatLength, RetreatSpeed, false);
                combat.GrantSkillInvulnerable(RetreatInvulnerable);
                break;
            case "rush_slash":   // 돌진베기 (Q/W/E 어느 칸에서도 사용)
                rushStartX = p.transform.position.x;
                p.StartDash(p.Facing, RushLength, RushSpeed);
                rushEndsAt = Time.time + RushLength;
                break;
            case "power_shot":   // 강궁: 다음 화살 한 발 공격력 2배
                combat.PowerShotReady = true;
                var chest = p.transform.position + Vector3.up * .3f;
                JinaFx.Glint(chest, new Color(1f, .85f, .35f));
                JinaFx.Dots(chest, 12, new Color(1f, .8f, .3f), 2.6f, .4f);
                JinaFx.Word(p.transform.position + Vector3.up * 1.5f, "강궁!", new Color(1f, .85f, .35f), 22);
                break;
        }
        s.readyAt = Time.time + s.cooldown;
    }

    // ── 화면 왼쪽 아래 스킬창 ──
    void OnGUI()
    {
        // 보따리(I)·대화·상점·컷신 중에는 체력바처럼 스킬창도 숨김
        if (combat == null || combat.PlayerHealth <= 0 || combat.Paused || slots == null) return;
        var oldMatrix = GUI.matrix;
        var oldColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;


        float size = 64, gap = 8, marginY = 20;
        float y = Screen.height - marginY - size;
        // 체력·스테미너 막대(너비 520, 화면 가운데)의 왼쪽 끝에 맞춤
        float startX = (Screen.width - Combat.HudWidth) / 2;
        var keyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = 13, fontStyle = FontStyle.Bold };
        var nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = 11 };
        var cdStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            var r = new Rect(startX + i * (size + gap), y, size, size);

            // 금빛 테두리와 바탕
            GUI.color = new Color(.8f, .65f, .35f, .9f);
            GUI.DrawTexture(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), Texture2D.whiteTexture);
            GUI.color = s.empty ? new Color(.12f, .12f, .12f, .9f) : new Color(.18f, .16f, .22f, .95f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);

            // 스킬 아이콘 (이름 글씨 위쪽에)
            var icon = s.empty ? null : IconOf(s.skillId);
            if (icon != null)
            {
                GUI.color = Color.white;
                InventoryUI.DrawIcon(icon, new Rect(r.x + 17, r.y + 7, r.width - 26, r.height - 25));
            }

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
            GUI.Label(new Rect(r.x, r.y, r.width, r.height - 4), s.empty ? "비어 있음" : s.name, nameStyle);

            // 강궁 준비 중: 금빛으로 칠하고 "준비"
            if (s.skillId == "power_shot" && combat.PowerShotReady)
            {
                GUI.color = new Color(1f, .82f, .3f, .35f + .15f * Mathf.Sin(Time.time * 8));
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(r, "준비", cdStyle);
            }
            // 활을 들면 돌진베기 칸, 칼을 들면 강궁 칸을 어둡게 덮고 빨간 X (어느 칸에 있든 스킬을 따라감)
            if ((s.skillId == "rush_slash" && combat.UsingBow) || (s.skillId == "power_shot" && !combat.UsingBow))
            {
                GUI.color = new Color(0, 0, 0, .55f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                var xStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter, fontSize = 44, fontStyle = FontStyle.Bold,
                    padding = new RectOffset(0, 0, 0, 0), clipping = TextClipping.Overflow
                };
                GUI.color = new Color(1f, .25f, .25f);
                GUI.Label(new Rect(r.x, r.y - 10, r.width, r.height + 20), "X", xStyle);
            }

            // 키 글자(Q·W·E): 왼쪽 위 어두운 네모 위에. 맨 마지막에 그려서 아이콘·X에 가려지지 않음
            var keyRect = new Rect(r.x + 2, r.y + 2, 18, 18);
            GUI.color = new Color(0, 0, 0, .75f);
            GUI.DrawTexture(keyRect, Texture2D.whiteTexture);
            var keyBadge = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontSize = 13, fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 0, 0), clipping = TextClipping.Overflow
            };
            GUI.color = new Color(1f, .86f, .45f);
            GUI.Label(keyRect, s.key, keyBadge);
            GUI.color = Color.white;
        }

        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }
}