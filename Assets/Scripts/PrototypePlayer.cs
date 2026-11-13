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
    // A swing: he steps forward with the blow, and his feet are slowed while the sword is moving.
    private float lungeUntil, lungeSpeed;
    public float SlowUntil { get; set; }
    public const float SwingSlow = .4f;
    public void Lunge(int direction, float speed, float length) { lungeSpeed = direction * speed; lungeUntil = Time.time + length; }
    private int dodgeDir = 1;
    private float dashSpeed = DodgeSpeed;   // 대시 속도 (스킬마다 다르게 쓰기 위해)
    // Light straw sandals (JinaItems) quicken his walk and run until then.
    public const float HasteBoost = 1.35f;
    public float HasteUntil { get; set; }
    public bool Hasted => Time.time < HasteUntil;
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
                var boat = groundHits[i].collider.GetComponent<PrototypeBoat>();
                if (boat != null) carry = boat.Velocity.x;
            }

        IsGrounded = grounded && body.velocity.y <= 0.1f;
        float walk = horizontal * (IsRunning ? runSpeed : moveSpeed) * (Hasted ? HasteBoost : 1) * (grounded && Time.time < SlowUntil ? SwingSlow : 1);
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
    public void StartDash(int dir, float length, float speed)
    {
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
        dodgeUntil = lungeUntil = 0f; SlowUntil = 0f;
        HasteUntil = 0f;
        IsGrounded = true;
        IsRunning = false;
        Facing = 1;
        CanMove = true;
        Downed = false;
    }

    public void SetSpawn(Vector2 position)
    {
        spawn = position;
        Respawn();
    }
}
