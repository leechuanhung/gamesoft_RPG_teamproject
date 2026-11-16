using System.Collections.Generic;
using UnityEngine;

// Applies one slash per enemy along the double-tap dash path; K backstep stays defensive.
public class DashSlashAttack : MonoBehaviour
{
    PrototypeCombat combat;
    PrototypePlayer player;
    readonly HashSet<PrototypeCamp.Mob> struck = new HashSet<PrototypeCamp.Mob>();
    bool wasDashing, bossStruck;
    Vector3 previous;

    public void Initialize(PrototypeCombat owner)
    {
        combat = owner;
        player = owner.player;
        previous = transform.position;
        combat.Restarted += ResetDash;
    }

    void ResetDash()
    {
        wasDashing = false;
        bossStruck = false;
        struck.Clear();
        previous = transform.position;
    }

    void FixedUpdate()
    {
        if (player == null || combat == null) return;
        Vector3 now = transform.position;
        bool active = player.DashSlashDamage && !combat.Paused && combat.PlayerHealth > 0 && player.CanMove;
        if (!active)
        {
            wasDashing = false;
            previous = now;
            return;
        }
        if (!wasDashing)
        {
            wasDashing = true;
            bossStruck = false;
            struck.Clear();
            JinaFx.Dust(now, -player.Facing * 2f, 1.2f);
        }
        // Swept volume covers the whole path between physics frames, plus the leading blade.
        float halfReach = .75f;
        Vector3 midpoint = (previous + now) * .5f + new Vector3(player.Facing * .25f, .35f, 0);
        var hitbox = new Bounds(midpoint,
            new Vector3(Mathf.Abs(now.x - previous.x) + halfReach * 2f,
                        Mathf.Abs(now.y - previous.y) + 1.55f, 1f));
        if (combat.ApplyDashSlash(hitbox, struck, ref bossStruck))
        {
            Vector3 blade = now + new Vector3(player.Facing * .65f, .65f, 0);
            JinaFx.Arc(blade, player.Facing, 0f, 1.2f, new Color(.8f, .9f, 1f), .16f);
            PrototypeView.Shake(.065f, .12f);
        }
        previous = now;
    }

    void OnDestroy()
    {
        if (combat != null) combat.Restarted -= ResetDash;
    }
}
