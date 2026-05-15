using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// InfiniteGameManager — Gerencia o modo Endless/Bônus do Virus Arena.
///
/// Diferenças em relação ao GameManager normal:
///   - Timer progressivo (0 → ∞) em vez de regressivo
///   - Nunca termina por tempo; termina apenas com morte do player ou do órgão
///   - A cada 3 minutos exibe painel de PowerUp in-game (sem trocar de cena)
///   - Triple Shot e Speed Boost só podem ser escolhidos 1x por run
///   - Vida Extra (+1 coração +30% corpo HP) pode ser escolhida infinitamente
///   - Fase 3: Boss reaparece a cada 5 minutos
///   - Salva o melhor tempo com PlayerPrefs
/// </summary>
public class InfiniteGameManager : MonoBehaviour
{
    public static InfiniteGameManager Instance { get; private set; }

    // ─── PlayerPrefs Keys ───────────────────────────────────────
    public const string PREF_KEY_PH1 = "InfiniteRecord_Ph1";
    public const string PREF_KEY_PH2 = "InfiniteRecord_Ph2";
    public const string PREF_KEY_PH3 = "InfiniteRecord_Ph3";

    [Header("Fase")]
    public int currentPhase = 1;
    public float damageMultiplier = 1f;

    [Header("Timer (Crescente)")]
    public float timeElapsed = 0f;

    [Header("Vida do Corpo")]
    public float bodyMaxHP = 1500f;
    public float bodyCurrentHP;

    [Header("Vida do Órgão")]
    public float organMaxHP = 500f;
    public float organCurrentHP;

    [Header("Prefabs dos Inimigos")]
    public GameObject antiCorpoPrefab;
    public GameObject playerShooterPrefab;
    public GameObject kamikazePrefab;

    [Header("Boss (Fase 3)")]
    public GameObject bossPrefab;
    public GameObject bossHealthPanel;
    private BossController activeBoss;
    private bool bossActive = false;
    private float nextBossSpawnTime = 300f; // Primeiro boss aos 5 min

    [Header("Escalonamento por Tempo")]
    public float speedMultiplier = 1f;

    [Header("Spawn de Inimigos")]
    public float baseSpawnInterval = 2.5f;
    public float minSpawnInterval = 1f;
    public int maxTotalEnemies = 8;
    public int maxPerType = 3;

    [Header("Arena")]
    public float arenaMinX = -9f;
    public float arenaMaxX = 9f;
    public float arenaMinY = 2f;
    public float arenaMaxY = 4f;

    [Header("Estado")]
    public bool isGameOver = false;
    public bool isPaused = false;

    [Header("UI Game Over (Infinito)")]
    public GameObject infiniteGameOverPanel;
    public Text infiniteTimeText;       // Tempo sobrevivido
    public Text infiniteRecordText;     // Recorde anterior
    public Text infiniteNewRecordText;  // "NOVO RECORDE!" (pode ser null)

    [Header("UI PowerUp In-Game")]
    public GameObject powerUpPanel;
    private float nextPowerUpTime = 180f; // Primeiro PowerUp aos 3 min
    private bool powerUpPending = false;

    [Header("UI Pausa")]
    public GameObject pausePanel;

    // Spawn state
    private float spawnTimer;

    // --- Audio ---
    private AudioSource bgmSource;
    private AudioClip bgmClip;

    // ─── Ciclo de Vida ──────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        currentPhase = GlobalState.infinitePhase;
        damageMultiplier = currentPhase;

        bodyCurrentHP = bodyMaxHP;
        organCurrentHP = organMaxHP;
        timeElapsed = 0f;
        spawnTimer = 2f;

        if (infiniteGameOverPanel != null) infiniteGameOverPanel.SetActive(false);
        if (powerUpPanel != null)         powerUpPanel.SetActive(false);
        if (bossHealthPanel != null)      bossHealthPanel.SetActive(false);

        // Fiação dos botões — Game Over
        if (infiniteGameOverPanel != null)
        {
            WireButton(infiniteGameOverPanel.transform, "RestartBtn",  RestartRun);
            WireButton(infiniteGameOverPanel.transform, "GoMenuBtn",   ReturnToInfiniteSelect);
        }

        // Fiação dos botões — PowerUp
        if (powerUpPanel != null)
        {
            WireButton(powerUpPanel.transform, "PowerUpBox/TripleBtn", ChoosePowerUpTripleShot);
            WireButton(powerUpPanel.transform, "PowerUpBox/SpeedBtn",  ChoosePowerUpSpeedBoost);
            WireButton(powerUpPanel.transform, "PowerUpBox/LifeBtn",   ChoosePowerUpExtraLife);
        }

        // Fiação dos botões — Pausa
        HUDManager hud = FindFirstObjectByType<HUDManager>();
        if (hud != null && hud.pausePanel != null)
        {
            pausePanel = hud.pausePanel;
            WireButton(pausePanel.transform, "ContinueBtn", () => hud.TogglePause());
            WireButton(pausePanel.transform, "MenuBtn",     ReturnToInfiniteSelect);
        }

        // Registrar morte do player
        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null) ph.OnPlayerDeath += OnPlayerDied;

        // --- Configurar Música de Fundo ---
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmClip = Resources.Load<AudioClip>("Audio/musica_fases");
        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = 0.4f;
            bgmSource.pitch = 0.9f;
            bgmSource.Play();
        }
    }

    void Update()
    {
        if (isGameOver || isPaused) return;

        timeElapsed += Time.deltaTime;

        // Escalonamento de velocidade: +10% por minuto
        float minutesPassed = timeElapsed / 60f;
        speedMultiplier = 1f + (minutesPassed * 0.1f);

        // PowerUp a cada 3 minutos
        if (!powerUpPending && timeElapsed >= nextPowerUpTime)
        {
            powerUpPending = true;
            ShowPowerUpPanel();
        }

        // Boss (apenas Fase 3) — reaparece a cada 5 minutos
        if (currentPhase == 3 && !bossActive && timeElapsed >= nextBossSpawnTime)
        {
            SpawnBoss();
        }

        // Spawn de inimigos normais (não spawna se boss ativo)
        if (!bossActive)
        {
            spawnTimer -= Time.deltaTime * speedMultiplier;
            if (spawnTimer <= 0f)
            {
                TrySpawnEnemy();
                float progress = Mathf.Clamp01(minutesPassed / 5f); // satura em 5 min
                spawnTimer = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, progress);
            }
        }
    }

    // ─── Spawn ──────────────────────────────────────────────────

    void TrySpawnEnemy()
    {
        int antiCorpoCount  = FindObjectsByType<EnemyShooter>(FindObjectsSortMode.None).Length;
        int playerShooterCount = FindObjectsByType<EnemyPlayerShooter>(FindObjectsSortMode.None).Length;
        int kamikazeCount   = FindObjectsByType<EnemyKamikaze>(FindObjectsSortMode.None).Length;
        int totalCount = antiCorpoCount + playerShooterCount + kamikazeCount;

        if (totalCount >= maxTotalEnemies) return;

        bool canSpawnAntiCorpo     = antiCorpoPrefab     != null && antiCorpoCount     < maxTotalEnemies;
        bool canSpawnPlayerShooter = playerShooterPrefab != null && playerShooterCount < maxPerType;
        bool canSpawnKamikaze      = kamikazePrefab      != null && kamikazeCount      < maxPerType;

        if (!canSpawnAntiCorpo && !canSpawnPlayerShooter && !canSpawnKamikaze) return;

        GameObject chosenPrefab = null;
        float roll = Random.value;
        if      (roll < 0.6f && canSpawnAntiCorpo)     chosenPrefab = antiCorpoPrefab;
        else if (roll < 0.8f && canSpawnPlayerShooter) chosenPrefab = playerShooterPrefab;
        else if (canSpawnKamikaze)                     chosenPrefab = kamikazePrefab;

        if (chosenPrefab == null)
        {
            if      (canSpawnAntiCorpo)     chosenPrefab = antiCorpoPrefab;
            else if (canSpawnPlayerShooter) chosenPrefab = playerShooterPrefab;
            else if (canSpawnKamikaze)      chosenPrefab = kamikazePrefab;
        }

        Vector3 spawnPos;
        if (chosenPrefab == kamikazePrefab)
        {
            GameObject pObj = GameObject.FindWithTag("Player");
            Vector3 playerPos = pObj != null ? pObj.transform.position : Vector3.zero;
            spawnPos = GetSafeKamikazeSpawnPos(playerPos);
        }
        else
        {
            spawnPos = GetRandomSpawnPos();
        }

        GameObject enemy = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);

        EnemyShooter shooter = enemy.GetComponent<EnemyShooter>();
        if (shooter != null)
        {
            shooter.currentPhase = currentPhase;
            shooter.arenaMinX = arenaMinX;
            shooter.arenaMaxX = arenaMaxX;
        }

        EnemyPlayerShooter pShooter = enemy.GetComponent<EnemyPlayerShooter>();
        if (pShooter != null)
        {
            pShooter.currentPhase = currentPhase;
            pShooter.arenaMinX = arenaMinX;
            pShooter.arenaMaxX = arenaMaxX;
        }
    }

    Vector3 GetRandomSpawnPos()
    {
        float x = Random.Range(arenaMinX + 2f, arenaMaxX - 2f);
        float y = Random.Range(arenaMinY, arenaMaxY);
        return new Vector3(x, y, 0f);
    }

    Vector3 GetSafeKamikazeSpawnPos(Vector3 playerPos)
    {
        float spawnX = playerPos.x < 0
            ? Random.Range(2f, arenaMaxX - 1f)
            : Random.Range(arenaMinX + 1f, -2f);

        float spawnY = playerPos.y < 0
            ? Random.Range(1f, arenaMaxY - 1f)
            : Random.Range(-3f, -1f);

        return new Vector3(spawnX, spawnY, 0f);
    }

    // ─── Dano ───────────────────────────────────────────────────

    /// <summary>
    /// Dano ao órgão (projéteis Anti-Corpo amarelos).
    /// No modo infinito, morte do órgão encerra a run.
    /// </summary>
    public void ApplyOrganDamage(float rawDamage)
    {
        if (isGameOver) return;

        organCurrentHP -= rawDamage;
        float bodyDamage = rawDamage * damageMultiplier;
        bodyCurrentHP -= bodyDamage;

        // PRIORIDADE: Se o corpo morreu, é Game Over imediato
        if (bodyCurrentHP <= 0f)
        {
            bodyCurrentHP = 0f;
            GameOver("FALÊNCIA SISTÊMICA");
            return;
        }

        if (organCurrentHP <= 0f)
        {
            organCurrentHP = 0f;
            GameOver("ÓRGÃO DESTRUÍDO");
        }
    }

    /// <summary>
    /// Dano direto do Boss ao corpo e órgão.
    /// </summary>
    public void ApplyBossDamage(float damage)
    {
        bodyCurrentHP  -= damage;
        organCurrentHP -= damage * 0.5f;

        if (organCurrentHP < 0f) organCurrentHP = 0f;

        if (bodyCurrentHP <= 0f)
        {
            bodyCurrentHP = 0f;
            GameOver("FALÊNCIA SISTÊMICA");
        }
    }

    void OnPlayerDied() => GameOver("GLÓBULO BRANCO DESTRUÍDO");

    // ─── Game Over ──────────────────────────────────────────────

    void GameOver(string reason)
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log($"[INFINITO] GAME OVER: {reason} | Tempo: {FormatTime(timeElapsed)}");

        // Salvar / comparar recorde
        string prefKey = GetPrefKey();
        float previousRecord = PlayerPrefs.GetFloat(prefKey, 0f);
        bool isNewRecord = timeElapsed > previousRecord;

        if (isNewRecord)
            PlayerPrefs.SetFloat(prefKey, timeElapsed);

        PlayerPrefs.Save();

        // Exibir painel
        if (infiniteGameOverPanel != null)
        {
            infiniteGameOverPanel.SetActive(true);

            if (infiniteTimeText != null)
                infiniteTimeText.text = $"TEMPO: {FormatTime(timeElapsed)}";

            if (infiniteRecordText != null)
            {
                float record = PlayerPrefs.GetFloat(prefKey, timeElapsed);
                infiniteRecordText.text = $"RECORDE: {FormatTime(record)}";
            }

            if (infiniteNewRecordText != null)
                infiniteNewRecordText.gameObject.SetActive(isNewRecord);
        }

        StartCoroutine(SlowTimeAndStop());
    }

    IEnumerator SlowTimeAndStop()
    {
        float duration = 1f, elapsed = 0f;
        while (elapsed < duration)
        {
            Time.timeScale = Mathf.Lerp(1f, 0f, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = 0f;
    }

    // ─── Boss (Fase 3) ──────────────────────────────────────────

    void SpawnBoss()
    {
        if (bossPrefab == null) return;

        bossActive = true;
        Debug.Log($"[INFINITO] BOSS apareceu! (t={FormatTime(timeElapsed)})");

        // Destruir inimigos normais
        foreach (var e in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (e.GetComponent<BossController>() == null)
                Destroy(e.gameObject);
        }

        GameObject bossObj = Instantiate(bossPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity);
        activeBoss = bossObj.GetComponent<BossController>();

        if (activeBoss != null)
        {
            activeBoss.OnBossDefeated += OnBossDefeated;

            if (bossHealthPanel != null)
            {
                bossHealthPanel.SetActive(true);
                activeBoss.bossHealthSlider = bossHealthPanel.GetComponentInChildren<UnityEngine.UI.Slider>();
                activeBoss.bossHealthFill   = bossHealthPanel.transform
                    .Find("BossHealthSlider/Fill Area/Fill")
                    ?.GetComponent<UnityEngine.UI.Image>();
                activeBoss.bossHealthLabel  = bossHealthPanel.transform
                    .Find("BossLabel")
                    ?.GetComponent<UnityEngine.UI.Text>();
            }

            activeBoss.ActivateBoss();
        }
    }

    void OnBossDefeated()
    {
        Debug.Log("[INFINITO] Boss derrotado! Run continua...");
        bossActive = false;

        if (bossHealthPanel != null) bossHealthPanel.SetActive(false);

        // Próximo boss em mais 5 minutos
        nextBossSpawnTime = timeElapsed + 300f;
    }

    // ─── PowerUp In-Game ────────────────────────────────────────

    void ShowPowerUpPanel()
    {
        if (powerUpPanel == null) return;

        isPaused = true;
        Time.timeScale = 0f;
        powerUpPanel.SetActive(true);

        // Atualizar estado visual dos botões (desabilitar os já usados)
        RefreshPowerUpButtons();
    }

    void RefreshPowerUpButtons()
    {
        if (powerUpPanel == null) return;

        Transform box = powerUpPanel.transform.Find("PowerUpBox");
        if (box == null) return;

        SetButtonInteractable(box, "TripleBtn", !GlobalState.infiniteTripleShotUsed);
        SetButtonInteractable(box, "SpeedBtn",  !GlobalState.infiniteSpeedBoostUsed);
        // LifeBtn sempre interativo
    }

    void SetButtonInteractable(Transform parent, string btnName, bool interactable)
    {
        Transform child = parent.Find(btnName);
        if (child == null) return;
        UnityEngine.UI.Button btn = child.GetComponent<UnityEngine.UI.Button>();
        if (btn != null) btn.interactable = interactable;

        // Escurecer visualmente
        UnityEngine.UI.Image img = child.GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.color = interactable ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.4f, 0.4f, 0.4f);
    }

    public void ChoosePowerUpTripleShot()
    {
        if (GlobalState.infiniteTripleShotUsed) return;
        GlobalState.hasTripleShot = true;
        GlobalState.infiniteTripleShotUsed = true;
        ClosePowerUpPanel();
    }

    public void ChoosePowerUpSpeedBoost()
    {
        if (GlobalState.infiniteSpeedBoostUsed) return;
        GlobalState.hasSpeedBoost = true;
        GlobalState.infiniteSpeedBoostUsed = true;

        // Aplicar velocidade ao player imediatamente
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.moveSpeed *= 1.3f;

        ClosePowerUpPanel();
    }

    public void ChoosePowerUpExtraLife()
    {
        // Pode ser escolhida infinitamente
        GlobalState.hasHeartAndBodyHP = true;

        // +30% corpo HP (cumulativo, mas limitado ao máximo por segurança)
        float gain = bodyMaxHP * 0.3f;
        bodyCurrentHP = Mathf.Min(bodyCurrentHP + gain, bodyMaxHP * 2f); // cap em 200% do max original

        // +1 coração ao player
        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            ph.currentHearts = Mathf.Min(ph.currentHearts + 1, ph.maxHearts + 5);
            ph.ForceRefreshHUD(); // Atualiza os ícones de coração na HUD
        }

        ClosePowerUpPanel();
    }

    void ClosePowerUpPanel()
    {
        if (powerUpPanel != null) powerUpPanel.SetActive(false);
        powerUpPending = false;
        nextPowerUpTime = timeElapsed + 180f; // Próximo PowerUp em +3 min
        isPaused = false;
        Time.timeScale = 1f;
    }

    // ─── Navegação ──────────────────────────────────────────────

    public void RestartRun()
    {
        Time.timeScale = 1f;
        GlobalState.ResetInfiniteRun();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToInfiniteSelect()
    {
        Time.timeScale = 1f;
        GlobalState.ResetInfiniteRun();
        SceneManager.LoadScene("InfiniteSelectScene");
    }

    // ─── Utilitários ────────────────────────────────────────────

    public string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0}:{1:D2}", m, s);
    }

    string GetPrefKey()
    {
        return currentPhase switch
        {
            2 => PREF_KEY_PH2,
            3 => PREF_KEY_PH3,
            _ => PREF_KEY_PH1,
        };
    }

    void WireButton(Transform parent, string childName, UnityEngine.Events.UnityAction action)
    {
        Transform child = parent.Find(childName);
        if (child == null) return;
        UnityEngine.UI.Button btn = child.GetComponent<UnityEngine.UI.Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        if (Instance == this) Instance = null;
    }
}
