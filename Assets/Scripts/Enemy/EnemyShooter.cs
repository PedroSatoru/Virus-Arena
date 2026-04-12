using UnityEngine;

/// <summary>
/// Controla o Atirador Anti-Corpo (Amarelo).
/// Na Fase 1 (Pulmão): atira APENAS no chão da arena.
/// Spawna nas bordas superiores, flutua e dispara periodicamente.
/// </summary>
public class EnemyShooter : MonoBehaviour
{
    [Header("Configurações de Tiro")]
    public GameObject bulletPrefab;
    public float fireInterval = 2.5f;
    public float bulletSpeed = 8f;

    [Header("Movimento")]
    public float floatSpeed = 1f;
    public float floatAmplitude = 0.5f;
    public float horizontalSpeed = 1.5f;

    [Header("Fase")]
    public int currentPhase = 1; // 1=Pulmão, 2=Coração, 3=Cérebro

    [Header("Referências da Arena")]
    public float arenaMinX = -8f;
    public float arenaMaxX = 8f;
    public float arenaFloorY = -4f;
    public float arenaLeftX = -8.5f;
    public float arenaRightX = 8.5f;
    public float arenaCeilingY = 4.5f;

    private float nextFireTime;
    private float startY;
    private float moveDirection = 1f;

    void Start()
    {
        startY = transform.position.y;
        nextFireTime = Time.time + Random.Range(0.5f, fireInterval);
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
            FireAtArena();
            nextFireTime = Time.time + fireInterval + Random.Range(-0.5f, 0.5f);
        }
    }

    /// <summary>
    /// Dispara projétil amarelo mirando nas superfícies da arena.
    /// Fase 1: apenas chão. Fase 2: chão + paredes. Fase 3: tudo.
    /// </summary>
    void FireAtArena()
    {
        if (bulletPrefab == null) return;

        Vector2 targetPos;
        
        switch (currentPhase)
        {
            case 1: // Pulmão - apenas chão
                targetPos = GetFloorTarget();
                break;
            case 2: // Coração - chão + paredes
                if (Random.value > 0.5f)
                    targetPos = GetFloorTarget();
                else
                    targetPos = GetWallTarget();
                break;
            case 3: // Cérebro - tudo
                float rand = Random.value;
                if (rand < 0.4f)
                    targetPos = GetFloorTarget();
                else if (rand < 0.7f)
                    targetPos = GetWallTarget();
                else
                    targetPos = GetCeilingTarget();
                break;
            default:
                targetPos = GetFloorTarget();
                break;
        }

        Vector2 direction = ((Vector2)targetPos - (Vector2)transform.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(direction, bulletSpeed, Projectile.BulletType.EnemyAntiBody);
        }
    }

    Vector2 GetFloorTarget()
    {
        float randomX = Random.Range(arenaMinX + 1f, arenaMaxX - 1f);
        return new Vector2(randomX, arenaFloorY);
    }

    Vector2 GetWallTarget()
    {
        float randomY = Random.Range(arenaFloorY + 0.5f, startY - 1f);
        if (Random.value > 0.5f)
            return new Vector2(arenaLeftX, randomY);
        else
            return new Vector2(arenaRightX, randomY);
    }

    Vector2 GetCeilingTarget()
    {
        float randomX = Random.Range(arenaMinX + 1f, arenaMaxX - 1f);
        return new Vector2(randomX, arenaCeilingY);
    }
}
