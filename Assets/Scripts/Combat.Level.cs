using UnityEngine;

// 레벨업 시스템 (Combat의 일부, partial class)
//   필요 경험치: Lv.1~10은 100, Lv.11~30은 150, Lv.31~49는 200. 만렙은 50.
//   레벨업할 때마다 최대 체력 +5, 최대 스테미너 +5. 2레벨마다 공격력 +1, 5레벨마다 방어력 +1.
//   Lv.10, 20, 30, 40, 50이 될 때 스테미너 회복 초당 +0.5 (화면에는 +1로 표시)
public partial class Combat
{
    public const int MaxLevel = 50;
    public const int HealthPerLevel = 5;      // 레벨업마다 최대 체력 증가
    public const int StaminaPerLevel = 5;     // 레벨업마다 최대 스테미너 증가
    public const float StaminaRegenPer10Levels = .5f;   // 10레벨마다 스테미너 회복 증가(초당)
    // 몹을 쓰러뜨리면 얻는 경험치
    public const int GhostExpMin = 4, GhostExpMax = 10;   // 들판 몹: 4~10 중 무작위 (모두 같은 확률)
    public const int WaterGhostExp = 20;      // 나루터 물귀신
    // 한양 가는 길의 몬스터(RoadMobs)와 보스를 해결하면 얻는 경험치(토벌이든 정화든 같다)
    public const int WispExp = 10, BruteExp = 20, RoadGhostExp = 12;
    public static readonly int[] SoldierExp = { 0, 10, 15, 20 };   // 관군 무장 단계별
    public const int DokkaebiExp = 100, KongExp = 150;
    // 레벨에 따른 공격력·방어력: 2레벨마다 공격력 +1(Lv.3 +1, Lv.5 +2 …), 5레벨마다 방어력 +1(Lv.6 +1, Lv.11 +2 …)
    public const int LevelsPerAttack = 2, LevelsPerDefense = 5;

    public int Level { get; private set; } = 1;
    public int Exp { get; private set; }       // 지금 레벨에서 모은 경험치
    public int MaxHealth => MaxPlayerHealth + (Level - 1) * HealthPerLevel;
    public int LevelAttack => (Level - 1) / LevelsPerAttack;
    public int LevelDefense => (Level - 1) / LevelsPerDefense;
    // 무기 피해에 더하는 공격력: 레벨 + 공격력 강화 아이템
    public int BonusAttack => LevelAttack + JinaItems.AttackBonus(this);
    public bool MaxedLevel => Level >= MaxLevel;

    // 지금 레벨에서 다음 레벨까지 필요한 경험치
    public static int ExpToNext(int level) => level <= 10 ? 100 : level <= 30 ? 150 : 200;

    // 경험치 얻기 (한 번에 여러 레벨이 오를 수도 있음)
    public void GainExp(int amount)
    {
        if (amount <= 0 || MaxedLevel || PlayerHealth <= 0) return;
        Exp += amount;
        while (!MaxedLevel && Exp >= ExpToNext(Level))
        {
            Exp -= ExpToNext(Level);
            Level++;
            ApplyLevelStats();
            // 늘어난 최대치만큼 지금 체력과 스테미너도 채워 줌
            PlayerHealth = Mathf.Min(PlayerHealth + HealthPerLevel, MaxHealth);
            player.RestoreStamina(StaminaPerLevel);
            // 레벨업 효과
            var at = player.transform.position;
            JinaFx.Rise(at, 16, new Color(1f, .86f, .35f), .5f, 2f, 1f);
            JinaFx.Glint(at + Vector3.up * .4f, new Color(1f, .9f, .5f));
            string text = "레벨 업! Lv." + Level;
            if ((Level - 1) % LevelsPerAttack == 0) text += "  공격력 +1";
            if ((Level - 1) % LevelsPerDefense == 0) text += "  방어력 +1";
            if (Level % 10 == 0) text += "  기력 회복 +1";
            JinaFx.Word(at + Vector3.up * 1.5f, text, new Color(1f, .86f, .35f), 24);
        }
        if (MaxedLevel) Exp = 0;
    }

    // 레벨에 따른 능력치 적용
    //   최대 스테미너 = 100 + (레벨 - 1) × 5
    //   스테미너 회복 = 초당 5 + (레벨 ÷ 10) × 0.5
    void ApplyLevelStats()
    {
        if (player == null) return;
        player.MaxStamina = Player.BaseMaxStamina + (Level - 1) * StaminaPerLevel;
        player.StaminaPerSecond = Player.BaseStaminaPerSecond + (Level / 10) * StaminaRegenPer10Levels;
    }

    // 처음부터 다시 할 때 Lv.1로
    void ResetLevel()
    {
        Level = 1; Exp = 0;
        ApplyLevelStats();
    }
}