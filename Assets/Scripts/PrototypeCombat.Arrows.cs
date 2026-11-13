using System.Collections.Generic;
using UnityEngine;

// 화살: 조준, 활을 떠나는 순간, 무언가에 맞을 때까지의 비행.
public partial class PrototypeCombat
{
    private class FlyingArrow
    {
        public SpriteRenderer art;
        public Vector2 direction;
        public float remaining = 12f;
    }
    // 쏠 때 위나 아래를 누르고 있으면 활을 30도 위·아래로 겨눈다(bow_up / bow_down 프레임).
    public const float AimAngle = 30f;
    private int pendingAim;
    // 오른쪽을 보고 쏠 때 화살이 활을 떠나는 위치(발 기준). 수평 사격은 그려진 시위 높이에서 1.1 앞,
    // 조준 사격은 그 점을 놓는 프레임의 어깨(x 400, 위로 285px)를 축으로 돌린다.
    public static Vector2 ArrowLaunch(int aim)
    {
        var level = new Vector2(1.1f, JinaArt.BowArrowHeight);
        if (aim == 0) return level;
        var pivot = new Vector2(100f, 285f) / (510f / 1.9f);
        return pivot + (Vector2)(Quaternion.Euler(0, 0, AimAngle * aim) * (level - pivot));
    }
    // 조준 단계와 방향으로 화살이 날아갈 방향을 구한다.
    public static Vector2 ArrowDirection(int aim, int facing) =>
        new Vector2(facing * Mathf.Cos(AimAngle * aim * Mathf.Deg2Rad), Mathf.Sin(AimAngle * aim * Mathf.Deg2Rad));
    private readonly List<FlyingArrow> arrows = new List<FlyingArrow>();

    // 활의 시위(조준하면 올리거나 내린 위치)에서 놓아 조준 방향으로 날아간다.
    void Shoot()
    {
        // 놓을 때 소모한다. 대화창 때문에 취소된 사격은 화살이 남는다.
        Arrows--;
        slashUntil = 0;
        var arrow = MakeSprite("Arrow", new Vector2(.55f, .1f), new Color(1f,.85f,.35f));
        arrow.sprite = JinaArt.Load("04_Weapons/arrow");
        arrow.transform.localScale = Vector3.one;
        arrow.color = Color.white;
        arrow.flipX = attackFacing < 0;
        arrow.sortingOrder = 4;
        arrow.transform.rotation = Quaternion.Euler(0, 0, AimAngle * pendingAim * attackFacing);
        // 점프 중에도 SeonbiVisual과 같은 발 기준을 쓴다.
        var launch = ArrowLaunch(pendingAim);
        arrow.transform.position = new Vector3(player.transform.position.x + attackFacing * launch.x,
            playerArt.bounds.min.y + launch.y, 0);
        arrows.Add(new FlyingArrow { art = arrow, direction = ArrowDirection(pendingAim, attackFacing) });
        JinaFx.Glint(arrow.transform.position, new Color(1, .96f, .8f));
        JinaFx.Dots(arrow.transform.position, 3, Color.white, 1.8f, .18f, attackFacing);
    }

    // 매 프레임 지나간 길을 훑어서 빠른 화살이 대상이나 벽을 뚫고 지나가지 않게 한다.
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
            bool kong = false; int crock = -1;
            if (KongBoss != null && KongBoss.Fighting)
            {
                float d = ArrowHitDistance(KongBoss.HurtBox, origin, direction, step);
                if (d < enemyDistance) { enemyDistance = d; kong = true; }
            }
            if (KongBoss != null)
            {
                float d = KongBoss.CrockArrowDistance(origin, direction, step, out int index);
                if (d < enemyDistance) { enemyDistance = d; kong = false; crock = index; }
            }
            PrototypeCamp.Mob mob = null; PrototypeCamp mobCamp = null;
            foreach (var camp in Camps)
                if (camp != null)
                    foreach (var candidate in camp.mobs)
                    {
                        if (candidate.hp <= 0) continue;
                        float distance = ArrowHitDistance(candidate.art.bounds, origin, direction, step);
                        if (distance < enemyDistance) { enemyDistance = distance; mob = candidate; mobCamp = camp; kong = false; crock = -1; }
                    }
            bool hitsEnemy = enemyDistance <= step && (wall.collider == null || enemyDistance < wall.distance);
            if (hitsEnemy)
            {
                if (mob != null) mobCamp.Hit(mob, ArrowDamage);
                else if (crock >= 0) KongBoss.BlowCrock(crock);
                else if (kong) KongBoss.Damage(ArrowDamage, false);
                else DamageEnemy(ArrowDamage);
                JinaFx.Stop(.03f);
            }
            arrow.remaining -= step;
            // 뒤로 옅은 줄기, 땅에 꽂히면 부스러기.
            if (Time.frameCount % 2 == 0) JinaFx.Rise(origin, 1, new Color(1, .96f, .84f, .7f), 0, 0, .14f);
            if (wall.collider != null && !hitsEnemy) JinaFx.Dots(origin + direction * wall.distance, 5, new Color(.7f, .6f, .46f), 2.2f, .28f, 0, 10);
            if (hitsEnemy || wall.collider != null || arrow.remaining <= 0)
            {
                Destroy(arrow.art.gameObject);
                arrows.RemoveAt(i);
            }
            else arrow.art.transform.position = origin + direction * step;
        }
    }

    // 화살은 땅과 벽에서 멈추지만 한 방향 발판은 통과한다.
    static RaycastHit2D Wall(Vector2 origin, Vector2 direction, float distance)
    {
        foreach (var hit in Physics2D.RaycastAll(origin, direction, distance, 1 << 8))
            if (!hit.collider.usedByEffector) return hit;
        return default;
    }
    // 수평 사격용: 대상에 맞기까지의 거리(안 맞으면 무한대).
    public static float ArrowHitDistance(Bounds target, Vector2 origin, int direction, float distance) =>
        ArrowHitDistance(target, origin, Vector2.right * direction, distance);
    // 화살이 대상에 맞기까지의 거리(안 맞으면 무한대).
    public static float ArrowHitDistance(Bounds target, Vector2 origin, Vector2 direction, float distance)
    {
        // 화살 두께를 고려하되, 피해 판정은 실제 지나간 구간을 넘지 않게 한다.
        target.Expand(new Vector3(.1f, .1f, 0));
        if (target.Contains(origin)) return 0;
        float hit;
        return target.IntersectRay(new Ray(origin, direction.normalized), out hit) && hit <= distance
            ? hit : float.PositiveInfinity;
    }

    // 날고 있는 화살을 모두 지운다(재시작).
    void ClearArrows()
    {
        foreach (var arrow in arrows)
            if (arrow.art != null) Destroy(arrow.art.gameObject);
        arrows.Clear();
    }
}
