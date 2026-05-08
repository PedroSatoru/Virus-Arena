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
    public float telegraphDuration = 2f;
    public float telegraphCeilingDuration = 3f;
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

        DefineAttackPatterns();
    }

    void DefineAttackPatterns()
    {
        attackPatterns = new Vector2[2][];

        // Padrão A — alvos bem espalhados, muitos para cima
        attackPatterns[0] = new Vector2[]
        {
            new Vector2(-7f, -4.5f),     // chão esquerda
            new Vector2(7f, -4.5f),      // chão direita
            new Vector2(-9f, -1f),       // parede esq baixa
            new Vector2(9f, 1f),         // parede dir alta
            new Vector2(-9f, 3f),        // parede esq alta
            new Vector2(-4f, 4.5f),      // teto esquerda
            new Vector2(2f, 4.5f),       // teto centro-dir
            new Vector2(7f, 4.5f),       // teto direita
        };

        // Padrão B — posições alternativas, também muitas para cima
        attackPatterns[1] = new Vector2[]
        {
            new Vector2(-4f, -4.5f),     // chão esq
            new Vector2(4f, -4.5f),      // chão dir
            new Vector2(9f, -2f),        // parede dir baixa
            new Vector2(-9f, 0f),        // parede esq meio
            new Vector2(9f, 3f),         // parede dir alta
            new Vector2(-6f, 4.5f),      // teto esquerda
            new Vector2(0f, 4.5f),       // teto centro
            new Vector2(6f, 4.5f),       // teto direita
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

    IEnumerator BossCycle()
    {
        // Pequena pausa inicial antes do primeiro ataque
        yield return new WaitForSeconds(1f);

        while (isActive)
        {
            Vector2[] pattern = attackPatterns[currentPattern];
            currentPattern = (currentPattern + 1) % 2;

            // Sortear qual dos 8 tiros será o amarelo (corpo) — qualquer posição
            int yellowIndex = Random.Range(0, pattern.Length);

            // Verificar se algum alvo é teto (y >= 4)
            bool hasCeiling = false;
            foreach (var target in pattern)
            {
                if (target.y >= 4f) { hasCeiling = true; break; }
            }

            // === TELEGRAFAR ===
            currentState = BossState.Telegraphing;
            if (health != null) health.isInvulnerable = true;

            float telegraphTime = hasCeiling ? telegraphCeilingDuration : telegraphDuration;
            List<GameObject> beams = CreateBeamIndicators(pattern, yellowIndex);

            yield return new WaitForSeconds(telegraphTime);

            // Destruir indicadores
            foreach (var beam in beams)
            {
                if (beam != null) Destroy(beam);
            }

            // === ATACAR ===
            currentState = BossState.Attacking;
            FireAttack(pattern, yellowIndex);

            // Aplicar dano direto ao corpo E órgão
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ApplyBossDamage(bodyDamagePerAttack);
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

    List<GameObject> CreateBeamIndicators(Vector2[] pattern, int yellowIndex)
    {
        List<GameObject> beams = new List<GameObject>();

        for (int i = 0; i < pattern.Length; i++)
        {
            GameObject beamObj = new GameObject($"BossBeam_{i}");
            beamObj.transform.position = transform.position;

            LineRenderer lr = beamObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, transform.position);
            lr.SetPosition(1, (Vector3)pattern[i]);
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;
            lr.sortingOrder = 20;
            lr.material = new Material(Shader.Find("Sprites/Default"));

            if (i == yellowIndex)
            {
                // Feixe amarelo (ataque ao corpo) — posição aleatória
                lr.startColor = new Color(1f, 0.9f, 0f, 0.6f);
                lr.endColor = new Color(1f, 0.9f, 0f, 0.3f);
            }
            else
            {
                // Feixe roxo (ataque ao player)
                lr.startColor = new Color(0.6f, 0f, 0.8f, 0.6f);
                lr.endColor = new Color(0.6f, 0f, 0.8f, 0.3f);
            }

            beamObj.AddComponent<BeamPulse>();
            beams.Add(beamObj);
        }

        return beams;
    }

    void FireAttack(Vector2[] pattern, int yellowIndex)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            Vector2 direction = ((Vector2)pattern[i] - (Vector2)transform.position).normalized;

            // O índice sorteado é amarelo (anti-corpo), resto = roxo (anti-player)
            bool isYellow = (i == yellowIndex);
            GameObject prefab = isYellow ? antiBodyBulletPrefab : antiPlayerBulletPrefab;
            if (prefab == null) continue;

            GameObject bullet = Instantiate(prefab, transform.position, Quaternion.identity);

            // Projéteis do boss são maiores (2x)
            bullet.transform.localScale *= 2f;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                Projectile.BulletType type = isYellow
                    ? Projectile.BulletType.EnemyAntiBody
                    : Projectile.BulletType.EnemyAntiPlayer;
                proj.Initialize(direction, bulletSpeed, type);
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
