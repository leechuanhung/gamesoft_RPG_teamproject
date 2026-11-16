using System.Collections.Generic;
using UnityEngine;

// W 돌진베기: 돌진하는 길에 닿은 적과 뒷산의 독을 하나마다 한 번씩 벤다(PrototypeCombat.ApplyDashSlash).
// Q 물러서기는 DashSlashing이 아니라서 베지 않는다.
public class DashSlashAttack : MonoBehaviour
{
    PrototypeCombat combat;
    PrototypePlayer player;
    // 이번 돌진에서 이미 벤 것(도깨비, 콩쥐 요괴, 유령, 뒷산의 독).
    readonly HashSet<object> struck = new HashSet<object>();
    bool wasDashing;
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
        struck.Clear();
        previous = transform.position;
    }

    void FixedUpdate()
    {
        if (player == null || combat == null) return;
        Vector3 now = transform.position;
        bool active = player.DashSlashing && !combat.Paused && combat.PlayerHealth > 0 && player.CanMove;
        if (!active)
        {
            wasDashing = false;
            previous = now;
            return;
        }
        if (!wasDashing)
        {
            wasDashing = true;
            struck.Clear();
            JinaFx.Dust(now, -player.Facing * 2f, 1.2f);
        }
        // Swept volume covers the whole path between physics frames, plus the leading blade.
        float halfReach = .75f;
        Vector3 midpoint = (previous + now) * .5f + new Vector3(player.Facing * .25f, .35f, 0);
        var hitbox = new Bounds(midpoint,
            new Vector3(Mathf.Abs(now.x - previous.x) + halfReach * 2f,
                        Mathf.Abs(now.y - previous.y) + 1.55f, 1f));
        if (combat.ApplyDashSlash(hitbox, struck))
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
