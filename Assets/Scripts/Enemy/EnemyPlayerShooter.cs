using UnityEngine;

/// <summary>
/// Controla o Atirador Anti-Player (Roxo/Violeta).
/// Mira diretamente no player e dispara projéteis roxos que causam dano a ele.
/// Flutua pela arena, Fase 1: mira no player. 1 HP (morre em um tiro).
/// </summary>
public class EnemyPlayerShooter : MonoBehaviour
{
    [Header("Configurações de Tiro")]
    public GameObject bulletPrefab;
    public float fireInterval = 3f;
    public float bulletSpeed = 5f;
    public float predictAhead = 0.4f; // Quanto prever a posição futura do player

    [Header("Movimento")]
    public float floatSpeed = 1.2f;
    public float floatAmplitude = 0.4f;
    public float horizontalSpeed = 1.2f;

    [Header("Fase")]
    public int currentPhase = 1;

    [Header("Referências da Arena")]
    public float arenaMinX = -9f;
    public float arenaMaxX = 9f;

    private float nextFireTime;
    private float startY;
    private float moveDirection = -1f; // Começa indo para o lado oposto do Atirador Anti-Corpo
    private Transform playerTransform;

    void Start()
    {
        startY = transform.position.y;
        nextFireTime = Time.time + Random.Range(1f, fireInterval);
        moveDirection = Random.value > 0.5f ? 1f : -1f;

        // Buscar player
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
    }

    void Update()
    {
        // Flutuação vertical
        float newY = startY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        // Movimento horizontal
        float newX = transform.position.x + moveDirection * horizontalSpeed * Time.deltaTime;

        // Inverter direção nas bordas
        if (newX > arenaMaxX - 1f)
            moveDirection = -1f;
        else if (newX < arenaMinX + 1f)
            moveDirection = 1f;

        transform.position = new Vector3(newX, newY, transform.position.z);

        // Disparar
        if (Time.time >= nextFireTime)
        {
            FireAtPlayer();
            nextFireTime = Time.time + fireInterval + Random.Range(-0.5f, 0.5f);
        }
    }

    /// <summary>
    /// Atira projétil roxo diretamente no player (com leve predição de posição).
    /// </summary>
    void FireAtPlayer()
    {
        if (bulletPrefab == null || playerTransform == null) return;

        // Calcular posição predita do player
        Vector2 playerPos = playerTransform.position;
        Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
        if (playerRb != null)
            playerPos += playerRb.linearVelocity * predictAhead;

        Vector2 direction = (playerPos - (Vector2)transform.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(direction, bulletSpeed, Projectile.BulletType.EnemyAntiPlayer);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0f, 0.8f, 0.5f);
        if (playerTransform != null)
            Gizmos.DrawLine(transform.position, playerTransform.position);
    }
}
