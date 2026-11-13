using System.Collections.Generic;
using UnityEngine;

// The combat layer owns damage, so dash movement does not depend on enemy internals.
public partial class PrototypeCombat
{
    public const int DashSlashDamage = 20;

    public bool ApplyDashSlash(Bounds swept, HashSet<PrototypeCamp.Mob> alreadyHit, ref bool bossHit)
    {
        if (Paused || PlayerHealth <= 0) return false;
        bool landed = false;
        if (!bossHit && enemy != null && !EnemyDefeated && swept.Intersects(enemy.bounds))
        {
            bossHit = true;
            DamageEnemy(DashSlashDamage);
            landed = true;
        }
        foreach (var camp in Camps)
        {
            if (camp == null) continue;
            foreach (var mob in camp.mobs)
            {
                if (mob == null || mob.hp <= 0 || alreadyHit.Contains(mob)) continue;
                if (!swept.Intersects(mob.art.bounds)) continue;
                alreadyHit.Add(mob);
                camp.Hit(mob, DashSlashDamage);
                landed = true;
            }
        }
        return landed;
    }
}
