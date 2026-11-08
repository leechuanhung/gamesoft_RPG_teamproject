using System.Collections.Generic;
using UnityEngine;

// Arrows: aiming, the shot leaving the bow, and their flight until they hit something.
public partial class PrototypeCombat
{
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
    private readonly List<FlyingArrow> arrows = new List<FlyingArrow>();

    // Released at the bow's nock (raised or lowered for an aimed shot), flying along the aim.
    void Shoot()
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
            PrototypeCamp.Mob mob = null; PrototypeCamp mobCamp = null;
            foreach (var camp in Camps)
                if (camp != null)
                    foreach (var candidate in camp.mobs)
                    {
                        if (candidate.hp <= 0) continue;
                        float distance = ArrowHitDistance(candidate.art.bounds, origin, direction, step);
                        if (distance < enemyDistance) { enemyDistance = distance; mob = candidate; mobCamp = camp; }
                    }
            bool hitsEnemy = enemyDistance <= step && (wall.collider == null || enemyDistance < wall.distance);
            if (hitsEnemy) { if (mob != null) mobCamp.Hit(mob, ArrowDamage); else DamageEnemy(ArrowDamage); }
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
}
