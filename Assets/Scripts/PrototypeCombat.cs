using UnityEngine;

// A small combat loop, attached automatically by the existing test scene: the seonbi's health and attacks,
// restarts and pausing, and the managers it attaches (quest, camp, shops, village, story scenes).
// The boss is in PrototypeCombat.Boss.cs and the arrows in PrototypeCombat.Arrows.cs.
public partial class PrototypeCombat : MonoBehaviour
{
    public bool InventoryPauseActive { get; set; }
    public event System.Action Restarted;
    public PrototypePlayer player;
    public const int MaxPlayerHealth = 100;
    public int PlayerHealth { get; private set; } = MaxPlayerHealth;
    public const int PlayerAttack = 10;
    public int PlayerDefense = 0;
    // A ward charm (JinaItems) takes the next blow in his place; the message shows for a moment after.
    public bool Warded { get; private set; }
    public float WardBrokeAt { get; private set; } = -1;
    public void GiveWard() { Warded = true; }
    // Coins found on fallen ghosts, shown for a moment.
    public float CoinsFoundAt { get; private set; } = -1;
    public int CoinsFound { get; private set; }
    public void FindCoins(int amount) { if (Tavern == null) return; Tavern.wallet.Earn(amount); CoinsFound = amount; CoinsFoundAt = Time.time; }
    // ── 보스(도깨비) 능력치 ──
    public const int EnemyMaxHealth = 200;
    public const int BossAttack = 20;
    // The elder's last blow takes a third of the boss's health.
    public const int ElderCounterDamage = 67;
    public int BossDefense = 0;
    // ── 일반 몬스터 능력치 (PrototypeCamp.cs에서 사용할 예정) ──
    public const int MonsterMaxHealth = 30;
    public const int MonsterAttack = 10;
    public const int MonsterDefense = 0;
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
    private SeonbiVisual playerVisual;
    public PrototypeQuest Quest { get; private set; }
    public bool UsingBow { get; private set; }
    private bool alerted;
    private bool strikeElder;
    public PrototypeCamp Camp { get; private set; }
    // The river's water ghosts at the ferry. Felling KeepsakeAt of them (they rise again) frees the drowned
    // mother's keepsake, which goes into the inventory once a run.
    public PrototypeCamp River { get; private set; }
    public const int KeepsakeAt = 10;
    public int RiverFelled { get; private set; }
    public float KeepsakeFreedAt { get; private set; } = -1;
    public PrototypeCamp[] Camps => new[] { Camp, River };
    public void FellWaterGhost()
    {
        if (++RiverFelled != KeepsakeAt) return;
        KeepsakeFreedAt = Time.time;
        var bag = JinaItems.Inventory;
        if (bag != null) bag.TryAdd(JinaItems.Keepsake);
    }
    public PrototypeTavern Tavern { get; private set; }
    public PrototypePeddler Peddler { get; private set; }
    public PrototypeVillage Village { get; private set; }
    public BossRemorse Remorse { get; private set; }
    public PrototypeEncounter Encounter { get; private set; }
    public PrototypeChatter Chatter { get; private set; }
    public PrototypeBossTalk BossTalk { get; private set; }
    public int Arrows { get; private set; } = ShopRules.MaxArrows;
    private float noArrowsUntil;
    public void AddArrows(int amount) { Arrows = Mathf.Clamp(Arrows + amount, 0, ShopRules.MaxArrows); }
    public PrototypeIntro Intro { get; private set; }
    public PrototypeEnding Ending { get; private set; }
    public PrototypeGameOver GameOver { get; private set; }
    public PrototypeMaps Maps { get; private set; }
    public PrototypeFerry Ferry { get; private set; }
    public PrototypeHud Hud { get; private set; }
    public PrototypePass Pass { get; private set; }
    public PrototypeKongVillage Kong { get; private set; }
    public PrototypeKongStay Stay { get; private set; }
    public bool Paused => (Intro != null && Intro.IsPlaying) || (Ending != null && Ending.Active) || (Encounter != null && Encounter.Playing) || (BossTalk != null && BossTalk.IsOpen) || (Quest != null && Quest.IsDialogOpen) || (Tavern != null && Tavern.IsOpen) || (Peddler != null && Peddler.IsOpen) || (Ferry != null && Ferry.IsOpen) || (Kong != null && Kong.IsOpen) || (Stay != null && Stay.Playing) || InventoryPauseActive;
    public void RestoreHealth(int amount) { if (PlayerHealth > 0) PlayerHealth = Mathf.Clamp(PlayerHealth + amount, 1, MaxPlayerHealth); }

    void Start()
    {
        if (player == null) { enabled = false; return; }
        playerArt = player.GetComponent<SpriteRenderer>();
        playerColor = playerArt.color;
        enemy = MakeSprite("Temporary Dokkaebi", new Vector2(1.6f, 2.4f), new Color(.8f,.2f,.25f));
        JinaActorVisual.Attach(enemy, JinaArt.Boss, this);
        slash = MakeSprite("Sword Range", new Vector2(1.35f, .9f), new Color(1f,.9f,.5f,.65f));
        slash.sortingOrder = 3;
        Quest = gameObject.AddComponent<PrototypeQuest>();
        Quest.combat = this;
        Camp = new GameObject("일반 몬스터와 봉인").AddComponent<PrototypeCamp>();
        Camp.Initialize(this, playerArt.sprite);
        River = new GameObject("나루터 물귀신").AddComponent<PrototypeCamp>();
        River.InitializeRiver(this, playerArt.sprite);
        Tavern = gameObject.AddComponent<PrototypeTavern>();
        Tavern.combat = this;
        Peddler = gameObject.AddComponent<PrototypePeddler>();
        Peddler.combat = this;
        Village = gameObject.AddComponent<PrototypeVillage>();
        Village.combat = this;
        Remorse = gameObject.AddComponent<BossRemorse>();
        Remorse.combat = this;
        Encounter = gameObject.AddComponent<PrototypeEncounter>();
        Encounter.combat = this;
        Chatter = gameObject.AddComponent<PrototypeChatter>();
        Chatter.combat = this;
        BossTalk = gameObject.AddComponent<PrototypeBossTalk>();
        BossTalk.combat = this;
        playerVisual = SeonbiVisual.Attach(player, this);
        Maps = gameObject.AddComponent<PrototypeMaps>();
        Maps.combat = this;
        Hud = gameObject.AddComponent<PrototypeHud>();
        Hud.combat = this;
        Ferry = gameObject.AddComponent<PrototypeFerry>();
        Ferry.combat = this;
        Pass = gameObject.AddComponent<PrototypePass>();
        Pass.combat = this;
        Kong = gameObject.AddComponent<PrototypeKongVillage>();
        Kong.combat = this;
        Stay = gameObject.AddComponent<PrototypeKongStay>();
        Stay.combat = this;
        Restart();
        Intro = gameObject.AddComponent<PrototypeIntro>();
        Intro.combat = this;
        Ending = gameObject.AddComponent<PrototypeEnding>();
        Ending.combat = this;
        GameOver = gameObject.AddComponent<PrototypeGameOver>();
        GameOver.combat = this;
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
        PlayerHealth = MaxPlayerHealth;
        EnemyHealth = EnemyMaxHealth;
        enemy.transform.position = new Vector3(PrototypeWorld.EnemyHome, .7f, 0);
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
        playerArt.color = playerColor;
        player.Respawn();
        UsingBow = false;
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
        Restarted?.Invoke();
    }

    void Update()
    {
        if (player == null || enemy == null) return;
        // The intro plays once per launch; R does not replay or interrupt it.
        if (Intro != null && Intro.IsPlaying) return;
        if (Input.GetKeyDown(KeyCode.R)) Restart();
        if (PlayerHealth <= 0) return;
        if (Paused)
        {
            playerImpactAt = -1;
            bossRecoveryUntil = 0;
            windupUntil = -1;
            enemyReadyAt = Time.time + .5f;
            slash.enabled = false;
            if (Move != BossMove.None) EndMove(0);
            return;
        }
        if (Input.GetKeyDown(KeyCode.Q)) UsingBow = !UsingBow;
        if (!EnemyDefeated && Input.GetKeyDown(KeyCode.F)
            && Vector2.Distance(player.transform.position, enemy.transform.position) < 2.2f
            && TryPurifyBoss())
        {
            // State and appearance are updated together by TryPurifyBoss.
        }

        if ((Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)) && Time.time >= nextAttack && UsingBow && Arrows <= 0)
            noArrowsUntil = Time.time + 1.5f;
        else if ((Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)) && Time.time >= nextAttack && !player.Dodging)
        {
            nextAttack = Time.time + (UsingBow ? .65f : .4f);
            attackFacing = player.Facing;
            pendingBow = UsingBow;
            pendingAim = !UsingBow ? 0 : Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1
                : Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
            playerImpactAt = Time.time + (pendingBow ? SeonbiVisual.BowReleaseDelay : SeonbiVisual.HitDelay);
            if (playerVisual != null) playerVisual.Attack(UsingBow, attackFacing, pendingAim);
        }
        if (playerImpactAt >= 0 && Time.time >= playerImpactAt)
        {
            playerImpactAt = -1;
            if (pendingBow) Shoot();
            else
            {
                slashUntil = Time.time + .13f;
                Vector2 center = (Vector2)player.transform.position + Vector2.right * attackFacing * .95f;
                var attackBounds = new Bounds(center, new Vector3(1.35f, .9f, 1));
                if (!EnemyDefeated && attackBounds.Intersects(enemy.bounds)) DamageEnemy(SwordDamage);
                foreach (var camp in Camps) if (camp != null) camp.Slash(attackBounds);
            }
        }
        AdvanceArrows(Time.deltaTime);
        slash.enabled = Time.time < slashUntil;
        if (playerVisual != null) slash.enabled = false;
        slash.transform.position = player.transform.position + Vector3.right * attackFacing * .95f;
        playerArt.color = Time.time < invulnerableUntil
            ? (Mathf.FloorToInt(Time.time * 14) % 2 == 0 ? Color.red : playerColor) : playerColor;
        if (EnemyDefeated) return;

        UpdateBoss();
    }

    public void HurtPlayer(int attack = MonsterAttack)
    {
        if (PlayerHealth <= 0 || Time.time < invulnerableUntil || player.Dodging) return;
        if (Warded) { Warded = false; WardBrokeAt = Time.time; invulnerableUntil = Time.time + 1; return; }
        PlayerHealth = Mathf.Max(0, PlayerHealth - CalcDamage(attack, PlayerDefense));
        invulnerableUntil = Time.time + 1;
        if (PlayerHealth > 0) { PlayerHurtAt = Time.time; return; }
        PlayerDownAt = Time.time;
        player.CanMove = false;
        player.Downed = true;
        playerImpactAt = -1;
        playerArt.color = Color.gray;
        slash.enabled = false;
        ClearArrows();
    }

    void OnGUI()
    {
        PrototypeKoreanUI.Apply();
        if (PlayerHealth <= 0) return;
        if (Time.time < noArrowsUntil) JinaUI.Toast("화살이 떨어졌습니다. 주막 옆 보부상에게서 사세요.");
        if (WardBrokeAt >= 0 && Time.time - WardBrokeAt < 1.6f) JinaUI.Toast("액막이 부적이 공격을 막고 재가 되었습니다.");
        if (KeepsakeFreedAt >= 0 && Time.time - KeepsakeFreedAt < 4f) JinaUI.Toast("흩어지는 물귀신 사이로 한 여인의 넋이 비쳤다가… 은비녀 하나가 남았습니다.");
        if (CoinsFoundAt >= 0 && Time.time - CoinsFoundAt < 1.6f) JinaUI.Toast("쓰러진 망령이 엽전 " + CoinsFound + "문을 떨어뜨렸습니다.");
    }

    void OnDestroy()
    {
        if (enemy != null) Destroy(enemy.gameObject);
        if (slash != null) Destroy(slash.gameObject);
        if (Camp != null) Destroy(Camp.gameObject);
        if (River != null) Destroy(River.gameObject);
        ClearArrows();
        if (player != null) player.CanMove = true;
    }
}
