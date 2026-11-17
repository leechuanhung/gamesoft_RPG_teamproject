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
    }

    public static readonly Dictionary<string, Stats> All = new Dictionary<string, Stats>
    {
        ["hwando"] = new Stats { id = "hwando", name = "환도", combo = true,
            damage = Combat.SwordDamage, stamina = Skills.AttackStamina, reach = 1.35f, carryScale = .92f, dropChance = .05f },
        ["bow"] = new Stats { id = "bow", name = "활", ranged = true,
            damage = Combat.ArrowDamage, stamina = Skills.AttackStamina, interval = .65f, hitDelay = SeonbiVisual.BowReleaseDelay, carryScale = 1f, dropChance = .05f },
        // 단검: 데미지 5, 스테미너 7, 사거리 1.2칸, 콤보 없음, 빠름.
        ["dagger"] = new Stats { id = "dagger", name = "단검",
            damage = 5, stamina = 7, reach = 1.2f, interval = .25f, hitDelay = .1f, carryScale = 1f, alwaysDrawn = true,
            art = "04_Weapons/dagger", grip = new Vector2(78, 78) },
    };

    // id로 무기 정보 찾기 (모르는 id면 환도)
    public static Stats Get(string id) => id != null && All.TryGetValue(id, out var s) ? s : All["hwando"];
}
