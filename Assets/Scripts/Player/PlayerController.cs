using UnityEngine;

/// <summary>
/// Controla a movimentação do Glóbulo Branco (Player).
/// Movimentação lateral (A/D), pulo (W/Espaço), confinamento na arena.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimentação")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Arena Limits")]
    public float minX = -8f;
    public float maxX = 8f;

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private bool isGrounded;
    private SpriteRenderer spriteRenderer;
    private float moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (GlobalState.hasSpeedBoost)
        {
            moveSpeed *= 1.3f;
        }

        // Configurar Rigidbody2D
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        // Input de movimentação
        moveInput = Input.GetAxisRaw("Horizontal");

        // Ground Check
        if (boxCol != null)
        {
            // Usa BoxCast ligeiramente abaixo dos pés para detectar o chão
            isGrounded = Physics2D.BoxCast(boxCol.bounds.center, boxCol.bounds.size, 0f, Vector2.down, 0.1f, groundLayer);
        }
        else if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else
        {
            // Fallback: raycast para baixo (agora com distância maior para garantir detecção com hitboxes grandes)
            isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer);
        }

        // Pulo
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Flip do sprite baseado na mira
        FlipTowardsMouse();
    }

    void FixedUpdate()
    {
        // Aplicar movimentação
        float targetVelX = moveInput * moveSpeed;
        rb.linearVelocity = new Vector2(targetVelX, rb.linearVelocity.y);

        // Confinar dentro da arena
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        transform.position = pos;
    }

    /// <summary>
    /// Flip do sprite na direção do mouse.
    /// </summary>
    void FlipTowardsMouse()
    {
        if (spriteRenderer == null) return;
        
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (mouseWorldPos.x < transform.position.x)
            spriteRenderer.flipX = true;
        else
            spriteRenderer.flipX = false;
    }

    void OnDrawGizmosSelected()
    {
        // Visualizar ground check no editor
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
