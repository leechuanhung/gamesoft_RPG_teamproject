using UnityEngine;

// 도깨비: 상태, 추격, 근접 공격, 돌진, 도약, 토벌과 정화.
public partial class PrototypeCombat
{
    public int BossFacing { get; private set; } = -1;
    // 도깨비 쓰러짐과 정화 연출의 시작 시각(JinaActorVisual 참고).
    public float BossDownAt { get; private set; } = -1;
    public float PurifiedAt { get; private set; } = -1;
    // 선비가 쓰러진 시각(SeonbiVisual이 여기서부터 쓰러짐을 재생).
    public float PlayerDownAt { get; private set; } = -1;
    private float bossRecoveryUntil;
    public int BossAttackFrame => EnemyDefeated ? -1 : Move == BossMove.LeapWindup ? 0 : Move == BossMove.Leaping ? 1
        : Move == BossMove.Landed ? (moveUntil - Time.time > .2f ? 2 : 3)
        : windupUntil >= 0 ? (windupUntil - Time.time > .3f ? 0 : 1)
        : Time.time < bossRecoveryUntil ? (bossRecoveryUntil - Time.time > .12f ? 2 : 3) : -1;
    // 근접 공격 말고도, 트인 땅에서는 선비에게 돌진하거나 위로 뛰어올라(발판 위로도)
    // 땅을 내려친다. 모든 공격은 주황색 준비 동작으로 미리 알린다.
    public enum BossMove { None, ChargeWindup, Charging, LeapWindup, Leaping, Landed }
    public BossMove Move { get; private set; }
    // 마지막으로 맞은 시각(그림이 여기서부터 번쩍인다).
    public float BossHitAt { get; private set; } = -1;
    private float nextTrail;
    Vector3 BossFeet => new Vector3(enemy.transform.position.x, enemy.bounds.min.y, 0);
    // 돌진·도약할 때 뒤에 잔상을 남긴다.
    void Trail(Color colour)
    {
        if (Time.time < nextTrail) return;
        nextTrail = Time.time + .05f;
        var visual = enemy.GetComponent<JinaActorVisual>();
        if (visual != null) JinaFx.AfterImage(visual.Picture, colour);
        JinaFx.Dust(BossFeet, -moveDir * 1.2f, 1.1f);
    }
    public const float ChaseSpeed = 2.6f, ChargeSpeed = 9f, ChargeWindup = .5f, ChargeLength = .7f;
    public const float LeapWindup = .45f, LeapLength = .75f, LeapHeight = 3f, LandedLength = .6f, SlamRadius = 2.6f;
    public const float BossGround = .7f, MoveCooldown = 2.4f;
    private float moveUntil, nextMoveAt;
    private int moveDir, moveCount;
    private float leapFrom, leapTo;
    private bool moveHit;
    public Vector3 BossPosition => enemy != null ? enemy.transform.position : Vector3.zero;
    // 화가 나서 선비를 따라 숲 경계를 넘어온 도깨비.
    public bool BossAlerted => alerted;
    public bool BossInVillage => alerted && !EnemyDefeated && enemy != null && enemy.transform.position.x < PrototypeWorld.ForestStart;

    // 화가 나면 선비를 따라간다(마을에 들어가면 노인부터 노림). 가까우면
    // 근접 공격, 멀면 돌진이나 도약. 도깨비가 서 있는 동안 Update에서 돈다.
    void UpdateBoss()
    {
        Vector2 delta = player.transform.position - enemy.transform.position;
        // 화가 난 도깨비는 마을까지 따라와 노인을 알아챈다.
        // 숲 맵이 화면에 보일 때만 알아채서, 조우가 마을 화면 뒤에서 시작되지 않게 한다.
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
            // 공격은 노린 자리에 그대로 떨어진다. 움직이거나 뛰면 피할 수 있다.
            if (Time.time >= windupUntil)
            {
                var strikeBounds = new Bounds(lockedStrike, new Vector3(1.7f, 1.5f, 1));
                bool killedElder = false;
                if (strikeElder && Quest != null && !Quest.progress.ElderDead
                    && Mathf.Abs(enemy.transform.position.x - PrototypeWorld.Elder.x) < 2f)
                {
                    if (Quest.progress.SacrificeElder())
                    {
                        // 더 무거운 일격: 화면이 흔들리고 몽둥이가 내려친 프레임에서 머문다.
                        killedElder = true;
                        Quest.ShowElderDeath();
                        PrototypeView.Shake(.12f, .4f);
                        DamageEnemy(ElderCounterDamage);
                    }
                }
                else if (!strikeElder && strikeBounds.Intersects(playerArt.bounds)) HurtPlayer(BossAttack);
                // 몽둥이가 내려온다: 휩쓰는 빛과 땅에 닿은 곳의 먼지.
                JinaFx.Arc((Vector3)lockedStrike + Vector3.up * .5f, BossFacing, -14, 1.45f, new Color(1, .74f, .6f), .18f);
                JinaFx.Dust(new Vector3(lockedStrike.x, BossFeet.y, 0), BossFacing * .8f, 1.3f);
                PrototypeView.Shake(.05f, .12f);
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
            JinaFx.Glint(enemy.transform.position + new Vector3(BossFacing * .7f, 1.4f, 0), new Color(1, .7f, .3f));
        }
        else if (alerted && Mathf.Abs(delta.x) > 1.25f && Time.time >= bossRecoveryUntil)
        {
            var position = enemy.transform.position;
            position.x = Mathf.Clamp(position.x + Mathf.Sign(delta.x) * ChaseSpeed * Time.deltaTime, PrototypeWorld.EnemyMin, PrototypeWorld.EnemyMax);
            enemy.transform.position = position;
        }
        TintBoss();
    }

    // 공격 준비 중에는 주황, 맞으면 잠깐 흰색.
    void TintBoss()
    {
        bool windup = windupUntil >= 0 || Move == BossMove.ChargeWindup || Move == BossMove.LeapWindup;
        enemy.color = Time.time < hitFlashUntil ? Color.white : windup ? new Color(1f,.65f,.1f) : new Color(.8f,.2f,.25f);
    }

    // 발판 위 선비에게는 뛰어오르고, 땅 위 중거리 선비에게는 돌진과 도약을 번갈아 한다.
    bool StartMove(Vector2 delta)
    {
        float dx = Mathf.Abs(delta.x);
        bool above = delta.y > 1.2f && dx < 7f;
        bool level = Mathf.Abs(delta.y) < 1.6f && dx > 3.5f && dx < 9f;
        if (!above && !level) return false;
        moveDir = delta.x >= 0 ? 1 : -1;
        BossFacing = moveDir;
        moveHit = false;
        JinaFx.Glint(enemy.transform.position + new Vector3(moveDir * .7f, 1.4f, 0), new Color(1, .7f, .3f));
        if (above || moveCount++ % 2 == 1)
        {
            Move = BossMove.LeapWindup; moveUntil = Time.time + LeapWindup;
            leapFrom = enemy.transform.position.x;
            leapTo = Mathf.Clamp(player.transform.position.x, PrototypeWorld.EnemyMin, PrototypeWorld.EnemyMax);
        }
        else { Move = BossMove.ChargeWindup; moveUntil = Time.time + ChargeWindup; }
        return true;
    }

    // 돌진이나 도약을 한 프레임 진행한다.
    void StepMove()
    {
        var position = enemy.transform.position;
        switch (Move)
        {
            case BossMove.ChargeWindup:
                if (Time.time >= moveUntil) { Move = BossMove.Charging; moveUntil = Time.time + ChargeLength; JinaFx.Dust(BossFeet, -moveDir * 2, 1.5f); }
                break;
            case BossMove.Charging:
                Trail(new Color(1, .45f, .35f, .55f));
                position.x = Mathf.Clamp(position.x + moveDir * ChargeSpeed * Time.deltaTime, PrototypeWorld.EnemyMin, PrototypeWorld.EnemyMax);
                enemy.transform.position = position;
                if (!moveHit && enemy.bounds.Intersects(playerArt.bounds)) { moveHit = true; HurtPlayer(BossAttack); }
                if (Time.time >= moveUntil || position.x <= PrototypeWorld.EnemyMin || position.x >= PrototypeWorld.EnemyMax) EndMove(.5f);
                break;
            case BossMove.LeapWindup:
                if (Time.time >= moveUntil) { Move = BossMove.Leaping; moveUntil = Time.time + LeapLength; JinaFx.Dust(BossFeet, -1, 1.5f); JinaFx.Dust(BossFeet, 1, 1.5f); }
                break;
            case BossMove.Leaping:
                float k = 1 - Mathf.Clamp01((moveUntil - Time.time) / LeapLength);
                position.x = Mathf.Lerp(leapFrom, leapTo, k);
                position.y = BossGround + 4 * LeapHeight * k * (1 - k);
                enemy.transform.position = position;
                // 가는 길에 있는 선비와도 부딪힌다(발판 위든 아니든).
                if (!moveHit && enemy.bounds.Intersects(playerArt.bounds)) { moveHit = true; HurtPlayer(BossAttack); }
                if (k >= 1) Land();
                break;
            case BossMove.Landed:
                if (Time.time >= moveUntil) EndMove(.3f);
                break;
        }
    }

    // 내려치기: 땅이 흔들리고, 근처 땅 위의 선비나 깔린 선비가 다친다.
    void Land()
    {
        var position = enemy.transform.position; position.y = BossGround; enemy.transform.position = position;
        Move = BossMove.Landed; moveUntil = Time.time + LandedLength;
        PrototypeView.Shake(.17f, .4f);
        JinaFx.Shock(BossFeet, 1.2f);
        bool onGround = player.IsGrounded && playerArt.bounds.min.y < -.3f;
        if (!moveHit && (enemy.bounds.Intersects(playerArt.bounds) || onGround && Mathf.Abs(player.transform.position.x - position.x) < SlamRadius)) { moveHit = true; HurtPlayer(BossAttack); }
    }

    // 돌진이나 도약을 끝내고 `recovery`초 쉰다.
    void EndMove(float recovery)
    {
        var position = enemy.transform.position; position.y = BossGround; enemy.transform.position = position;
        Move = BossMove.None;
        bossRecoveryUntil = Time.time + recovery;
        enemyReadyAt = Time.time + recovery + .3f;
        nextMoveAt = Time.time + MoveCooldown;
    }

    // 부적으로 도깨비를 정화한다. 성공하면 true.
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

    // 도깨비에게 피해를 준다. 체력이 다하면 토벌.
    void DamageEnemy(int amount = ArrowDamage)
    {
        if (EnemyDefeated) return;
        int before = EnemyHealth;
        EnemyHealth = Mathf.Max(0, EnemyHealth - CalcDamage(amount, BossDefense));
        alerted = true;
        hitFlashUntil = Time.time + .12f;
        BossHitAt = Time.time;
        var at = enemy.transform.position + new Vector3(-BossFacing * .1f, .35f, 0);
        int away = player.transform.position.x < enemy.transform.position.x ? 1 : -1;
        JinaFx.Hit(at, away, new Color(1, .94f, .78f), amount > 30 ? 1.8f : 1.2f);
        JinaFx.Number(at + Vector3.up * .9f + Vector3.right * Random.Range(-.25f, .25f), before - EnemyHealth, amount > 30 ? new Color(1, .76f, .3f) : Color.white, amount > 30 ? 30 : 22);
        if (!EnemyDefeated) return;
        JinaFx.Stop(.16f);
        PrototypeView.Shake(.2f, .4f);
        // JinaActorVisual이 쓰러짐을 재생하는 동안 렌더러를 켜 두고, 그 뒤 스스로 숨는다.
        BossDownAt = Time.time;
        windupUntil = -1;
        if (Move != BossMove.None) EndMove(0);
        if (Quest != null) Quest.progress.Resolve(false);
    }
}
