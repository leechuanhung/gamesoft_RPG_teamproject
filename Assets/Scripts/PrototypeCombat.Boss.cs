using UnityEngine;

// The boss: its state, how it chases, strikes, charges and leaps, and how it is slain or purified.
public partial class PrototypeCombat
{
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
    public Vector3 BossPosition => enemy != null ? enemy.transform.position : Vector3.zero;
    // A provoked boss that has followed the player past the forest edge.
    public bool BossAlerted => alerted;
    public bool BossInVillage => alerted && !EnemyDefeated && enemy != null && enemy.transform.position.x < PrototypeWorld.ForestStart;

    // Once provoked it follows the seonbi (into the village too, where it goes for the elder first); up close it
    // strikes, at range it charges or leaps. Runs from Update while the boss stands.
    void UpdateBoss()
    {
        Vector2 delta = player.transform.position - enemy.transform.position;
        // Once provoked, the boss follows into the village and notices the elder.
        // It notices him only once the forest map is on screen, so the encounter never starts behind the village's view.
        if ((Maps == null || Maps.Shows(PrototypeWorld.ForestStart)) && PrototypeWorld.CanDetect(player.transform.position, enemy.transform.position)) alerted = true;
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
                else if (!strikeElder && strikeBounds.Intersects(playerArt.bounds)) HurtPlayer(BossAttack);
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
                if (!moveHit && enemy.bounds.Intersects(playerArt.bounds)) { moveHit = true; HurtPlayer(BossAttack); }
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
                if (!moveHit && enemy.bounds.Intersects(playerArt.bounds)) { moveHit = true; HurtPlayer(BossAttack); }
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
        if (!moveHit && (enemy.bounds.Intersects(playerArt.bounds) || onGround && Mathf.Abs(player.transform.position.x - position.x) < SlamRadius)) { moveHit = true; HurtPlayer(BossAttack); }
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

    void DamageEnemy(int amount = ArrowDamage)
    {
        if (EnemyDefeated) return;
        EnemyHealth = Mathf.Max(0, EnemyHealth - CalcDamage(amount, BossDefense));
        alerted = true;
        hitFlashUntil = Time.time + .12f;
        if (!EnemyDefeated) return;
        // The renderer stays on while JinaActorVisual plays the death motion, then hides itself.
        BossDownAt = Time.time;
        windupUntil = -1;
        if (Move != BossMove.None) EndMove(0);
        if (Quest != null) Quest.progress.Resolve(false);
    }
}
