using UnityEngine;

public class PrototypePlayer : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float runSpeed = 10f;
    public KeyCode runKey = KeyCode.LeftShift;
    public float jumpSpeed = 11f;
    // Backstep (K): a short hop away from where he faces, untouchable while it lasts.
    public const float DodgeLength = .3f, DodgeSpeed = 9f, DodgeCooldown = .7f;
    private float dodgeUntil, nextDodge;
    public bool Dodging => Time.time < dodgeUntil;
    // A·D 더블클릭 대시: 두 번 누르는 간격(초), 마지막으로 누른 시각, 대시 방향
    public const float DoubleTapWindow = .25f;
    private float lastLeftTap = -1, lastRightTap = -1;
    private int dodgeDir = 1;
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
        // A를 빠르게 두 번 → 왼쪽 대시, D를 빠르게 두 번 → 오른쪽 대시
        int tapDir = 0;
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (Time.time - lastLeftTap <= DoubleTapWindow) tapDir = -1;
            lastLeftTap = Time.time;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (Time.time - lastRightTap <= DoubleTapWindow) tapDir = 1;
            lastRightTap = Time.time;
        }
        if (tapDir != 0 && IsGrounded && Time.time >= nextDodge)
        {
            dodgeDir = tapDir;
            Facing = tapDir;
            dodgeUntil = Time.time + DodgeLength;
            nextDodge = Time.time + DodgeCooldown;
            lastLeftTap = lastRightTap = -1;
            IsRunning = false;
            return;
        }
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
        body.velocity = new Vector2(Dodging ? dodgeDir * DodgeSpeed : horizontal * (IsRunning ? runSpeed : moveSpeed) * (Hasted ? HasteBoost : 1) + carry, body.velocity.y);
        if (jumpRequestedUntil > Time.time && grounded && body.velocity.y <= 0.1f)
        {
            body.velocity = new Vector2(body.velocity.x, jumpSpeed);
            IsGrounded = false;
            jumpRequestedUntil = 0f;
        }
    }

    public void Respawn()
    {
        transform.position = spawn;
        body.velocity = Vector2.zero;
        jumpRequestedUntil = 0f;
        dodgeUntil = nextDodge = 0f;
        lastLeftTap = lastRightTap = -1;
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
