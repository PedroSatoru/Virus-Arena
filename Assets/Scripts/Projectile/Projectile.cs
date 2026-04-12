using UnityEngine;

/// <summary>
/// Controla projéteis do jogo.
/// Tipos: PlayerBullet (branco/azul), EnemyAntiBody (amarelo - dano no cenário), EnemyAntiPlayer (roxo - dano no player).
/// O player BLOQUEIA projéteis AntiBody (body block) - eles não o ferem.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Projectile : MonoBehaviour
{
    public enum BulletType
    {
        PlayerBullet,    // Branco/Azul - dano em inimigos
        EnemyAntiBody,   // Amarelo - dano no cenário (bloqueável pelo player)
        EnemyAntiPlayer  // Roxo - dano no player
    }

    [Header("Configurações")]
    public BulletType bulletType;
    public float speed = 10f;
    public int damage = 1;
    public float lifetime = 5f;

    private Vector2 direction;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.15f;
    }

    /// <summary>
    /// Inicializa o projétil com direção, velocidade e tipo.
    /// </summary>
    public void Initialize(Vector2 dir, float spd, BulletType type)
    {
        direction = dir.normalized;
        speed = spd;
        bulletType = type;
        
        rb.linearVelocity = direction * speed;

        // Definir cor baseada no tipo
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            switch (bulletType)
            {
                case BulletType.PlayerBullet:
                    sr.color = new Color(0.6f, 0.85f, 1f, 1f); // Azul claro
                    break;
                case BulletType.EnemyAntiBody:
                    sr.color = new Color(1f, 0.9f, 0f, 1f); // Amarelo brilhante
                    break;
                case BulletType.EnemyAntiPlayer:
                    sr.color = new Color(0.6f, 0f, 0.8f, 1f); // Roxo
                    break;
            }
        }

        // Auto-destruir após lifetime
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        switch (bulletType)
        {
            case BulletType.PlayerBullet:
                HandlePlayerBulletCollision(other);
                break;
            case BulletType.EnemyAntiBody:
                HandleAntiBodyCollision(other);
                break;
            case BulletType.EnemyAntiPlayer:
                HandleAntiPlayerCollision(other);
                break;
        }
    }

    /// <summary>
    /// Projétil do player: causa dano em inimigos.
    /// </summary>
    void HandlePlayerBulletCollision(Collider2D other)
    {
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Destruir ao colidir com arena
        if (other.CompareTag("Arena") || other.CompareTag("ArenaFloor") 
            || other.CompareTag("ArenaWall") || other.CompareTag("ArenaCeiling"))
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Projétil Anti-Corpo (amarelo): causa dano no cenário.
    /// O player pode "body block" - o projétil é destruído ao tocar o player sem causar dano.
    /// </summary>
    void HandleAntiBodyCollision(Collider2D other)
    {
        // Body Block pelo player - projétil desaparece, sem dano ao player
        if (other.gameObject.layer == LayerMask.NameToLayer("Player") || other.CompareTag("Player"))
        {
            Destroy(gameObject);
            return;
        }

        // Dano ao cenário (arena)
        ArenaManager arena = other.GetComponent<ArenaManager>();
        if (arena != null)
        {
            arena.TakeArenaDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Colidir com superfícies da arena
        if (other.CompareTag("ArenaFloor") || other.CompareTag("ArenaWall") || other.CompareTag("ArenaCeiling"))
        {
            ArenaManager arenaManager = FindFirstObjectByType<ArenaManager>();
            if (arenaManager != null)
            {
                arenaManager.TakeArenaDamage(damage);
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Projétil Anti-Player (roxo): causa dano no player.
    /// </summary>
    void HandleAntiPlayerCollision(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Destruir ao colidir com arena
        if (other.CompareTag("Arena") || other.CompareTag("ArenaFloor") 
            || other.CompareTag("ArenaWall") || other.CompareTag("ArenaCeiling"))
        {
            Destroy(gameObject);
        }
    }
}
