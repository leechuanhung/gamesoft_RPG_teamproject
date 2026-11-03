using UnityEngine;

public class PrototypePlayer : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float runSpeed = 10f;
    public KeyCode runKey = KeyCode.LeftShift;
    public float jumpSpeed = 11f;
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
        for (int i = 0; i < count; i++)
            if (groundHits[i].normal.y > 0.65f) grounded = true;

        IsGrounded = grounded && body.velocity.y <= 0.1f;
        body.velocity = new Vector2(horizontal * (IsRunning ? runSpeed : moveSpeed), body.velocity.y);
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
