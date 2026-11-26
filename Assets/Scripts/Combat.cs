using UnityEngine;
using System.Collections.Generic;

// A small combat loop, attached automatically by the existing test scene: the seonbi's health and attacks,
// restarts and pausing, and the managers it attaches (quest, camp, shops, village, story scenes).
// The boss is in Combat.Boss.cs and the arrows in Combat.Arrows.cs.
public partial class Combat : MonoBehaviour
{
    public bool InventoryPauseActive { get; set; }
    public bool SettingsPauseActive { get; set; }
    public event System.Action Restarted;
    public Player player;
    public const int MaxPlayerHealth = 100;
    public int PlayerHealth { get; private set; } = MaxPlayerHealth;
    public const int PlayerAttack = 10;
    public int PlayerDefense = 0;
    public int EffectivePlayerDefense => PlayerDefense + LevelDefense + JinaItems.DefenseBonus(this);
    // 스킬: 스킬 무적(Q 물러서기)이 끝나는 시각과 스킬 관리자
    private float skillInvulnerableUntil;
    public bool SkillInvulnerable => Time.time < skillInvulnerableUntil;
    public void GrantSkillInvulnerable(float seconds) { skillInvulnerableUntil = Mathf.Max(skillInvulnerableUntil, Time.time + seconds); }
    public Skills Skills { get; private set; }
    // A ward charm (JinaItems) takes the next blow in his place; the message shows for a moment after.
    public bool Warded { get; private set; }
    public float WardBrokeAt { get; private set; } = -1;
    public void GiveWard() { Warded = true; }
    // Coins found on fallen ghosts, shown for a moment.
    public float CoinsFoundAt { get; private set; } = -1;
    public int CoinsFound { get; private set; }
    // 쓰러뜨린 것이 무엇이었는지 알리는 글. {0} 자리에 엽전 수가 들어간다.
    public string CoinsNote { get; private set; } = DefaultCoinsNote;
    public const string DefaultCoinsNote = "쓰러진 망령이 엽전 {0}문을 떨어뜨렸습니다.";
    public void FindCoins(int amount, string note = DefaultCoinsNote) { if (Tavern == null) return; Tavern.wallet.Earn(amount); CoinsFound = amount; CoinsFoundAt = Time.time; CoinsNote = note; }
    // ── 보스(도깨비) 능력치 ──
    public const int EnemyMaxHealth = 200;
    public const int BossAttack = 20;
    // The elder's last blow takes a third of the boss's health.
    public const int ElderCounterDamage = 67;
    public int BossDefense = 5;
    // ── 일반 몬스터 능력치 (Camp.cs에서 사용) ──
    public const int MonsterMaxHealth = 30;
    public const int MonsterAttack = 10;
    public const int MonsterDefense = 2;
    // 환도와 화살 모두 플레이어 공격력을 그대로 사용
    public const int SwordDamage = PlayerAttack, ArrowDamage = PlayerAttack;
    public int EnemyHealth { get; private set; } = EnemyMaxHealth;
    // 데미지 공식: 공격력 - 방어력, 최소 1
    public static int CalcDamage(int attack, int defense) => Mathf.Max(1, attack - defense);
    public bool BossPurified => Quest != null && Quest.progress.Outcome == EncounterOutcome.Purified;
    public bool EnemyDefeated => EnemyHealth <= 0 || BossPurified;
    private float playerImpactAt = -1;
    // When he last took a hit and lived: SeonbiVisual plays the recoil from here.
    public float PlayerHurtAt { get; private set; } = -1;
    private bool pendingBow;
    private SpriteRenderer enemy;
    private SpriteRenderer slash;
    private SpriteRenderer playerArt;
    private Color playerColor;
    private float nextAttack, slashUntil, invulnerableUntil, enemyReadyAt, hitFlashUntil;
    private float windupUntil = -1;
    private Vector2 lockedStrike;
    private int attackFacing = 1;
    // The sword's three steps: the first cut, a rising return, then a heavy finishing blow; each must follow in time.
    private int comboStep = -1, pendingStep;
    private float comboUntil;
    public const float ComboWindow = .3f;
    public int ComboStep => comboStep;
    private float lastPlayerHit = -1;
    private SeonbiVisual playerVisual;
    public ElderQuest Quest { get; private set; }
    // ── 무기: 1·2칸 장착(보따리 무기 탭), S로 전환. 수치는 Weapon.cs ──
    public readonly string[] EquippedWeapons = { "dagger", "bow" };
    public int ActiveWeaponSlot { get; private set; }
    public void RestoreActiveWeaponSlot(int slot)
    {
        ActiveWeaponSlot = Mathf.Clamp(slot, 0, 1);
        if (EquippedWeapons[ActiveWeaponSlot] == null && EquippedWeapons[1 - ActiveWeaponSlot] != null)
            ActiveWeaponSlot = 1 - ActiveWeaponSlot;
    }
    public string CurrentWeapon => EquippedWeapons[ActiveWeaponSlot] ?? EquippedWeapons[1 - ActiveWeaponSlot] ?? "dagger";
    public Weapon.Stats CurrentStats => Weapon.Get(CurrentWeapon);
    // 악행 = 선악 값이 음수일 때 그 크기 (선악 -25 → 악행 25)
    public int Evil => Quest != null ? Mathf.Max(0, -Quest.progress.Honor) : 0;
    // 무기 장착 조건 (사인검: 악행 20 이상이면 불가)
    public bool CanEquip(Weapon.Stats w) => w.evilLimit <= 0 || Evil < w.evilLimit;
    public bool UsingBow => CurrentStats.ranged;
    private Weapon.Stats pendingWeapon;
    private bool alerted;
    private bool strikeElder;
    public Camp Camp { get; private set; }
    // The river's water ghosts at the ferry. Felling KeepsakeAt of them (they rise again) frees the drowned
    // mother's keepsake, which goes into the inventory once a run.
    public Camp River { get; private set; }
    public const int KeepsakeAt = 10;
    public int RiverFelled { get; private set; }
    public float KeepsakeFreedAt { get; private set; } = -1;
    // 한양 가는 길의 유령: 버려진 역참과 북망산 무덤길(다른 몬스터·관군과 섞여 나온다, RoadMobs).
    public Camp StationGhosts { get; private set; }
    public Camp GraveGhosts { get; private set; }
    public Camp[] Camps => new[] { Camp, River, StationGhosts, GraveGhosts };
    // 한양 가는 길: 도깨비불·두억시니·관군, 봉화 산성, 한양 사람들, 최종 보스.
    public RoadMobs Mobs { get; private set; }
    public BeaconFort Fort { get; private set; }
    public HanyangTown Town { get; private set; }
    public FinalBoss FinalBoss { get; private set; }
    public void FellWaterGhost()
    {
        if (++RiverFelled != KeepsakeAt) return;
        KeepsakeFreedAt = Time.time;
        var bag = JinaItems.Inventory;
        if (bag != null) bag.TryAdd(JinaItems.Keepsake);
    }
    public TavernShop Tavern { get; private set; }
    public PeddlerShop Peddler { get; private set; }
    public VillageAlarm Village { get; private set; }
    public BossRemorse Remorse { get; private set; }
    public BossEncounter Encounter { get; private set; }
    public ShopChatter Chatter { get; private set; }
    public BossTalk BossTalk { get; private set; }
    public int Arrows { get; private set; } = ShopRules.MaxArrows;
    private float noArrowsUntil;
    public void AddArrows(int amount) { Arrows = Mathf.Clamp(Arrows + amount, 0, ShopRules.MaxArrows); }
    public void SetArrowsFromSave(int amount) { Arrows = Mathf.Clamp(amount, 0, ShopRules.MaxArrows); noArrowsUntil = 0; ClearArrows(); }
    public IntroNarration Intro { get; private set; }
    public EndingNarration Ending { get; private set; }
    public GameOver GameOver { get; private set; }
    public Maps Maps { get; private set; }
    public GameAudio Audio { get; private set; }
    public Ferry Ferry { get; private set; }
    public Hud Hud { get; private set; }
    public SeonbiGridPuzzle Puzzles { get; private set; }
    public MountainPass Pass { get; private set; }
    public KongVillage Kong { get; private set; }
    public KongNightStay Stay { get; private set; }
    public MapAmbience Ambience { get; private set; }
    public RoadsideLore Lore { get; private set; }
    // The Kongjwi demon on the back hill, and the scenes after her fight.
    public KongBoss KongBoss { get; private set; }
    public KongAftermath Aftermath { get; private set; }
    // The checkpoint at the foot of the back hill (뒷산 입구), saved when Kongjwi starts speaking. Until she is dealt
    // with, R after a death starts him there with what he had then (honour, arrows, everyone in Kongbat alive) and her
    // waiting on the hill again, instead of from the very beginning. The bag and the story so far are kept as they are.
    public struct HillSave { public int honor, lost, arrows; }
    public HillSave? Checkpoint { get; private set; }
    public static readonly Vector2 HillEntrance = new Vector2(WorldBuilder.HillStart + 1.5f, .3f);
    public bool CanRetryAtHill => Checkpoint.HasValue && KongBoss != null && !KongBoss.Resolved;
    public float RevivedAt { get; private set; } = -1;
    public void SaveAtHill()
    {
        if (Quest == null) return;
        Checkpoint = new HillSave { honor = Quest.progress.Honor, lost = Quest.progress.VillagersLost, arrows = Arrows };
    }
    public void RestartAtHill()
    {
        if (!Checkpoint.HasValue) { Restart(); return; }
        JinaItems.ClearEffects();
        var save = Checkpoint.Value;
        PlayerHealth = MaxHealth;
        nextAttack = slashUntil = invulnerableUntil = hitFlashUntil = 0;
        playerImpactAt = -1;
        PlayerDownAt = PlayerHurtAt = WardBrokeAt = CoinsFoundAt = -1;
        comboStep = -1; comboUntil = 0; lastPlayerHit = -1;
        JinaFx.Clear();
        View.Shake(0, .01f); View.Focus(0, 0);
        playerArt.color = playerColor;
        player.RespawnAt(HillEntrance);
        ActiveWeaponSlot = 0; Arrows = save.arrows; noArrowsUntil = 0;
        ClearArrows();
        if (Quest != null) Quest.progress.RewindKong(save.honor, save.lost);
        if (Kong != null) Kong.ResetRaid();
        if (Pass != null) Pass.ClearRocks();
        if (KongBoss != null) KongBoss.Retry();
        if (Aftermath != null) Aftermath.ResetAftermath();
        if (playerVisual != null) playerVisual.ResetPose();
        if (Maps != null) Maps.Jump();
        RevivedAt = Time.time;
    }
    // 콩쥐 요괴를 해결한 뒤 한양 가는 길의 체크포인트(산성, 한양 거리, 좌의정 술자리). 쓰러지면 R로 거기서 다시
    // 일어난다: 그때의 선악과 화살, 좌의정은 다시 누각에서 기다리고 관군 무리는 다시 나올 수 있다.
    public struct RoadSave { public Vector2 at; public int honor, arrows; }
    public RoadSave? RoadCheckpoint { get; private set; }
    public bool CanRetryOnRoad => RoadCheckpoint.HasValue && (FinalBoss == null || !FinalBoss.Resolved);
    public float RevivedOnRoadAt { get; private set; } = -1;
    public void SaveOnRoad(Vector2 at)
    {
        if (Quest == null) return;
        RoadCheckpoint = new RoadSave { at = at, honor = Quest.progress.Honor, arrows = Arrows };
    }
    // 추가 퀘스트 보상은 보따리처럼 체크포인트 재시도 때도 유지한다.
    public void PreserveQuestHonor(int delta)
    {
        if (RoadCheckpoint.HasValue)
        {
            var save = RoadCheckpoint.Value;
            save.honor += delta;
            RoadCheckpoint = save;
        }
        else if (Checkpoint.HasValue)
        {
            var save = Checkpoint.Value;
            save.honor += delta;
            Checkpoint = save;
        }
    }
    public void RestartOnRoad()
    {
        if (!RoadCheckpoint.HasValue) { Restart(); return; }
        JinaItems.ClearEffects();
        var save = RoadCheckpoint.Value;
        PlayerHealth = MaxPlayerHealth;
        nextAttack = slashUntil = invulnerableUntil = hitFlashUntil = 0;
        playerImpactAt = -1;
        PlayerDownAt = PlayerHurtAt = WardBrokeAt = CoinsFoundAt = -1;
        comboStep = -1; comboUntil = 0; lastPlayerHit = -1;
        JinaFx.Clear();
        View.Shake(0, .01f); View.Focus(0, 0);
        playerArt.color = playerColor;
        player.RespawnAt(save.at);
        Arrows = save.arrows; noArrowsUntil = 0;
        ClearArrows();
        if (Quest != null) Quest.progress.RewindRoad(save.honor);
        if (Mobs != null) Mobs.ClearSoldiers();
        if (FinalBoss != null) FinalBoss.ResetBoss();
        if (playerVisual != null) playerVisual.ResetPose();
        if (Maps != null) Maps.Jump();
        RevivedOnRoadAt = Time.time;
    }
    public QuestSystem SideQuests { get; set; }
    // Esc 설정 메뉴(SettingsUI)가 열린 동안도 멈춤: 메뉴 위의 클릭이 뒤의 대화·상점을 열거나 공격하지 않는다.
    public bool Paused => SettingsPauseActive || (SideQuests != null && SideQuests.IsDialogOpen) || (Puzzles != null && Puzzles.IsOpen) || (KongBoss != null && KongBoss.Cutscene) || (FinalBoss != null && FinalBoss.Cutscene) || (Town != null && Town.IsOpen) || (Intro != null && Intro.IsPlaying) || (Ending != null && Ending.Active) || (Encounter != null && Encounter.Playing) || (BossTalk != null && BossTalk.IsOpen) || (Quest != null && Quest.IsDialogOpen) || (Tavern != null && Tavern.IsOpen) || (Peddler != null && Peddler.IsOpen) || (Ferry != null && Ferry.IsOpen) || (Kong != null && Kong.IsOpen) || (Stay != null && Stay.Playing) || (Lore != null && Lore.WispAsking) || InventoryPauseActive;
    public void SetPlayerHealthFromSave(int health)
    {
        PlayerHealth = Mathf.Clamp(health, 0, MaxPlayerHealth);
        if (PlayerHealth > 0) PlayerDownAt = -1;
        else PlayerDownAt = Time.time;
    }
    public void RestoreHealth(int amount)
    {
        if (PlayerHealth <= 0) return;
        int before = PlayerHealth;
        PlayerHealth = Mathf.Clamp(PlayerHealth + amount, 1, MaxHealth);
        if (PlayerHealth <= before) return;
        var chest = player.transform.position + Vector3.up * .3f;
        JinaFx.Rise(chest, 12, new Color(.62f, 1, .66f), .5f, 1.5f, .8f);
        JinaFx.Number(chest + Vector3.up * .7f, PlayerHealth - before, new Color(.62f, 1, .66f));
    }

    void Start()
    {
        if (player == null) { enabled = false; return; }
        playerArt = player.GetComponent<SpriteRenderer>();
        playerColor = playerArt.color;
        enemy = MakeSprite("Temporary Dokkaebi", new Vector2(1.6f, 2.4f), new Color(.8f,.2f,.25f));
        JinaActorVisual.Attach(enemy, JinaArt.Boss, this);
        slash = MakeSprite("Sword Range", new Vector2(1.35f, .9f), new Color(1f,.9f,.5f,.65f));
        slash.sortingOrder = 3;
        Quest = gameObject.AddComponent<ElderQuest>();
        Quest.combat = this;
        Camp = new GameObject("일반 몬스터와 봉인").AddComponent<Camp>();
        Camp.Initialize(this, playerArt.sprite);
        River = new GameObject("나루터 물귀신").AddComponent<Camp>();
        River.InitializeRiver(this, playerArt.sprite);
        Tavern = gameObject.AddComponent<TavernShop>();
        Tavern.combat = this;
        Peddler = gameObject.AddComponent<PeddlerShop>();
        Peddler.combat = this;
        Village = gameObject.AddComponent<VillageAlarm>();
        Village.combat = this;
        Remorse = gameObject.AddComponent<BossRemorse>();
        Remorse.combat = this;
        Encounter = gameObject.AddComponent<BossEncounter>();
        Encounter.combat = this;
        Chatter = gameObject.AddComponent<ShopChatter>();
        Chatter.combat = this;
        BossTalk = gameObject.AddComponent<BossTalk>();
        BossTalk.combat = this;
        playerVisual = SeonbiVisual.Attach(player, this);
        player.gameObject.AddComponent<DashSlashAttack>().Initialize(this);
        Maps = gameObject.AddComponent<Maps>();
        Maps.combat = this;
        Hud = gameObject.AddComponent<Hud>();
        Hud.combat = this;
        Puzzles = gameObject.AddComponent<SeonbiGridPuzzle>();
        Puzzles.combat = this;
        Puzzles.onMainPuzzleSolved.AddListener(() => { if (Quest != null) Quest.StelePuzzleSolved(); });
        Ferry = gameObject.AddComponent<Ferry>();
        Ferry.combat = this;
        Pass = gameObject.AddComponent<MountainPass>();
        Pass.combat = this;
        Kong = gameObject.AddComponent<KongVillage>();
        Kong.combat = this;
        Stay = gameObject.AddComponent<KongNightStay>();
        Stay.combat = this;
        Ambience = gameObject.AddComponent<MapAmbience>();
        Ambience.combat = this;
        Lore = gameObject.AddComponent<RoadsideLore>();
        Lore.combat = this;
        KongBoss = new GameObject("콩쥐 요괴").AddComponent<KongBoss>();
        KongBoss.combat = this;
        Aftermath = gameObject.AddComponent<KongAftermath>();
        Aftermath.combat = this;
        StationGhosts = new GameObject("역참 유령").AddComponent<Camp>();
        StationGhosts.InitializeRoad(this, playerArt.sprite, new[] { 305f, 318f, 329f }, WorldBuilder.FortEnd, WorldBuilder.StationEnd);
        GraveGhosts = new GameObject("무덤길 유령").AddComponent<Camp>();
        GraveGhosts.InitializeRoad(this, playerArt.sprite, new[] { 350f, 366f, 379f }, WorldBuilder.StationEnd, WorldBuilder.GraveEnd);
        Mobs = new GameObject("한양 가는 길 몬스터와 관군").AddComponent<RoadMobs>();
        Mobs.combat = this;
        Fort = gameObject.AddComponent<BeaconFort>();
        Fort.combat = this;
        Town = gameObject.AddComponent<HanyangTown>();
        Town.combat = this;
        FinalBoss = new GameObject("좌의정").AddComponent<FinalBoss>();
        FinalBoss.combat = this;
        Restart();
        Intro = gameObject.AddComponent<IntroNarration>();
        Intro.combat = this;
        Ending = gameObject.AddComponent<EndingNarration>();
        Ending.combat = this;
        GameOver = gameObject.AddComponent<GameOver>();
        GameOver.combat = this;
        // 스킬: Q·W·E 스킬과 왼쪽 아래 스킬창
        Skills = gameObject.AddComponent<Skills>();
        Skills.combat = this;
        // 팀원이 만든 배경 음악과 효과음
        Audio = gameObject.AddComponent<GameAudio>();
        Audio.combat = this;
        PlayerSave save = gameObject.GetComponent<PlayerSave>();
        if (save == null) save = gameObject.AddComponent<PlayerSave>();
        SettingsUI settings = gameObject.GetComponent<SettingsUI>();
        if (settings == null) settings = gameObject.AddComponent<SettingsUI>();
        settings.Initialize(this, save);
    }

    SpriteRenderer MakeSprite(string label, Vector2 size, Color color)
    {
        var obj = new GameObject(label);
        obj.transform.localScale = size;
        var art = obj.AddComponent<SpriteRenderer>();
        art.sprite = playerArt.sprite;
        art.color = color;
        return art;
    }

    public void Restart()
    {
        if (enemy == null) return;
        if (Puzzles != null) Puzzles.ResetPuzzles();
        JinaItems.ClearEffects();
        ResetLevel();
        PlayerHealth = MaxHealth;
        EnemyHealth = EnemyMaxHealth;
        enemy.transform.position = new Vector3(WorldBuilder.EnemyHome, .7f, 0);
        enemy.enabled = true;
        slash.enabled = false;
        nextAttack = slashUntil = invulnerableUntil = hitFlashUntil = 0;
        enemyReadyAt = Time.time + 1;
        windupUntil = -1;
        bossRecoveryUntil = 0;
        playerImpactAt = -1;
        BossFacing = -1;
        Move = BossMove.None; nextMoveAt = Time.time + MoveCooldown; moveCount = 0;
        BossDownAt = PurifiedAt = PlayerDownAt = PlayerHurtAt = WardBrokeAt = CoinsFoundAt = -1;
        Warded = false;
        // 스킬: 스킬 무적과 쿨타임 초기화
        skillInvulnerableUntil = 0;
        if (Skills != null) Skills.ResetSkills();
        comboStep = -1; comboUntil = 0; lastPlayerHit = -1;
        JinaFx.Clear();
        View.Shake(0, .01f);
        playerArt.color = playerColor;
        player.Respawn();
        ActiveWeaponSlot = 0;
        EquippedWeapons[0] = "dagger"; EquippedWeapons[1] = "bow";
        WeaponPickup.ClearAll();
        Arrows = ShopRules.MaxArrows;
        noArrowsUntil = 0;
        alerted = false;
        strikeElder = false;
        if (Camp != null) Camp.ResetCamp();
        if (River != null) River.ResetCamp();
        RiverFelled = 0; KeepsakeFreedAt = -1;
        ClearArrows();
        if (Quest != null) Quest.ResetQuest();
        if (Tavern != null) Tavern.ResetShop();
        if (Peddler != null) Peddler.ResetShop();
        if (Village != null) Village.ResetVillage();
        if (Ending != null) Ending.ResetEnding();
        if (Encounter != null) Encounter.ResetEncounter();
        if (Chatter != null) Chatter.ResetChatter();
        if (BossTalk != null) BossTalk.ResetTalk();
        if (playerVisual != null) playerVisual.ResetPose();
        if (Maps != null) Maps.ResetMaps();
        if (Ferry != null) Ferry.ResetFerry();
        if (Pass != null) Pass.ResetPass();
        if (Kong != null) Kong.ResetVillage();
        if (Stay != null) Stay.ResetStay();
        if (Lore != null) Lore.ResetLore();
        if (KongBoss != null) KongBoss.ResetBoss();
        if (Aftermath != null) Aftermath.ResetAftermath();
        if (StationGhosts != null) StationGhosts.ResetCamp();
        if (GraveGhosts != null) GraveGhosts.ResetCamp();
        if (Mobs != null) Mobs.ResetMobs();
        if (Fort != null) Fort.ResetFort();
        if (Town != null) Town.ResetTown();
        if (FinalBoss != null) FinalBoss.ResetBoss();
        Checkpoint = null; RevivedAt = -1;
        RoadCheckpoint = null; RevivedOnRoadAt = -1;
        Restarted?.Invoke();
    }

    void Update()
    {
        if (player == null || enemy == null) return;
        JinaItems.UpdateEffects(this);
        // The intro plays once per launch; R does not replay or interrupt it.
        if (Intro != null && Intro.IsPlaying) return;
        // R은 쓰러졌을 때(게임 오버)와 결말 뒤에만: 플레이 중에 눌러 처음부터 초기화되지 않는다.
        bool canRestart = PlayerHealth <= 0 || (Ending != null && Ending.Active);
        if (canRestart && Input.GetKeyDown(KeyCode.R)) { if (PlayerHealth <= 0 && CanRetryOnRoad) RestartOnRoad(); else if (PlayerHealth <= 0 && CanRetryAtHill) RestartAtHill(); else Restart(); }
        if (PlayerHealth <= 0) return;
        if (Paused)
        {
            pausedFrame = Time.frameCount;
            playerImpactAt = -1;
            bossRecoveryUntil = 0;
            windupUntil = -1;
            enemyReadyAt = Time.time + .5f;
            slash.enabled = false;
            if (Move != BossMove.None) EndMove(0);
            return;
        }
        // 무기 스왑: Q → S (Q는 스킬로 사용)
        if (Input.GetKeyDown(KeyCode.S) && EquippedWeapons[1 - ActiveWeaponSlot] != null) ActiveWeaponSlot = 1 - ActiveWeaponSlot;
        // 장착 조건을 못 채우게 된 무기(악행이 쌓인 사인검)는 손에서 빠져나감
        for (int i = 0; i < 2; i++)
        {
            if (EquippedWeapons[i] == null || CanEquip(Weapon.Get(EquippedWeapons[i]))) continue;
            JinaFx.Word(player.transform.position + Vector3.up * 1.5f, "악행이 깊어 " + Weapon.Get(EquippedWeapons[i]).name + "이 손을 떠났습니다", new Color(1f, .6f, .52f), 20);
            EquippedWeapons[i] = null;
            if (i == ActiveWeaponSlot && EquippedWeapons[1 - i] != null) ActiveWeaponSlot = 1 - i;
        }
        if (!EnemyDefeated && Input.GetKeyDown(KeyCode.F)
            && Vector2.Distance(player.transform.position, enemy.transform.position) < 2.2f
            && TryPurifyBoss())
        {
            // State and appearance are updated together by TryPurifyBoss.
        }

        if (AttackPressed && Time.time >= nextAttack && UsingBow && Arrows <= 0)
            noArrowsUntil = Time.time + 1.5f;
        else if (AttackPressed && Time.time >= nextAttack && !player.Dodging
            && player.UseStamina(CurrentStats.stamina))
        {
            var w = CurrentStats;
            pendingWeapon = w;
            // 콤보 무기(환도)만 3연격, 나머지는 늘 같은 동작
            pendingStep = w.combo && Time.time <= comboUntil ? (comboStep + 1) % 3 : 0;
            comboStep = w.combo ? pendingStep : -1;
            float swing = w.Swing(pendingStep);
            nextAttack = Time.time + swing;
            comboUntil = w.combo ? nextAttack + ComboWindow : 0;
            player.SlowUntil = Time.time + (w.ranged ? .3f : swing);
            // 마우스 조준: 커서 쪽으로 몸을 돌리고, 칼과 활 모두 커서 방향(360도)으로 나간다.
            //   몸은 가장 가까운 겨눈 그림(활 15도, 칼 30도마다)으로 그 쪽을 향한다.
            player.Face(MouseSide());
            attackFacing = player.Facing;
            pendingBow = w.ranged;
            pendingAngle = MouseAngle();
            playerImpactAt = Time.time + w.HitAt(pendingStep);
            if (playerVisual != null) playerVisual.Attack(w.ranged, attackFacing, pendingAngle, pendingStep);
            if (!w.ranged) GameAudio.Swing(w.id);   // 활 소리는 시위를 놓을 때(Shoot)
            if (w.bloodPrice > 0) PayBlood(w.bloodPrice);
        }
        if (playerImpactAt >= 0 && Time.time >= playerImpactAt)
        {
            playerImpactAt = -1;
            if (pendingBow) Shoot();
            else
            {
                slashUntil = Time.time + .13f;
                var w = pendingWeapon ?? Weapon.Get("hwando");
                bool heavy = w.combo && pendingStep == 2;
                float reach = heavy ? 1.75f : w.reach;
                var attackBounds = SwordBox(player.transform.position, attackFacing, pendingAngle, reach, heavy || w.heavy ? 1.2f : .9f);
                int damage = w.Damage(pendingStep);
                bool landed = false;
                if (!EnemyDefeated && attackBounds.Intersects(enemy.bounds)) { DamageEnemy(damage); landed = true; }
                if (KongBoss != null && KongBoss.Fighting && attackBounds.Intersects(KongBoss.HurtBox) && KongBoss.Damage(damage, true)) landed = true;
                if (KongBoss != null && KongBoss.BlowCrocks(attackBounds)) landed = true;
                foreach (var camp in Camps) if (camp != null && camp.Slash(attackBounds, damage)) landed = true;
                if (FinalBoss != null && FinalBoss.Fighting && attackBounds.Intersects(FinalBoss.HurtBox) && FinalBoss.Damage(damage)) landed = true;
                if (Mobs != null && Mobs.Slash(attackBounds, damage)) landed = true;
                SwordFeel(pendingStep, landed);
                // 전용 그림 근접 무기: 단검은 흰 칼빛, 명검은 칼 크기에 맞춘 검은 칼빛
                if (w.art != null && !w.hwandoSlash)
                {
                    var feet = player.transform.position; feet.y = playerArt.bounds.min.y;
                    if (w.combo)
                    {
                        // 1타 내려베기, 2타 올려베기, 3타(가장 큼) 수평으로 크게
                        float size = w.carryScale * (pendingStep == 2 ? 1.1f : .8f);
                        float tilt = pendingStep == 1 ? 58 : pendingStep == 2 ? 0 : -30;
                        JinaFx.Arc(Swung(feet + new Vector3(attackFacing * (.15f + size * .35f), .85f, 0)), attackFacing, tilt + pendingAngle, size, new Color(.08f, .05f, .1f));
                        // 명검 3타: 더 화려하게
                        if (pendingStep == 2)
                        {
                            var fxCenter = feet + new Vector3(attackFacing * (.15f + size * .35f), .85f, 0);
                            // 겹친 보랏빛 칼빛(더 크게) + 반대로 남는 잔상
                            JinaFx.Arc(Swung(fxCenter + new Vector3(attackFacing * .2f, .1f, 0)), attackFacing, 15 + pendingAngle, size * 1.2f, new Color(.45f, .15f, .6f), .28f);
                            JinaFx.Arc(Swung(fxCenter), attackFacing, -20 + pendingAngle, size * .9f, new Color(.25f, .05f, .3f), .22f);
                            // 검보라 기운이 튀고 피어오름, 칼끝 번쩍임, 발밑 흙먼지
                            JinaFx.Dots(Swung(fxCenter), 18, new Color(.3f, .1f, .4f), 4f, .6f);
                            JinaFx.Rise(Swung(fxCenter), 14, new Color(.55f, .25f, .75f), .5f, 2f, .8f);
                            JinaFx.Glint(Swung(fxCenter + Vector3.right * attackFacing * size * .4f), new Color(.8f, .5f, 1f));
                            JinaFx.Dust(feet + Vector3.right * attackFacing * .6f, attackFacing * 1.6f, 1.4f);
                            // 맞혔으면 화면을 더 세게 흔들고 잠깐 더 멈춤
                            if (landed) { View.Shake(.16f, .25f); JinaFx.Stop(.12f); }
                        }
                    }
                    else if (w.heavy)
                    {
                        // 도깨비 방망이: 위에서 크게 내려치는 굵은 나무빛(피 묻은 것은 핏빛) 반달, 땅을 울리는 흙먼지
                        bool bloody = w.bloodPrice > 0;
                        var center = feet + new Vector3(attackFacing * (.2f + w.carryScale * .32f), .8f, 0);
                        JinaFx.Arc(Swung(center), attackFacing, -45 + pendingAngle, w.carryScale * .85f,
                            bloody ? new Color(.55f, .06f, .08f) : new Color(.42f, .26f, .16f), .24f);
                        var ground = feet + Vector3.right * attackFacing * .9f;
                        JinaFx.Dust(ground, attackFacing * 1.2f, 1.3f);
                        if (landed)
                        {
                            JinaFx.Shock(ground, .9f);
                            View.Shake(.14f, .22f);
                            JinaFx.Stop(.1f);
                            if (bloody) JinaFx.Dots(Swung(center), 10, new Color(.6f, .05f, .08f), 3f, .5f);
                        }
                    }
                    else JinaFx.Arc(Swung(feet + new Vector3(attackFacing * .52f, .78f, 0)), attackFacing, -30 + pendingAngle, .48f, new Color(.95f, .97f, 1));
                }
            }
        }
        AdvanceArrows(Time.deltaTime);
        slash.enabled = Time.time < slashUntil;
        if (playerVisual != null) slash.enabled = false;
        slash.transform.position = player.transform.position + (Vector3)AimDirection(pendingAngle, attackFacing) * .95f;
        playerArt.color = Time.time < invulnerableUntil
            ? (Mathf.FloorToInt(Time.time * 14) % 2 == 0 ? Color.red : playerColor) : playerColor;
        if (EnemyDefeated) return;

        UpdateBoss();
    }

    // 공격 입력: J 또는 마우스 왼쪽 클릭. 클릭은 상호작용(JinaUI.Interact)과 같은 버튼이라, 이럴 때는 공격하지 않는다:
    //   상호작용 안내가 떠 있을 때(곁의 사람에게 말을 걸음), 창의 버튼 위를 눌렀을 때, 방금 대화·상점이 닫혔을 때(닫는 클릭).
    int pausedFrame = -10;
    bool AttackPressed => Input.GetKeyDown(KeyCode.J)
        || (Input.GetMouseButtonDown(0) && !JinaUI.Offering && !JinaUI.OverButton && Time.frameCount - pausedFrame > 1);

    // What each step of the sword looks like when it lands: where its crescent falls, how hard the world holds still.
    void SwordFeel(int step, bool landed)
    {
        var feet = player.transform.position; feet.y = playerArt.bounds.min.y;
        // 전용 그림 무기(명검)는 칼빛을 따로 그리므로 환도 칼빛은 건너뜀
        bool ownArc = pendingWeapon != null && pendingWeapon.art != null && !pendingWeapon.hwandoSlash;
        if (step == 1 && !ownArc) JinaFx.Arc(Swung(feet + new Vector3(attackFacing * .78f, .92f, 0)), attackFacing, 58 + pendingAngle, .9f, new Color(.92f, .96f, 1));
        if (step == 2)
        {
            if (!ownArc) JinaFx.Arc(Swung(feet + new Vector3(attackFacing * 1.25f, .9f, 0)), attackFacing, pendingAngle, 1.3f, new Color(1, .93f, .68f), .22f);
            JinaFx.Dust(feet + Vector3.right * -attackFacing * .4f, -attackFacing * 1.4f, 1.2f);
            JinaFx.Dust(feet, -attackFacing * .6f, .9f);
        }
        // 앞으로 내딛는 힘은 수평 성분만큼(위·아래로 벨수록 덜 나아감)
        float forward = Mathf.Cos(pendingAngle * Mathf.Deg2Rad);
        if (player.IsGrounded) player.Lunge(attackFacing, (step == 2 ? 5f : 1.6f) * forward, step == 2 ? .14f : .1f);
        if (!landed) return;
        JinaFx.Stop(step == 2 ? .09f : .045f);
        View.Shake(step == 2 ? .11f : .045f, step == 2 ? .2f : .1f);
    }

    // 마우스 조준 ──
    // 마우스 커서의 월드 좌표
    Vector2 MouseWorld()
    {
        var cam = Camera.main;
        if (cam == null) return playerArt.bounds.center;
        var p = Input.mousePosition;
        p.z = -cam.transform.position.z;
        return cam.ScreenToWorldPoint(p);
    }
    // 커서가 선비 오른쪽이면 1, 왼쪽이면 -1
    int MouseSide() => MouseWorld().x >= playerArt.bounds.center.x ? 1 : -1;
    // 커서 쪽을 바라볼 때의 조준 각도(도): 위가 +, −90~90. 커서가 선비 한가운데면 수평.
    float MouseAngle()
    {
        Vector2 d = MouseWorld() - (Vector2)playerArt.bounds.center;
        if (d.sqrMagnitude < .0001f) return 0;
        return Mathf.Atan2(d.y, Mathf.Abs(d.x)) * Mathf.Rad2Deg;
    }
    // 칼이 닿는 곳: 몸 가운데(pivot)에서 조준 방향으로 뻗은 reach×thickness 사각형을 감싸는 상자.
    //   수평(0도)이면 예전과 같은 앞쪽 상자이고, 비스듬하면 돌린 사각형의 테두리 상자다.
    public static Bounds SwordBox(Vector2 pivot, int facing, float angle, float reach, float thickness)
    {
        var dir = AimDirection(angle, facing);
        float c = Mathf.Abs(dir.x), s = Mathf.Abs(dir.y);
        return new Bounds(pivot + dir * (.28f + reach / 2), new Vector3(reach * c + thickness * s, reach * s + thickness * c, 1));
    }
    // 칼빛·불티 자리를 몸 가운데를 축으로 조준 각도만큼 돌린다(수평이면 그대로).
    Vector3 Swung(Vector3 at)
    {
        var pivot = player.transform.position; pivot.z = at.z;
        return pivot + Quaternion.Euler(0, 0, pendingAngle * attackFacing) * (at - pivot);
    }
    // 스킬 공격: area 안의 보스와 일반 몬스터에게 damage만큼 피해
    public void SkillHit(Bounds area, int damage)
    {
        if (!EnemyDefeated && area.Intersects(enemy.bounds)) DamageEnemy(damage);
        if (KongBoss != null && KongBoss.Fighting && area.Intersects(KongBoss.HurtBox)) KongBoss.Damage(damage, true);
        foreach (var camp in Camps) if (camp != null) camp.Slash(area, damage);
        if (FinalBoss != null && FinalBoss.Fighting && area.Intersects(FinalBoss.HurtBox)) FinalBoss.Damage(damage);
        if (Mobs != null) Mobs.Slash(area, damage);
    }

    public void HurtPlayer(int attack = MonsterAttack)
    {
        // 스킬 무적 중에는 맞지 않음
        if (PlayerHealth <= 0 || Time.time < invulnerableUntil || player.Dodging || SkillInvulnerable) return;
        var chest = player.transform.position + Vector3.up * .25f;
        if (Warded)
        {
            Warded = false; WardBrokeAt = Time.time; invulnerableUntil = Time.time + 1;
            JinaFx.Glint(chest, new Color(1, .86f, .4f));
            JinaFx.Dots(chest, 12, new Color(1, .8f, .36f), 3, .5f);
            return;
        }
        int before = PlayerHealth;
        PlayerHealth = Mathf.Max(0, PlayerHealth - CalcDamage(attack, EffectivePlayerDefense));
        invulnerableUntil = Time.time + 1;
        lastPlayerHit = Time.time;
        JinaFx.Hit(chest, -player.Facing, new Color(1, .42f, .34f), 1.1f);
        JinaFx.Number(chest + Vector3.up * .5f, before - PlayerHealth, new Color(1, .46f, .38f), 22);
        View.Shake(.09f, .2f);
        JinaFx.Stop(.06f);
        if (PlayerHealth > 0) { PlayerHurtAt = Time.time; return; }
        PlayerDownAt = Time.time;
        player.CanMove = false;
        player.Downed = true;
        playerImpactAt = -1;
        playerArt.color = Color.gray;
        slash.enabled = false;
        ClearArrows();
    }


    // 피 묻은 도깨비 방망이: 다섯 번(Weapon.bloodPrice) 휘두를 때마다 체력 1을 잃는다. 이것으로는 쓰러지지 않는다(1에서 멈춤).
    public int BloodSwings { get; private set; }
    void PayBlood(int every)
    {
        if (++BloodSwings % every != 0 || PlayerHealth <= 1) return;
        PlayerHealth -= 1;
        var chest = player.transform.position + Vector3.up * .25f;
        JinaFx.Number(chest + Vector3.up * .5f, 1, new Color(.8f, .1f, .14f), 18);
        JinaFx.Dots(chest, 6, new Color(.6f, .05f, .08f), 1.5f, .4f);
    }

    // 무기 그림 (한 번 불러오면 저장해 두고 다시 씀)
    readonly Dictionary<string, Sprite> weaponIcons = new Dictionary<string, Sprite>();
    public Sprite WeaponIcon(Weapon.Stats w)
    {
        if (!weaponIcons.TryGetValue(w.id, out var sprite))
        {
            // 활은 bow 그림, 그림이 따로 없는 환도는 hwando 그림, 단검과 명검은 각자 그림
            sprite = JinaArt.Load(w.ranged ? (w.bowArt ?? "04_Weapons/bow") : w.art ?? "04_Weapons/hwando");
            weaponIcons[w.id] = sprite;
        }
        return sprite;
    }

    // 아래 UI 전체 너비(화면 픽셀). 스킬창(Skills.cs)이 이 값으로 자리를 잡고, Hud의 체력·기력·무기·레벨이 그 둘레에 맞춘다.
    public const float HudWidth = 720;

    // 도깨비(보스) 체력: 위 가운데 두루마리(JinaUI.BossBar).
    void DrawBossHealthBar()
    {
        if (!alerted || EnemyDefeated || PlayerHealth <= 0 || Paused) return;
        JinaUI.BossBar("도깨비", (float)EnemyHealth / EnemyMaxHealth, new Color(.78f, .16f, .14f), Mathf.Max(0, EnemyHealth) + " / " + EnemyMaxHealth);
    }

    void OnGUI()
    {
        KoreanUI.Apply();
        DrawBossHealthBar();
        if (PlayerHealth <= 0) return;
        if (Time.time < noArrowsUntil) JinaUI.Toast("화살이 떨어졌습니다. 주막 옆 보부상에게서 사세요.");
        if (WardBrokeAt >= 0 && Time.time - WardBrokeAt < 1.6f) JinaUI.Toast("액막이 부적이 공격을 막고 재가 되었습니다.");
        if (KeepsakeFreedAt >= 0 && Time.time - KeepsakeFreedAt < 4f) JinaUI.Toast("흩어지는 물귀신 사이로 한 여인의 넋이 비쳤다가… 은비녀 하나가 남았습니다.");
        if (RevivedAt >= 0 && Time.time - RevivedAt < 3.5f) JinaUI.Toast("뒷산 입구에서 다시 일어났습니다. 콩쥐가 다시 뒷산에서 기다립니다.");
        if (RevivedOnRoadAt >= 0 && Time.time - RevivedOnRoadAt < 3.5f) JinaUI.Toast("한양 가는 길에서 다시 일어났습니다.");
        if (CoinsFoundAt >= 0 && Time.time - CoinsFoundAt < 1.6f) JinaUI.Toast(string.Format(CoinsNote, CoinsFound));
    }

    void OnDestroy()
    {
        if (enemy != null) Destroy(enemy.gameObject);
        if (slash != null) Destroy(slash.gameObject);
        if (Camp != null) Destroy(Camp.gameObject);
        if (River != null) Destroy(River.gameObject);
        if (KongBoss != null) Destroy(KongBoss.gameObject);
        if (StationGhosts != null) Destroy(StationGhosts.gameObject);
        if (GraveGhosts != null) Destroy(GraveGhosts.gameObject);
        if (Mobs != null) Destroy(Mobs.gameObject);
        if (FinalBoss != null) Destroy(FinalBoss.gameObject);
        ClearArrows();
        if (player != null) player.CanMove = true;
    }
}
