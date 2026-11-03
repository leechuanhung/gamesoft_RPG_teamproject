using UnityEngine;
using System.Collections.Generic;

// A small combat loop, attached automatically by the existing test scene.
public class PrototypeCombat : MonoBehaviour
{
    public PrototypePlayer player;
    public int PlayerHealth { get; private set; } = 5;
    public const int EnemyMaxHealth = 30;
    public const int SwordDamage = 2, ArrowDamage = 1;
    public int EnemyHealth { get; private set; } = EnemyMaxHealth;
    public bool BossPurified => Quest != null && Quest.progress.Outcome == EncounterOutcome.Purified;
    public bool EnemyDefeated => EnemyHealth <= 0 || BossPurified;
    public int BossFacing { get; private set; } = -1;
    // Start times of the boss's death and purification motions (see JinaActorVisual).
    public float BossDownAt { get; private set; } = -1;
    public float PurifiedAt { get; private set; } = -1;
    // When the seonbi was struck down (SeonbiVisual plays his fall from it).
    public float PlayerDownAt { get; private set; } = -1;
    private float bossRecoveryUntil;
    public int BossAttackFrame => EnemyDefeated ? -1 : Move == BossMove.LeapWindup ? 0 : Move == BossMove.Leaping ? 1
        : Move == BossMove.Landed ? (moveUntil - Time.time > .2f ? 2 : 3)
        : windupUntil >= 0 ? (windupUntil - Time.time > .3f ? 0 : 1)
        : Time.time < bossRecoveryUntil ? (bossRecoveryUntil - Time.time > .12f ? 2 : 3) : -1;
    // Besides the close strike, the boss rushes the seonbi across open ground or leaps onto him (also onto ledges)
    // and slams the ground. Each move telegraphs with an orange windup.
    public enum BossMove { None, ChargeWindup, Charging, LeapWindup, Leaping, Landed }
    public BossMove Move { get; private set; }
    public const float ChaseSpeed = 2.6f, ChargeSpeed = 9f, ChargeWindup = .5f, ChargeLength = .7f;
    public const float LeapWindup = .45f, LeapLength = .75f, LeapHeight = 3f, LandedLength = .6f, SlamRadius = 2.6f;
    public const float BossGround = .7f, MoveCooldown = 2.4f;
    private float moveUntil, nextMoveAt;
    private int moveDir, moveCount;
    private float leapFrom, leapTo;
    private bool moveHit;
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
    public PrototypePeddler Peddler { get; private set; }
    public PrototypeVillage Village { get; private set; }
    public BossRemorse Remorse { get; private set; }
    public PrototypeEncounter Encounter { get; private set; }
    public PrototypeChatter Chatter { get; private set; }
    public PrototypeBossTalk BossTalk { get; private set; }
    public Vector3 BossPosition => enemy != null ? enemy.transform.position : Vector3.zero;
    // A provoked boss that has followed the player past the forest edge.
    public bool BossAlerted => alerted;
    public bool BossInVillage => alerted && !EnemyDefeated && enemy != null && enemy.transform.position.x < PrototypeWorld.ForestStart;
    public int Arrows { get; private set; } = ShopRules.MaxArrows;
    private float noArrowsUntil;
    public void AddArrows(int amount) { Arrows = Mathf.Clamp(Arrows + amount, 0, ShopRules.MaxArrows); }
    public PrototypeIntro Intro { get; private set; }
    public PrototypeEnding Ending { get; private set; }
    public bool Paused => (Intro != null && Intro.IsPlaying) || (Ending != null && Ending.Active) || (Encounter != null && Encounter.Playing) || (BossTalk != null && BossTalk.IsOpen) || (Quest != null && Quest.IsDialogOpen) || (Tavern != null && Tavern.IsOpen) || (Peddler != null && Peddler.IsOpen);
    public void RestoreHealth(int amount) { if (PlayerHealth > 0) PlayerHealth = Mathf.Clamp(PlayerHealth + amount, 1, 5); }
    private class FlyingArrow
    {
        public SpriteRenderer art;
        public Vector2 direction;
        public float remaining = 12f;
    }
    // Hold up or down while shooting to aim the bow 30° up or down (bow_up / bow_down frames).
    public const float AimAngle = 30f;
    private int pendingAim;
    // Where the arrow leaves the bow, from the feet, for a right-facing shot. The level shot leaves 1.1 ahead at
    // the drawn nock height; aimed shots turn that point about the release frame's shoulder (x 400, 285 px up).
    public static Vector2 ArrowLaunch(int aim)
    {
        var level = new Vector2(1.1f, JinaArt.BowArrowHeight);
        if (aim == 0) return level;
        var pivot = new Vector2(100f, 285f) / (510f / 1.9f);
        return pivot + (Vector2)(Quaternion.Euler(0, 0, AimAngle * aim) * (level - pivot));
    }
    public static Vector2 ArrowDirection(int aim, int facing) =>
        new Vector2(facing * Mathf.Cos(AimAngle * aim * Mathf.Deg2Rad), Mathf.Sin(AimAngle * aim * Mathf.Deg2Rad));

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
        Peddler = gameObject.AddComponent<PrototypePeddler>();
        Peddler.combat = this;
        Village = gameObject.AddComponent<PrototypeVillage>();
        Village.combat = this;
        Remorse = gameObject.AddComponent<BossRemorse>();
        Remorse.combat = this;
        Encounter = gameObject.AddComponent<PrototypeEncounter>();
        Encounter.combat = this;
        Chatter = gameObject.AddComponent<PrototypeChatter>();
        Chatter.combat = this;
        BossTalk = gameObject.AddComponent<PrototypeBossTalk>();
        BossTalk.combat = this;
        playerVisual = SeonbiVisual.Attach(player, this);
        Restart();
        Intro = gameObject.AddComponent<PrototypeIntro>();
        Intro.combat = this;
        Ending = gameObject.AddComponent<PrototypeEnding>();
        Ending.combat = this;
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
        Move = BossMove.None; nextMoveAt = Time.time + MoveCooldown; moveCount = 0;
        BossDownAt = PurifiedAt = PlayerDownAt = -1;
        playerArt.color = playerColor;
        player.Respawn();
        UsingBow = false;
        Arrows = ShopRules.MaxArrows;
        noArrowsUntil = 0;
        alerted = false;
        strikeElder = false;
        if (Camp != null) Camp.ResetCamp();
        ClearArrows();
        if (Quest != null) Quest.ResetQuest();
        if (Tavern != null) Tavern.ResetShop();
        if (Peddler != null) Peddler.ResetShop();
        if (Village != null) Village.ResetVillage();
        if (Ending != null) Ending.ResetEnding();
        if (Encounter != null) Encounter.ResetEncounter();
        if (Chatter != null) Chatter.ResetChatter();
        if (BossTalk != null) BossTalk.ResetTalk();
        if (playerVisual != null) playerVisual.ResetPose();
    }

    void Update()
    {
        if (player == null || enemy == null) return;
        // The intro plays once per launch; R does not replay or interrupt it.
        if (Intro != null && Intro.IsPlaying) return;
        if (Input.GetKeyDown(KeyCode.R)) Restart();
        if (PlayerHealth <= 0) return;
        if (Paused)
        {
            playerImpactAt = -1;
            bossRecoveryUntil = 0;
            windupUntil = -1;
            enemyReadyAt = Time.time + .5f;
            slash.enabled = false;
            if (Move != BossMove.None) EndMove(0);
            return;
        }
        if (Input.GetKeyDown(KeyCode.Q)) UsingBow = !UsingBow;
        if (!EnemyDefeated && Input.GetKeyDown(KeyCode.F)
            && Vector2.Distance(player.transform.position, enemy.transform.position) < 2.2f
            && TryPurifyBoss())
        {
            // State and appearance are updated together by TryPurifyBoss.
        }

        if (Input.GetKeyDown(KeyCode.J) && Time.time >= nextAttack && UsingBow && Arrows <= 0)
            noArrowsUntil = Time.time + 1.5f;
        else if (Input.GetKeyDown(KeyCode.J) && Time.time >= nextAttack)
        {
            nextAttack = Time.time + (UsingBow ? .65f : .4f);
            attackFacing = player.Facing;
            pendingBow = UsingBow;
            pendingAim = !UsingBow ? 0 : Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1
                : Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
            playerImpactAt = Time.time + (pendingBow ? SeonbiVisual.BowReleaseDelay : SeonbiVisual.HitDelay);
            if (playerVisual != null) playerVisual.Attack(UsingBow, attackFacing, pendingAim);
        }
        if (playerImpactAt >= 0 && Time.time >= playerImpactAt)
        {
            playerImpactAt = -1;
            if (pendingBow)
            {
                // Spent on release, so a shot cancelled by a dialog keeps its arrow.
                Arrows--;
                slashUntil = 0;
                var arrow = MakeSprite("Arrow", new Vector2(.55f, .1f), new Color(1f,.85f,.35f));
                arrow.sprite = JinaArt.Load("04_Weapons/arrow");
                arrow.transform.localScale = Vector3.one;
                arrow.color = Color.white;
                arrow.flipX = attackFacing < 0;
                arrow.sortingOrder = 4;
                arrow.transform.rotation = Quaternion.Euler(0, 0, AimAngle * pendingAim * attackFacing);
                // Use the same foot reference as SeonbiVisual, including during jumps.
                var launch = ArrowLaunch(pendingAim);
                arrow.transform.position = new Vector3(player.transform.position.x + attackFacing * launch.x,
                    playerArt.bounds.min.y + launch.y, 0);
                arrows.Add(new FlyingArrow { art = arrow, direction = ArrowDirection(pendingAim, attackFacing) });
            }
            else
            {
                slashUntil = Time.time + .13f;
                Vector2 center = (Vector2)player.transform.position + Vector2.right * attackFacing * .95f;
                var attackBounds = new Bounds(center, new Vector3(1.35f, .9f, 1));
                if (!EnemyDefeated && attackBounds.Intersects(enemy.bounds)) DamageEnemy(SwordDamage);
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
        if (Move != BossMove.None) { StepMove(); TintBoss(); return; }
        if (windupUntil < 0 && Time.time >= bossRecoveryUntil && Mathf.Abs(delta.x) > .01f) BossFacing = delta.x > 0 ? 1 : -1;
        if (alerted && !targetElder && windupUntil < 0 && Time.time >= bossRecoveryUntil && Time.time >= nextMoveAt && StartMove(delta))
        {
            TintBoss();
            return;
        }
        if (windupUntil >= 0)
        {
            // The strike stays where it was aimed, so moving or jumping can evade it.
            if (Time.time >= windupUntil)
            {
                var strikeBounds = new Bounds(lockedStrike, new Vector3(1.7f, 1.5f, 1));
                bool killedElder = false;
                if (strikeElder && Quest != null && !Quest.progress.ElderDead
                    && Mathf.Abs(enemy.transform.position.x - PrototypeWorld.Elder.x) < 2f)
                {
                    if (Quest.progress.SacrificeElder())
                    {
                        // A heavier blow: the screen shakes and the club stays down on the impact frame.
                        killedElder = true;
                        Quest.ShowElderDeath();
                        PrototypeView.Shake(.12f, .4f);
                        DamageEnemy(10);
                    }
                }
                else if (!strikeElder && strikeBounds.Intersects(playerArt.bounds)) HurtPlayer();
                windupUntil = -1;
                bossRecoveryUntil = Time.time + (killedElder ? .7f : .24f);
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
            position.x = Mathf.Clamp(position.x + Mathf.Sign(delta.x) * ChaseSpeed * Time.deltaTime, PrototypeWorld.EnemyMin, PrototypeWorld.EnemyMax);
            enemy.transform.position = position;
        }
        TintBoss();
    }

    // Orange while winding up any attack, white for a moment when hit.
    void TintBoss()
    {
        bool windup = windupUntil >= 0 || Move == BossMove.ChargeWindup || Move == BossMove.LeapWindup;
        enemy.color = Time.time < hitFlashUntil ? Color.white : windup ? new Color(1f,.65f,.1f) : new Color(.8f,.2f,.25f);
    }

    // A seonbi up on a ledge is leapt at; one at mid range on the ground is rushed or leapt at in turn.
    bool StartMove(Vector2 delta)
    {
        float dx = Mathf.Abs(delta.x);
        bool above = delta.y > 1.2f && dx < 7f;
        bool level = Mathf.Abs(delta.y) < 1.6f && dx > 3.5f && dx < 9f;
        if (!above && !level) return false;
        moveDir = delta.x >= 0 ? 1 : -1;
        BossFacing = moveDir;
        moveHit = false;
        if (above || moveCount++ % 2 == 1)
        {
            Move = BossMove.LeapWindup; moveUntil = Time.time + LeapWindup;
            leapFrom = enemy.transform.position.x;
            leapTo = Mathf.Clamp(player.transform.position.x, PrototypeWorld.EnemyMin, PrototypeWorld.EnemyMax);
        }
        else { Move = BossMove.ChargeWindup; moveUntil = Time.time + ChargeWindup; }
        return true;
    }

    void StepMove()
    {
        var position = enemy.transform.position;
        switch (Move)
        {
            case BossMove.ChargeWindup:
                if (Time.time >= moveUntil) { Move = BossMove.Charging; moveUntil = Time.time + ChargeLength; }
                break;
            case BossMove.Charging:
                position.x = Mathf.Clamp(position.x + moveDir * ChargeSpeed * Time.deltaTime, PrototypeWorld.EnemyMin, PrototypeWorld.EnemyMax);
                enemy.transform.position = position;
                if (!moveHit && enemy.bounds.Intersects(playerArt.bounds)) { moveHit = true; HurtPlayer(); }
                if (Time.time >= moveUntil || position.x <= PrototypeWorld.EnemyMin || position.x >= PrototypeWorld.EnemyMax) EndMove(.5f);
                break;
            case BossMove.LeapWindup:
                if (Time.time >= moveUntil) { Move = BossMove.Leaping; moveUntil = Time.time + LeapLength; }
                break;
            case BossMove.Leaping:
                float k = 1 - Mathf.Clamp01((moveUntil - Time.time) / LeapLength);
                position.x = Mathf.Lerp(leapFrom, leapTo, k);
                position.y = BossGround + 4 * LeapHeight * k * (1 - k);
                enemy.transform.position = position;
                // It also crashes into a seonbi in its path, up on a ledge or not.
                if (!moveHit && enemy.bounds.Intersects(playerArt.bounds)) { moveHit = true; HurtPlayer(); }
                if (k >= 1) Land();
                break;
            case BossMove.Landed:
                if (Time.time >= moveUntil) EndMove(.3f);
                break;
        }
    }

    // The slam: the ground shakes and hurts a seonbi standing near on the ground, or one it lands on.
    void Land()
    {
        var position = enemy.transform.position; position.y = BossGround; enemy.transform.position = position;
        Move = BossMove.Landed; moveUntil = Time.time + LandedLength;
        PrototypeView.Shake(.14f, .4f);
        bool onGround = player.IsGrounded && playerArt.bounds.min.y < -.3f;
        if (!moveHit && (enemy.bounds.Intersects(playerArt.bounds) || onGround && Mathf.Abs(player.transform.position.x - position.x) < SlamRadius)) { moveHit = true; HurtPlayer(); }
    }

    void EndMove(float recovery)
    {
        var position = enemy.transform.position; position.y = BossGround; enemy.transform.position = position;
        Move = BossMove.None;
        bossRecoveryUntil = Time.time + recovery;
        enemyReadyAt = Time.time + recovery + .3f;
        nextMoveAt = Time.time + MoveCooldown;
    }

    public bool TryPurifyBoss()
    {
        if (EnemyDefeated || Quest == null || !Quest.progress.Resolve(true)) return false;
        PurifiedAt = Time.time;
        enemy.enabled = true;
        enemy.color = new Color(.8f,.2f,.25f);
        windupUntil = -1;
        bossRecoveryUntil = 0;
        if (Move != BossMove.None) EndMove(0);
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
        PlayerDownAt = Time.time;
        player.CanMove = false;
        player.Downed = true;
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
        // The renderer stays on while JinaActorVisual plays the death motion, then hides itself.
        BossDownAt = Time.time;
        windupUntil = -1;
        if (Move != BossMove.None) EndMove(0);
        if (Quest != null) Quest.progress.Resolve(false);
    }

    // Sweep each frame so fast arrows cannot pass through a target or a wall.
    void AdvanceArrows(float deltaTime)
    {
        for (int i = arrows.Count - 1; i >= 0; i--)
        {
            var arrow = arrows[i];
            Vector2 origin = arrow.art.transform.position;
            Vector2 direction = arrow.direction;
            float step = Mathf.Min(14f * deltaTime, arrow.remaining);
            var wall = Wall(origin, direction, step);
            float enemyDistance = float.PositiveInfinity;
            if (!EnemyDefeated) enemyDistance = ArrowHitDistance(enemy.bounds, origin, direction, step);
            PrototypeCamp.Mob mob = null;
            if (Camp != null)
                foreach (var candidate in Camp.mobs)
                {
                    if (candidate.hp <= 0) continue;
                    float distance = ArrowHitDistance(candidate.art.bounds, origin, direction, step);
                    if (distance < enemyDistance) { enemyDistance = distance; mob = candidate; }
                }
            bool hitsEnemy = enemyDistance <= step && (wall.collider == null || enemyDistance < wall.distance);
            if (hitsEnemy) { if (mob != null) Camp.Hit(mob, ArrowDamage); else DamageEnemy(ArrowDamage); }
            arrow.remaining -= step;
            if (hitsEnemy || wall.collider != null || arrow.remaining <= 0)
            {
                Destroy(arrow.art.gameObject);
                arrows.RemoveAt(i);
            }
            else arrow.art.transform.position = origin + direction * step;
        }
    }

    // Arrows stop at the ground and walls but fly through the one-way ledges.
    static RaycastHit2D Wall(Vector2 origin, Vector2 direction, float distance)
    {
        foreach (var hit in Physics2D.RaycastAll(origin, direction, distance, 1 << 8))
            if (!hit.collider.usedByEffector) return hit;
        return default;
    }
    public static float ArrowHitDistance(Bounds target, Vector2 origin, int direction, float distance) =>
        ArrowHitDistance(target, origin, Vector2.right * direction, distance);
    public static float ArrowHitDistance(Bounds target, Vector2 origin, Vector2 direction, float distance)
    {
        // Account for arrow thickness without extending its damage past the traveled segment.
        target.Expand(new Vector3(.1f, .1f, 0));
        if (target.Contains(origin)) return 0;
        float hit;
        return target.IntersectRay(new Ray(origin, direction.normalized), out hit) && hit <= distance
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
        if (Time.time < noArrowsUntil && PlayerHealth > 0)
            GUI.Box(new Rect(Screen.width / 2f - 220, 280, 440, 35), "화살이 떨어졌습니다. 주막 옆 보부상에게서 사세요.");
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
