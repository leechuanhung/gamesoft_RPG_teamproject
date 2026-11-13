using UnityEngine;

public class PrototypePlayer : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float runSpeed = 10f;
    public KeyCode runKey = KeyCode.LeftShift;
    public float jumpSpeed = 11f;
    // Backstep (K): a short hop away from where he faces, untouchable while it lasts.
    public const float DodgeLength = .3f, DodgeSpeed = 9f, DodgeCooldown = .7f;
    private float dodgeUntil;
    public bool Dodging => Time.time < dodgeUntil;
    public float DashRemaining => Mathf.Max(0f, dodgeUntil - Time.time);
    // A swing: he steps forward with the blow, and his feet are slowed while the sword is moving.
    private float lungeUntil, lungeSpeed;
    public float SlowUntil { get; set; }
    public const float SwingSlow = .4f;
    public void Lunge(int direction, float speed, float length) { lungeSpeed = direction * speed; lungeUntil = Time.time + length; }
    private int dodgeDir = 1;
    private bool backstepDodge;
    // Distinguishes double-tap forward dash from K backstep (both use Dodging).
    public bool DashSlashing => Dodging && !backstepDodge;
    // 스킬(Q·W) 이동인지: 그림은 대시 베기 모션 그대로, 피해는 PrototypeSkills가 따로 줌
    private bool skillMove;
    public bool DashSlashDamage => DashSlashing && !skillMove;
    private float dashSpeed = DodgeSpeed;   // 대시 속도 (스킬마다 다르게 쓰기 위해)
    // Light straw sandals (JinaItems) quicken his walk and run until then.
    public const float HasteBoost = 1.35f;
    public float HasteUntil { get; set; }
    public bool Hasted => Time.time < HasteUntil;
    // ── 스테미너 ──
    public const int MaxStamina = 100;
    public const int StaminaPerSecond = 5;    // 초당 회복량 (0.2초마다 1씩)
    public int Stamina { get; private set; } = MaxStamina;
    private float staminaTimer;
    // 스테미너가 충분하면 amount만큼 깎고 true, 모자라면 깎지 않고 false
    public bool UseStamina(int amount)
    {
        if (Stamina < amount) return false;
        Stamina -= amount;
        return true;
    }
    // 달리기 속도 배율 (걷기 속도의 60%)
    public const float RunSpeedRate = 1.6f;
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private float horizontal;
    private float jumpRequestedUntil;
    private Vector3 spawn;
    public int Facing { get; private set; } = 1;
    public bool CanMove { get; set; } = true;
    // Struck down: no control, but gravity still brings him to the ground.
    public bool Downed { get; set; }
    public bool IsGrounded { get; private set; }
    public bool IsRunning { get; private set; }
    public float HorizontalSpeed => body == null ? 0 : body.velocity.x;
    public float VerticalSpeed => body == null ? 0 : body.velocity.y;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>();
        spawn = transform.position;
    }

    void Update()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        if (!CanMove) { horizontal = 0; IsRunning = false; return; }
        if (!CanMove) { horizontal = 0; IsRunning = false; return; }
        // 0.2초마다 1씩 회복 (초당 5)
        if (Stamina < MaxStamina)
        {
            staminaTimer += Time.deltaTime;
            float step = 1f / StaminaPerSecond;
            while (staminaTimer >= step && Stamina < MaxStamina) { staminaTimer -= step; Stamina++; }
        }
        else staminaTimer = 0;
        if (Dodging) { IsRunning = false; return; }
        if (Dodging) { IsRunning = false; return; }
        if (horizontal != 0) Facing = horizontal > 0 ? 1 : -1;
        IsRunning = horizontal != 0 && (Input.GetKey(runKey) || Input.GetKey(KeyCode.RightShift));
        if (Input.GetKeyDown(KeyCode.Space)) jumpRequestedUntil = Time.time + 0.12f;
        if (Input.GetKeyDown(KeyCode.R) || transform.position.y < -8f)
        {
            var combat = FindObjectOfType<PrototypeCombat>();
            if (combat != null) combat.Restart();
            else Respawn();
        }
    }

    void FixedUpdate()
    {
        if (!CanMove) { body.velocity = Downed ? new Vector2(0, body.velocity.y) : Vector2.zero; return; }
        var filter = new ContactFilter2D();
        filter.SetLayerMask(1 << 8);
        filter.useTriggers = false;
        int count = hitbox.Cast(Vector2.down, filter, groundHits, 0.08f);
        bool grounded = false;
        float carry = 0;   // standing on the ferry boat, he moves with it
        for (int i = 0; i < count; i++)
            if (groundHits[i].normal.y > 0.65f)
            {
                grounded = true;
                var boat = groundHits[i].collider.GetComponent<FerryBoat>();
                if (boat != null) carry = boat.Velocity.x;
            }

        IsGrounded = grounded && body.velocity.y <= 0.1f;
        float walk = horizontal * (IsRunning ? moveSpeed * RunSpeedRate : moveSpeed) * (Hasted ? HasteBoost : 1) * (grounded && Time.time < SlowUntil ? SwingSlow : 1);
        if (Time.time < lungeUntil && grounded) walk += lungeSpeed;
        body.velocity = new Vector2(Dodging ? dodgeDir * dashSpeed : walk + carry, body.velocity.y);
        if (jumpRequestedUntil > Time.time && grounded && body.velocity.y <= 0.1f)
        {
            body.velocity = new Vector2(body.velocity.x, jumpSpeed);
            IsGrounded = false;
            jumpRequestedUntil = 0f;
        }
    }

    // 공격할 때 마우스 쪽으로 몸 돌리기
    public void Face(int dir) { if (dir != 0) Facing = dir > 0 ? 1 : -1; }

    // 스킬용 이동: dir 방향으로 length초 동안 speed 속도로 이동 (이동 중엔 Dodging = true)
    public void StartDash(int dir, float length, float speed, bool backstep = false)
    {
        backstepDodge = false; // 대시 베기 모션으로 보이게 (원래대로)
        skillMove = true;      // 대신 DashSlashAttack의 피해는 막음
        dodgeDir = dir > 0 ? 1 : -1;
        dashSpeed = speed;
        dodgeUntil = Time.time + length;
        IsRunning = false;
    }

    public void Respawn()
    {
        transform.position = spawn;
        body.velocity = Vector2.zero;

        jumpRequestedUntil = 0f;
        dodgeUntil = 0f;
        lungeUntil = 0f;
        SlowUntil = 0f;
        HasteUntil = 0f;
        Stamina = MaxStamina;
        staminaTimer = 0;

        backstepDodge = false;
        skillMove = false;

        IsGrounded = true;
        IsRunning = false;
        Facing = 1;
        CanMove = true;
        Downed = false;
    }

    // Back on his feet somewhere other than the spawn (the back hill checkpoint).
    public void RespawnAt(Vector2 position)
    {
        Respawn();
        transform.position = position;
    }

    public void SetSpawn(Vector2 position)
    {
        spawn = position;
        Respawn();
    }
}
