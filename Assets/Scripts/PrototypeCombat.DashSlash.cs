using System.Collections.Generic;
using UnityEngine;

// W 돌진베기의 피해(DashSlashAttack이 돌진하는 동안 부른다). 피해는 전투 쪽이 맡아서, 돌진 이동은
// 적의 속사정을 몰라도 된다. 돌진베기의 피해는 여기 한 곳에서만 들어간다.
public partial class PrototypeCombat
{
    public const int DashSlashDamage = PrototypeSkills.RushDamage;

    // `swept`에 닿은 도깨비·콩쥐 요괴·유령을 베고 뒷산의 독을 친다. `alreadyHit`: 이번 돌진에서 이미 벤 것
    // (도깨비는 enemy, 콩쥐는 KongBoss, 유령은 Mob, 독은 그 독). 돌진 한 번에 하나마다 한 번만 맞는다.
    public bool ApplyDashSlash(Bounds swept, HashSet<object> alreadyHit)
    {
        if (Paused || PlayerHealth <= 0) return false;
        bool landed = false;
        if (enemy != null && !EnemyDefeated && swept.Intersects(enemy.bounds) && alreadyHit.Add(enemy))
        {
            DamageEnemy(DashSlashDamage);
            landed = true;
        }
        // 콩쥐 요괴: 칼질처럼 마을에서 사람을 죽이려던 손을 멈칫하게 하는 데에도 센다.
        if (KongBoss != null && KongBoss.Fighting && swept.Intersects(KongBoss.HurtBox) && alreadyHit.Add(KongBoss)
            && KongBoss.Damage(DashSlashDamage, true))
            landed = true;
        // 뒷산의 독: 칼질처럼 한 대(피해와 상관없이 두 대에 깨짐).
        if (KongBoss != null && KongBoss.BlowCrocks(swept, alreadyHit)) landed = true;
        foreach (var camp in Camps)
        {
            if (camp == null) continue;
            foreach (var mob in camp.mobs)
            {
                if (mob == null || mob.hp <= 0 || !swept.Intersects(mob.art.bounds) || !alreadyHit.Add(mob)) continue;
                camp.Hit(mob, DashSlashDamage);
                landed = true;
            }
        }
        return landed;
    }
}
