using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// GameManager Singleton - Gerencia o estado global do jogo.
/// Sistema de spawn com limite POR TIPO de inimigo:
///   - Max 5 inimigos totais em tela
///   - Max 2 de cada tipo (AntiCorpo amarelo, PlayerShooter roxo)
/// Se órgão morre: pode continuar para próxima fase. Se corpo morre: Game Over.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Timer")]
    public float totalTime = 180f; // 3 minutos
    public float timeRemaining;

    [Header("Vida do Corpo")]
    public float bodyMaxHP = 1500f;
    public float bodyCurrentHP;

    [Header("Vida do Órgão")]
    public float organMaxHP = 500f;
    public float organCurrentHP;

    [Header("Fase Atual")]
    public int currentPhase = 1; // 1=Pulmão, 2=Coração, 3=Cérebro
    public float damageMultiplier = 1f;

    [Header("Prefabs dos Inimigos")]
    public GameObject antiCorpoPrefab;       // Atirador Anti-Corpo (Amarelo)
    public GameObject playerShooterPrefab;   // Atirador Anti-Player (Roxo)
    public GameObject kamikazePrefab;        // Kamikaze (Laranja/Vermelho escuro)

    [Header("Escalonamento por Tempo")]
    public float speedMultiplier = 1f;

    [Header("Spawn de Inimigos")]
    public float baseSpawnInterval = 2.5f;
    public float minSpawnInterval = 1f;
    public int maxTotalEnemies = 8;          // Máximo total em tela
    public int maxPerType = 3;               // Máximo de cada tipo

    [Header("Arena")]
    public float arenaMinX = -9f;
    public float arenaMaxX = 9f;
    public float arenaMinY = 2f;
    public float arenaMaxY = 4f;

    [Header("Estado")]
    public bool isGameOver = false;
    public bool isOrganDead = false;
    public bool isPaused = false;

    [Header("UI Game Over")]
    public GameObject gameOverPanel;
    public Text gameOverReasonText;  // Texto de motivo (opcional)

    [Header("UI Power Up (Apenas fase 1)")]
    public GameObject powerUpPanel;

    // Spawn state
    private float spawnTimer;
    private int nextEnemyTypeIndex = 0; // Alterna entre tipos de inimigo

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // Resgata o phase correto (1 ou 2)
        currentPhase = GlobalState.currentPhase;
        totalTime = currentPhase == 1 ? 10f : 180f;

        if (GlobalState.hasHeartAndBodyHP)
            bodyMaxHP *= 1.3f;

        if (GlobalState.savedBodyHP > 0f)
            bodyCurrentHP = GlobalState.savedBodyHP;
        else
            bodyCurrentHP = bodyMaxHP;

        timeRemaining = totalTime;
        organCurrentHP = organMaxHP;
        damageMultiplier = currentPhase;
        spawnTimer = 2f;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (powerUpPanel != null) powerUpPanel.SetActive(false);

        // Auto-fiação dos botões do Game Over (via Transform.Find - funciona em objetos inativos)
        if (gameOverPanel != null)
        {
            WireButton(gameOverPanel.transform, "RestartBtn", RestartPhase);
            WireButton(gameOverPanel.transform, "GoMenuBtn", ReturnToMenu);
        }

        if (powerUpPanel != null)
        {
            WireButton(powerUpPanel.transform, "PowerUpBox/TripleBtn", ChoosePowerUpTripleShot);
            WireButton(powerUpPanel.transform, "PowerUpBox/SpeedBtn", ChoosePowerUpSpeedBoost);
            WireButton(powerUpPanel.transform, "PowerUpBox/LifeBtn", ChoosePowerUpHeartAndBody);
        }

        // Auto-fiação do painel de pausa e ajuste do HUD de texto
        HUDManager hud = FindFirstObjectByType<HUDManager>();
        if (hud != null)
        {
            if (hud.pausePanel != null)
            {
                WireButton(hud.pausePanel.transform, "ContinueBtn", () => hud.TogglePause());
                WireButton(hud.pausePanel.transform, "MenuBtn", ReturnToMenu);
            }
            
            // Ajustar o texto do HUD dinamicamente para corresponder à fase conectada
            if (hud.phaseNameText != null)
            {
                hud.phaseNameText.text = currentPhase == 1 ? "FASE 1 — PULMÃO" : "FASE 2 — CORAÇÃO";
            }
        }

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.OnPlayerDeath += OnPlayerDied;
    }

    void Update()
    {
        if (isGameOver || isPaused) return;

        // Tempo e Speed Scaling
        timeRemaining -= Time.deltaTime;
        float minutesPassed = (totalTime - timeRemaining) / 60f;
        speedMultiplier = 1f + (minutesPassed * 0.1f); // 10% de aumento por minuto

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            OnPhaseCompleted();
            return;
        }

        // Spawn (agora escala com velocidade, ou seja, nasce mais rápido)
        spawnTimer -= Time.deltaTime * speedMultiplier;
        if (spawnTimer <= 0f)
        {
            TrySpawnEnemy();
            float progress = 1f - (timeRemaining / totalTime);
            spawnTimer = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, progress);
        }
    }

    /// <summary>
    /// Tenta fazer spawn de um inimigo respeitando os limites por tipo e total.
    /// Alterna entre tipos para balancear.
    /// </summary>
    void TrySpawnEnemy()
    {
        // Contar ativos
        int antiCorpoCount = FindObjectsByType<EnemyShooter>(FindObjectsSortMode.None).Length;
        int playerShooterCount = FindObjectsByType<EnemyPlayerShooter>(FindObjectsSortMode.None).Length;
        int kamikazeCount = FindObjectsByType<EnemyKamikaze>(FindObjectsSortMode.None).Length;
        int totalCount = antiCorpoCount + playerShooterCount + kamikazeCount;

        if (totalCount >= maxTotalEnemies) return;

        // Amarelos (AntiCorpo) devem ter preferência, então não ficam restritos ao maxPerType
        bool canSpawnAntiCorpo = antiCorpoPrefab != null && antiCorpoCount < maxTotalEnemies;
        bool canSpawnPlayerShooter = playerShooterPrefab != null && playerShooterCount < maxPerType;
        bool canSpawnKamikaze = kamikazePrefab != null && kamikazeCount < maxPerType;

        if (!canSpawnAntiCorpo && !canSpawnPlayerShooter && !canSpawnKamikaze) return;

        GameObject chosenPrefab = null;
        
        // Probabilidade ponderada: 60% Amarelo, 20% Roxo, 20% Kamikaze
        float roll = Random.value;
        if (roll < 0.6f && canSpawnAntiCorpo) chosenPrefab = antiCorpoPrefab;
        else if (roll < 0.8f && canSpawnPlayerShooter) chosenPrefab = playerShooterPrefab;
        else if (canSpawnKamikaze) chosenPrefab = kamikazePrefab;
        
        // Fallback
        if (chosenPrefab == null)
        {
            if (canSpawnAntiCorpo) chosenPrefab = antiCorpoPrefab;
            else if (canSpawnPlayerShooter) chosenPrefab = playerShooterPrefab;
            else if (canSpawnKamikaze) chosenPrefab = kamikazePrefab;
        }

        nextEnemyTypeIndex++;

        // Escolher posição baseada no inimigo
        Vector3 spawnPos;
        if (chosenPrefab == kamikazePrefab)
        {
            Vector3 playerPos = Vector3.zero;
            GameObject pObj = GameObject.FindWithTag("Player");
            if (pObj != null) playerPos = pObj.transform.position;
            spawnPos = GetSafeKamikazeSpawnPos(playerPos);
        }
        else
        {
            spawnPos = GetRandomSpawnPos();
        }

        GameObject enemy = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);

        // Configurar fase no EnemyShooter (AntiCorpo)
        EnemyShooter shooter = enemy.GetComponent<EnemyShooter>();
        if (shooter != null)
        {
            shooter.currentPhase = currentPhase;
            shooter.arenaMinX = arenaMinX;
            shooter.arenaMaxX = arenaMaxX;
        }

        // Configurar fase no EnemyPlayerShooter
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
        // Spawnar no quadrante oposto ao player
        float spawnX, spawnY;

        if (playerPos.x < 0)
            spawnX = Random.Range(2f, arenaMaxX - 1f); // Player esquerda -> Spawn direita
        else
            spawnX = Random.Range(arenaMinX + 1f, -2f); // Player direita -> Spawn esquerda

        // O Y do player geralmente cai entre -4.5 e 4.5. Vamos assumir 0 como meio
        if (playerPos.y < 0)
            spawnY = Random.Range(1f, arenaMaxY - 1f); // Player embaixo -> Spawn em cima
        else
            spawnY = Random.Range(-3f, -1f); // Player em cima -> Spawn embaixo
            
        return new Vector3(spawnX, spawnY, 0f);
    }

    /// <summary>
    /// Chamado quando o cenário recebe dano (projéteis AntiCorpo).
    /// </summary>
    public void ApplyOrganDamage(float rawDamage)
    {
        if (isOrganDead) return;

        organCurrentHP -= rawDamage;
        float bodyDamage = rawDamage * damageMultiplier;
        bodyCurrentHP -= bodyDamage;

        if (organCurrentHP <= 0f)
        {
            organCurrentHP = 0f;
            if (!isOrganDead)
            {
                isOrganDead = true;
                if (currentPhase == 1)
                {
                    OnPhaseCompleted();
                    return;
                }
            }
        }

        if (bodyCurrentHP <= 0f)
        {
            bodyCurrentHP = 0f;
            GameOver("FALÊNCIA SISTÊMICA");
        }
    }

    void OnPlayerDied() => GameOver("GLÓBULO BRANCO DESTRUÍDO");

    void GameOver(string reason)
    {
        isGameOver = true;
        Debug.Log($"GAME OVER: {reason}");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // Exibir motivo se o texto estiver configurado
            if (gameOverReasonText != null)
                gameOverReasonText.text = reason;
            else
            {
                // Tentar encontrar texto de razão pelo nome
                var goSub = gameOverPanel.transform.Find("GOSub");
                if (goSub != null)
                {
                    var txt = goSub.GetComponent<Text>();
                    if (txt != null) txt.text = reason;
                }
            }
        }

        // Parar time
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

    void OnPhaseCompleted()
    {
        isGameOver = true;
        Debug.Log($"Fase {currentPhase} completada! Órgão morto: {isOrganDead}");

        if (currentPhase == 1 && powerUpPanel != null)
        {
            powerUpPanel.SetActive(true);
            StartCoroutine(SlowTimeAndStop());
        }
    }

    // ==== Escolhas de PowerUP ====
    public void ChoosePowerUpTripleShot()
    {
        GlobalState.hasTripleShot = true;
        AdvanceToNextPhase();
    }

    public void ChoosePowerUpSpeedBoost()
    {
        GlobalState.hasSpeedBoost = true;
        AdvanceToNextPhase();
    }

    public void ChoosePowerUpHeartAndBody()
    {
        GlobalState.hasHeartAndBodyHP = true;
        
        // Aumenta vida limitando estrito e recupera exatamente o tanto que aumentou
        float gain = bodyMaxHP * 0.3f;
        GlobalState.savedBodyHP = bodyCurrentHP + gain;

        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
            GlobalState.savedPlayerHearts = ph.currentHearts + 1;
        else
            GlobalState.savedPlayerHearts = 6;
            
        AdvanceToNextPhase();
    }

    void AdvanceToNextPhase()
    {
        Time.timeScale = 1f;
        if (!GlobalState.hasHeartAndBodyHP)
        {
            GlobalState.savedBodyHP = bodyCurrentHP;
            PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
            if (ph != null) GlobalState.savedPlayerHearts = ph.currentHearts;
        }

        GlobalState.currentPhase = 2; // Avança a fase internamente
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Recarrega a mesma cena (única), o GameManager adaptará o contexto
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        GlobalState.ResetState();
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartPhase()
    {
        Time.timeScale = 1f;
        GlobalState.ResetState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Utilitário: busca botão pelo nome dentro de um Transform e conecta a ação.
    /// Funciona mesmo em objetos inativos (Transform.Find não depende de SetActive).
    /// </summary>
    void WireButton(Transform parent, string childName, UnityEngine.Events.UnityAction action)
    {
        Transform child = parent.Find(childName);
        if (child == null) return;
        Button btn = child.GetComponent<Button>();
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
