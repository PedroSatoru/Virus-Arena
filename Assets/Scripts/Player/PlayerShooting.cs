using UnityEngine;

/// <summary>
/// Controla o sistema de tiro do Player.
/// Mira com mouse em 360°, disparo com botão esquerdo.
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    [Header("Disparo")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 15f;
    public float fireRate = 0.2f;
    public Transform firePoint;

    [Header("Visual da Mira")]
    public LineRenderer aimLine;
    public float aimLineLength = 2f;

    private float nextFireTime;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        
        // Configurar aim line se existir
        if (aimLine != null)
        {
            aimLine.startWidth = 0.03f;
            aimLine.endWidth = 0.01f;
            aimLine.startColor = new Color(0.5f, 0.8f, 1f, 0.6f);
            aimLine.endColor = new Color(0.5f, 0.8f, 1f, 0.1f);
            aimLine.positionCount = 2;
        }
    }

    void Update()
    {
        // Calcular direção da mira
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        Vector2 aimDirection = (mouseWorldPos - transform.position).normalized;

        // Atualizar visual da mira
        UpdateAimLine(aimDirection);

        // Disparar
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Fire(aimDirection);
            nextFireTime = Time.time + fireRate;
        }
    }

    void UpdateAimLine(Vector2 direction)
    {
        if (aimLine == null) return;

        Vector3 start = firePoint != null ? firePoint.position : transform.position;
        Vector3 end = start + (Vector3)(direction * aimLineLength);

        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    void Fire(Vector2 direction)
    {
        if (bulletPrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        
        // Configurar rotação do projétil
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Configurar projétil
        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(direction, bulletSpeed, Projectile.BulletType.PlayerBullet);
        }
        else
        {
            // Fallback: mover com Rigidbody2D
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = direction * bulletSpeed;
            }
        }
    }
}
