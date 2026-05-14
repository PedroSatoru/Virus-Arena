using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controlador do Boss Final da Fase 3.
/// Ciclo: Telegrafar (2s) → Atacar (8 projéteis) → Vulnerável (2.5s) → Repetir
/// 7 ataques roxos (anti-player) + 1 ataque amarelo (anti-corpo, dano ao body).
/// 2 variações de padrão de ataque que se alternam.
/// Feixes indicadores aparecem 2s antes do ataque.
/// </summary>
public class BossController : MonoBehaviour
{
    public enum BossState { Idle, Telegraphing, Attacking, Vulnerable }

    [Header("Estado")]
    public BossState currentState = BossState.Idle;

    [Header("Configurações de Ataque")]
    public float telegraphDuration = 1.5f;
    public float telegraphHighWallDuration = 2.5f;
    public float telegraphCeilingDuration = 3f;
    public float telegraphFloorDuration = 0.35f;
    public float vulnerableDuration = 2.5f;
    public float bodyDamagePerAttack = 50f;
    public int playerDamage = 1;

    [Header("Projéteis")]
    public GameObject antiBodyBulletPrefab;
    public GameObject antiPlayerBulletPrefab;
    public float bulletSpeed = 6f;

    [Header("Boss HP UI")]
    public Slider bossHealthSlider;
    public Image bossHealthFill;
    public Text bossHealthLabel;

    private EnemyHealth health;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private int currentPattern = 0;
    private bool isActive = false;

    // --- Audio ---
    private AudioSource audioSource;
    private AudioClip chargeClip;
    private AudioClip shootClip;

    // 2 padrões de ataque, cada um com 8 alvos
    // Índice 0 = amarelo (corpo), índices 1-7 = roxo (player)
    private Vector2[][] attackPatterns;

    public event System.Action OnBossDefeated;

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (health != null)
            health.OnEnemyDeath += HandleDeath;

        // Configurar Audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.7f;
        chargeClip = Resources.Load<AudioClip>("Audio/Boss_loaging_shoot");
        shootClip = Resources.Load<AudioClip>("Audio/Boss_shoot");

        DefineAttackPatterns();
    }

    void DefineAttackPatterns()
    {
        attackPatterns = new Vector2[2][];

        // Padrão A — cobertura total da arena, só plataformas protegem
        attackPatterns[0] = new Vector2[]
        {
            // Chão (6 tiros espalhados)
            new Vector2(-8f, -4.5f),
            new Vector2(-5f, -4.5f),
            new Vector2(-2f, -4.5f),
            new Vector2(2f, -4.5f),
            new Vector2(5f, -4.5f),
            new Vector2(8f, -4.5f),
            // Parede esquerda (4 tiros)
            new Vector2(-9f, -3f),
            new Vector2(-9f, -1f),
            new Vector2(-9f, 1f),
            new Vector2(-9f, 3f),
            // Parede direita (4 tiros)
            new Vector2(9f, -3f),
            new Vector2(9f, -1f),
            new Vector2(9f, 1f),
            new Vector2(9f, 3f),
            // Teto (6 tiros espalhados)
            new Vector2(-8f, 4.5f),
            new Vector2(-5f, 4.5f),
            new Vector2(-2f, 4.5f),
            new Vector2(2f, 4.5f),
            new Vector2(5f, 4.5f),
            new Vector2(8f, 4.5f),
            // Diagonais intermediárias (4 tiros)
            new Vector2(-6f, -3f),
            new Vector2(6f, -3f),
            new Vector2(-6f, 3f),
            new Vector2(6f, 3f),
        };

        // Padrão B — mesma cobertura total, posições ligeiramente deslocadas
        attackPatterns[1] = new Vector2[]
        {
            // Chão (6 tiros deslocados)
            new Vector2(-7f, -4.5f),
            new Vector2(-3.5f, -4.5f),
            new Vector2(0f, -4.5f),
            new Vector2(3.5f, -4.5f),
            new Vector2(6f, -4.5f),
            new Vector2(8.5f, -4.5f),
            // Parede esquerda (4 tiros deslocados)
            new Vector2(-9f, -2f),
            new Vector2(-9f, 0f),
            new Vector2(-9f, 2f),
            new Vector2(-9f, 4f),
            // Parede direita (4 tiros deslocados)
            new Vector2(9f, -2f),
            new Vector2(9f, 0f),
            new Vector2(9f, 2f),
            new Vector2(9f, 4f),
            // Teto (6 tiros deslocados)
            new Vector2(-7f, 4.5f),
            new Vector2(-3.5f, 4.5f),
            new Vector2(0f, 4.5f),
            new Vector2(3.5f, 4.5f),
            new Vector2(6f, 4.5f),
            new Vector2(8.5f, 4.5f),
            // Diagonais intermediárias (4 tiros deslocados)
            new Vector2(-5f, -2f),
            new Vector2(5f, -2f),
            new Vector2(-5f, 2f),
            new Vector2(5f, 2f),
        };
    }

    /// <summary>
    /// Chamado pelo GameManager quando o timer da fase 3 acaba.
    /// </summary>
    public void ActivateBoss()
    {
        isActive = true;

        // Configurar invulnerabilidade inicial
        if (health != null)
            health.isInvulnerable = true;

        UpdateBossUI();
        StartCoroutine(BossCycle());
    }

    Vector2 GetRandomYellowTarget()
    {
        // Usa sempre as probabilidades customizadas, nunca sobrepõe propositalmente
        float r = Random.value;
        if (r < 0.05f) // 5% teto
        {
            return new Vector2(Random.Range(-8f, 8f), 4.5f);
        }
        else if (r < 0.15f) // 10% parede alta (5% + 10% = 15%)
        {
            float x = Random.value > 0.5f ? 9f : -9f;
            return new Vector2(x, Random.Range(1f, 3.5f));
        }
        else if (r < 0.60f) // 45% parede baixa (15% + 45% = 60%)
        {
            float x = Random.value > 0.5f ? 9f : -9f;
            return new Vector2(x, Random.Range(-3f, 1f));
        }
        else // 40% chão
        {
            return new Vector2(Random.Range(-8f, 8f), -4.5f);
        }
    }

    IEnumerator BossCycle()
    {
        // Pequena pausa inicial antes do primeiro ataque
        yield return new WaitForSeconds(1f);

        while (isActive)
        {
            Vector2[] rawPurplePattern = attackPatterns[currentPattern];
            currentPattern = (currentPattern + 1) % 2;

            // Sortear alvo amarelo (corpo)
            Vector2 yellowTarget = GetRandomYellowTarget();

            // Filtrar os alvos roxos para remover qualquer um que esteja muito próximo do amarelo
            List<Vector2> filteredPurple = new List<Vector2>();
            float minDistance = 1.5f; // Distância mínima para não considerar "junto"
            foreach (var p in rawPurplePattern)
            {
                if (Vector2.Distance(p, yellowTarget) > minDistance)
                {
                    filteredPurple.Add(p);
                }
            }
            Vector2[] purplePattern = filteredPurple.ToArray();

            // === TELEGRAFAR ===
            currentState = BossState.Telegraphing;
            if (health != null) health.isInvulnerable = true;

            if (audioSource != null && chargeClip != null)
            {
                audioSource.clip = chargeClip;
                audioSource.Play();
            }

            // O tempo de telegrafia depende do alvo AMARELO
            float telegraphTime = telegraphDuration;
            if (yellowTarget.y >= 4f)
            {
                telegraphTime = telegraphCeilingDuration; // 3.0s
            }
            else if (yellowTarget.y >= 1f && (yellowTarget.x <= -8.5f || yellowTarget.x >= 8.5f))
            {
                telegraphTime = telegraphHighWallDuration; // 2.5s
            }
            else if (yellowTarget.y <= -4f)
            {
                telegraphTime = telegraphFloorDuration; // 0.35s
            }

            List<GameObject> beams = CreateBeamIndicators(purplePattern, yellowTarget);

            yield return new WaitForSeconds(telegraphTime);

            // Destruir indicadores
            foreach (var beam in beams)
            {
                if (beam != null) Destroy(beam);
            }

            // === ATACAR ===
            currentState = BossState.Attacking;

            if (audioSource != null && shootClip != null)
            {
                audioSource.clip = shootClip;
                audioSource.Play();
            }

            FireAttack(purplePattern, yellowTarget);

            // Aplicar dano direto ao corpo E órgão
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ApplyBossDamage(bodyDamagePerAttack);
            }
            else if (InfiniteGameManager.Instance != null)
            {
                InfiniteGameManager.Instance.ApplyBossDamage(bodyDamagePerAttack);
            }

            yield return new WaitForSeconds(0.3f);

            // === VULNERÁVEL ===
            currentState = BossState.Vulnerable;
            if (health != null) health.isInvulnerable = false;

            StartCoroutine(VulnerableFlash());

            yield return new WaitForSeconds(vulnerableDuration);

            // Voltar a invulnerável
            if (health != null) health.isInvulnerable = true;
            currentState = BossState.Idle;

            yield return new WaitForSeconds(0.2f);
        }
    }

    List<GameObject> CreateBeamIndicators(Vector2[] purplePattern, Vector2 yellowTarget)
    {
        List<GameObject> beams = new List<GameObject>();

        // Feixes Roxos
        for (int i = 0; i < purplePattern.Length; i++)
        {
            GameObject beamObj = new GameObject($"BossBeamPurple_{i}");
            beamObj.transform.position = transform.position;

            LineRenderer lr = beamObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, transform.position);
            lr.SetPosition(1, (Vector3)purplePattern[i]);
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;
            lr.sortingOrder = 20;
            lr.material = new Material(Shader.Find("Sprites/Default"));

            lr.startColor = new Color(0.6f, 0f, 0.8f, 0.6f);
            lr.endColor = new Color(0.6f, 0f, 0.8f, 0.3f);

            beamObj.AddComponent<BeamPulse>();
            beams.Add(beamObj);
        }

        // Feixe Amarelo
        GameObject yellowBeam = new GameObject("BossBeamYellow");
        yellowBeam.transform.position = transform.position;
        LineRenderer ylr = yellowBeam.AddComponent<LineRenderer>();
        ylr.positionCount = 2;
        ylr.SetPosition(0, transform.position);
        ylr.SetPosition(1, (Vector3)yellowTarget);
        ylr.startWidth = 0.08f;
        ylr.endWidth = 0.08f;
        ylr.sortingOrder = 21; // Por cima dos roxos
        ylr.material = new Material(Shader.Find("Sprites/Default"));
        ylr.startColor = new Color(1f, 0.9f, 0f, 0.6f);
        ylr.endColor = new Color(1f, 0.9f, 0f, 0.3f);
        yellowBeam.AddComponent<BeamPulse>();
        beams.Add(yellowBeam);

        return beams;
    }

    void FireAttack(Vector2[] purplePattern, Vector2 yellowTarget)
    {
        // Disparar Roxos
        for (int i = 0; i < purplePattern.Length; i++)
        {
            if (antiPlayerBulletPrefab == null) continue;

            Vector2 direction = ((Vector2)purplePattern[i] - (Vector2)transform.position).normalized;
            GameObject bullet = Instantiate(antiPlayerBulletPrefab, transform.position, Quaternion.identity);
            bullet.transform.localScale *= 2f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Initialize(direction, bulletSpeed, Projectile.BulletType.EnemyAntiPlayer);
            }
        }

        // Disparar Amarelo
        if (antiBodyBulletPrefab != null)
        {
            Vector2 direction = ((Vector2)yellowTarget - (Vector2)transform.position).normalized;
            GameObject bullet = Instantiate(antiBodyBulletPrefab, transform.position, Quaternion.identity);
            bullet.transform.localScale *= 2f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Initialize(direction, bulletSpeed, Projectile.BulletType.EnemyAntiBody);
            }
        }
    }

    IEnumerator VulnerableFlash()
    {
        if (spriteRenderer == null) yield break;

        float elapsed = 0f;
        while (elapsed < vulnerableDuration && currentState == BossState.Vulnerable)
        {
            float t = Mathf.PingPong(elapsed * 4f, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, Color.white, t * 0.6f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    void Update()
    {
        if (!isActive) return;

        // Atualizar UI do boss
        UpdateBossUI();
    }

    void UpdateBossUI()
    {
        if (health == null) return;

        int currentHP = health.GetCurrentHP();
        int maxHP = health.maxHP;

        if (bossHealthSlider != null)
        {
            bossHealthSlider.maxValue = maxHP;
            bossHealthSlider.value = currentHP;
        }

        if (bossHealthFill != null)
        {
            float ratio = (float)currentHP / maxHP;
            bossHealthFill.color = Color.Lerp(Color.red, new Color(0.5f, 0f, 0.7f), ratio);
        }

        if (bossHealthLabel != null)
        {
            bossHealthLabel.text = $"BOSS: {currentHP}/{maxHP}";
        }
    }

    void HandleDeath()
    {
        isActive = false;
        currentState = BossState.Idle;

        // Limpar feixes residuais
        foreach (var beam in FindObjectsByType<BeamPulse>(FindObjectsSortMode.None))
        {
            if (beam != null) Destroy(beam.gameObject);
        }

        OnBossDefeated?.Invoke();
    }
}
