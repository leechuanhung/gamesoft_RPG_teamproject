using UnityEngine;
using System.Collections.Generic;

// A small combat loop, attached automatically by the existing test scene.
public class PrototypeCombat : MonoBehaviour
{
    public PrototypePlayer player;
    public int PlayerHealth { get; private set; } = 5;
    public const int EnemyMaxHealth = 30;
    public int EnemyHealth { get; private set; } = EnemyMaxHealth;
    public bool BossPurified => Quest != null && Quest.progress.Outcome == EncounterOutcome.Purified;
    public bool EnemyDefeated => EnemyHealth <= 0 || BossPurified;
    public int BossFacing { get; private set; } = -1;
    private float bossRecoveryUntil;
    public int BossAttackFrame => EnemyDefeated ? -1 : windupUntil >= 0
        ? (windupUntil - Time.time > .3f ? 0 : 1)
        : Time.time < bossRecoveryUntil ? (bossRecoveryUntil - Time.time > .12f ? 2 : 3) : -1;
    private float playerImpactAt = -1;
    private bool pendingBow;
    private SpriteRenderer enemy;
    private SpriteRenderer slash;
    private SpriteRenderer playerArt;
    private Color playerColor;
    private float nextAttack, slashUntil, invulnerableUntil, enemyReadyAt, hitFlashUntil;
    private float windupUntil = -1;
    private Vector2 lockedStrike;
    private int attackFacing = 1;
    private SeonbiVisual playerVisual;
    public PrototypeQuest Quest { get; private set; }
    public bool UsingBow { get; private set; }
    private readonly List<FlyingArrow> arrows = new List<FlyingArrow>();
    private bool alerted;
    private bool strikeElder;
    public PrototypeCamp Camp { get; private set; }
    public PrototypeTavern Tavern { get; private set; }
    public bool Paused => (Quest != null && Quest.IsDialogOpen) || (Tavern != null && Tavern.IsOpen);
    public void RestoreHealth(int amount) { if (PlayerHealth > 0) PlayerHealth = Mathf.Clamp(PlayerHealth + amount, 1, 5); }
    private class FlyingArrow
    {
        public SpriteRenderer art;
        public int direction;
        public float remaining = 12f;
    }

    void Start()
    {
        if (player == null) { enabled = false; return; }
        playerArt = player.GetComponent<SpriteRenderer>();
        playerColor = playerArt.color;
        enemy = MakeSprite("Temporary Dokkaebi", new Vector2(1.6f, 2.4f), new Color(.8f,.2f,.25f));
        JinaActorVisual.Attach(enemy, JinaArt.Boss, this);
        slash = MakeSprite("Sword Range", new Vector2(1.35f, .9f), new Color(1f,.9f,.5f,.65f));
        slash.sortingOrder = 3;
        Quest = gameObject.AddComponent<PrototypeQuest>();
        Quest.combat = this;
        Camp = new GameObject("일반 몬스터와 봉인").AddComponent<PrototypeCamp>();
        Camp.Initialize(this, playerArt.sprite);
        Tavern = gameObject.AddComponent<PrototypeTavern>();
        Tavern.combat = this;
        playerVisual = SeonbiVisual.Attach(player, this);
        Restart();
    }

    SpriteRenderer MakeSprite(string label, Vector2 size, Color color)
    {
        var obj = new GameObject(label);
        obj.transform.localScale = size;
        var art = obj.AddComponent<SpriteRenderer>();
        art.sprite = playerArt.sprite;
        art.color = color;
        return art;
    }

    public void Restart()
    {
        if (enemy == null) return;
        PlayerHealth = 5;
        EnemyHealth = EnemyMaxHealth;
        enemy.transform.position = new Vector3(PrototypeWorld.EnemyHome, .7f, 0);
        enemy.enabled = true;
        slash.enabled = false;
        nextAttack = slashUntil = invulnerableUntil = hitFlashUntil = 0;
        enemyReadyAt = Time.time + 1;
        windupUntil = -1;
        bossRecoveryUntil = 0;
        playerImpactAt = -1;
        BossFacing = -1;
        playerArt.color = playerColor;
        player.Respawn();
        UsingBow = false;
        alerted = false;
        strikeElder = false;
        if (Camp != null) Camp.ResetCamp();
        ClearArrows();
        if (Quest != null) Quest.ResetQuest();
        if (Tavern != null) Tavern.ResetShop();
        if (playerVisual != null) playerVisual.ResetPose();
    }

    void Update()
    {
        if (player == null || enemy == null) return;
        if (Input.GetKeyDown(KeyCode.R)) Restart();
        if (PlayerHealth <= 0) return;
        if (Paused)
        {
            playerImpactAt = -1;
            bossRecoveryUntil = 0;
            windupUntil = -1;
            enemyReadyAt = Time.time + .5f;
            slash.enabled = false;
            return;
        }
        if (Input.GetKeyDown(KeyCode.Q)) UsingBow = !UsingBow;
        if (!EnemyDefeated && Input.GetKeyDown(KeyCode.F)
            && Vector2.Distance(player.transform.position, enemy.transform.position) < 2.2f
            && TryPurifyBoss())
        {
            // State and appearance are updated together by TryPurifyBoss.
        }

        if (Input.GetKeyDown(KeyCode.J) && Time.time >= nextAttack)
        {
            nextAttack = Time.time + (UsingBow ? .65f : .4f);
            attackFacing = player.Facing;
            pendingBow = UsingBow;
            playerImpactAt = Time.time + (pendingBow ? SeonbiVisual.BowReleaseDelay : SeonbiVisual.HitDelay);
            if (playerVisual != null) playerVisual.Attack(UsingBow, attackFacing);
        }
        if (playerImpactAt >= 0 && Time.time >= playerImpactAt)
        {
            playerImpactAt = -1;
            if (pendingBow)
            {
                slashUntil = 0;
                var arrow = MakeSprite("Arrow", new Vector2(.55f, .1f), new Color(1f,.85f,.35f));
                arrow.sprite = JinaArt.Load("04_Weapons/arrow");
                arrow.transform.localScale = Vector3.one;
                arrow.color = Color.white;
                arrow.flipX = attackFacing < 0;
                arrow.sortingOrder = 4;
                arrow.transform.position = player.transform.position;
                arrows.Add(new FlyingArrow { art = arrow, direction = attackFacing });
            }
            else
            {
                slashUntil = Time.time + .13f;
                Vector2 center = (Vector2)player.transform.position + Vector2.right * attackFacing * .95f;
                var attackBounds = new Bounds(center, new Vector3(1.35f, .9f, 1));
                if (!EnemyDefeated && attackBounds.Intersects(enemy.bounds)) DamageEnemy();
                if (Camp != null) Camp.Slash(attackBounds);
            }
        }
        AdvanceArrows(Time.deltaTime);
        slash.enabled = Time.time < slashUntil;
        if (playerVisual != null) slash.enabled = false;
        slash.transform.position = player.transform.position + Vector3.right * attackFacing * .95f;
        playerArt.color = Time.time < invulnerableUntil
            ? (Mathf.FloorToInt(Time.time * 14) % 2 == 0 ? Color.red : playerColor) : playerColor;
        if (EnemyDefeated) return;

        Vector2 delta = player.transform.position - enemy.transform.position;
        // Once provoked, the boss follows into the village and notices the elder.
        if (PrototypeWorld.CanDetect(player.transform.position, enemy.transform.position)) alerted = true;
        bool targetElder = alerted && Quest != null && !Quest.progress.ElderDead
            && Mathf.Abs(enemy.transform.position.x - PrototypeWorld.Elder.x) < 6f;
        if (targetElder) delta = (Vector3)PrototypeWorld.Elder - enemy.transform.position;
        if (windupUntil < 0 && Time.time >= bossRecoveryUntil && Mathf.Abs(delta.x) > .01f) BossFacing = delta.x > 0 ? 1 : -1;
        if (windupUntil >= 0)
        {
            // The strike stays where it was aimed, so moving or jumping can evade it.
            if (Time.time >= windupUntil)
            {
                var strikeBounds = new Bounds(lockedStrike, new Vector3(1.7f, 1.5f, 1));
                if (strikeElder && Quest != null && !Quest.progress.ElderDead
                    && Mathf.Abs(enemy.transform.position.x - PrototypeWorld.Elder.x) < 2f)
                {
                    if (Quest.progress.SacrificeElder())
                    {
                        Quest.ShowElderDeath();
                        DamageEnemy(10);
                    }
                }
                else if (!strikeElder && strikeBounds.Intersects(playerArt.bounds)) HurtPlayer();
                windupUntil = -1;
                bossRecoveryUntil = Time.time + .24f;
                enemyReadyAt = Time.time + .9f;
            }
        }
        else if (alerted && Time.time >= enemyReadyAt && Mathf.Abs(delta.x) < 1.5f && Mathf.Abs(delta.y) < 1.6f)
        {
            lockedStrike = (Vector2)enemy.transform.position + Vector2.right * (delta.x >= 0 ? .8f : -.8f);
            strikeElder = targetElder;
            windupUntil = Time.time + .6f;
        }
        else if (alerted && Mathf.Abs(delta.x) > 1.25f && Time.time >= bossRecoveryUntil)
        {
            var position = enemy.transform.position;
            position.x = Mathf.Clamp(position.x + Mathf.Sign(delta.x) * 1.8f * Time.deltaTime, PrototypeWorld.EnemyMin, PrototypeWorld.EnemyMax);
            enemy.transform.position = position;
        }
        enemy.color = Time.time < hitFlashUntil ? Color.white : windupUntil >= 0
            ? new Color(1f,.65f,.1f) : new Color(.8f,.2f,.25f);
    }

    public bool TryPurifyBoss()
    {
        if (EnemyDefeated || Quest == null || !Quest.progress.Resolve(true)) return false;
        enemy.enabled = true;
        enemy.color = new Color(.8f,.2f,.25f);
        windupUntil = -1;
        bossRecoveryUntil = 0;
        alerted = false;
        strikeElder = false;
        playerImpactAt = -1;
        ClearArrows();
        var visual = enemy.GetComponent<JinaActorVisual>();
        if (visual != null) visual.Refresh();
        return true;
    }

    public void HurtPlayer()
    {
        if (PlayerHealth <= 0 || Time.time < invulnerableUntil) return;
        PlayerHealth--;
        invulnerableUntil = Time.time + 1;
        if (PlayerHealth > 0) return;
        player.CanMove = false;
        playerImpactAt = -1;
        playerArt.color = Color.gray;
        slash.enabled = false;
        ClearArrows();
    }

    void DamageEnemy(int amount = 1)
    {
        if (EnemyDefeated) return;
        EnemyHealth = Mathf.Max(0, EnemyHealth - amount);
        alerted = true;
        hitFlashUntil = Time.time + .12f;
        if (!EnemyDefeated) return;
        enemy.enabled = false;
        windupUntil = -1;
        if (Quest != null) Quest.progress.Resolve(false);
    }

    // Sweep each frame so fast arrows cannot pass through a target or a wall.
    void AdvanceArrows(float deltaTime)
    {
        for (int i = arrows.Count - 1; i >= 0; i--)
        {
            var arrow = arrows[i];
            Vector2 origin = arrow.art.transform.position;
            Vector2 direction = Vector2.right * arrow.direction;
            float step = Mathf.Min(14f * deltaTime, arrow.remaining);
            var wall = Physics2D.Raycast(origin, direction, step, 1 << 8);
            float enemyDistance = float.PositiveInfinity;
            if (!EnemyDefeated) enemyDistance = ArrowHitDistance(enemy.bounds, origin, arrow.direction, step);
            PrototypeCamp.Mob mob = null;
            if (Camp != null)
                foreach (var candidate in Camp.mobs)
                {
                    if (candidate.hp <= 0) continue;
                    float distance = ArrowHitDistance(candidate.art.bounds, origin, arrow.direction, step);
                    if (distance < enemyDistance) { enemyDistance = distance; mob = candidate; }
                }
            bool hitsEnemy = enemyDistance <= step && (wall.collider == null || enemyDistance < wall.distance);
            if (hitsEnemy) { if (mob != null) Camp.Hit(mob); else DamageEnemy(); }
            arrow.remaining -= step;
            if (hitsEnemy || wall.collider != null || arrow.remaining <= 0)
            {
                Destroy(arrow.art.gameObject);
                arrows.RemoveAt(i);
            }
            else arrow.art.transform.position = origin + direction * step;
        }
    }

    public static float ArrowHitDistance(Bounds target, Vector2 origin, int direction, float distance)
    {
        // Account for arrow thickness without extending its damage past the traveled segment.
        target.Expand(new Vector3(0, .1f, 0));
        if (target.Contains(origin)) return 0;
        float hit;
        return target.IntersectRay(new Ray(origin, Vector3.right * direction), out hit) && hit <= distance
            ? hit : float.PositiveInfinity;
    }

    void ClearArrows()
    {
        foreach (var arrow in arrows)
            if (arrow.art != null) Destroy(arrow.art.gameObject);
        arrows.Clear();
    }

    void OnGUI()
    {
        PrototypeKoreanUI.Apply();
        GUI.Box(new Rect(15, 105, 440, 75), "선비 체력: " + PlayerHealth + " / 5     " + (BossPurified ? "도깨비: 정화됨 · 우호적" : "도깨비 체력: " + EnemyHealth + " / " + EnemyMaxHealth));
        GUI.Label(new Rect(30, 133, 410, 40), EnemyDefeated
            ? (Quest != null && Quest.progress.Outcome == EncounterOutcome.Purified
                ? "저주를 풀었습니다. 오른쪽 금빛 표식으로 향하세요."
                : "도깨비를 물리쳤습니다. 오른쪽 금빛 표식으로 향하세요.")
            : "주황빛은 공격의 전조입니다. 물러나거나 뛰어 피하세요.");
        if (PlayerHealth <= 0)
            GUI.Box(new Rect(Screen.width / 2f - 130, 280, 260, 60), "선비가 쓰러졌습니다.\nR: 처음부터 다시 시작");
    }

    void OnDestroy()
    {
        if (enemy != null) Destroy(enemy.gameObject);
        if (slash != null) Destroy(slash.gameObject);
        if (Camp != null) Destroy(Camp.gameObject);
        ClearArrows();
        if (player != null) player.CanMove = true;
    }
}
