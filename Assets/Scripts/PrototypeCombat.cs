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
    // The sword's three steps: the first cut, a rising return, then a heavy finishing blow; each must follow in time.
    private int comboStep = -1, pendingStep;
    private float comboUntil;
    public const float ComboWindow = .3f;
    public int ComboStep => comboStep;
    private float lastPlayerHit = -1;
    private SeonbiVisual playerVisual;
    public ElderQuest Quest { get; private set; }
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
    public IntroNarration Intro { get; private set; }
    public EndingNarration Ending { get; private set; }
    public GameOver GameOver { get; private set; }
    public PrototypeMaps Maps { get; private set; }
    public Ferry Ferry { get; private set; }
    public PrototypeHud Hud { get; private set; }
    public MountainPass Pass { get; private set; }
    public KongVillage Kong { get; private set; }
    public KongNightStay Stay { get; private set; }
    public MapAmbience Ambience { get; private set; }
    public RoadsideLore Lore { get; private set; }
    // The Kongjwi demon on the back hill, and the scenes after her fight.
    public KongBoss KongBoss { get; private set; }
    public KongAftermath Aftermath { get; private set; }
    public DevCheats Cheats { get; private set; }
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
        var save = Checkpoint.Value;
        PlayerHealth = MaxPlayerHealth;
        nextAttack = slashUntil = invulnerableUntil = hitFlashUntil = 0;
        playerImpactAt = -1;
        PlayerDownAt = PlayerHurtAt = WardBrokeAt = CoinsFoundAt = -1;
        comboStep = -1; comboUntil = 0; lastPlayerHit = -1;
        JinaFx.Clear();
        PrototypeView.Shake(0, .01f); PrototypeView.Focus(0, 0);
        playerArt.color = playerColor;
        player.RespawnAt(HillEntrance);
        UsingBow = false; Arrows = save.arrows; noArrowsUntil = 0;
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
    public bool Paused => (KongBoss != null && KongBoss.Cutscene) || (Intro != null && Intro.IsPlaying) || (Ending != null && Ending.Active) || (Encounter != null && Encounter.Playing) || (BossTalk != null && BossTalk.IsOpen) || (Quest != null && Quest.IsDialogOpen) || (Tavern != null && Tavern.IsOpen) || (Peddler != null && Peddler.IsOpen) || (Ferry != null && Ferry.IsOpen) || (Kong != null && Kong.IsOpen) || (Stay != null && Stay.Playing) || InventoryPauseActive;
    public void RestoreHealth(int amount)
    {
        if (PlayerHealth <= 0) return;
        int before = PlayerHealth;
        PlayerHealth = Mathf.Clamp(PlayerHealth + amount, 1, MaxPlayerHealth);
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
        Camp = new GameObject("일반 몬스터와 봉인").AddComponent<PrototypeCamp>();
        Camp.Initialize(this, playerArt.sprite);
        River = new GameObject("나루터 물귀신").AddComponent<PrototypeCamp>();
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
        Maps = gameObject.AddComponent<PrototypeMaps>();
        Maps.combat = this;
        Hud = gameObject.AddComponent<PrototypeHud>();
        Hud.combat = this;
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
        Cheats = gameObject.AddComponent<DevCheats>();
        Cheats.combat = this;
        Restart();
        Intro = gameObject.AddComponent<IntroNarration>();
        Intro.combat = this;
        Ending = gameObject.AddComponent<EndingNarration>();
        Ending.combat = this;
        GameOver = gameObject.AddComponent<GameOver>();
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
        comboStep = -1; comboUntil = 0; lastPlayerHit = -1;
        JinaFx.Clear();
        PrototypeView.Shake(0, .01f);
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
        if (Lore != null) Lore.ResetLore();
        if (KongBoss != null) KongBoss.ResetBoss();
        if (Aftermath != null) Aftermath.ResetAftermath();
        Checkpoint = null; RevivedAt = -1;
        Restarted?.Invoke();
    }

    void Update()
    {
        if (player == null || enemy == null) return;
        // The intro plays once per launch; R does not replay or interrupt it.
        if (Intro != null && Intro.IsPlaying) return;
        if (Input.GetKeyDown(KeyCode.R)) { if (PlayerHealth <= 0 && CanRetryAtHill) RestartAtHill(); else Restart(); }
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
            // Another swing soon enough after the last goes on to the next step; otherwise it starts over.
            pendingStep = UsingBow ? 0 : Time.time <= comboUntil ? (comboStep + 1) % 3 : 0;
            comboStep = UsingBow ? -1 : pendingStep;
            float swing = SeonbiVisual.SwordLength(pendingStep);
            nextAttack = Time.time + (UsingBow ? .65f : swing);
            comboUntil = UsingBow ? 0 : nextAttack + ComboWindow;
            player.SlowUntil = Time.time + (UsingBow ? .3f : swing);
            attackFacing = player.Facing;
            pendingBow = UsingBow;
            pendingAim = !UsingBow ? 0 : Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1
                : Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
            playerImpactAt = Time.time + (pendingBow ? SeonbiVisual.BowReleaseDelay : SeonbiVisual.SwordHit(pendingStep));
            if (playerVisual != null) playerVisual.Attack(UsingBow, attackFacing, pendingAim, pendingStep);
        }
        if (playerImpactAt >= 0 && Time.time >= playerImpactAt)
        {
            playerImpactAt = -1;
            if (pendingBow) Shoot();
            else
            {
                slashUntil = Time.time + .13f;
                bool heavy = pendingStep == 2;
                Vector2 center = (Vector2)player.transform.position + Vector2.right * attackFacing * (heavy ? 1.1f : .95f);
                var attackBounds = new Bounds(center, heavy ? new Vector3(1.75f, 1.15f, 1) : new Vector3(1.35f, .9f, 1));
                bool landed = false;
                if (!EnemyDefeated && attackBounds.Intersects(enemy.bounds)) { DamageEnemy(SwordDamage); landed = true; }
                if (KongBoss != null && KongBoss.Fighting && attackBounds.Intersects(KongBoss.HurtBox) && KongBoss.Damage(SwordDamage, true)) landed = true;
                if (KongBoss != null && KongBoss.BlowCrocks(attackBounds)) landed = true;
                foreach (var camp in Camps) if (camp != null && camp.Slash(attackBounds)) landed = true;
                SwordFeel(pendingStep, landed);
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

    // What each step of the sword looks like when it lands: where its crescent falls, how hard the world holds still.
    void SwordFeel(int step, bool landed)
    {
        var feet = player.transform.position; feet.y = playerArt.bounds.min.y;
        if (step == 1) JinaFx.Arc(feet + new Vector3(attackFacing * .78f, .92f, 0), attackFacing, 58, .9f, new Color(.92f, .96f, 1));
        if (step == 2)
        {
            JinaFx.Arc(feet + new Vector3(attackFacing * 1.25f, .9f, 0), attackFacing, 0, 1.3f, new Color(1, .93f, .68f), .22f);
            JinaFx.Dust(feet + Vector3.right * -attackFacing * .4f, -attackFacing * 1.4f, 1.2f);
            JinaFx.Dust(feet, -attackFacing * .6f, .9f);
        }
        if (player.IsGrounded) player.Lunge(attackFacing, step == 2 ? 5f : 1.6f, step == 2 ? .14f : .1f);
        if (!landed) return;
        JinaFx.Stop(step == 2 ? .09f : .045f);
        PrototypeView.Shake(step == 2 ? .11f : .045f, step == 2 ? .2f : .1f);
    }

    public void HurtPlayer(int attack = MonsterAttack)
    {
        if (PlayerHealth <= 0 || Time.time < invulnerableUntil || player.Dodging) return;
        var chest = player.transform.position + Vector3.up * .25f;
        if (Warded)
        {
            Warded = false; WardBrokeAt = Time.time; invulnerableUntil = Time.time + 1;
            JinaFx.Glint(chest, new Color(1, .86f, .4f));
            JinaFx.Dots(chest, 12, new Color(1, .8f, .36f), 3, .5f);
            return;
        }
        int before = PlayerHealth;
        PlayerHealth = Mathf.Max(0, PlayerHealth - CalcDamage(attack, PlayerDefense));
        invulnerableUntil = Time.time + 1;
        lastPlayerHit = Time.time;
        JinaFx.Hit(chest, -player.Facing, new Color(1, .42f, .34f), 1.1f);
        JinaFx.Number(chest + Vector3.up * .5f, before - PlayerHealth, new Color(1, .46f, .38f), 22);
        PrototypeView.Shake(.09f, .2f);
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

    // 선비 머리 위 체력바
    void DrawPlayerHealthBar()
    {
        if (PlayerHealth <= 0 || Paused || Camera.main == null) return;
        // 머리 위 지점(월드 좌표)을 화면 좌표로 바꿈
        Vector3 head = new Vector3(player.transform.position.x, playerArt.bounds.max.y + .8f, 0);
        Vector3 screen = Camera.main.WorldToScreenPoint(head);
        if (screen.z < 0) return;
        float width = 60, height = 8;
        float x = screen.x - width / 2;
        float y = Screen.height - screen.y - height;   // GUI는 y축이 위에서 아래로
        float ratio = (float)PlayerHealth / MaxPlayerHealth;

        var oldMatrix = GUI.matrix;
        var oldColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;
        // 검은 테두리
        GUI.color = new Color(0, 0, 0, .7f);
        GUI.DrawTexture(new Rect(x - 1, y - 1, width + 2, height + 2), Texture2D.whiteTexture);
        // 체력 비율만큼 채우기: 50% 초과 초록, 25% 초과 노랑, 그 이하 빨강
        GUI.color = ratio > .5f ? new Color(.3f, .85f, .3f) : ratio > .25f ? new Color(.95f, .75f, .2f) : new Color(.9f, .2f, .2f);
        GUI.DrawTexture(new Rect(x, y, width * ratio, height), Texture2D.whiteTexture);
        var numStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = 12, fontStyle = FontStyle.Bold };
        var numRect = new Rect(x - 20, y - 18, width + 40, 16);
        string numText = PlayerHealth + " / " + MaxPlayerHealth;
        GUI.color = Color.black;
        GUI.Label(new Rect(numRect.x + 1, numRect.y + 1, numRect.width, numRect.height), numText, numStyle);
        GUI.color = Color.white;
        GUI.Label(numRect, numText, numStyle);
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }

        // 화면 상단 중앙 보스 체력바 (보스와 마주친 뒤부터 표시, 쓰러뜨리거나 정화하면 숨김)
    void DrawBossHealthBar()
    {
        if (!alerted || EnemyDefeated || PlayerHealth <= 0 || Paused) return;
        var oldMatrix = GUI.matrix;
        var oldColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;

        float width = Screen.width * .4f, height = 18;
        float x = (Screen.width - width) / 2;
        float y = 32;
        float ratio = (float)EnemyHealth / EnemyMaxHealth;

        // 검은 테두리
        GUI.color = new Color(0, 0, 0, .75f);
        GUI.DrawTexture(new Rect(x - 2, y - 2, width + 4, height + 4), Texture2D.whiteTexture);
        // 빈 칸 (어두운 빨강)
        GUI.color = new Color(.3f, .08f, .08f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        // 남은 체력 (밝은 빨강)
        GUI.color = new Color(.85f, .15f, .2f);
        GUI.DrawTexture(new Rect(x, y, width * ratio, height), Texture2D.whiteTexture);

        // 막대 위에 이름과 숫자
        GUI.color = Color.white;
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(x, y - 26, width, 24), "도깨비   " + EnemyHealth + " / " + EnemyMaxHealth, style);

        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }

    void OnGUI()
    {
        PrototypeKoreanUI.Apply();
        DrawPlayerHealthBar();
        DrawBossHealthBar();
        if (PlayerHealth <= 0) return;
        if (Time.time < noArrowsUntil) JinaUI.Toast("화살이 떨어졌습니다. 주막 옆 보부상에게서 사세요.");
        if (WardBrokeAt >= 0 && Time.time - WardBrokeAt < 1.6f) JinaUI.Toast("액막이 부적이 공격을 막고 재가 되었습니다.");
        if (KeepsakeFreedAt >= 0 && Time.time - KeepsakeFreedAt < 4f) JinaUI.Toast("흩어지는 물귀신 사이로 한 여인의 넋이 비쳤다가… 은비녀 하나가 남았습니다.");
        if (RevivedAt >= 0 && Time.time - RevivedAt < 3.5f) JinaUI.Toast("뒷산 입구에서 다시 일어났습니다. 콩쥐가 다시 뒷산에서 기다립니다.");
        if (CoinsFoundAt >= 0 && Time.time - CoinsFoundAt < 1.6f) JinaUI.Toast("쓰러진 망령이 엽전 " + CoinsFound + "문을 떨어뜨렸습니다.");
    }

    void OnDestroy()
    {
        if (enemy != null) Destroy(enemy.gameObject);
        if (slash != null) Destroy(slash.gameObject);
        if (Camp != null) Destroy(Camp.gameObject);
        if (River != null) Destroy(River.gameObject);
        if (KongBoss != null) Destroy(KongBoss.gameObject);
        ClearArrows();
        if (player != null) player.CanMove = true;
    }
}
