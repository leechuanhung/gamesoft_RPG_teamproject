using System.Collections.Generic;
using UnityEngine;

// 무기 표: 무기마다 데미지, 스테미너, 사거리, 공격 간격 등을 한곳에 모아 둔다.
// 새 무기를 넣을 땐 아래 All에 한 줄 추가하고, 보따리(InventoryUI.Awake)에서 UnlockWeapon 하면 된다.
// 실제 공격 처리는 Combat이 이 표를 읽어서 한다.
public class Weapon : MonoBehaviour
{
    public class Stats
    {
        public string id, name;
        public bool ranged;       // 활처럼 화살을 쏨
        public bool combo;        // 환도처럼 3연격 (간격·맞는 시점은 SeonbiVisual이 정함)
        public int damage;        // 한 번 맞을 때 데미지
        public int stamina;       // 공격 1회 스테미너
        public float reach;       // 근접 사거리(칸)
        public float interval;    // 공격 간격(초), 콤보 무기는 안 씀
        public float hitDelay;    // 휘두르고 맞기까지(초), 콤보 무기는 안 씀
        public float carryScale;  // 들고 다닐 때 그림 크기
        public bool alwaysDrawn;  // 검집 없이 항상 뽑고 있음
        public string art;        // 전용 그림 경로 (없으면 환도 그림)
        public Vector2 grip;      // 그림에서 손잡이를 쥐는 점(픽셀, 왼쪽 아래 기준)
        public float dropChance;  // 들판 몹이 떨어뜨릴 확률 (0이면 안 떨어뜨림, .05f = 5%)
        public string sheathedArt; // 검집에 꽂았을 때 그림 (없으면 art 그대로)
        public int[] comboDamage;     // 콤보 무기: 1·2·3타 데미지 (없으면 damage)
        public float comboInterval;   // 콤보 무기: 한 타마다 걸리는 시간(초) (0이면 환도 기본값)
        public string bowArt;         // 활 무기: 전용 활 그림 (없으면 기본 활 그림)
        public bool hwandoSlash;      // 전용 그림 무기라도 칼빛·이펙트는 환도와 똑같이
        public int evilLimit;         // 악행이 이만큼 쌓이면 장착 불가 (0이면 제한 없음)

        // 이번 타(step 0·1·2)에 걸리는 시간, 맞는 시점, 데미지
        public float Swing(int step) => combo ? (comboInterval > 0 ? comboInterval : SeonbiVisual.SwordLength(step)) : interval;
        public float HitAt(int step) => combo ? SeonbiVisual.SwordHit(step) * Swing(step) / SeonbiVisual.SwordLength(step) : hitDelay;
        public int Damage(int step) => combo && comboDamage != null ? comboDamage[step] : damage;
    }

    public static readonly Dictionary<string, Stats> All = new Dictionary<string, Stats>
    {
        // 환도: 1타 6, 2타 10, 3타 14 / 스테미너 8 / 사거리 1.35칸 / 한 타 0.3초
        ["hwando"] = new Stats { id = "hwando", name = "환도", combo = true,
            damage = 10, comboDamage = new[] { 6, 10, 14 }, comboInterval = .35f,
            stamina = 8, reach = 1.35f, carryScale = .92f, dropChance = .05f },
        // 활: 데미지 10 / 스테미너 6 / 0.65초마다
        ["bow"] = new Stats { id = "bow", name = "활", ranged = true,
            damage = Combat.ArrowDamage, stamina = 6, interval = .65f, hitDelay = SeonbiVisual.BowReleaseDelay, carryScale = 1f, dropChance = 0},
        // 벽조궁: 번개 맞은 대추나무로 만든 활. 데미지 = 활의 1.5배(15) / 스테미너 6 / 0.65초마다 / 들판 몹 3% 드랍
        ["byeokjogung"] = new Stats { id = "byeokjogung", name = "벽조궁", ranged = true,
            damage = Combat.ArrowDamage * 3 / 2, stamina = 6, interval = .65f, hitDelay = SeonbiVisual.BowReleaseDelay,
            carryScale = 1f, dropChance = .03f, bowArt = "04_Weapons/byeokjo_bow" },
         // 사인검: 환도보다 조금 강함. 1타 7, 2타 12, 3타 16 / 나머지는 환도와 같음
        //   퀘스트 보상 전용(드랍 없음) / 악행 20 이상이면 장착 불가
        ["saingeom"] = new Stats { id = "saingeom", name = "사인검", combo = true,
            damage = 16, comboDamage = new[] { 7, 12, 16 }, comboInterval = .35f,
            stamina = 8, reach = 1.35f, carryScale = 1.47f, dropChance = 0,
            art = "04_Weapons/saingeom", sheathedArt = "04_Weapons/saingeom_sheathed", grip = new Vector2(103, 91),
            hwandoSlash = true, evilLimit = 20 },
        // 단검: 데미지 5 / 스테미너 4 / 사거리 1.1칸 / 0.25초마다
        ["dagger"] = new Stats { id = "dagger", name = "단검",
            damage = 5, stamina = 4, reach = 1.1f, interval = .25f, hitDelay = .1f, carryScale = 1f, alwaysDrawn = true,
            art = "04_Weapons/dagger", grip = new Vector2(78, 78) },
        // 명검: 1타 13, 2타 15, 3타 18 / 스테미너 10 / 사거리 1.4칸 / 한 타 0.35초 / 들판 몹 1% 드랍
        ["myeonggeom"] = new Stats { id = "myeonggeom", name = "명검", combo = true,
            damage = 18, comboDamage = new[] { 13, 15, 18 }, comboInterval = .4f,
            stamina = 10, reach = 1.4f, carryScale = 1.84f, dropChance = .01f,
            art = "04_Weapons/myeonggeom", sheathedArt = "04_Weapons/myeonggeom_sheathed", grip = new Vector2(126, 150) },
    };

    // id로 무기 정보 찾기 (모르는 id면 환도)
    public static Stats Get(string id) => id != null && All.TryGetValue(id, out var s) ? s : All["hwando"];
}
