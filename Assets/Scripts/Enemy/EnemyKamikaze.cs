using UnityEngine;

/// <summary>
/// Inimigo Kamikaze. 
/// Nasce em um ponto oposto ao jogador, persegue lentamente e, ao chegar muito perto,
/// explode, causando dano em área. Se explodir perto de paredes/teto/chão, dá dano no órgão.
/// </summary>
public class EnemyKamikaze : MonoBehaviour
{
    [Header("Configurações")]
    public float baseMoveSpeed = 3.5f;
    public float explosionRadius = 1.5f;
    public int playerDamage = 1;
    public float bodyDamage = 15f; // Dano ao órgão/corpo se bater na parede

    [Header("Limites da Arena")]
    public float arenaMinX = -9f;
    public float arenaMaxX = 9f;
    public float arenaMinY = -4f;
    public float arenaMaxY = 4.5f;

    private Transform playerTransform;
    private EnemyHealth health;
    private bool hasExploded = false;

    void Start()
    {
        health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.OnEnemyDeath += HandleDeath;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    void Update()
    {
        if (hasExploded) return;

        if (playerTransform != null)
        {
            // Movimentação em direção ao player
            float currentSpeed = baseMoveSpeed;
            if (GameManager.Instance != null)
                currentSpeed *= GameManager.Instance.speedMultiplier;

            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, currentSpeed * Time.deltaTime);

            // Verificar distância para explodir
            if (Vector2.Distance(transform.position, playerTransform.position) <= explosionRadius)
            {
                Explode();
            }
        }
    }

    void Explode()
    {
        hasExploded = true;

        // Feedback visual: cria um círculo vermelho efêmero
        GameObject explosionVisual = new GameObject("KamikazeExplosion");
        explosionVisual.transform.position = transform.position;
        SpriteRenderer sr = explosionVisual.AddComponent<SpriteRenderer>();
        // Será configurado pela cor depois, ou usamos OnDrawGizmos (apenas debug).
        // Um script para destrói-lo em 0.3s
        Destroy(explosionVisual, 0.3f);

        // Dano ao Player (Overlapping círculo)
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(transform.position, explosionRadius, LayerMask.GetMask("Player"));
        foreach (var hit in hitPlayers)
        {
            PlayerHealth pHealth = hit.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.TakeDamage(playerDamage);
            }
        }

        // Dano à Arena/Corpo
        // Verifica se a explosão está perto de algumas das bordas mapeadas
        bool hitArena = false;
        if (transform.position.x - explosionRadius <= arenaMinX ||
            transform.position.x + explosionRadius >= arenaMaxX ||
            transform.position.y - explosionRadius <= arenaMinY ||
            transform.position.y + explosionRadius >= arenaMaxY)
        {
            hitArena = true;
        }

        if (hitArena && GameManager.Instance != null)
        {
            GameManager.Instance.ApplyOrganDamage(bodyDamage);
        }

        // Morre imediatamente sem dar trigger em "OnEnemyDeath" padrão que spawna outros,
        // ou dá trigger normalmente para contar kill. Vamos apenas destruir.
        Destroy(gameObject);
    }

    void HandleDeath()
    {
        // Se morreu antes de explodir por ataques do player, pode fazer algo a mais ou apenas sumir normal
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
}
