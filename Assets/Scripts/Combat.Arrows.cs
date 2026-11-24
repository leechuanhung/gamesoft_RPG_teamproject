using System.Collections.Generic;
using UnityEngine;

// 화살: 조준, 활을 떠나는 순간, 무언가에 맞을 때까지의 비행.
public partial class Combat
{
    private class FlyingArrow
    {
        public SpriteRenderer art;
        public Vector2 direction;
        public float remaining = 12f;
        public int damage = ArrowDamage;   // 쏜 활의 데미지
        public bool lightning;             // 벽조궁 화살: 맞을 때 번개 불꽃
        public bool power;                 // 강궁 화살: 공격력 2배, 금빛
    }
<<<<<<< HEAD
    // 마우스 조준 각도(도). 바라보는 쪽 기준이고 위가 +, 범위는 −90~90. 칼과 활 모두 이 방향으로 나간다.
    private float pendingAngle;
    // 바라보는 쪽 기준 각도를 실제 방향으로 바꾼다(길이 1).
    public static Vector2 AimDirection(float angle, int facing) =>
        new Vector2(facing * Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
    // 겨눈 몸 그림(Tools/ArtGen/make_aim.py)은 활 15도, 칼 30도마다 있다(bow_aim_u45_00, sword_aim_d90_02 …).
    //   그 사이 각도는 가까운 그림을 쓰고, 화살과 칼빛은 마우스 각도 그대로 나간다.
    public const int BowAimStep = 15, SwordAimStep = 30;
    public static int AimBucket(float angle, int step) => Mathf.Clamp(Mathf.RoundToInt(angle / step), -90 / step, 90 / step) * step;
    // 90도로 겨눌 때 어깨 위가 기우는 양(그림 px, make_aim.py의 LEAN). 위로 겨누면 뒤로(−), 아래로 겨누면 앞으로(+).
    public const float AimLean = 60f;
    public static float AimLeanPixels(int bucket) => -Mathf.Round(AimLean * bucket / 90f / 10f) * 10f;
    // 오른쪽을 보고 쏠 때 화살이 활을 떠나는 위치(발 기준). 수평 사격은 그려진 시위 높이에서 1.1 앞.
    // 겨눈 그림은 쏘는 그림(2번)의 어깨(x 400, 위로 285px)를 축으로 팔을 돌리고, 어깨 위가 기운 만큼 옮긴다.
    public static Vector2 ArrowLaunch(int bucket)
=======
    // E 강궁: 켜져 있으면 다음 화살 한 발이 2배 (Skills.cs가 켜고, 쏘면 꺼짐)
    public bool PowerShotReady { get; set; }
    // 쏠 때 위나 아래를 누르고 있으면 활을 30도 위·아래로 겨눈다(bow_up / bow_down 프레임).
    public const float AimAngle = 30f;
    private int pendingAim;
    // 오른쪽을 보고 쏠 때 화살이 활을 떠나는 위치(발 기준). 수평 사격은 그려진 시위 높이에서 1.1 앞,
    // 조준 사격은 그 점을 놓는 프레임의 어깨(x 400, 위로 285px)를 축으로 돌린다.
    public static Vector2 ArrowLaunch(int aim)
>>>>>>> origin/main
    {
        var level = new Vector2(1.1f, JinaArt.BowArrowHeight);
        if (bucket == 0) return level;
        const float unit = 510f / 1.9f;
        var pivot = new Vector2(100f, 285f) / unit;
        return pivot + (Vector2)(Quaternion.Euler(0, 0, bucket) * (level - pivot)) + Vector2.right * AimLeanPixels(bucket) / unit;
    }
    private readonly List<FlyingArrow> arrows = new List<FlyingArrow>();

    // 그려진 활의 시위(가장 가까운 겨눈 그림)에서 놓아 마우스 각도 그대로 날아간다.
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
        arrow.transform.rotation = Quaternion.Euler(0, 0, pendingAngle * attackFacing);
        // 점프 중에도 SeonbiVisual과 같은 발 기준을 쓴다.
        var launch = ArrowLaunch(AimBucket(pendingAngle, BowAimStep));
        arrow.transform.position = new Vector3(player.transform.position.x + attackFacing * launch.x,
            playerArt.bounds.min.y + launch.y, 0);
<<<<<<< HEAD
        arrows.Add(new FlyingArrow { art = arrow, direction = AimDirection(pendingAngle, attackFacing) });
=======
        var bow = pendingWeapon ?? CurrentStats;
        bool lightning = bow.id == "byeokjogung";
        // 강궁: 이번 한 발만 공격력 2배, 크고 금빛인 화살
        bool power = PowerShotReady;
        PowerShotReady = false;
        int damage = power ? bow.damage * Skills.PowerShotMultiplier : bow.damage;
        if (power)
        {
            arrow.transform.localScale = Vector3.one * 1.35f;
            arrow.color = new Color(1f, .85f, .4f);
            JinaFx.Dots(arrow.transform.position, 10, new Color(1f, .8f, .3f), 3f, .3f, attackFacing);
        }
        arrows.Add(new FlyingArrow { art = arrow, direction = ArrowDirection(pendingAim, attackFacing), damage = damage, lightning = lightning, power = power });
>>>>>>> origin/main
        JinaFx.Glint(arrow.transform.position, new Color(1, .96f, .8f));
         // 벽조궁: 쏠 때 활 앞으로 짧은 번개 두 줄기 + 하늘색 튀김
        if (lightning)
        {
            var from = arrow.transform.position;
            var dir = (Vector3)ArrowDirection(pendingAim, attackFacing);
            Lightning.Bolt(from, from + dir * 1.1f + Vector3.up * .15f, .04f, .14f, .12f);
            Lightning.Bolt(from, from + dir * .8f - Vector3.up * .15f, .03f, .12f, .1f);
            JinaFx.Dots(from, 6, new Color(.55f, .8f, 1f), 2.4f, .25f, attackFacing);
        }
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
            Camp.Mob mob = null; Camp mobCamp = null;
            foreach (var camp in Camps)
                if (camp != null)
                    foreach (var candidate in camp.mobs)
                    {
                        if (candidate.hp <= 0) continue;
                        float distance = ArrowHitDistance(candidate.art.bounds, origin, direction, step);
                        if (distance < enemyDistance) { enemyDistance = distance; mob = candidate; mobCamp = camp; kong = false; crock = -1; }
                    }
            // 한양 가는 길의 몬스터·관군과 좌의정.
            RoadMobs.Mob roadMob = null; bool final = false;
            if (Mobs != null)
            {
                var candidate = Mobs.ArrowTarget(origin, direction, step, out float d);
                if (d < enemyDistance) { enemyDistance = d; roadMob = candidate; mob = null; kong = false; crock = -1; }
            }
            if (FinalBoss != null && FinalBoss.Fighting)
            {
                float d = ArrowHitDistance(FinalBoss.HurtBox, origin, direction, step);
                if (d < enemyDistance) { enemyDistance = d; final = true; roadMob = null; mob = null; kong = false; crock = -1; }
            }
            bool hitsEnemy = enemyDistance <= step && (wall.collider == null || enemyDistance < wall.distance);
            if (hitsEnemy)
            {
                if (final) FinalBoss.Damage(ArrowDamage);
                else if (roadMob != null) Mobs.Hit(roadMob, ArrowDamage);
                else if (mob != null) mobCamp.Hit(mob, ArrowDamage);
                else if (crock >= 0) KongBoss.BlowCrock(crock);
                else if (kong) KongBoss.Damage(arrow.damage, false);
                else DamageEnemy(arrow.damage);
                JinaFx.Stop(.03f);
                // 강궁 화살: 맞은 자리에 금빛 폭발
                if (arrow.power)
                {
                    var burst = origin + direction * enemyDistance;
                    JinaFx.Hit(burst, direction.x >= 0 ? 1 : -1, new Color(1f, .82f, .3f), 1.6f);
                    JinaFx.Dots(burst, 16, new Color(1f, .85f, .35f), 4f, .4f);
                    View.Shake(.08f, .15f);
                    JinaFx.Stop(.06f);
                }
                // 벽조궁 화살: 맞은 자리에 번개 불꽃
                if (arrow.lightning)
                {
                    var spark = origin + direction * enemyDistance;
                    // 하늘에서 번개가 내리꽂힘 + 옆 갈래 번개
                    Lightning.Bolt((Vector3)spark + new Vector3(Random.Range(-.4f, .4f), 4f, 0), spark, .09f, .24f, .35f);
                    Lightning.Bolt((Vector3)spark + new Vector3(Random.Range(-1f, 1f), 2.2f, 0), spark, .04f, .18f, .2f);
                    JinaFx.Glint(spark, new Color(.6f, .85f, 1f));
                    JinaFx.Dots(spark, 14, new Color(1f, .92f, .45f), 3.5f, .35f);
                    JinaFx.Dots(spark, 8, new Color(.55f, .8f, 1f), 2.5f, .3f);
                    View.Shake(.06f, .12f);
                }
            }
            arrow.remaining -= step;
            if (Time.frameCount % 2 == 0) JinaFx.Rise(origin, arrow.power ? 3 : 1,
                arrow.power ? new Color(1f, .8f, .3f, .9f) : arrow.lightning ? new Color(.55f, .8f, 1f, .8f) : new Color(1, .96f, .84f, .7f), 0, 0, arrow.power ? .25f : .14f);
            // 벽조궁 화살: 날아가며 가끔 작은 번개가 튐
            if (arrow.lightning && Random.value < .15f)
                Lightning.Bolt(origin, origin - direction * .6f + (Vector2)Random.insideUnitCircle * .2f, .025f, .08f, .08f);
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
