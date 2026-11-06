using UnityEngine;

// A small combat loop, attached automatically by the existing test scene: the seonbi's health and attacks,
// restarts and pausing, and the managers it attaches (quest, camp, shops, village, story scenes).
// The boss is in PrototypeCombat.Boss.cs and the arrows in PrototypeCombat.Arrows.cs.
public partial class PrototypeCombat : MonoBehaviour
{
    public PrototypePlayer player;
    public int PlayerHealth { get; private set; } = 5;
    public const int EnemyMaxHealth = 30;
    public const int SwordDamage = 2, ArrowDamage = 1;
    public int EnemyHealth { get; private set; } = EnemyMaxHealth;
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
    public bool Paused => (Intro != null && Intro.IsPlaying) || (Ending != null && Ending.Active) || (Encounter != null && Encounter.Playing) || (BossTalk != null && BossTalk.IsOpen) || (Quest != null && Quest.IsDialogOpen) || (Tavern != null && Tavern.IsOpen) || (Peddler != null && Peddler.IsOpen);
    public void RestoreHealth(int amount) { if (PlayerHealth > 0) PlayerHealth = Mathf.Clamp(PlayerHealth + amount, 1, 5); }

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
        Restart();
        Intro = gameObject.AddComponent<PrototypeIntro>();
        Intro.combat = this;
        Ending = gameObject.AddComponent<PrototypeEnding>();
        Ending.combat = this;
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
        PlayerHealth = 5;
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
        BossDownAt = PurifiedAt = PlayerDownAt = PlayerHurtAt = -1;
        playerArt.color = playerColor;
        player.Respawn();
        UsingBow = false;
        Arrows = ShopRules.MaxArrows;
        noArrowsUntil = 0;
        alerted = false;
        strikeElder = false;
        if (Camp != null) Camp.ResetCamp();
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

        if (Input.GetKeyDown(KeyCode.J) && Time.time >= nextAttack && UsingBow && Arrows <= 0)
            noArrowsUntil = Time.time + 1.5f;
        else if (Input.GetKeyDown(KeyCode.J) && Time.time >= nextAttack && !player.Dodging)
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
                if (Camp != null) Camp.Slash(attackBounds);
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

    public void HurtPlayer()
    {
        if (PlayerHealth <= 0 || Time.time < invulnerableUntil || player.Dodging) return;
        PlayerHealth--;
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
        GUI.Box(new Rect(15, 105, 440, 75), "선비 체력: " + PlayerHealth + " / 5     " + (BossPurified ? "도깨비: 정화됨 · 우호적" : "도깨비 체력: " + EnemyHealth + " / " + EnemyMaxHealth));
        GUI.Label(new Rect(30, 133, 410, 40), EnemyDefeated
            ? (Quest != null && Quest.progress.Outcome == EncounterOutcome.Purified
                ? "저주를 풀었습니다. 오른쪽 금빛 표식으로 향하세요."
                : "도깨비를 물리쳤습니다. 오른쪽 금빛 표식으로 향하세요.")
            : "주황빛은 공격의 전조입니다. K로 물러서거나 뛰어 피하세요.");
        if (Time.time < noArrowsUntil && PlayerHealth > 0)
            GUI.Box(new Rect(Screen.width / 2f - 220, 280, 440, 35), "화살이 떨어졌습니다. 주막 옆 보부상에게서 사세요.");
        if (PlayerHealth <= 0)
            GUI.Box(new Rect(Screen.width / 2f - 130, 280, 260, 60), "선비가 쓰러졌습니다.\nR: 처음부터 다시 시작");
    }

    void OnDestroy()
    {
        if (enemy != null) Destroy(enemy.gameObject);
        if (slash != null) Destroy(slash.gameObject);
        if (Camp != null) Destroy(Camp.gameObject);
        ClearArrows();
        if (player != null) player.CanMove = true;
    }
}
